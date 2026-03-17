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
    Task<DTOs.InvoiceReadDto> CreateAsync(DTOs.CreateInvoiceDto dto);
    Task DeleteAsync(int id);
}
