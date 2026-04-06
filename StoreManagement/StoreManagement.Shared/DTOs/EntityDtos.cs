using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Entities;

namespace StoreManagement.Shared.DTOs;

// ===================================
// ===== DTOs مشتركة =====
// ===================================

/// <summary>
/// DTO للاستعلام عن كشف الحساب (Customer + Supplier) مع Pagination
/// يُستخدم في: GET /customers/{id}/statement و GET /suppliers/{id}/statement
/// </summary>
public class StatementQueryDto
{
    // تصفية بالتاريخ — اختياريان، إذا لم يُحدَّدا يُعاد آخر 90 يوماً كافتراضي
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    // الصفحة الحالية (تبدأ من 1)
    public int PageNumber { get; set; } = 1;

    // عدد السطور في الصفحة (50 = عرض طبيعي، 200 = تصدير)
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// نتيجة كشف الحساب المُقسَّمة على صفحات
/// تُعاد من GET /customers/{id}/statement و GET /suppliers/{id}/statement
/// </summary>
public class StatementPagedResult<T>
{
    // سطور الكشف
    public List<T> Items { get; set; } = [];

    // ===== Pagination =====
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;

    // ===== ملخص الرصيد للصفحة الكاملة (ليس الصفحة الحالية فقط) =====
    // رصيد الافتتاح للفترة المختارة
    public decimal OpeningBalance { get; set; }

    // إجمالي المدين في الفترة
    public decimal TotalDebit { get; set; }

    // إجمالي الدائن في الفترة
    public decimal TotalCredit { get; set; }

    // الرصيد الختامي للفترة
    public decimal ClosingBalance { get; set; }
}

// ===================================
// ===== DTOs خاصة بالعميل =====
// ===================================

/// <summary>
/// DTO رقم الهاتف المستقل — يدعم تمييز الرقم الأساسي
/// يُستخدم في Create و Update
/// </summary>
public class PhoneDto
{
    // رقم الهاتف (مثال: 01012345678)
    public string PhoneNumber { get; set; } = string.Empty;

    // هل هذا الرقم هو الرقم الأساسي؟ (الأول في القائمة عادةً)
    public bool IsPrimary { get; set; } = false;
}

/// <summary>
/// بيانات إنشاء عميل جديد
/// </summary>
public class CreateCustomerDto
{
    // الاسم مطلوب
    public string Name { get; set; } = string.Empty;

    // كود داخلي اختياري — مثل: C001 (فريد داخل الشركة)
    public string? Code { get; set; }

    // العنوان الجغرافي أو العنوان التجاري
    public string? Address { get; set; }

    // البريد الإلكتروني (اختياري)
    public string? Email { get; set; }

    // ملاحظات داخلية على العميل
    public string? Notes { get; set; }

    // رصيد الافتتاح عند إضافة العميل للنظام (إن كان لديه دين سابق)
    public decimal OpeningBalance { get; set; } = 0;

    // الحد الائتماني المسموح (0 = لا حد أقصى)
    public decimal CreditLimit { get; set; } = 0;

    // قائمة أرقام الهواتف — يجب أن يكون أحدها IsPrimary = true إن أُرسل أكثر من رقم
    public List<PhoneDto> Phones { get; set; } = [];
}

/// <summary>
/// بيانات تعديل عميل موجود
/// </summary>
public class UpdateCustomerDto
{
    // الاسم مطلوب
    public string Name { get; set; } = string.Empty;

    // الكود الداخلي (اختياري)
    public string? Code { get; set; }

    // العنوان
    public string? Address { get; set; }

    // البريد الإلكتروني
    public string? Email { get; set; }

    // الملاحظات
    public string? Notes { get; set; }

    // الحد الائتماني
    public decimal CreditLimit { get; set; } = 0;

