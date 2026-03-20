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
[Route("api/v{version:apiVersion}/payroll")]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _payrollService;

    public PayrollController(IPayrollService payrollService)
    {
        _payrollService = payrollService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PayrollRunReadDto>>>> GetAll([FromQuery] int month, [FromQuery] int year)
    {
        var result = await _payrollService.GetPayrollRunsAsync(month, year);
        return Ok(ApiResponse<List<PayrollRunReadDto>>.SuccessResult(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PayrollRunDetailsDto>>> GetDetails(int id)
    {
        var result = await _payrollService.GetPayrollDetailsAsync(id);
        return Ok(ApiResponse<PayrollRunDetailsDto>.SuccessResult(result));
    }

    [HttpPost("generate")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<string>>> Generate([FromQuery] int month, [FromQuery] int year, [FromBody] List<int>? employeeIds = null)
    {
        await _payrollService.GeneratePayrollRunAsync(month, year, employeeIds);
        return Ok(ApiResponse<string>.SuccessResult("تم إنشاء/تحديث تشغيل الرواتب بنجاح"));
    }

    [HttpPost("lock-and-pay")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<string>>> LockAndPay([FromQuery] int month, [FromQuery] int year)
    {
        await _payrollService.LockAndPayPayrollAsync(month, year);
        return Ok(ApiResponse<string>.SuccessResult("تم اعتماد وصرف الرواتب بنجاح وتحديث الصندوق"));
    }
}
