using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using StoreManagement.Shared.Constants;

namespace StoreManagement.Server.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User == null)
            return Task.CompletedTask;

        // If the user has SuperAdmin role, they automatically bypass permission checks for platform scope.
        // Wait, standard SuperAdmin still needs to follow standard rules, but typically has all permissions. 
        // We'll leave it simple for now, but grant them success if they are SuperAdmin.
        if (context.User.IsInRole(DefaultRoles.SuperAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var permissions = context.User.Claims
            .Where(x => x.Type == AppClaims.Permission || x.Type == "Permission")
            .Select(x => x.Value);

        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Try getting an explicitly registered policy first (like PlatformUserOnly)
        var policy = await base.GetPolicyAsync(policyName);

        if (policy == null)
        {
            string? permission = null;

            if (policyName.StartsWith("RequirePermission:", StringComparison.OrdinalIgnoreCase))
            {
                permission = policyName.Substring("RequirePermission:".Length);
            }
            else if (Permissions.GetAll().Contains(policyName))
            {
                // إذا كان اسم السياسة مطابقاً تماماً لكود صلاحية معروف
                permission = policyName;
            }

            if (permission != null)
            {
                var builder = new AuthorizationPolicyBuilder();
                builder.AddRequirements(new PermissionRequirement(permission));
                return builder.Build();
            }
        }

        return policy;
    }
}
