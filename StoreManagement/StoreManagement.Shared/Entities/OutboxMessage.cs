namespace StoreManagement.Shared.Entities;

/// <summary>
/// رسالة Outbox لضمان عدم فقدان البيانات في العمليات المهمة
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // نوع الحدث (مثل: InvoiceCreated, StockUpdated)
    public string Type { get; set; } = string.Empty;

    // محتوى البيانات بصيغة JSON
    public string Payload { get; set; } = string.Empty;

    // حالة المعالجة
    public bool Processed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }

    // عدد محاولات المعالجة (للـ Retry Policy)
    public int RetryCount { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
