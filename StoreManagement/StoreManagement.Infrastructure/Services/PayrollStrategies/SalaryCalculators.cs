using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
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

namespace StoreManagement.Infrastructure.Services.PayrollStrategies;

public interface ISalaryCalculator
{
    EmployeeType SupportedType { get; }
    Task<decimal> CalculateBaseSalaryAsync(Employee employee, int month, int year);
}

public class MonthlySalaryCalculator : ISalaryCalculator
{
    private readonly IEmployeeStatusResolver _statusResolver;
    private readonly IPolicyService _policyService;

    public MonthlySalaryCalculator(IEmployeeStatusResolver statusResolver, IPolicyService policyService)
    {
        _statusResolver = statusResolver;
        _policyService = policyService;
    }

    public EmployeeType SupportedType => EmployeeType.Monthly;

    public async Task<decimal> CalculateBaseSalaryAsync(Employee employee, int month, int year)
    {
        // الحصول على حالة الموظف اليومية لهذا الشهر
        var statuses = await _statusResolver.GetMonthlyStatusAsync(employee.Id, month, year);
        int totalDays = statuses.Count;
        int activeDays = statuses.Count(s => s.IsActive);

        if (activeDays == 0) return 0;

        // إذا كان الموظف فعالاً طوال الشهر، يحصل على الراتب كاملاً
        if (activeDays == totalDays) return employee.Salary;

        // التناسب (Proration) في حالة التعيين أو الإنهاء خلال الشهر
        return (employee.Salary / totalDays) * activeDays;
    }
}

public class DailySalaryCalculator : ISalaryCalculator
{
    private readonly StoreDbContext _context;

    public DailySalaryCalculator(StoreDbContext context)
    {
        _context = context;
    }

    public EmployeeType SupportedType => EmployeeType.Daily;

    public async Task<decimal> CalculateBaseSalaryAsync(Employee employee, int month, int year)
    {
        // حساب الأيام التي حضر فيها الموظف فعلياً
        var presentDays = await _context.Attendances
            .CountAsync(a => a.EmployeeId == employee.Id && 
                             a.Date.Month == month && a.Date.Year == year && 
                             a.Status == AttendanceStatus.Present);

        return employee.Salary * presentDays; // الراتب هنا يعتبر "أجر اليوم"
    }
}
