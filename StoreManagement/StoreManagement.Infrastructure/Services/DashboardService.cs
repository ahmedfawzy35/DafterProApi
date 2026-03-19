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
                Revenue = g.Sum(ii => ii.Quantity * ii.UnitPrice)
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
}
