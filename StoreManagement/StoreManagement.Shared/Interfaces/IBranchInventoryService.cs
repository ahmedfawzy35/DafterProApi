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
    Task<double> GetAvailableQtyAsync(int productId, int branchId);
    Task<List<BranchStockDto>> GetStockByProductAsync(int productId);
    Task<List<BranchStockDto>> GetStockByBranchAsync(int branchId);
    Task<double> GetTotalStockAsync(int productId);

    // ===== الكتابة (Internal Logic) =====
    Task<BranchProductStock> GetOrCreateStockAsync(int productId, int branchId);
    Task IncreaseStockAsync(int productId, int branchId, double qty);
    Task DecreaseStockAsync(int productId, int branchId, double qty, bool allowNegative = false);
    Task TransferStockAsync(int productId, int fromBranchId, int toBranchId, double qty);

    // ===== تعمير البيانات (Backfill) =====
    Task<BranchStockInitializationResultDto> InitializeFromTransactionsAsync(int? companyId = null, bool forceReset = false, bool dryRun = false);
}
