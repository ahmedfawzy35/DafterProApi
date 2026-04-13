using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;

namespace StoreManagement.Shared.Interfaces;

public interface IInstallmentReportService
{
    Task<PagedResult<InstallmentReportItemDto>> GetOverdueInstallmentsAsync(PaginationQueryDto query, int? customerId);
    Task<InstallmentSummaryDto> GetInstallmentSummaryAsync(DateTime? from, DateTime? to);
}
