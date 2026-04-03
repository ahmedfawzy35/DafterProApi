using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using StoreManagement.Data;
using StoreManagement.Data.Seeding;
using StoreManagement.Infrastructure;
using StoreManagement.Infrastructure.Services;
using StoreManagement.Infrastructure.Services.PayrollStrategies;
using StoreManagement.Server.Jobs;
using StoreManagement.Server.Middleware;
using StoreManagement.Services.Mappings;
using StoreManagement.Services.Validators;
using StoreManagement.Shared.Entities;
using StoreManagement.Shared.Interfaces;
using StoreManagement.Shared.Settings;
using StoreManagement.Services.Services;
using StoreManagement.Shared.Constants;

// ===== إعداد Serilog مع Structured Logging =====
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
    .Enrich.FromLogContext()             // يدعم CorrelationId/UserId/CompanyId
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] [{ScopeType}] [{CompanyId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/store-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] [{ScopeType}] [{UserId}] [{CompanyId}] {Message:lj}{NewLine}{Exception}")
    // يمكن إضافة Seq/ELK هنا:
    // .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ===== قراءة الإعدادات =====
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<StorageSettings>(builder.Configuration.GetSection("StorageSettings"));
builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection("CacheSettings"));
builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimitSettings"));

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
var cacheSettings = builder.Configuration.GetSection("CacheSettings").Get<CacheSettings>() ?? new CacheSettings();
var rateLimitSettings = builder.Configuration.GetSection("RateLimitSettings").Get<RateLimitSettings>() ?? new RateLimitSettings();

// ===== قاعدة البيانات (Scoped) =====
builder.Services.AddDbContext<StoreDbContext>((sp, options) =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly("StoreManagement.Data"));
}, ServiceLifetime.Scoped);

// ===== Identity =====
builder.Services.AddIdentity<User, Role>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<StoreDbContext>()
.AddDefaultTokenProviders();

// ===== JWT Authentication =====
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ValidateIssuer = true, ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true, ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true, ClockSkew = TimeSpan.Zero
    };
});

// ===== Authorization Policies (Claims-Based Permissions) =====
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, StoreManagement.Server.Authorization.PlatformUserHandler>();

builder.Services.AddAuthorizationBuilder()
    // سياسة وصول المنصة
    .AddPolicy("PlatformUserOnly", policy => 
        policy.Requirements.Add(new StoreManagement.Server.Authorization.PlatformUserRequirement()))
    // صلاحيات النظام
    .AddPolicy("RequirePermission:settings.roles",
        p => p.RequireClaim(AppClaims.Permission, "settings.roles"))
    .AddPolicy("RequirePermission:settings.users",
        p => p.RequireClaim(AppClaims.Permission, "settings.users"))
    .AddPolicy("RequirePermission:settings.general",
        p => p.RequireClaim(AppClaims.Permission, "settings.general"))
    .AddPolicy("RequirePermission:settings.billing",
        p => p.RequireClaim(AppClaims.Permission, "settings.billing"))
    .AddPolicy("RequirePermission:employees.payroll",
        p => p.RequireClaim(AppClaims.Permission, "employees.payroll"))
    .AddPolicy("RequirePermission:employees.loans",
        p => p.RequireClaim(AppClaims.Permission, "employees.loans"))
    .AddPolicy("RequirePermission:sales.delete",
        p => p.RequireClaim(AppClaims.Permission, "sales.delete"))
    .AddPolicy("RequirePermission:sales.refund",
        p => p.RequireClaim(AppClaims.Permission, "sales.refund"))
    .AddPolicy("RequirePermission:finance.delete",
        p => p.RequireClaim(AppClaims.Permission, "finance.delete"))
    .AddPolicy("RequirePermission:reports.export",
        p => p.RequireClaim(AppClaims.Permission, "reports.export"));

// ===== Distributed Cache (Redis + MemoryCache Fallback) =====
builder.Services.AddMemoryCache();

try
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = cacheSettings.RedisConnectionString;
        options.InstanceName = "StoreManagement:";
    });
    Log.Information("Redis تم تكوينه على: {ConnectionString}", cacheSettings.RedisConnectionString);
}
catch (Exception ex)
{
    Log.Warning(ex, "فشل تكوين Redis - سيتم استخدام MemoryCache كـ Fallback");
    builder.Services.AddDistributedMemoryCache();
}

