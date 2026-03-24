using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.Constants;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Interfaces;
using StoreManagement.Shared.Constants;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// إدارة الأدوار والصلاحيات — متاح لصاحب الشركة والمسؤول فقط
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly RoleManager<Role> _roleManager;
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUserService _currentUser;

    public RolesController(
        RoleManager<Role> roleManager,
        UserManager<User> userManager,
        ICurrentUserService currentUser)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _currentUser = currentUser;
    }

    // ===== قائمة الأدوار =====
    [HttpGet]
    [Authorize(Policy = "RequirePermission:settings.roles")]
    public async Task<ActionResult<ApiResponse<List<RoleReadDto>>>> GetAll()
    {
        var companyId = _currentUser.CompanyId;
        var roles = await _roleManager.Roles
            .Where(r => r.CompanyId == companyId || r.CompanyId == null)
            .ToListAsync();

        var result = new List<RoleReadDto>();
        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            result.Add(new RoleReadDto
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                Permissions = claims.Select(c => c.Value).ToList(),
                UsersCount = usersInRole.Count
            });
        }

        return Ok(ApiResponse<List<RoleReadDto>>.SuccessResult(result));
    }

    // ===== إنشاء دور جديد =====
    [HttpPost]
    [Authorize(Policy = "RequirePermission:settings.roles")]
    public async Task<ActionResult<ApiResponse<RoleReadDto>>> Create([FromBody] CreateRoleDto dto)
    {
        var role = new Role
        {
            Name = dto.Name,
            Description = dto.Description,
            CompanyId = _currentUser.CompanyId
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<RoleReadDto>.Failure("فشل إنشاء الدور",
                result.Errors.Select(e => e.Description).ToList()));

        return Ok(ApiResponse<RoleReadDto>.SuccessResult(
            new RoleReadDto { Id = role.Id, Name = role.Name!, Description = role.Description },
            "تم إنشاء الدور بنجاح"));
    }

    // ===== تعديل وصف الدور =====
    [HttpPut("{id:int}")]
    [Authorize(Policy = "RequirePermission:settings.roles")]
    public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] UpdateRoleDto dto)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null || (role.CompanyId != _currentUser.CompanyId && role.CompanyId != null))
            return NotFound(ApiResponse<object>.Failure("الدور غير موجود"));

        if (DefaultRoles.All.Contains(role.Name) && role.CompanyId == null)
            return BadRequest(ApiResponse<object>.Failure("لا يمكن تعديل الأدوار الافتراضية للنظام"));

        role.Description = dto.Description;
        await _roleManager.UpdateAsync(role);
        return Ok(ApiResponse<object>.SuccessResult("تم تعديل الدور بنجاح"));
    }

    // ===== حذف دور =====
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "RequirePermission:settings.roles")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null || role.CompanyId != _currentUser.CompanyId)
            return NotFound(ApiResponse<object>.Failure("الدور غير موجود"));

        // منع حذف دور المالك أو الأدوار الافتراضية
        if (role.Name == DefaultRoles.Owner)
            return BadRequest(ApiResponse<object>.Failure("لا يمكن حذف دور المالك"));

        // منع الحذف إذا كان هناك مستخدمون مرتبطون
        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        if (usersInRole.Any())
            return BadRequest(ApiResponse<object>.Failure(
                $"لا يمكن حذف الدور — يوجد {usersInRole.Count} مستخدم مرتبط به"));

        await _roleManager.DeleteAsync(role);
        return Ok(ApiResponse<object>.SuccessResult("تم حذف الدور بنجاح"));
    }

    // ===== استعلام عن صلاحيات دور =====
    [HttpGet("{id:int}/permissions")]
    [Authorize(Policy = "RequirePermission:settings.roles")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetPermissions(int id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null) return NotFound(ApiResponse<List<string>>.Failure("الدور غير موجود"));

        var claims = await _roleManager.GetClaimsAsync(role);
        return Ok(ApiResponse<List<string>>.SuccessResult(claims.Select(c => c.Value).ToList()));
    }

    // ===== تحديث صلاحيات دور  =====
    [HttpPut("{id:int}/permissions")]
    [Authorize(Policy = "RequirePermission:settings.roles")]
    public async Task<ActionResult<ApiResponse<object>>> UpdatePermissions(
        int id, [FromBody] UpdateRolePermissionsDto dto)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null || (role.CompanyId != _currentUser.CompanyId && role.CompanyId != null))
            return NotFound(ApiResponse<object>.Failure("الدور غير موجود"));

        // التحقق من أن الصلاحيات المطلوبة موجودة في النظام
        var allPerms = Permissions.GetAll();
        var invalid = dto.Permissions.Except(allPerms).ToList();
        if (invalid.Count != 0)
            return BadRequest(ApiResponse<object>.Failure($"صلاحيات غير معروفة: {string.Join(", ", invalid)}"));

        // تحديث الصلاحيات بشكل آمن (Idempotent)
        var currentClaims = await _roleManager.GetClaimsAsync(role);
        var currentPermissionValues = currentClaims
            .Where(c => c.Type == AppClaims.Permission)
            .Select(c => c.Value)
            .ToHashSet();

        // 1. إضافة الصلاحيات المفقودة (الجديدة)
        var toAdd = dto.Permissions.Except(currentPermissionValues);
        foreach (var perm in toAdd)
        {
            await _roleManager.AddClaimAsync(role, new System.Security.Claims.Claim(AppClaims.Permission, perm));
        }

        // 2. مسح الصلاحيات التي يملكها النظام فقط ولم تعد مطلوبة (عدم مسح Custom Claims)
        var allPermsSet = allPerms.ToHashSet();
        foreach (var claim in currentClaims)
        {
            if (claim.Type == AppClaims.Permission && 
                allPermsSet.Contains(claim.Value) && 
                !dto.Permissions.Contains(claim.Value))
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }
        }

        return Ok(ApiResponse<object>.SuccessResult("تم تحديث الصلاحيات بنجاح"));
    }

    // ===== قائمة كل الصلاحيات المتاحة في النظام =====
    [HttpGet("permissions/all")]
    public ActionResult<ApiResponse<string[]>> GetAllPermissions()
        => Ok(ApiResponse<string[]>.SuccessResult(Permissions.GetAll()));
}
