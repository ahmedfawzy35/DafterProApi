using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities;
using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Interfaces;

/// <summary>
/// خدمة التحقق من حالة اشتراك الشركة
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// التحقق من أن اشتراك الشركة نشط وغير منتهي
    /// </summary>
    Task<bool> IsSubscriptionActiveAsync(int companyId);

    /// <summary>
    /// الحصول على بيانات الاشتراك الحالية (مع Caching)
    /// </summary>
    Task<CompanySubscription?> GetActiveSubscriptionAsync(int companyId);

    /// <summary>
    /// الحصول على تفاصيل حالة الاشتراك الشاملة (مع المميزات)
    /// </summary>
    Task<SubscriptionStatusDto?> GetSubscriptionStatusAsync(int companyId);
}

/// <summary>
/// خدمة التحقق من الميزات المتاحة وفق خطة الاشتراك
/// </summary>
public interface IFeatureService
{
    /// <summary>
    /// هل الميزة متاحة للشركة؟ (يأخذ في الاعتبار Plan + Override)
    /// </summary>
    Task<bool> IsFeatureEnabledAsync(int companyId, string featureKey);
}

/// <summary>
/// خدمة التخزين المؤقت الموحدة (تدعم Redis + MemoryCache fallback)
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
}

/// <summary>
/// خدمة رفع وتخزين الملفات (قابلة للتبديل بين Local/S3/Azure)
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// حفظ ملف وإرجاع المسار
    /// </summary>
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string entityFolder, int companyId);

    /// <summary>
    /// حذف ملف
    /// </summary>
    Task DeleteFileAsync(string filePath);

    /// <summary>
    /// التحقق من صحة الملف (الحجم والامتداد)
    /// </summary>
    (bool IsValid, string? Error) ValidateFile(string fileName, long fileSizeBytes);
}

/// <summary>
/// خدمة الـ Outbox لحفظ Events المهمة
/// </summary>
public interface IOutboxService
{
    Task PublishAsync(string eventType, object payload);
}

/// <summary>
/// خدمة إدارة العملاء (Business Logic)
/// عمليات CRUD والتفعيل/التعطيل والبحث وملف العميل
/// </summary>
public interface ICustomerService
{
    // جلب قائمة العملاء مع Filters و Pagination
    Task<PagedResult<DTOs.CustomerReadDto>> GetAllAsync(DTOs.CustomerFilterDto filter);

    // جلب عميل بال Id
    Task<DTOs.CustomerReadDto?> GetByIdAsync(int id);

    // إنشاء عميل جديد
    Task<DTOs.CustomerReadDto> CreateAsync(DTOs.CreateCustomerDto dto);

    // تعديل بيانات عميل
    Task UpdateAsync(int id, DTOs.UpdateCustomerDto dto);

    // حذف آمن — يتحقق من عدم وجود مستندات قبل الحذف
    Task DeleteAsync(int id);

    // تفعيل العميل (IsActive = true)
    Task ActivateAsync(int id);

    // تعطيل العميل (IsActive = false)
    Task DeactivateAsync(int id);

    // ملف العميل الشامل — بيانات + ملخص مالي + آخر حركات
    Task<DTOs.CustomerProfileDto> GetProfileAsync(int id);
}

/// <summary>
/// خدمة إدارة الموردين (Business Logic)
/// دائرة مستقلة بنفس مستوى ICustomerService
/// </summary>
public interface ISupplierService
{
    // جلب قائمة الموردين مع Filters و Pagination
    Task<PagedResult<DTOs.SupplierReadDto>> GetAllAsync(DTOs.SupplierFilterDto filter);

    // جلب مورد بال Id
    Task<DTOs.SupplierReadDto?> GetByIdAsync(int id);

    // إنشاء مورد جديد
    Task<DTOs.SupplierReadDto> CreateAsync(DTOs.CreateSupplierDto dto);

    // تعديل بيانات مورد
    Task UpdateAsync(int id, DTOs.UpdateSupplierDto dto);

    // حذف آمن — يتحقق من عدم وجود مستندات
    Task DeleteAsync(int id);

    // تفعيل المورد
    Task ActivateAsync(int id);

    // تعطيل المورد
    Task DeactivateAsync(int id);

    // ملف المورد الشامل
    Task<DTOs.SupplierProfileDto> GetProfileAsync(int id);
}

