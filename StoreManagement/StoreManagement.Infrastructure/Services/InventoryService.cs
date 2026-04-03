using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.Inventory;
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
                BeforeQuantity = st.BeforeQuantity,
                AfterQuantity = st.AfterQuantity,
                Type = st.MovementType.ToString(),
                ReferenceType = st.ReferenceType.ToString(),
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
        if (dto.BranchId <= 0)
            throw new ArgumentException("معرف الفرع ضروري لأي حركة مخزون");

        var product = await _context.Products.FindAsync(dto.ProductId)
            ?? throw new KeyNotFoundException($"المنتج رقم {dto.ProductId} غير موجود");

        var movementType = dto.Type == "In" ? StockMovementType.In : StockMovementType.Out;

        // منع الهبوط الواضح للرصيد دون الصفر
        if (movementType == StockMovementType.Out && product.StockQuantity - dto.Quantity < 0)
            throw new InvalidOperationException("لا يمكن أن يكون المخزون سالباً - الكمية المطلوبة غير متوفرة");

        var transaction = new StockTransaction
        {
            ProductId = dto.ProductId,
            BranchId = dto.BranchId,
            Quantity = dto.Quantity,
            BeforeQuantity = product.StockQuantity,
            MovementType = movementType,
            ReferenceType = StockReferenceType.Adjustment,
            ReasonType = (StockAdjustmentReason?)dto.ReasonType ?? StockAdjustmentReason.ManualCorrection,
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

        transaction.AfterQuantity = product.StockQuantity;

        _context.StockTransactions.Add(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task RegisterInitialStockAsync(int productId, double quantity, int branchId)
    {
        if (branchId <= 0)
            throw new ArgumentException("معرف الفرع ضروري للرصيد الافتتاحي");

        var product = await _context.Products.FindAsync(productId)
            ?? throw new KeyNotFoundException($"المنتج رقم {productId} غير موجود");

        var transaction = new StockTransaction
        {
            ProductId = productId,
            BranchId = branchId,
            Quantity = quantity,
            BeforeQuantity = product.StockQuantity,
            MovementType = StockMovementType.In,
            ReferenceType = StockReferenceType.InitialStock,
            ReasonType = StockAdjustmentReason.ManualCorrection,
            Date = DateTime.UtcNow,
            Notes = "رصيد أول المدة (جرد افتتاحي)",
            CompanyId = (int)_currentUser.CompanyId!,
            UserId = (int)_currentUser.UserId!
        };

        product.StockQuantity += quantity;
        transaction.AfterQuantity = product.StockQuantity;

        _context.StockTransactions.Add(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task ProcessInvoiceStockAsync(int invoiceId, int productId, double quantity, int branchId, bool isSale, string notes)
    {
        var product = await _context.Products.FindAsync(productId)
            ?? throw new KeyNotFoundException($"المنتج رقم {productId} غير موجود");

        var referenceType = StockReferenceType.Invoice;

        // Idempotency Check: التأكد من أن هذه الفاتورة لم تقم بتسجيل حركة مسبقاً لنفس المنتج
        var alreadyProcessed = await _context.StockTransactions.AnyAsync(st => 
            st.ReferenceId == invoiceId && 
            st.ReferenceType == referenceType && 
            st.ProductId == productId);

        if (alreadyProcessed)
            return; // تجاهل الحركة، تم التسجيل مسبقاً للحماية من الـ Retries

        if (isSale && product.StockQuantity - quantity < 0)
            throw new InvalidOperationException($"الكمية المتاحة للمنتج '{product.Name}' غير كافية ({product.StockQuantity})");

        var transaction = new StockTransaction
        {
            ProductId = productId,
            BranchId = branchId,
            Quantity = quantity,
            BeforeQuantity = product.StockQuantity,
            MovementType = isSale ? StockMovementType.Out : StockMovementType.In,
            ReferenceType = referenceType,
            ReferenceId = invoiceId,
            ReasonType = null,
            Date = DateTime.UtcNow,
            Notes = notes,
            CompanyId = (int)_currentUser.CompanyId!,
            UserId = (int)_currentUser.UserId!
        };

        if (isSale)
            product.StockQuantity -= quantity;
        else
            product.StockQuantity += quantity;

        transaction.AfterQuantity = product.StockQuantity;

        _context.StockTransactions.Add(transaction);
        
        // لن نقوم بتشغيل SaveChangesAsync هنا لنتركها لـ Unit of Work في الـ InvoiceService
    }
}
