using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.HR;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public EmployeeService(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<EmployeeReadDto>> GetEmployeesAsync(PaginationQueryDto query, bool? isEnabled)
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
                IsEnabled = e.IsEnabled, Phone = e.Phone,
                Type = e.Type,
                CurrentBranchId = e.CurrentBranchId,
                CurrentBranchName = e.CurrentBranch != null ? e.CurrentBranch.Name : null
            }).ToListAsync();

        return new PagedResult<EmployeeReadDto>
        {
            Items = employees, PageNumber = query.PageNumber,
            PageSize = query.PageSize, TotalCount = total
        };
    }

    public async Task<EmployeeReadDto> CreateEmployeeAsync(CreateEmployeeDto dto)
    {
        var employee = new Employee
        {
            Name = dto.Name, Salary = dto.Salary,
            Phone = dto.Phone, Type = dto.Type,
            CurrentBranchId = dto.CurrentBranchId ?? _currentUser.BranchId,
            CompanyId = (int)_currentUser.CompanyId!
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return new EmployeeReadDto 
        { 
            Id = employee.Id, Name = employee.Name, Salary = employee.Salary, Type = employee.Type 
        };
    }

    public async Task UpdateEmployeeAsync(int id, UpdateEmployeeDto dto)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == _currentUser.CompanyId);
        if (employee is null) throw new KeyNotFoundException("الموظف غير موجود أو لا تملك صلاحية الوصول إليه");

        employee.Name = dto.Name; employee.Salary = dto.Salary;
        employee.IsEnabled = dto.IsEnabled; employee.Phone = dto.Phone;
        employee.Type = dto.Type;
        if (dto.CurrentBranchId.HasValue) employee.CurrentBranchId = dto.CurrentBranchId;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteEmployeeAsync(int id)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id && e.CompanyId == _currentUser.CompanyId);
        if (employee is null) throw new KeyNotFoundException("الموظف غير موجود أو لا تملك صلاحية الوصول إليه");

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();
    }
}
