using StoreManagement.Shared.Entities.Core;
using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Entities.Configuration;

/// <summary>
/// كيان الإعدادات المركزية للشركة (Company Settings)
/// يحتوي على تفضيلات وإعدادات الشركة باستخدام النمط الهجين (Hybrid Approach)
/// بحيث تكون الأعمدة قوية النوع (Strongly-typed) ولكن مجتمعة في جدول واحد لتسهيل الاستعلام والصيانة
/// </summary>
public class CompanySettings : BaseEntity
{
    // ملاحظة: يرث الكيان BaseEntity خصائص (Id, CompanyId, IsDeleted, DeletedAt, DeletedByUserId, CreatedDate...)

    public virtual Company Company { get; set; } = null!;

    // ==========================================
    // حقول الميتا والمراجعة (Meta & Audit)
    // ==========================================
    public int SettingsVersion { get; set; } = 1;
    public bool IsLocked { get; set; } = false;

    // ==========================================
    // القسم العام (General)
    // ==========================================
    public bool HasBranches { get; set; } = false;
    public int? DefaultBranchId { get; set; }
    public bool MultiUserMode { get; set; } = false;
    public string CurrencyCode { get; set; } = "EGP";
    public int DecimalPlaces { get; set; } = 2;
    public bool EnableTaxes { get; set; } = false;
    public bool PricesIncludeTax { get; set; } = false;
    public bool RequireNotesOnManualActions { get; set; } = false;

    // ==========================================
    // قسم المبيعات (Sales)
    // ==========================================
    public bool EnableSales { get; set; } = true;
    public bool AllowCashSales { get; set; } = true;
    public bool AllowCreditSales { get; set; } = true;
    public bool AllowInstallmentSales { get; set; } = false;
    public bool RequireCustomerOnSale { get; set; } = false;
    public bool AllowAnonymousCustomer { get; set; } = true;
    public bool AllowPriceOverride { get; set; } = false;
    public bool AllowDiscount { get; set; } = true;
    public decimal MaxDiscountPercent { get; set; } = 100;
    public bool AllowBelowCostSale { get; set; } = false;
    public bool AutoReserveStockOnDraft { get; set; } = false;

    // ==========================================
    // قسم المشتريات (Purchases)
    // ==========================================
    public bool EnablePurchases { get; set; } = true;
    public bool AllowCreditPurchases { get; set; } = true;
    public bool RequireSupplierOnPurchase { get; set; } = false;
    public bool AllowEditCostAfterPosting { get; set; } = false;
    public bool EnablePurchaseReturns { get; set; } = true;

    // ==========================================
    // قسم المخزون (Inventory)
    // ==========================================
    public bool EnableInventory { get; set; } = true;
    public bool TrackStockByBranch { get; set; } = false;
    public bool AllowNegativeStock { get; set; } = false;
    public bool EnableStockTransfers { get; set; } = false;
    public bool EnableStockAdjustments { get; set; } = true;
    public bool EnableBatchOrExpiryTracking { get; set; } = false;
    public bool EnableBarcode { get; set; } = false;
    public bool AutoGenerateSku { get; set; } = false;

    // ==========================================
    // قسم المرتجعات (Returns)
    // ==========================================
    public bool EnableReturns { get; set; } = true;
    public ReturnProcessMode ReturnMode { get; set; } = ReturnProcessMode.Simple;
    public bool RequireReferenceInvoice { get; set; } = false;
    public bool AllowCashRefund { get; set; } = true;
    public bool AllowStoreCreditRefund { get; set; } = true;
    public bool AllowExchange { get; set; } = false;
    public int MaxReturnDays { get; set; } = 30;
    public bool RequireApprovalForReturns { get; set; } = false;

    // ==========================================
    // قسم التقسيط (Installments)
    // ==========================================
    public bool EnableInstallments { get; set; } = false;
    public bool AllowPartialInstallmentPayment { get; set; } = true;
    public bool AllowEarlySettlement { get; set; } = true;
    public bool AllowReschedule { get; set; } = false;
    public bool ApplyLateFees { get; set; } = false;
    public decimal DefaultLateFeeAmount { get; set; } = 0;
    public int DefaultInstallmentCount { get; set; } = 6;
    public InstallmentFrequency DefaultFrequency { get; set; } = InstallmentFrequency.Monthly;
    public bool RequireApprovalForReschedule { get; set; } = false;

    // ==========================================
    // قسم الموافقات (Approvals)
    // ==========================================
    public bool EnableApprovals { get; set; } = false;
    public bool RequireApprovalForHighDiscount { get; set; } = false;
    public decimal DiscountApprovalThresholdPercent { get; set; } = 20;
    public bool RequireApprovalForExpense { get; set; } = false;
    public decimal ExpenseApprovalThreshold { get; set; } = 1000;
    public bool RequireApprovalForStockAdjustment { get; set; } = false;
    public bool RequireApprovalForCreditOverLimit { get; set; } = false;

    // ==========================================
    // قسم المالية والخزينة (Finance)
    // ==========================================
    public bool EnableTreasury { get; set; } = true;
    public bool EnableExpenses { get; set; } = true;
    public bool EnableRevenues { get; set; } = true;
    public bool AllowMultipleCashboxes { get; set; } = false;
    public bool EnableShiftTracking { get; set; } = false;
    public bool RequireShiftOpenBeforeSale { get; set; } = false;

    // ==========================================
    // قسم الموارد البشرية (HR)
    // ==========================================
    public bool EnableEmployees { get; set; } = false;
    public bool EnableAttendance { get; set; } = false;
    public bool EnablePayroll { get; set; } = false;
    public bool EnableAdvances { get; set; } = false;

    // ==========================================
    // قسم واجهة المستخدم والـ Bootstrap (UI / Bootstrap)
    // ==========================================
    public bool ShowAdvancedMenus { get; set; } = false;
    public bool UseSimpleDashboard { get; set; } = true;
    public bool EnableQuickSaleScreen { get; set; } = true;
    public bool EnableKeyboardShortcuts { get; set; } = false;
    public bool EnableTouchMode { get; set; } = false;
    public bool ShowCostPriceToAuthorizedUsersOnly { get; set; } = true;
}
