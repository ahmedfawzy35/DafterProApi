using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;

namespace StoreManagement.Shared.Interfaces;

public interface IInstallmentService
{
    Task<InstallmentSchedulePreviewDto> PreviewScheduleAsync(CreateInstallmentPlanDto dto);
    Task<InstallmentPlanReadDto> CreatePlanAsync(CreateInstallmentPlanDto dto);
    Task<PagedResult<InstallmentPlanReadDto>> GetAllPlansAsync(PaginationQueryDto query, int? customerId, string? status);
    Task<InstallmentPlanReadDto?> GetPlanByIdAsync(int id);
    Task<InstallmentPaymentResultDto> PayInstallmentAsync(int scheduleItemId, decimal amount, int? branchId = null);
    Task ProcessOverdueInstallmentsAsync(DateTime referenceDate);
}
