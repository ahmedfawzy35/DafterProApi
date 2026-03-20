using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Data.Interceptors;
using StoreManagement.Shared.Entities.HR;
using StoreManagement.Shared.Entities.Inventory;
using StoreManagement.Shared.Entities.Sales;
using StoreManagement.Shared.Entities.Finance;
using StoreManagement.Shared.Entities.Identity;
using StoreManagement.Shared.Entities.Partners;
using StoreManagement.Shared.Entities.Configuration;
using StoreManagement.Shared.Entities.Core;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Data;

/// <summary>
/// سياق قاعدة بيانات المتاجر الأساسي مع دعم Identity وعزل البيانات
/// </summary>
public class StoreDbContext : IdentityDbContext<User, Role, int>
{
    private readonly ICurrentUserService _currentUserService;
    //ahmedfawzyph9584@gmail.com
    public StoreDbContext(
        DbContextOptions<StoreDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    // ===== الشركات والفروع =====
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyPhoneNumber> CompanyPhoneNumbers => Set<CompanyPhoneNumber>();
    public DbSet<CompanyLogo> CompanyLogos => Set<CompanyLogo>();
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
    public DbSet<AccountSettlement> AccountSettlements => Set<AccountSettlement>();

    // ===== الموظفون =====
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<EmployeeAction> EmployeeActions => Set<EmployeeAction>();
    public DbSet<EmployeeSalary> EmployeeSalaries => Set<EmployeeSalary>();
    public DbSet<SalaryAdjustment> SalaryAdjustments => Set<SalaryAdjustment>();
    public DbSet<RecurringAdjustment> RecurringAdjustments => Set<RecurringAdjustment>();
    public DbSet<EmployeeLoan> EmployeeLoans => Set<EmployeeLoan>();
    public DbSet<LoanInstallment> LoanInstallments => Set<LoanInstallment>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollRunItem> PayrollRunItems => Set<PayrollRunItem>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();

    public DbSet<CompanyPolicy> CompanyPolicies => Set<CompanyPolicy>();

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

        // تصفية التسويات
        builder.Entity<AccountSettlement>()
            .HasQueryFilter(s => !s.IsDeleted && s.CompanyId == companyId);

        // تصفية الموظفين وتوابعهم
        builder.Entity<Employee>()
            .HasQueryFilter(e => !e.IsDeleted && e.CompanyId == companyId);

        builder.Entity<Attendance>()
            .HasQueryFilter(a => !a.IsDeleted && a.CompanyId == companyId);

        builder.Entity<EmployeeAction>()
            .HasQueryFilter(a => !a.IsDeleted && a.CompanyId == companyId);

        builder.Entity<EmployeeSalary>()
            .HasQueryFilter(s => !s.IsDeleted && s.CompanyId == companyId);

        builder.Entity<SalaryAdjustment>()
            .HasQueryFilter(s => !s.IsDeleted && s.CompanyId == companyId);

        builder.Entity<RecurringAdjustment>()
            .HasQueryFilter(s => !s.IsDeleted && s.CompanyId == companyId);

        builder.Entity<EmployeeLoan>()
            .HasQueryFilter(l => !l.IsDeleted && l.CompanyId == companyId);

        builder.Entity<LoanInstallment>()
            .HasQueryFilter(i => !i.IsDeleted && i.CompanyId == companyId);

        builder.Entity<PayrollRun>()
            .HasQueryFilter(p => !p.IsDeleted && p.CompanyId == companyId);

        builder.Entity<PayrollRunItem>()
            .HasQueryFilter(p => !p.IsDeleted && p.CompanyId == companyId);

        builder.Entity<CompanyPolicy>()
            .HasQueryFilter(p => !p.IsDeleted && p.CompanyId == companyId);

        // تصفية حركات المخزون
        builder.Entity<StockTransaction>()
            .HasQueryFilter(st => !st.IsDeleted && st.CompanyId == companyId);

        // تصفية الشركات (Soft Delete)
        builder.Entity<Company>()
            .HasQueryFilter(c => !c.IsDeleted);

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

        // ===== دقة أنواع الـ Decimal لمنع التقريب غير المتوقع =====
        // خطة الاشتراك
        builder.Entity<Plan>()
            .Property(p => p.MonthlyPrice).HasColumnType("decimal(18,4)");
        builder.Entity<Plan>()
            .Property(p => p.AnnualPrice).HasColumnType("decimal(18,4)");

        // الفاتورة
        builder.Entity<Invoice>()
            .Property(i => i.TotalValue).HasColumnType("decimal(18,4)");
        builder.Entity<Invoice>()
            .Property(i => i.Discount).HasColumnType("decimal(18,4)");
        builder.Entity<Invoice>()
            .Property(i => i.Paid).HasColumnType("decimal(18,4)");

        // عناصر الفاتورة
        builder.Entity<InvoiceItem>()
            .Property(ii => ii.UnitPrice).HasColumnType("decimal(18,4)");

        // المنتج
        builder.Entity<Product>()
            .Property(p => p.Price).HasColumnType("decimal(18,4)");
        builder.Entity<Product>()
            .Property(p => p.CostPrice).HasColumnType("decimal(18,4)");

        // العميل والمورد
        builder.Entity<Customer>()
            .Property(c => c.CashBalance).HasColumnType("decimal(18,4)");
        builder.Entity<Supplier>()
            .Property(s => s.CashBalance).HasColumnType("decimal(18,4)");

        // المعاملات المالية
        builder.Entity<CashTransaction>()
            .Property(t => t.Value).HasColumnType("decimal(18,4)");
        builder.Entity<AccountSettlement>()
            .Property(s => s.Amount).HasColumnType("decimal(18,4)");

        // الرواتب والموظفون
        builder.Entity<Employee>()
            .Property(e => e.Salary).HasColumnType("decimal(18,4)");
        builder.Entity<Employee>()
            .Property(e => e.Allowances).HasColumnType("decimal(18,4)");
        builder.Entity<Employee>()
            .Property(e => e.Deductions).HasColumnType("decimal(18,4)");
        builder.Entity<Attendance>()
            .Property(a => a.WorkingHours).HasColumnType("decimal(18,4)");
        builder.Entity<EmployeeSalary>()
            .Property(s => s.Amount).HasColumnType("decimal(18,4)");
        builder.Entity<SalaryAdjustment>()
            .Property(s => s.Amount).HasColumnType("decimal(18,4)");
        builder.Entity<RecurringAdjustment>()
            .Property(r => r.Amount).HasColumnType("decimal(18,4)");

        builder.Entity<EmployeeLoan>()
            .Property(l => l.TotalAmount).HasColumnType("decimal(18,4)");
        builder.Entity<EmployeeLoan>()
            .Property(l => l.InstallmentAmount).HasColumnType("decimal(18,4)");
        builder.Entity<LoanInstallment>()
            .Property(li => li.Amount).HasColumnType("decimal(18,4)");

        // تشغيلات الرواتب
        builder.Entity<PayrollRun>()
            .Property(p => p.BasicSalary).HasColumnType("decimal(18,4)");
        builder.Entity<PayrollRun>()
            .Property(p => p.TotalAllowances).HasColumnType("decimal(18,4)");
        builder.Entity<PayrollRun>()
            .Property(p => p.TotalDeductions).HasColumnType("decimal(18,4)");
        builder.Entity<PayrollRun>()
            .Property(p => p.LoanDeductions).HasColumnType("decimal(18,4)");
        builder.Entity<PayrollRun>()
            .Property(p => p.NetSalary).HasColumnType("decimal(18,4)");
        builder.Entity<PayrollRunItem>()
            .Property(pi => pi.Amount).HasColumnType("decimal(18,4)");

        // سجلات الرواتب القديمة (Legacy)
        builder.Entity<Payroll>()
            .Property(p => p.Salary).HasColumnType("decimal(18,4)");
        builder.Entity<Payroll>()
            .Property(p => p.Bonuses).HasColumnType("decimal(18,4)");
        builder.Entity<Payroll>()
            .Property(p => p.Deductions).HasColumnType("decimal(18,4)");

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

        builder.Entity<AccountSettlement>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StockTransaction>()
            .HasOne(st => st.User)
            .WithMany()
            .HasForeignKey(st => st.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ===== إعداد علاقات الشركة =====
        builder.Entity<CompanyPhoneNumber>()
            .HasOne(p => p.Company)
            .WithMany(c => c.PhoneNumbers)
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CompanyLogo>()
            .HasOne(l => l.Company)
            .WithOne(c => c.Logo)
            .HasForeignKey<CompanyLogo>(l => l.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

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
        optionsBuilder.AddInterceptors(new SoftDeleteAndAuditInterceptor(_currentUserService));
        base.OnConfiguring(optionsBuilder);
    }
}