/// <summary>
/// خدمة إدارة الفواتير (Business Logic)
/// </summary>
public interface IInvoiceService
{
    Task<PagedResult<DTOs.InvoiceReadDto>> GetAllAsync(
        DTOs.PaginationQueryDto query,
        Enums.InvoiceType? type,
        DateTime? from,
        DateTime? to);
    Task<DTOs.InvoiceReadDto?> GetByIdAsync(int id);
    Task<DTOs.InvoiceReadDto> CreateAsync(DTOs.CreateInvoiceDto dto);
    Task DeleteAsync(int id);
    Task CancelAsync(int id);
}

/// <summary>
/// خدمة إدارة المرتجعات (المرجعية واليدوية)
/// </summary>
public interface IReturnService
{
    Task<DTOs.InvoiceReadDto> CreateReferencedReturnAsync(DTOs.CreateInvoiceDto dto, Enums.InvoiceType returnType);
    Task<DTOs.InvoiceReadDto> CreateManualReturnAsync(DTOs.CreateInvoiceDto dto, Enums.InvoiceType returnType);
    Task<DTOs.InvoiceReadDto> ApproveManualReturnAsync(int invoiceId, string? notes);
    Task RejectManualReturnAsync(int invoiceId, string? reason);
    Task<Common.PagedResult<DTOs.InvoiceReadDto>> SearchForOriginalInvoiceAsync(
        int? customerId, int? supplierId, int? productId,
        DateTime? from, DateTime? to, DTOs.PaginationQueryDto query);
}
/// <summary>
/// خدمة إدارة المعاملات النقدية (Business Logic)
/// </summary>
public interface ICashTransactionService
{
    Task<PagedResult<DTOs.CashTransactionReadDto>> GetAllAsync(
        DTOs.PaginationQueryDto query,
        Enums.TransactionType? type,
        Enums.TransactionSource? source,
        DateTime? from,
        DateTime? to);

    Task<DTOs.CashTransactionReadDto?> GetByIdAsync(int id);
    Task<DTOs.CashTransactionReadDto> CreateAsync(DTOs.CreateCashTransactionDto dto);
    Task UpdateAsync(int id, DTOs.CreateCashTransactionDto dto);
    Task DeleteAsync(int id);
}
/// <summary>
/// خدمة لوحة التحكم (إحصائيات وملخصات)
/// </summary>
/// <summary>
/// خدمة لوحة التحكم (إحصائيات وملخصات)
/// </summary>
public interface IDashboardService
{
    // النظام القديم (يحتفظ به للتوافقية حتى يتم نقله لواجهات أخرى)
    Task<DTOs.DashboardStatsDto> GetDailyStatsAsync();
    Task<DTOs.FinancialSummaryDto> GetFinancialSummaryAsync();
    Task<List<DTOs.TopProductDto>> GetTopSellingProductsAsync(int count = 5);
    Task<List<DTOs.DebtAlertDto>> GetDebtAlertsAsync();

    // النظام الحديث
    Task<DTOs.DashboardKpiDto> GetKpisAsync();
    Task<List<DTOs.CustomerReadDto>> GetTopCustomersAsync(int count = 5);
    Task<List<DTOs.ProductReadDto>> GetLowStockProductsAsync();

    // لوحة تحكم الفرع (Branch KPIs)
    Task<DTOs.BranchDashboardKpiDto> GetBranchKpisAsync(int? branchId = null);
}

/// <summary>
/// خدمة التقارير الشاملة (Business Logic)
/// </summary>
public interface IReportService
{
    // تقارير أعمار الديون
    Task<List<DTOs.AgingReportRowDto>> GetCustomerAgingReportAsync(bool excludeZeroBalances = true);
    Task<List<DTOs.AgingReportRowDto>> GetSupplierAgingReportAsync(bool excludeZeroBalances = true);
    
    // تقارير المبيعات والأرباح
    Task<DTOs.SalesSummaryDto> GetSalesSummaryAsync(DateTime? from, DateTime? to);
    Task<Common.PagedResult<DTOs.InvoiceProfitDto>> GetInvoiceProfitabilityAsync(DTOs.PaginationQueryDto query, DateTime? from, DateTime? to);

    // ===== تقارير المخزون والفروع =====
    Task<Common.PagedResult<DTOs.StockPerBranchReportDto>> GetStockPerBranchReportAsync(DTOs.PaginationQueryDto query, int? branchId, int? productId);
    Task<Common.PagedResult<DTOs.BranchInventoryMovementReportDto>> GetBranchInventoryMovementsReportAsync(DTOs.PaginationQueryDto query, int? branchId, int? productId, DateTime? from, DateTime? to);
    Task<DTOs.ProductStockDistributionDto> GetProductStockDistributionAsync(int productId);
}

