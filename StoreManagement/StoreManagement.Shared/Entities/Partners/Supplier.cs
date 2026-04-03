namespace StoreManagement.Shared.Entities.Partners;

/// <summary>
/// كيان المورد — يمثل المورد التجاري في النظام
/// يرث من BaseEntity الذي يحتوي على: Id, CompanyId, IsDeleted, CreatedDate, RowVersion
/// </summary>
public class Supplier : BaseEntity
{
    // ===== بيانات الهوية الأساسية =====

    /// <summary>
    /// اسم المورد — مطلوب، فريد داخل الشركة (index موجود في DbContext)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// كود المورد الداخلي (اختياري) — مثل: S001, SUP-0010
    /// يُستخدم للبحث السريع
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// عنوان المورد — مفيد للتواصل والمراسلة
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// البريد الإلكتروني (اختياري)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// ملاحظات داخلية على المورد
    /// </summary>
    public string? Notes { get; set; }

    // ===== حالة النشاط =====

    /// <summary>
    /// هل المورد نشط؟
    /// IsActive = false: المورد معطّل (لا يظهر في قوائم الشراء والمعاملات الجديدة)
    /// IsDeleted = true: محذوف منطقياً مخفي من كل الاستعلامات
    /// </summary>
    public bool IsActive { get; set; } = true;

    // ===== Audit Trail لعمليات التفعيل/التعطيل =====

    /// <summary>
    /// تاريخ ووقت آخر تغيير في حالة النشاط
    /// </summary>
    public DateTime? StatusChangedAt { get; set; }

    /// <summary>
    /// المستخدم الذي قام بتغيير حالة النشاط
    /// </summary>
    public string? StatusChangedBy { get; set; }

    // ===== الحقول المالية =====

    /// <summary>
    /// [LEGACY FIELD] الرصيد النقدي القديم — يُعامَل كـ Opening Balance فقط
    /// مصدر الحقيقة الآن هو: Σ(Invoices) - Σ(SupplierPayments) + CashBalance
    /// لا يُحدَّث تلقائياً من أي عملية جديدة
    /// </summary>
    public decimal CashBalance { get; set; } = 0;

    /// <summary>
    /// رصيد الافتتاح (Opening Balance) عند إضافة المورد للنظام
    /// قيمة موجبة = مديونية للمورد من قبل بداية النظام
    /// قيمة سالبة = لهم رصيد دائن من قبل
    /// </summary>
    public decimal OpeningBalance { get; set; } = 0;

    // ===== العلاقات =====

    /// <summary>
    /// قائمة أرقام هواتف المورد (واحد أو أكثر)
    /// </summary>
    public ICollection<SupplierPhone> Phones { get; set; } = [];
}
