using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
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

/// <summary>
/// طبقة Business Logic للفواتير مع إدارة المخزون والـ Outbox Pattern
/// </summary>
public class InvoiceService : IInvoiceService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IOutboxService _outboxService;
    private readonly IInventoryService _inventoryService;
    private readonly IProductService _productService;
    private readonly IFinanceService _financeService;
    private readonly IReturnService _returnService;

    public InvoiceService(
        StoreDbContext context,
        ICurrentUserService currentUser,
        IOutboxService outboxService,
        IInventoryService inventoryService,
        IProductService productService,
        IFinanceService financeService,
        IReturnService returnService)
    {
        _context = context;
        _currentUser = currentUser;
        _outboxService = outboxService;
        _inventoryService = inventoryService;
        _productService = productService;
        _financeService = financeService;
        _returnService = returnService;
    }

    public async Task<PagedResult<InvoiceReadDto>> GetAllAsync(
        PaginationQueryDto query, InvoiceType? type, DateTime? from, DateTime? to)
    {
        var baseQuery = _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .Include(i => i.Items).ThenInclude(item => item.Product)
            .AsQueryable();

        if (type.HasValue) baseQuery = baseQuery.Where(i => i.Type == type.Value);
        if (from.HasValue) baseQuery = baseQuery.Where(i => i.Date >= from.Value);
        if (to.HasValue) baseQuery = baseQuery.Where(i => i.Date <= to.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            baseQuery = baseQuery.Where(i => 
                (i.Customer != null && i.Customer.Name.Contains(query.Search)) || 
                (i.Supplier != null && i.Supplier.Name.Contains(query.Search)) ||
                (i.Notes != null && i.Notes.Contains(query.Search)));
        }

        var company = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == _currentUser.CompanyId);
        var merchantName = company?.Name ?? "DafterPro";

        var total = await baseQuery.CountAsync();
        var items = await baseQuery
            .OrderByDescending(i => i.Date)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(i => new InvoiceReadDto
            {
                Id = i.Id, InvoiceType = i.Type.ToString(),
                CustomerName = i.Customer != null ? i.Customer.Name : null,
                SupplierName = i.Supplier != null ? i.Supplier.Name : null,
                MerchantName = merchantName,
                Date = i.Date, TotalValue = i.TotalValue,
                Discount = i.Discount, Paid = i.Paid, IsInstallment = i.IsInstallment,
                Items = i.Items.Select(item => new InvoiceItemReadDto
                {
                    ProductId = item.ProductId, ProductName = item.Product.Name,
                    Quantity = item.Quantity, UnitPrice = item.UnitPrice,
                    Subtotal = (decimal)item.Quantity * item.UnitPrice
                }).ToList()
            }).ToListAsync();

        return new PagedResult<InvoiceReadDto>
        {
            Items = items, PageNumber = query.PageNumber,
            PageSize = query.PageSize, TotalCount = total
        };
    }

    public async Task<InvoiceReadDto?> GetByIdAsync(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .Include(i => i.Items).ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return null;

        var company = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == _currentUser.CompanyId);

        return new InvoiceReadDto
        {
            Id = invoice.Id, InvoiceType = invoice.Type.ToString(),
            CustomerName = invoice.Customer?.Name,
            SupplierName = invoice.Supplier?.Name,
            MerchantName = company?.Name ?? "DafterPro",
            Date = invoice.Date, TotalValue = invoice.TotalValue,
            Discount = invoice.Discount, Paid = invoice.Paid, IsInstallment = invoice.IsInstallment,
            Items = invoice.Items.Select(item => new InvoiceItemReadDto
            {
                ProductId = item.ProductId, ProductName = item.Product.Name,
                Quantity = item.Quantity, UnitPrice = item.UnitPrice,
                Subtotal = (decimal)item.Quantity * item.UnitPrice
            }).ToList()
        };
    }

    public async Task<InvoiceReadDto> CreateAsync(CreateInvoiceDto dto)
    {
        var type = (InvoiceType)dto.InvoiceType;
        bool isReturn = type is InvoiceType.SalesReturn or InvoiceType.PurchaseReturn;

        if (isReturn)
        {
            if (dto.ReturnMode == null)
                throw new InvalidOperationException("ReturnMode مطلوب للمرتجع (1=مرجعي، 2=يدوي).");

            return (ReturnMode)dto.ReturnMode.Value == ReturnMode.Referenced
                ? await _returnService.CreateReferencedReturnAsync(dto, type)
                : await _returnService.CreateManualReturnAsync(dto, type);
        }

        return await CreateSaleOrPurchaseInternalAsync(dto, type);
    }

    private async Task<InvoiceReadDto> CreateSaleOrPurchaseInternalAsync(CreateInvoiceDto dto, InvoiceType invoiceTypeEnum)
    {
        var company = await _context.Companies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == _currentUser.CompanyId)
            ?? throw new InvalidOperationException("الشركة غير موجودة");

        var invoiceStatus = (InvoiceStatus)(dto.Status ?? (int)InvoiceStatus.Confirmed);

        if (invoiceStatus == InvoiceStatus.Draft && dto.Paid > 0)
        {
            throw new InvalidOperationException("لا يمكن تسجيل دفعة مالية (Paid) لفاتورة مسودة (Draft).");
        }



        if (dto.BranchId <= 0)
            throw new ArgumentException("معرف الفرع (BranchId) التابع للفاتورة إلزامي ولا يمكن الاعتماد على الفرع الافتراضي.");
            
        var branchId = dto.BranchId;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var invoice = new Invoice
            {
                Type = invoiceTypeEnum,
                CustomerId = dto.CustomerId, SupplierId = dto.SupplierId,
                Date = dto.Date, Discount = dto.Discount, 
                Tax = dto.Tax, Status = invoiceStatus,
                Paid = dto.Paid, IsInstallment = dto.IsInstallment, 
                Notes = dto.Notes,
                BranchId = branchId,
                OriginalInvoiceId = dto.OriginalInvoiceId,
                CompanyId = _currentUser.CompanyId!.Value
            };

            decimal totalValue = 0;
            var isSale = invoiceTypeEnum == InvoiceType.Sale;
            var isPurchase = invoiceTypeEnum == InvoiceType.Purchase;

            foreach (var itemDto in dto.Items)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId)
                    ?? throw new KeyNotFoundException($"المنتج رقم {itemDto.ProductId} غير موجود");

                invoice.Items.Add(new InvoiceItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    CostPriceAtSale = product.CostPrice // ✅ تخزين تكلفة الوحدة وقت البيع لاحتساب الأرباح
                });

                totalValue += (decimal)itemDto.Quantity * itemDto.UnitPrice;
            }

            invoice.TotalValue = totalValue;

            // ✅ Validation: Paid لا يجب أن يتجاوز NetTotal = (TotalValue - Discount + Tax)
            // NetTotal هنا لم يُحفظ بعد (computed property)، لذا نحسبه يدوياً
            var computedNetTotal = totalValue - dto.Discount + dto.Tax;
            if (dto.Paid > computedNetTotal)
                throw new ArgumentException(
                    $"المبلغ المدفوع ({dto.Paid:F2}) يتجاوز صافي قيمة الفاتورة ({computedNetTotal:F2}). " +
                    $"الحد الأقصى المسموح = NetTotal (الإجمالي - الخصم + الضريبة).");

            _context.Invoices.Add(invoice);

            // نحفظ الفاتورة أولاً للحصول على Invoice.Id الخاص بها
            await _context.SaveChangesAsync();

            // معالجة المخزون والتكلفة بعد حفظ الفاتورة (داخل نفس الـ Transaction)
            if (invoice.Status == InvoiceStatus.Confirmed)
            {
                foreach (var item in invoice.Items)
                {
                    if (company.ManageInventory)
                    {
                        await _inventoryService.ProcessInvoiceStockAsync(
                            invoiceId: invoice.Id,
                            invoiceItemId: item.Id,
                            productId: item.ProductId,
                            quantity: item.Quantity,
                            branchId: invoice.BranchId,
                            invoiceType: invoice.Type,
                            notes: $"حركة مرتبطة بالفاتورة {invoice.Id}");
                    }

                    // تحديث سعر التكلفة إذا كانت الفاتورة مشتريات (آخر تكلفة شراء)
                    if (isPurchase)
                    {
                        await _productService.UpdateCostPriceAsync(
                            productId: item.ProductId,
                            newCost: item.UnitPrice,
                            reason: $"فاتورة مشتريات رقم {invoice.Id}");
                    }
                }
            }

            // حفظ Event الفاتورة في Outbox
            await _outboxService.PublishAsync("InvoiceCreated", new
            {
                InvoiceId = invoice.Id,
                CompanyId = _currentUser.CompanyId,
                TotalValue = totalValue,
                Type = dto.InvoiceType,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // سجل حركة نقدية أو سند وفق النظام المالي الحديث إذا تم دفع مبلغ وكانت مؤكدة
            // ✅ نستخدم Min(Paid, NetTotal) كـ ceiling إضافي للحماية من أي bypass مستقبلي
            var effectivePaid = Math.Min(invoice.Paid, invoice.NetTotal);
            if (effectivePaid > 0 && invoice.Status == InvoiceStatus.Confirmed)
            {
                var receiptDto = new CreateReceiptDto
                {
                    PartnerId = invoice.CustomerId ?? invoice.SupplierId ?? 0,
                    Amount = effectivePaid,
                    Date = invoice.Date,
                    Method = PaymentMethod.Cash,
                    Notes = $"معاملة مالية مع الفاتورة رقم {invoice.Id}",
                    AutoAllocate = false
                };

                if (isSale)
                {
                    var receipt = await _financeService.CreateCustomerReceiptAsync(receiptDto, explicitBranchId: invoice.BranchId);
                    await _financeService.AllocateDirectToInvoiceAsync(receipt.Id, invoice.Id, effectivePaid);
                }
                else if (isPurchase)
                {
                    var payment = await _financeService.CreateSupplierPaymentAsync(receiptDto, explicitBranchId: invoice.BranchId);
                    await _financeService.AllocateDirectToSupplierInvoiceAsync(payment.Id, invoice.Id, effectivePaid);
                }
            }

            await transaction.CommitAsync();

            return new InvoiceReadDto
            {
                Id = invoice.Id, TotalValue = invoice.TotalValue,
                Discount = invoice.Discount, Paid = invoice.Paid,
                Date = invoice.Date, IsInstallment = invoice.IsInstallment,
                InvoiceType = invoice.Type.ToString()
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException("تم تعديل رصيد المنتج بواسطة عملية أخرى، يرجى تحديث البيانات وإعادة المحاولة. (Concurrency Conflict)");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == _currentUser.CompanyId)
            ?? throw new KeyNotFoundException($"الفاتورة رقم {id} غير موجودة");

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException("لا يمكن حذف فاتورة غير مسودة. استخدم Cancellation (الإلغاء).");
        }

        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();
    }

    public async Task CancelAsync(int id)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.CustomerAllocations) // Includes allocations if Sale/SalesReturn (we can reverse them)
            .Include(i => i.SupplierAllocations) 
            .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == _currentUser.CompanyId)
            ?? throw new KeyNotFoundException($"الفاتورة رقم {id} غير موجودة");

        if (invoice.Status == InvoiceStatus.Cancelled)
            throw new InvalidOperationException("الفاتورة ملغية بالفعل.");

        if (invoice.Status == InvoiceStatus.Draft)
            throw new InvalidOperationException("لا يمكن إلغاء فاتورة مسودة. يمكنك حذفها باستخدام (Delete).");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            invoice.Status = InvoiceStatus.Cancelled;

            // Reverse Inventory manually via InventoryService
            var company = await _context.Companies
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == _currentUser.CompanyId);

            if (company != null && company.ManageInventory)
            {
                foreach (var item in invoice.Items)
                {
                    await _inventoryService.ReverseInvoiceStockAsync(
                        invoiceId: invoice.Id,
                        invoiceItemId: item.Id,
                        productId: item.ProductId,
                        quantity: item.Quantity,
                        branchId: invoice.BranchId,
                        originalInvoiceType: invoice.Type,
                        notes: $"إلغاء الفاتورة {invoice.Id}");
                }
            }

            // Reverse financial allocations
            if (invoice.CustomerAllocations != null && invoice.CustomerAllocations.Any())
            {
                foreach (var alloc in invoice.CustomerAllocations.ToList())
                {
                    var receipt = await _context.CustomerReceipts.FindAsync(alloc.CustomerReceiptId);
                    if (receipt != null)
                    {
                        receipt.UnallocatedAmount += alloc.Amount;
                    }
                    _context.CustomerReceiptAllocations.Remove(alloc);
                }
            }

            if (invoice.SupplierAllocations != null && invoice.SupplierAllocations.Any())
            {
                foreach (var alloc in invoice.SupplierAllocations.ToList())
                {
                    var payment = await _context.SupplierPayments.FindAsync(alloc.SupplierPaymentId);
                    if (payment != null)
                    {
                        payment.UnallocatedAmount += alloc.Amount;
                    }
                    _context.SupplierPaymentAllocations.Remove(alloc);
                }
            }

            invoice.AllocatedAmount = 0;
            invoice.PaymentStatus = PaymentStatus.Unpaid;

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


