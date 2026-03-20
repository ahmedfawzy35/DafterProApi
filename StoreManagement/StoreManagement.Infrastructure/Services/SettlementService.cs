using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.HR;
using StoreManagement.Shared.Entities.Inventory;
using StoreManagement.Shared.Entities.Sales;
using StoreManagement.Shared.Entities.Finance;
using StoreManagement.Shared.Entities.Identity;
using StoreManagement.Shared.Entities.Partners;
using StoreManagement.Shared.Entities.Configuration;
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

        var company = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == _currentUser.CompanyId);
        var merchantName = company?.Name ?? "DafterPro";

        var total = await baseQuery.CountAsync();
        var items = await baseQuery
            .OrderByDescending(s => s.Date)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new SettlementReadDto
            {
                Id = s.Id,
                SourceType = s.SourceType.ToString(),
                Type = s.Type.ToString(),
                Amount = s.Amount,
                Date = s.Date,
                Notes = s.Notes,
                UserName = s.User.UserName ?? "Unknown",
                MerchantName = merchantName
            })
            .ToListAsync();

        // ملاءمة أسماء الكيانات المرتبطة (العملاء أو الموردين)
        // ملاحظة: لإبقاء الكود بسيطاً وسريعاً، يمكننا جلب الأسماء في كود الـ C# أو استخدام Join
        // هنا سنقوم بجلبها بشكل منفصل لتجنب تعقيد الـ Query
        foreach (var item in items)
        {
            var settlement = await _context.AccountSettlements.FindAsync(item.Id);
            if (settlement != null)
            {
                if (settlement.SourceType == SettlementSource.Customer)
                {
                    var customer = await _context.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == settlement.RelatedEntityId);
                    item.RelatedEntityName = customer?.Name;
                }
                else
                {
                    var supplier = await _context.Suppliers.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == settlement.RelatedEntityId);
                    item.RelatedEntityName = supplier?.Name;
                }
            }
        }

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

            // تحديث الأرصدة
            if (settlement.SourceType == SettlementSource.Customer)
            {
                var customer = await _context.Customers.FindAsync(dto.RelatedEntityId)
                    ?? throw new KeyNotFoundException($"العميل رقم {dto.RelatedEntityId} غير موجود");
                
                // إضافة رصيد (Add): العميل له فلوس أكثر (أو عليه أقل) -> زيادة CashBalance
                // خصم رصيد (Subtract): العميل عليه فلوس أكثر -> نقص CashBalance
                if (settlement.Type == SettlementType.Add) customer.CashBalance += settlement.Amount;
                else customer.CashBalance -= settlement.Amount;
            }
            else
            {
                var supplier = await _context.Suppliers.FindAsync(dto.RelatedEntityId)
                    ?? throw new KeyNotFoundException($"المورد رقم {dto.RelatedEntityId} غير موجود");

                if (settlement.Type == SettlementType.Add) supplier.CashBalance += settlement.Amount;
                else supplier.CashBalance -= settlement.Amount;
            }

            _context.AccountSettlements.Add(settlement);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var user = await _context.Users.FindAsync(_currentUser.UserId);

            var companyEntity = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == settlement.CompanyId);

            return new SettlementReadDto
            {
                Id = settlement.Id,
                SourceType = settlement.SourceType.ToString(),
                Type = settlement.Type.ToString(),
                Amount = settlement.Amount,
                Date = settlement.Date,
                Notes = settlement.Notes,
                UserName = user?.UserName ?? "Unknown",
                MerchantName = companyEntity?.Name ?? "DafterPro"
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