    // قائمة الهواتف الجديدة (تُحدَّث بالكامل — replace all)
    public List<PhoneDto> Phones { get; set; } = [];
}

/// <summary>
/// بيانات عرض العميل (استجابة القراءة العامة)
/// يُستخدم في قوائم العملاء والبحث
/// </summary>
public class CustomerReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }

    // [LEGACY] الرصيد القديم — يُعرض للتوافق الخلفي فقط
    // مصدر الحقيقة الفعلي هو /profile endpoint
    public decimal CashBalance { get; set; }

    // رصيد الافتتاح
    public decimal OpeningBalance { get; set; }

    // الحد الائتماني
    public decimal CreditLimit { get; set; }

    // الرقم الأساسي فقط (للعرض السريع في القوائم)
    public string? PrimaryPhone { get; set; }

    // كل الأرقام
    public List<PhoneDto> Phones { get; set; } = [];

    public DateTime CreatedDate { get; set; }

    // ===== Audit Trail (حالة النشاط) =====
    // متى تم آخر تفعيل أو تعطيل لهذا العميل؟
    public DateTime? StatusChangedAt { get; set; }
    // من قام بالتفعيل/التعطيل؟
    public string? StatusChangedBy { get; set; }
}

/// <summary>
/// فلاتر البحث في العملاء
/// يُستخدم في GET /api/v1/customers
/// </summary>
public class CustomerFilterDto : PaginationQueryDto
{
    // البحث النصي — يشمل الاسم والكود ورقم الهاتف
    // (Search موروث من PaginationQueryDto)

    // فلتر حالة النشاط: null = الكل، true = نشط فقط، false = معطّل فقط
    public bool? IsActive { get; set; } = true;

    // فلتر: العملاء الذين عليهم دين غير محسوم (HasDebt)
    // يُحسب بشكل Dynamic في الـ Service ولا يُخزَّن
    public bool? HasDebt { get; set; }

    // فلتر: العملاء الذين لديهم فواتير مفتوحة
    public bool? HasOpenInvoices { get; set; }
}

/// <summary>
/// ملف العميل الشامل — يُعاد من GET /api/v1/customers/{id}/profile
/// يجمع بيانات العميل + الرصيد المحسوب + آخر الحركات
/// </summary>
public class CustomerProfileDto
{
    // البيانات الأساسية للعميل
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public List<PhoneDto> Phones { get; set; } = [];
    public decimal CreditLimit { get; set; }
    public DateTime CreatedDate { get; set; }

    // ===== الملخص المالي المحسوب =====

    // رصيد الافتتاح المُدخَّل عند إنشاء العميل
    public decimal OpeningBalance { get; set; }

    // إجمالي قيمة فواتير البيع المؤكدة
    public decimal TotalInvoiced { get; set; }

    // إجمالي المقبوضات من العميل
    public decimal TotalReceived { get; set; }

    // الرصيد الحالي = OpeningBalance + TotalInvoiced - TotalReceived
    // قيمة موجبة = العميل مدين
    // قيمة سالبة = العميل دائن (زاد من دفعاته)
    public decimal CurrentBalance { get; set; }

    // إجمالي الفواتير غير المدفوعة بالكامل
    public decimal TotalOutstanding { get; set; }

    // المقبوضات التي لم تُخصص على فواتير بعد
    public decimal UnallocatedReceipts { get; set; }

    // عدد الفواتير المفتوحة
    public int OpenInvoicesCount { get; set; }

    // هل تجاوز الحد الائتماني؟
    public bool IsOverCreditLimit { get; set; }

    // آخر 5 فواتير
    public List<InvoiceSummaryDto> RecentInvoices { get; set; } = [];

    // آخر 5 مقبوضات
    public List<ReceiptSummaryDto> RecentReceipts { get; set; } = [];
}

/// <summary>
/// ملخص سريع لفاتورة — يُستخدم في Profile
/// </summary>
public class InvoiceSummaryDto
{
    public int Id { get; set; }
    public string InvoiceType { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal NetTotal { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal Remaining { get; set; }
}

/// <summary>
/// ملخص سريع لسند قبض — يُستخدم في Profile
/// </summary>
public class ReceiptSummaryDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public decimal UnallocatedAmount { get; set; }
    public string Method { get; set; } = string.Empty;
}

// ===================================
// ===== DTOs خاصة بالمورد =====
// ===================================

/// <summary>
/// بيانات إنشاء مورد جديد
/// </summary>
public class CreateSupplierDto
{
    // الاسم مطلوب
    public string Name { get; set; } = string.Empty;

