using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.Inventory;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/product-categories")]
[Authorize]
public class ProductCategoriesController : ControllerBase
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ProductCategoriesController(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ProductCategoryReadDto>>>> GetAll()
    {
        var companyId = _currentUser.CompanyId.Value;
        var categories = await _context.ProductCategories
            .Where(c => c.CompanyId == companyId)
            .Select(c => new ProductCategoryReadDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            })
            .ToListAsync();

        return Ok(ApiResponse<List<ProductCategoryReadDto>>.SuccessResult(categories));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<ProductCategoryReadDto>>> Create(CreateProductCategoryDto dto)
    {
        var companyId = _currentUser.CompanyId.Value;

        var exists = await _context.ProductCategories.AnyAsync(c => c.Name == dto.Name && c.CompanyId == companyId);
        if (exists)
            return BadRequest(ApiResponse<ProductCategoryReadDto>.Failure("اسم التصنيف موجود مسبقاً"));

        var category = new ProductCategory
        {
            Name = dto.Name,
            Description = dto.Description,
            CompanyId = companyId
        };

        _context.ProductCategories.Add(category);
        await _context.SaveChangesAsync();

        var readDto = new ProductCategoryReadDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };

        return CreatedAtAction(nameof(GetAll), null, ApiResponse<ProductCategoryReadDto>.SuccessResult(readDto, "تم إنشاء التصنيف بنجاح"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Warehouse")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var category = await _context.ProductCategories.FindAsync(id);
        if (category == null)
            return NotFound(ApiResponse<object>.Failure("التصنيف غير موجود"));

        // التحقق من عدم الاستخدام
        var isUsed = await _context.Products.AnyAsync(p => p.CategoryId == id);
        if (isUsed)
            return BadRequest(ApiResponse<object>.Failure("لا يمكن حذف التصنيف لاحتوائه على منتجات"));

        category.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.SuccessResult("تم حذف التصنيف بنجاح"));
    }
}
