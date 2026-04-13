using StoreManagement.Shared.Interfaces;
using StoreManagement.Shared.Enums;

namespace StoreManagement.Services.Services.Policies;

public class ReturnPolicyService : IReturnPolicyService
{
    private readonly ICompanySettingsService _settingsService;

    public ReturnPolicyService(ICompanySettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task EnsureReturnIsAllowedAsync(DateTime invoiceDate, decimal returnAmount, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
        
        if (!settings.EnableReturns)
        {
            throw new InvalidOperationException("نظام المرتجعات معطل حالياً من قِبل إدارة الشركة.");
        }

        var daysSinceInvoice = (DateTime.UtcNow - invoiceDate).TotalDays;
        if (daysSinceInvoice > settings.MaxReturnDays)
        {
            throw new InvalidOperationException($"لقد تجاوزت فترة الإرجاع المسموحة وهي {settings.MaxReturnDays} يوم/أيام.");
        }
    }

    public async Task<ReturnProcessMode> GetReturnProcessModeAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
        return settings.ReturnMode;
    }
}
