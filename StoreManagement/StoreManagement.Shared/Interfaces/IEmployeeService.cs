using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;

namespace StoreManagement.Shared.Interfaces;

public interface IEmployeeService
{
    Task<PagedResult<EmployeeReadDto>> GetEmployeesAsync(PaginationQueryDto query, bool? isEnabled);
    Task<EmployeeReadDto> CreateEmployeeAsync(CreateEmployeeDto dto);
    Task UpdateEmployeeAsync(int id, UpdateEmployeeDto dto);
    Task DeleteEmployeeAsync(int id);
}
