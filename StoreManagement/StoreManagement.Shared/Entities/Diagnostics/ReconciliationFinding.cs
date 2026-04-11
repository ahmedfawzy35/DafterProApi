using System;
using StoreManagement.Shared.Entities.Core;
using StoreManagement.Shared.Entities.Configuration;
using StoreManagement.Shared.Entities.Identity;
using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Entities.Diagnostics;

/// <summary>
/// كيان تسجيل أخطاء وتناقضات البيانات التي تكتشفها المعالجات الخلفية
/// يسجل التشخيص والتحليل لغرض المراجعة فقط بدون تعديل أوتوماتيكي
/// </summary>
public class ReconciliationFinding : BaseEntity
{
    // Company ID is inherited from BaseEntity
    public Company Company { get; set; } = null!;

    // الفئة العامة للمشكلة (مثال: Inventory, Financial)
    public string Category { get; set; } = string.Empty;

    // كود القاعدة الثابت الذي تم انتهاكه (مثال: INV_NEGATIVE_STOCK)
    // مفيد في البحث والتصنيف والـ Deduplication
    public string RuleCode { get; set; } = string.Empty;

    // مستوى الخطورة (مثال: Critical, Warning)
    public string Severity { get; set; } = string.Empty;

    // نوع الكيان المتأثر (مثال: Product, Customer)
    public string EntityType { get; set; } = string.Empty;

    // معرف الكيان المتأثر
    public int EntityId { get; set; }

    // رسالة قابلة للقراءة تشرح المشكلة (Human-readable)
    public string Message { get; set; } = string.Empty;

    // أول مرة تم اكتشاف المشكلة فيها
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    // آخر مرة ظهرت فيها المشكلة (مفيد في التكرارات)
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    // الحالة الحالية للإجراء (مفتوح، مراجع، محلول)
    public FindingStatus Status { get; set; } = FindingStatus.Open;

    // تاريخ حل المشكلة (إن تم الحل)
    public DateTime? ResolvedAt { get; set; }

    // مصدر الحل (مثال: AutoReconciledScan, AdminUser)
    public string? ResolutionSource { get; set; }

    // المستخدم الذي قام بالحل اليدوي (إن وجد)
    public int? ResolvedByUserId { get; set; }
    public User? ResolvedByUser { get; set; }

    // توقيع مميز لمنع تكرار نفس المشكلة يومياً (Composite Hash أو String)
    public string AnomalySignature { get; set; } = string.Empty;
}
