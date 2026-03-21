using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StoreManagement.Server.Controllers.Base;

[ApiController]
[Authorize(Policy = "PlatformUserOnly")]
[Route("api/v{version:apiVersion}/platform/[controller]")]
public abstract class BasePlatformController : ControllerBase
{
}
