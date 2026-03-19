using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// متحكم إدارة تسويات الحسابات (الخصم والإضافة اليدوي)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class SettlementsController : ControllerBase
{
    private readonly ISettlementService _settlementService;

    public SettlementsController(ISettlementService settlementService)
    {
        _settlementService = settlementService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<SettlementReadDto>>>> GetAll(
        [FromQuery] PaginationQueryDto query,
        [FromQuery] SettlementSource? source,
        [FromQuery] SettlementType? type,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await _settlementService.GetAllAsync(query, source, type, from, to);
        return Ok(ApiResponse<PagedResult<SettlementReadDto>>.SuccessResult(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<SettlementReadDto>>> Create([FromBody] CreateSettlementDto dto)
    {
        var result = await _settlementService.CreateAsync(dto);
        return Ok(ApiResponse<SettlementReadDto>.SuccessResult(result, "تم تسجيل التسوية الحسابية بنجاح"));
    }
}
