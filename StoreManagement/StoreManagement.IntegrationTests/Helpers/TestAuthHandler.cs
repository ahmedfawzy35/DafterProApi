using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StoreManagement.Shared.Constants;

namespace StoreManagement.IntegrationTests.Helpers;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestScheme";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>();

        // 1. CompanyId (Default 1) - Matches CurrentUserService (AppClaims.CompanyId)
        var companyId = "1";
        if (Request.Headers.TryGetValue("X-Test-CompanyId", out var companyVals))
            companyId = companyVals.FirstOrDefault() ?? "1";
        claims.Add(new Claim(AppClaims.CompanyId, companyId));

        // 2. UserId (Default 999) - MUST USE ClaimTypes.NameIdentifier for CurrentUserService
        var userId = "999";
        if (Request.Headers.TryGetValue("X-Test-UserId", out var userVals))
            userId = userVals.FirstOrDefault() ?? "999";
        claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        claims.Add(new Claim(ClaimTypes.Name, "Test User"));

        // 3. Permission Claims (Required for policy-based auth)
        var allTenantPermissions = StoreManagement.Shared.Constants.Permissions.GetAllTenant();
        foreach (var p in allTenantPermissions)
        {
            claims.Add(new Claim(AppClaims.Permission, p));
        }

        // 4. Role (Default user)
        if (Request.Headers.TryGetValue("X-Test-Role", out var roleVals))
        {
            var role = roleVals.FirstOrDefault() ?? "user";
            
            // Map common test roles to system roles
            if (role.ToLower() == "admin")
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin")); // For IsSuperAdmin check
                claims.Add(new Claim(AppClaims.IsPlatformUser, "1"));
            }
            else
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.Role, "user"));
        }

        // 5. BranchId (Optional)
        if (Request.Headers.TryGetValue("X-Test-BranchId", out var branchVals))
        {
            var branchIdStr = branchVals.FirstOrDefault();
            if (!string.IsNullOrEmpty(branchIdStr))
                claims.Add(new Claim(AppClaims.BranchId, branchIdStr));
        }

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
