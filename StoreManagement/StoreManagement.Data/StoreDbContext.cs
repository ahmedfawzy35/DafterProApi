using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Data.Interceptors;
using StoreManagement.Shared.Entities;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Data;

/// <summary>
/// سياق قاعدة بيانات المتاجر الأساسي مع دعم Identity وعزل البيانات
/// </summary>
public class StoreDbContext : IdentityDbContext<User, Role, int>
{
    private readonly ICurrentUserService _currentUserService;

    public StoreDbContext(
        DbContextOptions<StoreDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    // ===== الشركات والفروع =====
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();

    // ===== العملاء والموردون =====
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerPhone> CustomerPhones => Set<CustomerPhone>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierPhone> SupplierPhones => Set<SupplierPhone>();

    // ===== المنتجات =====
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    // ===== الفواتير والمخزون =====
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

    // ===== المعاملات النقدية =====
    public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();

    // ===== الموظفون =====
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();

    // ===== النظام والإضافات =====
    public DbSet<Plugin> Plugins => Set<Plugin>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // ===== SaaS: الاشتراكات والخطط =====
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<PlanFeature> PlanFeatures => Set<PlanFeature>();
    public DbSet<CompanySubscription> CompanySubscriptions => Set<CompanySubscription>();
    public DbSet<CompanyFeatureOverride> CompanyFeatureOverrides => Set<CompanyFeatureOverride>();

    // ===== SaaS: الأمان =====
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // ===== SaaS: Outbox Pattern =====
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // الحصول على معرف الشركة الحالية من الـ JWT
        var companyId = _currentUserService.CompanyId;

        // ===== عوامل التصفية الشاملة (Global Query Filters) =====
        // تصفية العملاء حسب الشركة وعدم الحذف
        builder.Entity<Customer>()
            .HasQueryFilter(c => !c.IsDeleted && c.CompanyId == companyId);

        // تصفية الموردين
        builder.Entity<Supplier>()
            .HasQueryFilter(s => !s.IsDeleted && s.CompanyId == companyId);

        // تصفية المنتجات
        builder.Entity<Product>()
            .HasQueryFilter(p => !p.IsDeleted && p.CompanyId == companyId);

        // تصفية الفواتير
        builder.Entity<Invoice>()
            .HasQueryFilter(i => !i.IsDeleted && i.CompanyId == companyId);

        // تصفية المعاملات النقدية
        builder.Entity<CashTransaction>()
            .HasQueryFilter(t => !t.IsDeleted && t.CompanyId == companyId);

        // تصفية الموظفين
        builder.Entity<Employee>()
            .HasQueryFilter(e => !e.IsDeleted && e.CompanyId == companyId);

        // تصفية حركات المخزون
        builder.Entity<StockTransaction>()
            .HasQueryFilter(st => !st.IsDeleted && st.CompanyId == companyId);

        // ===== الفهارس (Indexes) لتحسين الأداء =====
        builder.Entity<Customer>().HasIndex(c => c.CompanyId);
        builder.Entity<Customer>().HasIndex(c => c.Name);

        builder.Entity<Supplier>().HasIndex(s => s.CompanyId);
        builder.Entity<Supplier>().HasIndex(s => s.Name);

        builder.Entity<Product>().HasIndex(p => p.CompanyId);
        builder.Entity<Product>().HasIndex(p => p.Name);

        builder.Entity<Invoice>().HasIndex(i => i.CompanyId);
        builder.Entity<Invoice>().HasIndex(i => new { i.CompanyId, i.Date });

        builder.Entity<Employee>().HasIndex(e => e.CompanyId);

        builder.Entity<CashTransaction>().HasIndex(t => t.CompanyId);
        builder.Entity<CashTransaction>().HasIndex(t => new { t.CompanyId, t.Date });

        // ===== إعداد الـ Concurrency Token =====
        builder.Entity<Customer>()
            .Property(c => c.RowVersion)
            .IsRowVersion();

        builder.Entity<Product>()
            .Property(p => p.RowVersion)
            .IsRowVersion();

        builder.Entity<Invoice>()
            .Property(i => i.RowVersion)
            .IsRowVersion();

        // ===== إعداد العلاقات =====
        builder.Entity<User>()
            .HasOne(u => u.Company)
            .WithMany(c => c.Users)
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<User>()
            .HasOne(u => u.Branch)
            .WithMany()
            .HasForeignKey(u => u.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<CashTransaction>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StockTransaction>()
            .HasOne(st => st.User)
            .WithMany()
            .HasForeignKey(st => st.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ===== إعداد علاقات SaaS =====

        // كل شركة لها اشتراكات
        builder.Entity<CompanySubscription>()
            .HasOne(cs => cs.Company)
            .WithMany()
            .HasForeignKey(cs => cs.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CompanySubscription>()
            .HasOne(cs => cs.Plan)
            .WithMany(cs => cs.Subscriptions)
            .HasForeignKey(cs => cs.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index للبحث السريع عن اشتراك الشركة
        builder.Entity<CompanySubscription>()
            .HasIndex(cs => new { cs.CompanyId, cs.IsActive });

        // Refresh Tokens
        builder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token);

        // OutboxMessages لا تحتاج FK (مستقلة)
        builder.Entity<OutboxMessage>()
            .HasKey(o => o.Id);

        builder.Entity<OutboxMessage>()
            .HasIndex(o => new { o.Processed, o.CreatedDate });

        // تطبيق إعدادات EF من ملفات الإعداد المنفصلة
        builder.ApplyConfigurationsFromAssembly(typeof(StoreDbContext).Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // إضافة الـ Interceptor لضمان Soft Delete وتحديث التواريخ تلقائياً
        optionsBuilder.AddInterceptors(new SoftDeleteAndAuditInterceptor());
        base.OnConfiguring(optionsBuilder);
    }
}
