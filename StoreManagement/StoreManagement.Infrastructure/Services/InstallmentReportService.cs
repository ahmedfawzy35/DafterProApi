using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

public class InstallmentReportService : IInstallmentReportService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public InstallmentReportService(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<InstallmentReportItemDto>> GetOverdueInstallmentsAsync(PaginationQueryDto query, int? customerId)
    {
        var dbQuery = _context.InstallmentScheduleItems
            .Include(i => i.InstallmentPlan)
                .ThenInclude(p => p.Customer)
            .Where(i => i.CompanyId == _currentUser.CompanyId
                     && i.Status != InstallmentItemStatus.Paid
                     && i.DueDate < DateTime.UtcNow.Date);

        if (customerId.HasValue)
        {
            dbQuery = dbQuery.Where(i => i.InstallmentPlan.CustomerId == customerId.Value);
        }

        var total = await dbQuery.CountAsync();

        var items = await dbQuery
            .OrderBy(i => i.DueDate)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(i => new InstallmentReportItemDto
            {
                PlanId = i.InstallmentPlanId,
                ScheduleItemId = i.Id,
                CustomerName = i.InstallmentPlan.Customer.Name,
                CustomerPhone = i.InstallmentPlan.Customer.Phones.FirstOrDefault() != null 
                    ? i.InstallmentPlan.Customer.Phones.First().PhoneNumber 
                    : string.Empty,
                DueDate = i.DueDate,
                DaysOverdue = (int)(DateTime.UtcNow.Date - i.DueDate.Date).TotalDays,
                AmountDue = i.Amount - i.PaidAmount,
                PenaltyAmount = i.PenaltyAmount,
                TotalRequired = (i.Amount - i.PaidAmount) + i.PenaltyAmount
            })
            .ToListAsync();

        return new PagedResult<InstallmentReportItemDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<InstallmentSummaryDto> GetInstallmentSummaryAsync(DateTime? from, DateTime? to)
    {
        var planQuery = _context.InstallmentPlans.Where(p => p.CompanyId == _currentUser.CompanyId);
        var scheduleQuery = _context.InstallmentScheduleItems.Where(i => i.CompanyId == _currentUser.CompanyId);

        if (from.HasValue)
        {
            planQuery = planQuery.Where(p => p.CreatedDate >= from.Value);
            scheduleQuery = scheduleQuery.Where(i => i.CreatedDate >= from.Value);
        }
        if (to.HasValue)
        {
            planQuery = planQuery.Where(p => p.CreatedDate <= to.Value);
            scheduleQuery = scheduleQuery.Where(i => i.CreatedDate <= to.Value);
        }

        var totalActive = await planQuery
            .Where(p => p.Status == InstallmentPlanStatus.Active)
            .SumAsync(p => p.RemainingAmount);

        var totalCollected = await scheduleQuery
            .SumAsync(i => i.PaidAmount + i.PenaltyAmount);

        var overdueQuery = _context.InstallmentScheduleItems
            .Include(i => i.InstallmentPlan)
            .Where(i => i.CompanyId == _currentUser.CompanyId
                     && i.Status != InstallmentItemStatus.Paid
                     && i.DueDate < DateTime.UtcNow.Date);

        if (from.HasValue) overdueQuery = overdueQuery.Where(i => i.DueDate >= from.Value);
        if (to.HasValue) overdueQuery = overdueQuery.Where(i => i.DueDate <= to.Value);

        var totalOverdue = await overdueQuery.SumAsync(i => (i.Amount - i.PaidAmount) + i.PenaltyAmount);
        var overdueCustomersCount = await overdueQuery.Select(i => i.InstallmentPlan.CustomerId).Distinct().CountAsync();

        return new InstallmentSummaryDto
        {
            TotalActivePlansAmount = totalActive,
            TotalCollectedAmount = totalCollected,
            TotalOverdueAmount = totalOverdue,
            OverdueCustomersCount = overdueCustomersCount
        };
    }
}
