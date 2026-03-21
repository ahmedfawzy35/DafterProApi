using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class TenantHardGuardAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
        
        if (!currentUser.IsPlatformUser && (currentUser.CompanyId == null || currentUser.CompanyId <= 0))
        {
            context.Result = new ObjectResult(ApiResponse<object>.Failure("غير مصرح: يجب أن تكون مرتبطاً بشركة صالحة للوصول إلى هذا المورد."))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        base.OnActionExecuting(context);
    }
}
