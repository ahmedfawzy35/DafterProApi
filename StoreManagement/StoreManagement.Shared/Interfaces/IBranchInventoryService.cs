using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.Inventory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StoreManagement.Shared.Interfaces;

/// <summary>
/// خدمة إدارة المخزون على مستوى الفروع (المصدر الوحيد للحقيقة).
/// </summary>
public interface IBranchInventoryService
{
    // ===== القراءة =====
    Task<decimal> GetAvailableQtyAsync(int productId, int branchId);
    Task<List<BranchStockDto>> GetStockByProductAsync(int productId);
    Task<List<BranchStockDto>> GetStockByBranchAsync(int branchId);
    Task<decimal> GetTotalStockAsync(int productId);

    /// <summary>
    /// Bulk: إجمالي الكمية لكل منتج (مجموع الفروع) — يتجنب N+1 في قوائم المنتجات
    /// </summary>
    Task<Dictionary<int, (decimal Total, decimal Available)>> GetStockAggregatesForProductsAsync(IEnumerable<int> productIds);

    // ===== الكتابة (Internal Logic) =====
    Task<BranchProductStock> GetOrCreateStockAsync(int productId, int branchId);
    Task IncreaseStockAsync(int productId, int branchId, decimal qty);
    Task DecreaseStockAsync(int productId, int branchId, decimal qty, bool allowNegative = false);
    Task TransferStockAsync(int productId, int fromBranchId, int toBranchId, decimal qty);

    // ===== تعمير البيانات (Backfill) =====
    Task<BranchStockInitializationResultDto> InitializeFromTransactionsAsync(int? companyId = null, bool forceReset = false, bool dryRun = false);
}
