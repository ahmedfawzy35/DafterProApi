using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StoreManagement.Data;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(StoreDbContext context, ICurrentUserService currentUser, IMemoryCache cache, ILogger<DashboardService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _cache = cache;
        _logger = logger;
    }

    public async Task<DashboardStatsDto> GetDailyStatsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

        // ✅ مبيعات اليوم: Confirmed فقط + NetTotal (يشمل Tax ويطرح Discount) + خصم المرتجعات
        var todaySales = await _context.Invoices
            .Where(i => i.Type == InvoiceType.Sale
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Date >= today)
            .SumAsync(i => (decimal?)i.NetTotal) ?? 0m;

        var todayReturns = await _context.Invoices
            .Where(i => i.Type == InvoiceType.SalesReturn
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Date >= today)
            .SumAsync(i => (decimal?)i.NetTotal) ?? 0m;

        var todaySalesTotal = todaySales - todayReturns;

        // ✅ مصروفات اليوم (CashTransactions لا تزال المصدر الصحيح للمصروفات النقدية)
        var todayExpensesTotal = await _context.CashTransactions
            .Where(t => t.SourceType == TransactionSource.Expense && t.Date >= today)
            .SumAsync(t => (decimal?)t.Value) ?? 0m;

        // ✅ عدد الفواتير Confirmed اليوم
        var todayInvoicesCount = await _context.Invoices
            .CountAsync(i => i.Date >= today && i.Status == InvoiceStatus.Confirmed);

        // ✅ مبيعات الشهر: Confirmed فقط + NetTotal + خصم المرتجعات
        var monthlySales = await _context.Invoices
            .Where(i => i.Type == InvoiceType.Sale
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Date >= firstDayOfMonth)
            .SumAsync(i => (decimal?)i.NetTotal) ?? 0m;

        var monthlyReturns = await _context.Invoices
            .Where(i => i.Type == InvoiceType.SalesReturn
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Date >= firstDayOfMonth)
            .SumAsync(i => (decimal?)i.NetTotal) ?? 0m;

        var monthlySalesTotal = monthlySales - monthlyReturns;

        // ✅ ديون العملاء: مبني على NetTotal للفواتير Confirmed غير المسددة
        var totalCustomerDebts = await _context.Invoices
            .Where(i => i.Type == InvoiceType.Sale
                     && i.Status == InvoiceStatus.Confirmed
                     && i.PaymentStatus != PaymentStatus.Paid)
            .SumAsync(i => (decimal?)(i.NetTotal - i.AllocatedAmount)) ?? 0m;

        // ✅ ديون الموردين: نفس المنطق لفواتير الشراء
        var totalSupplierDebts = await _context.Invoices
            .Where(i => i.Type == InvoiceType.Purchase
                     && i.Status == InvoiceStatus.Confirmed
                     && i.PaymentStatus != PaymentStatus.Paid)
            .SumAsync(i => (decimal?)(i.NetTotal - i.AllocatedAmount)) ?? 0m;

        var topProducts = await GetTopSellingProductsAsync(5);

        var recentInvoices = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .Where(i => i.Status == InvoiceStatus.Confirmed)
            .OrderByDescending(i => i.Date)
            .Take(5)
            .Select(i => new RecentInvoiceDto
            {
                Id = i.Id,
                Type = i.Type.ToString(),
                PartnerName = i.Type == InvoiceType.Sale ? i.Customer!.Name : i.Supplier!.Name,
                TotalValue = i.NetTotal,
                Date = i.Date
            })
            .ToListAsync();

        return new DashboardStatsDto
        {
            TodaySalesToal = todaySalesTotal,
            TodayExpensesTotal = todayExpensesTotal,
            TodayInvoicesCount = todayInvoicesCount,
            MonthlySalesTotal = monthlySalesTotal,
            TotalCustomerDebts = totalCustomerDebts,
            TotalSupplierDebts = totalSupplierDebts,
            TopSellingProducts = topProducts,
            RecentInvoices = recentInvoices
        };
    }

    public async Task<FinancialSummaryDto> GetFinancialSummaryAsync()
    {
        var totalIncome = await _context.CashTransactions
            .Where(t => t.Type == TransactionType.In)
            .SumAsync(t => t.Value);

        var totalExpenses = await _context.CashTransactions
            .Where(t => t.Type == TransactionType.Out)
            .SumAsync(t => t.Value);

        return new FinancialSummaryDto
        {
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses
        };
    }

    public async Task<List<TopProductDto>> GetTopSellingProductsAsync(int count = 5)
    {
        return await _context.InvoiceItems
            .Include(ii => ii.Invoice)
            .Include(ii => ii.Product)
            .Where(ii => ii.Invoice.Type == InvoiceType.Sale && ii.Invoice.Status == InvoiceStatus.Confirmed)
            .GroupBy(ii => new { ii.ProductId, ii.Product.Name })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                Name = g.Key.Name,
                QuantitySold = g.Sum(ii => ii.Quantity),
                Revenue = g.Sum(ii => (decimal)ii.Quantity * ii.UnitPrice)
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<DebtAlertDto>> GetDebtAlertsAsync()
    {
        var companyId = _currentUser.CompanyId!.Value;

        // ديون العملاء: من فواتير البيع المؤكدة غير المسددة بالكامل
        var customerDebts = await _context.Invoices
            .Where(i => i.CompanyId == companyId
                     && i.Type == InvoiceType.Sale
                     && i.Status == InvoiceStatus.Confirmed
                     && i.PaymentStatus != PaymentStatus.Paid
                     && i.CustomerId != null)
            .GroupBy(i => new { i.CustomerId, i.Customer!.Name })
            .Select(g => new DebtAlertDto
            {
                PartnerId = g.Key.CustomerId!.Value,
                PartnerName = g.Key.Name,
                Amount = g.Sum(i => i.NetTotal - i.AllocatedAmount),
                Type = "Customer"
            })
            .Where(d => d.Amount > 0)
            .ToListAsync();

        // ديون الموردين: من فواتير الشراء المؤكدة غير المسددة بالكامل
        var supplierDebts = await _context.Invoices
            .Where(i => i.CompanyId == companyId
                     && i.Type == InvoiceType.Purchase
                     && i.Status == InvoiceStatus.Confirmed
                     && i.PaymentStatus != PaymentStatus.Paid
                     && i.SupplierId != null)
            .GroupBy(i => new { i.SupplierId, i.Supplier!.Name })
            .Select(g => new DebtAlertDto
            {
                PartnerId = g.Key.SupplierId!.Value,
                PartnerName = g.Key.Name,
                Amount = g.Sum(i => i.NetTotal - i.AllocatedAmount),
                Type = "Supplier"
            })
            .Where(d => d.Amount > 0)
            .ToListAsync();

        return customerDebts.Concat(supplierDebts)
            .OrderByDescending(d => d.Amount)
            .ToList();
    }

    // ============================================================
    // النظام الحديث (Modern Dashboard API)
    // ============================================================

    public async Task<DashboardKpiDto> GetKpisAsync()
    {
        var companyId = _currentUser.CompanyId!.Value;
        
        var today = DateTime.UtcNow.Date;
        var firstDayThisMonth = new DateTime(today.Year, today.Month, 1);
        var firstDayLastMonth = firstDayThisMonth.AddMonths(-1);
        var firstDayNextMonth = firstDayThisMonth.AddMonths(1);

        // 1. مبيعات وأرباح الشهر الحالي (مخصوماً منها المرتجعات)
        var currentMonthSalesDetails = await _context.Invoices
            .Include(i => i.Items)
            .Where(i => i.CompanyId == companyId 
                     && (i.Type == InvoiceType.Sale || i.Type == InvoiceType.SalesReturn)
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Date >= firstDayThisMonth && i.Date < firstDayNextMonth)
            .Select(i => new
            {
                i.Date,
                i.Type,
                i.NetTotal,
                TotalCost = i.Items.Sum(item => item.Quantity * (double)item.CostPriceAtSale)
            })
            .ToListAsync();

        var monthSalesTotal = currentMonthSalesDetails.Where(i => i.Type == InvoiceType.Sale).Sum(i => i.NetTotal);
        var monthReturnTotal = currentMonthSalesDetails.Where(i => i.Type == InvoiceType.SalesReturn).Sum(i => i.NetTotal);
        var monthSales = monthSalesTotal - monthReturnTotal;

        var todaySalesTotal = currentMonthSalesDetails.Where(i => i.Date >= today && i.Type == InvoiceType.Sale).Sum(i => i.NetTotal);
        var todayReturnTotal = currentMonthSalesDetails.Where(i => i.Date >= today && i.Type == InvoiceType.SalesReturn).Sum(i => i.NetTotal);
        var todaySales = todaySalesTotal - todayReturnTotal;
        
        var currentMonthCostSales = currentMonthSalesDetails.Where(i => i.Type == InvoiceType.Sale).Sum(i => (decimal)i.TotalCost);
        var currentMonthCostReturns = currentMonthSalesDetails.Where(i => i.Type == InvoiceType.SalesReturn).Sum(i => (decimal)i.TotalCost);
        var currentMonthCost = currentMonthCostSales - currentMonthCostReturns;

        var monthProfit = monthSales - currentMonthCost;
        var monthMargin = monthSales > 0 ? (monthProfit / monthSales) * 100 : 0m;

        var lastMonthSalesData = await _context.Invoices
            .Where(i => i.CompanyId == companyId 
                     && (i.Type == InvoiceType.Sale || i.Type == InvoiceType.SalesReturn)
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Date >= firstDayLastMonth && i.Date < firstDayThisMonth)
            .Select(i => new { i.Type, i.NetTotal })
            .ToListAsync();

        var lastMonthSales = lastMonthSalesData.Where(i => i.Type == InvoiceType.Sale).Sum(i => i.NetTotal) - 
                             lastMonthSalesData.Where(i => i.Type == InvoiceType.SalesReturn).Sum(i => i.NetTotal);

        var salesVsPrevious = lastMonthSales > 0 
            ? ((monthSales - lastMonthSales) / lastMonthSales) * 100 
            : 0m;

        // 3. ديون العملاء (إجمالي ما لم يُدفع من فواتير البيع) - بديل CashBalance
        var totalReceivables = await _context.Invoices
            .Where(i => i.CompanyId == companyId 
                     && i.Type == InvoiceType.Sale 
                     && i.Status == InvoiceStatus.Confirmed
                     && i.PaymentStatus != PaymentStatus.Paid
                     && i.CustomerId != null)
            .SumAsync(i => i.NetTotal - i.AllocatedAmount);
            
        // طرح سندات القبض غير المخصصة
        var unallocatedReceipts = await _context.CustomerReceipts
            .Where(r => r.CompanyId == companyId && r.UnallocatedAmount > 0)
            .SumAsync(r => r.UnallocatedAmount);
            
        totalReceivables -= unallocatedReceipts;

        // 4. ديون الموردين (إجمالي ما لم يُدفع من فواتير الشراء)
        var totalPayables = await _context.Invoices
            .Where(i => i.CompanyId == companyId 
                     && i.Type == InvoiceType.Purchase 
                     && i.Status == InvoiceStatus.Confirmed
                     && i.PaymentStatus != PaymentStatus.Paid
                     && i.SupplierId != null)
            .SumAsync(i => i.NetTotal - i.AllocatedAmount);
            
        // طرح سندات الصرف غير المخصصة
        var unallocatedPayments = await _context.SupplierPayments
            .Where(p => p.CompanyId == companyId && p.UnallocatedAmount > 0)
            .SumAsync(p => p.UnallocatedAmount);
            
        totalPayables -= unallocatedPayments;

        // 5. المنتجات منخفضة المخزون
        var lowStockCount = await _context.Products
            .Where(p => p.CompanyId == companyId && p.IsActive && p.StockQuantity <= p.MinimumStock)
            .CountAsync();

        // 6. الفواتير المعلقة
        var openInvoicesCount = await _context.Invoices
            .Where(i => i.CompanyId == companyId 
                     && i.Type == InvoiceType.Sale 
                     && i.Status == InvoiceStatus.Confirmed
                     && i.PaymentStatus != PaymentStatus.Paid)
            .CountAsync();

        return new DashboardKpiDto
        {
            TodaySales = todaySales,
            MonthSales = monthSales,
            MonthSalesVsPrevious = salesVsPrevious,
            MonthProfit = monthProfit,
            MonthMargin = monthMargin,
            TotalReceivables = totalReceivables,
            TotalPayables = totalPayables,
            LowStockItemsCount = lowStockCount
        };
    }

    public async Task<List<CustomerReadDto>> GetTopCustomersAsync(int count = 5)
    {
        var companyId = _currentUser.CompanyId!.Value;

        // إرجاع أعلى عملاء من حيث المبيعات خلال فترة 90 يوماً للتعبير عن النشاط الحديث
        var cutoff = DateTime.UtcNow.AddDays(-90);

        var topCustomerIds = await _context.Invoices
            .Where(i => i.CompanyId == companyId
                     && i.Type == InvoiceType.Sale
                     && i.Status == InvoiceStatus.Confirmed
                     && i.CustomerId != null
                     && i.Date >= cutoff)
            .GroupBy(i => i.CustomerId)
            .OrderByDescending(g => g.Sum(i => i.NetTotal))
            .Take(count)
            .Select(g => g.Key)
            .ToListAsync();

        var customers = await _context.Customers
            .Include(c => c.Phones)
            .Where(c => topCustomerIds.Contains(c.Id))
            .ToListAsync();

        // نحافظ على الترتيب الأصلي للـ GroupBy (من الأعلى مبيعاً)
        return topCustomerIds
            .Select(id => customers.First(c => c.Id == id))
            .Select(c => new CustomerReadDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                PrimaryPhone = c.Phones.FirstOrDefault(p => p.IsPrimary)?.PhoneNumber
                    ?? c.Phones.FirstOrDefault()?.PhoneNumber,
                IsActive = c.IsActive
            })
            .ToList();
    }

    public async Task<List<ProductReadDto>> GetLowStockProductsAsync()
    {
        var companyId = _currentUser.CompanyId!.Value;

        var products = await _context.Products
            .Where(p => p.CompanyId == companyId && p.IsActive && p.StockQuantity <= p.MinimumStock)
            .OrderBy(p => p.StockQuantity)
            .Take(50) // أقصى حد 50 للوحة التحكم تجنباً للأحجام الضخمة
            .Select(p => new ProductReadDto
            {
                Id = p.Id,
                Name = p.Name,
                SKU = p.SKU,
                Barcode = p.Barcode,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                MinimumStock = p.MinimumStock,
                Unit = p.Unit,
                IsActive = true
            })
            .ToListAsync();

        return products;
    }

    public async Task<BranchDashboardKpiDto> GetBranchKpisAsync(int? branchId = null)
    {
        var companyId = _currentUser.CompanyId!.Value;
        
        int? targetBranchId = null;
        bool isAdmin = _currentUser.IsSuperAdmin || _currentUser.Roles.Contains("admin") || _currentUser.Roles.Contains("owner");
        
        if (isAdmin)
        {
            targetBranchId = branchId;
        }
        else
        {
            targetBranchId = _currentUser.BranchId;
            if (branchId.HasValue && branchId.Value != targetBranchId)
            {
                _logger.LogWarning("User {UserId} attempted to access Dashboard for unauthorized branch {RequestedBranch}. Forced to their assigned branch {AssignedBranch}.", _currentUser.UserId, branchId.Value, targetBranchId);
            }
        }

        var cacheBranch = targetBranchId?.ToString() ?? "all";
        var cacheKey = $"dashboard_branch_kpis_{companyId}_{cacheBranch}";

        if (_cache.TryGetValue(cacheKey, out BranchDashboardKpiDto? cached) && cached != null)
        {
            _logger.LogInformation("Cache hit for key {CacheKey}", cacheKey);
            return cached;
        }
        _logger.LogInformation("Cache miss for key {CacheKey}. Fetching from DB.", cacheKey);

        // 1. تقييم المخزون الحالي (BranchProductStocks)
        var baseStockQuery = _context.BranchProductStocks
            .AsNoTracking()
            .Where(bps => bps.CompanyId == companyId);

        if (targetBranchId.HasValue && targetBranchId.Value > 0)
        {
            baseStockQuery = baseStockQuery.Where(bps => bps.BranchId == targetBranchId.Value);
        }

        var totalStockQuantity = await baseStockQuery.SumAsync(bps => (double?)bps.Quantity) ?? 0;
        
        var lowStockQuery = baseStockQuery
            .Where(bps => bps.Product.MinimumStock > 0 && bps.Quantity <= bps.Product.MinimumStock);

        var lowStockItemsCount = await lowStockQuery.CountAsync();

        var topLowStockItems = await lowStockQuery
            .OrderBy(bps => bps.Quantity - bps.Product.MinimumStock)
            .Take(5)
            .Select(bps => new LowStockAlertDto
            {
                BranchId = bps.BranchId,
                BranchName = bps.Branch.Name,
                ProductId = bps.ProductId,
                ProductName = bps.Product.Name,
                SKU = bps.Product.SKU,
                Unit = bps.Product.Unit ?? "",
                Quantity = bps.Quantity,
                MinimumStock = bps.Product.MinimumStock,
                ShortageQuantity = bps.Product.MinimumStock - bps.Quantity
            })
            .ToListAsync();

        // 2. حركات الشهر الحالي
        var today = DateTime.UtcNow.Date;
        var firstDayThisMonth = new DateTime(today.Year, today.Month, 1);
        
        var recentMovementsQuery = _context.StockTransactions
            .AsNoTracking()
            .Where(st => st.CompanyId == companyId && st.Date >= firstDayThisMonth);
            
        if (targetBranchId.HasValue && targetBranchId.Value > 0)
        {
            recentMovementsQuery = recentMovementsQuery.Where(st => st.BranchId == targetBranchId.Value);
        }
        
        var recentMovementsCount = await recentMovementsQuery.CountAsync();

        // 3. توزيع كميات المخزون
        List<BranchStockSummaryDto> stockDistribution = new();
        var isAuthorizedForDistribution = _currentUser.IsSuperAdmin || 
                                          _currentUser.Roles.Contains("admin") || 
                                          _currentUser.Roles.Contains("owner");
                                          
        if (isAuthorizedForDistribution)
        {
            stockDistribution = await _context.BranchProductStocks
                .AsNoTracking()
                .Where(bps => bps.CompanyId == companyId)
                .GroupBy(bps => new { bps.BranchId, bps.Branch.Name })
                .Select(g => new BranchStockSummaryDto
                {
                    BranchId = g.Key.BranchId,
                    BranchName = g.Key.Name,
                    TotalQuantity = g.Sum(bps => bps.Quantity)
                })
                .OrderByDescending(b => b.TotalQuantity)
                .ToListAsync();
        }

        string resolvedBranchName = "كل الفروع";
        if (targetBranchId.HasValue && targetBranchId.Value > 0)
        {
            resolvedBranchName = await _context.Branches
                .Where(b => b.Id == targetBranchId.Value)
                .Select(b => b.Name)
                .FirstOrDefaultAsync() ?? "غير معروف";
        }

        var result = new BranchDashboardKpiDto
        {
            BranchId = targetBranchId ?? 0,
            BranchName = resolvedBranchName,
            LowStockItemsCount = lowStockItemsCount,
            TotalStockQuantity = totalStockQuantity,
            RecentMovementsCount = recentMovementsCount,
            TopLowStockItems = topLowStockItems,
            StockDistribution = stockDistribution
        };

        _cache.Set(cacheKey, result, TimeSpan.FromSeconds(10));
        return result;
    }
}
