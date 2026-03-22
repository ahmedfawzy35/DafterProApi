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
    private readonly ICurrentUserService _currentUser;
    private readonly ISubscriptionService _subscriptionService;

    public CompanyController(
        ICompanyService companyService,
        ICurrentUserService currentUser,
        ISubscriptionService subscriptionService)
    {
        _companyService = companyService;
        _currentUser = currentUser;
        _subscriptionService = subscriptionService;
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CompanyReadDto>>>> GetAll()
    {
        var result = await _companyService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<CompanyReadDto>>.SuccessResult(result));
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<CompanyReadDto>>> GetMyCompany([FromQuery] bool includeLogo = false)
    {
        var result = await _companyService.GetMyCompanyAsync(includeLogo);
        if (result == null)
            return NotFound(ApiResponse<CompanyReadDto>.Failure("الشركة غير موجودة أو المستخدم غير مرتبط بشركة"));
            
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

    // ===== الحصول على مستخدمي الشركة =====
    [HttpGet("my/users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<UserReadDto>>>> GetMyCompanyUsers()
    {
        if (!_currentUser.CompanyId.HasValue)
            return BadRequest(ApiResponse<List<UserReadDto>>.Failure("المستخدم غير مرتبط بشركة"));

        var result = await _companyService.GetCompanyUsersAsync((int)_currentUser.CompanyId);
        return Ok(ApiResponse<List<UserReadDto>>.SuccessResult(result));
    }

    [HttpGet("{id:int}/users")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<List<UserReadDto>>>> GetCompanyUsers(int id)
    {
        var result = await _companyService.GetCompanyUsersAsync(id);
        return Ok(ApiResponse<List<UserReadDto>>.SuccessResult(result));
    }

    // ===== الحصول على حالة اشتراك الشركة (Subscriptions & Features) =====
    [HttpGet("my/subscription")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<SubscriptionStatusDto>>> GetMySubscription()
    {
        if (!_currentUser.CompanyId.HasValue)
            return BadRequest(ApiResponse<SubscriptionStatusDto>.Failure("المستخدم غير مرتبط بشركة"));

        var result = await _subscriptionService.GetSubscriptionStatusAsync((int)_currentUser.CompanyId);
        if (result == null)
            return NotFound(ApiResponse<SubscriptionStatusDto>.Failure("الشركة لا تملك اشتراكاً نشطاً"));

        return Ok(ApiResponse<SubscriptionStatusDto>.SuccessResult(result));
    }

    [HttpGet("{id:int}/subscription")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<SubscriptionStatusDto>>> GetCompanySubscription(int id)
    {
        var result = await _subscriptionService.GetSubscriptionStatusAsync(id);
        if (result == null)
            return NotFound(ApiResponse<SubscriptionStatusDto>.Failure("الشركة لا تملك اشتراكاً نشطاً أو غير موجودة"));

        return Ok(ApiResponse<SubscriptionStatusDto>.SuccessResult(result));
    }
}
