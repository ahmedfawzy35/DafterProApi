using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

public class PayrollService : IPayrollService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public PayrollService(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<PayrollReadDto>> GetAllAsync(DateTime month)
    {
        return await _context.Payrolls
            .Include(p => p.Employee)
            .Where(p => p.Month == month.Month && p.Year == month.Year && p.Employee.CompanyId == (int)_currentUser.CompanyId!)
            .Select(p => new PayrollReadDto
            {
                Id = p.Id,
                EmployeeId = p.EmployeeId,
                EmployeeName = p.Employee.Name,
                Month = new DateTime(p.Year, p.Month, 1),
                Salary = p.Salary,
                Allowances = p.Bonuses,
                Deductions = p.Deductions,
                IsPaid = p.PaidDate != null,
                PaymentDate = p.PaidDate
            })
            .ToListAsync();
    }

    public async Task GeneratePayrollAsync(CreatePayrollDto dto)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId && e.CompanyId == (int)_currentUser.CompanyId!)
            ?? throw new KeyNotFoundException($"الموظف رقم {dto.EmployeeId} غير موجود");

        var existing = await _context.Payrolls
            .AnyAsync(p => p.EmployeeId == dto.EmployeeId && p.Month == dto.Month.Month && p.Year == dto.Month.Year);

        if (existing) throw new InvalidOperationException("تم إنشاء مسير رواتب لهذا الموظف بالفعل لهذا الشهر");

        var payroll = new Payroll
        {
            EmployeeId = dto.EmployeeId,
            Month = dto.Month.Month,
            Year = dto.Month.Year,
            Salary = employee.Salary,
            Bonuses = employee.Allowances,
            Deductions = employee.Deductions,
            PaidDate = null
        };

        _context.Payrolls.Add(payroll);
        await _context.SaveChangesAsync();
    }

    public async Task PaySalaryAsync(int payrollId)
    {
        var payroll = await _context.Payrolls.Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.Id == payrollId && p.Employee.CompanyId == (int)_currentUser.CompanyId!)
            ?? throw new KeyNotFoundException($"مسير الرواتب رقم {payrollId} غير موجود");

        if (payroll.PaidDate != null) throw new InvalidOperationException("هذا الراتب مدفوع بالفعل");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            payroll.PaidDate = DateTime.UtcNow;

            var cashTransaction = new CashTransaction
            {
                Type = TransactionType.Out,
                SourceType = TransactionSource.Salary,
                Value = payroll.Salary + payroll.Bonuses - payroll.Deductions,
                Date = DateTime.UtcNow,
                Notes = $"صرف راتب شهر {payroll.Month}/{payroll.Year} للموظف {payroll.Employee.Name}",
                CompanyId = (int)_currentUser.CompanyId!,
                UserId = (int)_currentUser.UserId!,
                RelatedEntityId = payroll.EmployeeId
            };

            _context.CashTransactions.Add(cashTransaction);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
