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
[Route("api/v{version:apiVersion}/loans")]
public class LoansController : ControllerBase
{
    private readonly ILoanService _loanService;

    public LoansController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<LoanReadDto>>> Create([FromBody] CreateLoanDto dto)
    {
        var result = await _loanService.CreateLoanAsync(dto);
        return Ok(ApiResponse<LoanReadDto>.SuccessResult(result, "تم تسجيل القرض بنجاح وتوليد الأقساط"));
    }

    [HttpGet("employee/{employeeId}")]
    public async Task<ActionResult<ApiResponse<List<LoanReadDto>>>> GetEmployeeLoans(int employeeId)
    {
        var result = await _loanService.GetEmployeeLoansAsync(employeeId);
        return Ok(ApiResponse<List<LoanReadDto>>.SuccessResult(result));
    }
}
