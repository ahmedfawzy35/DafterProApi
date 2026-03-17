using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// متحكم إدارة العملاء - Business Logic في ICustomerService
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomerReadDto>>>> GetAll(
        [FromQuery] PaginationQueryDto query)
    {
        var result = await _customerService.GetAllAsync(query);
        return Ok(ApiResponse<PagedResult<CustomerReadDto>>.SuccessResult(result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<CustomerReadDto>>> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer is null)
            return NotFound(ApiResponse<CustomerReadDto>.Failure("العميل غير موجود"));

        return Ok(ApiResponse<CustomerReadDto>.SuccessResult(customer));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Accountant,Sales")]
    public async Task<ActionResult<ApiResponse<CustomerReadDto>>> Create([FromBody] CreateCustomerDto dto)
    {
        var customer = await _customerService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id },
            ApiResponse<CustomerReadDto>.SuccessResult(customer, "تم إضافة العميل بنجاح"));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] UpdateCustomerDto dto)
    {
        await _customerService.UpdateAsync(id, dto);
        return Ok(ApiResponse<object>.SuccessResult("تم تعديل بيانات العميل بنجاح"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _customerService.DeleteAsync(id);
        return Ok(ApiResponse<object>.SuccessResult("تم حذف العميل بنجاح"));
    }
}
