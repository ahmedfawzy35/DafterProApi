using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
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

public class LoanService : ILoanService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public LoanService(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<LoanReadDto> CreateLoanAsync(CreateLoanDto dto)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId)
            ?? throw new KeyNotFoundException("الموظف غير موجود");

        var loan = new EmployeeLoan
            {
                EmployeeId = dto.EmployeeId,
                TotalAmount = dto.TotalAmount,
                InstallmentAmount = dto.InstallmentAmount,
                NumberOfMonths = dto.NumberOfMonths,
                StartDate = dto.StartDate,
                Status = LoanStatus.Active,
                Notes = dto.Notes,
                CompanyId = (int)_currentUser.CompanyId!
            };

        // توليد الأقساط تلقائياً
        var currentDate = dto.StartDate;
        for (int i = 0; i < dto.NumberOfMonths; i++)
        {
            loan.Installments.Add(new LoanInstallment
            {
                Month = currentDate.Month,
                Year = currentDate.Year,
                Amount = dto.InstallmentAmount,
                IsPaid = false,
                CompanyId = (int)_currentUser.CompanyId!
            });
            currentDate = currentDate.AddMonths(1);
        }

        _context.EmployeeLoans.Add(loan);
        await _context.SaveChangesAsync();

        return new LoanReadDto
        {
            Id = loan.Id,
            EmployeeId = loan.EmployeeId,
            EmployeeName = employee.Name,
            TotalAmount = loan.TotalAmount,
            RemainingAmount = loan.TotalAmount,
            Status = loan.Status.ToString()
        };
    }

    public async Task<List<LoanReadDto>> GetEmployeeLoansAsync(int employeeId)
    {
        return await _context.EmployeeLoans
            .Where(l => l.EmployeeId == employeeId)
            .Select(l => new LoanReadDto
            {
                Id = l.Id,
                EmployeeId = l.EmployeeId,
                EmployeeName = l.Employee.Name,
                TotalAmount = l.TotalAmount,
                RemainingAmount = l.Installments.Where(i => !i.IsPaid).Sum(i => i.Amount),
                Status = l.Status.ToString()
            }).ToListAsync();
    }

    public async Task ProcessLoanDeductionAsync(int payrollRunId, int month, int year)
    {
        var payrollRun = await _context.PayrollRuns
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.Id == payrollRunId)
            ?? throw new KeyNotFoundException("سجل الراتب غير موجود");

        // البحث عن الأقساط المستحقة لهذا الموظف في هذا الشهر
        var installments = await _context.LoanInstallments
            .Include(i => i.Loan)
            .Where(i => i.Loan.EmployeeId == payrollRun.EmployeeId && 
                        i.Month == month && i.Year == year && !i.IsPaid)
            .ToListAsync();

        decimal totalDeducted = 0;
        foreach (var inst in installments)
        {
            inst.IsPaid = true;
            inst.PaidAt = DateTime.UtcNow;
            totalDeducted += inst.Amount;

            // إضافة بند في تفاصيل الراتب
            _context.PayrollRunItems.Add(new PayrollRunItem
            {
                PayrollRunId = payrollRunId,
                Label = $"قسط قرض ({inst.Loan.Notes ?? "قرض موظف"})",
                Amount = inst.Amount,
                Type = AdjustmentType.Deduction,
                Category = "Loans",
                CompanyId = (int)_currentUser.CompanyId!
            });

            // تحديث حالة القرض إذا انتهت الأقساط
            var remaining = await _context.LoanInstallments
                .AnyAsync(i => i.LoanId == inst.LoanId && !i.IsPaid);
            if (!remaining)
            {
                inst.Loan.Status = LoanStatus.Paid;
            }
        }

        if (totalDeducted > 0)
        {
            payrollRun.LoanDeductions += totalDeducted;
            payrollRun.NetSalary -= totalDeducted;
            await _context.SaveChangesAsync();
        }
    }
}
