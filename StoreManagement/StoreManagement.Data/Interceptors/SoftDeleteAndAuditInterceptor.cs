using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using StoreManagement.Shared.Entities;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Data.Interceptors;

/// <summary>
/// معترض لحفظ التغييرات يقوم بـ:
/// 1- تحويل عمليات الحذف إلى Soft Delete
/// 2- تحديث ModifiedDate و EditCount تلقائياً
/// 3- تعيين CreatedByUserId و CreatedDate
/// </summary>
public class SoftDeleteAndAuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public SoftDeleteAndAuditInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    // تنفيذ المنطق قبل الحفظ الفعلي
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ProcessEntries(eventData.Context, _currentUser);
        return base.SavingChanges(eventData, result);
    }

    // النسخة غير المتزامنة
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ProcessEntries(eventData.Context, _currentUser);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ProcessEntries(DbContext? context, ICurrentUserService currentUser)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries<IAuditEntity>())
        {
            // تحويل الحذف الفعلي إلى حذف مؤقت
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.ModifiedDate = DateTime.UtcNow;
            }

            // تحديث تاريخ التعديل
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedDate = DateTime.UtcNow;
                
                // إذا كان الكيان يرمث من BaseEntity، نحدث عداد التعديلات
                if (entry.Entity is BaseEntity baseEntity)
                {
                    baseEntity.EditCount++;
                }
            }

            // تعيين بيانات الإنشاء تلقائياً
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = DateTime.UtcNow;
                entry.Entity.CreatedByUserId = currentUser.UserId;
            }
        }
    }
}
