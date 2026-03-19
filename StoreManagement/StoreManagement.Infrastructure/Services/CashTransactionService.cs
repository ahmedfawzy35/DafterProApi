using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

/// <summary>
/// خدمة إدارة المعاملات النقدية (Business Logic)
/// </summary>
public class CashTransactionService : ICashTransactionService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CashTransactionService(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<CashTransactionReadDto>> GetAllAsync(
        PaginationQueryDto query, TransactionType? type, TransactionSource? source, DateTime? from, DateTime? to)
    {
        var baseQuery = _context.CashTransactions
            .Include(t => t.User)
            .AsQueryable();

        if (type.HasValue) baseQuery = baseQuery.Where(t => t.Type == type.Value);
        if (source.HasValue) baseQuery = baseQuery.Where(t => t.SourceType == source.Value);
        if (from.HasValue) baseQuery = baseQuery.Where(t => t.Date >= from.Value);
        if (to.HasValue) baseQuery = baseQuery.Where(t => t.Date <= to.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            baseQuery = baseQuery.Where(t => 
                (t.Notes != null && t.Notes.Contains(query.Search)));
            
            // ملاحظة: البحث بالاسم يحتاج Join أو Include للعملاء والموردين
            // لتجنب التعقيد سنبقي البحث في الملاحظات حالياً أو نضيف Logique مخصص
        }

        var company = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == _currentUser.CompanyId);
        var merchantName = company?.Name ?? "DafterPro";

        var total = await baseQuery.CountAsync();
        var items = await baseQuery
            .OrderByDescending(t => t.Date)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new CashTransactionReadDto
            {
                Id = t.Id, Type = t.Type.ToString(),
                SourceType = t.SourceType.ToString(),
                Value = t.Value, Date = t.Date, Notes = t.Notes,
                UserName = t.User.UserName ?? "Unknown",
                RelatedEntityId = t.RelatedEntityId,
                MerchantName = merchantName
            }).ToListAsync();

        // جلب أسماء الكيانات المرتبطة بشكل منفصل لضمان السرعة
        foreach (var item in items)
        {
            if (item.RelatedEntityId.HasValue)
            {
                if (item.SourceType == TransactionSource.Customer.ToString())
                {
                    var customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == item.RelatedEntityId);
                    item.RelatedEntityName = customer?.Name;
                }
                else if (item.SourceType == TransactionSource.Supplier.ToString())
                {
                    var supplier = await _context.Suppliers.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == item.RelatedEntityId);
                    item.RelatedEntityName = supplier?.Name;
                }
            }
        }

        return new PagedResult<CashTransactionReadDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task<CashTransactionReadDto?> GetByIdAsync(int id)
    {
        var t = await _context.CashTransactions
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (t == null) return null;

        var company = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == t.CompanyId);
        
        var dtoResult = new CashTransactionReadDto
        {
            Id = t.Id,
            Type = t.Type.ToString(),
            SourceType = t.SourceType.ToString(),
            Value = t.Value,
            Date = t.Date,
            Notes = t.Notes,
            UserName = t.User.UserName ?? "Unknown",
            RelatedEntityId = t.RelatedEntityId,
            MerchantName = company?.Name ?? "DafterPro"
        };

        if (t.RelatedEntityId.HasValue)
        {
            if (t.SourceType == TransactionSource.Customer)
            {
                var customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == t.RelatedEntityId);
                dtoResult.RelatedEntityName = customer?.Name;
            }
            else if (t.SourceType == TransactionSource.Supplier)
            {
                var supplier = await _context.Suppliers.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == t.RelatedEntityId);
                dtoResult.RelatedEntityName = supplier?.Name;
            }
        }

        return dtoResult;
    }

    public async Task<CashTransactionReadDto> CreateAsync(CreateCashTransactionDto dto)
    {
        var transaction = new CashTransaction
        {
            Type = (TransactionType)dto.Type,
            SourceType = (TransactionSource)dto.SourceType,
            Value = dto.Value,
            Date = dto.Date,
            Notes = dto.Notes,
            RelatedEntityId = dto.RelatedEntityId,
            CompanyId = (int)_currentUser.CompanyId!,
            UserId = (int)_currentUser.UserId!
        };

        _context.CashTransactions.Add(transaction);

        // تحديث أرصدة العملاء/الموردين إذا كانت المعاملة مرتبطة بهم
        if (dto.RelatedEntityId.HasValue)
        {
            if (dto.SourceType == (int)TransactionSource.Customer)
            {
                var customer = await _context.Customers.FindAsync(dto.RelatedEntityId.Value);
                if (customer != null)
                {
                    // إذا كان "In" (وارد من عميل) -> يقلل المديونية (رصيد موجب يعني له، سالب يعني عليه)
                    // حسب منطق السيستم: CashBalance غالباً يعبر عن صافي الحساب
                    if (transaction.Type == TransactionType.In) customer.CashBalance += transaction.Value;
                    else customer.CashBalance -= transaction.Value;
                }
            }
            else if (dto.SourceType == (int)TransactionSource.Supplier)
            {
                var supplier = await _context.Suppliers.FindAsync(dto.RelatedEntityId.Value);
                if (supplier != null)
                {
                    // إذا كان "Out" (صادر لمورد) -> يقلل المديونية للمورد
                    if (transaction.Type == TransactionType.Out) supplier.CashBalance += transaction.Value;
                    else supplier.CashBalance -= transaction.Value;
                }
            }
        }

        await _context.SaveChangesAsync();

        // جلب اسم المستخدم للرد
        var user = await _context.Users.FindAsync(_currentUser.UserId);

        var companyEntity = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == transaction.CompanyId);

        var finalDto = new CashTransactionReadDto
        {
            Id = transaction.Id,
            Type = transaction.Type.ToString(),
            SourceType = transaction.SourceType.ToString(),
            Value = transaction.Value,
            Date = transaction.Date,
            Notes = transaction.Notes,
            UserName = user?.UserName ?? "Unknown",
            RelatedEntityId = transaction.RelatedEntityId,
            MerchantName = companyEntity?.Name ?? "DafterPro"
        };

        if (transaction.RelatedEntityId.HasValue)
        {
            if (transaction.SourceType == TransactionSource.Customer)
            {
                var customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == transaction.RelatedEntityId);
                finalDto.RelatedEntityName = customer?.Name;
            }
            else if (transaction.SourceType == TransactionSource.Supplier)
            {
                var supplier = await _context.Suppliers.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == transaction.RelatedEntityId);
                finalDto.RelatedEntityName = supplier?.Name;
            }
        }

        return finalDto;
    }

    public async Task DeleteAsync(int id)
    {
        var transaction = await _context.CashTransactions.FindAsync(id)
            ?? throw new KeyNotFoundException($"المعاملة رقم {id} غير موجودة");

        // عكس تأثير الرصيد عند الحذف
        if (transaction.RelatedEntityId.HasValue)
        {
            if (transaction.SourceType == TransactionSource.Customer)
            {
                var customer = await _context.Customers.FindAsync(transaction.RelatedEntityId.Value);
                if (customer != null)
                {
                    if (transaction.Type == TransactionType.In) customer.CashBalance -= transaction.Value;
                    else customer.CashBalance += transaction.Value;
                }
            }
            else if (transaction.SourceType == TransactionSource.Supplier)
            {
                var supplier = await _context.Suppliers.FindAsync(transaction.RelatedEntityId.Value);
                if (supplier != null)
                {
                    if (transaction.Type == TransactionType.Out) supplier.CashBalance -= transaction.Value;
                    else supplier.CashBalance += transaction.Value;
                }
            }
        }

        _context.CashTransactions.Remove(transaction);
        await _context.SaveChangesAsync();
    }
}