    // كود داخلي اختياري — مثل: S001
    public string? Code { get; set; }

    // العنوان
    public string? Address { get; set; }

    // البريد الإلكتروني (اختياري)
    public string? Email { get; set; }

    // ملاحظات داخلية
    public string? Notes { get; set; }

    // رصيد الافتتاح (إن كان للمورد دين سابق من قبل النظام)
    public decimal OpeningBalance { get; set; } = 0;

    // قائمة الهواتف
    public List<PhoneDto> Phones { get; set; } = [];
}

/// <summary>
/// بيانات تعديل مورد موجود
/// </summary>
public class UpdateSupplierDto
{
    // الاسم مطلوب
    public string Name { get; set; } = string.Empty;

    // الكود الداخلي (اختياري)
    public string? Code { get; set; }

    // العنوان
    public string? Address { get; set; }

    // البريد الإلكتروني
    public string? Email { get; set; }

    // الملاحظات
    public string? Notes { get; set; }

    // قائمة الهواتف الجديدة (تُحدَّث بالكامل)
    public List<PhoneDto> Phones { get; set; } = [];
}

/// <summary>
/// بيانات عرض المورد (استجابة القراءة العامة)
/// </summary>
public class SupplierReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }

    // [LEGACY] الرصيد القديم — للتوافق الخلفي
    public decimal CashBalance { get; set; }

    // رصيد الافتتاح
    public decimal OpeningBalance { get; set; }

    // الرقم الأساسي للعرض السريع
    public string? PrimaryPhone { get; set; }

    // كل الأرقام
    public List<PhoneDto> Phones { get; set; } = [];

    public DateTime CreatedDate { get; set; }

    // ===== Audit Trail (حالة النشاط) =====
    public DateTime? StatusChangedAt { get; set; }
    public string? StatusChangedBy { get; set; }
}

/// <summary>
/// فلاتر البحث في الموردين
/// </summary>
public class SupplierFilterDto : PaginationQueryDto
{
    // فلتر حالة النشاط: null = الكل، true = نشط، false = معطّل
    public bool? IsActive { get; set; } = true;

    // فلتر: الموردون الذين عليهم مبالغ مستحقة
    public bool? HasPayable { get; set; }

    // فلتر: الموردون الذين لديهم فواتير مفتوحة
    public bool? HasOpenInvoices { get; set; }
}

/// <summary>
/// ملف المورد الشامل — يُعاد من GET /api/v1/suppliers/{id}/profile
/// </summary>
public class SupplierProfileDto
{
    // البيانات الأساسية للمورد
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public List<PhoneDto> Phones { get; set; } = [];
    public DateTime CreatedDate { get; set; }

    // ===== الملخص المالي المحسوب =====

    // رصيد الافتتاح
    public decimal OpeningBalance { get; set; }

    // إجمالي فواتير الشراء المؤكدة
    public decimal TotalPurchased { get; set; }

    // إجمالي المدفوعات للمورد
    public decimal TotalPaid { get; set; }

    // الرصيد الحالي = OpeningBalance + TotalPurchased - TotalPaid
    // قيمة موجبة = مدينون للمورد
    // قيمة سالبة = المورد دائن (زادت مدفوعاتنا)
    public decimal CurrentBalance { get; set; }

    // إجمالي الفواتير غير المدفوعة
    public decimal TotalOutstanding { get; set; }

    // المدفوعات التي لم تُخصص على فواتير بعد
    public decimal UnallocatedPayments { get; set; }

    // عدد الفواتير المفتوحة
    public int OpenInvoicesCount { get; set; }

    // آخر 5 فواتير
    public List<InvoiceSummaryDto> RecentInvoices { get; set; } = [];

