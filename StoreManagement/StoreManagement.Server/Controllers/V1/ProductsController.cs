using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Entities.Inventory;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// متحكم إدارة المنتجات مع دعم كامل للباركود
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ProductsController> _logger;
    private readonly IBarcodeService _barcodeService;
    private readonly IProductService _productService;
    private readonly IInventoryService _inventoryService;
    private readonly IFileStorageService _fileStorageService;

    public ProductsController(
        StoreDbContext context,
        ICurrentUserService currentUser,
        ILogger<ProductsController> logger,
        IBarcodeService barcodeService,
        IProductService productService,
        IInventoryService inventoryService,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
        _barcodeService = barcodeService;
        _productService = productService;
        _inventoryService = inventoryService;
        _fileStorageService = fileStorageService;
    }

    /// <summary>
    /// استرجاع قائمة المنتجات مع البحث والتصفح
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductReadDto>>>> GetAll(
        [FromQuery] PaginationQueryDto query,
        [FromQuery] bool? isLowStock,
        [FromQuery] int? categoryId,
        [FromQuery] string? brand,
        [FromQuery] bool? isActive)
    {
        var productsQuery = _context.Products
            .Include(p => p.ProductImages)
            .Include(p => p.Category)
            .AsQueryable();

        if (isLowStock == true)
            productsQuery = productsQuery.Where(p => p.StockQuantity <= p.MinimumStock);
            
        if (categoryId.HasValue)
            productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            
        if (!string.IsNullOrWhiteSpace(brand))
            productsQuery = productsQuery.Where(p => p.Brand == brand);
            
        if (isActive.HasValue)
            productsQuery = productsQuery.Where(p => p.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
            productsQuery = productsQuery.Where(p =>
                p.Name.Contains(query.Search) ||
                p.Barcode.Contains(query.Search) ||
                (p.SKU != null && p.SKU.Contains(query.Search))); // دعم البحث بالاسم أو الباركود أو SKU

        var totalCount = await productsQuery.CountAsync();

        var products = await productsQuery
            .OrderBy(p => p.Name)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new ProductReadDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                CostPrice = p.CostPrice,
                StockQuantity = p.StockQuantity,
                MinimumStock = p.MinimumStock,
                ReorderLevel = p.ReorderLevel,
                Unit = p.Unit,
                SKU = p.SKU,
                Description = p.Description,
                Brand = p.Brand,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : null,
                IsActive = p.IsActive,
                IsSellable = p.IsSellable,
                IsPurchasable = p.IsPurchasable,
                ThumbnailUrl = p.ProductImages.Where(i => i.IsThumbnail).Select(i => i.ImageUrl).FirstOrDefault(),
                Barcode = p.Barcode,
                BarcodeType = p.BarcodeType.ToString(),
                BarcodeFormat = p.BarcodeFormat.ToString()
            })
            .ToListAsync();

        var result = new PagedResult<ProductReadDto>
        {
            Items = products,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };

        return Ok(ApiResponse<PagedResult<ProductReadDto>>.SuccessResult(result));
    }

    /// <summary>
    /// استرجاع منتج بواسطة المعرف
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProductReadDto>>> GetById(int id)
    {
        var product = await _context.Products
            .Include(p => p.ProductImages)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return NotFound(ApiResponse<ProductReadDto>.Failure("المنتج غير موجود"));

        return Ok(ApiResponse<ProductReadDto>.SuccessResult(MapToReadDto(product)));
    }

    /// <summary>
    /// البحث عن منتج بالباركود (مُحسَّن للكاشير — مسح سريع)
    /// </summary>
    [HttpGet("by-barcode/{barcode}")]
    public async Task<ActionResult<ApiResponse<ProductReadDto>>> GetByBarcode(string barcode)
    {
        // تأكيد إضافي للأمان (Multi-Tenant) حتى مع وجود Global Filters
        var companyId = _currentUser.CompanyId.Value;
        
        var product = await _context.Products
            .Include(p => p.ProductImages)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Barcode == barcode && p.CompanyId == companyId);

        if (product is null)
            return NotFound(ApiResponse<ProductReadDto>.Failure("لا يوجد منتج بهذا الباركود"));

        return Ok(ApiResponse<ProductReadDto>.SuccessResult(MapToReadDto(product)));
    }

    /// <summary>
    /// إضافة منتج جديد — يُولَّد الباركود تلقائياً إذا لم يُرسَل
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<ProductReadDto>>> Create([FromBody] CreateProductDto dto)
    {
        var companyId = _currentUser.CompanyId.Value;

        // التحقق من تكرار اسم المنتج
        var nameExists = await _context.Products
            .AnyAsync(p => p.Name.ToLower() == dto.Name.ToLower() && p.CompanyId == companyId);

        if (nameExists)
            return BadRequest(ApiResponse<ProductReadDto>.Failure("اسم المنتج مسجل مسبقاً بنفس الشركة"));

        if (!string.IsNullOrWhiteSpace(dto.SKU))
        {
            var skuExists = await _context.Products.AnyAsync(p => p.SKU == dto.SKU && p.CompanyId == companyId);
            if (skuExists)
                return BadRequest(ApiResponse<ProductReadDto>.Failure("رمز SKU مستخدم مسبقاً لمنتج آخر"));
        }

        // تحديد الباركود والصيغة
        string barcode;
        BarcodeType barcodeType;
        BarcodeFormat barcodeFormat;

        if (!string.IsNullOrWhiteSpace(dto.Barcode))
        {
            // باركود مصنعي — تحقق من التكرار أولاً
            var exists = await _context.Products
                .AnyAsync(p => p.Barcode == dto.Barcode);

            if (exists)
                return BadRequest(ApiResponse<ProductReadDto>.Failure("هذا الباركود مستخدم بالفعل"));

            barcode = dto.Barcode;
            barcodeType = BarcodeType.Factory;
            barcodeFormat = _barcodeService.DetectFormat(dto.Barcode);
        }
        else
        {
            // توليد باركود EAN-13 حتمي مع Retry عند التكرار (Concurrency Protection)
            _logger.LogInformation("بدء توليد باركود تلقائي بصيغة EAN-13 للمنتج الجديد التابع للشركة {CompanyId}", companyId);
            barcode = await GenerateUniqueBarcodeAsync(companyId);
            barcodeType = BarcodeType.Generated;
            barcodeFormat = BarcodeFormat.EAN13;
        }

        var product = new Product
        {
            Name = dto.Name,
            Price = dto.Price,
            CostPrice = dto.CostPrice,
            Unit = dto.Unit,
            SKU = dto.SKU,
            Description = dto.Description,
            Brand = dto.Brand,
            CategoryId = dto.CategoryId,
            MinimumStock = dto.MinimumStock,
            ReorderLevel = dto.ReorderLevel,
            IsSellable = dto.IsSellable,
            IsPurchasable = dto.IsPurchasable,
            Barcode = barcode,
            BarcodeType = barcodeType,
            BarcodeFormat = barcodeFormat,
            CompanyId = companyId
        };

        _context.Products.Add(product);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true ||
                                            ex.InnerException?.Message.Contains("unique") == true)
        {
            // Fallback: في حالة التزامن النادرة يُعاد المحاولة مرة أخيرة
            _logger.LogWarning("Duplicate conflict: تعارض في الباركود أثناء الإنشاء للمنتج {Name}. سيتم إعادة محاولة التوليد (Retry).", dto.Name);
            product.Barcode = await GenerateUniqueBarcodeAsync(companyId, seed: 1);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("تم إضافة منتج جديد: {Name}، باركود: {Barcode} ({Type})",
            dto.Name, product.Barcode, product.BarcodeType);

        return CreatedAtAction(nameof(GetById), new { id = product.Id },
            ApiResponse<ProductReadDto>.SuccessResult(MapToReadDto(product), "تم إضافة المنتج بنجاح"));
    }

    /// <summary>
    /// تعديل بيانات منتج — مع قيود تحديث الباركود (Generated → Factory فقط)
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] UpdateProductDto dto)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return NotFound(ApiResponse<object>.Failure("المنتج غير موجود"));

        var companyId = _currentUser.CompanyId.Value;

        // التحقق من تكرار اسم المنتج الجديد
        if (product.Name != dto.Name)
        {
            var nameExists = await _context.Products
                .AnyAsync(p => p.Name.ToLower() == dto.Name.ToLower() && p.CompanyId == companyId && p.Id != id);

            if (nameExists)
                return BadRequest(ApiResponse<object>.Failure("اسم المنتج مسجل مسبقاً بمنتج آخر"));
        }

        // ===== الشيكات على الأكواد عبر ProductService =====
        if (!string.IsNullOrWhiteSpace(dto.Barcode) && dto.Barcode != product.Barcode)
        {
            await _productService.ValidateCanUpdateBarcodeAsync(id, dto.Barcode);

            var oldBarcode = product.Barcode;
            product.Barcode = dto.Barcode;
            product.BarcodeType = BarcodeType.Factory;
            product.BarcodeFormat = dto.BarcodeFormat ?? _barcodeService.DetectFormat(dto.Barcode);

            _logger.LogInformation(
                "تم تحديث باركود المنتج #{Id}: {Old} → {New} (Generated → Factory) بواسطة {User}",
                id, oldBarcode, dto.Barcode, _currentUser.UserId);
        }

        if (!string.IsNullOrWhiteSpace(dto.SKU) && dto.SKU != product.SKU)
        {
            var skuExists = await _context.Products.AnyAsync(p => p.SKU == dto.SKU && p.CompanyId == companyId && p.Id != id);
            if (skuExists)
                return BadRequest(ApiResponse<object>.Failure("رمز SKU مستخدم مسبقاً لمنتج آخر"));
        }

        product.Name = dto.Name;
        product.Price = dto.Price;
        product.CostPrice = dto.CostPrice;
        product.Unit = dto.Unit;
        product.SKU = dto.SKU;
        product.Description = dto.Description;
        product.Brand = dto.Brand;
        product.CategoryId = dto.CategoryId;
        product.MinimumStock = dto.MinimumStock;
        product.ReorderLevel = dto.ReorderLevel;
        product.IsActive = dto.IsActive;
        product.IsSellable = dto.IsSellable;
        product.IsPurchasable = dto.IsPurchasable;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("تم تعديل بيانات المنتج بنجاح"));
    }

    /// <summary>
    /// حذف مؤقت للمنتج
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return NotFound(ApiResponse<object>.Failure("المنتج غير موجود"));

        var usedInInvoices = await _context.InvoiceItems.AnyAsync(i => i.ProductId == id);
        
        if (usedInInvoices)
            return BadRequest(ApiResponse<object>.Failure("لا يمكن حذف المنتج لوجود فواتير مسجلة عليه. تم إيقاف تفعيله."));

        product.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("تم حذف المنتج بنجاح"));
    }

    /// <summary>
    /// بيانات ملصق الطباعة — اسم المنتج، السعر، الباركود (للطباعة في Flutter)
    /// </summary>
    [HttpGet("{id:int}/label")]
    public async Task<ActionResult<ApiResponse<ProductLabelDto>>> GetLabel(int id)
    {
        var companyId = _currentUser.CompanyId.Value;
        
        var companyCurrency = await _context.Companies
            .Where(c => c.Id == companyId)
            .Select(c => c.Currency)
            .FirstOrDefaultAsync();
            
        var currencyStr = companyCurrency?.ToString() ?? "جنيه";
        
        var product = await _context.Products
            .Where(p => p.Id == id && p.CompanyId == companyId)
            .Select(p => new ProductLabelDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Currency = currencyStr,
                Unit = p.Unit,
                Barcode = p.Barcode,
                BarcodeFormat = p.BarcodeFormat.ToString()
            })
            .FirstOrDefaultAsync();

        if (product is null)
            return NotFound(ApiResponse<ProductLabelDto>.Failure("المنتج غير موجود"));

        return Ok(ApiResponse<ProductLabelDto>.SuccessResult(product, "بيانات الملصق"));
    }

    /// <summary>
    /// رفع صورة للمنتج
    /// </summary>
    [HttpPost("{id:int}/image")]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<object>>> UploadImage(int id, IFormFile file, [FromQuery] bool isThumbnail = false)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return NotFound(ApiResponse<object>.Failure("المنتج غير موجود"));

        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Failure("الملف غير صالح"));

        if (file.Length > 2 * 1024 * 1024)
            return BadRequest(ApiResponse<object>.Failure("حجم الصورة يتخطى 2 ميجابايت"));

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        ms.Position = 0;
        
        string fileUrl;
        try
        {
            fileUrl = await _fileStorageService.SaveFileAsync(ms, file.FileName, "products", _currentUser.CompanyId!.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل رفع الصورة للمنتج {ProductId}", id);
            return BadRequest(ApiResponse<object>.Failure("حدث خطأ أثناء حفظ الصورة"));
        }

        var productImage = new ProductImage
        {
            ProductId = id,
            ImageUrl = fileUrl,
            IsThumbnail = isThumbnail
        };

        if (isThumbnail)
        {
            var existingThumbnail = await _context.ProductImages
                .Where(i => i.ProductId == id && i.IsThumbnail)
                .ToListAsync();
            foreach (var img in existingThumbnail) img.IsThumbnail = false;
        }

        _context.ProductImages.Add(productImage);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult(new { id = productImage.Id, url = fileUrl }, "تم رفع الصورة بنجاح"));
    }

    // ===== Helper Methods =====

    private ProductReadDto MapToReadDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price,
        CostPrice = product.CostPrice,
        StockQuantity = product.StockQuantity,
        MinimumStock = product.MinimumStock,
        ReorderLevel = product.ReorderLevel,
        Unit = product.Unit,
        SKU = product.SKU,
        Description = product.Description,
        Brand = product.Brand,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name,
        IsActive = product.IsActive,
        IsSellable = product.IsSellable,
        IsPurchasable = product.IsPurchasable,
        ThumbnailUrl = product.ProductImages?.FirstOrDefault(i => i.IsThumbnail)?.ImageUrl,
        Barcode = product.Barcode,
        BarcodeType = product.BarcodeType.ToString(),
        BarcodeFormat = product.BarcodeFormat.ToString()
    };

    // ===== نقاط النهاية الإضافية (Extra Endpoints) =====

    [HttpGet("{id:int}/summary")]
    public async Task<ActionResult<ApiResponse<ProductSummaryDto>>> GetSummary(int id)
    {
        var companyId = _currentUser.CompanyId.Value;
        
        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

        if (product == null)
            return NotFound(ApiResponse<ProductSummaryDto>.Failure("المنتج غير موجود"));

        var lastTransaction = await _context.StockTransactions
            .Where(st => st.ProductId == id && st.CompanyId == companyId)
            .OrderByDescending(st => st.Date)
            .FirstOrDefaultAsync();

        var summary = new ProductSummaryDto
        {
            ProductId = product.Id,
            Name = product.Name,
            CurrentStock = product.StockQuantity,
            CurrentCost = product.CostPrice,
            CurrentPrice = product.Price,
            LastMovementDate = lastTransaction?.Date,
            CategoryName = product.Category?.Name,
            Brand = product.Brand
        };

        return Ok(ApiResponse<ProductSummaryDto>.SuccessResult(summary));
    }

    [HttpPatch("{id:int}/activate")]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<object>>> Activate(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null) return NotFound(ApiResponse<object>.Failure("المنتج غير موجود"));
        product.IsActive = true;
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResult("تم تفعيل المنتج بنجاح"));
    }

    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null) return NotFound(ApiResponse<object>.Failure("المنتج غير موجود"));
        product.IsActive = false;
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResult("تم إيقاف تفعيل المنتج بنجاح"));
    }

    [HttpGet("{id:int}/stock-history")]
    public async Task<ActionResult<ApiResponse<PagedResult<StockTransactionReadDto>>>> GetStockHistory(
        int id, [FromQuery] PaginationQueryDto query, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var result = await _inventoryService.GetHistoryAsync(query, id, from, to);
        return Ok(ApiResponse<PagedResult<StockTransactionReadDto>>.SuccessResult(result));
    }
    
    [HttpGet("low-stock")]
    public async Task<ActionResult<ApiResponse<List<ProductReadDto>>>> GetLowStock()
    {
        var companyId = _currentUser.CompanyId.Value;
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .Where(p => p.CompanyId == companyId && p.IsActive && p.StockQuantity <= p.MinimumStock)
            .Select(p => MapToReadDto(p))
            .ToListAsync();
            
        return Ok(ApiResponse<List<ProductReadDto>>.SuccessResult(products));
    }
    
    [HttpGet("reorder-needed")]
    public async Task<ActionResult<ApiResponse<List<ProductReadDto>>>> GetReorderNeeded()
    {
        var companyId = _currentUser.CompanyId.Value;
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .Where(p => p.CompanyId == companyId && p.IsActive && p.StockQuantity <= p.ReorderLevel)
            .Select(p => MapToReadDto(p))
            .ToListAsync();
            
        return Ok(ApiResponse<List<ProductReadDto>>.SuccessResult(products));
    }

    /// <summary>
    /// يولّد باركود EAN-13 فريداً مع إعادة المحاولة عند التكرار (Concurrency Protection)
    /// </summary>
    private async Task<string> GenerateUniqueBarcodeAsync(int companyId, int seed = 0)
    {
        const int maxRetries = 5;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            // الـ sequence يُبنى من: عدد المنتجات + الوقت الحالي + seed للإزاحة عند التجربة
            var baseSequence = await _context.Products
                .IgnoreQueryFilters()
                .CountAsync(p => p.CompanyId == companyId);

            var sequence = (long)(baseSequence + attempt + seed + DateTime.UtcNow.Ticks % 100);
            var candidate = _barcodeService.GenerateEan13(companyId, sequence);

            var exists = await _context.Products.AnyAsync(p => p.Barcode == candidate && p.CompanyId == companyId);
            if (!exists) 
            {
                _logger.LogInformation("تم توليد باركود تلقائي بنجاح. المحاولة: {Attempt}, التسلسل: {Sequence}", attempt + 1, sequence);
                return candidate;
            }
            
            _logger.LogWarning("الباركود المولد {Barcode} مكرر محليًا، إعادة المحاولة (Attempt: {Attempt})...", candidate, attempt + 1);
        }

        // Fallback نهائي باستخدام Ticks للضمان الكامل
        _logger.LogWarning("تم استنفاد محاولات التوليد، استخدام Fallback نهائي...");
        return _barcodeService.GenerateEan13(companyId, DateTime.UtcNow.Ticks % 100000);
    }
}


