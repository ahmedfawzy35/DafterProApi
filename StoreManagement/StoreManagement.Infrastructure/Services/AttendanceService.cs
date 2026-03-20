using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.HR;
using StoreManagement.Shared.Entities.Inventory;
using StoreManagement.Shared.Entities.Sales;
using StoreManagement.Shared.Entities.Finance;
using StoreManagement.Shared.Entities.Identity;
using StoreManagement.Shared.Entities.Partners;
using StoreManagement.Shared.Entities.Configuration;
using StoreManagement.Shared.Entities.Core;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

public class AttendanceService : IAttendanceService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AttendanceService(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task RecordAttendanceAsync(AttendanceCreateDto dto)
    {
        var companyId = (int)_currentUser.CompanyId!;
        
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId && e.CompanyId == companyId)
            ?? throw new KeyNotFoundException($"الموظف رقم {dto.EmployeeId} غير موجود");

        var recordDate = dto.Date.Date;

        var existing = await _context.Attendances
            .FirstOrDefaultAsync(a => a.EmployeeId == dto.EmployeeId && a.Date == recordDate);

        if (existing != null)
        {
            existing.Status = dto.Status;
            existing.WorkingHours = dto.WorkingHours;
            existing.Notes = dto.Notes;
        }
        else
        {
            var record = new Attendance
            {
                EmployeeId = dto.EmployeeId,
                Date = recordDate,
                Status = dto.Status,
                WorkingHours = dto.WorkingHours,
                Notes = dto.Notes,
                CompanyId = companyId
            };
            _context.Attendances.Add(record);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<AttendanceReadDto>> GetAllAsync(DateTime date)
    {
        var queryDate = date.Date;
        var companyId = (int)_currentUser.CompanyId!;

        return await _context.Attendances
            .Include(a => a.Employee)
            .Where(a => a.Date == queryDate && a.CompanyId == companyId)
            .Select(a => new AttendanceReadDto
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                EmployeeName = a.Employee.Name,
                Status = a.Status.ToString(),
                Date = a.Date,
                WorkingHours = a.WorkingHours,
                Notes = a.Notes
            })
            .ToListAsync();
    }

    public async Task<List<AttendanceReadDto>> GetEmployeeAttendanceAsync(int employeeId, int month, int year)
    {
        var companyId = (int)_currentUser.CompanyId!;

        return await _context.Attendances
            .Where(a => a.EmployeeId == employeeId && a.Date.Month == month && a.Date.Year == year && a.CompanyId == companyId)
            .Select(a => new AttendanceReadDto
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                Status = a.Status.ToString(),
                Date = a.Date,
                WorkingHours = a.WorkingHours,
                Notes = a.Notes
            })
            .ToListAsync();
    }
}