    // آخر 5 مدفوعات
    public List<ReceiptSummaryDto> RecentPayments { get; set; } = [];
}

/// <summary>
/// DTO لكشف حساب المورد — يُعاد من GET /api/v1/suppliers/{id}/statement
/// </summary>
public class SupplierStatementDto
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public int DocumentId { get; set; }
    public decimal Debit { get; set; }   // علينا (فاتورة مشتريات = نحن مدينون)
    public decimal Credit { get; set; }  // لنا (سداد أو مرتجع)
    public decimal Balance { get; set; }
}


// ===== DTOs خاصة بتصنيف المنتج (Category) =====

public class CreateProductCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateProductCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ProductCategoryReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// ===== DTOs خاصة بالمنتج =====

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public string Unit { get; set; } = "قطعة";
    public string? SKU { get; set; }
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public int? CategoryId { get; set; }
    public double MinimumStock { get; set; }
    public double ReorderLevel { get; set; }
    public bool IsSellable { get; set; } = true;
    public bool IsPurchasable { get; set; } = true;

    // باركود اختياري — إذا أُرسل يُعامَل كباركود مصنعي
    public string? Barcode { get; set; }
    public BarcodeFormat BarcodeFormat { get; set; } = BarcodeFormat.EAN13;
}

public class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public string Unit { get; set; } = "قطعة";
    public string? SKU { get; set; }
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public int? CategoryId { get; set; }
    public double MinimumStock { get; set; }
    public double ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSellable { get; set; } = true;
    public bool IsPurchasable { get; set; } = true;

    // تحديث الباركود — مقيَّد بسياسة (Generated → Factory فقط)
    public string? Barcode { get; set; }
    public BarcodeFormat? BarcodeFormat { get; set; }
}

public class ProductReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public double StockQuantity { get; set; }
    public double MinimumStock { get; set; }
    public double ReorderLevel { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool IsActive { get; set; }
    public bool IsSellable { get; set; }
    public bool IsPurchasable { get; set; }
    public string? ThumbnailUrl { get; set; }

    // بيانات الباركود
    public string Barcode { get; set; } = string.Empty;
    public string BarcodeType { get; set; } = string.Empty;
    public string BarcodeFormat { get; set; } = string.Empty;
}

public class ProductSummaryDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double CurrentStock { get; set; }
    public decimal CurrentCost { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime? LastMovementDate { get; set; }
    public string? CategoryName { get; set; }
    public string? Brand { get; set; }
}

/// <summary>
/// بيانات ملصق الطباعة — يُعاد من endpoint /label
/// </summary>
public class ProductLabelDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string BarcodeFormat { get; set; } = string.Empty; // "EAN13" أو "CODE128"
}

// ===== DTOs خاصة بالفاتورة =====

