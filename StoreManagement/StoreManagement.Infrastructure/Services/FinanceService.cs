using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.Finance;
using StoreManagement.Shared.Entities.Partners;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;
using StoreManagement.Shared.Entities.Sales;

namespace StoreManagement.Infrastructure.Services;

public class FinanceService : IFinanceService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IShiftService _shiftService;
    private readonly IAccountingPeriodService _accountingPeriodService;

    public FinanceService(StoreDbContext context, ICurrentUserService currentUser, IShiftService shiftService, IAccountingPeriodService accountingPeriodService)
    {
        _context = context;
        _currentUser = currentUser;
        _shiftService = shiftService;
        _accountingPeriodService = accountingPeriodService;
    }

    private void ValidateBranch(int branchId)
    {
        if (branchId == 0)
            throw new InvalidOperationException("رقم الفرع غير صالح (BranchId = 0)، هذا غير مسموح.");
    }

    // ==========================================
    // Customer Receipts
    // ==========================================

    public async Task<ReceiptReadDto> CreateCustomerReceiptAsync(CreateReceiptDto dto, int? explicitBranchId = null)
    {
        var branchId = explicitBranchId ?? _currentUser.BranchId ?? 0;
        ValidateBranch(branchId);

        if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
        {
            var existing = await _context.CustomerReceipts.FirstOrDefaultAsync(r => r.IdempotencyKey == dto.IdempotencyKey);
            if (existing != null) throw new InvalidOperationException($"هذه العملية تم تنفيذها بالفعل.");
        }

        await _accountingPeriodService.EnsureDateIsOpenAsync(_currentUser.CompanyId!.Value, dto.Date);

        var transaction = _context.Database.CurrentTransaction;
        var isLocal = transaction == null;
        if (isLocal) transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var receipt = new CustomerReceipt
            {
                CustomerId = dto.PartnerId,
                Amount = dto.Amount,
                UnallocatedAmount = dto.Amount,
                Date = dto.Date,
                Method = dto.Method,
                Notes = dto.Notes,
                BranchId = branchId,
                CompanyId = _currentUser.CompanyId!.Value,
                IdempotencyKey = dto.IdempotencyKey
            };
            
            _context.CustomerReceipts.Add(receipt);

            var shiftId = await _shiftService.GetCurrentShiftIdAsync();

            // Record in CashTransaction
            var cashTran = new CashTransaction
            {
                CompanyId = receipt.CompanyId,
                Value = dto.Amount,
                Date = dto.Date,
                Type = TransactionType.In,
                SourceType = TransactionSource.Customer,
                RelatedEntityId = dto.PartnerId,
                ShiftId = shiftId,
                Notes = $"سند قبض بعميل رقم {dto.PartnerId} - الملاحظات: {dto.Notes}",
                UserId = _currentUser.UserId!.Value,
                IdempotencyKey = dto.IdempotencyKey != null ? $"{dto.IdempotencyKey}_cash" : null
            };
            _context.CashTransactions.Add(cashTran);

            await _context.SaveChangesAsync();

            if (dto.AutoAllocate)
            {
                await AutoAllocateCustomerInternalAsync(receipt);
                await _context.SaveChangesAsync();
            }

            if (isLocal) await transaction.CommitAsync();

            return MapCustomerReceiptToDto(receipt);
        }
        catch
        {
            if (isLocal) await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            if (isLocal) await transaction.DisposeAsync();
        }
    }

    /// <summary>
    /// تُنشئ قيد إرجاع في حساب العميل.
    /// createCashTransaction = true  → مرتجع نقدي: يُنشئ CashTransaction OUT (نقدية تخرج من الخزنة).
    /// createCashTransaction = false → إشعار دائن: قيد محاسبي فقط بدون أي حركة نقدية.
    /// القاعدة: Amount دائمًا موجب — Kind هو الذي يحدد الأثر المحاسبي.
    /// </summary>
    public async Task<ReceiptReadDto> CreateCustomerReturnSettlementAsync(
        CreateReceiptDto dto,
        int? explicitBranchId = null,
        bool createCashTransaction = true,
        int? returnInvoiceId = null)
    {
        if (dto.Amount <= 0)
            throw new ArgumentException("مبلغ الإرجاع يجب أن يكون موجبًا.");

        var branchId = explicitBranchId ?? _currentUser.BranchId ?? 0;
        ValidateBranch(branchId);

        if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
        {
            var existing = await _context.CustomerReceipts.FirstOrDefaultAsync(r => r.IdempotencyKey == dto.IdempotencyKey);
            if (existing != null) throw new InvalidOperationException($"هذه العملية تم تنفيذها بالفعل.");
        }

        await _accountingPeriodService.EnsureDateIsOpenAsync(_currentUser.CompanyId!.Value, dto.Date);

        var transaction = _context.Database.CurrentTransaction;
        var isLocal = transaction == null;
        if (isLocal) transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Amount سالب لأنه يُخفّض رصيد العميل (يُخفّض دينه).
            // الإشارة السالبة للمبلغ تعني تخفيض الرصيد في Partner Ledger.
            // Kind يُحدد ما إذا كانت هناك حركة نقدية أم لا:
            //   Refund = نقدية خرجت فعلًا من الخزنة
            //   CreditNote = تخفيض محاسبي بدون حركة نقدية
            var kind = createCashTransaction ? TransactionKind.Refund : TransactionKind.CreditNote;
            var descriptionPrefix = createCashTransaction ? "مرتجع نقدي" : "إشعار دائن";

            var receipt = new CustomerReceipt
            {
                CustomerId = dto.PartnerId,
                Amount = dto.Amount,          // Value is ALWAYS positive now
                UnallocatedAmount = dto.Amount,
                Date = dto.Date,
                Method = dto.Method,
                Kind = kind,
                Notes = dto.Notes,
                BranchId = branchId,
                CompanyId = _currentUser.CompanyId!.Value,
                IdempotencyKey = dto.IdempotencyKey,
                // === Source Tracking: تتبع مصدر القيد للربط بفاتورة المرتجع ===
                FinancialSourceType = FinancialSourceType.Return,
                FinancialSourceId = returnInvoiceId
            };

            _context.CustomerReceipts.Add(receipt);

            if (createCashTransaction)
            {
                var shiftId = await _shiftService.GetCurrentShiftIdAsync();
                // مرتجع نقدي للعميل = خروج نقدية من الخزنة (OUT)
                var cashTran = new CashTransaction
                {
                    CompanyId = receipt.CompanyId,
                    BranchId = branchId,
                    Value = dto.Amount,  // موجب دائمًا — TransactionType يحدد الاتجاه
                    Date = dto.Date,
                    Type = TransactionType.Out,
                    SourceType = TransactionSource.Customer,
                    RelatedEntityId = dto.PartnerId,
                    ShiftId = shiftId,
                    Notes = $"{descriptionPrefix} لعميل رقم {dto.PartnerId} - مرتجع فاتورة #{returnInvoiceId} - {dto.Notes}",
                    UserId = _currentUser.UserId!.Value,
                    IdempotencyKey = dto.IdempotencyKey != null ? $"{dto.IdempotencyKey}_cash" : null,
                    FinancialSourceType = FinancialSourceType.Return,
                    FinancialSourceId = returnInvoiceId
                };
                _context.CashTransactions.Add(cashTran);
            }

            await _context.SaveChangesAsync();
            if (isLocal) await transaction.CommitAsync();

            return MapCustomerReceiptToDto(receipt);
        }
        catch
        {
            if (isLocal) await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            if (isLocal) await transaction.DisposeAsync();
        }
    }

    // Backward-compat shim — delegates to the new unified method
    [Obsolete("Use CreateCustomerReturnSettlementAsync instead.")]
    public Task<ReceiptReadDto> CreateCustomerRefundAsync(CreateReceiptDto dto, int? explicitBranchId = null)
        => CreateCustomerReturnSettlementAsync(dto, explicitBranchId, createCashTransaction: true);

    public async Task AllocateCustomerReceiptAsync(AllocateReceiptDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var receipt = await _context.CustomerReceipts
                .Include(r => r.Allocations)
                .FirstOrDefaultAsync(r => r.Id == dto.ReceiptId)
                ?? throw new KeyNotFoundException("سند القبض غير موجود");

            await _accountingPeriodService.EnsureDateIsOpenAsync(_currentUser.CompanyId!.Value, receipt.Date);

            decimal totalRequired = dto.Allocations.Sum(a => a.Amount);
            if (totalRequired > receipt.UnallocatedAmount)
                throw new InvalidOperationException("المبلغ المراد تخصيصه يتجاوز الرصيد المتبقي في السند.");

            foreach (var alloc in dto.Allocations)
            {
                var invoice = await _context.Invoices.FindAsync(alloc.InvoiceId);
                
                if (invoice == null || invoice.Status != InvoiceStatus.Confirmed)
                    throw new InvalidOperationException($"الفاتورة {alloc.InvoiceId} غير موجودة أو ليست مؤكدة.");

                if (invoice.Type != InvoiceType.Sale)
                    throw new InvalidOperationException("لا يمكن تخصيص سند عميل لفاتورة ليست فاتورة بيع.");

                if (invoice.CustomerId != receipt.CustomerId)
                    throw new InvalidOperationException($"الفاتورة {invoice.Id} لا تخص نفس العميل.");

                if (alloc.Amount > invoice.RemainingAmount)
                    throw new InvalidOperationException($"المبلغ المخصص أكبر من المتبقي للفاتورة {invoice.Id}. المتبقي هو: {invoice.RemainingAmount}");

                receipt.Allocations.Add(new CustomerReceiptAllocation
                {
                    InvoiceId = invoice.Id,
                    Amount = alloc.Amount
                });

                receipt.UnallocatedAmount -= alloc.Amount;
                invoice.AllocatedAmount += alloc.Amount;

                invoice.PaymentStatus = invoice.RemainingAmount <= 0 ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid;
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

    private async Task AutoAllocateCustomerInternalAsync(CustomerReceipt receipt)
    {
        var unpaidInvoices = await _context.Invoices
            .Where(i => i.CustomerId == receipt.CustomerId && i.Status == InvoiceStatus.Confirmed && i.Type == InvoiceType.Sale && i.PaymentStatus != PaymentStatus.Paid)
            .OrderBy(i => i.Date) // الاقدم اولا
            .ToListAsync();

        foreach (var invoice in unpaidInvoices)
        {
            if (receipt.UnallocatedAmount <= 0) break;

            decimal allocationAmount = Math.Min(receipt.UnallocatedAmount, invoice.RemainingAmount);

            receipt.Allocations.Add(new CustomerReceiptAllocation
            {
                InvoiceId = invoice.Id,
                Amount = allocationAmount
            });

            receipt.UnallocatedAmount -= allocationAmount;
            invoice.AllocatedAmount += allocationAmount;

            invoice.PaymentStatus = invoice.RemainingAmount <= 0 ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid;
        }
    }

    public async Task AllocateDirectToInvoiceAsync(int receiptId, int invoiceId, decimal amount)
    {
        var tx = _context.Database.CurrentTransaction;
        var isLocal = tx == null;
        if (isLocal) tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var receipt = await _context.CustomerReceipts
                .Include(r => r.Allocations)
                .FirstOrDefaultAsync(r => r.Id == receiptId)
                ?? throw new KeyNotFoundException("سند القبض غير موجود");

            await _accountingPeriodService.EnsureDateIsOpenAsync(_currentUser.CompanyId!.Value, receipt.Date);

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == invoiceId)
                ?? throw new KeyNotFoundException($"الفاتورة {invoiceId} غير موجودة");

            if (invoice.Status != InvoiceStatus.Confirmed)
                throw new InvalidOperationException("لا يمكن تخصيص لفاتورة غير مؤكدة.");
            if (amount > invoice.RemainingAmount)
                throw new InvalidOperationException($"المبلغ ({amount}) أكبر من المتبقي ({invoice.RemainingAmount}).");
            if (amount > receipt.UnallocatedAmount)
                throw new InvalidOperationException("المبلغ يتجاوز غير المخصص في السند.");
            if (invoice.CustomerId != receipt.CustomerId)
                throw new InvalidOperationException("الفاتورة لا تخص نفس العميل.");

            receipt.Allocations.Add(new CustomerReceiptAllocation { InvoiceId = invoiceId, Amount = amount });
            receipt.UnallocatedAmount -= amount;
            invoice.AllocatedAmount += amount;
            invoice.PaymentStatus = invoice.RemainingAmount <= 0 ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid;

            await _context.SaveChangesAsync();
            if (isLocal) await tx.CommitAsync();
        }
        catch
        {
            if (isLocal) await tx.RollbackAsync();
            throw;
        }
        finally
        {
            if (isLocal) await tx.DisposeAsync();
        }
    }

    public async Task VoidCustomerReceiptAsync(int receiptId)
    {
        var transaction = _context.Database.CurrentTransaction;
        var isLocal = transaction == null;
        if (isLocal) transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var receipt = await _context.CustomerReceipts
                .Include(r => r.Allocations)
                .FirstOrDefaultAsync(r => r.Id == receiptId && r.CompanyId == _currentUser.CompanyId)
                ?? throw new KeyNotFoundException("سند القبض غير موجود");

            if (receipt.FinancialStatus != FinancialStatus.Active)
                throw new InvalidOperationException("السند ملغي أو معطل مسبقاً.");

            // Guard against closed periods using the receipt's original date
            await _accountingPeriodService.EnsureDateIsOpenAsync(_currentUser.CompanyId!.Value, receipt.Date);

            // Void the receipt
            receipt.FinancialStatus = FinancialStatus.Voided;
            receipt.CancelledAt = DateTime.UtcNow;
            receipt.CancelledByUserId = _currentUser.UserId;
            receipt.UnallocatedAmount = receipt.Amount; // Reset

            // Reverse any allocations to invoices
            if (receipt.Allocations != null && receipt.Allocations.Any())
            {
                foreach (var alloc in receipt.Allocations.ToList())
                {
                    var invoice = await _context.Invoices.FindAsync(alloc.InvoiceId);
                    if (invoice != null)
                    {
                        invoice.AllocatedAmount -= alloc.Amount;
                        if (invoice.AllocatedAmount < 0) invoice.AllocatedAmount = 0;
                        invoice.PaymentStatus = invoice.AllocatedAmount == 0 ? PaymentStatus.Unpaid : PaymentStatus.PartiallyPaid;
                    }
                    _context.CustomerReceiptAllocations.Remove(alloc);
                }
            }

            // If this receipt was generated from a Return, void its associated CashTransaction
            if (receipt.FinancialSourceType == FinancialSourceType.Return && receipt.FinancialSourceId.HasValue)
            {
                var cashTx = await _context.CashTransactions.FirstOrDefaultAsync(c =>
                    c.CompanyId == receipt.CompanyId &&
                    c.FinancialSourceType == FinancialSourceType.Return &&
                    c.FinancialSourceId == receipt.FinancialSourceId.Value &&
                    c.FinancialStatus == FinancialStatus.Active);

                if (cashTx != null)
                {
                    cashTx.FinancialStatus = FinancialStatus.Voided;
                    cashTx.CancelledAt = DateTime.UtcNow;
                    cashTx.CancelledByUserId = _currentUser.UserId;
                }
            }

            await _context.SaveChangesAsync();
            if (isLocal) await transaction.CommitAsync();
        }
        catch
        {
            if (isLocal) await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            if (isLocal) await transaction.DisposeAsync();
        }
    }

    // ==========================================
    // Supplier Payments
    // ==========================================

    public async Task<ReceiptReadDto> CreateSupplierPaymentAsync(CreateReceiptDto dto, int? explicitBranchId = null)
    {
        var branchId = explicitBranchId ?? _currentUser.BranchId ?? 0;
        ValidateBranch(branchId);

        if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
        {
            var existing = await _context.SupplierPayments.FirstOrDefaultAsync(p => p.IdempotencyKey == dto.IdempotencyKey);
            if (existing != null) throw new InvalidOperationException($"هذه العملية تم تنفيذها بالفعل.");
        }

        await _accountingPeriodService.EnsureDateIsOpenAsync(_currentUser.CompanyId!.Value, dto.Date);

        var transaction = _context.Database.CurrentTransaction;
        var isLocal = transaction == null;
        if (isLocal) transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var payment = new SupplierPayment
            {
                SupplierId = dto.PartnerId,
                Amount = dto.Amount,
                UnallocatedAmount = dto.Amount,
                Date = dto.Date,
                Method = dto.Method,
                Notes = dto.Notes,
                BranchId = branchId,
                CompanyId = _currentUser.CompanyId!.Value,
                IdempotencyKey = dto.IdempotencyKey
            };
            
            _context.SupplierPayments.Add(payment);

            var shiftId = await _shiftService.GetCurrentShiftIdAsync();

            // Record in CashTransaction
            var cashTran = new CashTransaction
            {
                CompanyId = payment.CompanyId,
                Value = dto.Amount,
                Date = dto.Date,
                Type = TransactionType.Out,
                SourceType = TransactionSource.Supplier,
                RelatedEntityId = dto.PartnerId,
                ShiftId = shiftId,
                Notes = $"سند صرف لمورد رقم {dto.PartnerId} - الملاحظات: {dto.Notes}",
                UserId = _currentUser.UserId!.Value,
                IdempotencyKey = dto.IdempotencyKey != null ? $"{dto.IdempotencyKey}_cash" : null
            };
            _context.CashTransactions.Add(cashTran);

            await _context.SaveChangesAsync();

            if (dto.AutoAllocate)
            {
                await AutoAllocateSupplierInternalAsync(payment);
                await _context.SaveChangesAsync();
            }

            if (isLocal) await transaction.CommitAsync();

            return MapSupplierPaymentToDto(payment);
        }
        catch
        {
            if (isLocal) await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            if (isLocal) await transaction.DisposeAsync();
        }
    }

    /// <summary>
    /// تُنشئ قيد إرجاع في حساب المورد.
    /// createCashTransaction = true  → مرتجع نقدي: يُنشئ CashTransaction IN (نقدية تدخل من المورد).
    /// createCashTransaction = false → إشعار دائن: قيد محاسبي فقط بدون أي حركة نقدية.
    /// القاعدة: Amount دائمًا موجب — Kind هو الذي يحدد الأثر المحاسبي.
    /// </summary>
    public async Task<ReceiptReadDto> CreateSupplierReturnSettlementAsync(
        CreateReceiptDto dto,
        int? explicitBranchId = null,
        bool createCashTransaction = true,
        int? returnInvoiceId = null)
    {
        if (dto.Amount <= 0)
            throw new ArgumentException("مبلغ الإرجاع يجب أن يكون موجبًا.");

        var branchId = explicitBranchId ?? _currentUser.BranchId ?? 0;
        ValidateBranch(branchId);

        if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
        {
            var existing = await _context.SupplierPayments.FirstOrDefaultAsync(p => p.IdempotencyKey == dto.IdempotencyKey);
            if (existing != null) throw new InvalidOperationException($"هذه العملية تم تنفيذها بالفعل.");
        }

        await _accountingPeriodService.EnsureDateIsOpenAsync(_currentUser.CompanyId!.Value, dto.Date);

        var transaction = _context.Database.CurrentTransaction;
        var isLocal = transaction == null;
        if (isLocal) transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Amount سالب لأنه يُخفّض رصيد المورد (يُخفّض ما ندين به له).
            var kind = createCashTransaction ? TransactionKind.Refund : TransactionKind.CreditNote;
            var descriptionPrefix = createCashTransaction ? "مرتجع نقدي" : "إشعار دائن";

            var payment = new SupplierPayment
            {
                SupplierId = dto.PartnerId,
                Amount = dto.Amount,         // Value is ALWAYS positive now
                UnallocatedAmount = dto.Amount,
                Date = dto.Date,
                Method = dto.Method,
                Kind = kind,
                Notes = dto.Notes,
                BranchId = branchId,
                CompanyId = _currentUser.CompanyId!.Value,
                IdempotencyKey = dto.IdempotencyKey,
                // === Source Tracking ===
                FinancialSourceType = FinancialSourceType.Return,
                FinancialSourceId = returnInvoiceId
            };

            _context.SupplierPayments.Add(payment);

            if (createCashTransaction)
            {
                var shiftId = await _shiftService.GetCurrentShiftIdAsync();
                // مرتجع نقدي من المورد = دخول نقدية للخزنة (IN)
                var cashTran = new CashTransaction
                {
                    CompanyId = payment.CompanyId,
                    BranchId = branchId,
                    Value = dto.Amount,  // موجب دائمًا — TransactionType يحدد الاتجاه
                    Date = dto.Date,
                    Type = TransactionType.In,
                    SourceType = TransactionSource.Supplier,
                    RelatedEntityId = dto.PartnerId,
                    ShiftId = shiftId,
                    Notes = $"{descriptionPrefix} من مورد رقم {dto.PartnerId} - مرتجع فاتورة #{returnInvoiceId} - {dto.Notes}",
                    UserId = _currentUser.UserId!.Value,
                    IdempotencyKey = dto.IdempotencyKey != null ? $"{dto.IdempotencyKey}_cash" : null,
                    FinancialSourceType = FinancialSourceType.Return,
                    FinancialSourceId = returnInvoiceId
                };
                _context.CashTransactions.Add(cashTran);
            }

            await _context.SaveChangesAsync();
            if (isLocal) await transaction.CommitAsync();

            return MapSupplierPaymentToDto(payment);
        }
        catch
        {
            if (isLocal) await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            if (isLocal) await transaction.DisposeAsync();
        }
    }

    // Backward-compat shim — delegates to the new unified method
    [Obsolete("Use CreateSupplierReturnSettlementAsync instead.")]
    public Task<ReceiptReadDto> CreateSupplierRefundAsync(CreateReceiptDto dto, int? explicitBranchId = null)
        => CreateSupplierReturnSettlementAsync(dto, explicitBranchId, createCashTransaction: true);

    public async Task AllocateSupplierPaymentAsync(AllocateReceiptDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var payment = await _context.SupplierPayments
                .Include(p => p.Allocations)
                .FirstOrDefaultAsync(p => p.Id == dto.ReceiptId)
                ?? throw new KeyNotFoundException("سند الصرف غير موجود");

            await _accountingPeriodService.EnsureDateIsOpenAsync(_currentUser.CompanyId!.Value, payment.Date);

            decimal totalRequired = dto.Allocations.Sum(a => a.Amount);
            if (totalRequired > payment.UnallocatedAmount)
                throw new InvalidOperationException("المبلغ المراد تخصيصه يتجاوز الرصيد المتبقي في السند.");

            foreach (var alloc in dto.Allocations)
            {
                var invoice = await _context.Invoices.FindAsync(alloc.InvoiceId);
                
                if (invoice == null || invoice.Status != InvoiceStatus.Confirmed)
                    throw new InvalidOperationException($"الفاتورة {alloc.InvoiceId} غير موجودة أو ليست مؤكدة.");

                if (invoice.Type != InvoiceType.Purchase)
                    throw new InvalidOperationException("لا يمكن تخصيص سند للمورد لفاتورة ليست فاتورة مشتريات.");

                if (invoice.SupplierId != payment.SupplierId)
                    throw new InvalidOperationException($"الفاتورة {invoice.Id} لا تخص نفس المورد.");

                if (alloc.Amount > invoice.RemainingAmount)
                    throw new InvalidOperationException($"المبلغ المخصص أكبر من المتبقي للفاتورة {invoice.Id}.");

                payment.Allocations.Add(new SupplierPaymentAllocation
                {
                    InvoiceId = invoice.Id,
                    Amount = alloc.Amount
                });

                payment.UnallocatedAmount -= alloc.Amount;
                invoice.AllocatedAmount += alloc.Amount;

                invoice.PaymentStatus = invoice.RemainingAmount <= 0 ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid;
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

    private async Task AutoAllocateSupplierInternalAsync(SupplierPayment payment)
    {
        var unpaidInvoices = await _context.Invoices
            .Where(i => i.SupplierId == payment.SupplierId && i.Status == InvoiceStatus.Confirmed && i.Type == InvoiceType.Purchase && i.PaymentStatus != PaymentStatus.Paid)
            .OrderBy(i => i.Date) // الاقدم اولا
            .ToListAsync();

        foreach (var invoice in unpaidInvoices)
        {
            if (payment.UnallocatedAmount <= 0) break;

            decimal allocationAmount = Math.Min(payment.UnallocatedAmount, invoice.RemainingAmount);

            payment.Allocations.Add(new SupplierPaymentAllocation
            {
                InvoiceId = invoice.Id,
                Amount = allocationAmount
            });

            payment.UnallocatedAmount -= allocationAmount;
            invoice.AllocatedAmount += allocationAmount;

            invoice.PaymentStatus = invoice.RemainingAmount <= 0 ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid;
        }
    }

    public async Task AllocateDirectToSupplierInvoiceAsync(int paymentId, int invoiceId, decimal amount)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var payment = await _context.SupplierPayments
                .Include(p => p.Allocations)
                .FirstOrDefaultAsync(p => p.Id == paymentId)
                ?? throw new KeyNotFoundException("سند الصرف غير موجود");

            await _accountingPeriodService.EnsureDateIsOpenAsync(_currentUser.CompanyId!.Value, payment.Date);

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == invoiceId)
                ?? throw new KeyNotFoundException($"الفاتورة {invoiceId} غير موجودة");

            if (invoice.Status != InvoiceStatus.Confirmed)
                throw new InvalidOperationException("لا يمكن تخصيص لفاتورة غير مؤكدة.");
            if (amount > invoice.RemainingAmount)
                throw new InvalidOperationException($"المبلغ ({amount}) أكبر من المتبقي ({invoice.RemainingAmount}).");
            if (amount > payment.UnallocatedAmount)
                throw new InvalidOperationException("المبلغ يتجاوز غير المخصص في السند.");
            if (invoice.SupplierId != payment.SupplierId)
                throw new InvalidOperationException("الفاتورة لا تخص نفس المورد.");

            payment.Allocations.Add(new SupplierPaymentAllocation { InvoiceId = invoiceId, Amount = amount });
            payment.UnallocatedAmount -= amount;
            invoice.AllocatedAmount += amount;
            invoice.PaymentStatus = invoice.RemainingAmount <= 0 ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task VoidSupplierPaymentAsync(int paymentId)
    {
        var transaction = _context.Database.CurrentTransaction;
        var isLocal = transaction == null;
        if (isLocal) transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var payment = await _context.SupplierPayments
                .Include(p => p.Allocations)
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.CompanyId == _currentUser.CompanyId)
                ?? throw new KeyNotFoundException("سند الصرف غير موجود");

            if (payment.FinancialStatus != FinancialStatus.Active)
                throw new InvalidOperationException("السند ملغي أو معطل مسبقاً.");

            await _accountingPeriodService.EnsureDateIsOpenAsync(_currentUser.CompanyId!.Value, payment.Date);

            payment.FinancialStatus = FinancialStatus.Voided;
            payment.CancelledAt = DateTime.UtcNow;
            payment.CancelledByUserId = _currentUser.UserId;
            payment.UnallocatedAmount = payment.Amount;

            if (payment.Allocations != null && payment.Allocations.Any())
            {
                foreach (var alloc in payment.Allocations.ToList())
                {
                    var invoice = await _context.Invoices.FindAsync(alloc.InvoiceId);
                    if (invoice != null)
                    {
                        invoice.AllocatedAmount -= alloc.Amount;
                        if (invoice.AllocatedAmount < 0) invoice.AllocatedAmount = 0;
                        invoice.PaymentStatus = invoice.AllocatedAmount == 0 ? PaymentStatus.Unpaid : PaymentStatus.PartiallyPaid;
                    }
                    _context.SupplierPaymentAllocations.Remove(alloc);
                }
            }

            if (payment.FinancialSourceType == FinancialSourceType.Return && payment.FinancialSourceId.HasValue)
            {
                var cashTx = await _context.CashTransactions.FirstOrDefaultAsync(c =>
                    c.CompanyId == payment.CompanyId &&
                    c.FinancialSourceType == FinancialSourceType.Return &&
                    c.FinancialSourceId == payment.FinancialSourceId.Value &&
                    c.FinancialStatus == FinancialStatus.Active);

                if (cashTx != null)
                {
                    cashTx.FinancialStatus = FinancialStatus.Voided;
                    cashTx.CancelledAt = DateTime.UtcNow;
                    cashTx.CancelledByUserId = _currentUser.UserId;
                }
            }

            await _context.SaveChangesAsync();
            if (isLocal) await transaction.CommitAsync();
        }
        catch
        {
            if (isLocal) await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            if (isLocal) await transaction.DisposeAsync();
        }
    }

    // ==========================================
    // Queries / Endpoints
    // ==========================================

    public async Task<StatementPagedResult<CustomerStatementDto>> GetCustomerStatementAsync(
        int customerId, StatementQueryDto query)
    {
        // ===== 1. ضبط الفترة الزمنية =====
        // الافتراضي: آخر 90 يوماً إذا لم تُحدَّد فترة
        var from = query.From ?? DateTime.UtcNow.AddDays(-90);
        var to = query.To ?? DateTime.UtcNow;

        // تصحيح: نضمن أن to يشمل نهاية اليوم
        to = to.Date.AddDays(1).AddTicks(-1);

        // ===== 2. رصيد ما قبل الفترة (Opening Balance للكشف) =====
        // نحسب ما كان عليه الحساب قبل بداية الفترة
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        var entityOpeningBalance = customer?.OpeningBalance ?? 0m;

        // ما تراكم قبل from
        var invoicesBeforeFrom = await _context.Invoices
            .Where(i => i.CustomerId == customerId
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Date < from
                     && (i.Type == InvoiceType.Sale || i.Type == InvoiceType.SalesReturn))
            .SumAsync(i => i.Type == InvoiceType.Sale ? (decimal?)(i.TotalValue - i.Discount + i.Tax) : -(decimal?)(i.TotalValue - i.Discount + i.Tax)) ?? 0m;

        var receiptsBeforeFrom = await _context.CustomerReceipts
            .Where(r => r.CustomerId == customerId && r.Date < from && r.FinancialStatus == FinancialStatus.Active)
            .SumAsync(r => r.Kind == TransactionKind.Normal ? (decimal?)r.Amount :
                           r.Kind == TransactionKind.Refund ? -(decimal?)r.Amount : 0m) ?? 0m;

        var settlementsBeforeFrom = await _context.AccountSettlements
            .Where(s => s.SourceType == SettlementSource.Customer && s.RelatedEntityId == customerId && s.Date < from)
            .ToListAsync();
        
        var settlementEffectBeforeFrom = settlementsBeforeFrom.Sum(s => SettlementAccountingHelper.MapCustomerSettlement(s).BalanceEffect);

        // رصيد بداية الكشف = رصيد الافتتاح + حركات ما قبل الفترة
        var openingBalance = entityOpeningBalance + invoicesBeforeFrom - receiptsBeforeFrom + settlementEffectBeforeFrom;

        // ===== 3. جلب حركات الفترة =====
        var invoices = await _context.Invoices
            .Where(i => i.CustomerId == customerId
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Date >= from && i.Date <= to
                     && (i.Type == InvoiceType.Sale || i.Type == InvoiceType.SalesReturn))
            .ToListAsync();

        var receipts = await _context.CustomerReceipts
            .Where(r => r.CustomerId == customerId && r.Date >= from && r.Date <= to && r.FinancialStatus == FinancialStatus.Active)
            .ToListAsync();

        var settlements = await _context.AccountSettlements
            .Where(s => s.SourceType == SettlementSource.Customer && s.RelatedEntityId == customerId && s.Date >= from && s.Date <= to)
            .ToListAsync();

        // ===== 4. دمج وترتيب زمني =====
        // استخدام Date ثم Id لمنع الخلط بين الحركات في نفس اليوم
        var allEvents = invoices
            .Select(i => new { i.Date, Id = i.Id, EventType = "Invoice", Data = (object)i })
            .Concat(receipts.Select(r => new { r.Date, Id = r.Id, EventType = "Receipt", Data = (object)r }))
            .Concat(settlements.Select(s => new { s.Date, Id = s.Id, EventType = "Settlement", Data = (object)s }))
            .OrderBy(e => e.Date).ThenBy(e => e.Id)
            .ToList();

        // ===== 5. بناء سطور الكشف مع الرصيد المتراكم =====
        var allLines = new List<CustomerStatementDto>();
        var runningBalance = openingBalance;
        var totalDebit = 0m;
        var totalCredit = 0m;

        foreach (var ev in allEvents)
        {
            var stmt = new CustomerStatementDto { Date = ev.Date };

            if (ev.EventType == "Invoice" && ev.Data is Invoice inv)
            {
                if (inv.Type == InvoiceType.Sale)
                {
                    stmt.DocumentType = "Sale Invoice";
                    stmt.TransactionType = "Invoice";
                    stmt.DocumentId = inv.Id;
                    stmt.Description = $"فاتورة مبيعات #{inv.Id}";
                    stmt.Debit = inv.NetTotal;
                    runningBalance += inv.NetTotal;
                    totalDebit += inv.NetTotal;
                }
                else if (inv.Type == InvoiceType.SalesReturn)
                {
                    stmt.DocumentType = "Sales Return";
                    stmt.TransactionType = "Invoice";
                    stmt.DocumentId = inv.Id;
                    stmt.Description = $"مرتجع مبيعات #{inv.Id}";
                    stmt.Credit = inv.NetTotal;
                    runningBalance -= inv.NetTotal;
                    totalCredit += inv.NetTotal;
                }
            }
            else if (ev.EventType == "Receipt" && ev.Data is CustomerReceipt rec)
            {
                if (rec.Kind == TransactionKind.Refund)
                {
                    // مرتجع نقدي: نقدية خرجت فعلًا من الخزنة → يزيد الرصيد (Debit)
                    stmt.DocumentType = "Refund";
                    stmt.TransactionType = "Refund";
                    stmt.DocumentId = rec.Id;
                    stmt.Description = $"مرتجع نقدي للعميل#{rec.FinancialSourceId} - {rec.Method}";
                    stmt.Debit = Math.Abs(rec.Amount);
                    runningBalance += Math.Abs(rec.Amount);
                    totalDebit += Math.Abs(rec.Amount);
                }
                else if (rec.Kind == TransactionKind.CreditNote)
                {
                    // إشعار دائن: تخفيض محاسبي بدون نقدية → يخفض الرصيد (Credit)
                    stmt.DocumentType = "CreditNote";
                    stmt.TransactionType = "CreditNote";
                    stmt.DocumentId = rec.Id;
                    stmt.Description = $"إشعار دائن - مرتجع #{rec.FinancialSourceId}";
                    stmt.Credit = Math.Abs(rec.Amount);
                    runningBalance -= Math.Abs(rec.Amount);
                    totalCredit += Math.Abs(rec.Amount);
                }
                else
                {
                    // سند قبض عادي: Amount موجب (نقدية دخلت) → يخفض الرصيد (Credit)
                    stmt.DocumentType = "Receipt";
                    stmt.TransactionType = rec.Kind.ToString();
                    stmt.DocumentId = rec.Id;
                    stmt.Description = $"سند قبض - {rec.Method}";
                    stmt.Credit = rec.Amount;
                    runningBalance -= rec.Amount;
                    totalCredit += rec.Amount;
                }
            }
            else if (ev.EventType == "Settlement" && ev.Data is AccountSettlement setl)
            {
                var mapping = SettlementAccountingHelper.MapCustomerSettlement(setl);
                stmt.DocumentType = "Settlement";
                stmt.TransactionType = setl.Type.ToString();
                stmt.DocumentId = setl.Id;
                stmt.Description = mapping.Description;
                stmt.Debit = mapping.DisplayDebit;
                stmt.Credit = mapping.DisplayCredit;
                runningBalance += mapping.BalanceEffect;
                totalDebit += mapping.DisplayDebit;
                totalCredit += mapping.DisplayCredit;
            }

            stmt.Balance = runningBalance;
            allLines.Add(stmt);
        }

        // ===== 6. تطبيق Pagination =====
        var totalCount = allLines.Count;
        var pageSize = Math.Clamp(query.PageSize, 1, 500); // الحد الأقصى 500 سطر
        var pageNumber = Math.Max(1, query.PageNumber);

        var pagedItems = allLines
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new StatementPagedResult<CustomerStatementDto>
        {
            Items = pagedItems,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            OpeningBalance = openingBalance,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            ClosingBalance = runningBalance
        };
    }

    public async Task<List<InvoiceReadDto>> GetOpenCustomerInvoicesAsync(int customerId)
    {
        var invoices = await _context.Invoices
            .Where(i => i.CustomerId == customerId 
                     && i.Status == InvoiceStatus.Confirmed 
                     && i.PaymentStatus != PaymentStatus.Paid
                     && i.Type == InvoiceType.Sale)
            .OrderBy(i => i.Date)
            .ToListAsync();

        return invoices.Select(i => new InvoiceReadDto
        {
            Id = i.Id,
            InvoiceType = i.Type.ToString(),
            Date = i.Date,
            TotalValue = i.TotalValue,
            Discount = i.Discount,
            Paid = i.Paid,
            AllocatedAmount = i.AllocatedAmount,
            Status = i.Status.ToString(),
            PaymentStatus = i.PaymentStatus.ToString()
        }).ToList();
    }

    public async Task<List<ReceiptReadDto>> GetUnallocatedCustomerReceiptsAsync(int customerId)
    {
        var receipts = await _context.CustomerReceipts
            .Include(r => r.Allocations)
            .Where(r => r.CustomerId == customerId && r.UnallocatedAmount > 0)
            .OrderBy(r => r.Date)
            .ToListAsync();

        return receipts.Select(MapCustomerReceiptToDto).ToList();
    }

    // ==========================================
    // Customer Balance Calculation
    // ==========================================

    /// <summary>
    /// حساب الرصيد الحالي للعميل من مصدر الحقيقة الفعلي (Receipts/Invoices)
    /// الرصيد = OpeningBalance + صافي الفواتير - المقبوضات
    /// قيمة موجبة = العميل مدين | قيمة سالبة = العميل دائن (دفع أكثر)
    /// </summary>
    public async Task<decimal> GetCustomerCurrentBalanceAsync(int customerId)
    {
        // جلب رصيد الافتتاح من الكيان
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer is null) return 0m;

        // إجمالي فواتير البيع المؤكدة
        var invoicedTotal = await _context.Invoices
            .Where(i => i.CustomerId == customerId
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Type == InvoiceType.Sale)
            .SumAsync(i => (decimal?)(i.TotalValue - i.Discount + i.Tax)) ?? 0m;

        // إجمالي مرتجعات البيع
        var returnTotal = await _context.Invoices
            .Where(i => i.CustomerId == customerId
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Type == InvoiceType.SalesReturn)
            .SumAsync(i => (decimal?)(i.TotalValue - i.Discount + i.Tax)) ?? 0m;

        // إجمالي المقبوضات العادية (تُخفّض الدين)
        var normalReceipts = await _context.CustomerReceipts
            .Where(r => r.CustomerId == customerId && r.Kind == TransactionKind.Normal && r.FinancialStatus == FinancialStatus.Active)
            .SumAsync(r => (decimal?)r.Amount) ?? 0m;

        // إجمالي المرتجعات النقدية (تزيد الدين لأن العميل أخذ نقدية فتُلغي أثر مرتجع المبيعات)
        var refundReceipts = await _context.CustomerReceipts
            .Where(r => r.CustomerId == customerId && r.Kind == TransactionKind.Refund && r.FinancialStatus == FinancialStatus.Active)
            .SumAsync(r => (decimal?)r.Amount) ?? 0m;

        // CreditNote لا تُجمع هنا لأن InvoiceType.SalesReturn (returnTotal) تتكفل بتخفيض الرصيد.        // إجمالي التسويات (أثرها على الرصيد)
        var settlements = await _context.AccountSettlements
            .Where(s => s.SourceType == SettlementSource.Customer && s.RelatedEntityId == customerId)
            .ToListAsync();

        var settlementEffect = settlements.Sum(s => SettlementAccountingHelper.MapCustomerSettlement(s).BalanceEffect);

        return customer.OpeningBalance + (invoicedTotal - returnTotal) - normalReceipts + refundReceipts + settlementEffect;
    }

    // ==========================================
    // Supplier Statement & Queries
    // ==========================================

    /// <summary>
    /// كشف حساب المورد خلال فترة محددة
    /// يدمج فواتير الشراء والمرتجعات والمدفوعات في سطر واحد متسلسل مع الرصيد المتراكم
    /// </summary>
    public async Task<StatementPagedResult<SupplierStatementDto>> GetSupplierStatementAsync(
        int supplierId, StatementQueryDto query)
    {
        // ===== 1. ضبط الفترة الزمنية =====
        var from = query.From ?? DateTime.UtcNow.AddDays(-90);
        var to = (query.To ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);

        // ===== 2. رصيد ما قبل الفترة (Opening Balance للكشف) =====
        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId);
        var entityOpeningBalance = supplier?.OpeningBalance ?? 0m;

        // فواتير ومرتجعات قبل الفترة
        var purchasesBeforeFrom = await _context.Invoices
            .Where(i => i.SupplierId == supplierId
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Date < from
                     && (i.Type == InvoiceType.Purchase || i.Type == InvoiceType.PurchaseReturn))
            .SumAsync(i => i.Type == InvoiceType.Purchase ? (decimal?)(i.TotalValue - i.Discount + i.Tax) : -(decimal?)(i.TotalValue - i.Discount + i.Tax)) ?? 0m;

        var paymentsBeforeFrom = await _context.SupplierPayments
            .Where(p => p.SupplierId == supplierId && p.Date < from && p.FinancialStatus == FinancialStatus.Active)
            .SumAsync(p => p.Kind == TransactionKind.Normal ? (decimal?)p.Amount :
                           p.Kind == TransactionKind.Refund ? -(decimal?)p.Amount : 0m) ?? 0m;

        var settlementsBeforeFrom = await _context.AccountSettlements
            .Where(s => s.SourceType == SettlementSource.Supplier && s.RelatedEntityId == supplierId && s.Date < from)
            .ToListAsync();
            
        var settlementEffectBeforeFrom = settlementsBeforeFrom.Sum(s => SettlementAccountingHelper.MapSupplierSettlement(s).BalanceEffect);

        var openingBalance = entityOpeningBalance + purchasesBeforeFrom - paymentsBeforeFrom + settlementEffectBeforeFrom;

        // ===== 3. جلب حركات الفترة =====
        var invoices = await _context.Invoices
            .Where(i => i.SupplierId == supplierId
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Date >= from && i.Date <= to
                     && (i.Type == InvoiceType.Purchase || i.Type == InvoiceType.PurchaseReturn))
            .ToListAsync();

        var payments = await _context.SupplierPayments
            .Where(p => p.SupplierId == supplierId && p.Date >= from && p.Date <= to && p.FinancialStatus == FinancialStatus.Active)
            .ToListAsync();

        var settlements = await _context.AccountSettlements
            .Where(s => s.SourceType == SettlementSource.Supplier && s.RelatedEntityId == supplierId && s.Date >= from && s.Date <= to)
            .ToListAsync();

        // ===== 4. دمج وترتيب زمني =====
        var allEvents = invoices
            .Select(i => new { i.Date, Id = i.Id, EventType = "Invoice", Data = (object)i })
            .Concat(payments.Select(p => new { p.Date, Id = p.Id, EventType = "Payment", Data = (object)p }))
            .Concat(settlements.Select(s => new { s.Date, Id = s.Id, EventType = "Settlement", Data = (object)s }))
            .OrderBy(e => e.Date).ThenBy(e => e.Id)
            .ToList();

        // ===== 5. بناء سطور الكشف =====
        var allLines = new List<SupplierStatementDto>();
        var runningBalance = openingBalance;
        var totalDebit = 0m;
        var totalCredit = 0m;

        foreach (var ev in allEvents)
        {
            var stmt = new SupplierStatementDto { Date = ev.Date };

            if (ev.EventType == "Invoice" && ev.Data is Invoice inv)
            {
                if (inv.Type == InvoiceType.Purchase)
                {
                    stmt.DocumentType = "Purchase Invoice";
                    stmt.TransactionType = "Invoice";
                    stmt.DocumentId = inv.Id;
                    stmt.Description = $"فاتورة مشتريات #{inv.Id}";
                    stmt.Debit = inv.NetTotal;
                    runningBalance += inv.NetTotal;
                    totalDebit += inv.NetTotal;
                }
                else if (inv.Type == InvoiceType.PurchaseReturn)
                {
                    stmt.DocumentType = "Purchase Return";
                    stmt.TransactionType = "Invoice";
                    stmt.DocumentId = inv.Id;
                    stmt.Description = $"مرتجع مشتريات #{inv.Id}";
                    stmt.Credit = inv.NetTotal;
                    runningBalance -= inv.NetTotal;
                    totalCredit += inv.NetTotal;
                }
            }
            else if (ev.EventType == "Payment" && ev.Data is SupplierPayment pmt)
            {
                if (pmt.Kind == TransactionKind.Refund)
                {
                    // مرتجع نقدي من المورد: نقدية دخلت للخزنة → يزيد الرصيد (Debit) للمورد مؤقتًا
                    stmt.DocumentType = "Refund";
                    stmt.TransactionType = "Refund";
                    stmt.DocumentId = pmt.Id;
                    stmt.Description = $"مرتجع نقدي من المورد#{pmt.FinancialSourceId} - {pmt.Method}";
                    stmt.Debit = Math.Abs(pmt.Amount);
                    runningBalance += Math.Abs(pmt.Amount);
                    totalDebit += Math.Abs(pmt.Amount);
                }
                else if (pmt.Kind == TransactionKind.CreditNote)
                {
                    // إشعار دائن: تخفيض محاسبي لرصيد المورد بدون نقدية
                    stmt.DocumentType = "CreditNote";
                    stmt.TransactionType = "CreditNote";
                    stmt.DocumentId = pmt.Id;
                    stmt.Description = $"إشعار دائن - مرتجع مشتريات #{pmt.FinancialSourceId}";
                    stmt.Debit = Math.Abs(pmt.Amount);
                    runningBalance += Math.Abs(pmt.Amount);
                    totalDebit += Math.Abs(pmt.Amount);
                }
                else
                {
                    // سند صرف عادي: Amount موجب → يخفض رصيد المورد (Credit)
                    stmt.DocumentType = "Payment";
                    stmt.TransactionType = pmt.Kind.ToString();
                    stmt.DocumentId = pmt.Id;
                    stmt.Description = $"سند صرف - {pmt.Method}";
                    stmt.Credit = pmt.Amount;
                    runningBalance -= pmt.Amount;
                    totalCredit += pmt.Amount;
                }
            }
            else if (ev.EventType == "Settlement" && ev.Data is AccountSettlement setl)
            {
                var mapping = SettlementAccountingHelper.MapSupplierSettlement(setl);
                stmt.DocumentType = "Settlement";
                stmt.TransactionType = setl.Type.ToString();
                stmt.DocumentId = setl.Id;
                stmt.Description = mapping.Description;
                stmt.Debit = mapping.DisplayDebit;
                stmt.Credit = mapping.DisplayCredit;
                runningBalance += mapping.BalanceEffect;
                totalDebit += mapping.DisplayDebit;
                totalCredit += mapping.DisplayCredit;
            }

            stmt.Balance = runningBalance;
            allLines.Add(stmt);
        }

        // ===== 6. Pagination =====
        var totalCount = allLines.Count;
        var pageSize = Math.Clamp(query.PageSize, 1, 500);
        var pageNumber = Math.Max(1, query.PageNumber);

        return new StatementPagedResult<SupplierStatementDto>
        {
            Items = allLines.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            OpeningBalance = openingBalance,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            ClosingBalance = runningBalance
        };
    }

    /// <summary>
    /// فواتير المورد المفتوحة (التي لم تُدفع بالكامل)
    /// </summary>
    public async Task<List<InvoiceReadDto>> GetOpenSupplierInvoicesAsync(int supplierId)
    {
        var invoices = await _context.Invoices
            .Where(i => i.SupplierId == supplierId
                     && i.Status == InvoiceStatus.Confirmed
                     && i.PaymentStatus != PaymentStatus.Paid
                     && i.Type == InvoiceType.Purchase)
            .OrderBy(i => i.Date)
            .ToListAsync();

        return invoices.Select(i => new InvoiceReadDto
        {
            Id = i.Id,
            InvoiceType = i.Type.ToString(),
            Date = i.Date,
            TotalValue = i.TotalValue,
            Discount = i.Discount,
            Paid = i.Paid,
            AllocatedAmount = i.AllocatedAmount,
            Status = i.Status.ToString(),
            PaymentStatus = i.PaymentStatus.ToString()
        }).ToList();
    }

    /// <summary>
    /// مدفوعات المورد التي لم تُخصص على فواتير بعد
    /// </summary>
    public async Task<List<ReceiptReadDto>> GetUnallocatedSupplierPaymentsAsync(int supplierId)
    {
        var payments = await _context.SupplierPayments
            .Include(p => p.Allocations)
            .Where(p => p.SupplierId == supplierId && p.UnallocatedAmount > 0)
            .OrderBy(p => p.Date)
            .ToListAsync();

        return payments.Select(MapSupplierPaymentToDto).ToList();
    }

    /// <summary>
    /// حساب الرصيد الحالي للمورد من مصدر الحقيقة الفعلي
    /// الرصيد = OpeningBalance + صافي الفواتير - المدفوعات
    /// قيمة موجبة = مدينون للمورد | قيمة سالبة = المورد دائن (دفعنا أكثر)
    /// </summary>
    public async Task<decimal> GetSupplierCurrentBalanceAsync(int supplierId)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == supplierId);

        if (supplier is null) return 0m;

        // إجمالي فواتير الشراء
        var purchaseTotal = await _context.Invoices
            .Where(i => i.SupplierId == supplierId
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Type == InvoiceType.Purchase)
            .SumAsync(i => (decimal?)(i.TotalValue - i.Discount + i.Tax)) ?? 0m;

        // إجمالي مرتجعات الشراء
        var returnTotal = await _context.Invoices
            .Where(i => i.SupplierId == supplierId
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Type == InvoiceType.PurchaseReturn)
            .SumAsync(i => (decimal?)(i.TotalValue - i.Discount + i.Tax)) ?? 0m;

        // إجمالي المدفوعات للمورد العادية (أعطيناه نقدية، يقل دينه لنا)
        var normalPayments = await _context.SupplierPayments
            .Where(p => p.SupplierId == supplierId && p.Kind == TransactionKind.Normal && p.FinancialStatus == FinancialStatus.Active)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        // إجمالي المرتجعات النقدية للمورد (أخذنا نقدية منه، فيعود الرصيد كما كان قبل المرتجع)
        var refundPayments = await _context.SupplierPayments
            .Where(p => p.SupplierId == supplierId && p.Kind == TransactionKind.Refund && p.FinancialStatus == FinancialStatus.Active)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        // إجمالي التسويات (أثرها على الرصيد)
        var settlements = await _context.AccountSettlements
            .Where(s => s.SourceType == SettlementSource.Supplier && s.RelatedEntityId == supplierId)
            .ToListAsync();

        var settlementEffect = settlements.Sum(s => SettlementAccountingHelper.MapSupplierSettlement(s).BalanceEffect);

        return supplier.OpeningBalance + (purchaseTotal - returnTotal) - normalPayments + refundPayments + settlementEffect;
    }

    // ==========================================
    // Helpers
    // ==========================================

    private ReceiptReadDto MapCustomerReceiptToDto(CustomerReceipt receipt)
    {
        return new ReceiptReadDto
        {
            Id = receipt.Id,
            PartnerId = receipt.CustomerId,
            Date = receipt.Date,
            Amount = receipt.Amount,
            UnallocatedAmount = receipt.UnallocatedAmount,
            Method = receipt.Method.ToString(),
            Notes = receipt.Notes,
            Allocations = receipt.Allocations?.Select(a => new AllocationReadDto
            {
                InvoiceId = a.InvoiceId,
                Amount = a.Amount
            }).ToList() ?? new List<AllocationReadDto>()
        };
    }

    private ReceiptReadDto MapSupplierPaymentToDto(SupplierPayment payment)
    {
        return new ReceiptReadDto
        {
            Id = payment.Id,
            PartnerId = payment.SupplierId,
            Date = payment.Date,
            Amount = payment.Amount,
            UnallocatedAmount = payment.UnallocatedAmount,
            Method = payment.Method.ToString(),
            Notes = payment.Notes,
            Allocations = payment.Allocations?.Select(a => new AllocationReadDto
            {
                InvoiceId = a.InvoiceId,
                Amount = a.Amount
            }).ToList() ?? new List<AllocationReadDto>()
        };
    }
}
