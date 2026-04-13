using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ShiftsController : ControllerBase
{
    private readonly IShiftService _shiftService;

    public ShiftsController(IShiftService shiftService)
    {
        _shiftService = shiftService;
    }

    [HttpPost("open")]
    [Authorize(Policy = "RequirePermission:sales.view")] // يمكن تعديل الصلاحية حسب هيكل الصلاحيات
    public async Task<ActionResult<ShiftReadDto>> OpenShift(OpenShiftDto dto)
    {
        var result = await _shiftService.OpenShiftAsync(dto);
        return Ok(result);
    }

    [HttpPost("{id}/close")]
    [Authorize(Policy = "RequirePermission:sales.view")]
    public async Task<ActionResult<ShiftReadDto>> CloseShift(int id, CloseShiftDto dto)
    {
        var result = await _shiftService.CloseShiftAsync(id, dto);
        return Ok(result);
    }

    [HttpGet("current")]
    public async Task<ActionResult<ShiftReadDto>> GetCurrentShift()
    {
        var shift = await _shiftService.GetCurrentShiftAsync();
        if (shift == null) return NotFound("لا توجد وردية مفتوحة حالياً.");
        return Ok(shift);
    }

    [HttpGet]
    [Authorize(Policy = "RequirePermission:sales.view")]
    public async Task<ActionResult<PagedResult<ShiftReadDto>>> GetAllShifts([FromQuery] PaginationQueryDto query)
    {
        var result = await _shiftService.GetAllShiftsAsync(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "RequirePermission:sales.view")]
    public async Task<ActionResult<ShiftReadDto>> GetShiftById(int id)
    {
        var result = await _shiftService.GetShiftByIdAsync(id);
        return Ok(result);
    }
}
