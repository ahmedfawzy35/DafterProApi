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

    public ReturnService(
        StoreDbContext context,
        ICurrentUserService currentUser,
        IInventoryService inventoryService,
        IFinanceService financeService,
        IProductService productService,
        IOutboxService outboxService)
    {
        _context = context;
        _currentUser = currentUser;
        _inventoryService = inventoryService;
        _financeService = financeService;
        _productService = productService;
        _outboxService = outboxService;
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

        if (dto.Items.Any(i => i.OriginalInvoiceItemId == null))
            throw new InvalidOperationException("جميع بنود المرتجع المرجعي يجب أن تحمل OriginalInvoiceItemId");

        var branchId = dto.BranchId > 0 ? dto.BranchId : (_currentUser.BranchId ?? throw new ArgumentException("معرف الفرع ضروري"));
        if (originalInvoice.BranchId != branchId)
            throw new InvalidOperationException("لا يمكن إرجاع فاتورة من فرع مختلف.");

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
                Paid = dto.Paid,
                IsInstallment = dto.IsInstallment,
                Notes = dto.Notes,
                BranchId = branchId,
                OriginalInvoiceId = dto.OriginalInvoiceId,
                ReturnMode = ReturnMode.Referenced,
                CompanyId = _currentUser.CompanyId!.Value
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

            var computedNetTotal = totalValue - dto.Discount + dto.Tax;
            if (dto.Paid > computedNetTotal)
                throw new ArgumentException($"المبلغ المدفوع يتجاوز صافي قيمة المرتجع ({computedNetTotal:F2}).");

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

            await _outboxService.PublishAsync("InvoiceCreated", new
            {
                InvoiceId = invoice.Id,
                CompanyId = _currentUser.CompanyId,
                TotalValue = totalValue,
                Type = returnType,
                Timestamp = DateTime.UtcNow
            });

            var effectivePaid = Math.Min(invoice.Paid, invoice.NetTotal);
            if (effectivePaid > 0)
            {
                var receiptDto = new CreateReceiptDto
                {
                    PartnerId = invoice.CustomerId ?? invoice.SupplierId ?? 0,
                    Amount = effectivePaid,
                    Date = invoice.Date,
                    Method = PaymentMethod.Cash,
                    Notes = $"معاملة مالية مع الفاتورة المرتجعة رقم {invoice.Id}",
                    AutoAllocate = false
                };

                if (returnType == InvoiceType.SalesReturn)
                {
                    await _financeService.CreateCustomerRefundAsync(receiptDto, explicitBranchId: invoice.BranchId);
                }
                else if (returnType == InvoiceType.PurchaseReturn)
                {
                    await _financeService.CreateSupplierRefundAsync(receiptDto, explicitBranchId: invoice.BranchId);
                }
            }

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

        var branchId = dto.BranchId > 0 ? dto.BranchId : (_currentUser.BranchId ?? throw new ArgumentException("معرف الفرع ضروري"));

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
                Status = InvoiceStatus.PendingApproval, // PendingApproval not Confirmed
                Paid = dto.Paid,
                IsInstallment = dto.IsInstallment,
                Notes = dto.Notes,
                BranchId = branchId,
                ReturnMode = ReturnMode.Manual,
                ReturnReason = dto.ReturnReason,
                RequiresApproval = true,
                IsApproved = false,
                OriginalInvoiceId = null,
                CompanyId = _currentUser.CompanyId!.Value
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

            var computedNetTotal = totalValue - dto.Discount + dto.Tax;
            if (dto.Paid > computedNetTotal)
                throw new ArgumentException($"المبلغ المدفوع يتجاوز صافي قيمة المرتجع ({computedNetTotal:F2}).");

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // No ProcessInvoiceStockAsync or CreateRefundAsync here until approved

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

            var effectivePaid = Math.Min(invoice.Paid, invoice.NetTotal);
            if (effectivePaid > 0)
            {
                var refundDto = new CreateReceiptDto
                {
                    PartnerId = invoice.CustomerId ?? invoice.SupplierId ?? 0,
                    Amount = effectivePaid,
                    Date = DateTime.UtcNow,
                    Method = PaymentMethod.Cash,
                    Notes = $"اعتماد مرتجع يدوي رقم {invoice.Id}",
                    AutoAllocate = false
                };

                if (invoice.Type == InvoiceType.SalesReturn)
                {
                    await _financeService.CreateCustomerRefundAsync(refundDto, explicitBranchId: invoice.BranchId);
                }
                else if (invoice.Type == InvoiceType.PurchaseReturn)
                {
                    await _financeService.CreateSupplierRefundAsync(refundDto, explicitBranchId: invoice.BranchId);
                }
            }

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
