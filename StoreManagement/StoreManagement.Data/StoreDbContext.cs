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
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductCostHistory> ProductCostHistories => Set<ProductCostHistory>();

    // ===== الفواتير والمخزون =====
    public DbSet<BranchProductStock> BranchProductStocks => Set<BranchProductStock>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockAdjustmentItem> StockAdjustmentItems => Set<StockAdjustmentItem>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();

    // ===== المعاملات النقدية والديون =====
    public DbSet<CashRegisterShift> CashRegisterShifts => Set<CashRegisterShift>();
    public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();
    public DbSet<AccountSettlement> AccountSettlements => Set<AccountSettlement>();
    public DbSet<CustomerReceipt> CustomerReceipts => Set<CustomerReceipt>();

    public DbSet<CustomerReceiptAllocation> CustomerReceiptAllocations => Set<CustomerReceiptAllocation>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<SupplierPaymentAllocation> SupplierPaymentAllocations => Set<SupplierPaymentAllocation>();

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

        // ===== التصفية الديناميكية للكيانات (Dynamic Query Filters) =====
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            // إذا كان الكيان فرعياً (IBranchEntity)
            if (typeof(IBranchEntity).IsAssignableFrom(clrType))
            {
                var method = typeof(StoreDbContext).GetMethod(nameof(ConfigureBranchFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                method.MakeGenericMethod(clrType).Invoke(this, new object[] { builder });
            }
            // إذا كان الكيان تابعاً لشركة ومختلفاً عن الفرع (ICompanyEntity)
            else if (typeof(ICompanyEntity).IsAssignableFrom(clrType) && clrType != typeof(Company))
            {
                var method = typeof(StoreDbContext).GetMethod(nameof(ConfigureCompanyFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
                method.MakeGenericMethod(clrType).Invoke(this, new object[] { builder });
            }
            // إذا كان مجرد BaseEntity بدون واجهات الصلاحيات
            else if (typeof(IAuditEntity).IsAssignableFrom(clrType) && clrType == typeof(Company))
            {
                builder.Entity<Company>().HasQueryFilter(c => !c.IsDeleted);
            }
        }

        // ===== الفهارس (Indexes) لتحسين الأداء وتطبيق القيود =====
        builder.Entity<Customer>().HasIndex(c => c.CompanyId);
        builder.Entity<Customer>()
            .HasIndex(c => new { c.CompanyId, c.Name })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        // فهرس Code - فريد داخل الشركة إن وُجد (يسمح بـ null)
        builder.Entity<Customer>()
            .HasIndex(c => new { c.CompanyId, c.Code })
            .IsUnique()
            .HasFilter("[Code] IS NOT NULL AND [IsDeleted] = 0");
        // فهرس IsActive لتسريع الفلتر الافتراضي
        builder.Entity<Customer>().HasIndex(c => c.IsActive);

        builder.Entity<Supplier>().HasIndex(s => s.CompanyId);
        builder.Entity<Supplier>()
            .HasIndex(s => new { s.CompanyId, s.Name })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        // فهرس Code - فريد داخل الشركة إن وُجد
        builder.Entity<Supplier>()
            .HasIndex(s => new { s.CompanyId, s.Code })
            .IsUnique()
            .HasFilter("[Code] IS NOT NULL AND [IsDeleted] = 0");
        // فهرس IsActive
        builder.Entity<Supplier>().HasIndex(s => s.IsActive);

        builder.Entity<Product>().HasIndex(p => p.CompanyId);
        builder.Entity<Product>()
            .HasIndex(p => new { p.CompanyId, p.Name })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // فهرس فريد للباركود داخل كل شركة (خط الدفاع الأساسي ضد التكرار)
        builder.Entity<Product>()
            .HasIndex(p => new { p.CompanyId, p.Barcode })
            .IsUnique()
            .HasFilter($"[{nameof(Product.Barcode)}] != '' AND [IsDeleted] = 0"); 

        // فهرس فريد لـ SKU داخل كل شركة
        builder.Entity<Product>()
            .HasIndex(p => new { p.CompanyId, p.SKU })
            .IsUnique()
            .HasFilter($"[{nameof(Product.SKU)}] IS NOT NULL AND [{nameof(Product.SKU)}] != '' AND [IsDeleted] = 0");

        // فهارس إضافية لتحسين بحث المنتجات
        builder.Entity<Product>().HasIndex(p => p.CategoryId);
        builder.Entity<Product>().HasIndex(p => p.IsActive);

        // إعداد علاقة المنتج بالتصنيف
        builder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // إعداد هيكل الفئات الهرمية
        builder.Entity<ProductCategory>()
            .HasOne(pc => pc.ParentCategory)
            .WithMany(pc => pc.SubCategories)
            .HasForeignKey(pc => pc.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // ===== إعداد هيكل المخزون الفرعي (BranchProductStock) =====
        
        // قيد فريد يمنع تكرار المنتج في نفس الفرع لأكثر من سجل
        builder.Entity<BranchProductStock>()
            .HasIndex(bps => new { bps.ProductId, bps.BranchId })
            .IsUnique();

        // فهارس إضافية للبحث
        builder.Entity<BranchProductStock>().HasIndex(bps => bps.BranchId);
        builder.Entity<BranchProductStock>().HasIndex(bps => bps.ProductId);

        // إعداد الـ Query Filter يدوياً للشركة وعزلها
        builder.Entity<BranchProductStock>()
            .HasQueryFilter(bps =>
                (!_currentUserService.IsPlatformUser && bps.CompanyId == _currentUserService.CompanyId)
                ||
                (_currentUserService.IsPlatformUser && (_currentUserService.ScopedCompanyId == null || bps.CompanyId == _currentUserService.ScopedCompanyId))
            );

        // إعداد Concurrency Token
        builder.Entity<BranchProductStock>()
            .Property(bps => bps.RowVersion)
            .IsRowVersion();

        // منع القيم السالبة في حقل ReservedQuantity على مستوى قاعدة البيانات
        builder.Entity<BranchProductStock>()
            .ToTable(t => t.HasCheckConstraint("CK_BranchProductStock_ReservedQuantity", "[ReservedQuantity] >= 0"));

        builder.Entity<Invoice>().HasIndex(i => i.CompanyId);
        builder.Entity<Invoice>().HasIndex(i => new { i.CompanyId, i.Date });

        builder.Entity<InvoiceItem>()
            .HasOne(i => i.OriginalInvoiceItem)
            .WithMany()
            .HasForeignKey(i => i.OriginalInvoiceItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<InvoiceItem>().HasIndex(i => i.OriginalInvoiceItemId);

        builder.Entity<StockTransaction>().HasIndex(st => new { st.ReferenceType, st.ReferenceId });

        builder.Entity<Employee>().HasIndex(e => e.CompanyId);

        builder.Entity<CashTransaction>().HasIndex(t => t.CompanyId);
        builder.Entity<CashTransaction>().HasIndex(t => new { t.CompanyId, t.Date });
        builder.Entity<CashTransaction>().HasIndex(t => t.ShiftId);

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
        builder.Entity<Invoice>()
            .Property(i => i.AllocatedAmount).HasColumnType("decimal(18,4)");
        builder.Entity<Invoice>()
            .Property(i => i.Tax).HasColumnType("decimal(18,4)");

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
        // حقل رصيد الافتتاح الجديد
        builder.Entity<Customer>()
            .Property(c => c.OpeningBalance).HasColumnType("decimal(18,4)");
        // حقل الحد الائتماني الجديد
        builder.Entity<Customer>()
            .Property(c => c.CreditLimit).HasColumnType("decimal(18,4)");

        builder.Entity<Supplier>()
            .Property(s => s.CashBalance).HasColumnType("decimal(18,4)");
        // حقل رصيد الافتتاح الجديد
        builder.Entity<Supplier>()
            .Property(s => s.OpeningBalance).HasColumnType("decimal(18,4)");

        // المعاملات المالية
        builder.Entity<CashTransaction>()
            .Property(t => t.Value).HasColumnType("decimal(18,4)");
        builder.Entity<AccountSettlement>()
            .Property(s => s.Amount).HasColumnType("decimal(18,4)");

        // إيصالات الدفع والتخصيص
        builder.Entity<CustomerReceipt>().Property(x => x.Amount).HasColumnType("decimal(18,4)");
        builder.Entity<CustomerReceipt>().Property(x => x.UnallocatedAmount).HasColumnType("decimal(18,4)");
        builder.Entity<CustomerReceiptAllocation>().Property(x => x.Amount).HasColumnType("decimal(18,4)");
        
        builder.Entity<SupplierPayment>().Property(x => x.Amount).HasColumnType("decimal(18,4)");
        builder.Entity<SupplierPayment>().Property(x => x.UnallocatedAmount).HasColumnType("decimal(18,4)");
        builder.Entity<SupplierPaymentAllocation>().Property(x => x.Amount).HasColumnType("decimal(18,4)");

        // الرواتب والموظفون
        builder.Entity<Employee>()
            .Property(e => e.Salary).HasColumnType("decimal(18,4)");
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

        builder.Entity<Employee>()
            .HasOne(e => e.CurrentBranch)
            .WithMany()
            .HasForeignKey(e => e.CurrentBranchId)
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

        builder.Entity<StockAdjustment>()
            .HasOne(sa => sa.User)
            .WithMany()
            .HasForeignKey(sa => sa.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StockTransfer>()
            .HasOne(st => st.User)
            .WithMany()
            .HasForeignKey(st => st.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // إعداد علاقات الإيصالات والتخصيص
        builder.Entity<CustomerReceiptAllocation>()
            .HasOne(a => a.Invoice)
            .WithMany(i => i.CustomerAllocations)
            .HasForeignKey(a => a.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CustomerReceiptAllocation>()
            .HasIndex(a => new { a.CustomerReceiptId, a.InvoiceId }).IsUnique();

        builder.Entity<SupplierPaymentAllocation>()
            .HasOne(a => a.Invoice)
            .WithMany(i => i.SupplierAllocations)
            .HasForeignKey(a => a.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SupplierPaymentAllocation>()
            .HasIndex(a => new { a.SupplierPaymentId, a.InvoiceId }).IsUnique();

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

    private void ConfigureBranchFilter<TEntity>(ModelBuilder builder) where TEntity : class, IBranchEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e =>
            !e.IsDeleted &&
            (
                (!_currentUserService.IsPlatformUser && e.CompanyId == _currentUserService.CompanyId && (_currentUserService.BranchId == null || e.BranchId == _currentUserService.BranchId))
                ||
                (_currentUserService.IsPlatformUser && (_currentUserService.ScopedCompanyId == null || e.CompanyId == _currentUserService.ScopedCompanyId) && (_currentUserService.BranchId == null || e.BranchId == _currentUserService.BranchId))
            )
        );
    }

    private void ConfigureCompanyFilter<TEntity>(ModelBuilder builder) where TEntity : class, ICompanyEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e =>
            !e.IsDeleted &&
            (
                (!_currentUserService.IsPlatformUser && e.CompanyId == _currentUserService.CompanyId)
                ||
                (_currentUserService.IsPlatformUser && (_currentUserService.ScopedCompanyId == null || e.CompanyId == _currentUserService.ScopedCompanyId))
            )
        );
    }
}


