using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

[Authorize(Roles = "Admin")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/policies")]
public class PoliciesController : ControllerBase
{
    private readonly IPolicyService _policyService;

    public PoliciesController(IPolicyService policyService)
    {
        _policyService = policyService;
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<ApiResponse<string>>> GetValue(string key)
    {
        var result = await _policyService.GetPolicyValueAsync(key);
        return Ok(ApiResponse<string>.SuccessResult(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<string>>> SetValue([FromQuery] string key, [FromQuery] string value, [FromQuery] PolicyDataType dataType)
    {
        await _policyService.SetPolicyValueAsync(key, value, dataType);
        return Ok(ApiResponse<string>.SuccessResult("تم تحديث السياسة بنجاح"));
    }

    [HttpPost("seed")]
    public async Task<ActionResult<ApiResponse<string>>> SeedDefaults([FromQuery] int companyId)
    {
        await _policyService.SeedDefaultPoliciesAsync(companyId);
        return Ok(ApiResponse<string>.SuccessResult("تم تهيئة السياسات الافتراضية بنجاح"));
    }
}
