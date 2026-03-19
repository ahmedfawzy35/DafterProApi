using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities;
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

    public async Task RecordAttendanceAsync(AttendanceRecordDto dto)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId && e.CompanyId == (int)_currentUser.CompanyId!)
            ?? throw new KeyNotFoundException($"الموظف رقم {dto.EmployeeId} غير موجود");

        var record = new AttendanceRecord
        {
            EmployeeId = dto.EmployeeId,
            Date = dto.Date.Date,
            Status = (AttendanceStatus)dto.Status,
            Notes = dto.Notes
        };

        var existing = await _context.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == dto.EmployeeId && r.Date == record.Date);

        if (existing != null)
        {
            existing.Status = record.Status;
            existing.Notes = record.Notes;
        }
        else
        {
            _context.AttendanceRecords.Add(record);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<AttendanceReadDto>> GetAllAsync(DateTime date)
    {
        var queryDate = date.Date;

        return await _context.AttendanceRecords
            .Include(r => r.Employee)
            .Where(r => r.Date == queryDate && r.Employee.CompanyId == (int)_currentUser.CompanyId!)
            .Select(r => new AttendanceReadDto
            {
                Id = r.Id,
                EmployeeId = r.EmployeeId,
                EmployeeName = r.Employee.Name,
                Status = r.Status.ToString(),
                Date = r.Date,
                Notes = r.Notes
            })
            .ToListAsync();
    }
}
