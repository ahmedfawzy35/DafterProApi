using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StoreManagement.Shared.Entities.Configuration; // For Branch
using StoreManagement.Shared.Entities.Core;

namespace StoreManagement.Shared.Entities.Inventory;

/// <summary>
/// يمثل الرصيد الحقيقي لمنتج معين داخل فرع معين.
/// يعتبر هذا الكيان المصدر الوحيد (Source of Truth) لمخزون الفروع.
/// </summary>
public class BranchProductStock
{
    public int Id { get; set; }
    
    // معرف الشركة
    public int CompanyId { get; set; }

    // المنتج
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // الفرع
    public int BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    // الرصيد الفعلي (نستخدم double للحفاظ على التوافق مع الأنظمة السابقة التي تعتمد على مزدوج، مع خطة لتحويلها لـ decimal لاحقاً)
    public double Quantity { get; set; } = 0;

    // الكمية المحجوزة لعملية معلقة (غير مستخدمة حالياً في الـ Logic لكن محجوزة للاستخدام المستقبلي)
    // وضعنا قيداً في قاعدة البيانات لمنع قيم سالبة هنا.
    public double ReservedQuantity { get; set; } = 0;

    // Concurrency Token لمنع الـ Race conditions
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // لتتبع التحركات والتدقيق
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastTransactionAt { get; set; }
}
