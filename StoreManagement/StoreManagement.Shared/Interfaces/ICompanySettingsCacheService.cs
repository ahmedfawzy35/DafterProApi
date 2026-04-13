using StoreManagement.Shared.DTOs.Settings;

namespace StoreManagement.Shared.Interfaces;

public interface ICompanySettingsCacheService
{
    Task<SettingsSnapshotDto?> GetAsync(int companyId);
    Task SetAsync(int companyId, SettingsSnapshotDto settings);
    Task InvalidateAsync(int companyId);
}
