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

public class InventoryService : IInventoryService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public InventoryService(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<StockTransactionReadDto>> GetHistoryAsync(
        PaginationQueryDto query, int? productId, DateTime? from, DateTime? to)
    {
        var baseQuery = _context.StockTransactions
            .Include(st => st.Product)
            .Include(st => st.User)
            .Where(st => st.CompanyId == _currentUser.CompanyId);

        if (productId.HasValue) baseQuery = baseQuery.Where(st => st.ProductId == productId.Value);
        if (from.HasValue) baseQuery = baseQuery.Where(st => st.Date >= from.Value);
        if (to.HasValue) baseQuery = baseQuery.Where(st => st.Date <= to.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(st => (st.Notes != null && st.Notes.Contains(query.Search)) || st.Product.Name.Contains(query.Search));

        var company = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == _currentUser.CompanyId);
        var merchantName = company?.Name ?? "DafterPro";

        var total = await baseQuery.CountAsync();
        var items = await baseQuery
            .OrderByDescending(st => st.Date)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(st => new StockTransactionReadDto
            {
                Id = st.Id,
                ProductId = st.ProductId,
                ProductName = st.Product.Name,
                Quantity = st.Quantity,
                Type = st.MovementType.ToString(),
                Date = st.Date,
                Notes = st.Notes,
                UserName = st.User.UserName ?? "Unknown",
                MerchantName = merchantName
            })
            .ToListAsync();

        return new PagedResult<StockTransactionReadDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task CreateAdjustmentAsync(CreateStockAdjustmentDto dto)
    {
        var product = await _context.Products.FindAsync(dto.ProductId)
            ?? throw new KeyNotFoundException($"المنتج رقم {dto.ProductId} غير موجود");

        var transaction = new StockTransaction
        {
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            MovementType = dto.Type == "In" ? StockMovementType.In : StockMovementType.Out,
            Date = DateTime.UtcNow,
            Notes = dto.Notes,
            CompanyId = (int)_currentUser.CompanyId!,
            UserId = (int)_currentUser.UserId!
        };

        // تحديث كمية المخزون في المنتج
        if (transaction.MovementType == StockMovementType.In)
            product.StockQuantity += dto.Quantity;
        else
            product.StockQuantity -= dto.Quantity;

        _context.StockTransactions.Add(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task RegisterInitialStockAsync(int productId, double quantity)
    {
        var product = await _context.Products.FindAsync(productId)
            ?? throw new KeyNotFoundException($"المنتج رقم {productId} غير موجود");

        var transaction = new StockTransaction
        {
            ProductId = productId,
            Quantity = quantity,
            MovementType = StockMovementType.In,
            Date = DateTime.UtcNow,
            Notes = "رصيد أول المدة (جرد افتتاحي)",
            CompanyId = (int)_currentUser.CompanyId!,
            UserId = (int)_currentUser.UserId!
        };

        product.StockQuantity = quantity;

        _context.StockTransactions.Add(transaction);
        await _context.SaveChangesAsync();
    }
}
