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
/// حالة الحضور والغياب
/// </summary>
public enum AttendanceStatus
{
    Present = 1,  // حاضر
    Absent = 2,   // غائب
    Late = 3,     // متأخر
    OnLeave = 4   // في إجازة
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
