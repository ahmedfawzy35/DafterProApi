using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class BootstrapController : ControllerBase
{
    private readonly IBootstrapService _bootstrapService;

    public BootstrapController(IBootstrapService bootstrapService)
    {
        _bootstrapService = bootstrapService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<BootstrapDto>>> Get()
    {
        var data = await _bootstrapService.GetInitialAppDataAsync();
        return Ok(ApiResponse<BootstrapDto>.SuccessResult(data, "تم تحميل بيانات التهيئة الابتدائية بنجاح"));
    }
}
