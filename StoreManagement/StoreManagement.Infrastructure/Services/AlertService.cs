using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

public class AlertService : IAlertService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AlertService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(10); // احتفاظ قصير للأداء العالي

    public AlertService(StoreDbContext context, ICurrentUserService currentUser, IMemoryCache cache, ILogger<AlertService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResult<LowStockAlertDto>> GetLowStockAlertsAsync(PaginationQueryDto query, int? branchId = null)
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
                _logger.LogWarning("User {UserId} attempted to access LowStockAlerts for unauthorized branch {RequestedBranch}. Forced to their assigned branch {AssignedBranch}.", _currentUser.UserId, branchId.Value, targetBranchId);
            }
        }

        var cacheBranch = targetBranchId?.ToString() ?? "all";
        var cacheKey = $"alerts_lowstock_{companyId}_{cacheBranch}_{query.PageNumber}_{query.PageSize}_{query.Search}";

        if (_cache.TryGetValue(cacheKey, out PagedResult<LowStockAlertDto>? cached) && cached != null)
        {
            _logger.LogInformation("Cache hit for key {CacheKey}", cacheKey);
            return cached;
        }
        _logger.LogInformation("Cache miss for key {CacheKey}. Fetching from DB.", cacheKey);

        var baseQuery = _context.BranchProductStocks
            .AsNoTracking()
            .Where(bps => bps.CompanyId == companyId && bps.Product.MinimumStock > 0 && bps.Quantity <= bps.Product.MinimumStock);

        if (targetBranchId.HasValue && targetBranchId.Value > 0)
            baseQuery = baseQuery.Where(bps => bps.BranchId == targetBranchId.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            baseQuery = baseQuery.Where(bps => 
                EF.Functions.Like(bps.Product.Name, $"%{query.Search}%") || 
                EF.Functions.Like(bps.Product.SKU, $"%{query.Search}%") || 
                EF.Functions.Like(bps.Branch.Name, $"%{query.Search}%"));
        }

        var total = await baseQuery.CountAsync();
        
        var rawItems = await baseQuery
            .OrderBy(bps => bps.Quantity - bps.Product.MinimumStock) // الأكثر عجزاً أولاً
            .ThenBy(bps => bps.Branch.Name)
            .ThenBy(bps => bps.Product.Name)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(bps => new LowStockAlertDto
            {
                BranchId = bps.BranchId,
                BranchName = bps.Branch.Name,
                ProductId = bps.ProductId,
                ProductName = bps.Product.Name,
                SKU = bps.Product.SKU,
                Unit = bps.Product.Unit ?? "",
                // ملاحظة هامة يُترك للفيوتشر: نعتمد حالياً على Quantity وتم تجهيز الكود للتحول إلى AvailableQuantity لاحقاً عند تفعيل الحجز
                Quantity = bps.Quantity,
                MinimumStock = bps.Product.MinimumStock,
                ShortageQuantity = bps.Product.MinimumStock - bps.Quantity
            })
            .ToListAsync();

        var result = new PagedResult<LowStockAlertDto>
        {
            Items = rawItems,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total
        };

        _cache.Set(cacheKey, result, CacheDuration);
        return result;
    }

    public async Task<PagedResult<OverdueCustomerAlertDto>> GetOverdueInvoicesAlertsAsync(PaginationQueryDto query, int dayThreshold = 30)
    {
        var companyId = _currentUser.CompanyId!.Value;
        var thresholdDate = DateTime.UtcNow.AddDays(-dayThreshold);

        var cacheKey = $"alerts_overdue_{companyId}_{query.PageNumber}_{query.PageSize}_{dayThreshold}";

        if (_cache.TryGetValue(cacheKey, out PagedResult<OverdueCustomerAlertDto>? cached) && cached != null)
            return cached;

        // الفواتير التي لم تُسدد بالكامل ومرّ عليها الـ Threshold
        var baseQuery = _context.Invoices
            .Include(i => i.Customer)
            .Where(i => i.CompanyId == companyId
                     && i.Type == InvoiceType.Sale
                     && i.Status == InvoiceStatus.Confirmed
                     && i.PaymentStatus != PaymentStatus.Paid
                     && i.Date <= thresholdDate);

        var today = DateTime.UtcNow;

        // تجميع حسب العميل
        var groupedQuery = baseQuery
            .GroupBy(i => new { i.CustomerId, CustomerName = i.Customer!.Name })
            .Select(g => new OverdueCustomerAlertDto
            {
                CustomerId = g.Key.CustomerId!.Value,
                CustomerName = g.Key.CustomerName,
                TotalOverdue = g.Sum(i => i.RemainingAmount),
                OldestInvoiceDays = (int)(today - g.Min(i => i.Date)).TotalDays,
                InvoiceCount = g.Count()
            });

        var total = await groupedQuery.CountAsync();
        var rawItems = await groupedQuery
            .OrderByDescending(c => c.TotalOverdue)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new PagedResult<OverdueCustomerAlertDto>
        {
            Items = rawItems,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total
        };

        _cache.Set(cacheKey, result, CacheDuration);
        return result;
    }

    public async Task<PagedResult<HighDebtCustomerAlertDto>> GetHighDebtCustomersAlertsAsync(PaginationQueryDto query)
    {
        var companyId = _currentUser.CompanyId!.Value;

        var cacheKey = $"alerts_highdebt_{companyId}_{query.PageNumber}_{query.PageSize}";

        if (_cache.TryGetValue(cacheKey, out PagedResult<HighDebtCustomerAlertDto>? cached) && cached != null)
            return cached;

        var baseQuery = _context.Customers
            .Where(c => c.CompanyId == companyId && c.CreditLimit > 0);

        var total = await baseQuery.CountAsync();
        // نقوم بتحميل بعض العملاء ثم نحسب لأن الرصيد لا يكون مخزنا حقلا بالكيان دائما (يعتمد على Snapshot/Current)
        // ولكن للتبسيط وتقليل الضغط، يفضل لو كان هناك حقل CurrentBalance لكن نحن نعتمد على GetCustomerCurrentBalanceAsync
        // كحل مؤقت، سنقوم بجلب العملاء في الصفحة، ثم نحسب أرصدتهم

        var customersInPage = await baseQuery
            .OrderByDescending(c => c.CreditLimit)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var alerts = new List<HighDebtCustomerAlertDto>();

        foreach (var c in customersInPage)
        {
            // حساب الرصيد الحالي 
            // ملحوظة: في المشاريع الضخمة يُفضّل حفظ الرصيد كحقل مُحدث تلقائيا لتسريع الـ Queries
            var invoicedTotal = await _context.Invoices
                .Where(i => i.CustomerId == c.Id && i.Status == InvoiceStatus.Confirmed && i.Type == InvoiceType.Sale)
                .SumAsync(i => (decimal?)i.NetTotal) ?? 0m;

            var returnTotal = await _context.Invoices
                .Where(i => i.CustomerId == c.Id && i.Status == InvoiceStatus.Confirmed && i.Type == InvoiceType.SalesReturn)
                .SumAsync(i => (decimal?)i.NetTotal) ?? 0m;

            var receivedTotal = await _context.CustomerReceipts
                .Where(r => r.CustomerId == c.Id)
                .SumAsync(r => (decimal?)r.Amount) ?? 0m;

            var currentBalance = c.OpeningBalance + (invoicedTotal - returnTotal) - receivedTotal;

            if (currentBalance >= c.CreditLimit * 0.8m) // تحذير إذا تجاوز 80% من الحد
            {
                alerts.Add(new HighDebtCustomerAlertDto
                {
                    CustomerId = c.Id,
                    CustomerName = c.Name,
                    CreditLimit = c.CreditLimit,
                    NetBalance = currentBalance,
                    ExcessAmount = currentBalance - c.CreditLimit
                });
            }
        }

        var result = new PagedResult<HighDebtCustomerAlertDto>
        {
            Items = alerts,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total 
            // TotalCount الفعلي للذين تجاوزوا الحد سيتطلب Query معقدة، لكن هذا مقبول حالياً
        };

        _cache.Set(cacheKey, result, CacheDuration);
        return result;
    }
}
