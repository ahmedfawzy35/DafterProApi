using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Infrastructure.Services.PayrollStrategies;
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

public class PayrollService : IPayrollService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IEnumerable<ISalaryCalculator> _calculators;
    private readonly ILoanService _loanService;
    private readonly IPolicyService _policyService;

    public PayrollService(
        StoreDbContext context, 
        ICurrentUserService currentUser, 
        IEnumerable<ISalaryCalculator> calculators,
        ILoanService loanService,
        IPolicyService policyService)
    {
        _context = context;
        _currentUser = currentUser;
        _calculators = calculators;
        _loanService = loanService;
        _policyService = policyService;
    }

    public async Task<List<PayrollRunReadDto>> GetPayrollRunsAsync(int month, int year)
    {
        var companyId = (int)_currentUser.CompanyId!;
        return await _context.PayrollRuns
            .Include(p => p.Employee)
            .Where(p => p.Month == month && p.Year == year && p.CompanyId == companyId)
            .OrderBy(p => p.Employee.Name)
            .Select(p => new PayrollRunReadDto
            {
                Id = p.Id,
                EmployeeId = p.EmployeeId,
                EmployeeName = p.Employee.Name,
                Month = p.Month,
                Year = p.Year,
                BasicSalary = p.BasicSalary,
                NetSalary = p.NetSalary,
                IsLocked = p.IsLocked,
                GeneratedAt = p.CreatedDate
            })
            .ToListAsync();
    }

    public async Task GeneratePayrollRunAsync(int month, int year, List<int>? employeeIds = null)
    {
        var companyId = (int)_currentUser.CompanyId!;
        
        // 1. جلب الموظفين المطلوبين (أو الكل)
        var employeesQuery = _context.Employees.Where(e => e.IsEnabled);
        if (employeeIds != null && employeeIds.Any())
        {
            employeesQuery = employeesQuery.Where(e => employeeIds.Contains(e.Id));
        }
        
        var employees = await employeesQuery.ToListAsync();

        foreach (var employee in employees)
        {
            // التحقق من وجود مسير حالي غير مقفل لحذفه وإعادة إنشائه (أو منع التعديل إذا كان مقفلاً)
            var existing = await _context.PayrollRuns
                .FirstOrDefaultAsync(p => p.EmployeeId == employee.Id && p.Month == month && p.Year == year);

            if (existing != null)
            {
                if (existing.IsLocked) continue; // تخطي المقفل
                
                // حذف التفاصيل القديمة
                var oldItems = _context.PayrollRunItems.Where(i => i.PayrollRunId == existing.Id);
                _context.PayrollRunItems.RemoveRange(oldItems);
                _context.PayrollRuns.Remove(existing);
            }

            // 2. اختيار الاستراتيجية المناسبة لحساب الراتب الأساسي
            var calculator = _calculators.FirstOrDefault(c => c.SupportedType == employee.Type)
                ?? _calculators.First(c => c.SupportedType == EmployeeType.Monthly); // الافتراضي شهري

            decimal baseSalary = await calculator.CalculateBaseSalaryAsync(employee, month, year);

            // 3. إنشاء كائن SNAPSHOT
            var payrollRun = new PayrollRun
            {
                EmployeeId = employee.Id,
                Month = month,
                Year = year,
                BasicSalary = baseSalary,
                TotalAllowances = 0,
                TotalDeductions = 0,
                LoanDeductions = 0,
                NetSalary = baseSalary,
                IsLocked = false,
                CompanyId = companyId
            };

            _context.PayrollRuns.Add(payrollRun);
            await _context.SaveChangesAsync(); // لحفظ المعرف واستخدامه في البنود

            // 4. إضافة بند الراتب الأساسي
            _context.PayrollRunItems.Add(new PayrollRunItem
            {
                PayrollRunId = payrollRun.Id,
                Label = "الراتب الأساسي (محتسب)",
                Amount = baseSalary,
                Type = AdjustmentType.Addition,
                Category = "BaseSalary",
                CompanyId = companyId
            });

            // 5. معالجة الإضافات والخصومات لمرة واحدة (SalaryAdjustments)
            var adjustments = await _context.SalaryAdjustments
                .Where(a => a.EmployeeId == employee.Id && a.Month == month && a.Year == year)
                .ToListAsync();

            foreach (var adj in adjustments)
            {
                _context.PayrollRunItems.Add(new PayrollRunItem
                {
                    PayrollRunId = payrollRun.Id,
                    Label = adj.Notes ?? "تعديل راتب",
                    Amount = adj.Amount,
                    Type = adj.Type,
                    Category = "MonthlyAdjustment",
                    CompanyId = companyId
                });

                if (adj.Type == AdjustmentType.Addition)
                {
                    payrollRun.TotalAllowances += adj.Amount;
                    payrollRun.NetSalary += adj.Amount;
                }
                else
                {
                    payrollRun.TotalDeductions += adj.Amount;
                    payrollRun.NetSalary -= adj.Amount;
                }
            }

            // 6. معالجة الإضافات والخصومات المتكررة (RecurringAdjustments)
            var date = new DateTime(year, month, 1);
            var recurring = await _context.RecurringAdjustments
                .Where(r => r.EmployeeId == employee.Id && r.IsActive &&
                            r.EffectiveFrom <= date && (r.EffectiveTo == null || r.EffectiveTo >= date))
                .ToListAsync();

            foreach (var rec in recurring)
            {
                _context.PayrollRunItems.Add(new PayrollRunItem
                {
                    PayrollRunId = payrollRun.Id,
                    Label = rec.Notes ?? "تسوية متكررة",
                    Amount = rec.Amount,
                    Type = rec.Mode,
                    Category = "Recurring",
                    CompanyId = companyId
                });

                if (rec.Mode == AdjustmentType.Addition)
                {
                    payrollRun.TotalAllowances += rec.Amount;
                    payrollRun.NetSalary += rec.Amount;
                }
                else
                {
                    payrollRun.TotalDeductions += rec.Amount;
                    payrollRun.NetSalary -= rec.Amount;
                }
            }

            // 7. معالجة القروض والأقساط
            await _loanService.ProcessLoanDeductionAsync(payrollRun.Id, month, year);

            await _context.SaveChangesAsync();
        }
    }

    public async Task LockAndPayPayrollAsync(int month, int year)
    {
        var companyId = (int)_currentUser.CompanyId!;
        var userId = (int)_currentUser.UserId!;

        var runs = await _context.PayrollRuns
            .Include(p => p.Employee)
            .Where(p => p.Month == month && p.Year == year && !p.IsLocked && p.CompanyId == companyId)
            .ToListAsync();

        if (!runs.Any()) return;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var run in runs)
            {
                run.IsLocked = true;

                // ترحيل للمعاملات النقدية
                var cashTx = new CashTransaction
                {
                    Type = TransactionType.Out,
                    SourceType = TransactionSource.Salary,
                    Value = run.NetSalary,
                    Date = DateTime.UtcNow,
                    Notes = $"صرف راتب {month}/{year} للموظف {run.Employee.Name} (رقم التشغيل: {run.Id})",
                    CompanyId = companyId,
                    UserId = userId,
                    RelatedEntityId = run.EmployeeId
                };
                _context.CashTransactions.Add(cashTx);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PayrollRunDetailsDto> GetPayrollDetailsAsync(int payrollRunId)
    {
        var companyId = (int)_currentUser.CompanyId!;
        
        var run = await _context.PayrollRuns
            .Include(p => p.Employee)
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == payrollRunId && p.CompanyId == companyId)
            ?? throw new KeyNotFoundException("سجل الراتب غير موجود");

        return new PayrollRunDetailsDto
        {
            Id = run.Id,
            EmployeeId = run.EmployeeId,
            EmployeeName = run.Employee.Name,
            Month = run.Month,
            Year = run.Year,
            BasicSalary = run.BasicSalary,
            TotalAllowances = run.TotalAllowances,
            TotalDeductions = run.TotalDeductions,
            LoanDeductions = run.LoanDeductions,
            NetSalary = run.NetSalary,
            IsLocked = run.IsLocked,
            GeneratedAt = run.CreatedDate,
            Items = run.Items.Select(i => new PayrollRunItemReadDto
            {
                Label = i.Label,
                Amount = i.Amount,
                Type = i.Type.ToString(),
                Category = i.Category
            }).ToList()
        };
    }
}
