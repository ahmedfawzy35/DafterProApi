using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// متحكم إدارة العمليات المالية (قبض / صرف / مصاريف / رواتب)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CashTransactionsController : ControllerBase
{
    private readonly ICashTransactionService _cashService;

    public CashTransactionsController(ICashTransactionService cashService)
    {
        _cashService = cashService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CashTransactionReadDto>>>> GetAll(
        [FromQuery] PaginationQueryDto query,
        [FromQuery] TransactionType? type,
        [FromQuery] TransactionSource? source,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await _cashService.GetAllAsync(query, type, source, from, to);
        return Ok(ApiResponse<PagedResult<CashTransactionReadDto>>.SuccessResult(result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<CashTransactionReadDto>>> GetById(int id)
    {
        var result = await _cashService.GetByIdAsync(id);
        if (result == null) return NotFound(ApiResponse<CashTransactionReadDto>.Failure("المعاملة غير موجودة"));
        return Ok(ApiResponse<CashTransactionReadDto>.SuccessResult(result));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<CashTransactionReadDto>>> Create([FromBody] CreateCashTransactionDto dto)
    {
        var result = await _cashService.CreateAsync(dto);
        return Ok(ApiResponse<CashTransactionReadDto>.SuccessResult(result, "تم تسجيل العملية المالية بنجاح"));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] CreateCashTransactionDto dto)
    {
        await _cashService.UpdateAsync(id, dto);
        return Ok(ApiResponse<object>.SuccessResult("تم تحديث العملية المالية بنجاح"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _cashService.DeleteAsync(id);
        return Ok(ApiResponse<object>.SuccessResult("تم حذف العملية المالية بنجاح"));
    }
}
