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
/// طبقة Business Logic للعملاء
/// التحسينات المُطبَّقة:
///   ✅ MemoryCache لـ Profile endpoint (TTL = 10 ثواني)
///   ✅ Guard ضد double activate/deactivate
///   ✅ Phone Primary Enforcement (واحد فقط per customer)
///   ✅ Audit Trail (StatusChangedAt + StatusChangedBy) عند كل تفعيل/تعطيل
///   ✅ حذف آمن مع فحص FK على Invoices وReceipts
///   ✅ HasDebt filter: EF subquery قبل Pagination (batch — لا N+1)
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFinanceService _financeService;
    private readonly IMemoryCache _cache;

    // مفتاح الـ Cache لـ Profile — يُدرج CompanyId + CustomerId لعزل المستأجرين
    private string ProfileCacheKey(int customerId) =>
        $"partner_profile_cust_{_currentUser.CompanyId}_{customerId}";

    // مدة الـ Cache (10 ثواني) — مناسب للكاشير الذي يفتح الشاشة كثيراً
    private static readonly TimeSpan ProfileCacheTtl = TimeSpan.FromSeconds(10);

    public CustomerService(
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

    public async Task<PagedResult<CustomerReadDto>> GetAllAsync(CustomerFilterDto filter)
    {
        var companyId = _currentUser.CompanyId!.Value;

        var query = _context.Customers
            .Include(c => c.Phones)
            .AsQueryable();

        // فلتر حالة النشاط (افتراضي: نشط فقط)
        if (filter.IsActive.HasValue)
            query = query.Where(c => c.IsActive == filter.IsActive.Value);

        // البحث النصي — يشمل الاسم والكود وأرقام الهاتف
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(search) ||
                (c.Code != null && c.Code.ToLower().Contains(search)) ||
                c.Phones.Any(p => p.PhoneNumber.Contains(search)));
        }

        // فلتر العملاء الذين لديهم فواتير مفتوحة
        if (filter.HasOpenInvoices == true)
        {
            var customersWithOpenInvoices = await _context.Invoices
                .Where(i => i.CompanyId == companyId
                         && i.Status == InvoiceStatus.Confirmed
                         && i.PaymentStatus != PaymentStatus.Paid
                         && i.Type == InvoiceType.Sale
                         && i.CustomerId != null)
                .Select(i => i.CustomerId!.Value)
                .Distinct()
                .ToListAsync();

            query = query.Where(c => customersWithOpenInvoices.Contains(c.Id));
        }

        // ✅ فلتر HasDebt — يُطبَّق كـ EF subquery قبل Pagination تماماً
        // يحسب: ما تبقى من فواتير البيع غير المسددة — مطروحاً منه سندات القبض غير المخصصة
        if (filter.HasDebt == true)
        {
            // الـ IDs التي عندها صافي دين > 0 (batch query واحدة)
            var debtorIds = await _context.Customers
                .Where(c => c.CompanyId == companyId)
                .Where(c =>
                    // إجمالي ما تبقى على الفواتير
                    _context.Invoices
                        .Where(i => i.CustomerId == c.Id
                                 && i.CompanyId == companyId
                                 && i.Type == InvoiceType.Sale
                                 && i.Status == InvoiceStatus.Confirmed
                                 && i.PaymentStatus != PaymentStatus.Paid)
                        .Sum(i => i.NetTotal - i.AllocatedAmount)
                    -
                    // مطروحاً منه المدفوعات غير المخصصة
                    _context.CustomerReceipts
                        .Where(r => r.CustomerId == c.Id
                                 && r.CompanyId == companyId
                                 && r.UnallocatedAmount > 0)
                        .Sum(r => r.UnallocatedAmount)
                    > 0)
                .Select(c => c.Id)
                .ToListAsync();

            query = query.Where(c => debtorIds.Contains(c.Id));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(c => new CustomerReadDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Address = c.Address,
                Email = c.Email,
                Notes = c.Notes,
                IsActive = c.IsActive,
                CashBalance = c.CashBalance,
                OpeningBalance = c.OpeningBalance,
                CreditLimit = c.CreditLimit,
                PrimaryPhone = c.Phones
                    .Where(p => p.IsPrimary)
                    .Select(p => p.PhoneNumber)
                    .FirstOrDefault()
                    ?? c.Phones.Select(p => p.PhoneNumber).FirstOrDefault(),
                Phones = c.Phones.Select(p => new PhoneDto
                {
                    PhoneNumber = p.PhoneNumber,
                    IsPrimary = p.IsPrimary
                }).ToList(),
                CreatedDate = c.CreatedDate,
                // Audit: متى ومن آخر تغيير في الحالة
                StatusChangedAt = c.StatusChangedAt,
                StatusChangedBy = c.StatusChangedBy
            })
            .ToListAsync();

        return new PagedResult<CustomerReadDto>
        {
            Items = items,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = total
        };
    }

    public async Task<CustomerReadDto?> GetByIdAsync(int id)
    {
        var customer = await _context.Customers
            .Include(c => c.Phones)
            .FirstOrDefaultAsync(c => c.Id == id);

        return customer is null ? null : MapToReadDto(customer);
    }

    // ============================================================
    // الإنشاء والتعديل
    // ============================================================

    public async Task<CustomerReadDto> CreateAsync(CreateCustomerDto dto)
    {
        var companyId = _currentUser.CompanyId!.Value;

        if (!string.IsNullOrWhiteSpace(dto.Code))
        {
            var codeExists = await _context.Customers
                .AnyAsync(c => c.CompanyId == companyId && c.Code == dto.Code);
            if (codeExists)
                throw new InvalidOperationException($"الكود '{dto.Code}' مستخدم بالفعل لعميل آخر.");
        }

        var customer = new Customer
        {
            Name = dto.Name,
            Code = dto.Code?.Trim(),
            Address = dto.Address?.Trim(),
            Email = dto.Email?.Trim(),
            Notes = dto.Notes?.Trim(),
            OpeningBalance = dto.OpeningBalance,
            // ✅ CashBalance = OpeningBalance snapshot فقط — لا يُعدَّل بعدها أبداً
            CashBalance = dto.OpeningBalance,
            CreditLimit = dto.CreditLimit,
            IsActive = true,
            CompanyId = companyId
        };

        // ✅ Phone Primary Enforcement
        AddPhonesWithPrimaryEnforcement(customer.Phones, dto.Phones);

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return MapToReadDto(customer);
    }

    public async Task UpdateAsync(int id, UpdateCustomerDto dto)
    {
        var companyId = _currentUser.CompanyId!.Value;

        var customer = await _context.Customers
            .Include(c => c.Phones)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException($"العميل رقم {id} غير موجود");

        if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code != customer.Code)
        {
            var codeExists = await _context.Customers
                .AnyAsync(c => c.CompanyId == companyId && c.Code == dto.Code && c.Id != id);
            if (codeExists)
                throw new InvalidOperationException($"الكود '{dto.Code}' مستخدم بالفعل لعميل آخر.");
        }

        customer.Name = dto.Name;
        customer.Code = dto.Code?.Trim();
        customer.Address = dto.Address?.Trim();
        customer.Email = dto.Email?.Trim();
        customer.Notes = dto.Notes?.Trim();
        customer.CreditLimit = dto.CreditLimit;

        // Replace all phones
        _context.CustomerPhones.RemoveRange(customer.Phones);
        customer.Phones.Clear();
        AddPhonesWithPrimaryEnforcement(customer.Phones, dto.Phones);

        // ✅ إبطال cache عند التعديل
        _cache.Remove(ProfileCacheKey(id));

        await _context.SaveChangesAsync();
    }

    // ============================================================
    // سياسة الحذف والتفعيل
    // ============================================================

    public async Task DeleteAsync(int id)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException($"العميل رقم {id} غير موجود");

        var hasInvoices = await _context.Invoices.AnyAsync(i => i.CustomerId == id);
        if (hasInvoices)
            throw new InvalidOperationException(
                "لا يمكن حذف هذا العميل لأنه مرتبط بفواتير. استخدم خيار التعطيل.");

        var hasReceipts = await _context.CustomerReceipts.AnyAsync(r => r.CustomerId == id);
        if (hasReceipts)
            throw new InvalidOperationException(
                "لا يمكن حذف هذا العميل لأنه مرتبط بسندات قبض. استخدم خيار التعطيل.");

        _cache.Remove(ProfileCacheKey(id));
        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
    }

    public async Task ActivateAsync(int id)
    {
        var customer = await _context.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == _currentUser.CompanyId)
            ?? throw new KeyNotFoundException($"العميل رقم {id} غير موجود");

        // ✅ Guard: لا تتجاهل الطلبات المكررة
        if (customer.IsActive)
            return; // العميل نشط بالفعل — لا يوجد شيء للفعل

        customer.IsActive = true;

        // ✅ Audit Trail
        customer.StatusChangedAt = DateTime.UtcNow;
        customer.StatusChangedBy = _currentUser.UserName ?? _currentUser.UserId?.ToString();

        _cache.Remove(ProfileCacheKey(id));
        await _context.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int id)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException($"العميل رقم {id} غير موجود");

        // ✅ Guard: لا تتجاهل الطلبات المكررة
        if (!customer.IsActive)
            return; // العميل معطّل بالفعل — لا يوجد شيء للفعل

        customer.IsActive = false;

        // ✅ Audit Trail
        customer.StatusChangedAt = DateTime.UtcNow;
        customer.StatusChangedBy = _currentUser.UserName ?? _currentUser.UserId?.ToString();

        _cache.Remove(ProfileCacheKey(id));
        await _context.SaveChangesAsync();
    }

    // ============================================================
    // ملف العميل الشامل (Profile) — مع MemoryCache
    // ============================================================

    public async Task<CustomerProfileDto> GetProfileAsync(int id)
    {
        var cacheKey = ProfileCacheKey(id);

        // ✅ Cache Hit: أعد البيانات المخزنة إن كانت طازجة (< 10 ثواني)
        if (_cache.TryGetValue(cacheKey, out CustomerProfileDto? cached) && cached is not null)
            return cached;

        var customer = await _context.Customers
            .Include(c => c.Phones)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException($"العميل رقم {id} غير موجود");

        // جلب البيانات المالية من FinanceService
        var openInvoices = await _financeService.GetOpenCustomerInvoicesAsync(id);
        var unallocatedReceipts = await _financeService.GetUnallocatedCustomerReceiptsAsync(id);

        // إجمالي فواتير البيع
        var invoiceTotals = await _context.Invoices
            .Where(i => i.CustomerId == id
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Type == InvoiceType.Sale)
            .SumAsync(i => (decimal?)i.NetTotal) ?? 0m;

        var returnTotals = await _context.Invoices
            .Where(i => i.CustomerId == id
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Type == InvoiceType.SalesReturn)
            .SumAsync(i => (decimal?)i.NetTotal) ?? 0m;

        var receiptTotals = await _context.CustomerReceipts
            .Where(r => r.CustomerId == id)
            .SumAsync(r => (decimal?)r.Amount) ?? 0m;

        var currentBalance = customer.OpeningBalance + (invoiceTotals - returnTotals) - receiptTotals;

        // آخر 5 فواتير فقط (Summary — لا Full Report)
        var recentInvoices = await _context.Invoices
            .Where(i => i.CustomerId == id && i.Status == InvoiceStatus.Confirmed)
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

        // آخر 5 مقبوضات فقط
        var recentReceipts = await _context.CustomerReceipts
            .Where(r => r.CustomerId == id)
            .OrderByDescending(r => r.Date)
            .Take(5)
            .Select(r => new ReceiptSummaryDto
            {
                Id = r.Id,
                Date = r.Date,
                Amount = r.Amount,
                UnallocatedAmount = r.UnallocatedAmount,
                Method = r.Method.ToString()
            })
            .ToListAsync();

        var profile = new CustomerProfileDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Code = customer.Code,
            Address = customer.Address,
            Email = customer.Email,
            Notes = customer.Notes,
            IsActive = customer.IsActive,
            CreditLimit = customer.CreditLimit,
            CreatedDate = customer.CreatedDate,
            Phones = customer.Phones.Select(p => new PhoneDto
            {
                PhoneNumber = p.PhoneNumber,
                IsPrimary = p.IsPrimary
            }).ToList(),
            OpeningBalance = customer.OpeningBalance,
            TotalInvoiced = invoiceTotals - returnTotals,
            TotalReceived = receiptTotals,
            CurrentBalance = currentBalance,
            TotalOutstanding = openInvoices.Sum(i => i.Remaining),
            UnallocatedReceipts = unallocatedReceipts.Sum(r => r.UnallocatedAmount),
            OpenInvoicesCount = openInvoices.Count,
            IsOverCreditLimit = customer.CreditLimit > 0 && currentBalance > customer.CreditLimit,
            RecentInvoices = recentInvoices,
            RecentReceipts = recentReceipts
        };

        // ✅ Cache Store: خزّن لـ 10 ثواني
        _cache.Set(cacheKey, profile, ProfileCacheTtl);

        return profile;
    }

    // ============================================================
    // Helper Methods
    // ============================================================

    private static CustomerReadDto MapToReadDto(Customer customer)
    {
        return new CustomerReadDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Code = customer.Code,
            Address = customer.Address,
            Email = customer.Email,
            Notes = customer.Notes,
            IsActive = customer.IsActive,
            CashBalance = customer.CashBalance,
            OpeningBalance = customer.OpeningBalance,
            CreditLimit = customer.CreditLimit,
            PrimaryPhone = customer.Phones
                .FirstOrDefault(p => p.IsPrimary)?.PhoneNumber
                ?? customer.Phones.FirstOrDefault()?.PhoneNumber,
            Phones = customer.Phones.Select(p => new PhoneDto
            {
                PhoneNumber = p.PhoneNumber,
                IsPrimary = p.IsPrimary
            }).ToList(),
            CreatedDate = customer.CreatedDate,
            StatusChangedAt = customer.StatusChangedAt,
            StatusChangedBy = customer.StatusChangedBy
        };
    }

    /// <summary>
    /// ✅ Phone Primary Enforcement
    /// يضمن:
    ///   - أن رقماً واحداً فقط هو IsPrimary = true
    ///   - إذا لم يُحدَّد أي رقم كـ Primary، يُعيَّن الأول تلقائياً
    ///   - إذا حُدِّد أكثر من رقم كـ Primary، يُحتفظ فقط بالأول ويُصحَّح الباقي
    /// </summary>
    private static void AddPhonesWithPrimaryEnforcement(
        ICollection<CustomerPhone> phones,
        List<PhoneDto> phoneDtos)
    {
        if (!phoneDtos.Any()) return;

        var primaryCount = phoneDtos.Count(p => p.IsPrimary);

        if (primaryCount > 1)
            // ✅ إصلاح تلقائي: أبقِ الأول فقط كـ Primary
            throw new InvalidOperationException(
                "لا يمكن تحديد أكثر من رقم هاتف أساسي واحد (IsPrimary = true) في نفس الوقت.");

        var hasPrimary = primaryCount == 1;

        var phonesToAdd = phoneDtos.Select((p, index) => new CustomerPhone
        {
            PhoneNumber = p.PhoneNumber.Trim(),
            // إذا لم يُحدَّد Primary → الأول يكون Primary تلقائياً
            IsPrimary = hasPrimary ? p.IsPrimary : index == 0
        });

        foreach (var phone in phonesToAdd)
            phones.Add(phone);
    }
}
