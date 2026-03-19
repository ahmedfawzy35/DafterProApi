using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// متحكم إدارة بيانات الشركة الحالية
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompanyController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<CompanyReadDto>>> GetMyCompany([FromQuery] bool includeLogo = false)
    {
        var result = await _companyService.GetMyCompanyAsync(includeLogo);
        return Ok(ApiResponse<CompanyReadDto>.SuccessResult(result));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<ApiResponse<CompanyReadDto>>> Create([FromBody] CompanyCreateDto dto)
    {
        var result = await _companyService.CreateAsync(dto);
        return Ok(ApiResponse<CompanyReadDto>.SuccessResult(result, "تم إنشاء الشركة بنجاح"));
    }

    [HttpPut("my")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateMyCompany([FromBody] CompanyUpdateDto dto)
    {
        await _companyService.UpdateMyCompanyAsync(dto);
        return Ok(ApiResponse<object>.SuccessResult("تم تحديث بيانات الشركة بنجاح"));
    }

    [HttpPost("my/logo")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> UploadLogo(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Failure("الملف غير صالح"));

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        await _companyService.UploadLogoAsync(ms.ToArray(), file.ContentType);

        return Ok(ApiResponse<object>.SuccessResult("تم رفع اللوجو بنجاح"));
    }
}
