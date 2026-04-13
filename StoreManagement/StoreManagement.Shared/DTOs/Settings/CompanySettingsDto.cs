using System.ComponentModel.DataAnnotations;
using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.DTOs.Settings;

public class CompanySettingsDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int SettingsVersion { get; set; }
    public bool IsLocked { get; set; }

    // General
    public bool HasBranches { get; set; }
    public int? DefaultBranchId { get; set; }
    public bool MultiUserMode { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int DecimalPlaces { get; set; }
    public bool EnableTaxes { get; set; }
    public bool PricesIncludeTax { get; set; }
    public bool RequireNotesOnManualActions { get; set; }

    // Sales
    public bool EnableSales { get; set; }
    public bool AllowCashSales { get; set; }
    public bool AllowCreditSales { get; set; }
    public bool AllowInstallmentSales { get; set; }
    public bool RequireCustomerOnSale { get; set; }
    public bool AllowAnonymousCustomer { get; set; }
    public bool AllowPriceOverride { get; set; }
    public bool AllowDiscount { get; set; }
    public decimal MaxDiscountPercent { get; set; }
    public bool AllowBelowCostSale { get; set; }
    public bool AutoReserveStockOnDraft { get; set; }

    // Purchases
    public bool EnablePurchases { get; set; }
    public bool AllowCreditPurchases { get; set; }
    public bool RequireSupplierOnPurchase { get; set; }
    public bool AllowEditCostAfterPosting { get; set; }
    public bool EnablePurchaseReturns { get; set; }

    // Inventory
    public bool EnableInventory { get; set; }
    public bool TrackStockByBranch { get; set; }
    public bool AllowNegativeStock { get; set; }
    public bool EnableStockTransfers { get; set; }
    public bool EnableStockAdjustments { get; set; }
    public bool EnableBatchOrExpiryTracking { get; set; }
    public bool EnableBarcode { get; set; }
    public bool AutoGenerateSku { get; set; }

    // Returns
    public bool EnableReturns { get; set; }
    public ReturnProcessMode ReturnMode { get; set; }
    public bool RequireReferenceInvoice { get; set; }
    public bool AllowCashRefund { get; set; }
    public bool AllowStoreCreditRefund { get; set; }
    public bool AllowExchange { get; set; }
    public int MaxReturnDays { get; set; }
    public bool RequireApprovalForReturns { get; set; }

    // Installments
    public bool EnableInstallments { get; set; }
    public bool AllowPartialInstallmentPayment { get; set; }
    public bool AllowEarlySettlement { get; set; }
    public bool AllowReschedule { get; set; }
    public bool ApplyLateFees { get; set; }
    public decimal DefaultLateFeeAmount { get; set; }
    public int DefaultInstallmentCount { get; set; }
    public InstallmentFrequency DefaultFrequency { get; set; }
    public bool RequireApprovalForReschedule { get; set; }

    // Approvals
    public bool EnableApprovals { get; set; }
    public bool RequireApprovalForHighDiscount { get; set; }
    public decimal DiscountApprovalThresholdPercent { get; set; }
    public bool RequireApprovalForExpense { get; set; }
    public decimal ExpenseApprovalThreshold { get; set; }
    public bool RequireApprovalForStockAdjustment { get; set; }
    public bool RequireApprovalForCreditOverLimit { get; set; }

    // Finance
    public bool EnableTreasury { get; set; }
    public bool EnableExpenses { get; set; }
    public bool EnableRevenues { get; set; }
    public bool AllowMultipleCashboxes { get; set; }
    public bool EnableShiftTracking { get; set; }
    public bool RequireShiftOpenBeforeSale { get; set; }

    // HR
    public bool EnableEmployees { get; set; }
    public bool EnableAttendance { get; set; }
    public bool EnablePayroll { get; set; }
    public bool EnableAdvances { get; set; }

    // UI
    public bool ShowAdvancedMenus { get; set; }
    public bool UseSimpleDashboard { get; set; }
    public bool EnableQuickSaleScreen { get; set; }
    public bool EnableKeyboardShortcuts { get; set; }
    public bool EnableTouchMode { get; set; }
    public bool ShowCostPriceToAuthorizedUsersOnly { get; set; }
}

// ==========================================
// Update Section DTOs (With Validation)
// ==========================================

public class UpdateSalesSettingsDto
{
    public bool EnableSales { get; set; }
    public bool AllowCashSales { get; set; }
    public bool AllowCreditSales { get; set; }
    public bool AllowInstallmentSales { get; set; }
    public bool RequireCustomerOnSale { get; set; }
    public bool AllowAnonymousCustomer { get; set; }
    public bool AllowPriceOverride { get; set; }
    public bool AllowDiscount { get; set; }
    
    [Range(0, 100)]
    public decimal MaxDiscountPercent { get; set; }
    public bool AllowBelowCostSale { get; set; }
    public bool AutoReserveStockOnDraft { get; set; }
}

public class UpdateInventorySettingsDto
{
    public bool EnableInventory { get; set; }
    public bool TrackStockByBranch { get; set; }
    public bool AllowNegativeStock { get; set; }
    public bool EnableStockTransfers { get; set; }
    public bool EnableStockAdjustments { get; set; }
    public bool EnableBatchOrExpiryTracking { get; set; }
    public bool EnableBarcode { get; set; }
    public bool AutoGenerateSku { get; set; }
}

public class UpdateReturnsSettingsDto
{
    public bool EnableReturns { get; set; }
    public ReturnProcessMode ReturnMode { get; set; }
    public bool RequireReferenceInvoice { get; set; }
    public bool AllowCashRefund { get; set; }
    public bool AllowStoreCreditRefund { get; set; }
    public bool AllowExchange { get; set; }
    
    [Range(0, int.MaxValue)]
    public int MaxReturnDays { get; set; }
    public bool RequireApprovalForReturns { get; set; }
}

public class UpdateInstallmentsSettingsDto
{
    public bool EnableInstallments { get; set; }
    public bool AllowPartialInstallmentPayment { get; set; }
    public bool AllowEarlySettlement { get; set; }
    public bool AllowReschedule { get; set; }
    public bool ApplyLateFees { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal DefaultLateFeeAmount { get; set; }
    
    [Range(1, 360)]
    public int DefaultInstallmentCount { get; set; }
    public InstallmentFrequency DefaultFrequency { get; set; }
    public bool RequireApprovalForReschedule { get; set; }
}

public class UpdateApprovalsSettingsDto
{
    public bool EnableApprovals { get; set; }
    public bool RequireApprovalForHighDiscount { get; set; }
    
    [Range(0, 100)]
    public decimal DiscountApprovalThresholdPercent { get; set; }
    public bool RequireApprovalForExpense { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal ExpenseApprovalThreshold { get; set; }
    public bool RequireApprovalForStockAdjustment { get; set; }
    public bool RequireApprovalForCreditOverLimit { get; set; }
}

// Snapshot is used for cached responses / Frontend contexts
public class SettingsSnapshotDto
{
    public int SettingsVersion { get; set; }
    public bool EnableSales { get; set; }
    public bool EnableInstallments { get; set; }
    public bool EnableReturns { get; set; }
    public bool EnableApprovals { get; set; }
    public bool EnableInventory { get; set; }
    public bool EnableHR { get; set; } // Map EnableEmployees
    public bool TrackStockByBranch { get; set; }
    public bool UseSimpleDashboard { get; set; }
}
