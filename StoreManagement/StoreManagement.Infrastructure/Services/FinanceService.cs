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

    public FinanceService(StoreDbContext context, ICurrentUserService currentUser, IShiftService shiftService)
    {
        _context = context;
        _currentUser = currentUser;
        _shiftService = shiftService;
    }

    private void ValidateBranch(int branchId)
    {
        if (branchId == 0)
            throw new InvalidOperationException("رقم الفرع غير صالح (BranchId = 0)، هذا غير مسموح.");
    }

    // ==========================================
    // Customer Receipts
    // ==========================================

    public async Task<ReceiptReadDto> CreateCustomerReceiptAsync(CreateReceiptDto dto)
    {
        var branchId = _currentUser.BranchId ?? 0;
        ValidateBranch(branchId);

        await using var transaction = await _context.Database.BeginTransactionAsync();
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
                CompanyId = _currentUser.CompanyId!.Value
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
                UserId = _currentUser.UserId!.Value
            };
            _context.CashTransactions.Add(cashTran);

            await _context.SaveChangesAsync();

            if (dto.AutoAllocate)
            {
                await AutoAllocateCustomerInternalAsync(receipt);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            return MapCustomerReceiptToDto(receipt);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ReceiptReadDto> CreateCustomerRefundAsync(CreateReceiptDto dto)
    {
        var branchId = _currentUser.BranchId ?? 0;
        ValidateBranch(branchId);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var receipt = new CustomerReceipt
            {
                CustomerId = dto.PartnerId,
                Amount = -dto.Amount, // Negative amount for refund
                UnallocatedAmount = -dto.Amount,
                Date = dto.Date,
                Method = dto.Method,
                Kind = TransactionKind.Refund,
                Notes = dto.Notes,
                BranchId = branchId,
                CompanyId = _currentUser.CompanyId!.Value
            };
            
            _context.CustomerReceipts.Add(receipt);

            var shiftId = await _shiftService.GetCurrentShiftIdAsync();

            // Record in CashTransaction: Refund to customer is OUT
            var cashTran = new CashTransaction
            {
                CompanyId = receipt.CompanyId,
                Value = dto.Amount, // Positive value for cash out
                Date = dto.Date,
                Type = TransactionType.Out,
                SourceType = TransactionSource.Customer,
                RelatedEntityId = dto.PartnerId,
                ShiftId = shiftId,
                Notes = $"رديات لعميل رقم {dto.PartnerId} - الملاحظات: {dto.Notes}",
                UserId = _currentUser.UserId!.Value
            };
            _context.CashTransactions.Add(cashTran);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapCustomerReceiptToDto(receipt);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task AllocateCustomerReceiptAsync(AllocateReceiptDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var receipt = await _context.CustomerReceipts
                .Include(r => r.Allocations)
                .FirstOrDefaultAsync(r => r.Id == dto.ReceiptId)
                ?? throw new KeyNotFoundException("سند القبض غير موجود");

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

    // ==========================================
    // Supplier Payments
    // ==========================================

    public async Task<ReceiptReadDto> CreateSupplierPaymentAsync(CreateReceiptDto dto)
    {
        var branchId = _currentUser.BranchId ?? 0;
        ValidateBranch(branchId);

        await using var transaction = await _context.Database.BeginTransactionAsync();
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
                CompanyId = _currentUser.CompanyId!.Value
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
                UserId = _currentUser.UserId!.Value
            };
            _context.CashTransactions.Add(cashTran);

            await _context.SaveChangesAsync();

            if (dto.AutoAllocate)
            {
                await AutoAllocateSupplierInternalAsync(payment);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            return MapSupplierPaymentToDto(payment);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ReceiptReadDto> CreateSupplierRefundAsync(CreateReceiptDto dto)
    {
        var branchId = _currentUser.BranchId ?? 0;
        ValidateBranch(branchId);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var payment = new SupplierPayment
            {
                SupplierId = dto.PartnerId,
                Amount = -dto.Amount, // Negative amount for refund
                UnallocatedAmount = -dto.Amount,
                Date = dto.Date,
                Method = dto.Method,
                Kind = TransactionKind.Refund,
                Notes = dto.Notes,
                BranchId = branchId,
                CompanyId = _currentUser.CompanyId!.Value
            };
            
            _context.SupplierPayments.Add(payment);

            var shiftId = await _shiftService.GetCurrentShiftIdAsync();

            // Record in CashTransaction: Refund from supplier is IN
            var cashTran = new CashTransaction
            {
                CompanyId = payment.CompanyId,
                Value = dto.Amount, // Positive value for cash in
                Date = dto.Date,
                Type = TransactionType.In,
                SourceType = TransactionSource.Supplier,
                RelatedEntityId = dto.PartnerId,
                ShiftId = shiftId,
                Notes = $"رديات من مورد رقم {dto.PartnerId} - الملاحظات: {dto.Notes}",
                UserId = _currentUser.UserId!.Value
            };
            _context.CashTransactions.Add(cashTran);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapSupplierPaymentToDto(payment);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task AllocateSupplierPaymentAsync(AllocateReceiptDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var payment = await _context.SupplierPayments
                .Include(p => p.Allocations)
                .FirstOrDefaultAsync(p => p.Id == dto.ReceiptId)
                ?? throw new KeyNotFoundException("سند الصرف غير موجود");

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
            .SumAsync(i => i.Type == InvoiceType.Sale ? (decimal?)i.NetTotal : -(decimal?)i.NetTotal) ?? 0m;

        var receiptsBeforeFrom = await _context.CustomerReceipts
            .Where(r => r.CustomerId == customerId && r.Date < from)
            .SumAsync(r => (decimal?)r.Amount) ?? 0m;

        // رصيد بداية الكشف = رصيد الافتتاح + حركات ما قبل الفترة
        var openingBalance = entityOpeningBalance + invoicesBeforeFrom - receiptsBeforeFrom;

        // ===== 3. جلب حركات الفترة =====
        var invoices = await _context.Invoices
            .Where(i => i.CustomerId == customerId
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Date >= from && i.Date <= to
                     && (i.Type == InvoiceType.Sale || i.Type == InvoiceType.SalesReturn))
            .ToListAsync();

        var receipts = await _context.CustomerReceipts
            .Where(r => r.CustomerId == customerId && r.Date >= from && r.Date <= to)
            .ToListAsync();

        // ===== 4. دمج وترتيب زمني =====
        var allEvents = invoices
            .Select(i => new { i.Date, EventType = "Invoice", Data = (object)i })
            .Concat(receipts.Select(r => new { r.Date, EventType = "Receipt", Data = (object)r }))
            .OrderBy(e => e.Date)
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
                if (rec.Kind != TransactionKind.Refund)
                {
                    stmt.DocumentType = "Receipt";
                    stmt.TransactionType = rec.Kind.ToString();
                    stmt.DocumentId = rec.Id;
                    stmt.Description = $"سند قبض - {rec.Method}";
                    stmt.Credit = rec.Amount;
                    runningBalance -= rec.Amount;
                    totalCredit += rec.Amount;
                }
                else
                {
                    // Refund
                    stmt.DocumentType = "Refund";
                    stmt.TransactionType = rec.Kind.ToString();
                    stmt.DocumentId = rec.Id;
                    stmt.Description = $"رد نقدي للعميل - {rec.Method}";
                    stmt.Debit = Math.Abs(rec.Amount);
                    runningBalance += Math.Abs(rec.Amount);
                    totalDebit += Math.Abs(rec.Amount);
                }
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
            .SumAsync(i => (decimal?)i.NetTotal) ?? 0m;

        // إجمالي مرتجعات البيع
        var returnTotal = await _context.Invoices
            .Where(i => i.CustomerId == customerId
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Type == InvoiceType.SalesReturn)
            .SumAsync(i => (decimal?)i.NetTotal) ?? 0m;

        // إجمالي المقبوضات
        var receivedTotal = await _context.CustomerReceipts
            .Where(r => r.CustomerId == customerId)
            .SumAsync(r => (decimal?)r.Amount) ?? 0m;

        return customer.OpeningBalance + (invoicedTotal - returnTotal) - receivedTotal;
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
            .SumAsync(i => i.Type == InvoiceType.Purchase ? (decimal?)i.NetTotal : -(decimal?)i.NetTotal) ?? 0m;

        var paymentsBeforeFrom = await _context.SupplierPayments
            .Where(p => p.SupplierId == supplierId && p.Date < from)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var openingBalance = entityOpeningBalance + purchasesBeforeFrom - paymentsBeforeFrom;

        // ===== 3. جلب حركات الفترة =====
        var invoices = await _context.Invoices
            .Where(i => i.SupplierId == supplierId
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Date >= from && i.Date <= to
                     && (i.Type == InvoiceType.Purchase || i.Type == InvoiceType.PurchaseReturn))
            .ToListAsync();

        var payments = await _context.SupplierPayments
            .Where(p => p.SupplierId == supplierId && p.Date >= from && p.Date <= to)
            .ToListAsync();

        // ===== 4. دمج وترتيب زمني =====
        var allEvents = invoices
            .Select(i => new { i.Date, EventType = "Invoice", Data = (object)i })
            .Concat(payments.Select(p => new { p.Date, EventType = "Payment", Data = (object)p }))
            .OrderBy(e => e.Date)
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
                if (pmt.Kind != TransactionKind.Refund)
                {
                    stmt.DocumentType = "Payment";
                    stmt.TransactionType = pmt.Kind.ToString();
                    stmt.DocumentId = pmt.Id;
                    stmt.Description = $"سند صرف - {pmt.Method}";
                    stmt.Credit = pmt.Amount;
                    runningBalance -= pmt.Amount;
                    totalCredit += pmt.Amount;
                }
                else
                {
                    stmt.DocumentType = "Refund";
                    stmt.TransactionType = pmt.Kind.ToString();
                    stmt.DocumentId = pmt.Id;
                    stmt.Description = $"رد نقدي من المورد - {pmt.Method}";
                    stmt.Debit = Math.Abs(pmt.Amount);
                    runningBalance += Math.Abs(pmt.Amount);
                    totalDebit += Math.Abs(pmt.Amount);
                }
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
            .SumAsync(i => (decimal?)i.NetTotal) ?? 0m;

        // إجمالي مرتجعات الشراء
        var returnTotal = await _context.Invoices
            .Where(i => i.SupplierId == supplierId
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Type == InvoiceType.PurchaseReturn)
            .SumAsync(i => (decimal?)i.NetTotal) ?? 0m;

        // إجمالي المدفوعات للمورد
        var paidTotal = await _context.SupplierPayments
            .Where(p => p.SupplierId == supplierId)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        return supplier.OpeningBalance + (purchaseTotal - returnTotal) - paidTotal;
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
