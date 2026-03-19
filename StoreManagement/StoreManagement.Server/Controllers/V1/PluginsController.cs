using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

[Authorize(Roles = "SuperAdmin,Admin")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/plugins")]
public class PluginsController : ControllerBase
{
    private readonly IPluginService _pluginService;

    public PluginsController(IPluginService pluginService)
    {
        _pluginService = pluginService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PluginReadDto>>>> GetAll()
    {
        var result = await _pluginService.GetAllAsync();
        return Ok(ApiResponse<List<PluginReadDto>>.SuccessResult(result));
    }

    [HttpPost("{id}/toggle")]
    public async Task<ActionResult<ApiResponse<string>>> Toggle(int id, [FromQuery] bool enabled)
    {
        await _pluginService.TogglePluginAsync(id, enabled);
        return Ok(ApiResponse<string>.SuccessResult("تم تحديث حالة الإضافة بنجاح"));
    }
}
