namespace StoreManagement.Shared.Entities.Core;

/// <summary>
/// Interface for entities that belong to a specific branch
/// Inherits from ICompanyEntity because a branch always belongs to a company
/// </summary>
public interface IBranchEntity : ICompanyEntity
{
    public int BranchId { get; set; }
}
