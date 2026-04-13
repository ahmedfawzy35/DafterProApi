using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using StoreManagement.Shared.DTOs.Settings;
using StoreManagement.Shared.Entities.Configuration;
using StoreManagement.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace StoreManagement.Services.Services;

public class CompanySettingsService : ICompanySettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICompanySettingsCacheService _cache;

    public CompanySettingsService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ICompanySettingsCacheService cache)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _cache = cache;
    }

    public async Task<SettingsSnapshotDto> GetSettingsSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var companyId = _currentUserService.CompanyId ?? 0;
        if (companyId == 0) return new SettingsSnapshotDto();

        var cachedSnapshot = await _cache.GetAsync(companyId);
        if (cachedSnapshot != null)
        {
            return cachedSnapshot;
        }

        var settings = await GetOrCreateSettingsAsync();
        var snapshot = _mapper.Map<SettingsSnapshotDto>(settings);

        snapshot.EnableHR = settings.EnableEmployees;

        await _cache.SetAsync(companyId, snapshot);

        return snapshot;
    }

    public async Task<CompanySettingsDto> GetCompanySettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync();
        return _mapper.Map<CompanySettingsDto>(settings);
    }

    public async Task UpdateSalesSettingsAsync(UpdateSalesSettingsDto dto, CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync();
        _mapper.Map(dto, settings);
        await SaveChangesAndInvalidateCacheAsync(settings);
    }

    public async Task UpdateInventorySettingsAsync(UpdateInventorySettingsDto dto, CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync();
        _mapper.Map(dto, settings);
        await SaveChangesAndInvalidateCacheAsync(settings);
    }

    public async Task UpdateReturnsSettingsAsync(UpdateReturnsSettingsDto dto, CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync();
        _mapper.Map(dto, settings);
        await SaveChangesAndInvalidateCacheAsync(settings);
    }

    public async Task UpdateInstallmentsSettingsAsync(UpdateInstallmentsSettingsDto dto, CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync();
        _mapper.Map(dto, settings);
        await SaveChangesAndInvalidateCacheAsync(settings);
    }

    public async Task UpdateApprovalsSettingsAsync(UpdateApprovalsSettingsDto dto, CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync();
        _mapper.Map(dto, settings);
        await SaveChangesAndInvalidateCacheAsync(settings);
    }

    public async Task InitializeDefaultSettingsAsync(int companyId, CancellationToken cancellationToken = default)
    {
        // 1. Verify company exists to avoid FK error
        var companyExists = await _unitOfWork.Repository<StoreManagement.Shared.Entities.Configuration.Company>()
            .Query()
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id == companyId);
            
        if (!companyExists) return;

        var repo = _unitOfWork.Repository<CompanySettings>();
        
        // 2. Check if already initialized
        var exists = await repo.Query()
            .IgnoreQueryFilters()
            .AnyAsync(x => x.CompanyId == companyId);
            
        if (exists) return;

        var defaultSettings = new CompanySettings
        {
            CompanyId = companyId,
            EnableSales = true,
            EnableReturns = true,
            EnableInventory = true,
            ReturnMode = StoreManagement.Shared.Enums.ReturnProcessMode.Simple,
            EnableQuickSaleScreen = true,
            CurrencyCode = "EGP",
            DecimalPlaces = 2
        };

        await repo.AddAsync(defaultSettings);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<CompanySettings> GetOrCreateSettingsAsync()
    {
        var companyId = _currentUserService.CompanyId ?? 0;
        if (companyId == 0) throw new UnauthorizedAccessException("CompanyId is not valid.");

        var repo = _unitOfWork.Repository<CompanySettings>();
        var settings = await repo.Query()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId);

        if (settings == null)
        {
            await InitializeDefaultSettingsAsync(companyId);
            settings = await repo.Query()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId);
        }

        return settings ?? throw new InvalidOperationException("Failed to initialize company settings.");
    }

    private async Task SaveChangesAndInvalidateCacheAsync(CompanySettings settings)
    {
        settings.SettingsVersion++; 
        
        var repo = _unitOfWork.Repository<CompanySettings>();
        repo.Update(settings);
        await _unitOfWork.SaveChangesAsync();
        
        await _cache.InvalidateAsync(settings.CompanyId);
    }
}
