using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.Sales.Installments;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

public class InstallmentService : IInstallmentService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IOutboxService _outboxService;
    private readonly IInstallmentPolicyService _installmentPolicy;
    private readonly IFinanceService _financeService;

    public InstallmentService(
        StoreDbContext context,
        ICurrentUserService currentUser,
        IOutboxService outboxService,
        IInstallmentPolicyService installmentPolicy,
        IFinanceService financeService)
    {
        _context = context;
        _currentUser = currentUser;
        _outboxService = outboxService;
        _installmentPolicy = installmentPolicy;
        _financeService = financeService;
    }

    public async Task<InstallmentSchedulePreviewDto> PreviewScheduleAsync(CreateInstallmentPlanDto dto)
    {
        await _installmentPolicy.EnsureInstallmentIsValidAsync(dto.TotalAmount, dto.DownPayment, dto.Months);

        var amountToFinance = dto.TotalAmount - dto.DownPayment;
        var monthlyInstallment = dto.Months > 0 ? Math.Round(amountToFinance / dto.Months, 2) : 0;

        var preview = new InstallmentSchedulePreviewDto
        {
            TotalAmount = dto.TotalAmount,
            DownPayment = dto.DownPayment,
            AmountToFinance = amountToFinance,
            Months = dto.Months,
            MonthlyInstallment = monthlyInstallment
        };

        var currentDueDate = dto.StartDate;
        decimal runningTotal = 0;

        for (int i = 1; i <= dto.Months; i++)
        {
            currentDueDate = currentDueDate.AddMonths(1);
            
            // Adjust last month for rounding differences
            var amount = (i == dto.Months) ? (amountToFinance - runningTotal) : monthlyInstallment;
            runningTotal += amount;

            preview.Items.Add(new InstallmentScheduleItemPreviewDto
            {
                MonthNumber = i,
                DueDate = currentDueDate,
                Amount = amount
            });
        }

        return preview;
    }

    public async Task<InstallmentPlanReadDto> CreatePlanAsync(CreateInstallmentPlanDto dto)
    {
        await _installmentPolicy.EnsureInstallmentIsValidAsync(dto.TotalAmount, dto.DownPayment, dto.Months);

        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == dto.InvoiceId && i.CompanyId == _currentUser.CompanyId)
            ?? throw new KeyNotFoundException("الفاتورة غير موجودة.");

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == dto.CustomerId && c.CompanyId == _currentUser.CompanyId)
            ?? throw new KeyNotFoundException("العميل غير موجود.");

        var amountToFinance = dto.TotalAmount - dto.DownPayment;
        var monthlyInstallment = dto.Months > 0 ? Math.Round(amountToFinance / dto.Months, 2) : 0;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var plan = new InstallmentPlan
            {
                InvoiceId = invoice.Id,
                CustomerId = customer.Id,
                TotalAmount = dto.TotalAmount,
                DownPayment = dto.DownPayment,
                RemainingAmount = amountToFinance,
                StartDate = dto.StartDate,
                EndDate = dto.StartDate.AddMonths(dto.Months),
                Status = InstallmentPlanStatus.Active,
                CompanyId = _currentUser.CompanyId!.Value
            };

            var currentDueDate = dto.StartDate;
            decimal runningTotal = 0;

            for (int i = 1; i <= dto.Months; i++)
            {
                currentDueDate = currentDueDate.AddMonths(1);
                var amount = (i == dto.Months) ? (amountToFinance - runningTotal) : monthlyInstallment;
                runningTotal += amount;

                plan.Schedules.Add(new InstallmentScheduleItem
                {
                    DueDate = currentDueDate,
                    Amount = amount,
                    PaidAmount = 0,
                    PenaltyAmount = 0,
                    Status = InstallmentItemStatus.Pending,
                    CompanyId = _currentUser.CompanyId!.Value
                });
            }

            _context.InstallmentPlans.Add(plan);
            await _context.SaveChangesAsync();

            // DownPayment integration
            if (dto.DownPayment > 0)
            {
                var receiptDto = new CreateReceiptDto
                {
                    PartnerId = customer.Id,
                    Amount = dto.DownPayment,
                    Date = DateTime.UtcNow,
                    Method = PaymentMethod.Cash,
                    Notes = $"الدفعة المقدمة لقسط الفاتورة {invoice.Id}",
                    AutoAllocate = false
                };
                
                var receipt = await _financeService.CreateCustomerReceiptAsync(receiptDto, explicitBranchId: invoice.BranchId);
                await _financeService.AllocateDirectToInvoiceAsync(receipt.Id, invoice.Id, dto.DownPayment);
            }

            await _outboxService.PublishAsync("InstallmentCreated", new
            {
                PlanId = plan.Id,
                InvoiceId = invoice.Id,
                CompanyId = _currentUser.CompanyId,
                Timestamp = DateTime.UtcNow
            });

            await transaction.CommitAsync();

            return await GetPlanByIdAsync(plan.Id) ?? throw new Exception("فشل قراءة الخطة بعد إنشائها.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PagedResult<InstallmentPlanReadDto>> GetAllPlansAsync(PaginationQueryDto query, int? customerId, string? status)
    {
        var dbQuery = _context.InstallmentPlans
            .Include(p => p.Customer)
            .Where(p => p.CompanyId == _currentUser.CompanyId);

        if (customerId.HasValue) dbQuery = dbQuery.Where(p => p.CustomerId == customerId.Value);
        
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InstallmentPlanStatus>(status, true, out var parsedStatus))
        {
            dbQuery = dbQuery.Where(p => p.Status == parsedStatus);
        }

        var total = await dbQuery.CountAsync();
        var items = await dbQuery
            .OrderByDescending(p => p.CreatedDate)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new InstallmentPlanReadDto
            {
                Id = p.Id,
                InvoiceId = p.InvoiceId,
                CustomerName = p.Customer.Name,
                TotalAmount = p.TotalAmount,
                DownPayment = p.DownPayment,
                RemainingAmount = p.RemainingAmount,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status.ToString()
            }).ToListAsync();

        return new PagedResult<InstallmentPlanReadDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task<InstallmentPlanReadDto?> GetPlanByIdAsync(int id)
    {
        var plan = await _context.InstallmentPlans
            .Include(p => p.Customer)
            .Include(p => p.Schedules)
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == _currentUser.CompanyId);

        if (plan == null) return null;

        return new InstallmentPlanReadDto
        {
            Id = plan.Id,
            InvoiceId = plan.InvoiceId,
            CustomerName = plan.Customer.Name,
            TotalAmount = plan.TotalAmount,
            DownPayment = plan.DownPayment,
            RemainingAmount = plan.RemainingAmount,
            StartDate = plan.StartDate,
            EndDate = plan.EndDate,
            Status = plan.Status.ToString(),
            Schedules = plan.Schedules.OrderBy(s => s.DueDate).Select(s => new InstallmentScheduleItemDto
            {
                Id = s.Id,
                DueDate = s.DueDate,
                Amount = s.Amount,
                PaidAmount = s.PaidAmount,
                PenaltyAmount = s.PenaltyAmount,
                Status = s.Status.ToString(),
                SettledDate = s.SettledDate
            }).ToList()
        };
    }

    public async Task<InstallmentPaymentResultDto> PayInstallmentAsync(int scheduleItemId, decimal amount, int? branchId = null)
    {
        if (amount <= 0) throw new InvalidOperationException("المبلغ المدفوع يجب أن يكون أكبر من الصفر.");

        var item = await _context.InstallmentScheduleItems
            .Include(i => i.InstallmentPlan)
            .FirstOrDefaultAsync(i => i.Id == scheduleItemId && i.InstallmentPlan.CompanyId == _currentUser.CompanyId)
            ?? throw new KeyNotFoundException("القسط غير موجود.");

        if (item.Status == InstallmentItemStatus.Paid)
            throw new InvalidOperationException("القسط مدفوع بالكامل بالفعل.");

        var outstandingPenalty = item.PenaltyAmount; // Assuming penalty is separately calculated and frozen or dynamically calculated.
        
        // For dynamic penalty calculation checking:
        var lateDays = (int)(DateTime.UtcNow.Date - item.DueDate.Date).TotalDays;
        if (lateDays > 0 && item.Status != InstallmentItemStatus.Paid)
        {
            var calculatedPenalty = await _installmentPolicy.CalculateLatePenaltyAsync(item.Amount, lateDays);
            if (calculatedPenalty > item.PenaltyAmount)
            {
                item.PenaltyAmount = calculatedPenalty;
            }
        }

        var totalRequired = (item.Amount - item.PaidAmount) + item.PenaltyAmount;
        if (amount > totalRequired)
            throw new InvalidOperationException($"المبلغ المدفوع ({amount}) يتجاوز المطلوب المتبقي للقسط مع الغرامات ({totalRequired}).");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Payment breakdown
            decimal penaltyApplied = 0;
            decimal principalApplied = 0;

            if (amount <= item.PenaltyAmount)
            {
                penaltyApplied = amount;
                item.PenaltyAmount -= amount;
            }
            else
            {
                penaltyApplied = item.PenaltyAmount;
                principalApplied = amount - penaltyApplied;
                item.PenaltyAmount = 0;
                item.PaidAmount += principalApplied;
            }

            if (item.PaidAmount >= item.Amount && item.PenaltyAmount <= 0)
            {
                item.Status = InstallmentItemStatus.Paid;
                item.SettledDate = DateTime.UtcNow;
            }
            else
            {
                item.Status = InstallmentItemStatus.Partial;
            }

            item.InstallmentPlan.RemainingAmount -= principalApplied;

            // Finance integration (CustomerReceipt creation)
            var receiptDto = new CreateReceiptDto
            {
                PartnerId = item.InstallmentPlan.CustomerId,
                Amount = amount,
                Date = DateTime.UtcNow,
                Method = PaymentMethod.Cash,
                Notes = $"تحصيل القسط رقم {item.Id} من خطة التقسيط {item.InstallmentPlanId}",
                AutoAllocate = false
            };
            
            var receipt = await _financeService.CreateCustomerReceiptAsync(receiptDto, explicitBranchId: branchId);

            _context.InstallmentPaymentAllocations.Add(new InstallmentPaymentAllocation
            {
                InstallmentScheduleItemId = item.Id,
                CustomerReceiptId = receipt.Id,
                AmountAllocated = principalApplied,
                PenaltyAllocated = penaltyApplied,
                CompanyId = _currentUser.CompanyId!.Value
            });
            
            await _financeService.AllocateDirectToInvoiceAsync(receipt.Id, item.InstallmentPlan.InvoiceId, principalApplied); // Optional, if invoice needs the allocation.

            await _context.SaveChangesAsync();

            // Check if plan is completed
            if (!item.InstallmentPlan.Schedules.Any(s => s.Status != InstallmentItemStatus.Paid))
            {
                item.InstallmentPlan.Status = InstallmentPlanStatus.Completed;
                await _context.SaveChangesAsync();
            }

            await _outboxService.PublishAsync("PaymentCollected", new
            {
                ScheduleItemId = item.Id,
                PlanId = item.InstallmentPlanId,
                Amount = amount,
                CompanyId = _currentUser.CompanyId,
                Timestamp = DateTime.UtcNow
            });

            await transaction.CommitAsync();

            return new InstallmentPaymentResultDto
            {
                Success = true,
                Message = "تم سداد القسط بنجاح.",
                AppliedAmount = principalApplied,
                AppliedPenalty = penaltyApplied,
                ReceiptId = receipt.Id
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ProcessOverdueInstallmentsAsync(DateTime referenceDate)
    {
        // Not restricted by companyId since this would typically run in a background job across all tenants.
        // It calculates penalties based on policies. This would be elaborated later.
        await Task.CompletedTask;
    }
}
