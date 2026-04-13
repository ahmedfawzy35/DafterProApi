using StoreManagement.Shared.DTOs.Settings;

namespace StoreManagement.Shared.Interfaces;

public interface ICompanySettingsService
{
    // إحضار نسخة سريعة (Snapshot) في الـ Bootstrap للـ Frontend (مخبأة - Cached)
    Task<SettingsSnapshotDto> GetSettingsSnapshotAsync(CancellationToken cancellationToken = default);

    // إحضار الإعدادات الكاملة
    Task<CompanySettingsDto> GetCompanySettingsAsync(CancellationToken cancellationToken = default);

    // تحديثات مجزأة لكل قسم (Partial Updates)
    Task UpdateSalesSettingsAsync(UpdateSalesSettingsDto dto, CancellationToken cancellationToken = default);
    Task UpdateInventorySettingsAsync(UpdateInventorySettingsDto dto, CancellationToken cancellationToken = default);
    Task UpdateReturnsSettingsAsync(UpdateReturnsSettingsDto dto, CancellationToken cancellationToken = default);
    Task UpdateInstallmentsSettingsAsync(UpdateInstallmentsSettingsDto dto, CancellationToken cancellationToken = default);
    Task UpdateApprovalsSettingsAsync(UpdateApprovalsSettingsDto dto, CancellationToken cancellationToken = default);
    
    // الدالة المسئولة عن تهيئة إعدادات الشركة الجديدة عند التسجيل الديفولت
    Task InitializeDefaultSettingsAsync(int companyId, CancellationToken cancellationToken = default);
}
