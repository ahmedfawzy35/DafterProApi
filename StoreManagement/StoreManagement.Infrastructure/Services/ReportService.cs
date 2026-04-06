using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

/// <summary>
/// خدمة التقارير المالية والإدارية (أعمار الديون، أرباح، مبيعات)
/// تعتمد على IMemoryCache لتحسين الأداء لأن التقارير ثقيلة ولا تحتاج لدقة لحظية (Real-time) في كل طلب.
/// </summary>
public class ReportService : IReportService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ReportService> _logger;

    // مدة تخزين التقارير في الذاكرة لتخفيف الضغط (5 دقائق)
    private static readonly TimeSpan ReportCacheTtl = TimeSpan.FromMinutes(5);

    public ReportService(
        StoreDbContext context,
        ICurrentUserService currentUser,
        IMemoryCache cache,
        ILogger<ReportService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _cache = cache;
        _logger = logger;
    }

    // ==========================================
    // 1. تقارير أعمار الديون (Aging Reports)
    // ==========================================

    public async Task<List<AgingReportRowDto>> GetCustomerAgingReportAsync(bool excludeZeroBalances = true)
    {
        var companyId = _currentUser.CompanyId!.Value;
        var cacheKey = $"report_aging_cust_{companyId}_{excludeZeroBalances}";

        if (_cache.TryGetValue(cacheKey, out List<AgingReportRowDto>? cached) && cached is not null)
            return cached;

        // 1. الحصول على الديون المفتوحة (الفواتير التي لم تُسدد بالكامل)
        var openInvoices = await _context.Invoices
            .Include(i => i.Customer)
            .Where(i => i.CompanyId == companyId
                     && i.Type == InvoiceType.Sale
                     && i.Status == InvoiceStatus.Confirmed
                     && i.PaymentStatus != PaymentStatus.Paid
                     && i.CustomerId != null)
            .ToListAsync();

        // 2. توزيع الديون على فترات التقادم
        // ⚠️ القاعدة: الـ buckets (Current, 31-60, 61-90, Over90) تُجمع لتساوي Total دائماً
        // UnallocatedCredit يُعرض منفصلاً بدون المسّ بالـ buckets
        var today = DateTime.UtcNow.Date;
        var reportBase = openInvoices
            .GroupBy(i => new { i.CustomerId, i.Customer!.Name, i.Customer.Code })
            .Select(g =>
            {
                var row = new AgingReportRowDto
                {
                    PartnerId = g.Key.CustomerId!.Value,
                    PartnerName = g.Key.Name,
                    PartnerCode = g.Key.Code
                    // Total محسوب تلقائياً: Total = Current + Days31_60 + Days61_90 + Over90
                };

                foreach (var inv in g)
                {
                    var daysOld = (today - inv.Date.Date).Days;
                    var amount = inv.RemainingAmount;

                    if (daysOld <= 30) row.Current += amount;
                    else if (daysOld <= 60) row.Days31_60 += amount;
                    else if (daysOld <= 90) row.Days61_90 += amount;
                    else row.Over90 += amount;
                }
                return row;
            })
            .ToList();

        // 3. جلب الأرصدة غير المخصصة لكل عميل (batch query واحدة)
        //    لا تُعدَّل بها الـ buckets — تُعرض في UnallocatedCredit منفصلاً
        //    NetBalance = Total - UnallocatedCredit هو الرقم الحقيقي للمتابعة
        var unallocatedReceipts = await _context.CustomerReceipts
            .Where(r => r.CompanyId == companyId && r.UnallocatedAmount > 0)
            .GroupBy(r => r.CustomerId)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(r => r.UnallocatedAmount));

        foreach (var row in reportBase)
        {
            if (unallocatedReceipts.TryGetValue(row.PartnerId, out var unallocated))
                row.UnallocatedCredit = unallocated;
        }

        // 4. الفلترة: نستبعد الصفوف التي NetBalance صفر أو أقل (لا دين فعلي)
        if (excludeZeroBalances)
            reportBase = reportBase.Where(r => r.NetBalance > 0).ToList();

        var result = reportBase.OrderByDescending(r => r.NetBalance).ToList();

        _cache.Set(cacheKey, result, ReportCacheTtl);
        return result;
    }

    public async Task<List<AgingReportRowDto>> GetSupplierAgingReportAsync(bool excludeZeroBalances = true)
    {
        var companyId = _currentUser.CompanyId!.Value;
        var cacheKey = $"report_aging_supp_{companyId}_{excludeZeroBalances}";

        if (_cache.TryGetValue(cacheKey, out List<AgingReportRowDto>? cached) && cached is not null)
            return cached;

        var openInvoices = await _context.Invoices
            .Include(i => i.Supplier)
            .Where(i => i.CompanyId == companyId
                     && i.Type == InvoiceType.Purchase
                     && i.Status == InvoiceStatus.Confirmed
                     && i.PaymentStatus != PaymentStatus.Paid
                     && i.SupplierId != null)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var reportBase = openInvoices
            .GroupBy(i => new { i.SupplierId, i.Supplier!.Name, i.Supplier.Code })
            .Select(g =>
            {
                var row = new AgingReportRowDto
                {
                    PartnerId = g.Key.SupplierId!.Value,
                    PartnerName = g.Key.Name,
                    PartnerCode = g.Key.Code
                    // Total محسوب تلقائياً = Current + Days31_60 + Days61_90 + Over90
                };

                foreach (var inv in g)
                {
                    var daysOld = (today - inv.Date.Date).Days;
                    var amount = inv.RemainingAmount;

                    if (daysOld <= 30) row.Current += amount;
                    else if (daysOld <= 60) row.Days31_60 += amount;
                    else if (daysOld <= 90) row.Days61_90 += amount;
                    else row.Over90 += amount;
                }
                return row;
            })
            .ToList();

        // UnallocatedCredit منفصل — لا يعدّل الـ buckets لضمان الاتساق
        var unallocatedPayments = await _context.SupplierPayments
            .Where(p => p.CompanyId == companyId && p.UnallocatedAmount > 0)
            .GroupBy(p => p.SupplierId)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(p => p.UnallocatedAmount));

        foreach (var row in reportBase)
        {
            if (unallocatedPayments.TryGetValue(row.PartnerId, out var unallocated))
                row.UnallocatedCredit = unallocated;
        }

        if (excludeZeroBalances)
            reportBase = reportBase.Where(r => r.NetBalance > 0).ToList();

        var result = reportBase.OrderByDescending(r => r.NetBalance).ToList();

        _cache.Set(cacheKey, result, ReportCacheTtl);
        return result;
    }

    // ==========================================
    // 2. تقارير المبيعات والأرباح (Sales & Profit)
    // ==========================================

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(DateTime? from, DateTime? to)
    {
        var companyId = _currentUser.CompanyId!.Value;
        
        var fromDate = from ?? DateTime.UtcNow.Date.AddDays(-30);
        var toDate = (to ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);

        var cacheKey = $"report_sales_summary_{companyId}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}";
        if (_cache.TryGetValue(cacheKey, out SalesSummaryDto? cached) && cached is not null)
            return cached;

        // جلب جميع فواتير المبيعات في الفترة
        var sales = await _context.Invoices
            .Include(i => i.Items)
            .Where(i => i.CompanyId == companyId
                     && i.Type == InvoiceType.Sale
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Date >= fromDate && i.Date <= toDate)
            .ToListAsync();

        var summary = new SalesSummaryDto
        {
            From = fromDate,
            To = toDate,
            InvoiceCount = sales.Count,
            TotalRevenue = sales.Sum(i => i.NetTotal), // تم استبدال TotalValue بـ NetTotal لمعالجة الخصومات والضرائب
            TotalDiscount = sales.Sum(i => i.Discount),
            TotalTax = sales.Sum(i => i.Tax),
            NetRevenue = sales.Sum(i => i.NetTotal)
        };

        // حساب التكلفة الإجمالية من الـ CostPriceAtSale المسجل داخل سطور الفاتورة
        summary.TotalCost = sales.SelectMany(i => i.Items).Sum(item => item.TotalCost);
        
        // الأرباح
        summary.GrossProfit = summary.NetRevenue - summary.TotalCost;
        summary.GrossMargin = summary.NetRevenue > 0 ? (summary.GrossProfit / summary.NetRevenue) * 100 : 0;

        // خصم مرتجعات المبيعات (نطرحها من الملخص لنحصل على الصافي الحقيقي)
        var returns = await _context.Invoices
            .Include(i => i.Items)
            .Where(i => i.CompanyId == companyId
                     && i.Type == InvoiceType.SalesReturn
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Date >= fromDate && i.Date <= toDate)
            .ToListAsync();

        if (returns.Any())
        {
            var returnRevenue = returns.Sum(i => i.NetTotal);
            var returnCost = returns.SelectMany(i => i.Items).Sum(item => item.TotalCost);
            
            summary.NetRevenue -= returnRevenue;
            summary.TotalCost -= returnCost;
            summary.GrossProfit = summary.NetRevenue - summary.TotalCost;
            summary.GrossMargin = summary.NetRevenue > 0 ? (summary.GrossProfit / summary.NetRevenue) * 100 : 0;
        }

        _cache.Set(cacheKey, summary, ReportCacheTtl);
        return summary;
    }

    public async Task<PagedResult<InvoiceProfitDto>> GetInvoiceProfitabilityAsync(PaginationQueryDto query, DateTime? from, DateTime? to)
    {
        var companyId = _currentUser.CompanyId!.Value;
        
        var baseQuery = _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Where(i => i.CompanyId == companyId
                     && i.Type == InvoiceType.Sale
                     && i.Status == InvoiceStatus.Confirmed);

        if (from.HasValue) baseQuery = baseQuery.Where(i => i.Date >= from.Value);
        if (to.HasValue)
        {
            var toDate = to.Value.Date.AddDays(1).AddTicks(-1);
            baseQuery = baseQuery.Where(i => i.Date <= toDate);
        }

        var totalCount = await baseQuery.CountAsync();

        var invoices = await baseQuery
            .OrderByDescending(i => i.Date)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var items = invoices.Select(i => 
        {
            var totalCost = i.Items.Sum(item => item.TotalCost);
            var netSales = i.NetTotal;
            var profit = netSales - totalCost;

            return new InvoiceProfitDto
            {
                InvoiceId = i.Id,
                Date = i.Date,
                CustomerName = i.Customer?.Name ?? "عميل نقدي",
                TotalSales = netSales,
                TotalCost = totalCost,
                Profit = profit,
                ProfitMargin = netSales > 0 ? (profit / netSales) * 100 : 0
            };
        }).ToList();

        return new PagedResult<InvoiceProfitDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    // ==========================================
    // 3. تقارير المخزون والفروع (Inventory Reports)
    // ==========================================

    public async Task<PagedResult<StockPerBranchReportDto>> GetStockPerBranchReportAsync(PaginationQueryDto query, int? branchId, int? productId)
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
                _logger.LogWarning("User {UserId} attempted to access StockPerBranchReport for unauthorized branch {RequestedBranch}. Forced to their assigned branch {AssignedBranch}.", _currentUser.UserId, branchId.Value, targetBranchId);
            }
        }

        // Lightweight cache (10 seconds)
        var cacheBranch = targetBranchId?.ToString() ?? "all";
        var cacheKey = $"report_stock_branch_{companyId}_{cacheBranch}_{productId}_{query.PageNumber}_{query.PageSize}_{query.Search}";
        
        if (_cache.TryGetValue(cacheKey, out PagedResult<StockPerBranchReportDto>? cached) && cached is not null)
        {
            _logger.LogInformation("Cache hit for key {CacheKey}", cacheKey);
            return cached;
        }
        _logger.LogInformation("Cache miss for key {CacheKey}. Fetching from DB.", cacheKey);

        var baseQuery = _context.BranchProductStocks
            .AsNoTracking()
            .Where(bps => bps.CompanyId == companyId);

        if (targetBranchId.HasValue)
            baseQuery = baseQuery.Where(bps => bps.BranchId == targetBranchId.Value);

        if (productId.HasValue)
            baseQuery = baseQuery.Where(bps => bps.ProductId == productId.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            baseQuery = baseQuery.Where(bps => 
                EF.Functions.Like(bps.Product.Name, $"%{query.Search}%") || 
                EF.Functions.Like(bps.Branch.Name, $"%{query.Search}%"));
        }

        var totalCount = await baseQuery.CountAsync();

        var items = await baseQuery
            .OrderBy(bps => bps.Branch.Name)
            .ThenBy(bps => bps.Product.Name)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(bps => new StockPerBranchReportDto
            {
                BranchId = bps.BranchId,
                BranchName = bps.Branch.Name,
                ProductId = bps.ProductId,
                ProductName = bps.Product.Name,
                Quantity = bps.Quantity,
                ReservedQuantity = bps.ReservedQuantity,
                AvailableQuantity = bps.Quantity - bps.ReservedQuantity
            })
            .ToListAsync();

        var result = new PagedResult<StockPerBranchReportDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };

        _cache.Set(cacheKey, result, TimeSpan.FromSeconds(10));
        return result;
    }

    public async Task<PagedResult<BranchInventoryMovementReportDto>> GetBranchInventoryMovementsReportAsync(PaginationQueryDto query, int? branchId, int? productId, DateTime? from, DateTime? to)
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
                _logger.LogWarning("User {UserId} attempted to access BranchInventoryMovements for unauthorized branch {RequestedBranch}. Forced to their assigned branch {AssignedBranch}.", _currentUser.UserId, branchId.Value, targetBranchId);
            }
        }
        
        var baseQuery = _context.StockTransactions
            .AsNoTracking()
            .Where(st => st.CompanyId == companyId && st.BranchId > 0);

        if (targetBranchId.HasValue)
            baseQuery = baseQuery.Where(st => st.BranchId == targetBranchId.Value);

        if (productId.HasValue)
            baseQuery = baseQuery.Where(st => st.ProductId == productId.Value);

        if (from.HasValue)
            baseQuery = baseQuery.Where(st => st.Date >= from.Value);

        if (to.HasValue)
        {
            var toDateExclusive = to.Value.Date.AddDays(1);
            baseQuery = baseQuery.Where(st => st.Date < toDateExclusive);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            baseQuery = baseQuery.Where(st => 
                EF.Functions.Like(st.Product.Name, $"%{query.Search}%") || 
                (st.Notes != null && EF.Functions.Like(st.Notes, $"%{query.Search}%")));
        }

        var totalCount = await baseQuery.CountAsync();

        var items = await (
                from st in baseQuery
                join b in _context.Branches on st.BranchId equals b.Id into bj
                from branch in bj.DefaultIfEmpty()
                select new BranchInventoryMovementReportDto
                {
                    Date = st.Date,
                    ProductId = st.ProductId,
                    ProductName = st.Product.Name,
                    BranchId = st.BranchId,
                    BranchName = branch != null ? branch.Name : "غير معروف",
                    MovementType = st.MovementType,
                    Quantity = st.Quantity,
                    BeforeQuantity = st.BeforeQuantity,
                    AfterQuantity = st.AfterQuantity,
                    ReferenceId = st.ReferenceId
                })
            .OrderByDescending(dto => dto.Date)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<BranchInventoryMovementReportDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProductStockDistributionDto> GetProductStockDistributionAsync(int productId)
    {
        var companyId = _currentUser.CompanyId!.Value;

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.CompanyId == companyId);
        if (product == null) throw new KeyNotFoundException("المنتج غير موجود.");

        var allocations = await _context.BranchProductStocks
            .AsNoTracking()
            .Where(bps => bps.CompanyId == companyId && bps.ProductId == productId)
            .Select(bps => new BranchStockAllocationDto
            {
                BranchId = bps.BranchId,
                BranchName = bps.Branch.Name,
                Quantity = bps.Quantity - bps.ReservedQuantity
            })
            .ToListAsync();

        return new ProductStockDistributionDto
        {
            ProductId = productId,
            ProductName = product.Name,
            TotalQuantity = allocations.Sum(a => a.Quantity),
            Branches = allocations
        };
    }
}
