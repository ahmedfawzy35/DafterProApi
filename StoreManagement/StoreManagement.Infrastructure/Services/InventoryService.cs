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
    private readonly IBranchInventoryService _branchInventory;

    public InventoryService(StoreDbContext context, ICurrentUserService currentUser, IBranchInventoryService branchInventory)
    {
        _context = context;
        _currentUser = currentUser;
        _branchInventory = branchInventory;
    }

    // =========================================================================
    // سجل حركات المخزون
    // =========================================================================

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
            baseQuery = baseQuery.Where(st =>
                (st.Notes != null && st.Notes.Contains(query.Search)) ||
                st.Product.Name.Contains(query.Search));

        var company = await _context.Companies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == _currentUser.CompanyId);
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
                ReasonType = st.ReasonType != null ? st.ReasonType.ToString() : null,
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

    // =========================================================================
    // التسويات (Adjustments)
    // =========================================================================

    public async Task<StockAdjustmentReadDto> CreateStockAdjustmentAsync(CreateStockAdjustmentDto dto)
    {
        // --- Validation ---
        if (dto.BranchId <= 0)
            throw new ArgumentException("معرف الفرع ضروري لأي تسوية مخزون.");

        if (dto.Items == null || dto.Items.Count == 0)
            throw new ArgumentException("يجب أن يحتوي مستند التسوية على عنصر واحد على الأقل.");

        // التحقق من عدم وجود كميات صفرية
        if (dto.Items.Any(i => i.Quantity == 0))
            throw new ArgumentException("لا يمكن أن تكون كمية أي عنصر صفراً. استخدم قيمة موجبة للإضافة أو سالبة للخصم.");

        // جلب كل المنتجات المعنية دفعة واحدة (Pre-Validation)
        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var item in dto.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                throw new KeyNotFoundException($"المنتج رقم {item.ProductId} غير موجود.");

            // سياسة strict: لا يُسمح بمخزون سالب
            if (item.Quantity < 0)
            {
                var availableQty = await _branchInventory.GetAvailableQtyAsync(item.ProductId, dto.BranchId);
                if (availableQty + item.Quantity < 0)
                {
                    throw new InvalidOperationException(
                        $"التسوية ستؤدي إلى مخزون سالب للمنتج '{product.Name}' في هذا الفرع. " +
                        $"الرصيد الحالي للفرع: {availableQty}، الكمية المخصومة: {Math.Abs(item.Quantity)}.");
                }
            }
        }

        // --- Execution inside DB Transaction ---
        await using var dbTx = await _context.Database.BeginTransactionAsync();
        try
        {
            var adjustment = new StockAdjustment
            {
                BranchId = dto.BranchId,
                CompanyId = (int)_currentUser.CompanyId!,
                Date = DateTime.UtcNow,
                Notes = dto.Notes,
                UserId = (int)_currentUser.UserId!
            };

            _context.StockAdjustments.Add(adjustment);
            await _context.SaveChangesAsync(); // نحتاج Id للـ ReferenceId

            foreach (var itemDto in dto.Items)
            {
                var product = products[itemDto.ProductId];
                var adjustmentItem = new StockAdjustmentItem
                {
                    StockAdjustmentId = adjustment.Id,
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    ReasonType = (StockAdjustmentReason)itemDto.ReasonType
                };
                _context.StockAdjustmentItems.Add(adjustmentItem);

                // إنشاء StockTransaction لكل عنصر
                var isAdding = itemDto.Quantity > 0;
                var absQty = Math.Abs(itemDto.Quantity);

                // جلب رصيد الفرع قبل العملية
                var beforeQty = await _branchInventory.GetAvailableQtyAsync(itemDto.ProductId, dto.BranchId);

                var stockTx = new StockTransaction
                {
                    ProductId = itemDto.ProductId,
                    BranchId = dto.BranchId,
                    Quantity = absQty,
                    BeforeQuantity = beforeQty,
                    MovementType = isAdding ? StockMovementType.In : StockMovementType.Out,
                    ReferenceType = StockReferenceType.Adjustment,
                    ReferenceId = adjustment.Id,
                    ReasonType = (StockAdjustmentReason)itemDto.ReasonType,
                    Date = DateTime.UtcNow,
                    Notes = dto.Notes,
                    CompanyId = (int)_currentUser.CompanyId!,
                    UserId = (int)_currentUser.UserId!
                };

                // تعديل الرصيد عبر الخدمة المتخصصة
                if (isAdding)
                    await _branchInventory.IncreaseStockAsync(itemDto.ProductId, dto.BranchId, absQty);
                else
                    await _branchInventory.DecreaseStockAsync(itemDto.ProductId, dto.BranchId, absQty, allowNegative: false);

                stockTx.AfterQuantity = beforeQty + itemDto.Quantity;

                _context.StockTransactions.Add(stockTx);
            }

            await _context.SaveChangesAsync();
            await dbTx.CommitAsync();

            // إعادة المستند كاملاً
            return await GetAdjustmentByIdAsync(adjustment.Id)
                   ?? throw new InvalidOperationException("تعذّر إعادة بيانات التسوية بعد الحفظ.");
        }
        catch
        {
            await dbTx.RollbackAsync();
            throw;
        }
    }

    public async Task<StockAdjustmentReadDto?> GetAdjustmentByIdAsync(int id)
    {
        var adjustment = await _context.StockAdjustments
            .Include(a => a.User)
            .Include(a => a.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (adjustment == null) return null;

        return new StockAdjustmentReadDto
        {
            Id = adjustment.Id,
            BranchId = adjustment.BranchId,
            Date = adjustment.Date,
            Notes = adjustment.Notes,
            UserName = adjustment.User.UserName ?? "Unknown",
            Items = adjustment.Items.Select(i => new StockAdjustmentItemReadDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                ReasonType = i.ReasonType.ToString()
            }).ToList()
        };
    }

    public async Task<PagedResult<StockAdjustmentReadDto>> GetAllAdjustmentsAsync(PaginationQueryDto query)
    {
        var baseQuery = _context.StockAdjustments
            .Include(a => a.User)
            .Include(a => a.Items)
                .ThenInclude(i => i.Product)
            .Where(a => a.CompanyId == _currentUser.CompanyId);

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(a => a.Notes != null && a.Notes.Contains(query.Search));

        var total = await baseQuery.CountAsync();
        var items = await baseQuery
            .OrderByDescending(a => a.Date)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<StockAdjustmentReadDto>
        {
            Items = items.Select(a => new StockAdjustmentReadDto
            {
                Id = a.Id,
                BranchId = a.BranchId,
                Date = a.Date,
                Notes = a.Notes,
                UserName = a.User.UserName ?? "Unknown",
                Items = a.Items.Select(i => new StockAdjustmentItemReadDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    Quantity = i.Quantity,
                    ReasonType = i.ReasonType.ToString()
                }).ToList()
            }).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    // =========================================================================
    // التحويلات بين الفروع (Transfers)
    // =========================================================================

    public async Task<StockTransferReadDto> CreateStockTransferAsync(CreateStockTransferDto dto)
    {
        // --- Validation ---
        if (dto.FromBranchId <= 0 || dto.ToBranchId <= 0)
            throw new ArgumentException("معرفا الفرع المرسِل والمستقبِل ضروريان.");

        if (dto.FromBranchId == dto.ToBranchId)
            throw new ArgumentException("لا يمكن إجراء تحويل من الفرع لنفسه. يجب أن يكون الفرع المرسِل مختلفاً عن الفرع المستقبِل.");

        if (dto.Items == null || dto.Items.Count == 0)
            throw new ArgumentException("يجب أن يحتوي مستند التحويل على عنصر واحد على الأقل.");

        if (dto.Items.Any(i => i.Quantity <= 0))
            throw new ArgumentException("كميات التحويل يجب أن تكون موجبة دائماً.");

        // Pre-Validation لكل المنتجات دفعة واحدة قبل التنفيذ
        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var stockErrors = new List<string>();
        foreach (var item in dto.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                stockErrors.Add($"المنتج رقم {item.ProductId} غير موجود.");
                continue;
            }
            var availableQty = await _branchInventory.GetAvailableQtyAsync(item.ProductId, dto.FromBranchId);
            if (availableQty < item.Quantity)
                stockErrors.Add(
                    $"المنتج '{product.Name}': الكمية المطلوبة ({item.Quantity}) تتجاوز الرصيد المتاح بالفرع المرسِل ({availableQty}).");
        }

        if (stockErrors.Count > 0)
            throw new InvalidOperationException(string.Join("\n", stockErrors));

        // --- Execution inside DB Transaction ---
        await using var dbTx = await _context.Database.BeginTransactionAsync();
        try
        {
            var transfer = new StockTransfer
            {
                CompanyId = (int)_currentUser.CompanyId!,
                FromBranchId = dto.FromBranchId,
                ToBranchId = dto.ToBranchId,
                Date = DateTime.UtcNow,
                Notes = dto.Notes,
                UserId = (int)_currentUser.UserId!
            };

            _context.StockTransfers.Add(transfer);
            await _context.SaveChangesAsync(); // نحتاج Id للـ ReferenceId

            foreach (var itemDto in dto.Items)
            {
                var product = products[itemDto.ProductId];

                _context.StockTransferItems.Add(new StockTransferItem
                {
                    StockTransferId = transfer.Id,
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity
                });

                var beforeFromQty = await _branchInventory.GetAvailableQtyAsync(itemDto.ProductId, dto.FromBranchId);
                var beforeToQty = await _branchInventory.GetAvailableQtyAsync(itemDto.ProductId, dto.ToBranchId);

                // إتمام النقل الفعلي بين الفروع عبر الـ Service لضمان كل الـ guards
                await _branchInventory.DecreaseStockAsync(itemDto.ProductId, dto.FromBranchId, itemDto.Quantity, allowNegative: false);
                await _branchInventory.IncreaseStockAsync(itemDto.ProductId, dto.ToBranchId, itemDto.Quantity);

                // حركة الخروج من الفرع المرسِل
                var outTx = new StockTransaction
                {
                    ProductId = itemDto.ProductId,
                    BranchId = dto.FromBranchId,
                    Quantity = itemDto.Quantity,
                    BeforeQuantity = beforeFromQty,
                    MovementType = StockMovementType.TransferOut,
                    ReferenceType = StockReferenceType.Transfer,
                    ReferenceId = transfer.Id,
                    Date = DateTime.UtcNow,
                    Notes = $"تحويل صادر إلى الفرع {dto.ToBranchId}. {dto.Notes}".Trim(),
                    CompanyId = (int)_currentUser.CompanyId!,
                    UserId = (int)_currentUser.UserId!
                };
                outTx.AfterQuantity = beforeFromQty - itemDto.Quantity;
                _context.StockTransactions.Add(outTx);

                // حركة الدخول إلى الفرع المستقبِل
                var inTx = new StockTransaction
                {
                    ProductId = itemDto.ProductId,
                    BranchId = dto.ToBranchId,
                    Quantity = itemDto.Quantity,
                    BeforeQuantity = beforeToQty,
                    MovementType = StockMovementType.TransferIn,
                    ReferenceType = StockReferenceType.Transfer,
                    ReferenceId = transfer.Id,
                    Date = DateTime.UtcNow,
                    Notes = $"تحويل وارد من الفرع {dto.FromBranchId}. {dto.Notes}".Trim(),
                    CompanyId = (int)_currentUser.CompanyId!,
                    UserId = (int)_currentUser.UserId!
                };
                inTx.AfterQuantity = beforeToQty + itemDto.Quantity;
                _context.StockTransactions.Add(inTx);
            }

            await _context.SaveChangesAsync();
            await dbTx.CommitAsync();

            return await GetTransferByIdAsync(transfer.Id)
                   ?? throw new InvalidOperationException("تعذّر إعادة بيانات التحويل بعد الحفظ.");
        }
        catch
        {
            await dbTx.RollbackAsync();
            throw;
        }
    }

    public async Task<StockTransferReadDto?> GetTransferByIdAsync(int id)
    {
        var transfer = await _context.StockTransfers
            .Include(t => t.User)
            .Include(t => t.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transfer == null) return null;

        return new StockTransferReadDto
        {
            Id = transfer.Id,
            FromBranchId = transfer.FromBranchId,
            ToBranchId = transfer.ToBranchId,
            Date = transfer.Date,
            Notes = transfer.Notes,
            UserName = transfer.User.UserName ?? "Unknown",
            Items = transfer.Items.Select(i => new StockTransferItemReadDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Quantity = i.Quantity
            }).ToList()
        };
    }

    public async Task<PagedResult<StockTransferReadDto>> GetAllTransfersAsync(PaginationQueryDto query)
    {
        var baseQuery = _context.StockTransfers
            .Include(t => t.User)
            .Include(t => t.Items)
                .ThenInclude(i => i.Product)
            .Where(t => t.CompanyId == _currentUser.CompanyId);

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(t => t.Notes != null && t.Notes.Contains(query.Search));

        var total = await baseQuery.CountAsync();
        var items = await baseQuery
            .OrderByDescending(t => t.Date)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<StockTransferReadDto>
        {
            Items = items.Select(t => new StockTransferReadDto
            {
                Id = t.Id,
                FromBranchId = t.FromBranchId,
                ToBranchId = t.ToBranchId,
                Date = t.Date,
                Notes = t.Notes,
                UserName = t.User.UserName ?? "Unknown",
                Items = t.Items.Select(i => new StockTransferItemReadDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    Quantity = i.Quantity
                }).ToList()
            }).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    // =========================================================================
    // Internal — يُستخدم من InvoiceService فقط
    // =========================================================================

    public async Task RegisterInitialStockAsync(int productId, double quantity, int branchId)
    {
        if (branchId <= 0)
            throw new ArgumentException("معرف الفرع ضروري للرصيد الافتتاحي.");

        var product = await _context.Products.FindAsync(productId)
            ?? throw new KeyNotFoundException($"المنتج رقم {productId} غير موجود.");

        var beforeQty = await _branchInventory.GetAvailableQtyAsync(productId, branchId);

        var transaction = new StockTransaction
        {
            ProductId = productId,
            BranchId = branchId,
            Quantity = quantity,
            BeforeQuantity = beforeQty,
            MovementType = StockMovementType.In,
            ReferenceType = StockReferenceType.InitialStock,
            ReasonType = StockAdjustmentReason.ManualCorrection,
            Date = DateTime.UtcNow,
            Notes = "رصيد أول المدة (جرد افتتاحي)",
            CompanyId = (int)_currentUser.CompanyId!,
            UserId = (int)_currentUser.UserId!
        };

        await _branchInventory.IncreaseStockAsync(productId, branchId, quantity);
        transaction.AfterQuantity = beforeQty + quantity;

        _context.StockTransactions.Add(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task ProcessInvoiceStockAsync(
        int invoiceId, int invoiceItemId, int productId,
        double quantity, int branchId, InvoiceType invoiceType, string notes)
    {
        if (branchId <= 0)
            throw new ArgumentException("معرف الفرع ضروري لأي حركة مخزون.");

        var product = await _context.Products.FindAsync(productId)
            ?? throw new KeyNotFoundException($"المنتج رقم {productId} غير موجود.");

        var referenceType = StockReferenceType.Invoice;

        // Idempotency Check
        var alreadyProcessed = await _context.StockTransactions.AnyAsync(st =>
            st.ReferenceId == invoiceId &&
            st.InvoiceItemId == invoiceItemId &&
            st.ReferenceType == referenceType &&
            st.ProductId == productId);

        if (alreadyProcessed) return;

        var isDeducting = invoiceType == InvoiceType.Sale || invoiceType == InvoiceType.PurchaseReturn;

        var availableQty = await _branchInventory.GetAvailableQtyAsync(productId, branchId);

        if (isDeducting && availableQty - quantity < 0)
            throw new InvalidOperationException(
                $"الكمية المتاحة للمنتج '{product.Name}' في الفرع الحالي غير كافية ({availableQty}).");

        var transaction = new StockTransaction
        {
            ProductId = productId,
            BranchId = branchId,
            Quantity = quantity,
            BeforeQuantity = availableQty,
            MovementType = isDeducting ? StockMovementType.Out : StockMovementType.In,
            ReferenceType = referenceType,
            ReferenceId = invoiceId,
            InvoiceItemId = invoiceItemId,
            ReasonType = null,
            Date = DateTime.UtcNow,
            Notes = notes,
            CompanyId = (int)_currentUser.CompanyId!,
            UserId = (int)_currentUser.UserId!
        };

        if (isDeducting)
            await _branchInventory.DecreaseStockAsync(productId, branchId, quantity, allowNegative: false);
        else
            await _branchInventory.IncreaseStockAsync(productId, branchId, quantity);

        transaction.AfterQuantity = isDeducting ? availableQty - quantity : availableQty + quantity;

        _context.StockTransactions.Add(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task ReverseInvoiceStockAsync(
        int invoiceId, int invoiceItemId, int productId,
        double quantity, int branchId, InvoiceType originalInvoiceType, string notes)
    {
        if (branchId <= 0)
            throw new ArgumentException("معرف الفرع ضروري لأي حركة مخزون.");

        var product = await _context.Products.FindAsync(productId)
            ?? throw new KeyNotFoundException($"المنتج رقم {productId} غير موجود.");

        var referenceType = StockReferenceType.Cancellation;

        // Idempotency Check
        var alreadyReversed = await _context.StockTransactions.AnyAsync(st =>
            st.ReferenceId == invoiceId &&
            st.InvoiceItemId == invoiceItemId &&
            st.ReferenceType == referenceType &&
            st.ProductId == productId);

        if (alreadyReversed) return;

        // عكس الحركة الأصلية
        var originalDeducted = originalInvoiceType == InvoiceType.Sale || originalInvoiceType == InvoiceType.PurchaseReturn;
        var isDeductingNow = !originalDeducted;

        var availableQty = await _branchInventory.GetAvailableQtyAsync(productId, branchId);

        if (isDeductingNow && availableQty - quantity < 0)
            throw new InvalidOperationException(
                $"الكمية المتاحة للمنتج '{product.Name}' بالفرع الحالي غير كافية ({availableQty}) لإتمام الإلغاء.");

        var transaction = new StockTransaction
        {
            ProductId = productId,
            BranchId = branchId,
            Quantity = quantity,
            BeforeQuantity = availableQty,
            MovementType = isDeductingNow ? StockMovementType.Out : StockMovementType.In,
            ReferenceType = referenceType,
            ReferenceId = invoiceId,
            InvoiceItemId = invoiceItemId,
            ReasonType = null,
            Date = DateTime.UtcNow,
            Notes = notes,
            CompanyId = (int)_currentUser.CompanyId!,
            UserId = (int)_currentUser.UserId!
        };

        if (isDeductingNow)
            await _branchInventory.DecreaseStockAsync(productId, branchId, quantity, allowNegative: false);
        else
            await _branchInventory.IncreaseStockAsync(productId, branchId, quantity);

        transaction.AfterQuantity = isDeductingNow ? availableQty - quantity : availableQty + quantity;

        _context.StockTransactions.Add(transaction);
        await _context.SaveChangesAsync();
    }
}
