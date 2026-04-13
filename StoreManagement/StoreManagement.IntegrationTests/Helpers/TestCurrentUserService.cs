using Microsoft.AspNetCore.Http;
using StoreManagement.Shared.Interfaces;
using StoreManagement.Shared.Constants;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace StoreManagement.IntegrationTests.Helpers;

public class TestCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TestCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public int? UserId
    {
        get
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 999;
        }
    }

    public string UserName => User?.FindFirst(ClaimTypes.Name)?.Value ?? "Test User";
    public string Email => "test@example.com";
    
    public int? CompanyId
    {
        get
        {
            var claim = User?.FindFirst(AppClaims.CompanyId)?.Value;
            return int.TryParse(claim, out var id) ? id : 1;
        }
    }

    public int? BranchId
    {
        get
        {
            var claim = User?.FindFirst(AppClaims.BranchId)?.Value;
            return int.TryParse(claim, out var id) ? id : 1;
        }
    }

    public string Role => Roles.FirstOrDefault() ?? "Admin";

    public IEnumerable<string> Roles => User?.FindAll(ClaimTypes.Role).Select(c => c.Value) 
                                       ?? new List<string> { "Admin", "SuperAdmin" };

    public bool IsPlatformUser => User?.FindFirst(AppClaims.IsPlatformUser)?.Value == "1";
    public int? ScopedCompanyId { get; set; }

    public IReadOnlyList<string> Permissions => User?.FindAll(AppClaims.Permission).Select(c => c.Value).ToList() 
                                               ?? StoreManagement.Shared.Constants.Permissions.GetAllTenant().ToList();

    public bool IsSuperAdmin => Roles.Contains("SuperAdmin");
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? true;
}