/// <summary>
/// خدمة إدارة الفروع (Business Logic)
/// </summary>
public interface IBranchService
{
    Task<List<DTOs.BranchReadDto>> GetAllAsync();
    Task<DTOs.BranchReadDto?> GetByIdAsync(int id);
    Task<DTOs.BranchReadDto> CreateAsync(DTOs.CreateBranchDto dto);
    Task UpdateAsync(int id, DTOs.UpdateBranchDto dto);
    Task DeleteAsync(int id);
    Task<string> GetBranchStatusAsync(int id);
}
/// <summary>
/// خدمة إدارة المخزون (Business Logic)
/// </summary>
public interface IInventoryService
{
    Task<PagedResult<DTOs.StockTransactionReadDto>> GetHistoryAsync(
        DTOs.PaginationQueryDto query,
        int? productId,
        DateTime? from,
        DateTime? to);

    // ===== التسويات (Adjustments) =====
    Task<DTOs.StockAdjustmentReadDto> CreateStockAdjustmentAsync(DTOs.CreateStockAdjustmentDto dto);
    Task<DTOs.StockAdjustmentReadDto?> GetAdjustmentByIdAsync(int id);
    Task<PagedResult<DTOs.StockAdjustmentReadDto>> GetAllAdjustmentsAsync(DTOs.PaginationQueryDto query);

    // ===== التحويلات بين الفروع (Transfers) =====
    Task<DTOs.StockTransferReadDto> CreateStockTransferAsync(DTOs.CreateStockTransferDto dto);
    Task<DTOs.StockTransferReadDto?> GetTransferByIdAsync(int id);
    Task<PagedResult<DTOs.StockTransferReadDto>> GetAllTransfersAsync(DTOs.PaginationQueryDto query);

    // Legacy — يُستخدم لتسجيل الرصيد الافتتاحي
    Task RegisterInitialStockAsync(int productId, decimal quantity, int branchId);

    // تسجيل الكميات الخاصة بالفواتير (لا تتم إلا من خلال InvoiceService)
    Task ProcessInvoiceStockAsync(int invoiceId, int invoiceItemId, int productId, decimal quantity, int branchId, Enums.InvoiceType invoiceType, string notes);

    // عكس كميات الفواتير في حالة الإلغاء
    Task ReverseInvoiceStockAsync(int invoiceId, int invoiceItemId, int productId, decimal quantity, int branchId, Enums.InvoiceType originalInvoiceType, string notes);
}

/// <summary>
/// خدمة المنتجات والأعمال المعقدة (Business Logic)
/// </summary>
public interface IProductService
{
    // التحقق من أن الباركود يمكن تحديثه وأنه غير مستخدم بحركات أخرى
    Task ValidateCanUpdateBarcodeAsync(int productId, string newBarcode);

    // تحديث تكلفة المنتج وتسجيل التغير التاريخي لتكلفة الشراء
    Task UpdateCostPriceAsync(int productId, decimal newCost, string reason);
}

/// <summary>
/// خدمة تسجيل الحضور والانصراف
/// </summary>
public interface IAttendanceService
{
    Task RecordAttendanceAsync(DTOs.AttendanceCreateDto dto);
    Task<List<DTOs.AttendanceReadDto>> GetAllAsync(DateTime date);
    Task<List<DTOs.AttendanceReadDto>> GetEmployeeAttendanceAsync(int employeeId, int month, int year);
}

/// <summary>
/// خدمة إدارة الرواتب (Snapshots & Payroll Runs)
/// </summary>
public interface IPayrollService
{
    // الحصول على كافة تشغيلات الرواتب لشهر محدد
    Task<List<DTOs.PayrollRunReadDto>> GetPayrollRunsAsync(int month, int year);

    // توليد (حساب) الرواتب وحفظها كـ Snapshot
    Task GeneratePayrollRunAsync(int month, int year, List<int>? employeeIds = null);

    // قفل (اعتماد) الرواتب لمنع التعديل وترحيلها للحسابات
    Task LockAndPayPayrollAsync(int month, int year);

    // الحصول على تفاصيل راتب محدد (Detailed Breakdown)
    Task<DTOs.PayrollRunDetailsDto> GetPayrollDetailsAsync(int payrollRunId);
}

/// <summary>
/// خدمة تحديد حالة الموظف في تاريخ محدد (Domain Service)
/// </summary>
public interface IEmployeeStatusResolver
{
    Task<EmployeeStatusResult> GetStatusAsync(int employeeId, DateTime date);
    Task<List<EmployeeStatusResult>> GetMonthlyStatusAsync(int employeeId, int month, int year);
}

