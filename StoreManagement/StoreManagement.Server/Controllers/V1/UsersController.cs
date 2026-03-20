using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.Constants;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// إدارة المستخدمين — إنشاء وتعيين أدوار ومتابعة
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize(Policy = "RequirePermission:settings.users")]
public class UsersController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly ICurrentUserService _currentUser;
    private readonly StoreDbContext _context;

    public UsersController(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        ICurrentUserService currentUser,
        StoreDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _currentUser = currentUser;
        _context = context;
    }

    // ===== قائمة المستخدمين في الشركة =====
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserReadDto>>>> GetAll()
    {
        var companyId = _currentUser.CompanyId;
        var users = await _userManager.Users
            .Where(u => u.CompanyId == companyId)
            .ToListAsync();

        var result = new List<UserReadDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            result.Add(new UserReadDto
            {
                Id = u.Id,
                Email = u.Email ?? "",
                UserName = u.UserName ?? "",
                Roles = roles.ToList()
            });
        }

        return Ok(ApiResponse<List<UserReadDto>>.SuccessResult(result));
    }

    // ===== إنشاء مستخدم جديد في الشركة =====
    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserReadDto>>> Create([FromBody] CreateUserDto dto)
    {
        var companyId = _currentUser.CompanyId;
        var user = new User
        {
            UserName = dto.Email,
            Email = dto.Email,
            CompanyId = (int)companyId!,
            BranchId = dto.BranchId
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<UserReadDto>.Failure("فشل إنشاء المستخدم",
                result.Errors.Select(e => e.Description).ToList()));

        if (!string.IsNullOrWhiteSpace(dto.Role))
            await _userManager.AddToRoleAsync(user, dto.Role);

        return Ok(ApiResponse<UserReadDto>.SuccessResult(
            new UserReadDto { Id = user.Id, Email = user.Email!, UserName = user.UserName!, Roles = [dto.Role] },
            "تم إنشاء المستخدم بنجاح"));
    }

    // ===== تعيين أدوار لمستخدم =====
    [HttpPut("{id:int}/roles")]
    public async Task<ActionResult<ApiResponse<object>>> AssignRoles(int id, [FromBody] AssignRolesDto dto)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null || user.CompanyId != _currentUser.CompanyId)
            return NotFound(ApiResponse<object>.Failure("المستخدم غير موجود"));

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRolesAsync(user, dto.Roles);

        return Ok(ApiResponse<object>.SuccessResult("تم تعيين الأدوار بنجاح"));
    }

    // ===== حذف مستخدم =====
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null || user.CompanyId != _currentUser.CompanyId)
            return NotFound(ApiResponse<object>.Failure("المستخدم غير موجود"));

        // منع حذف المستخدم نفسه
        var currentUserId = int.Parse(_userManager.GetUserId(User)!);
        if (user.Id == currentUserId)
            return BadRequest(ApiResponse<object>.Failure("لا يمكن حذف حسابك الشخصي"));

        await _userManager.DeleteAsync(user);
        return Ok(ApiResponse<object>.SuccessResult("تم حذف المستخدم بنجاح"));
    }
}
