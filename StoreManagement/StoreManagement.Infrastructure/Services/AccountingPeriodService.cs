using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Exceptions;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

public class AccountingPeriodService : IAccountingPeriodService
{
    private readonly StoreDbContext _context;

    public AccountingPeriodService(StoreDbContext context)
    {
        _context = context;
    }

    public async Task EnsureDateIsOpenAsync(int companyId, DateTime operationDate)
    {
        // نستخرج تاريخ اليوم فقط للمقارنة (تجاهل الوقت)
        var dateOnly = operationDate.Date;

        // نبحث عن أي فترة محاسبية مُغلقة تشمل هذا التاريخ للشركة
        var closedPeriod = await _context.AccountingPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(ap => ap.CompanyId == companyId 
                                       && ap.IsClosed 
                                       && ap.StartDate.Date <= dateOnly 
                                       && ap.EndDate.Date >= dateOnly);

        if (closedPeriod != null)
        {
            throw new ClosedAccountingPeriodException(operationDate);
        }
    }
}
