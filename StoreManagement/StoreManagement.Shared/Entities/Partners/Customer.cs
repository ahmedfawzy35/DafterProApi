namespace StoreManagement.Shared.Entities.Partners;

/// <summary>
/// كيان العميل — يمثل العميل التجاري في النظام
/// يرث من BaseEntity الذي يحتوي على: Id, CompanyId, IsDeleted, CreatedDate, RowVersion
/// </summary>
public class Customer : BaseEntity
{
    // ===== بيانات الهوية الأساسية =====

    /// <summary>
    /// اسم العميل — مطلوب، فريد داخل الشركة (index موجود في DbContext)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// كود العميل الداخلي (اختياري) — مثل: C001, CUST-0023
    /// يُستخدم للبحث السريع في POS
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// عنوان العميل — مفيد للشحن والمخاطبة
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// البريد الإلكتروني (اختياري)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// ملاحظات داخلية على العميل
    /// </summary>
    public string? Notes { get; set; }

    // ===== حالة النشاط =====

    /// <summary>
    /// هل العميل نشط؟
    /// يختلف عن IsDeleted:
    ///   - IsActive = false: العميل موجود لكن معطّل (لا يظهر في قوائم الإضافة والبيع)
    ///   - IsDeleted = true: محذوف منطقياً ومخفي من كل الاستعلامات
    /// </summary>
    public bool IsActive { get; set; } = true;

    // ===== Audit Trail لعمليات التفعيل/التعطيل =====

    /// <summary>
    /// تاريخ ووقت آخر تغيير في حالة النشاط (تفعيل أو تعطيل)
    /// يُضبط تلقائياً من CustomerService.ActivateAsync/DeactivateAsync
    /// </summary>
    public DateTime? StatusChangedAt { get; set; }

    /// <summary>
    /// المستخدم الذي قام بتغيير حالة النشاط
    /// يُخزَّن كـ Username أو Email للتعريف السريع
    /// </summary>
    public string? StatusChangedBy { get; set; }

    // ===== الحقول المالية =====

    /// <summary>
    /// [LEGACY FIELD] الرصيد النقدي القديم — عُمل به في النظام الأول قبل نظام Running Balance
    /// الدور الحالي: يُعامَل كـ Opening Balance (رصيد بداية) فقط
    /// مصدر الحقيقة الحقيقي الآن هو: Σ(Invoices) - Σ(CustomerReceipts) + CashBalance
    /// لا يُحدَّث تلقائياً من أي عملية جديدة
    /// </summary>
    public decimal CashBalance { get; set; } = 0;

    /// <summary>
    /// رصيد الافتتاح (Opening Balance) عند إضافة العميل للنظام
    /// إذا كان الميزان يبدأ من صفر = 0
    /// إذا كان العميل مديوناً من قبل = قيمة موجبة
    /// إذا كان له رصيد دائن = قيمة سالبة
    /// </summary>
    public decimal OpeningBalance { get; set; } = 0;

    /// <summary>
    /// الحد الائتماني المسموح للعميل (الحد الأقصى للدين)
    /// 0 = لا حد
    /// </summary>
    public decimal CreditLimit { get; set; } = 0;

    // ===== العلاقات =====

    /// <summary>
    /// قائمة أرقام هواتف العميل (واحد أو أكثر)
    /// </summary>
    public ICollection<CustomerPhone> Phones { get; set; } = [];
}
