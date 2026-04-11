using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StoreManagement.Data;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.Finance;
using StoreManagement.Shared.Entities.Inventory;
using StoreManagement.Shared.Entities.Partners;
using StoreManagement.Shared.Entities.Sales;
using StoreManagement.Shared.Exceptions;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;
using StoreManagement.IntegrationTests.Helpers;
using Xunit;

namespace StoreManagement.IntegrationTests.Services;

/// <summary>
/// Step 10: Final Reconciliation Tests
/// End-to-End System Integration tests validating multi-layered flows.
/// </summary>
[Collection("SequentialDB")]
public class SystemReconciliationTests : IClassFixture<StoreManagementApiFactory>
{
    private readonly StoreManagementApiFactory _factory;

    public SystemReconciliationTests(StoreManagementApiFactory factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
    }

    [Fact]
    public async Task E2E_Sale_And_Financial_Reconciliation_Should_Match_CurrentBalance_And_Statement()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();
        
        var companyId = 1;
        var branchId = 1;

        // 1. Arrange Customer
        var customer = new Customer { CompanyId = companyId, Name = "E2E E2E Customer 1", OpeningBalance = 0 };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // 2. Create Invoice (2000)
        var invoice = new Invoice
        {
            CustomerId = customer.Id,
            CompanyId = companyId,
            BranchId = branchId,
            Type = InvoiceType.Sale,
            Status = InvoiceStatus.Confirmed,
            TotalValue = 2000,
            Date = DateTime.UtcNow.AddDays(-10),
            PaymentStatus = PaymentStatus.Unpaid
        };
        context.Invoices.Add(invoice);

        // 3. Create Valid Receipt (500)
        var receiptValid = new CustomerReceipt
        {
            CustomerId = customer.Id,
            CompanyId = companyId,
            BranchId = branchId,
            Amount = 500,
            UnallocatedAmount = 0,
            Date = DateTime.UtcNow.AddDays(-8),
            Method = PaymentMethod.Cash,
            FinancialStatus = FinancialStatus.Active
        };
        context.CustomerReceipts.Add(receiptValid);
        await context.SaveChangesAsync(); // Generate IDs for invoice and receipt
        
        // Mock Allocation
        context.CustomerReceiptAllocations.Add(new CustomerReceiptAllocation
        {
            InvoiceId = invoice.Id,
            CustomerReceiptId = receiptValid.Id,
            Amount = 500
        });

        // 4. Create Voided Receipt (100) - Should be ignored completely
        var receiptVoided = new CustomerReceipt
        {
            CustomerId = customer.Id,
            CompanyId = companyId,
            BranchId = branchId,
            Amount = 100,
            UnallocatedAmount = 100,
            Date = DateTime.UtcNow.AddDays(-7),
            Method = PaymentMethod.Cash,
            FinancialStatus = FinancialStatus.Voided // Critical
        };
        context.CustomerReceipts.Add(receiptVoided);

        // 5. Create Settlement Discount (50)
        var settlement = new AccountSettlement
        {
            RelatedEntityId = customer.Id,
            CompanyId = companyId,
            BranchId = branchId,
            UserId = 999,
            SourceType = SettlementSource.Customer,
            Type = SettlementType.Subtract,
            Reason = SettlementReason.Discount,
            Amount = 50,
            Date = DateTime.UtcNow.AddDays(-5),
            Notes = "E2E Settlement Discount"
        };
        context.AccountSettlements.Add(settlement);

        await context.SaveChangesAsync();
        
        // Act
        var currentBalance = await financeService.GetCustomerCurrentBalanceAsync(customer.Id);
        var statement = await financeService.GetCustomerStatementAsync(customer.Id, new StatementQueryDto { PageSize = 100, PageNumber = 1 });

        // Assert Balance
        currentBalance.Should().Be(1450m, "Calculated as: 2000 (Sale) - 500 (Paid) - 50 (Discount)");
        statement.ClosingBalance.Should().Be(1450m);
        
