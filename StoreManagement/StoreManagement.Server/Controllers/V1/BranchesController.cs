using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// متحكم إدارة الفروع
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/branches")]
public class BranchesController : ControllerBase
{
    private readonly IBranchService _branchService;

    public BranchesController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<BranchReadDto>>>> GetAll()
    {
        var result = await _branchService.GetAllAsync();
        return Ok(ApiResponse<List<BranchReadDto>>.SuccessResult(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<BranchReadDto>>> GetById(int id)
    {
        var result = await _branchService.GetByIdAsync(id);
        if (result == null) return NotFound(ApiResponse<BranchReadDto>.Failure("الفرع غير موجود"));
        return Ok(ApiResponse<BranchReadDto>.SuccessResult(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BranchReadDto>>> Create([FromBody] CreateBranchDto dto)
    {
        var result = await _branchService.CreateAsync(dto);
        return Ok(ApiResponse<BranchReadDto>.SuccessResult(result));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<string>>> Update(int id, [FromBody] UpdateBranchDto dto)
    {
        await _branchService.UpdateAsync(id, dto);
        return Ok(ApiResponse<string>.SuccessResult("تم تعديل بيانات الفرع بنجاح"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _branchService.DeleteAsync(id);
        return Ok(ApiResponse<object>.SuccessResult("تم حذف الفرع بنجاح"));
    }

    [HttpGet("{id}/status")]
    public async Task<ActionResult<ApiResponse<string>>> GetStatus(int id)
    {
        var status = await _branchService.GetBranchStatusAsync(id);
        return Ok(ApiResponse<string>.SuccessResult(status));
    }
}
