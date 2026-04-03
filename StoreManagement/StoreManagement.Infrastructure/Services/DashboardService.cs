using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DashboardService(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<DashboardStatsDto> GetDailyStatsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

        var todaySalesTotal = await _context.Invoices
            .Where(i => i.Type == InvoiceType.Sale && i.Date >= today)
            .SumAsync(i => i.TotalValue - i.Discount);

        var todayExpensesTotal = await _context.CashTransactions
            .Where(t => t.SourceType == TransactionSource.Expense && t.Date >= today)
            .SumAsync(t => t.Value);

        var todayInvoicesCount = await _context.Invoices
            .CountAsync(i => i.Date >= today);

        var monthlySalesTotal = await _context.Invoices
            .Where(i => i.Type == InvoiceType.Sale && i.Date >= firstDayOfMonth)
            .SumAsync(i => i.TotalValue - i.Discount);

        var totalCustomerDebts = await _context.Customers
            .Where(c => c.CashBalance < 0)
            .SumAsync(c => Math.Abs(c.CashBalance));

        var totalSupplierDebts = await _context.Suppliers
            .Where(s => s.CashBalance < 0)
            .SumAsync(s => Math.Abs(s.CashBalance));

        var topProducts = await GetTopSellingProductsAsync(5);

        var recentInvoices = await _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .OrderByDescending(i => i.Date)
            .Take(5)
            .Select(i => new RecentInvoiceDto
            {
                Id = i.Id,
                Type = i.Type.ToString(),
                PartnerName = i.Type == InvoiceType.Sale ? i.Customer!.Name : i.Supplier!.Name,
                TotalValue = i.TotalValue,
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
            .Where(ii => ii.Invoice.Type == InvoiceType.Sale)
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
        var customerDebts = await _context.Customers
            .Where(c => c.CashBalance < 0)
            .Select(c => new DebtAlertDto
            {
                PartnerId = c.Id,
                PartnerName = c.Name,
                Amount = Math.Abs(c.CashBalance),
                Type = "Customer"
            })
            .ToListAsync();

        var supplierDebts = await _context.Suppliers
            .Where(s => s.CashBalance < 0)
            .Select(s => new DebtAlertDto
            {
                PartnerId = s.Id,
                PartnerName = s.Name,
                Amount = Math.Abs(s.CashBalance),
                Type = "Supplier"
            })
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

        // 1. مبيعات وأرباح الشهر الحالي
        var currentMonthSalesDetails = await _context.Invoices
            .Include(i => i.Items)
            .Where(i => i.CompanyId == companyId 
                     && i.Type == InvoiceType.Sale 
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Date >= firstDayThisMonth && i.Date < firstDayNextMonth)
            .Select(i => new
            {
                i.Date,
                i.NetTotal,
                TotalCost = i.Items.Sum(item => item.Quantity * (double)item.CostPriceAtSale)
            })
            .ToListAsync();

        var monthSales = currentMonthSalesDetails.Sum(i => i.NetTotal);
        var todaySales = currentMonthSalesDetails.Where(i => i.Date >= today).Sum(i => i.NetTotal);
        
        var currentMonthCost = currentMonthSalesDetails.Sum(i => (decimal)i.TotalCost);
        var monthProfit = monthSales - currentMonthCost;
        var monthMargin = monthSales > 0 ? (monthProfit / monthSales) * 100 : 0m;

        // 2. مبيعات الشهر السابق للمقارنة
        var lastMonthSales = await _context.Invoices
            .Where(i => i.CompanyId == companyId 
                     && i.Type == InvoiceType.Sale 
                     && i.Status == InvoiceStatus.Confirmed
                     && i.Date >= firstDayLastMonth && i.Date < firstDayThisMonth)
            .SumAsync(i => i.NetTotal);

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
            LowStockItemsCount = lowStockCount,
            OpenCustomerInvoices = openInvoicesCount
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
}
