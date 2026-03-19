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
    public async Task<ActionResult<ApiResponse<CompanyReadDto>>> GetMyCompany()
    {
        var result = await _companyService.GetMyCompanyAsync();
        return Ok(ApiResponse<CompanyReadDto>.SuccessResult(result));
    }

    [HttpPut("my")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateMyCompany([FromBody] CompanyUpdateDto dto)
    {
        await _companyService.UpdateMyCompanyAsync(dto);
        return Ok(ApiResponse<object>.SuccessResult("تم تحديث بيانات الشركة بنجاح"));
    }
}
