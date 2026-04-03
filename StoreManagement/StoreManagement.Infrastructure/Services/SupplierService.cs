using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.Partners;
using StoreManagement.Shared.Entities.Finance;
using StoreManagement.Shared.Entities.Sales;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

/// <summary>
/// طبقة Business Logic للموردين
/// جميع التحسينات المُطبَّقة:
///   ✅ MemoryCache لـ Profile endpoint (TTL = 10 ثواني)
///   ✅ Guard ضد double activate/deactivate
///   ✅ Phone Primary Enforcement (واحد فقط per supplier)
///   ✅ Audit Trail (StatusChangedAt + StatusChangedBy)
///   ✅ حذف آمن مع فحص FK على Invoices وPayments
/// </summary>
public class SupplierService : ISupplierService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFinanceService _financeService;
    private readonly IMemoryCache _cache;

    private string ProfileCacheKey(int supplierId) =>
        $"partner_profile_supp_{_currentUser.CompanyId}_{supplierId}";

    private static readonly TimeSpan ProfileCacheTtl = TimeSpan.FromSeconds(10);

    public SupplierService(
        StoreDbContext context,
        ICurrentUserService currentUser,
        IFinanceService financeService,
        IMemoryCache cache)
    {
        _context = context;
        _currentUser = currentUser;
        _financeService = financeService;
        _cache = cache;
    }

    // ============================================================
    // القراءة والبحث
    // ============================================================

    public async Task<PagedResult<SupplierReadDto>> GetAllAsync(SupplierFilterDto filter)
    {
        var query = _context.Suppliers
            .Include(s => s.Phones)
            .AsQueryable();

        if (filter.IsActive.HasValue)
            query = query.Where(s => s.IsActive == filter.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(search) ||
                (s.Code != null && s.Code.ToLower().Contains(search)) ||
                s.Phones.Any(p => p.PhoneNumber.Contains(search)));
        }

        if (filter.HasOpenInvoices == true)
        {
            var companyId = _currentUser.CompanyId!.Value;
            var suppliersWithOpenInvoices = await _context.Invoices
                .Where(i => i.CompanyId == companyId
                         && i.Status == InvoiceStatus.Confirmed
                         && i.PaymentStatus != PaymentStatus.Paid
                         && i.Type == InvoiceType.Purchase
                         && i.SupplierId != null)
                .Select(i => i.SupplierId!.Value)
                .Distinct()
                .ToListAsync();

            query = query.Where(s => suppliersWithOpenInvoices.Contains(s.Id));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(s => s.Name)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(s => new SupplierReadDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Address = s.Address,
                Email = s.Email,
                Notes = s.Notes,
                IsActive = s.IsActive,
                CashBalance = s.CashBalance,
                OpeningBalance = s.OpeningBalance,
                PrimaryPhone = s.Phones
                    .Where(p => p.IsPrimary)
                    .Select(p => p.PhoneNumber)
                    .FirstOrDefault()
                    ?? s.Phones.Select(p => p.PhoneNumber).FirstOrDefault(),
                Phones = s.Phones.Select(p => new PhoneDto
                {
                    PhoneNumber = p.PhoneNumber,
                    IsPrimary = p.IsPrimary
                }).ToList(),
                CreatedDate = s.CreatedDate,
                StatusChangedAt = s.StatusChangedAt,
                StatusChangedBy = s.StatusChangedBy
            })
            .ToListAsync();

        // فلتر HasPayable — يحتاج حساب الرصيد (Post-query)
        if (filter.HasPayable == true)
        {
            var supplierIds = items.Select(s => s.Id).ToList();
            var payables = new List<int>();
            foreach (var sid in supplierIds)
            {
                var balance = await _financeService.GetSupplierCurrentBalanceAsync(sid);
                if (balance > 0) payables.Add(sid);
            }
            items = items.Where(s => payables.Contains(s.Id)).ToList();
        }

        return new PagedResult<SupplierReadDto>
        {
            Items = items,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = total
        };
    }

    public async Task<SupplierReadDto?> GetByIdAsync(int id)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.Phones)
            .FirstOrDefaultAsync(s => s.Id == id);

        return supplier is null ? null : MapToReadDto(supplier);
    }

    // ============================================================
    // الإنشاء والتعديل
    // ============================================================

    public async Task<SupplierReadDto> CreateAsync(CreateSupplierDto dto)
    {
        var companyId = _currentUser.CompanyId!.Value;

        if (!string.IsNullOrWhiteSpace(dto.Code))
        {
            var codeExists = await _context.Suppliers
                .AnyAsync(s => s.CompanyId == companyId && s.Code == dto.Code);
            if (codeExists)
                throw new InvalidOperationException($"الكود '{dto.Code}' مستخدم بالفعل لمورد آخر.");
        }

        var supplier = new Supplier
        {
            Name = dto.Name,
            Code = dto.Code?.Trim(),
            Address = dto.Address?.Trim(),
            Email = dto.Email?.Trim(),
            Notes = dto.Notes?.Trim(),
            OpeningBalance = dto.OpeningBalance,
            CashBalance = dto.OpeningBalance,
            IsActive = true,
            CompanyId = companyId
        };

        // ✅ Phone Primary Enforcement
        AddPhonesWithPrimaryEnforcement(supplier.Phones, dto.Phones);

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return MapToReadDto(supplier);
    }

    public async Task UpdateAsync(int id, UpdateSupplierDto dto)
    {
        var companyId = _currentUser.CompanyId!.Value;

        var supplier = await _context.Suppliers
            .Include(s => s.Phones)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException($"المورد رقم {id} غير موجود");

        if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code != supplier.Code)
        {
            var codeExists = await _context.Suppliers
                .AnyAsync(s => s.CompanyId == companyId && s.Code == dto.Code && s.Id != id);
            if (codeExists)
                throw new InvalidOperationException($"الكود '{dto.Code}' مستخدم بالفعل لمورد آخر.");
        }

        supplier.Name = dto.Name;
        supplier.Code = dto.Code?.Trim();
        supplier.Address = dto.Address?.Trim();
        supplier.Email = dto.Email?.Trim();
        supplier.Notes = dto.Notes?.Trim();

        _context.SupplierPhones.RemoveRange(supplier.Phones);
        supplier.Phones.Clear();
        AddPhonesWithPrimaryEnforcement(supplier.Phones, dto.Phones);

        _cache.Remove(ProfileCacheKey(id));
        await _context.SaveChangesAsync();
    }

    // ============================================================
    // سياسة الحذف والتفعيل
    // ============================================================

    public async Task DeleteAsync(int id)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException($"المورد رقم {id} غير موجود");

        var hasInvoices = await _context.Invoices.AnyAsync(i => i.SupplierId == id);
        if (hasInvoices)
            throw new InvalidOperationException(
                "لا يمكن حذف هذا المورد لأنه مرتبط بفواتير مشتريات. استخدم خيار التعطيل.");

        var hasPayments = await _context.SupplierPayments.AnyAsync(p => p.SupplierId == id);
        if (hasPayments)
            throw new InvalidOperationException(
                "لا يمكن حذف هذا المورد لأنه مرتبط بسندات صرف. استخدم خيار التعطيل.");

        _cache.Remove(ProfileCacheKey(id));
        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();
    }

    public async Task ActivateAsync(int id)
    {
        var supplier = await _context.Suppliers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id && s.CompanyId == _currentUser.CompanyId)
            ?? throw new KeyNotFoundException($"المورد رقم {id} غير موجود");

        // ✅ Guard: المورد نشط بالفعل — لا تهدر قاعدة البيانات
        if (supplier.IsActive)
            return;

        supplier.IsActive = true;

        // ✅ Audit Trail
        supplier.StatusChangedAt = DateTime.UtcNow;
        supplier.StatusChangedBy = _currentUser.UserName ?? _currentUser.UserId?.ToString();

        _cache.Remove(ProfileCacheKey(id));
        await _context.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int id)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException($"المورد رقم {id} غير موجود");

        // ✅ Guard: المورد معطّل بالفعل
        if (!supplier.IsActive)
            return;

        supplier.IsActive = false;

        // ✅ Audit Trail
        supplier.StatusChangedAt = DateTime.UtcNow;
        supplier.StatusChangedBy = _currentUser.UserName ?? _currentUser.UserId?.ToString();

        _cache.Remove(ProfileCacheKey(id));
        await _context.SaveChangesAsync();
    }

    // ============================================================
    // ملف المورد الشامل (Profile) — مع MemoryCache
    // ============================================================

    public async Task<SupplierProfileDto> GetProfileAsync(int id)
    {
        var cacheKey = ProfileCacheKey(id);

        // ✅ Cache Hit
        if (_cache.TryGetValue(cacheKey, out SupplierProfileDto? cached) && cached is not null)
            return cached;

        var supplier = await _context.Suppliers
            .Include(s => s.Phones)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException($"المورد رقم {id} غير موجود");

        var openInvoices = await _financeService.GetOpenSupplierInvoicesAsync(id);
        var unallocatedPayments = await _financeService.GetUnallocatedSupplierPaymentsAsync(id);

        var purchaseTotals = await _context.Invoices
            .Where(i => i.SupplierId == id
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Type == InvoiceType.Purchase)
            .SumAsync(i => (decimal?)i.NetTotal) ?? 0m;

        var returnTotals = await _context.Invoices
            .Where(i => i.SupplierId == id
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Type == InvoiceType.PurchaseReturn)
            .SumAsync(i => (decimal?)i.NetTotal) ?? 0m;

        var paymentTotals = await _context.SupplierPayments
            .Where(p => p.SupplierId == id)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var currentBalance = supplier.OpeningBalance + (purchaseTotals - returnTotals) - paymentTotals;

        // آخر 5 فواتير فقط — Profile يعرض Summary لا Full Report
        var recentInvoices = await _context.Invoices
            .Where(i => i.SupplierId == id && i.Status == InvoiceStatus.Confirmed)
            .OrderByDescending(i => i.Date)
            .Take(5)
            .Select(i => new InvoiceSummaryDto
            {
                Id = i.Id,
                InvoiceType = i.Type.ToString(),
                Date = i.Date,
                NetTotal = i.NetTotal,
                PaymentStatus = i.PaymentStatus.ToString(),
                Remaining = i.RemainingAmount
            })
            .ToListAsync();

        var recentPayments = await _context.SupplierPayments
            .Where(p => p.SupplierId == id)
            .OrderByDescending(p => p.Date)
            .Take(5)
            .Select(p => new ReceiptSummaryDto
            {
                Id = p.Id,
                Date = p.Date,
                Amount = p.Amount,
                UnallocatedAmount = p.UnallocatedAmount,
                Method = p.Method.ToString()
            })
            .ToListAsync();

        var profile = new SupplierProfileDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            Code = supplier.Code,
            Address = supplier.Address,
            Email = supplier.Email,
            Notes = supplier.Notes,
            IsActive = supplier.IsActive,
            CreatedDate = supplier.CreatedDate,
            Phones = supplier.Phones.Select(p => new PhoneDto
            {
                PhoneNumber = p.PhoneNumber,
                IsPrimary = p.IsPrimary
            }).ToList(),
            OpeningBalance = supplier.OpeningBalance,
            TotalPurchased = purchaseTotals - returnTotals,
            TotalPaid = paymentTotals,
            CurrentBalance = currentBalance,
            TotalOutstanding = openInvoices.Sum(i => i.Remaining),
            UnallocatedPayments = unallocatedPayments.Sum(p => p.UnallocatedAmount),
            OpenInvoicesCount = openInvoices.Count,
            RecentInvoices = recentInvoices,
            RecentPayments = recentPayments
        };

        // ✅ Cache Store
        _cache.Set(cacheKey, profile, ProfileCacheTtl);

        return profile;
    }

    // ============================================================
    // Helper Methods
    // ============================================================

    private static SupplierReadDto MapToReadDto(Supplier supplier)
    {
        return new SupplierReadDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            Code = supplier.Code,
            Address = supplier.Address,
            Email = supplier.Email,
            Notes = supplier.Notes,
            IsActive = supplier.IsActive,
            CashBalance = supplier.CashBalance,
            OpeningBalance = supplier.OpeningBalance,
            PrimaryPhone = supplier.Phones
                .FirstOrDefault(p => p.IsPrimary)?.PhoneNumber
                ?? supplier.Phones.FirstOrDefault()?.PhoneNumber,
            Phones = supplier.Phones.Select(p => new PhoneDto
            {
                PhoneNumber = p.PhoneNumber,
                IsPrimary = p.IsPrimary
            }).ToList(),
            CreatedDate = supplier.CreatedDate,
            StatusChangedAt = supplier.StatusChangedAt,
            StatusChangedBy = supplier.StatusChangedBy
        };
    }

    /// <summary>
    /// ✅ Phone Primary Enforcement للموردين
    /// يضمن رقماً رئيسياً واحداً فقط، وإذا لم يُحدَّد يُعيَّن الأول تلقائياً
    /// </summary>
    private static void AddPhonesWithPrimaryEnforcement(
        ICollection<SupplierPhone> phones,
        List<PhoneDto> phoneDtos)
    {
        if (!phoneDtos.Any()) return;

        var primaryCount = phoneDtos.Count(p => p.IsPrimary);

        if (primaryCount > 1)
            throw new InvalidOperationException(
                "لا يمكن تحديد أكثر من رقم هاتف أساسي واحد (IsPrimary = true) في نفس الوقت.");

        var hasPrimary = primaryCount == 1;

        var phonesToAdd = phoneDtos.Select((p, index) => new SupplierPhone
        {
            PhoneNumber = p.PhoneNumber.Trim(),
            IsPrimary = hasPrimary ? p.IsPrimary : index == 0
        });

        foreach (var phone in phonesToAdd)
            phones.Add(phone);
    }
}
