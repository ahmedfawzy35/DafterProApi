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
/// </summary>
public interface ICustomerService
{
    Task<PagedResult<DTOs.CustomerReadDto>> GetAllAsync(DTOs.PaginationQueryDto query);
    Task<DTOs.CustomerReadDto?> GetByIdAsync(int id);
    Task<DTOs.CustomerReadDto> CreateAsync(DTOs.CreateCustomerDto dto);
    Task UpdateAsync(int id, DTOs.UpdateCustomerDto dto);
    Task DeleteAsync(int id);
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
    Task<DTOs.DashboardStatsDto> GetDailyStatsAsync();
    Task<DTOs.FinancialSummaryDto> GetFinancialSummaryAsync();
    Task<List<DTOs.TopProductDto>> GetTopSellingProductsAsync(int count = 5);
    Task<List<DTOs.DebtAlertDto>> GetDebtAlertsAsync();
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

    Task CreateAdjustmentAsync(DTOs.CreateStockAdjustmentDto dto);
    Task RegisterInitialStockAsync(int productId, double quantity);
}

/// <summary>
/// خدمة تسجيل الحضور والانصراف
/// </summary>
public interface IAttendanceService
{
    Task RecordAttendanceAsync(DTOs.AttendanceRecordDto dto);
    Task<List<DTOs.AttendanceReadDto>> GetAllAsync(DateTime date);
}

/// <summary>
/// خدمة إدارة الرواتب والمدفوعات
/// </summary>
public interface IPayrollService
{
    Task<List<DTOs.PayrollReadDto>> GetAllAsync(DateTime month);
    Task GeneratePayrollAsync(DTOs.CreatePayrollDto dto);
    Task PaySalaryAsync(int payrollId);
}

/// <summary>
/// خدمة سجلات التغيير (Audit Logs)
/// </summary>
public interface IAuditLogService
{
    Task<PagedResult<DTOs.AuditLogReadDto>> GetAllAsync(
        DTOs.PaginationQueryDto query,
        string? entityName = null,
        int? userId = null);
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
    Task<DTOs.CompanyReadDto> GetMyCompanyAsync(bool includeLogo = false);
    Task<DTOs.CompanyReadDto> CreateAsync(DTOs.CompanyCreateDto dto);
    Task UpdateMyCompanyAsync(DTOs.CompanyUpdateDto dto);
    Task UploadLogoAsync(byte[] logoContent, string contentType);
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
