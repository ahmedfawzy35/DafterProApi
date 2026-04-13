using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.Sales;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StoreManagement.Infrastructure.Services;

public class ReturnService : IReturnService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IInventoryService _inventoryService;
    private readonly IFinanceService _financeService;
    private readonly IProductService _productService;
    private readonly IOutboxService _outboxService;
    private readonly IAccountingPeriodService _accountingPeriodService;
    private readonly IReturnPolicyService _returnPolicy;

    public ReturnService(
        StoreDbContext context,
        ICurrentUserService currentUser,
        IInventoryService inventoryService,
        IFinanceService financeService,
        IProductService productService,
        IOutboxService outboxService,
        IAccountingPeriodService accountingPeriodService,
        IReturnPolicyService returnPolicy)
    {
        _context = context;
        _currentUser = currentUser;
        _inventoryService = inventoryService;
        _financeService = financeService;
        _productService = productService;
        _outboxService = outboxService;
        _accountingPeriodService = accountingPeriodService;
        _returnPolicy = returnPolicy;
    }

    public async Task<InvoiceReadDto> CreateReferencedReturnAsync(CreateInvoiceDto dto, InvoiceType returnType)
    {
        if (dto.OriginalInvoiceId == null)
            throw new InvalidOperationException("رقم الفاتورة الأصلية مطلوب للمرتجع المرجعي.");

        var originalInvoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == dto.OriginalInvoiceId && i.CompanyId == _currentUser.CompanyId);

        if (originalInvoice == null)
            throw new InvalidOperationException("الفاتورة الأصلية غير موجودة أو لا تتبع مساحتك.");

        if (originalInvoice.Status != InvoiceStatus.Confirmed)
            throw new InvalidOperationException("لا يمكن عمل مرتجع لفاتورة غير مؤكدة.");

        if ((returnType == InvoiceType.SalesReturn && originalInvoice.Type != InvoiceType.Sale) ||
            (returnType == InvoiceType.PurchaseReturn && originalInvoice.Type != InvoiceType.Purchase))
            throw new InvalidOperationException("نوع المرتجع لا يتطابق مع نوع الفاتورة الأصلية.");

        if (originalInvoice.CustomerId != dto.CustomerId && originalInvoice.SupplierId != dto.SupplierId)
            throw new InvalidOperationException("العميل أو المورد لا يتطابق مع الفاتورة الأصلية.");

        if (dto.Items == null || !dto.Items.Any())
            throw new InvalidOperationException("لا يمكن إنشاء مرتجع بدون أصناف.");

        if (dto.Items.Any(i => i.Quantity <= 0))
            throw new InvalidOperationException("الكميات المرتجعة يجب أن تكون أكبر من الصفر.");

        // Idempotency Check
        if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
        {
            var existingReturn = await _context.Invoices.FirstOrDefaultAsync(i => i.IdempotencyKey == dto.IdempotencyKey);
            if (existingReturn != null)
                throw new InvalidOperationException($"هذه العملية تم تنفيذها بالفعل. (IdempotencyKey: {dto.IdempotencyKey})");
        }

        if (dto.Items.Any(i => i.OriginalInvoiceItemId == null))
            throw new InvalidOperationException("جميع بنود المرتجع المرجعي يجب أن تحمل OriginalInvoiceItemId");

        if (dto.BranchId <= 0)
            throw new ArgumentException("معرف الفرع (BranchId) التابع للمرتجع إلزامي ولا يمكن الاعتماد على الفرع الافتراضي.");
        var branchId = dto.BranchId;
        if (originalInvoice.BranchId != branchId)
            throw new InvalidOperationException("لا يمكن إرجاع فاتورة من فرع مختلف.");

        // التحقق من سياسة المرتجعات (Policy Check)
        await _returnPolicy.EnsureReturnIsAllowedAsync(originalInvoice.Date, dto.Items.Sum(x => (decimal)x.Quantity * x.UnitPrice));

        await _accountingPeriodService.EnsureDateIsOpenAsync(_currentUser.CompanyId!.Value, dto.Date);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var invoice = new Invoice
            {
                Type = returnType,
                CustomerId = dto.CustomerId,
                SupplierId = dto.SupplierId,
                Date = dto.Date,
                Discount = dto.Discount,
                Tax = dto.Tax,
                Status = InvoiceStatus.Confirmed,
                Paid = 0,  // Paid always 0 for returns — settlement created via CreateCustomerReturnSettlementAsync
                IsInstallment = dto.IsInstallment,
                Notes = dto.Notes,
                BranchId = branchId,
                OriginalInvoiceId = dto.OriginalInvoiceId,
                ReturnMode = ReturnMode.Referenced,
                IssueCashRefund = dto.IssueCashRefund,  // Persist for traceability/audit
                CompanyId = _currentUser.CompanyId!.Value,
                IdempotencyKey = dto.IdempotencyKey
            };

            decimal totalValue = 0;

            foreach (var itemDto in dto.Items)
            {
                var originalItem = await _context.InvoiceItems
                    .Include(x => x.Invoice)
                    .FirstOrDefaultAsync(x => x.Id == itemDto.OriginalInvoiceItemId && x.InvoiceId == dto.OriginalInvoiceId);

                if (originalItem == null)
                    throw new InvalidOperationException($"البند الأصلي {itemDto.OriginalInvoiceItemId} غير موجود.");

                if (originalItem.Invoice.CompanyId != _currentUser.CompanyId)
                    throw new InvalidOperationException("حماية الصلاحيات: البند لا يتبع لهذه الشركة.");

                if (originalItem.ProductId != itemDto.ProductId)
                    throw new InvalidOperationException("المنتج في المرتجع لا يتطابق مع المنتج في الفاتورة الأصلية.");

                var alreadyReturned = await _context.InvoiceItems
                    .Where(i => i.OriginalInvoiceItemId == itemDto.OriginalInvoiceItemId && i.Invoice.Type == returnType && i.Invoice.Status == InvoiceStatus.Confirmed)
                    .SumAsync(i => i.Quantity);

                var allowed = originalItem.Quantity - alreadyReturned;

                if (itemDto.Quantity > allowed)
                    throw new InvalidOperationException($"الكمية المرتجعة للمنتج {itemDto.ProductId} تتجاوز المسموح (المسموح: {allowed}).");

                invoice.Items.Add(new InvoiceItem
                {
                    ProductId = originalItem.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = originalItem.UnitPrice, // السعر مثبت من الفاتورة الأصلية
                    CostPriceAtSale = originalItem.CostPriceAtSale,
                    OriginalInvoiceItemId = itemDto.OriginalInvoiceItemId
                });

                totalValue += (decimal)itemDto.Quantity * originalItem.UnitPrice;
            }

            invoice.TotalValue = totalValue;

            // Paid لا يُستخدم في منطق المرتجعات. القيد المالي يتم دائمًا عبر CreateCustomerReturnSettlementAsync
            invoice.Paid = 0;

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Double check logic guards against concurrent double returns
            foreach (var item in invoice.Items)
            {
                var alreadyReturnedAfterSave = await _context.InvoiceItems
                    .Where(i => i.OriginalInvoiceItemId == item.OriginalInvoiceItemId && i.Invoice.Type == returnType && i.Invoice.Status == InvoiceStatus.Confirmed && i.InvoiceId != invoice.Id)
                    .SumAsync(i => i.Quantity);

                var originalQty = await _context.InvoiceItems.Where(i => i.Id == item.OriginalInvoiceItemId).Select(i => i.Quantity).FirstOrDefaultAsync();

                if (alreadyReturnedAfterSave + item.Quantity > originalQty)
                {
                    throw new DbUpdateConcurrencyException("تم اكتشاف محاولة إرجاع مزدوجة متزامنة (Concurrency Conflict).");
                }
            }

            foreach (var item in invoice.Items)
            {
                await _inventoryService.ProcessInvoiceStockAsync(
                    invoiceId: invoice.Id,
                    invoiceItemId: item.Id,
                    productId: item.ProductId,
                    quantity: item.Quantity,
                    branchId: invoice.BranchId,
                    invoiceType: invoice.Type,
                    notes: $"مرتجع مرجعي مرتبط بالفاتورة الأصلية {invoice.OriginalInvoiceId}");
            }

            // إنشاء قيد الإرجاع (CreditNote أو Refund) بناءً على خيار IssueCashRefund
            var settlementDto = new CreateReceiptDto
            {
                PartnerId = invoice.CustomerId ?? invoice.SupplierId ?? 0,
                Amount = invoice.NetTotal,  // موجب دائمًا
                Date = invoice.Date,
                Method = PaymentMethod.Cash,
                Notes = $"مرتجع فاتورة #{invoice.Id} - {invoice.Notes}",
                AutoAllocate = false,
                IdempotencyKey = dto.IdempotencyKey != null ? $"{dto.IdempotencyKey}_settlement" : null
            };

            if (returnType == InvoiceType.SalesReturn)
            {
                await _financeService.CreateCustomerReturnSettlementAsync(
                    settlementDto,
                    explicitBranchId: invoice.BranchId,
                    createCashTransaction: invoice.IssueCashRefund,
                    returnInvoiceId: invoice.Id);
            }
            else if (returnType == InvoiceType.PurchaseReturn)
            {
                await _financeService.CreateSupplierReturnSettlementAsync(
                    settlementDto,
                    explicitBranchId: invoice.BranchId,
                    createCashTransaction: invoice.IssueCashRefund,
                    returnInvoiceId: invoice.Id);
            }

            // تحديث PaymentStatus = Paid
            // ملاحظة: PaymentStatus.Paid هنا تعني Fully Settled وليس بالضرورة دفعًا نقديًا.
            // فاتورة المرتجع دائمًا مسوَّاة بالكامل فور إنشائها أو اعتمادها.
            invoice.PaymentStatus = PaymentStatus.Paid;

            await _outboxService.PublishAsync("InvoiceCreated", new
            {
                InvoiceId = invoice.Id,
                CompanyId = _currentUser.CompanyId,
                TotalValue = totalValue,
                Type = returnType,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapToReadDto(invoice);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException("تم تعديل كمية المنتج المرتجع بواسطة عملية أخرى، يرجى تحديث البيانات وإعادة المحاولة.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<InvoiceReadDto> CreateManualReturnAsync(CreateInvoiceDto dto, InvoiceType returnType)
    {
        if (string.IsNullOrWhiteSpace(dto.ReturnReason))
            throw new InvalidOperationException("سبب الإرجاع مطلوب للمرتجع اليدوي.");

        if (dto.CustomerId == null && dto.SupplierId == null)
            throw new InvalidOperationException("يجب تحديد العميل أو المورد في المرتجع اليدوي.");

        if (dto.Items.Any(i => i.OriginalInvoiceItemId != null))
            throw new InvalidOperationException("المرتجع اليدوي لا يجب أن يحتوي على OriginalInvoiceItemId");

        if (dto.BranchId <= 0)
            throw new ArgumentException("معرف الفرع (BranchId) التابع للمرتجع إلزامي ولا يمكن الاعتماد على الفرع الافتراضي.");

        if (dto.Items == null || !dto.Items.Any())
            throw new InvalidOperationException("لا يمكن إنشاء مرتجع بدون أصناف.");

        if (dto.Items.Any(i => i.Quantity <= 0))
            throw new InvalidOperationException("الكميات المرتجعة يجب أن تكون أكبر من الصفر.");

        // Idempotency Check
        if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
        {
            var existingReturn = await _context.Invoices.FirstOrDefaultAsync(i => i.IdempotencyKey == dto.IdempotencyKey);
            if (existingReturn != null)
                throw new InvalidOperationException($"هذه العملية تم تنفيذها بالفعل. (IdempotencyKey: {dto.IdempotencyKey})");
        }

        var branchId = dto.BranchId;

        // التحقق من سياسة المرتجعات (Policy Check)
        // في المرتجع اليدوي، نعتبر تاريخ الإرجاع هو تاريخ اليوم ولكن نتحقق من تفعيل النظام
        await _returnPolicy.EnsureReturnIsAllowedAsync(DateTime.UtcNow, dto.Items.Sum(x => (decimal)x.Quantity * x.UnitPrice));

        await _accountingPeriodService.EnsureDateIsOpenAsync(_currentUser.CompanyId!.Value, dto.Date);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var invoice = new Invoice
            {
                Type = returnType,
                CustomerId = dto.CustomerId,
                SupplierId = dto.SupplierId,
                Date = dto.Date,
                Discount = dto.Discount,
                Tax = dto.Tax,
                Status = InvoiceStatus.PendingApproval,
                Paid = 0,  // Paid always 0 for returns
                IsInstallment = dto.IsInstallment,
                Notes = dto.Notes,
                BranchId = branchId,
                ReturnMode = ReturnMode.Manual,
                ReturnReason = dto.ReturnReason,
                RequiresApproval = true,
                IsApproved = false,
                OriginalInvoiceId = null,
                IssueCashRefund = dto.IssueCashRefund,  // Persist for use during ApproveManualReturnAsync
                CompanyId = _currentUser.CompanyId!.Value,
                IdempotencyKey = dto.IdempotencyKey
            };

            decimal totalValue = 0;

            foreach (var itemDto in dto.Items)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId)
                    ?? throw new KeyNotFoundException($"المنتج رقم {itemDto.ProductId} غير موجود");

                invoice.Items.Add(new InvoiceItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice, // User defined price
                    CostPriceAtSale = product.CostPrice
                });

                totalValue += (decimal)itemDto.Quantity * itemDto.UnitPrice;
            }

            invoice.TotalValue = totalValue;

            // Paid لا يُستخدم في منطق المرتجعات.
            invoice.Paid = 0;

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // لا يتم إنشاء Settlement أو Stock حتى يتم اعتماد المرتجع اليدوي.

            await _outboxService.PublishAsync("ManualReturnPendingApproval", new
            {
                InvoiceId = invoice.Id,
                CompanyId = _currentUser.CompanyId,
                TotalValue = totalValue,
                Type = returnType,
                Timestamp = DateTime.UtcNow
            });

            await transaction.CommitAsync();
            return MapToReadDto(invoice);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<InvoiceReadDto> ApproveManualReturnAsync(int invoiceId, string? notes)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.CompanyId == _currentUser.CompanyId)
            ?? throw new KeyNotFoundException("الفاتورة غير موجودة.");

        if (invoice.ReturnMode != ReturnMode.Manual)
            throw new InvalidOperationException("يمكن اعتماد المرتجعات اليدوية فقط.");

        if (invoice.Status != InvoiceStatus.PendingApproval)
            throw new InvalidOperationException("حالة الفاتورة تمنع اعتمادها.");

        await _accountingPeriodService.EnsureDateIsOpenAsync(invoice.CompanyId, invoice.Date);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            invoice.Status = InvoiceStatus.Confirmed;
            invoice.IsApproved = true;
            invoice.ApprovedByUserId = _currentUser.UserId;
            invoice.ApprovedAt = DateTime.UtcNow;
            invoice.ApprovalNotes = notes;

            var company = await _context.Companies.FindAsync(invoice.CompanyId);

            foreach (var item in invoice.Items)
            {
                if (company?.ManageInventory == true)
                {
                    await _inventoryService.ProcessInvoiceStockAsync(
                        invoiceId: invoice.Id,
                        invoiceItemId: item.Id,
                        productId: item.ProductId,
                        quantity: item.Quantity,
                        branchId: invoice.BranchId,
                        invoiceType: invoice.Type,
                        notes: $"اعتماد مرتجع يدوي رقم {invoice.Id}");
                }
            }

            // إنشاء قيد الإرجاع بناءً على خيار IssueCashRefund المخزَّن في الفاتورة
            var settlementDto = new CreateReceiptDto
            {
                PartnerId = invoice.CustomerId ?? invoice.SupplierId ?? 0,
                Amount = invoice.NetTotal,  // موجب دائمًا
                Date = DateTime.UtcNow,
                Method = PaymentMethod.Cash,
                Notes = $"اعتماد مرتجع يدوي رقم {invoice.Id}"
            };

            if (invoice.Type == InvoiceType.SalesReturn)
            {
                await _financeService.CreateCustomerReturnSettlementAsync(
                    settlementDto,
                    explicitBranchId: invoice.BranchId,
                    createCashTransaction: invoice.IssueCashRefund,  // مخزَّن من وقت إنشاء المرتجع
                    returnInvoiceId: invoice.Id);
            }
            else if (invoice.Type == InvoiceType.PurchaseReturn)
            {
                await _financeService.CreateSupplierReturnSettlementAsync(
                    settlementDto,
                    explicitBranchId: invoice.BranchId,
                    createCashTransaction: invoice.IssueCashRefund,
                    returnInvoiceId: invoice.Id);
            }

            // تحديث PaymentStatus للمرتجع
            // ملاحظة: Paid هنا تعني Fully Settled وليس بالضرورة دفعًا نقديًا.
            invoice.PaymentStatus = PaymentStatus.Paid;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapToReadDto(invoice);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task RejectManualReturnAsync(int invoiceId, string? reason)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.CompanyId == _currentUser.CompanyId)
            ?? throw new KeyNotFoundException("الفاتورة غير موجودة.");

        if (invoice.ReturnMode != ReturnMode.Manual)
            throw new InvalidOperationException("يمكن رفض المرتجعات اليدوية فقط.");

        if (invoice.Status != InvoiceStatus.PendingApproval)
            throw new InvalidOperationException("حالة الفاتورة تمنع رفضها.");

        invoice.Status = InvoiceStatus.Rejected;
        invoice.RejectionReason = reason;
        invoice.IsApproved = false;

        await _context.SaveChangesAsync();
    }

    public async Task<StoreManagement.Shared.Common.PagedResult<InvoiceReadDto>> SearchForOriginalInvoiceAsync(int? customerId, int? supplierId, int? productId, DateTime? from, DateTime? to, PaginationQueryDto query)
    {
        var baseQuery = _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .Include(i => i.Items).ThenInclude(item => item.Product)
            .Where(i => i.CompanyId == _currentUser.CompanyId)
            .Where(i => i.Status == InvoiceStatus.Confirmed)
            .Where(i => i.Type == InvoiceType.Sale || i.Type == InvoiceType.Purchase) // Only original sales or purchases
            .AsQueryable();

        if (customerId.HasValue) baseQuery = baseQuery.Where(i => i.CustomerId == customerId.Value);
        if (supplierId.HasValue) baseQuery = baseQuery.Where(i => i.SupplierId == supplierId.Value);
        if (from.HasValue) baseQuery = baseQuery.Where(i => i.Date >= from.Value);
        if (to.HasValue) baseQuery = baseQuery.Where(i => i.Date <= to.Value);
        
        if (productId.HasValue) 
        {
            baseQuery = baseQuery.Where(i => i.Items.Any(item => item.ProductId == productId.Value));
        }

        var total = await baseQuery.CountAsync();
        var items = await baseQuery
            .OrderByDescending(i => i.Date)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(i => MapToReadDto(i))
            .ToListAsync();

        return new StoreManagement.Shared.Common.PagedResult<InvoiceReadDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    private InvoiceReadDto MapToReadDto(Invoice invoice)
    {
        return new InvoiceReadDto
        {
            Id = invoice.Id,
            InvoiceType = invoice.Type.ToString(),
            TotalValue = invoice.TotalValue,
            Discount = invoice.Discount,
            Tax = invoice.Tax,
            Paid = invoice.Paid,
            AllocatedAmount = invoice.AllocatedAmount,
            Date = invoice.Date,
            IsInstallment = invoice.IsInstallment,
            Status = invoice.Status.ToString(),
            PaymentStatus = invoice.PaymentStatus.ToString(),
            ReturnMode = invoice.ReturnMode?.ToString(),
            ReturnReason = invoice.ReturnReason,
            RequiresApproval = invoice.RequiresApproval,
            IsApproved = invoice.IsApproved,
            Items = invoice.Items.Select(i => new InvoiceItemReadDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal,
                OriginalInvoiceItemId = i.OriginalInvoiceItemId
            }).ToList()
        };
    }
}
