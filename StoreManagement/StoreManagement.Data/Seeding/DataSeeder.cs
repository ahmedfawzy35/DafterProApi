using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreManagement.Shared.Entities;

namespace StoreManagement.Data.Seeding;

/// <summary>
/// بيانات التهيئة الأولية للنظام (الأدوار، المشرف العام، الإضافات)
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(
        StoreDbContext context,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        // إنشاء الأدوار الافتراضية
        await SeedRolesAsync(roleManager, logger);

        // إنشاء شركة ومستخدم مشرف افتراضي
        await SeedDefaultCompanyAndAdminAsync(context, userManager);

        // إنشاء الإضافات الأساسية
        await SeedPluginsAsync(context);

        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<Role> roleManager, Microsoft.Extensions.Logging.ILogger logger)
    {
        // الأدوار الوظيفية الافتراضية
        foreach (var roleName in StoreManagement.Shared.Constants.DefaultRoles.All)
        {
            var role = await EnsureRoleExistsAsync(roleManager, roleName, logger);
            await SyncRolePermissionsAsync(roleManager, role, roleName, logger);
        }
    }

    private static async Task<Role> EnsureRoleExistsAsync(RoleManager<Role> roleManager, string roleName, Microsoft.Extensions.Logging.ILogger logger)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            role = new Role
            {
                Name = roleName,
                Description = GetRoleDescription(roleName)
            };
            var result = await roleManager.CreateAsync(role);
            if (result.Succeeded)
            {
                logger.LogInformation("Role created successfully: {RoleName}", roleName);
            }
            else
            {
                logger.LogError("Error creating role {RoleName}: {Errors}", roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        return role!;
    }

    private static async Task SyncRolePermissionsAsync(RoleManager<Role> roleManager, Role role, string roleName, Microsoft.Extensions.Logging.ILogger logger)
    {
        var expectedPermissions = StoreManagement.Shared.Constants.Permissions.GetForRole(roleName);
        var currentClaims = await roleManager.GetClaimsAsync(role);
        var currentPermissions = currentClaims.Select(c => c.Value).ToHashSet();

        foreach (var permission in expectedPermissions)
        {
            if (!currentPermissions.Contains(permission))
            {
                var result = await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("permission", permission));
                if (result.Succeeded)
                {
                    logger.LogInformation("Permission '{Permission}' added to role '{RoleName}'.", permission, roleName);
                }
                else
                {
                    logger.LogError("Failed to add permission '{Permission}' to role '{RoleName}'.", permission, roleName);
                }
            }
        }
    }

    private static async Task SeedDefaultCompanyAndAdminAsync(
        StoreDbContext context,
        UserManager<User> userManager)
    {
        // التحقق من عدم وجود شركة مسبقاً
        if (await context.Companies.IgnoreQueryFilters().AnyAsync())
            return;

        // إنشاء الشركة الافتراضية
        var company = new Company
        {
            Name = "شركة المتاجر الافتراضية",
            HasBranches = false,
            ManageInventory = true
        };

        context.Companies.Add(company);
        await context.SaveChangesAsync();

        // التحقق من عدم وجود مستخدم مسبقاً
        if (await userManager.FindByEmailAsync("admin@store.com") is not null)
            return;

        // إنشاء مستخدم المشرف العام
        var admin = new User
        {
            UserName = "admin@store.com",
            Email = "admin@store.com",
            CompanyId = company.Id,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, "Admin@123456");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, StoreManagement.Shared.Constants.DefaultRoles.Owner);
        }
    }

    private static async Task SeedPluginsAsync(StoreDbContext context)
    {
        // التحقق من عدم وجود إضافات مسبقاً
        if (await context.Plugins.IgnoreQueryFilters().AnyAsync())
            return;

        // الشركة الأولى
        var firstCompany = await context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync();
        if (firstCompany is null) return;

        var plugins = new List<Plugin>
        {
            new()
            {
                Name = "DigitalWallet",
                DisplayName = "المحفظة الرقمية",
                Description = "إدارة رصيد العملاء الإلكتروني وعمليات الشحن والخصم",
                IsEnabled = false,
                CompanyId = firstCompany.Id
            },
            new()
            {
                Name = "Installments",
                DisplayName = "نظام الأقساط",
                Description = "إدارة الفواتير المدفوعة على أقساط دورية",
                IsEnabled = false,
                CompanyId = firstCompany.Id
            }
        };

        context.Plugins.AddRange(plugins);
    }

    private static string GetRoleDescription(string roleName) => roleName switch
    {
        StoreManagement.Shared.Constants.DefaultRoles.Owner => "مالك الشركة - صلاحيات كاملة",
        StoreManagement.Shared.Constants.DefaultRoles.Manager => "مدير الشركة - إدارة الكيانات الرئيسية",
        StoreManagement.Shared.Constants.DefaultRoles.Accountant => "محاسب - إدارة الفواتير والمعاملات المالية",
        StoreManagement.Shared.Constants.DefaultRoles.Sales => "موظف مبيعات - إنشاء فواتير المبيعات",
        StoreManagement.Shared.Constants.DefaultRoles.InventoryClerk => "أمين المستودع - إدارة المخزون",
        _ => string.Empty
    };
}
