namespace StoreManagement.Shared.Interfaces;

public interface ISalesPolicyService
{
    Task EnsureCanSellAsync(decimal totalInvoiceAmount, CancellationToken cancellationToken = default);
    Task EnsureCustomerCanPurchaseAsync(int customerId, decimal totalInvoiceAmount, CancellationToken cancellationToken = default);
    Task EnsureItemInventoryAvailableAsync(int branchId, int productId, decimal requestedQuantity, CancellationToken cancellationToken = default);
}
