using StoreManagement.Shared.Interfaces;
using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Entities.Configuration;

/// <summary>
/// كيان الشركة - الوحدة الرئيسية في النظام
/// </summary>
public class Company : IAuditEntity
{
    public int Id { get; set; }

    // اسم الشركة
    public string Name { get; set; } = string.Empty;

    // الاسم المختصر بالإنجليزية (يستخدم للـ Username والروابط)
    public string CompanyCode { get; set; } = string.Empty;

    // حالة تفعيل الشركة (للإيقاف المؤقت أو النهائي)
    public bool Enabled { get; set; } = true;

    // العنوان
    public string? Address { get; set; }

    // نوع النشاط
    public string? BusinessType { get; set; }

    // هل للشركة فروع متعددة
    public bool HasBranches { get; set; } = false;

    // هل تدير الشركة المخزون
    public bool ManageInventory { get; set; } = false;

    // ===== بيانات إضافية (اختيارية) =====
    public string? TaxId { get; set; }              // الرقم الضريبي
    public string? CommercialRegistry { get; set; }    // السجل التجاري
    public string? OfficialEmail { get; set; }      // البريد الإلكتروني الرسمي
    public string? Website { get; set; }            // الموقع الإلكتروني
    public Currency? Currency { get; set; }         // العملة الافتراضية
    public string? Description { get; set; }        // وصف النشاط

    // ===== حقول التدقيق (Audit) =====
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; } = false;
    public int? CreatedByUserId { get; set; }

    // علاقة الشركة بالفروع
    public ICollection<Branch> Branches { get; set; } = [];

    // علاقة الشركة بالمستخدمين
    public ICollection<User> Users { get; set; } = [];

    // علاقة الشركة بأرقام الهاتف
    public ICollection<CompanyPhoneNumber> PhoneNumbers { get; set; } = [];

    // علاقة الشركة باللوجو
    public CompanyLogo? Logo { get; set; }
}

/// <summary>
/// كيان الفرع التابع للشركة
/// </summary>
public class Branch
{
    public int Id { get; set; }

    // اسم الفرع
    public string Name { get; set; } = string.Empty;

    // حالة تفعيل الفرع
    public bool Enabled { get; set; } = true;

    // معرف الشركة الأم
    public int CompanyId { get; set; }

    // علاقة الفرع بالشركة
    public Company Company { get; set; } = null!;
}
