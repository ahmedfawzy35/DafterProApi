using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities;
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

    public InvoiceService(
        StoreDbContext context,
        ICurrentUserService currentUser,
        IOutboxService outboxService)
    {
        _context = context;
        _currentUser = currentUser;
        _outboxService = outboxService;
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
                Date = i.Date, TotalValue = i.TotalValue,
                Discount = i.Discount, Paid = i.Paid, IsInstallment = i.IsInstallment,
                Items = i.Items.Select(item => new InvoiceItemReadDto
                {
                    ProductId = item.ProductId, ProductName = item.Product.Name,
                    Quantity = item.Quantity, UnitPrice = item.UnitPrice,
                    Subtotal = item.Quantity * item.UnitPrice
                }).ToList()
            }).ToListAsync();

        return new PagedResult<InvoiceReadDto>
        {
            Items = items, PageNumber = query.PageNumber,
            PageSize = query.PageSize, TotalCount = total
        };
    }

    public async Task<InvoiceReadDto> CreateAsync(CreateInvoiceDto dto)
    {
        var company = await _context.Companies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == _currentUser.CompanyId)
            ?? throw new InvalidOperationException("الشركة غير موجودة");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var invoice = new Invoice
            {
                Type = (InvoiceType)dto.InvoiceType,
                CustomerId = dto.CustomerId, SupplierId = dto.SupplierId,
                Date = dto.Date, Discount = dto.Discount, Paid = dto.Paid,
                IsInstallment = dto.IsInstallment, Notes = dto.Notes,
                CompanyId = _currentUser.CompanyId
            };

            double totalValue = 0;
            var isSale = (InvoiceType)dto.InvoiceType == InvoiceType.Sale;
            var isPurchase = (InvoiceType)dto.InvoiceType == InvoiceType.Purchase;

            foreach (var itemDto in dto.Items)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId)
                    ?? throw new KeyNotFoundException($"المنتج رقم {itemDto.ProductId} غير موجود");

                if (company.ManageInventory)
                {
                    if (isSale && product.StockQuantity < itemDto.Quantity)
                        throw new InvalidOperationException(
                            $"الكمية المتاحة للمنتج '{product.Name}' غير كافية ({product.StockQuantity})");

                    if (isSale) product.StockQuantity -= itemDto.Quantity;
                    else if (isPurchase) product.StockQuantity += itemDto.Quantity;

                    // حفظ حركة المخزون في Outbox
                    await _outboxService.PublishAsync("StockUpdated", new
                    {
                        ProductId = itemDto.ProductId,
                        Quantity = itemDto.Quantity,
                        MovementType = isSale ? "Out" : "In",
                        CompanyId = _currentUser.CompanyId,
                        Timestamp = DateTime.UtcNow
                    });
                }

                invoice.Items.Add(new InvoiceItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice
                });

                totalValue += itemDto.Quantity * itemDto.UnitPrice;
            }

            invoice.TotalValue = totalValue;
            _context.Invoices.Add(invoice);

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
            await transaction.CommitAsync();

            return new InvoiceReadDto
            {
                Id = invoice.Id, TotalValue = invoice.TotalValue,
                Discount = invoice.Discount, Paid = invoice.Paid,
                Date = invoice.Date, IsInstallment = invoice.IsInstallment,
                InvoiceType = invoice.Type.ToString()
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new KeyNotFoundException($"الفاتورة رقم {id} غير موجودة");

        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();
    }
}
