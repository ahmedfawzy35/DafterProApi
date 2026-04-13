using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Services.Services.Policies;

public class SalesPolicyService : ISalesPolicyService
{
    private readonly ICompanySettingsService _settingsService;
    private readonly IBranchInventoryService _branchInventoryService;

    public SalesPolicyService(ICompanySettingsService settingsService, IBranchInventoryService branchInventoryService)
    {
        _settingsService = settingsService;
        _branchInventoryService = branchInventoryService;
    }

    public async Task EnsureCanSellAsync(decimal totalInvoiceAmount, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
        
        if (!settings.EnableSales)
        {
            throw new InvalidOperationException("عمليات البيع معطلة حالياً بناءً على إعدادات الشركة.");
        }

        if (totalInvoiceAmount < 0)
        {
            throw new InvalidOperationException("إجمالي الفاتورة يجب أن يكون أكبر من أو يساوي صفر.");
        }

        if (totalInvoiceAmount > 0 && !settings.AllowCashSales && !settings.AllowCreditSales && !settings.AllowInstallmentSales)
        {
            throw new InvalidOperationException("إعدادات الشركة تمنع جميع طرق البيع (نقدي، آجل، تقسيط).");
        }
    }

    public async Task EnsureCustomerCanPurchaseAsync(int customerId, decimal totalInvoiceAmount, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }

    public async Task EnsureItemInventoryAvailableAsync(int branchId, int productId, decimal requestedQuantity, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.GetCompanySettingsAsync(cancellationToken);
        
        if (settings.AllowNegativeStock)
            return; // Allow selling beyond stock if setting is true

        var availability = await _branchInventoryService.GetAvailableQtyAsync(productId, branchId);
        if (availability < requestedQuantity)
        {
            throw new InvalidOperationException($"الكمية المطلوبة غير متوفرة. المتوفر الحالي: {availability}");
        }
    }
}
