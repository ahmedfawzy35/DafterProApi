using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StoreManagement.Server.Filters;

namespace StoreManagement.Server.Controllers.Base;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[TenantHardGuard]
[PlatformScope]
public abstract class BaseTenantController : ControllerBase
{
}
