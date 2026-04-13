using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Shared.Entities.Core;

/// <summary>
/// الكيان الأساسي الذي ترث منه جميع جداول النظام
/// </summary>
public abstract class BaseEntity : IAuditEntity, ICompanyEntity
{
    // المعرف الفريد
    public int Id { get; set; }

    // معرف الشركة المالكة للسجل
    public int CompanyId { get; set; }

    // حذف مؤقت (Soft Delete)
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByUserId { get; set; }

    // عداد مرات التعديل
    public int EditCount { get; set; } = 0;

    // تاريخ الإنشاء
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // تاريخ آخر تعديل
    public DateTime? ModifiedDate { get; set; }

    // المستخدم المنشئ
    public int? CreatedByUserId { get; set; }

    // معالجة التزامن والتعديل المتزامن (Optimistic Concurrency)
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[]? RowVersion { get; set; }
}
