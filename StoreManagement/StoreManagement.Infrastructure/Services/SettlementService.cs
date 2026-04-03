using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.Finance;
using StoreManagement.Shared.Entities.Core;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

public class SettlementService : ISettlementService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SettlementService(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<SettlementReadDto>> GetAllAsync(
        PaginationQueryDto query, SettlementSource? source, SettlementType? type, DateTime? from, DateTime? to)
    {
        var baseQuery = _context.AccountSettlements
            .Include(s => s.User)
            .AsQueryable();

        if (source.HasValue) baseQuery = baseQuery.Where(s => s.SourceType == source.Value);
        if (type.HasValue) baseQuery = baseQuery.Where(s => s.Type == type.Value);
        if (from.HasValue) baseQuery = baseQuery.Where(s => s.Date >= from.Value);
        if (to.HasValue) baseQuery = baseQuery.Where(s => s.Date <= to.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(s => s.Notes != null && s.Notes.Contains(query.Search));

        var company = await _context.Companies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == _currentUser.CompanyId);
        var merchantName = company?.Name ?? "DafterPro";

        // جلب IDs المرتبطة دفعة واحدة لتجنب N+1
        var customerIds = await baseQuery
            .Where(s => s.SourceType == SettlementSource.Customer)
            .Select(s => s.RelatedEntityId)
            .Distinct()
            .ToListAsync();

        var supplierIds = await baseQuery
            .Where(s => s.SourceType == SettlementSource.Supplier)
            .Select(s => s.RelatedEntityId)
            .Distinct()
            .ToListAsync();

        var customers = await _context.Customers.IgnoreQueryFilters()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var suppliers = await _context.Suppliers.IgnoreQueryFilters()
            .Where(s => supplierIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var total = await baseQuery.CountAsync();
        var rawItems = await baseQuery
            .OrderByDescending(s => s.Date)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var items = rawItems.Select(s => new SettlementReadDto
        {
            Id = s.Id,
            SourceType = s.SourceType.ToString(),
            RelatedEntityName = s.SourceType == SettlementSource.Customer
                ? customers.GetValueOrDefault(s.RelatedEntityId)
                : suppliers.GetValueOrDefault(s.RelatedEntityId),
            Type = s.Type.ToString(),
            Amount = s.Amount,
            Date = s.Date,
            Notes = s.Notes,
            UserName = s.User?.UserName ?? "Unknown",
            MerchantName = merchantName
        }).ToList();

        return new PagedResult<SettlementReadDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task<SettlementReadDto> CreateAsync(CreateSettlementDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // التحقق من وجود الكيان المرتبط
            string? partnerName = null;
            if ((SettlementSource)dto.SourceType == SettlementSource.Customer)
            {
                var customer = await _context.Customers.FindAsync(dto.RelatedEntityId)
                    ?? throw new KeyNotFoundException($"العميل رقم {dto.RelatedEntityId} غير موجود");
                partnerName = customer.Name;
            }
            else
            {
                var supplier = await _context.Suppliers.FindAsync(dto.RelatedEntityId)
                    ?? throw new KeyNotFoundException($"المورد رقم {dto.RelatedEntityId} غير موجود");
                partnerName = supplier.Name;
            }

            // تسجيل مستند التسوية فقط — لا تعديل على CashBalance
            // الرصيد يُحسب من Receipts/Invoices/Settlements بشكل ديناميكي
            var settlement = new AccountSettlement
            {
                SourceType = (SettlementSource)dto.SourceType,
                RelatedEntityId = dto.RelatedEntityId,
                Type = (SettlementType)dto.Type,
                Amount = dto.Amount,
                Date = dto.Date,
                Notes = dto.Notes,
                CompanyId = (int)_currentUser.CompanyId!,
                UserId = (int)_currentUser.UserId!
            };

            _context.AccountSettlements.Add(settlement);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var user = await _context.Users.FindAsync(_currentUser.UserId);
            var company = await _context.Companies.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == settlement.CompanyId);

            return new SettlementReadDto
            {
                Id = settlement.Id,
                SourceType = settlement.SourceType.ToString(),
                RelatedEntityName = partnerName,
                Type = settlement.Type.ToString(),
                Amount = settlement.Amount,
                Date = settlement.Date,
                Notes = settlement.Notes,
                UserName = user?.UserName ?? "Unknown",
                MerchantName = company?.Name ?? "DafterPro"
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