/// <summary>
/// خدمة إدارة القروض والأقساط
/// </summary>
public interface ILoanService
{
    Task<DTOs.LoanReadDto> CreateLoanAsync(DTOs.CreateLoanDto dto);
    Task<List<DTOs.LoanReadDto>> GetEmployeeLoansAsync(int employeeId);
    Task ProcessLoanDeductionAsync(int payrollRunId, int month, int year);
}

/// <summary>
/// خدمة سياسات الشركة
/// </summary>
public interface IPolicyService
{
    Task<string> GetPolicyValueAsync(string key, string defaultValue = "");
    Task<T> GetPolicyValueAsync<T>(string key, T defaultValue = default!);
    Task SetPolicyValueAsync(string key, string value, PolicyDataType dataType);
    Task SeedDefaultPoliciesAsync(int companyId);
}

/// <summary>
/// خدمة سجلات التغيير (Audit Logs)
/// </summary>
public interface IAuditLogService
{
    Task<PagedResult<DTOs.AuditLogReadDto>> GetAllAsync(
        DTOs.PaginationQueryDto query,
        string? entityName = null,
        int? userId = null,
        string? entityId = null);
}

/// <summary>
/// خدمة إدارة الإضافات (Plugins)
/// </summary>
public interface IPluginService
{
    Task<List<DTOs.PluginReadDto>> GetAllAsync();
    Task TogglePluginAsync(int pluginId, bool enabled);
}

/// <summary>
/// خدمة إدارة بيانات الشركة (الإعدادات الأساسية)
/// </summary>
public interface ICompanyService
{
    Task<List<DTOs.CompanyReadDto>> GetAllAsync();
    Task<DTOs.CompanyReadDto?> GetMyCompanyAsync(bool includeLogo = false);
    Task<DTOs.CompanyReadDto> CreateAsync(DTOs.CompanyCreateDto dto);
    Task UpdateMyCompanyAsync(DTOs.CompanyUpdateDto dto);
    Task UploadLogoAsync(byte[] logoContent, string contentType);
    Task<List<DTOs.UserReadDto>> GetCompanyUsersAsync(int companyId);
}

/// <summary>
/// خدمة تسويات الحسابات (خصم أو إضافة رصيد يدوي)
/// </summary>
public interface ISettlementService
{
    Task<PagedResult<DTOs.SettlementReadDto>> GetAllAsync(
        DTOs.PaginationQueryDto query,
        Enums.SettlementSource? source,
        Enums.SettlementType? type,
        DateTime? from,
        DateTime? to);

    Task<DTOs.SettlementReadDto> CreateAsync(DTOs.CreateSettlementDto dto);
}

/// <summary>
/// خدمة الإدارة المالية والمقبوضات/المدفوعات (نظام الحساب المفتوح للعملاء والموردين)
/// </summary>
public interface IFinanceService
{
    // ===== المقبوضات من العميل =====
    Task<DTOs.ReceiptReadDto> CreateCustomerReceiptAsync(DTOs.CreateReceiptDto dto, int? explicitBranchId = null);

    /// <summary>
    /// إنشاء قيد إرجاع في حساب العميل (مرتجع نقدي أو إشعار دائن).
    /// createCashTransaction = true  → مرتجع نقدي (Amount موجب دائمًا، Kind=Refund).
    /// createCashTransaction = false → إشعار دائن بدون حركة نقدية (Kind=CreditNote).
    /// </summary>
    Task<DTOs.ReceiptReadDto> CreateCustomerReturnSettlementAsync(DTOs.CreateReceiptDto dto, int? explicitBranchId = null, bool createCashTransaction = true, int? returnInvoiceId = null);

    [Obsolete("Use CreateCustomerReturnSettlementAsync instead.")]
    Task<DTOs.ReceiptReadDto> CreateCustomerRefundAsync(DTOs.CreateReceiptDto dto, int? explicitBranchId = null);

    Task AllocateCustomerReceiptAsync(DTOs.AllocateReceiptDto dto);
    Task AllocateDirectToInvoiceAsync(int receiptId, int invoiceId, decimal amount);
    Task VoidCustomerReceiptAsync(int receiptId);

    // ===== المدفوعات للمورد =====
    Task<DTOs.ReceiptReadDto> CreateSupplierPaymentAsync(DTOs.CreateReceiptDto dto, int? explicitBranchId = null);

