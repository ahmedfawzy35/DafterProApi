using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using StoreManagement.Data;
using StoreManagement.Shared.Entities.Identity;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.IntegrationTests.Helpers;

public class StoreManagementApiFactory : WebApplicationFactory<Program>
{
    // شيرد كونكشن للـ SQLite لضمان بقاء البيانات طوال فترة حياة الـ Factory
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public StoreManagementApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // 1. تسجيل DbContext بـ SQLite باستخدام الكونكشن المفتوحة
            services.AddDbContext<StoreDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // 1.5 Register mock Current User for service tests
            services.AddScoped<ICurrentUserService, TestCurrentUserService>();

            // 2. تسجيل Identity Stores لأننا حذفناها من Program.cs في بيئة الـ Testing
            services.AddIdentityCore<User>()
                    .AddRoles<Role>()
                    .AddEntityFrameworkStores<StoreDbContext>();

            // 3. استبدال الـ Authentication بالـ TestAuthHandler
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultScheme = TestAuthHandler.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.AuthenticationScheme, _ => { });
        });
    }

    // الطريقة الصحيحة للـ Seeding في Minimal APIs هي بعد بناء الـ Host
    public void SeedDatabase()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        
        DatabaseSeeder.SeedForTests(context);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Close();
            _connection.Dispose();
        }
    }
}