public class CreateInvoiceDto
{
    public int InvoiceType { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public int BranchId { get; set; }
    public int? OriginalInvoiceId { get; set; }
    public decimal Tax { get; set; }
    public int? Status { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public decimal Discount { get; set; }
    public decimal Paid { get; set; }
    public bool IsInstallment { get; set; }
    public string? Notes { get; set; }
    public int? ReturnMode { get; set; }
    public string? ReturnReason { get; set; }
    public List<CreateInvoiceItemDto> Items { get; set; } = [];
}

public class CreateInvoiceItemDto
{
    public int ProductId { get; set; }
    public double Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int? OriginalInvoiceItemId { get; set; }
}

public class InvoiceReadDto
{
    public int Id { get; set; }
    public string InvoiceType { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? SupplierName { get; set; }
    public string MerchantName { get; set; } = string.Empty; // اسم الشركة مالكة الدفتر
    public DateTime Date { get; set; }
    public decimal TotalValue { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal NetTotal => TotalValue - Discount + Tax;
    public decimal Paid { get; set; } // Legacy
    public decimal AllocatedAmount { get; set; }
    public decimal Remaining => NetTotal - AllocatedAmount;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public bool IsInstallment { get; set; }
    public string? ReturnMode { get; set; }
    public string? ReturnReason { get; set; }
    public bool RequiresApproval { get; set; }
    public bool IsApproved { get; set; }
    public List<InvoiceItemReadDto> Items { get; set; } = [];
}

public class InvoiceItemReadDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public int? OriginalInvoiceItemId { get; set; }
}

public class ApproveReturnDto
{
    public string? Notes { get; set; }
}

public class RejectReturnDto
{
    public string Reason { get; set; } = string.Empty;
}

// ===== DTOs خاصة بالموظف =====

public class CreateEmployeeDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public string? Phone { get; set; }
    public EmployeeType Type { get; set; }
    // الفرع الذي سيُسجل الموظف فيه (اختياري - يأخذ الفرع الحالي)
    public int? CurrentBranchId { get; set; }
}

public class UpdateEmployeeDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public bool IsEnabled { get; set; }
    public string? Phone { get; set; }
    public EmployeeType Type { get; set; }
    public int? CurrentBranchId { get; set; }
}

public class EmployeeReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public bool IsEnabled { get; set; }
    public string? Phone { get; set; }
    public EmployeeType Type { get; set; }
    public int? CurrentBranchId { get; set; }
    public string? CurrentBranchName { get; set; }
}
// ===== DTOs خاصة بالعمليات المالية =====

/// <summary>
/// بيانات إنشاء معاملة نقدية جديدة (مصروف، قبض، دفع، إلخ)
/// </summary>
public class CreateCashTransactionDto
{
    public int Type { get; set; }           // TransactionType (1: In, 2: Out)
    public int SourceType { get; set; }     // TransactionSource (1: Customer, 2: Supplier, आदि)
    public decimal Value { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public int? RelatedEntityId { get; set; }
}

public class CashTransactionReadDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int? RelatedEntityId { get; set; }
    public string? RelatedEntityName { get; set; }
    public string MerchantName { get; set; } = string.Empty;
}
// ===== DTOs خاصة بلوحة التحكم (Dashboard) =====

/// <summary>
/// إحصائيات لوحة التحكم الأساسية
/// </summary>
public class DashboardStatsDto
{
    public decimal TodaySalesToal { get; set; }
    public decimal TodayExpensesTotal { get; set; }
    public double TodayInvoicesCount { get; set; }
    public decimal MonthlySalesTotal { get; set; }
    public decimal TotalCustomerDebts { get; set; }
    public decimal TotalSupplierDebts { get; set; }
    public List<TopProductDto> TopSellingProducts { get; set; } = [];
    public List<RecentInvoiceDto> RecentInvoices { get; set; } = [];
}

public class TopProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class FinancialSummaryDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit => TotalIncome - TotalExpenses;
}

public class DebtAlertDto
{
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class RecentInvoiceDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? PartnerName { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime Date { get; set; }
}
// ===== DTOs خاصة بالفروع (Branches) =====

public class CreateBranchDto
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateBranchDto
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

public class BranchReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

// ===== DTOs خاصة بالمخزون (Inventory / Stock Adjustments) =====

public class CreateStockAdjustmentDto
{
    public int BranchId { get; set; }
    public string? Notes { get; set; }
    public List<CreateStockAdjustmentItemDto> Items { get; set; } = new();
}

public class CreateStockAdjustmentItemDto
{
    public int ProductId { get; set; }
    // (+) لإضافة بضاعة، (-) لخصم بضاعة مقبولة بدلاً من تحديد IN/OUT كنص
    public double Quantity { get; set; } 
    public int ReasonType { get; set; }
}

public class StockAdjustmentReadDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public string UserName { get; set; } = string.Empty;
    public List<StockAdjustmentItemReadDto> Items { get; set; } = new();
}

public class StockAdjustmentItemReadDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public string ReasonType { get; set; } = string.Empty;
}

public class CreateStockTransferDto
{
    public int FromBranchId { get; set; }
    public int ToBranchId { get; set; }
    public string? Notes { get; set; }
    public List<CreateStockTransferItemDto> Items { get; set; } = new();
}

