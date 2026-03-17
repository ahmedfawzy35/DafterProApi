using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        RoleManager<Role> roleManager)
    {
        // إنشاء الأدوار الافتراضية
        await SeedRolesAsync(roleManager);

        // إنشاء شركة ومستخدم مشرف افتراضي
        await SeedDefaultCompanyAndAdminAsync(context, userManager);

        // إنشاء الإضافات الأساسية
        await SeedPluginsAsync(context);

        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<Role> roleManager)
    {
        // الأدوار الوظيفية الافتراضية
        string[] roles = ["SuperAdmin", "Admin", "Accountant", "Sales", "Warehouse"];

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new Role
                {
                    Name = roleName,
                    Description = GetRoleDescription(roleName)
                });
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
            await userManager.AddToRoleAsync(admin, "SuperAdmin");
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
        "SuperAdmin" => "مشرف عام - صلاحيات كاملة",
        "Admin" => "مدير الشركة - إدارة الكيانات الرئيسية",
        "Accountant" => "محاسب - إدارة الفواتير والمعاملات المالية",
        "Sales" => "موظف مبيعات - إنشاء فواتير المبيعات",
        "Warehouse" => "أمين المستودع - إدارة المخزون",
        _ => string.Empty
    };
}
