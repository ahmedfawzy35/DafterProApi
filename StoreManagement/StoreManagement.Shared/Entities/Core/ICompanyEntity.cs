namespace StoreManagement.Shared.Entities.Core;

/// <summary>
/// Interface for entities that belong to a specific company.
/// Exposes CompanyId and IsDeleted for global query filter usage.
/// </summary>
public interface ICompanyEntity
{
    int CompanyId { get; set; }
    bool IsDeleted { get; set; }
}