public class CreateStockTransferItemDto
{
    public int ProductId { get; set; }
    public double Quantity { get; set; } // يجب أن تكون قيمة موجبة
}

public class StockTransferReadDto
{
    public int Id { get; set; }
    public int FromBranchId { get; set; }
    public int ToBranchId { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public string UserName { get; set; } = string.Empty;
    public List<StockTransferItemReadDto> Items { get; set; } = new();
}

public class StockTransferItemReadDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double Quantity { get; set; }
}

public class StockTransactionReadDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public double BeforeQuantity { get; set; }
    public double AfterQuantity { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public string? ReasonType { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
}

// ===== DTOs خاصة بالموارد البشرية (HR: Attendance & Payroll) =====

public class AttendanceCreateDto
{
    public int EmployeeId { get; set; }
    public AttendanceStatus Status { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public decimal WorkingHours { get; set; }
    public string? Notes { get; set; }
}

public class AttendanceReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal WorkingHours { get; set; }
    public string? Notes { get; set; }
}

public class PayrollRunReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal NetSalary { get; set; }
    public bool IsLocked { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class PayrollRunDetailsDto : PayrollRunReadDto
{
    public decimal TotalAllowances { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal LoanDeductions { get; set; }
    public List<PayrollRunItemReadDto> Items { get; set; } = [];
}

public class PayrollRunItemReadDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Category { get; set; }
}

public class CreateLoanDto
{
    public int EmployeeId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public int NumberOfMonths { get; set; }
    public DateTime StartDate { get; set; }
    public string? Notes { get; set; }
}

public class LoanReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class EmployeeStatusResult
{
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive => Status == "Active" || Status == "Present";
}

// ===== DTOs خاصة بمسؤول النظام (System Admin: Audit & Plugins) =====

public class AuditLogReadDto
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Changes { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string UserName { get; set; } = string.Empty;
}

public class PluginReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

// ===== DTOs خاصة بالشركة (Company) =====

// ===== DTOs خاصة بالشركة (Company) =====

public class CompanyCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? BusinessType { get; set; }
    public bool HasBranches { get; set; }
    public bool ManageInventory { get; set; }
    public List<CompanyPhoneNumberDto> PhoneNumbers { get; set; } = [];
}

public class CompanyUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? BusinessType { get; set; }
    public bool HasBranches { get; set; }
    public bool ManageInventory { get; set; }
    
    // بيانات إضافية للتحديث فقط
    public string? TaxId { get; set; }
    public string? CommercialRegistry { get; set; }
    public string? OfficialEmail { get; set; }
    public string? Website { get; set; }
    public int? Currency { get; set; }
    public string? Description { get; set; }
}

public class CompanyReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? Address { get; set; }
    public string? BusinessType { get; set; }
    public bool HasBranches { get; set; }
    public bool ManageInventory { get; set; }
    
    public string? TaxId { get; set; }
    public string? CommercialRegistry { get; set; }
    public string? OfficialEmail { get; set; }
    public string? Website { get; set; }
    public string? Currency { get; set; }
    public string? Description { get; set; }
    
    public List<CompanyPhoneNumberDto> PhoneNumbers { get; set; } = [];
    public CompanyLogoDto? Logo { get; set; }

    // حقول إضافية تُعاد فقط عند إنشاء شركة جديدة
    public string? OwnerUserName { get; set; }
    public string? OwnerTempPassword { get; set; }
    public int? MainBranchId { get; set; }
}

public class CompanyPhoneNumberDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsWhatsApp { get; set; }
}

public class CompanyLogoDto
{
    public byte[] Content { get; set; } = [];
    public string? ContentType { get; set; }
}

// ===== DTOs خاصة بالتسويات (Settlements) =====

public class CreateSettlementDto
{
    public int SourceType { get; set; }     // (1: عميل، 2: مورد)
    public int RelatedEntityId { get; set; }
    public int Type { get; set; }           // (1: إضافة، 2: خصم)
    public int Reason { get; set; }         // سبب التسوية (من SettlementReason)
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
}

