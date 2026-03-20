using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Entities.Configuration;

/// <summary>
/// كيان سياسة الشركة (قواعد عمل متغيرة لكل متجر)
/// </summary>
public class CompanyPolicy : BaseEntity
{
    // مفتاح السياسة (مثال: "MinSalaryProtection")
    public string PolicyKey { get; set; } = string.Empty;

    // قيمة السياسة (تخزن كنص)
    public string PolicyValue { get; set; } = string.Empty;

    // نوع البيانات للقيمة (للتمثيل الصحيح برمجياً)
    public PolicyDataType DataType { get; set; }

    // وصف توضيحي للسياسة
    public string? Description { get; set; }
}
