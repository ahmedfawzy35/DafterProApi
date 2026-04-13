using Microsoft.Extensions.Caching.Memory;
using StoreManagement.Shared.DTOs.Settings;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

public class CompanySettingsCacheService : ICompanySettingsCacheService
{
    private readonly IMemoryCache _cache;
    private static readonly string CachePrefix = "CS_";
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(2);

    public CompanySettingsCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    private string GetKey(int companyId) => $"{CachePrefix}{companyId}";

    public Task<SettingsSnapshotDto?> GetAsync(int companyId)
    {
        _cache.TryGetValue(GetKey(companyId), out SettingsSnapshotDto? result);
        return Task.FromResult(result);
    }

    public Task SetAsync(int companyId, SettingsSnapshotDto settings)
    {
        _cache.Set(GetKey(companyId), settings, DefaultExpiration);
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(int companyId)
    {
        _cache.Remove(GetKey(companyId));
        return Task.CompletedTask;
    }
}
