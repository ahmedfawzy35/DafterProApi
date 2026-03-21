using Microsoft.AspNetCore.Authorization;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Authorization;

public class PlatformUserRequirement : IAuthorizationRequirement { }

public class PlatformUserHandler : AuthorizationHandler<PlatformUserRequirement>
{
    private readonly ICurrentUserService _currentUser;

    public PlatformUserHandler(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PlatformUserRequirement requirement)
    {
        if (_currentUser.IsPlatformUser)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}
