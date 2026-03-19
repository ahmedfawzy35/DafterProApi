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
[Route("api/v{version:apiVersion}/audit-logs")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogReadDto>>>> GetAll(
        [FromQuery] PaginationQueryDto query, [FromQuery] string? entityName, [FromQuery] int? userId)
    {
        var result = await _auditLogService.GetAllAsync(query, entityName, userId);
        return Ok(ApiResponse<PagedResult<AuditLogReadDto>>.SuccessResult(result));
    }
}