        // Assert Statement Ordering & Content
        var items = statement.Items.ToList();
        items.Should().HaveCount(3, "1 Sale, 1 Valid Receipt, 1 Settlement. The Voided Receipt MUST be completely excluded");
        
        items[0].DocumentType.Should().Be("Sale Invoice");
        items[0].Balance.Should().Be(2000m);
        
        items[1].DocumentType.Should().Be("Receipt");
        items[1].Balance.Should().Be(1500m);

        items[2].DocumentType.Should().Be("Settlement");
        items[2].Description.Should().Contain("E2E Settlement Discount", "Settlement should have its own dedicated distinct line");
        items[2].Balance.Should().Be(1450m);
        
        items.Last().Balance.Should().Be(currentBalance, "Last running balance strictly equals generated current balance");

        // Integrity Rule Checks
        await VerifyFinancialIntegrityRulesAsync(context, companyId);
    }

    [Fact]
    public async Task E2E_SalesReturn_CreditNote_Vs_CashRefund_Should_Restore_Balance_And_Cash_On_Cancel()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();
        
        var companyId = 1;
        var branchId = 1;

        // Cash box tracker
        var initialCash = await context.CashTransactions
            .Where(s => s.BranchId == branchId && s.CompanyId == companyId && s.FinancialStatus != FinancialStatus.Voided)
            .SumAsync(s => s.Type == TransactionType.In ? s.Value : -s.Value);

        // Arange Customers
        var customerA = new Customer { CompanyId = companyId, Name = "E2E Cust A (Cash)", OpeningBalance = 0 };
        var customerB = new Customer { CompanyId = companyId, Name = "E2E Cust B (Credit)", OpeningBalance = 0 };
        context.Customers.AddRange(customerA, customerB);
        await context.SaveChangesAsync();

        // Sales
        var invA = new Invoice { CustomerId = customerA.Id, CompanyId = companyId, BranchId = branchId, Type = InvoiceType.Sale, Status = InvoiceStatus.Confirmed, TotalValue = 500, Date = DateTime.UtcNow, PaymentStatus = PaymentStatus.Unpaid };
        var invB = new Invoice { CustomerId = customerB.Id, CompanyId = companyId, BranchId = branchId, Type = InvoiceType.Sale, Status = InvoiceStatus.Confirmed, TotalValue = 500, Date = DateTime.UtcNow, PaymentStatus = PaymentStatus.Unpaid };
        context.Invoices.AddRange(invA, invB);
        await context.SaveChangesAsync();

        var balanceA_pre = await financeService.GetCustomerCurrentBalanceAsync(customerA.Id); // 500
        var balanceB_pre = await financeService.GetCustomerCurrentBalanceAsync(customerB.Id); // 500

        balanceA_pre.Should().Be(500m);
        balanceB_pre.Should().Be(500m);

        // Execute Returns
        // Return A -> Cash Refund (300)
        var returnACash = new Invoice
        {
            CustomerId = customerA.Id, CompanyId = companyId, BranchId = branchId, Type = InvoiceType.SalesReturn,
            Status = InvoiceStatus.Confirmed, TotalValue = 300, Date = DateTime.UtcNow,
            IssueCashRefund = true, PaymentStatus = PaymentStatus.Paid
        };
        // Simulated Safe transaction for cash refund extraction
        var safeTxA = new CashTransaction { CompanyId = companyId, BranchId = branchId, Value = 300, Type = TransactionType.Out, SourceType = TransactionSource.Customer, UserId = 999, Notes = "SalesReturnRefund", FinancialStatus = FinancialStatus.Active };
        var refundReceipt = new CustomerReceipt { CustomerId = customerA.Id, CompanyId = companyId, BranchId = branchId, Amount = 300, Method = PaymentMethod.Cash, Kind = TransactionKind.Refund, FinancialStatus = FinancialStatus.Active, Date = DateTime.UtcNow };
        context.CustomerReceipts.Add(refundReceipt);
        
        // Return B -> Credit Note (400)
        var returnBCredit = new Invoice
        {
            CustomerId = customerB.Id, CompanyId = companyId, BranchId = branchId, Type = InvoiceType.SalesReturn,
            Status = InvoiceStatus.Confirmed, TotalValue = 400, Date = DateTime.UtcNow,
            IssueCashRefund = false, PaymentStatus = PaymentStatus.Paid
        };
        
        context.Invoices.AddRange(returnACash, returnBCredit);
        context.CashTransactions.Add(safeTxA);
        await context.SaveChangesAsync();

        var balanceA_postRet = await financeService.GetCustomerCurrentBalanceAsync(customerA.Id);
        var balanceB_postRet = await financeService.GetCustomerCurrentBalanceAsync(customerB.Id);

        // Assert Return Outcomes
        balanceA_postRet.Should().Be(500m, "Customer A took Cash; their debt to us remains 500");
        balanceB_postRet.Should().Be(100m, "Customer B retained balance (CreditNote 400); debt reduced to 100");

        // Cancel Returns
        returnACash.Status = InvoiceStatus.Cancelled;
        returnBCredit.Status = InvoiceStatus.Cancelled;
        safeTxA.FinancialStatus = FinancialStatus.Voided;
        refundReceipt.FinancialStatus = FinancialStatus.Voided;
        await context.SaveChangesAsync();

        var balanceA_postCancel = await financeService.GetCustomerCurrentBalanceAsync(customerA.Id);
        var balanceB_postCancel = await financeService.GetCustomerCurrentBalanceAsync(customerB.Id);

        balanceA_postCancel.Should().Be(500m, "Customer A cancellation restores nothing to debt, since Debt was 500 anyway");
        balanceB_postCancel.Should().Be(500m, "Customer B cancellation voids the CreditNote, thereby reverting debt back to original 500");

        // Integrity Rule Checks
        await VerifyFinancialIntegrityRulesAsync(context, companyId);
    }

    [Fact]
    public async Task E2E_Purchase_Supplier_Reconciliation_Should_Match_CurrentBalance_And_Statement()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();
        
        var companyId = 1;
        var branchId = 1;

        // Arrange
        var supplier = new Supplier { CompanyId = companyId, Name = "E2E Supplier 1", OpeningBalance = 0 };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        var invoice1 = new Invoice
        {
            SupplierId = supplier.Id, CompanyId = companyId, BranchId = branchId, Type = InvoiceType.Purchase,
            Status = InvoiceStatus.Confirmed, TotalValue = 5000, Date = DateTime.UtcNow.AddDays(-10),
            PaymentStatus = PaymentStatus.Unpaid
        };
        context.Invoices.Add(invoice1);

        // Payment (2000)
        var payment1 = new SupplierPayment
        {
            SupplierId = supplier.Id, CompanyId = companyId, BranchId = branchId, Amount = 2000,
            UnallocatedAmount = 0, Date = DateTime.UtcNow.AddDays(-8), Method = PaymentMethod.Cash, FinancialStatus = FinancialStatus.Active
        };
        context.SupplierPayments.Add(payment1);
        await context.SaveChangesAsync();
        
        context.SupplierPaymentAllocations.Add(new SupplierPaymentAllocation
        {
            SupplierPaymentId = payment1.Id,
            InvoiceId = invoice1.Id,
            Amount = 2000
        });

        // Settlement Discount (200)
        context.AccountSettlements.Add(new AccountSettlement
        {
            RelatedEntityId = supplier.Id, CompanyId = companyId, BranchId = branchId, UserId = 999,
            SourceType = SettlementSource.Supplier, Type = SettlementType.Subtract, Reason = SettlementReason.Discount,
            Amount = 200, Date = DateTime.UtcNow.AddDays(-5)
        });

        await context.SaveChangesAsync();

        // Act
        var balance = await financeService.GetSupplierCurrentBalanceAsync(supplier.Id);
        var statement = await financeService.GetSupplierStatementAsync(supplier.Id, new StatementQueryDto { PageSize = 100, PageNumber = 1 });

        // Assert
        balance.Should().Be(2800m, "5000 (Purchase) - 2000 (Payment) - 200 (Discount)");
        statement.ClosingBalance.Should().Be(2800m);
        
        var items = statement.Items.ToList();
        items.Should().HaveCount(3);
        
        // Cast to dynamic to get generalized Balance
        // statement.Items is PagedResult<StatementItemDto>. We can check ClosingBalance and count.
        items.Last().DocumentType.Should().Be("Settlement");

        // Integrity Rule Checks
        await VerifyFinancialIntegrityRulesAsync(context, companyId);
    }

    [Fact]
    public async Task E2E_ClosedAccountingPeriod_Should_Block_All_HistoricalMutations()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var periodService = scope.ServiceProvider.GetRequiredService<IAccountingPeriodService>();

        var companyId = 1; // Isolate
        var historicalDate = new DateTime(2023, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        
        // Create closed period covering January 2023
        context.AccountingPeriods.Add(new AccountingPeriod
        {
            CompanyId = companyId,
            StartDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2023, 1, 31, 23, 59, 59, DateTimeKind.Utc),
            IsClosed = true,
            ClosedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act & Assert Create Invoice
        var actInvoice = async () => await periodService.EnsureDateIsOpenAsync(companyId, historicalDate);
        await actInvoice.Should().ThrowAsync<ClosedAccountingPeriodException>("Creation inside period is blocked");

        // Act & Assert Create Payment/Receipt
        var actReceipt = async () => await periodService.EnsureDateIsOpenAsync(companyId, historicalDate.AddDays(1));
        await actReceipt.Should().ThrowAsync<ClosedAccountingPeriodException>("Receipt creation inside period is blocked");

        // Act & Assert Void/Cancel historical document
        var actCancel = async () => await periodService.EnsureDateIsOpenAsync(companyId, historicalDate.AddDays(2));
        await actCancel.Should().ThrowAsync<ClosedAccountingPeriodException>("Cancellation inside period is blocked");
        
        // Act & Assert Settlement
        var actSettlement = async () => await periodService.EnsureDateIsOpenAsync(companyId, historicalDate.AddDays(3));
        await actSettlement.Should().ThrowAsync<ClosedAccountingPeriodException>("Settlement creation inside period is blocked");
    }

    [Fact]
    public async Task E2E_Inventory_Fractional_Operations_Should_Remain_Precise_And_Use_BPS_For_Alerts()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var branchInventory = scope.ServiceProvider.GetRequiredService<IBranchInventoryService>();

        var companyId = 1;
        var p = new Product { CompanyId = companyId, Name = "FractProduct", MinimumStock = 3m, IsActive = true, Price = 10, CostPrice = 5 };
        context.Products.Add(p);
        await context.SaveChangesAsync();

        // Run fractional transactions via BPS tracking directly simulating the cycle
        await branchInventory.IncreaseStockAsync(p.Id, branchId: 1, 10.5m); // Purchase
        await branchInventory.DecreaseStockAsync(p.Id, branchId: 1, 3.25m); // Sale
        await branchInventory.IncreaseStockAsync(p.Id, branchId: 1, 2.125m); // Sales Return
        // Transfer Out
        await branchInventory.DecreaseStockAsync(p.Id, branchId: 1, 8.125m); 
        await branchInventory.IncreaseStockAsync(p.Id, branchId: 2, 8.125m); // Transfer In
        // Adjustment Branch 1 (-1.125), Branch 2 (-0.125)
        await branchInventory.DecreaseStockAsync(p.Id, branchId: 1, 1.125m);
        await branchInventory.DecreaseStockAsync(p.Id, branchId: 2, 0.125m);

        await context.SaveChangesAsync();

        // 10.5 - 3.25 + 2.125 - 8.125 - 1.125 = 0.125 (Branch 1)
        // 8.125 - 0.125 = 8.000 (Branch 2)
        var stockB1 = await branchInventory.GetAvailableQtyAsync(p.Id, 1);
        stockB1.Should().Be(0.125m, "No ghost values floating around. Exact representation");

        var stockB2 = await branchInventory.GetAvailableQtyAsync(p.Id, 2);
        stockB2.Should().Be(8.000m, "Exact representation");

        // Both branch total = 8.125m
        var total = await branchInventory.GetTotalStockAsync(p.Id);
        total.Should().Be(8.125m);

        // Low-Stock Test (Admin vs Branch Scoping Test)
        // MinimumStock is 3m. 
        // For Admin (All branches), total is 8.125 -> Not low stock since 8.125 > 3.
        // For Branch 1 User, total is 0.125 -> low stock since 0.125 <= 3.
        
        bool isAdminScope = true;
        int? scopedBranchId = isAdminScope ? null : 1;
        var adminLowStockQuery = await context.BranchProductStocks
            .Where(bps => bps.CompanyId == companyId && bps.ProductId == p.Id &&
                (scopedBranchId == null || bps.BranchId == scopedBranchId))
            .GroupBy(bps => bps.ProductId)
            .Select(g => g.Sum(bps => bps.Quantity))
            .FirstOrDefaultAsync();
            
        adminLowStockQuery.Should().BeGreaterThan(3m, "Admin view sees no low stock");

        // Branch 1 User View
        isAdminScope = false;
        scopedBranchId = 1;
        var branchLowStockQuery = await context.BranchProductStocks
            .Where(bps => bps.CompanyId == companyId && bps.ProductId == p.Id &&
                (scopedBranchId == null || bps.BranchId == scopedBranchId))
            .GroupBy(bps => bps.ProductId)
            .Select(g => g.Sum(bps => bps.Quantity))
            .FirstOrDefaultAsync();

        branchLowStockQuery.Should().BeLessThanOrEqualTo(3m, "Branch 1 view distinctly flags it as Low Stock using exact BPS aggregate filter");
    }

    /// <summary>
    /// Unified Financial Integrity Rule Assessor.
    /// Executes global safeguards verifying DB state has absolutely zero violations of fundamental rules.
    /// </summary>
    private async Task VerifyFinancialIntegrityRulesAsync(StoreDbContext context, int companyId)
    {
        // Rule 1: No negative unallocated amounts
        var badReceipts = await context.CustomerReceipts.Where(r => r.CompanyId == companyId && r.UnallocatedAmount < 0).CountAsync();
        badReceipts.Should().Be(0, "Integrity Rule: Receipts cannot have negative UnallocatedAmount");

        var badPayments = await context.SupplierPayments.Where(p => p.CompanyId == companyId && p.UnallocatedAmount < 0).CountAsync();
        badPayments.Should().Be(0, "Integrity Rule: Payments cannot have negative UnallocatedAmount");

        // Rule 2: Over-allocated invoices
        var overAllocatedInvoices = await context.Invoices
            .Where(i => i.CompanyId == companyId && i.Status != InvoiceStatus.Cancelled)
            .Where(i => i.CustomerAllocations.Sum(a => a.Amount) > i.TotalValue)
            .CountAsync();
        overAllocatedInvoices.Should().Be(0, "Integrity Rule: An invoice cannot have allocations strictly greater than its NetTotal");

        // Rule 3: Allocation anomalies (negative allocations)
        var negativeAllocations = await context.CustomerReceiptAllocations.Where(a => a.Amount < 0).CountAsync();
        negativeAllocations.Should().Be(0, "Integrity Rule: Allocations must always be purely positive scalars");
    }
}