public class SettlementReadDto
{
    public int Id { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string? RelatedEntityName { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
}

// ===== DTOs خاصة بإدارة الأدوار والصلاحيات =====

/// <summary>DTO لإنشاء دور جديد</summary>
public class CreateRoleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>DTO لتعديل دور موجود</summary>
public class UpdateRoleDto
{
    public string? Description { get; set; }
}

/// <summary>DTO لعرض الدور</summary>
public class RoleReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = [];
    public int UsersCount { get; set; }
}

/// <summary>DTO لتحديث صلاحيات الدور</summary>
public class UpdateRolePermissionsDto
{
    public List<string> Permissions { get; set; } = [];
}

/// <summary>DTO لعرض بيانات مستخدم في إدارة المستخدمين</summary>
public class UserReadDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
}

/// <summary>DTO لإنشاء مستخدم جديد</summary>
public class CreateUserDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? BranchId { get; set; }
}

/// <summary>DTO لتعيين أدوار للمستخدم</summary>
public class AssignRolesDto
{
    public List<string> Roles { get; set; } = [];
}

/// <summary>DTO لبيانات المستخدم الحالي (GET /auth/me)</summary>
public class CurrentUserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
    public CompanyReadDto? Company { get; set; }
}

// ===== DTOs خاصة بالمقبوضات والمدفوعات (Finance Services) =====

public class CreateReceiptDto
{
    public int PartnerId { get; set; } // CustomerId أو SupplierId حسَب نوع الخدمة
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public PaymentMethod Method { get; set; }
    public string? Notes { get; set; }
    // إذا كانت true يتم عمل Auto-Allocate على أقدم الفواتير المفتوحة مباشرة
    public bool AutoAllocate { get; set; } = false;
}

public class ManualAllocationDto
{
    public int InvoiceId { get; set; }
    public decimal Amount { get; set; }
}

public class AllocateReceiptDto
{
    public int ReceiptId { get; set; }
    public List<ManualAllocationDto> Allocations { get; set; } = [];
}

public class ReceiptReadDto
{
    public int Id { get; set; }
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public decimal UnallocatedAmount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<AllocationReadDto> Allocations { get; set; } = [];
}

public class AllocationReadDto
{
    public int InvoiceId { get; set; }
    public decimal Amount { get; set; }
}

public class CustomerStatementDto
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public int DocumentId { get; set; }
    public decimal Debit { get; set; }   // عليه (مثلُا فاتورة مبيعات)
    public decimal Credit { get; set; }  // له (مثلاً سند قبض أو مرتجع)
    public decimal Balance { get; set; }
}

// ===== DTOs خاصة بالتنبيهات (Alerts) =====
public class LowStockAlertDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string Unit { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public double MinimumStock { get; set; }
    public double ShortageQuantity { get; set; }
}

public class OverdueCustomerAlertDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalOverdue { get; set; }
    public int OldestInvoiceDays { get; set; }
    public int InvoiceCount { get; set; }
}

public class HighDebtCustomerAlertDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal NetBalance { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal ExcessAmount { get; set; }
}

// ===== DTOs خاصة بالورديات (Shifts) =====
public class OpenShiftDto
{
    public decimal OpeningBalance { get; set; }
}

public class CloseShiftDto
{
    public decimal ActualClosingBalance { get; set; }
}

public class ShiftReadDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalCashIn { get; set; }
    public decimal TotalCashOut { get; set; }
    public decimal? ClosingBalance { get; set; }
    public decimal? ActualClosingBalance { get; set; }
    public decimal? Difference { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class BranchStockInitializationResultDto
{
    public int CompaniesProcessed { get; set; }
    public int TransactionsProcessed { get; set; }
    public int BranchStockRowsCreated { get; set; }
    public int BranchStockRowsUpdated { get; set; }
    public int SkippedTransactions { get; set; }
    public double DurationMs { get; set; }
    public List<string> Warnings { get; set; } = new();
}
