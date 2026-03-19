using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// متحكم إدارة المنتجات
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

    public ProductsController(
        StoreDbContext context,
        ICurrentUserService currentUser,
        ILogger<ProductsController> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// استرجاع قائمة المنتجات مع البحث والتصفح
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductReadDto>>>> GetAll(
        [FromQuery] PaginationQueryDto query,
        [FromQuery] bool? isLowStock)
    {
        var productsQuery = _context.Products
            .Include(p => p.ProductImages)
            .AsQueryable();

        if (isLowStock == true)
            productsQuery = productsQuery.Where(p => p.StockQuantity <= 5); // حد افتراضي للنقص

        if (!string.IsNullOrWhiteSpace(query.Search))
            productsQuery = productsQuery.Where(p => p.Name.Contains(query.Search));

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
                Unit = p.Unit,
                ThumbnailUrl = p.ProductImages.Where(i => i.IsThumbnail).Select(i => i.ImageUrl).FirstOrDefault()
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
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return NotFound(ApiResponse<ProductReadDto>.Failure("المنتج غير موجود"));

        return Ok(ApiResponse<ProductReadDto>.SuccessResult(new ProductReadDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            CostPrice = product.CostPrice,
            StockQuantity = product.StockQuantity,
            Unit = product.Unit,
            ThumbnailUrl = product.ProductImages.FirstOrDefault(i => i.IsThumbnail)?.ImageUrl
        }));
    }

    /// <summary>
    /// إضافة منتج جديد
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<ProductReadDto>>> Create([FromBody] CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Price = dto.Price,
            CostPrice = dto.CostPrice,
            Unit = dto.Unit,
            CompanyId = _currentUser.CompanyId
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("تم إضافة منتج جديد: {Name}", dto.Name);

        return CreatedAtAction(nameof(GetById), new { id = product.Id },
            ApiResponse<ProductReadDto>.SuccessResult(new ProductReadDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                CostPrice = product.CostPrice,
                StockQuantity = product.StockQuantity,
                Unit = product.Unit
            }, "تم إضافة المنتج بنجاح"));
    }

    /// <summary>
    /// تعديل بيانات منتج
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] UpdateProductDto dto)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return NotFound(ApiResponse<object>.Failure("المنتج غير موجود"));

        product.Name = dto.Name;
        product.Price = dto.Price;
        product.CostPrice = dto.CostPrice;
        product.Unit = dto.Unit;

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

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("تم حذف المنتج بنجاح"));
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
        var base64 = Convert.ToBase64String(ms.ToArray());
        var dataUrl = $"data:{file.ContentType};base64,{base64}";

        var productImage = new ProductImage
        {
            ProductId = id,
            ImageUrl = dataUrl,
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

        return Ok(ApiResponse<object>.SuccessResult(new { id = productImage.Id, url = dataUrl }, "تم رفع الصورة بنجاح"));
    }
}
