using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.Constants;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Settings;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// متحكم المصادقة مع دعم Refresh Tokens وصلاحيات مدمجة في JWT
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly SignInManager<User> _signInManager;
    private readonly JwtSettings _jwtSettings;
    private readonly StoreDbContext _context;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        SignInManager<User> signInManager,
        IOptions<JwtSettings> jwtSettings,
        StoreDbContext context,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _jwtSettings = jwtSettings.Value;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// تسجيل الدخول - يُرجع JWT (يحتوي على الصلاحيات) + Refresh Token + بيانات الشركة
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<TokenResponseDto>>> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.Users
            .Include(u => u.Company)
            .ThenInclude(c => c!.PhoneNumbers)
            .Include(u => u.Company)
            .ThenInclude(c => c!.Logo)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user is null)
            return Unauthorized(ApiResponse<TokenResponseDto>.Failure("البريد الإلكتروني أو كلمة المرور غير صحيحة"));

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                return Unauthorized(ApiResponse<TokenResponseDto>.Failure("تم تعليق الحساب مؤقتاً لأسباب أمنية"));
            return Unauthorized(ApiResponse<TokenResponseDto>.Failure("البريد الإلكتروني أو كلمة المرور غير صحيحة"));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var ipAddress = GetClientIp();

        // توليد JWT (يحتوي على الصلاحيات) + Refresh Token
        var accessToken = await GenerateJwtTokenAsync(user, roles);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, ipAddress);

        _logger.LogInformation("تسجيل دخول ناجح: {Email} من IP: {IP}", dto.Email, ipAddress);

        var companyDto = user.Company != null ? new CompanyReadDto
        {
            Id = user.Company.Id,
            Name = user.Company.Name,
            Address = user.Company.Address,
            BusinessType = user.Company.BusinessType,
            HasBranches = user.Company.HasBranches,
            ManageInventory = user.Company.ManageInventory,
            TaxId = user.Company.TaxId,
            CommercialRegistry = user.Company.CommercialRegistry,
            OfficialEmail = user.Company.OfficialEmail,
            Website = user.Company.Website,
            Currency = user.Company.Currency?.ToString(),
            Description = user.Company.Description,
            PhoneNumbers = user.Company.PhoneNumbers.Select(p => new CompanyPhoneNumberDto
            {
                PhoneNumber = p.PhoneNumber,
                IsWhatsApp = p.IsWhatsApp
            }).ToList(),
            Logo = user.Company.Logo != null ? new CompanyLogoDto
            {
                Content = user.Company.Logo.Content,
                ContentType = user.Company.Logo.ContentType
            } : null
        } : null;

        return Ok(ApiResponse<TokenResponseDto>.SuccessResult(new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            Company = companyDto
        }, "تم تسجيل الدخول بنجاح"));
    }

    /// <summary>
    /// تسجيل مستخدم جديد
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<object>>> Register([FromBody] RegisterDto dto)
    {
        var user = new User
        {
            UserName = dto.Email,
            Email = dto.Email,
            CompanyId = dto.CompanyId,
            BranchId = dto.BranchId
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<object>.Failure("فشل إنشاء الحساب",
                result.Errors.Select(e => e.Description).ToList()));

        await _userManager.AddToRoleAsync(user, dto.Role);
        return Ok(ApiResponse<object>.SuccessResult(new { }, "تم إنشاء الحساب بنجاح"));
    }

    /// <summary>
    /// GET /auth/me — بيانات المستخدم الحالي مع أدواره وصلاحياته كاملةً
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<CurrentUserDto>>> Me()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.Users
            .Include(u => u.Company)
            .ThenInclude(c => c!.PhoneNumbers)
            .Include(u => u.Company)
            .ThenInclude(c => c!.Logo)
            .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

        if (user is null) return NotFound(ApiResponse<CurrentUserDto>.Failure("المستخدم غير موجود"));

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = new List<string>();
        foreach (var roleName in roles)
        {
            var roleEntity = await _roleManager.FindByNameAsync(roleName);
            if (roleEntity is null) continue;
            var claims = await _roleManager.GetClaimsAsync(roleEntity);
            permissions.AddRange(claims.Select(c => c.Value));
        }

        var company = user.Company;
        return Ok(ApiResponse<CurrentUserDto>.SuccessResult(new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            UserName = user.UserName ?? "",
            Roles = roles.ToList(),
            Permissions = permissions.Distinct().ToList(),
            Company = company != null ? new CompanyReadDto
            {
                Id = company.Id, Name = company.Name, Address = company.Address,
                BusinessType = company.BusinessType, HasBranches = company.HasBranches,
                ManageInventory = company.ManageInventory, Currency = company.Currency?.ToString(),
                PhoneNumbers = company.PhoneNumbers?.Select(p => new CompanyPhoneNumberDto
                    { PhoneNumber = p.PhoneNumber, IsWhatsApp = p.IsWhatsApp }).ToList() ?? [],
                Logo = company.Logo != null
                    ? new CompanyLogoDto { Content = company.Logo.Content, ContentType = company.Logo.ContentType }
                    : null
            } : null
        }));
    }

    /// <summary>
    /// تجديد الـ JWT باستخدام Refresh Token
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<TokenResponseDto>>> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        var principal = GetPrincipalFromExpiredToken(dto.AccessToken);
        if (principal is null)
            return Unauthorized(ApiResponse<TokenResponseDto>.Failure("رمز الوصول غير صالح"));

        var userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var ipAddress = GetClientIp();

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken && rt.UserId == userId);

        if (storedToken is null || !storedToken.IsActive)
        {
            _logger.LogWarning("محاولة استخدام Refresh Token غير صالح من IP: {IP}", ipAddress);
            return Unauthorized(ApiResponse<TokenResponseDto>.Failure("رمز التجديد غير صالح أو منتهي"));
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Unauthorized(ApiResponse<TokenResponseDto>.Failure("المستخدم غير موجود"));

        var roles = await _userManager.GetRolesAsync(user);

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;

        var newAccessToken = await GenerateJwtTokenAsync(user, roles);
        var newRefreshToken = await CreateRefreshTokenAsync(user.Id, ipAddress);
        storedToken.ReplacedByToken = newRefreshToken.Token;

        await _context.SaveChangesAsync();
        _logger.LogInformation("تم تجديد Tokens للمستخدم: {UserId}", userId);

        return Ok(ApiResponse<TokenResponseDto>.SuccessResult(new TokenResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            RefreshTokenExpiresAt = newRefreshToken.ExpiresAt
        }));
    }

    /// <summary>
    /// إلغاء الـ Refresh Token (تسجيل الخروج)
    /// </summary>
    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> RevokeToken([FromBody] string token)
    {
        var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
        if (storedToken is null || !storedToken.IsActive)
            return BadRequest(ApiResponse<object>.Failure("رمز التجديد غير صالح"));

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.SuccessResult("تم تسجيل الخروج بنجاح"));
    }

    /// <summary>
    /// تغيير كلمة المرور
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        if (user is null) return NotFound(ApiResponse<object>.Failure("المستخدم غير موجود"));

        var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<object>.Failure("فشل تغيير كلمة المرور",
                result.Errors.Select(e => e.Description).ToList()));

        return Ok(ApiResponse<object>.SuccessResult("تم تغيير كلمة المرور بنجاح"));
    }

    // ===== Private Methods =====

    /// <summary>
    /// توليد JWT يحتوي على أدوار المستخدم وصلاحياته مباشرةً لتفادي DB queries لاحقاً
    /// </summary>
    private async Task<string> GenerateJwtTokenAsync(User user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("CompanyId", user.CompanyId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        if (user.BranchId.HasValue)
            claims.Add(new Claim("BranchId", user.BranchId.Value.ToString()));

        // تضمين الصلاحيات من الأدوار مباشرةً في JWT
        var addedPerms = new HashSet<string>();
        foreach (var roleName in roles)
        {
            var roleEntity = await _roleManager.FindByNameAsync(roleName);
            if (roleEntity is null) continue;
            var roleClaims = await _roleManager.GetClaimsAsync(roleEntity);
            foreach (var rc in roleClaims)
                if (addedPerms.Add(rc.Value))
                    claims.Add(new Claim("permission", rc.Value));
        }

        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            signingCredentials: credentials));
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(int userId, string? ipAddress)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = token,
            IpAddress = ipAddress,
            UserAgent = Request.Headers.UserAgent.ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
        return refreshToken;
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        try
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
                ValidateIssuer = true, ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true, ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = false
            };

            return new JwtSecurityTokenHandler()
                .ValidateToken(token, tokenValidationParameters, out _);
        }
        catch
        {
            return null;
        }
    }

    private string? GetClientIp()
        => HttpContext.Connection.RemoteIpAddress?.ToString();
}

public class ChangePasswordDto
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
