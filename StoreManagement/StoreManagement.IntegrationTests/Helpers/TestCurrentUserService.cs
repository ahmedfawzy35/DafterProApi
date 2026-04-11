using StoreManagement.Shared.Interfaces;
using System.Collections.Generic;

namespace StoreManagement.IntegrationTests.Helpers;

public class TestCurrentUserService : ICurrentUserService
{
    public int? UserId => 999;
    public string UserName => "Test User";
    public string Email => "test@example.com";
    public int? CompanyId => 1;
    public int? BranchId => 1;
    public string Role => "Admin";
    public IEnumerable<string> Roles => new List<string> { "Admin", "SuperAdmin" };
    public bool IsPlatformUser => false;
    public int? ScopedCompanyId { get; set; }
    public IReadOnlyList<string> Permissions => new List<string> { "purchases.view", "sales.view" };
    public bool IsSuperAdmin => true;
    public bool IsAuthenticated => true;
}
