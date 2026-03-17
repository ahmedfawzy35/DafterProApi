using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using StoreManagement.Shared.Entities;

namespace StoreManagement.Data.Interceptors;

/// <summary>
/// معترض لحفظ التغييرات يقوم بـ:
/// 1- تحويل عمليات الحذف إلى Soft Delete
/// 2- تحديث ModifiedDate و EditCount تلقائياً
/// </summary>
public class SoftDeleteAndAuditInterceptor : SaveChangesInterceptor
{
    // تنفيذ المنطق قبل الحفظ الفعلي
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ProcessEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    // النسخة غير المتزامنة
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ProcessEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ProcessEntries(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            // تحويل الحذف الفعلي إلى حذف مؤقت
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.ModifiedDate = DateTime.UtcNow;
            }

            // تحديث تاريخ التعديل وعداد التعديلات
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedDate = DateTime.UtcNow;
                entry.Entity.EditCount++;
            }

            // تعيين تاريخ الإنشاء تلقائياً
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = DateTime.UtcNow;
            }
        }
    }
}
