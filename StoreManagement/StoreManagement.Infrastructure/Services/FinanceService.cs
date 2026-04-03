using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.Finance;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;
using StoreManagement.Shared.Entities.Sales;

namespace StoreManagement.Infrastructure.Services;

public class FinanceService : IFinanceService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public FinanceService(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
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

            // Record in CashTransaction
            var cashTran = new CashTransaction
            {
                CompanyId = receipt.CompanyId,
                Value = dto.Amount,
                Date = dto.Date,
                Type = TransactionType.In,
                SourceType = TransactionSource.Customer,
                RelatedEntityId = dto.PartnerId,
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

            // Record in CashTransaction
            var cashTran = new CashTransaction
            {
                CompanyId = payment.CompanyId,
                Value = dto.Amount,
                Date = dto.Date,
                Type = TransactionType.Out,
                SourceType = TransactionSource.Supplier,
                RelatedEntityId = dto.PartnerId,
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

    public async Task<List<CustomerStatementDto>> GetCustomerStatementAsync(int customerId, DateTime? from, DateTime? to)
    {
        var result = new List<CustomerStatementDto>();
        var balance = 0m;

        // Query Invoices
        var invoicesQuery = _context.Invoices
            .Where(i => i.CustomerId == customerId && i.Status == InvoiceStatus.Confirmed);
        
        if (from.HasValue) invoicesQuery = invoicesQuery.Where(i => i.Date >= from.Value);
        if (to.HasValue) invoicesQuery = invoicesQuery.Where(i => i.Date <= to.Value);

        var invoices = await invoicesQuery.OrderBy(i => i.Date).ToListAsync();

        // Query Receipts
        var receiptsQuery = _context.CustomerReceipts.Where(r => r.CustomerId == customerId);
        if (from.HasValue) receiptsQuery = receiptsQuery.Where(r => r.Date >= from.Value);
        if (to.HasValue) receiptsQuery = receiptsQuery.Where(r => r.Date <= to.Value);

        var receipts = await receiptsQuery.OrderBy(r => r.Date).ToListAsync();

        // Process sequentially to determine balance over time
        var allEvents = invoices.Select(i => new { i.Date, EventType = "Invoice", Data = (object)i })
            .Concat(receipts.Select(r => new { r.Date, EventType = "Receipt", Data = (object)r }))
            .OrderBy(e => e.Date)
            .ToList();

        foreach (var ev in allEvents)
        {
            var stmt = new CustomerStatementDto
            {
                Date = ev.Date
            };

            if (ev.EventType == "Invoice" && ev.Data is Invoice inv)
            {
                if (inv.Type == InvoiceType.Sale)
                {
                    stmt.DocumentType = "Sale Invoice";
                    stmt.DocumentId = inv.Id;
                    stmt.Description = $"مبيعات فاتورة #{inv.Id}";
                    stmt.Debit = inv.NetTotal;
                    balance += inv.NetTotal;
                }
                else if (inv.Type == InvoiceType.SalesReturn)
                {
                    stmt.DocumentType = "Sales Return";
                    stmt.DocumentId = inv.Id;
                    stmt.Description = $"مرتجع مبيعات #{inv.Id}";
                    stmt.Credit = inv.NetTotal;
                    balance -= inv.NetTotal;
                }
            }
            else if (ev.EventType == "Receipt" && ev.Data is CustomerReceipt rec)
            {
                stmt.DocumentType = "Receipt";
                stmt.DocumentId = rec.Id;
                stmt.Description = $"سند تحصيل نقدية {rec.Method.ToString()}";
                stmt.Credit = rec.Amount;
                balance -= rec.Amount;
            }

            stmt.Balance = balance;
            result.Add(stmt);
        }

        return result;
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
