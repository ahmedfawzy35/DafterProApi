using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// متحكم إدارة الموردين
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public SuppliersController(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<SupplierReadDto>>>> GetAll(
        [FromQuery] PaginationQueryDto query)
    {
        var suppliersQuery = _context.Suppliers.Include(s => s.Phones).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            suppliersQuery = suppliersQuery.Where(s => s.Name.Contains(query.Search));

        var total = await suppliersQuery.CountAsync();
        var suppliers = await suppliersQuery
            .OrderBy(s => s.Name)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new SupplierReadDto
            {
                Id = s.Id, Name = s.Name, CashBalance = s.CashBalance,
                Phones = s.Phones.Select(p => p.PhoneNumber).ToList(),
                CreatedDate = s.CreatedDate
            }).ToListAsync();

        return Ok(ApiResponse<PagedResult<SupplierReadDto>>.SuccessResult(new PagedResult<SupplierReadDto>
        {
            Items = suppliers, PageNumber = query.PageNumber,
            PageSize = query.PageSize, TotalCount = total
        }));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<SupplierReadDto>>> Create([FromBody] CreateSupplierDto dto)
    {
        var supplier = new Supplier { Name = dto.Name, CompanyId = _currentUser.CompanyId };

        foreach (var phone in dto.Phones)
            supplier.Phones.Add(new SupplierPhone { PhoneNumber = phone });

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<SupplierReadDto>.SuccessResult(
            new SupplierReadDto { Id = supplier.Id, Name = supplier.Name, Phones = dto.Phones },
            "تم إضافة المورد بنجاح"));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Accountant")]
    public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] UpdateSupplierDto dto)
    {
        var supplier = await _context.Suppliers.Include(s => s.Phones)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier is null) return NotFound(ApiResponse<object>.Failure("المورد غير موجود"));

        supplier.Name = dto.Name;
        _context.SupplierPhones.RemoveRange(supplier.Phones);
        foreach (var phone in dto.Phones)
            supplier.Phones.Add(new SupplierPhone { PhoneNumber = phone, SupplierId = id });

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResult("تم تعديل بيانات المورد بنجاح"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
        if (supplier is null) return NotFound(ApiResponse<object>.Failure("المورد غير موجود"));

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResult("تم حذف المورد بنجاح"));
    }
}

/// <summary>
/// متحكم إدارة الموظفين
/// </summary>
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
                IsEnabled = e.IsEnabled, Phone = e.Phone
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
            Phone = dto.Phone, CompanyId = _currentUser.CompanyId
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<EmployeeReadDto>.SuccessResult(
            new EmployeeReadDto { Id = employee.Id, Name = employee.Name, Salary = employee.Salary },
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
