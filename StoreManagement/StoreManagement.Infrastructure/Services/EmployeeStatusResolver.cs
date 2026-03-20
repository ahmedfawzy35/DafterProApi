using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
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
using StoreManagement.Shared.DTOs;

namespace StoreManagement.Infrastructure.Services;

public class EmployeeStatusResolver : IEmployeeStatusResolver
{
    private readonly StoreDbContext _context;

    public EmployeeStatusResolver(StoreDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeStatusResult> GetStatusAsync(int employeeId, DateTime date)
    {
        // البحث عن الإجراء الفعال في هذا التاريخ
        // الأولوية للتواريخ الأحدث أو الإجراء الذي ليس له تاريخ انتهاء
        var action = await _context.EmployeeActions
            .Where(a => a.EmployeeId == employeeId && a.EffectiveFrom <= date)
            .OrderByDescending(a => a.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (action == null)
        {
            return new EmployeeStatusResult { Date = date, Status = "Inactive" };
        }

        // إذا كان هناك تاريخ انتهاء وتجاوزناه
        if (action.EffectiveTo != null && action.EffectiveTo < date)
        {
            // إذا انتهى الإجراء الأخير، نعود لحالة افتراضية بناءً على نوع الإجراء المنتهي
            // مثال: إذا انتهى Suspension، قد يكون عاد للعمل أو أصبح غير فعال حسب سياسة النظام
            // للتبسيط: سنعتبره غير فعال إلا إذا كان هناك إجراء تعيين لاحق
            return new EmployeeStatusResult { Date = date, Status = "Inactive" };
        }

        string statusText = action.ActionType switch
        {
            EmployeeActionType.Hire => "Active",
            EmployeeActionType.ReturnToWork => "Active",
            EmployeeActionType.Termination => "Terminated",
            EmployeeActionType.UnpaidLeave => "OnUnpaidLeave",
            EmployeeActionType.Suspension => "Suspended",
            _ => "Unknown"
        };

        return new EmployeeStatusResult { Date = date, Status = statusText };
    }

    public async Task<List<EmployeeStatusResult>> GetMonthlyStatusAsync(int employeeId, int month, int year)
    {
        var results = new List<EmployeeStatusResult>();
        int daysInMonth = DateTime.DaysInMonth(year, month);

        for (int i = 1; i <= daysInMonth; i++)
        {
            var date = new DateTime(year, month, i);
            results.Add(await GetStatusAsync(employeeId, date));
        }

        return results;
    }
}
