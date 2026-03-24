using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.HR;
using StoreManagement.Shared.Entities.Inventory;
using StoreManagement.Shared.Entities.Sales;
using StoreManagement.Shared.Entities.Finance;
using StoreManagement.Shared.Entities.Identity;
using StoreManagement.Shared.Entities.Partners;
using StoreManagement.Shared.Entities.Configuration;
using StoreManagement.Shared.Entities.Core;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Admin,Accountant")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeReadDto>>>> GetAll(
        [FromQuery] PaginationQueryDto query, [FromQuery] bool? isEnabled)
    {
        var result = await _employeeService.GetEmployeesAsync(query, isEnabled);
        return Ok(ApiResponse<PagedResult<EmployeeReadDto>>.SuccessResult(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EmployeeReadDto>>> Create([FromBody] CreateEmployeeDto dto)
    {
        var result = await _employeeService.CreateEmployeeAsync(dto);
        return Ok(ApiResponse<EmployeeReadDto>.SuccessResult(result, "تم إضافة الموظف بنجاح"));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] UpdateEmployeeDto dto)
    {
        try
        {
            await _employeeService.UpdateEmployeeAsync(id, dto);
            return Ok(ApiResponse<object>.SuccessResult("تم تعديل بيانات الموظف بنجاح"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Failure(ex.Message));
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        try
        {
            await _employeeService.DeleteEmployeeAsync(id);
            return Ok(ApiResponse<object>.SuccessResult("تم حذف الموظف بنجاح"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Failure(ex.Message));
        }
    }
}
