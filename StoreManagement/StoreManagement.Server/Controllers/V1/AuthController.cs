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
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities;
using StoreManagement.Shared.Settings;

namespace StoreManagement.Server.Controllers.V1;

/// <summary>
/// متحكم المصادقة مع دعم Refresh Tokens كامل
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly JwtSettings _jwtSettings;
    private readonly StoreDbContext _context;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IOptions<JwtSettings> jwtSettings,
        StoreDbContext context,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtSettings = jwtSettings.Value;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// تسجيل الدخول - يُرجع JWT + Refresh Token
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<TokenResponseDto>>> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
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

        // توليد JWT + Refresh Token
        var accessToken = GenerateJwtToken(user, roles);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, ipAddress);

        _logger.LogInformation("تسجيل دخول ناجح: {Email} من IP: {IP}", dto.Email, ipAddress);

        return Ok(ApiResponse<TokenResponseDto>.SuccessResult(new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            RefreshTokenExpiresAt = refreshToken.ExpiresAt
        }, "تم تسجيل الدخول بنجاح"));
    }

    /// <summary>
    /// تجديد الـ JWT باستخدام Refresh Token
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<TokenResponseDto>>> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        // التحقق من الـ Access Token (حتى لو منتهي)
        var principal = GetPrincipalFromExpiredToken(dto.AccessToken);
        if (principal is null)
            return Unauthorized(ApiResponse<TokenResponseDto>.Failure("رمز الوصول غير صالح"));

        var userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var ipAddress = GetClientIp();

        // البحث عن الـ Refresh Token
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

        // إلغاء الـ Refresh Token القديم
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;

        // توليد Tokens جديدة
        var newAccessToken = GenerateJwtToken(user, roles);
        var newRefreshToken = await CreateRefreshTokenAsync(user.Id, ipAddress);

        // تسجيل العلاقة بين التوكنات
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
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);

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

        if (user is null)
            return NotFound(ApiResponse<object>.Failure("المستخدم غير موجود"));

        var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<object>.Failure("فشل تغيير كلمة المرور",
                result.Errors.Select(e => e.Description).ToList()));

        return Ok(ApiResponse<object>.SuccessResult("تم تغيير كلمة المرور بنجاح"));
    }

    // ===== Private Methods =====

    private string GenerateJwtToken(User user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("CompanyId", user.CompanyId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID لمنع replay
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        if (user.BranchId.HasValue)
            claims.Add(new Claim("BranchId", user.BranchId.Value.ToString()));

        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            signingCredentials: credentials));
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(int userId, string? ipAddress)
    {
        // توليد رمز عشوائي آمن
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
                ValidateLifetime = false // نقبل التوكن حتى لو منتهي
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
