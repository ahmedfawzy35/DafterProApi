namespace StoreManagement.Shared.Interfaces;

public interface IReturnPolicyService
{
    Task EnsureReturnIsAllowedAsync(DateTime invoiceDate, decimal returnAmount, CancellationToken cancellationToken = default);
    Task<StoreManagement.Shared.Enums.ReturnProcessMode> GetReturnProcessModeAsync(CancellationToken cancellationToken = default);
}
