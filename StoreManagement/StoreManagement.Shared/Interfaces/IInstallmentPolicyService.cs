namespace StoreManagement.Shared.Interfaces;

public interface IInstallmentPolicyService
{
    Task EnsureInstallmentIsValidAsync(decimal totalAmount, decimal downPayment, int months, CancellationToken cancellationToken = default);
    Task<decimal> CalculateLatePenaltyAsync(decimal installmentAmount, int lateDays, CancellationToken cancellationToken = default);
}