    /// <summary>
    /// إنشاء قيد إرجاع في حساب المورد (مرتجع نقدي أو إشعار دائن).
    /// createCashTransaction = true  → مرتجع نقدي (Kind=Refund).
    /// createCashTransaction = false → إشعار دائن (Kind=CreditNote).
    /// </summary>
    Task<DTOs.ReceiptReadDto> CreateSupplierReturnSettlementAsync(DTOs.CreateReceiptDto dto, int? explicitBranchId = null, bool createCashTransaction = true, int? returnInvoiceId = null);

    [Obsolete("Use CreateSupplierReturnSettlementAsync instead.")]
    Task<DTOs.ReceiptReadDto> CreateSupplierRefundAsync(DTOs.CreateReceiptDto dto, int? explicitBranchId = null);

    Task AllocateSupplierPaymentAsync(DTOs.AllocateReceiptDto dto);
    Task AllocateDirectToSupplierInvoiceAsync(int paymentId, int invoiceId, decimal amount);
    Task VoidSupplierPaymentAsync(int paymentId);

    // ===== كشوفات وشاشات العميل =====

    // كشف حساب العميل بالفترة مع Pagination
    Task<DTOs.StatementPagedResult<DTOs.CustomerStatementDto>> GetCustomerStatementAsync(int customerId, DTOs.StatementQueryDto query);

    // فواتير العميل المفتوحة (التي لم تُدفع بالكامل)
    Task<List<DTOs.InvoiceReadDto>> GetOpenCustomerInvoicesAsync(int customerId);

    // مقبوضات العميل التي لم تُخصص
    Task<List<DTOs.ReceiptReadDto>> GetUnallocatedCustomerReceiptsAsync(int customerId);

    // حساب الرصيد الحالي للعميل من مصادر النظام المالي (Receipts/Invoices)
    Task<decimal> GetCustomerCurrentBalanceAsync(int customerId);

    // ===== كشوفات وشاشات المورد =====

    // كشف حساب المورد بالفترة مع Pagination
    Task<DTOs.StatementPagedResult<DTOs.SupplierStatementDto>> GetSupplierStatementAsync(int supplierId, DTOs.StatementQueryDto query);

    // فواتير المورد المفتوحة (لم تُدفع بالكامل)
    Task<List<DTOs.InvoiceReadDto>> GetOpenSupplierInvoicesAsync(int supplierId);

    // مدفوعات المورد التي لم تُخصص
    Task<List<DTOs.ReceiptReadDto>> GetUnallocatedSupplierPaymentsAsync(int supplierId);

    // حساب الرصيد الحالي للمورد
    Task<decimal> GetSupplierCurrentBalanceAsync(int supplierId);
}

// ===== واجهات الورديات والتنبيهات (Enterprise Upgrades) =====

/// <summary>
/// خدمة إدارة الورديات (Shift Management) الخاصة بالكاشير والفرع
/// </summary>
public interface IShiftService
{
    Task<DTOs.ShiftReadDto> OpenShiftAsync(DTOs.OpenShiftDto dto);
    Task<DTOs.ShiftReadDto> CloseShiftAsync(int shiftId, DTOs.CloseShiftDto dto);
    Task<DTOs.ShiftReadDto?> GetCurrentShiftAsync();
    Task<int?> GetCurrentShiftIdAsync();
    Task<PagedResult<DTOs.ShiftReadDto>> GetAllShiftsAsync(DTOs.PaginationQueryDto query);
    Task<DTOs.ShiftReadDto> GetShiftByIdAsync(int id);
}

/// <summary>
/// خدمة التنبيهات مع Paging و Caching
/// </summary>
public interface IAlertService
{
    Task<PagedResult<DTOs.LowStockAlertDto>> GetLowStockAlertsAsync(DTOs.PaginationQueryDto query, int? branchId = null);
    Task<PagedResult<DTOs.OverdueCustomerAlertDto>> GetOverdueInvoicesAlertsAsync(DTOs.PaginationQueryDto query, int dayThreshold = 30);
    Task<PagedResult<DTOs.HighDebtCustomerAlertDto>> GetHighDebtCustomersAlertsAsync(DTOs.PaginationQueryDto query);
}

/// <summary>
/// خدمة الفترات المحاسبية والقفل للعمليات المالية
/// </summary>
public interface IAccountingPeriodService
{
    // تحقق مركزي لوقاية أي عملية من التنفيذ في فترة مغلقة
    Task EnsureDateIsOpenAsync(int companyId, DateTime operationDate);
    
    // إنشاء فترة جديدة أو إغلاقها
    // (يمكن استكمال هذه العمليات لاحقاً في لوحة التحكم الإدارية)
}