// ===== Rate Limiting (مرتبط بـ CompanyId أو UserId) =====
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"success\":false,\"message\":\"تجاوزت الحد المسموح به من الطلبات. حاول مرة أخرى لاحقاً\"}", token);
    };

    // Policy مرتبطة بـ CompanyId أو IP كـ fallback
    options.AddPolicy("PerCompany", context =>
    {
        var companyId = context.User?.FindFirst("CompanyId")?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetSlidingWindowLimiter(companyId, _ =>
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = rateLimitSettings.PermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitSettings.WindowSeconds),
                SegmentsPerWindow = 6,
                QueueLimit = rateLimitSettings.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });
});

// ===== خدمات الـ Infrastructure =====
builder.Services.AddHttpContextAccessor();

// Core Services (Scoped)
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// SaaS Services (Scoped)
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IFeatureService, FeatureService>();
builder.Services.AddScoped<IOutboxService, OutboxService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// Business Logic Services (Scoped)
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();  // خدمة الموردين المستقلة الجديدة
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<ICashTransactionService, CashTransactionService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IPluginService, PluginService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<ISettlementService, SettlementService>();
builder.Services.AddScoped<IBarcodeService, BarcodeService>();

// HR & Payroll Engine Services (Scoped)
builder.Services.AddScoped<IEmployeeStatusResolver, EmployeeStatusResolver>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddScoped<ISalaryCalculator, MonthlySalaryCalculator>();
builder.Services.AddScoped<ISalaryCalculator, DailySalaryCalculator>();

// ===== AutoMapper + FluentValidation =====
builder.Services.AddAutoMapper(cfg => {}, typeof(MappingProfile).Assembly);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateCustomerValidator>();

// ===== API Versioning =====
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ===== Hangfire (Background Jobs) =====
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

// ===== Health Checks =====
builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "SQL Server", tags: ["database"]);

// ===== Controllers + Swagger =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "نظام إدارة المتاجر - SaaS Platform",
        Version = "v1",
        Description = "واجهة برمجية لنظام إدارة المتاجر المتعدد الشركات والفروع"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer", BearerFormat = "JWT", In = ParameterLocation.Header,
        Description = "أدخل: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, [] }
    });
});

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("StorePolicy", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ===== Data Seeding =====
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeder");

    await context.Database.MigrateAsync();
    await DataSeeder.SeedAsync(context, userManager, roleManager, logger);
}

// ===== Middleware Pipeline (الترتيب مهم جداً) =====
// 1. CorrelationId لكل Request (أول شيء)
app.UseMiddleware<CorrelationIdMiddleware>();

// 2. معالج الاستثناءات
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSerilogRequestLogging();

// 3. HTTPS + CORS
app.UseHttpsRedirection();
app.UseCors("StorePolicy");

// 4. Swagger (Development فقط)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "StoreManagement SaaS v1");
        c.RoutePrefix = "swagger";
    });
}

// 5. Rate Limiting
app.UseRateLimiter();

// 6. Authentication + Authorization
app.UseAuthentication();
app.UseAuthorization();

// 7. Tenant Resolution (بعد Authentication)
app.UseMiddleware<TenantResolutionMiddleware>();

// 8. Tenant Status Check (التحقق من تفعيل الشركة والفرع والمستخدم)
app.UseTenantStatus();

// 9. Subscription Check (بعد Tenant Resolution)
app.UseMiddleware<SubscriptionMiddleware>();

// ===== Hangfire Dashboard =====
app.UseHangfireDashboard("/hangfire", new DashboardOptions { Authorization = [] });

// ===== جدولة Outbox Processor (كل دقيقة) =====
RecurringJob.AddOrUpdate<OutboxProcessorJob>(
    "outbox-processor",
    job => job.ProcessAsync(),
    Cron.Minutely());

// ===== Health + Controllers =====
app.MapHealthChecks("/health");
app.MapControllers().RequireRateLimiting("PerCompany");

Log.Information("تم تشغيل StoreManagement SaaS Platform بنجاح 🚀");

await app.RunAsync();
