namespace StoreManagement.Shared.Enums;

/// <summary>
/// نوع المعاملة النقدية: وارد أو صادر
/// </summary>
public enum TransactionType
{
    In = 1,   // وارد (دخل)
    Out = 2   // صادر (خرج)
}

/// <summary>
/// مصدر المعاملة النقدية
/// </summary>
public enum TransactionSource
{
    Customer = 1,   // عميل
    Supplier = 2,   // مورد
    Bank = 3,       // بنك
    Salary = 4,     // راتب
    Expense = 5,    // مصروف
    Other = 6       // أخرى
}















/// <summary>
/// نوع حركة المخزون: وارد أو صادر
/// </summary>
public enum StockMovementType
{
    In = 1,   // توريد
    Out = 2   // صرف
}

/// <summary>
/// نوع الفاتورة: مبيعات أو مشتريات
/// </summary>
public enum InvoiceType
{
    Sale = 1,      // فاتورة مبيعات
    Purchase = 2   // فاتورة مشتريات
}

/// <summary>
/// نوع التسوية الحسابية
/// </summary>
public enum SettlementType
{
    Add = 1,       // إضافة للرصيد (أرباح/تسوية موجبة)
    Subtract = 2   // خصم من الرصيد (خسائر/تسوية سالبة)
}

/// <summary>
/// مصدر التسوية
/// </summary>
public enum SettlementSource
{
    Customer = 1,
    Supplier = 2
}

/// <summary>
/// العملات المشهورة في مصر والوطن العربي
/// </summary>
public enum Currency
{
    EGP = 1,   // جنيه مصري
    SAR = 2,   // ريال سعودي
    AED = 3,   // درهم إماراتي
    KWD = 4,   // دينار كويتي
    QAR = 5,   // ريال قطري
    BHD = 6,   // دينار بحريني
    OMR = 7,   // ريال عماني
    JOD = 8,   // دينار أردني
    LBP = 9,   // ليرة لبنانية
    LYD = 10,  // دينار ليبي
    TND = 11,  // دينار تونسي
    MAD = 12,  // درهم مغربي
    SDG = 13,  // جنيه سوداني
    IQD = 14,  // دينار عراقي
    YER = 15,  // ريال يمني
    USD = 16,  // دولار أمريكي
    EUR = 17   // يورو
}

// ===== HR & Payroll Enums =====

public enum EmployeeType
{
    Monthly = 1,
    Daily = 2,
    Weekly = 3,
    Commission = 4
}

public enum EmployeeActionType
{
    Hire = 1,           // تعيين
    Termination = 2,    // إنهاء خدمة
    Leave = 3,          // إجازة (سنوية/مرضية)
    UnpaidLeave = 4,    // إجازة بدون راتب
    Suspension = 5,     // توقيف عن العمل
    ReturnToWork = 6    // عودة للعمل
}

public enum AdjustmentType
{
    Addition = 1,   // إضافة (بدل، مكافأة)
    Deduction = 2   // خصم (جزاء، استقطاع)
}

public enum RecurringAdjustmentType
{
    FixedAmount = 1,
    PercentageOfBasic = 2
}

public enum LoanStatus
{
    Active = 1,
    Paid = 2,
    Closed = 3,
    Restructured = 4
}

public enum PolicyDataType
{
    String = 1,
    Int = 2,
    Decimal = 3,
    Boolean = 4,
    Json = 5
}

public enum AttendanceStatus
{
    Present = 1,    // حاضر
    Absent = 2,     // غائب
    Late = 3,       // متأخر
    Leave = 4,      // إجازة
    Holiday = 5,    // عطلة رسمية
    Weekend = 6     // عطلة نهاية أسبوع
}
