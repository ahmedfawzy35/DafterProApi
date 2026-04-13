namespace StoreManagement.Shared.Interfaces;

/// <summary>
/// واجهة للحذف المؤقت
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    int? DeletedByUserId { get; set; }
}

/// <summary>
/// واجهة لحقول التدقيق الأساسية
/// </summary>
public interface IAuditEntity : ISoftDelete
{
    DateTime CreatedDate { get; set; }
    DateTime? ModifiedDate { get; set; }
    int? CreatedByUserId { get; set; }
}
