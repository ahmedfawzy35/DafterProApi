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
    public async Task<ActionResult<ApiResponse<List<PayrollReadDto>>>> GetAll([FromQuery] DateTime month)
    {
        var result = await _payrollService.GetAllAsync(month);
        return Ok(ApiResponse<List<PayrollReadDto>>.SuccessResult(result));
    }

    [HttpPost("generate")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<string>>> Generate([FromBody] CreatePayrollDto dto)
    {
        await _payrollService.GeneratePayrollAsync(dto);
        return Ok(ApiResponse<string>.SuccessResult("تم إنشاء مسير الرواتب بنجاح"));
    }

    [HttpPost("{id}/pay")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<string>>> Pay(int id)
    {
        await _payrollService.PaySalaryAsync(id);
        return Ok(ApiResponse<string>.SuccessResult("تم صرف الراتب بنجاح وتحديث الصندوق"));
    }
}
