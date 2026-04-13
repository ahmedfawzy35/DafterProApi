using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Services.Services.Policies;

public class InstallmentPolicyService : IInstallmentPolicyService
{
    private readonly ICompanySettingsService _settingsService;

    public InstallmentPolicyService(ICompanySettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task EnsureInstallmentIsValidAsync(decimal totalAmount, decimal downPayment, int months, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetCompanySettingsAsync(cancellationToken);

        // Verify if installments are enabled
        if (!settings.EnableInstallments)
        {
            throw new InvalidOperationException("نظام التقسيط معطل حالياً من قِبل إدارة الشركة.");
        }

        // Validate max allowed installment term (Example Rule: Max allowed is Default * 3 assuming generous max)
        var absoluteMaxMonths = settings.DefaultInstallmentCount * 3 > 0 ? settings.DefaultInstallmentCount * 3 : 60;
        if (months > absoluteMaxMonths)
        {
            throw new InvalidOperationException($"فترة التقسيط المطلوبة ({months} شهر) تتجاوز الحد الأقصى وهو ({absoluteMaxMonths} شهر).");
        }
    }

    public async Task<decimal> CalculateLatePenaltyAsync(decimal installmentAmount, int lateDays, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
        
        if (settings.ApplyLateFees && lateDays > 0)
        {
            // Simple flat penalty approach
            return settings.DefaultLateFeeAmount;
        }

        return 0; // No penalty if not late or late fees disabled
    }
}
