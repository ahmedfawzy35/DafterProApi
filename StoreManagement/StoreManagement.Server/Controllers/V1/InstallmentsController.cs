using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.Constants;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;
using StoreManagement.Shared.Enums;

namespace StoreManagement.Server.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class InstallmentsController : ControllerBase
{
    private readonly IInstallmentService _installmentService;

    public InstallmentsController(IInstallmentService installmentService)
    {
        _installmentService = installmentService;
    }

    [HttpPost("preview")]
    // Allowed for multiple roles to simulate before saving
    [Authorize(Roles = "Admin,Accountant,Sales")]
    public async Task<ActionResult<ApiResponse<InstallmentSchedulePreviewDto>>> PreviewPlan([FromBody] CreateInstallmentPlanDto dto)
    {
        var preview = await _installmentService.PreviewScheduleAsync(dto);
        return Ok(ApiResponse<InstallmentSchedulePreviewDto>.SuccessResult(preview, "تم احتساب الخطة المبدئية بنجاح"));
    }

    [HttpPost]
    [Authorize(Policy = "RequirePermission:sales.create")]
    public async Task<ActionResult<ApiResponse<InstallmentPlanReadDto>>> CreatePlan([FromBody] CreateInstallmentPlanDto dto)
    {
        var plan = await _installmentService.CreatePlanAsync(dto);
        return Ok(ApiResponse<InstallmentPlanReadDto>.SuccessResult(plan, "تم تشغيل خطة التقسيط بنجاح"));
    }

    [HttpGet]
    [Authorize(Policy = "RequirePermission:sales.view")]
    public async Task<ActionResult<ApiResponse<PagedResult<InstallmentPlanReadDto>>>> GetAll(
        [FromQuery] PaginationQueryDto query,
        [FromQuery] int? customerId,
        [FromQuery] string? status)
    {
        var result = await _installmentService.GetAllPlansAsync(query, customerId, status);
        return Ok(ApiResponse<PagedResult<InstallmentPlanReadDto>>.SuccessResult(result));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "RequirePermission:sales.view")]
    public async Task<ActionResult<ApiResponse<InstallmentPlanReadDto>>> GetById(int id)
    {
        var plan = await _installmentService.GetPlanByIdAsync(id);
        if (plan == null) return NotFound(ApiResponse<InstallmentPlanReadDto>.Failure("خطة التقسيط غير موجودة"));
        return Ok(ApiResponse<InstallmentPlanReadDto>.SuccessResult(plan));
    }

    [HttpPost("schedules/{scheduleId}/pay")]
    [Authorize(Policy = "RequirePermission:sales.create")] // In real scenarios it might need custom permissions like collection.receive
    public async Task<ActionResult<ApiResponse<InstallmentPaymentResultDto>>> PayInstallment(
        int scheduleId, 
        [FromBody] PayInstallmentDto dto)
    {
        if (dto.Amount <= 0) return BadRequest(ApiResponse<InstallmentPaymentResultDto>.Failure("المبلغ يجب أن يكون أكبر من الصفر."));
        
        var result = await _installmentService.PayInstallmentAsync(scheduleId, dto.Amount, dto.BranchId);
        return Ok(ApiResponse<InstallmentPaymentResultDto>.SuccessResult(result, result.Message));
    }
}

public class PayInstallmentDto
{
    public decimal Amount { get; set; }
    public int? BranchId { get; set; }
}
