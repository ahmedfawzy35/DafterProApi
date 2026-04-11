using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StoreManagement.Data;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.Core;
using StoreManagement.Shared.Entities.Finance;
using StoreManagement.Shared.Entities.Partners;
using StoreManagement.Shared.Entities.Sales;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;
using StoreManagement.IntegrationTests.Helpers;

namespace StoreManagement.IntegrationTests.Services;

[Collection("SequentialDB")]
public class ReturnCancellationTests : IClassFixture<StoreManagementApiFactory>
{
    private readonly StoreManagementApiFactory _factory;

    public ReturnCancellationTests(StoreManagementApiFactory factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
    }

    [Fact]
    public async Task Cancel_SalesReturn_Should_Void_Associated_CustomerReceipt()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();

        var customer = new Customer { CompanyId = 1, Name = "Cancel_Test_Customer" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var invoice = new Invoice
        {
            CustomerId = customer.Id,
            CompanyId = 1,
            BranchId = 1,
            Type = InvoiceType.SalesReturn,
            Status = InvoiceStatus.Confirmed,
            TotalValue = 500m,
            Date = DateTime.UtcNow,
            PaymentStatus = PaymentStatus.Paid
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        // Simulate created receipt via ReturnService
        var receiptDto = new CreateReceiptDto
        {
            PartnerId = customer.Id,
            Amount = 500m,
            Date = invoice.Date,
            Method = PaymentMethod.Cash
        };

        var createdReceipt = await financeService.CreateCustomerReturnSettlementAsync(
            receiptDto, explicitBranchId: 1, createCashTransaction: false, returnInvoiceId: invoice.Id);

        // Act
        await invoiceService.CancelAsync(invoice.Id);

        // Assert
        var voidedReceipt = await context.CustomerReceipts.FindAsync(createdReceipt.Id);
        voidedReceipt.Should().NotBeNull();
        voidedReceipt!.FinancialStatus.Should().Be(FinancialStatus.Voided);
        voidedReceipt.CancelledByUserId.Should().NotBeNull();
    }

    [Fact]
    public async Task Cancel_SalesReturn_WithCashRefund_Should_Void_CashTransaction()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();

        var customer = new Customer { CompanyId = 1, Name = "Cancel_Refund_Customer" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var invoice = new Invoice
        {
            CustomerId = customer.Id,
            CompanyId = 1,
            BranchId = 1,
            Type = InvoiceType.SalesReturn,
            Status = InvoiceStatus.Confirmed,
            TotalValue = 700m,
            Date = DateTime.UtcNow,
            PaymentStatus = PaymentStatus.Paid
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        var receiptDto = new CreateReceiptDto
        {
            PartnerId = customer.Id,
            Amount = 700m,
            Date = invoice.Date,
            Method = PaymentMethod.Cash
        };

        var createdReceipt = await financeService.CreateCustomerReturnSettlementAsync(
            receiptDto, explicitBranchId: 1, createCashTransaction: true, returnInvoiceId: invoice.Id);

        // Pre-assert
        var cashTxBefore = await context.CashTransactions.FirstOrDefaultAsync(c => 
            c.FinancialSourceType == FinancialSourceType.Return && c.FinancialSourceId == invoice.Id);
        cashTxBefore.Should().NotBeNull();
        cashTxBefore!.FinancialStatus.Should().Be(FinancialStatus.Active);

        // Act
        await invoiceService.CancelAsync(invoice.Id);

        // Assert
        var cashTxAfter = await context.CashTransactions.FindAsync(cashTxBefore.Id);
        cashTxAfter!.FinancialStatus.Should().Be(FinancialStatus.Voided);
    }

    [Fact]
    public async Task Cancelled_Receipts_Should_Not_Affect_CustomerBalance()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();

        var customer = new Customer { CompanyId = 1, Name = "Balance_Customer" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var originalBalance = await financeService.GetCustomerCurrentBalanceAsync(customer.Id);
        originalBalance.Should().Be(0m);

        // Create Sale
        var saleInvoice = new Invoice
        {
            CustomerId = customer.Id,
            CompanyId = 1,
            BranchId = 1,
            Type = InvoiceType.Sale,
            Status = InvoiceStatus.Confirmed,
            TotalValue = 1000m,
            Date = DateTime.UtcNow,
        };
        context.Invoices.Add(saleInvoice);
        await context.SaveChangesAsync();

        var balAfterSale = await financeService.GetCustomerCurrentBalanceAsync(customer.Id);
        balAfterSale.Should().Be(1000m);

        // Create Return + Refund
        var returnInvoice = new Invoice
        {
            CustomerId = customer.Id,
            CompanyId = 1,
            BranchId = 1,
            Type = InvoiceType.SalesReturn,
            Status = InvoiceStatus.Confirmed,
            TotalValue = 300m,
            Date = DateTime.UtcNow,
        };
        context.Invoices.Add(returnInvoice);
        await context.SaveChangesAsync();

        await financeService.CreateCustomerReturnSettlementAsync(
            new CreateReceiptDto { PartnerId = customer.Id, Amount = 300m, Date = DateTime.UtcNow, Method = PaymentMethod.Cash },
            createCashTransaction: true, returnInvoiceId: returnInvoice.Id);

        var balAfterReturn = await financeService.GetCustomerCurrentBalanceAsync(customer.Id);
        // Math: 1000 (sale) - 300 (return) + 300 (Refund) = 1000
        balAfterReturn.Should().Be(1000m);

        // Act - Cancel Return
        await invoiceService.CancelAsync(returnInvoice.Id);

        // Assert - balance should return back to after sale (1000)
        // Math: Sale (1000). (The return is voided, and its refund is voided)
        var balAfterCancellation = await financeService.GetCustomerCurrentBalanceAsync(customer.Id);
        balAfterCancellation.Should().Be(1000m);
    }

    [Fact]
    public async Task Cancel_SalesReturn_CreditNote_Should_Not_Touch_CashTransaction()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();

        var customer = new Customer { CompanyId = 1, Name = "Cancel_No_Refund" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var invoice = new Invoice
        {
            CustomerId = customer.Id, CompanyId = 1, BranchId = 1,
            Type = InvoiceType.SalesReturn, Status = InvoiceStatus.Confirmed, TotalValue = 700m,
            Date = DateTime.UtcNow, PaymentStatus = PaymentStatus.Paid
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        // createCashTransaction: false
        await financeService.CreateCustomerReturnSettlementAsync(
            new CreateReceiptDto { PartnerId = customer.Id, Amount = 700m, Date = DateTime.UtcNow, Method = PaymentMethod.Cash },
            createCashTransaction: false, returnInvoiceId: invoice.Id);

        var cashTxCountBefore = await context.CashTransactions.CountAsync(c => c.CompanyId == 1 && c.FinancialSourceId == invoice.Id);
        cashTxCountBefore.Should().Be(0, "No cash transaction should be created for CreditNote-only return");

        await invoiceService.CancelAsync(invoice.Id);

        var cashTxCountAfter = await context.CashTransactions.CountAsync(c => c.CompanyId == 1 && c.FinancialSourceId == invoice.Id);
        cashTxCountAfter.Should().Be(0);
    }

    [Fact]
    public async Task Cancel_Return_Should_Fail_In_Closed_Period()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
        var periodService = scope.ServiceProvider.GetRequiredService<IAccountingPeriodService>();

        var pastDate = DateTime.UtcNow.AddMonths(-2);
        
        // Setup a closed period
        var period = new AccountingPeriod
        {
            CompanyId = 1, StartDate = pastDate.AddDays(-10), EndDate = pastDate.AddDays(10), IsClosed = true, ClosedAt = DateTime.UtcNow
        };
        context.AccountingPeriods.Add(period);
        await context.SaveChangesAsync();

        var invoice = new Invoice
        {
            CustomerId = 1, CompanyId = 1, BranchId = 1,
            Type = InvoiceType.SalesReturn, Status = InvoiceStatus.Confirmed, TotalValue = 300m,
            Date = pastDate, PaymentStatus = PaymentStatus.Paid
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        var act = async () => await invoiceService.CancelAsync(invoice.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*closed*"); // English message used by AccountingPeriodService
    }

    [Fact]
    public async Task Cancel_NormalInvoice_Should_Unallocate_NotVoid_SharedReceipt()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();

        var customer = new Customer { CompanyId = 1, Name = "Shared_Receipt_Customer" };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // 1. Create a Normal Sale Invoice
        var invoice = new Invoice
        {
            CustomerId = customer.Id, CompanyId = 1, BranchId = 1,
            Type = InvoiceType.Sale, Status = InvoiceStatus.Confirmed, TotalValue = 500m,
            Date = DateTime.UtcNow, PaymentStatus = PaymentStatus.Unpaid
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        // 2. Create a generic receipt (NOT inherently a return settlement)
        var receipt = await financeService.CreateCustomerReceiptAsync(
            new CreateReceiptDto { PartnerId = customer.Id, Amount = 1000m, Date = DateTime.UtcNow, Method = PaymentMethod.BankTransfer }
        );

        // 3. Allocate partially to the invoice
        await financeService.AllocateDirectToInvoiceAsync(receipt.Id, invoice.Id, 500m);

        var allocatedReceipt = await context.CustomerReceipts.FindAsync(receipt.Id);
        allocatedReceipt!.UnallocatedAmount.Should().Be(500m); // 1000 - 500

        // Act - Cancel the Normal Invoice
        await invoiceService.CancelAsync(invoice.Id);

        // Assert - The standard receipt is NOT Voided, its allocation is just reversed.
        var receiptAfterCancel = await context.CustomerReceipts.FindAsync(receipt.Id);
        receiptAfterCancel!.FinancialStatus.Should().Be(FinancialStatus.Active, "Normal receipt should not be voided, only unallocated");
        receiptAfterCancel.UnallocatedAmount.Should().Be(1000m, "Allocation amount is returned to the pool");
        
        var allocs = await context.CustomerReceiptAllocations.Where(a => a.InvoiceId == invoice.Id).CountAsync();
        allocs.Should().Be(0);
    }
}
