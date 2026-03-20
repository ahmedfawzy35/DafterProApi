using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/attendance")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost("record")]
    public async Task<ActionResult<ApiResponse<string>>> Record([FromBody] AttendanceCreateDto dto)
    {
        await _attendanceService.RecordAttendanceAsync(dto);
        return Ok(ApiResponse<string>.SuccessResult("تم تسجيل الحضور بنجاح"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AttendanceReadDto>>>> GetAll([FromQuery] DateTime date)
    {
        var result = await _attendanceService.GetAllAsync(date);
        return Ok(ApiResponse<List<AttendanceReadDto>>.SuccessResult(result));
    }

    [HttpGet("employee/{employeeId}")]
    public async Task<ActionResult<ApiResponse<List<AttendanceReadDto>>>> GetEmployeeAttendance(int employeeId, [FromQuery] int month, [FromQuery] int year)
    {
        var result = await _attendanceService.GetEmployeeAttendanceAsync(employeeId, month, year);
        return Ok(ApiResponse<List<AttendanceReadDto>>.SuccessResult(result));
    }
}
