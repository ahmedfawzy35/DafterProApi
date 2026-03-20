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
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public EmployeesController(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeReadDto>>>> GetAll(
        [FromQuery] PaginationQueryDto query, [FromQuery] bool? isEnabled)
    {
        var employeesQuery = _context.Employees.AsQueryable();

        if (isEnabled.HasValue)
            employeesQuery = employeesQuery.Where(e => e.IsEnabled == isEnabled.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
            employeesQuery = employeesQuery.Where(e => e.Name.Contains(query.Search));

        var total = await employeesQuery.CountAsync();
        var employees = await employeesQuery
            .OrderBy(e => e.Name)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new EmployeeReadDto
            {
                Id = e.Id, Name = e.Name, Salary = e.Salary,
                Allowances = e.Allowances, Deductions = e.Deductions,
                IsEnabled = e.IsEnabled, Phone = e.Phone,
                Type = e.Type
            }).ToListAsync();

        return Ok(ApiResponse<PagedResult<EmployeeReadDto>>.SuccessResult(new PagedResult<EmployeeReadDto>
        {
            Items = employees, PageNumber = query.PageNumber,
            PageSize = query.PageSize, TotalCount = total
        }));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EmployeeReadDto>>> Create([FromBody] CreateEmployeeDto dto)
    {
        var employee = new Employee
        {
            Name = dto.Name, Salary = dto.Salary,
            Allowances = dto.Allowances, Deductions = dto.Deductions,
            Phone = dto.Phone, Type = dto.Type,
            CompanyId = (int)_currentUser.CompanyId!
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<EmployeeReadDto>.SuccessResult(
            new EmployeeReadDto { Id = employee.Id, Name = employee.Name, Salary = employee.Salary, Type = employee.Type },
            "تم إضافة الموظف بنجاح"));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] UpdateEmployeeDto dto)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee is null) return NotFound(ApiResponse<object>.Failure("الموظف غير موجود"));

        employee.Name = dto.Name; employee.Salary = dto.Salary;
        employee.Allowances = dto.Allowances; employee.Deductions = dto.Deductions;
        employee.IsEnabled = dto.IsEnabled; employee.Phone = dto.Phone;
        employee.Type = dto.Type;

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResult("تم تعديل بيانات الموظف بنجاح"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee is null) return NotFound(ApiResponse<object>.Failure("الموظف غير موجود"));

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResult("تم حذف الموظف بنجاح"));
    }
}
