using StoreManagement.Shared.Entities.Diagnostics;
using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Interfaces;

public interface IReconciliationService
{
    Task RunCompanyReconciliationAsync(int companyId);
    Task<IEnumerable<ReconciliationFinding>> GetStoredFindingsAsync(int companyId, FindingStatus? status = null, string? category = null, string? severity = null, DateTime? from = null, DateTime? to = null);
    Task<ReconciliationFinding?> GetFindingByIdAsync(int id, int companyId);
}
