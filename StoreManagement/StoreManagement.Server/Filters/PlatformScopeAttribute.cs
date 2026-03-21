using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;

namespace StoreManagement.Server.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public class PlatformScopeAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

        // Apply scoping ONLY if the user is a platform user
        if (currentUser.IsPlatformUser)
        {
            var companyIdStr = context.HttpContext.Request.Query["companyId"].ToString();

            if (!string.IsNullOrEmpty(companyIdStr))
            {
                if (int.TryParse(companyIdStr, out var compId))
                {
                    var dbContext = context.HttpContext.RequestServices.GetRequiredService<StoreDbContext>();
                    var companyExists = await dbContext.Companies.IgnoreQueryFilters().AnyAsync(c => c.Id == compId);

                    if (!companyExists)
                    {
                        context.Result = new ObjectResult(ApiResponse<object>.Failure("الشركة المحددة في النطاق غير موجودة."))
                        {
                            StatusCode = StatusCodes.Status404NotFound
                        };
                        return;
                    }

                    currentUser.ScopedCompanyId = compId;
                }
                else
                {
                    context.Result = new ObjectResult(ApiResponse<object>.Failure("قيمة معرف الشركة غير صحيحة."))
                    {
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                    return;
                }
            }
        }

        await next();
    }
}
