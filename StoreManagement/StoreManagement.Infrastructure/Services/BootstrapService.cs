using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreManagement.Data;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.Identity;
using StoreManagement.Shared.Interfaces;
using StoreManagement.Shared.Constants;
using System.Security.Claims;

namespace StoreManagement.Infrastructure.Services;

public class BootstrapService : IBootstrapService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ICompanySettingsService _settingsService;
    private readonly IBranchService _branchService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<BootstrapService> _logger;

    public BootstrapService(
        StoreDbContext context,
        ICurrentUserService currentUser,
        ICompanySettingsService settingsService,
        IBranchService branchService,
        UserManager<User> userManager,
        ILogger<BootstrapService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _settingsService = settingsService;
        _branchService = branchService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<BootstrapDto> GetInitialAppDataAsync()
    {
        var companyId = _currentUser.CompanyId ?? throw new UnauthorizedAccessException("غير مصرح.");
        var userId = _currentUser.UserId;

        var user = userId.HasValue ? await _userManager.FindByIdAsync(userId.Value.ToString()) : null;
        var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
        var primaryRole = roles.FirstOrDefault() ?? "Staff";

        // Fetch user basic permissions based on Role
        var permissions = new List<string>();
        if (user != null)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            permissions = claims.Where(c => c.Type == AppClaims.Permission).Select(c => c.Value).ToList();
        }
        
        // If empty, fetch role claims
        if (!permissions.Any())
        {
            // For now, return basic structure
            permissions.Add("sales.view");
            permissions.Add("purchases.view");
            permissions.Add("finance.view");
            permissions.Add("inventory.view");
        }

        var settings = await _settingsService.GetCompanySettingsAsync();
        var branches = await _branchService.GetAllAsync();

        var company = await _context.Companies.FindAsync(companyId);

        var bootstrap = new BootstrapDto
        {
            User = new UserProfileDto
            {
                Id = user?.Id.ToString() ?? userId?.ToString() ?? "0",
                Name = user?.UserName ?? _currentUser.UserName ?? "User",
                Email = user?.Email ?? "user@example.com",
                Role = primaryRole,
                Permissions = permissions
            },
            Company = new CompanyInfoGroupDto
            {
                Id = companyId,
                Name = company?.Name ?? "Store",
                CurrencyCode = settings.CurrencyCode
            },
            Features = new FeatureFlagsDto
            {
                EnableSales = settings.EnableSales,
                EnablePurchases = settings.EnablePurchases,
                EnableInventory = settings.EnableInventory,
                EnableReturns = settings.EnableReturns,
                EnableInstallments = settings.EnableInstallments,
                EnableHR = settings.EnableEmployees,
                EnableTreasury = settings.EnableTreasury
            },
            Branches = branches,
            UI = new UIPreferencesDto
            {
                ShowAdvancedMenus = settings.ShowAdvancedMenus,
                EnableQuickSaleScreen = settings.EnableQuickSaleScreen,
                EnableKeyboardShortcuts = settings.EnableKeyboardShortcuts
            }
        };

        _logger.LogInformation("Bootstrap Snapshot for User {UserId} in Company {CompanyId}: Role={Role}, PermissionsCount={PermCount}, FeaturesEnabled={Features}",
            userId, companyId, bootstrap.User.Role, bootstrap.User.Permissions.Count, 
            string.Join(",", typeof(FeatureFlagsDto).GetProperties()
                .Where(p => p.PropertyType == typeof(bool) && (bool)p.GetValue(bootstrap.Features)!)
                .Select(p => p.Name)));

        return bootstrap;
    }
}
