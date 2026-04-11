using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StoreManagement.Data;
using StoreManagement.IntegrationTests.Helpers;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.Finance;
using StoreManagement.Shared.Entities.Inventory;
using StoreManagement.Shared.Entities.Partners;
using StoreManagement.Shared.Entities.Sales;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;
using Xunit;

namespace StoreManagement.IntegrationTests.Services;

/// <summary>
/// اختبارات تغطي منطق المرتجعات (Step 5):
/// - IssueCashRefund = false → CreditNote (إشعار دائن بدون حركة نقدية)
/// - IssueCashRefund = true  → Cash Refund (مرتجع نقدي + CashTransaction)
/// - PaymentStatus يُحدَّث دائمًا إلى Paid (Fully Settled) بعد إنشاء السند
/// - Amount يبقى موجبًا دائمًا — Kind هو الذي يحدد الأثر المحاسبي
/// </summary>
public class ReturnLogicTests : IClassFixture<StoreManagementApiFactory>
{
    private readonly StoreManagementApiFactory _factory;

    public ReturnLogicTests(StoreManagementApiFactory factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
    }

    // ===== Test 1: CreditNote (IssueCashRefund = false) =====

    [Fact]
    public async Task SalesReturn_CreditOnly_Should_CreateCreditNote_No_CashTransaction()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();

        var dto = new CreateReceiptDto
        {
            PartnerId = 1,
            Amount = 300m,
            Date = DateTime.UtcNow,
            Method = PaymentMethod.Cash,
            Notes = "Credit Only Return Test"
        };

        var cashCountBefore = await context.CashTransactions.CountAsync();
        var receiptCountBefore = await context.CustomerReceipts.CountAsync();

        // Act — CreditNote (no cash, no cash transaction)
        await financeService.CreateCustomerReturnSettlementAsync(
            dto,
            explicitBranchId: 1,
            createCashTransaction: false,
            returnInvoiceId: 99);

        // Assert
        var cashCountAfter = await context.CashTransactions.CountAsync();
        var receiptCountAfter = await context.CustomerReceipts.CountAsync();

        // No CashTransaction created
        (cashCountAfter - cashCountBefore).Should().Be(0,
            "CreditNote return must NOT create a cash transaction");

        // CustomerReceipt created with Kind = CreditNote, Amount negative (reduces balance)
        (receiptCountAfter - receiptCountBefore).Should().Be(1,
            "A CreditNote CustomerReceipt entry must be created");

        var creditNote = await context.CustomerReceipts
            .OrderByDescending(r => r.Id)
            .FirstAsync();

        creditNote.Kind.Should().Be(TransactionKind.CreditNote);
        creditNote.Amount.Should().Be(300m,
            "Amount must be positive, Kind determines effect");
        creditNote.FinancialSourceType.Should().Be(FinancialSourceType.Return);
        creditNote.FinancialSourceId.Should().Be(99);
    }

    // ===== Test 2: Cash Refund (IssueCashRefund = true) =====

    [Fact]
    public async Task SalesReturn_CashRefund_Should_Create_CashTransaction_Out()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();

        var dto = new CreateReceiptDto
        {
            PartnerId = 1,
            Amount = 400m,
            Date = DateTime.UtcNow,
            Method = PaymentMethod.Cash,
            Notes = "Cash Refund Test"
        };

        var cashCountBefore = await context.CashTransactions.CountAsync();
        var receiptCountBefore = await context.CustomerReceipts.CountAsync();

        // Act — Cash Refund (creates CashTransaction OUT)
        await financeService.CreateCustomerReturnSettlementAsync(
            dto,
            explicitBranchId: 1,
            createCashTransaction: true,
            returnInvoiceId: 88);

        // Assert
        var cashCountAfter = await context.CashTransactions.CountAsync();

        // One CashTransaction OUT must be created
        (cashCountAfter - cashCountBefore).Should().Be(1,
            "Cash refund must create exactly one CashTransaction");

        var cashTran = await context.CashTransactions
            .OrderByDescending(t => t.Id)
            .FirstAsync();

        cashTran.Type.Should().Be(TransactionType.Out, "Refund to customer = cash OUT");
        cashTran.Value.Should().Be(400m, "Cash value must always be positive");
        cashTran.FinancialSourceType.Should().Be(FinancialSourceType.Return);
        cashTran.FinancialSourceId.Should().Be(88);

        var receipt = await context.CustomerReceipts
            .OrderByDescending(r => r.Id)
            .FirstAsync();

        receipt.Kind.Should().Be(TransactionKind.Refund);
        receipt.Amount.Should().Be(400m, "Amount must be positive");
    }

    // ===== Test 3: PurchaseReturn CreditNote =====

    [Fact]
    public async Task PurchaseReturn_CreditOnly_Should_CreateCreditNote_No_CashTransaction()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();

        var dto = new CreateReceiptDto
        {
            PartnerId = 1,
            Amount = 250m,
            Date = DateTime.UtcNow,
            Method = PaymentMethod.Cash,
            Notes = "Supplier Credit Return Test"
        };

        var cashCountBefore = await context.CashTransactions.CountAsync();

        // Act
        await financeService.CreateSupplierReturnSettlementAsync(
            dto,
            explicitBranchId: 1,
            createCashTransaction: false,
            returnInvoiceId: 77);

        // Assert — no cash
        var cashCountAfter = await context.CashTransactions.CountAsync();
        (cashCountAfter - cashCountBefore).Should().Be(0,
            "CreditNote must NOT create cash transaction");

        var payment = await context.SupplierPayments
            .OrderByDescending(p => p.Id)
            .FirstAsync();

        payment.Kind.Should().Be(TransactionKind.CreditNote);
        payment.Amount.Should().Be(250m);
        payment.FinancialSourceType.Should().Be(FinancialSourceType.Return);
    }

    // ===== Test 4: PurchaseReturn Cash Refund =====

    [Fact]
    public async Task PurchaseReturn_CashRefund_Should_Create_CashTransaction_In()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();

        var dto = new CreateReceiptDto
        {
            PartnerId = 1,
            Amount = 180m,
            Date = DateTime.UtcNow,
            Method = PaymentMethod.Cash,
            Notes = "Supplier Cash Refund Test"
        };

        var cashCountBefore = await context.CashTransactions.CountAsync();

        // Act
        await financeService.CreateSupplierReturnSettlementAsync(
            dto,
            explicitBranchId: 1,
            createCashTransaction: true,
            returnInvoiceId: 66);

        // Assert
        var cashCountAfter = await context.CashTransactions.CountAsync();
        (cashCountAfter - cashCountBefore).Should().Be(1);

        var cashTran = await context.CashTransactions
            .OrderByDescending(t => t.Id)
            .FirstAsync();

        cashTran.Type.Should().Be(TransactionType.In, "Refund from supplier = cash IN");
        cashTran.Value.Should().Be(180m, "Value always positive");
    }

    // ===== Test 5: Balance decreases correctly after CreditNote =====

    [Fact]
    public async Task CustomerBalance_Decreases_After_CreditNote_Return()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();

        var customer = new Customer { CompanyId = 1, Name = $"Balance_CreditNote_{Guid.NewGuid():N}", OpeningBalance = 0 };
        context.Customers.Add(customer);
        await context.SaveChangesAsync(); // Save customer first to get a valid Id

        context.Invoices.Add(new Invoice
        {
            CustomerId = customer.Id,
            CompanyId = 1,
            BranchId = 1,
            Type = InvoiceType.Sale,
            Status = InvoiceStatus.Confirmed,
            TotalValue = 1000m,
            Date = DateTime.UtcNow.AddDays(-2),
            PaymentStatus = PaymentStatus.Unpaid
        });
        await context.SaveChangesAsync();

        var balanceBefore = await financeService.GetCustomerCurrentBalanceAsync(customer.Id);
        balanceBefore.Should().Be(1000m);

        // Act — issue CreditNote for 300 via Return
        // In the real system, a SalesReturn invoice is created which reduces the balance.
        var returnInvoice = new Invoice
        {
            CustomerId = customer.Id,
            CompanyId = 1,
            BranchId = 1,
            Type = InvoiceType.SalesReturn,
            Status = InvoiceStatus.Confirmed,
            TotalValue = 300m,
            Date = DateTime.UtcNow.AddDays(-1),
            PaymentStatus = PaymentStatus.Paid
        };
        context.Invoices.Add(returnInvoice);
        await context.SaveChangesAsync();

        var dto = new CreateReceiptDto
        {
            PartnerId = customer.Id,
            Amount = 300m,
            Date = DateTime.UtcNow,
            Method = PaymentMethod.Cash,
            Notes = "Credit Note for balance test"
        };

        await financeService.CreateCustomerReturnSettlementAsync(
            dto,
            explicitBranchId: 1,
            createCashTransaction: false,   // CreditNote only
            returnInvoiceId: returnInvoice.Id);

        var balanceAfter = await financeService.GetCustomerCurrentBalanceAsync(customer.Id);

        // Assert
        balanceAfter.Should().Be(700m,
            "Balance must decrease by the CreditNote amount (1000 - 300 = 700)");
    }

    // ===== Test 6: Statement shows CreditNote in Credit column =====

    [Fact]
    public async Task CustomerStatement_Shows_CreditNote_In_Credit_Column()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();

        var customer = new Customer { CompanyId = 1, Name = $"Stmt_CreditNote_{Guid.NewGuid():N}", OpeningBalance = 0 };
        context.Customers.Add(customer);
        await context.SaveChangesAsync(); // Save customer first

        context.Invoices.Add(new Invoice
        {
            CustomerId = customer.Id,
            CompanyId = 1,
            BranchId = 1,
            Type = InvoiceType.Sale,
            Status = InvoiceStatus.Confirmed,
            TotalValue = 800m,
            Date = DateTime.UtcNow.AddDays(-5),
            PaymentStatus = PaymentStatus.Unpaid
        });
        await context.SaveChangesAsync();

        context.CustomerReceipts.Add(new CustomerReceipt
        {
            CustomerId = customer.Id,
            CompanyId = 1,
            BranchId = 1,
            Amount = 200m,    // CreditNote: positive amount, Kind determines effect
            UnallocatedAmount = 200m,
            Date = DateTime.UtcNow.AddDays(-2),
            Method = PaymentMethod.Cash,
            Kind = TransactionKind.CreditNote,
            FinancialSourceType = FinancialSourceType.Return,
            FinancialSourceId = 55
        });
        await context.SaveChangesAsync();

        // Act
        var statement = await financeService.GetCustomerStatementAsync(
            customer.Id,
            new StatementQueryDto { PageSize = 100, PageNumber = 1 });

        // Assert
        statement.ClosingBalance.Should().Be(600m,
            "Balance = 800 (Invoice) - 200 (CreditNote) = 600");

        var creditNoteLine = statement.Items.FirstOrDefault(i => i.DocumentType == "CreditNote");
        creditNoteLine.Should().NotBeNull("CreditNote must appear in statement");
        creditNoteLine!.Credit.Should().Be(200m, "CreditNote appears in Credit column");
        creditNoteLine.Debit.Should().Be(0m);
    }

    // ===== Test 7: Statement shows Cash Refund in Debit column =====

    [Fact]
    public async Task CustomerStatement_Shows_CashRefund_In_Debit_Column()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();

        var customer = new Customer { CompanyId = 1, Name = $"Stmt_Refund_{Guid.NewGuid():N}", OpeningBalance = 0 };
        context.Customers.Add(customer);
        await context.SaveChangesAsync(); // Save customer first

        context.Invoices.Add(new Invoice
        {
            CustomerId = customer.Id,
            CompanyId = 1,
            BranchId = 1,
            Type = InvoiceType.Sale,
            Status = InvoiceStatus.Confirmed,
            TotalValue = 1000m,
            Date = DateTime.UtcNow.AddDays(-5),
            PaymentStatus = PaymentStatus.Unpaid
        });
        await context.SaveChangesAsync();

        // An already-paid receipt
        context.CustomerReceipts.Add(new CustomerReceipt
        {
            CustomerId = customer.Id,
            CompanyId = 1,
            BranchId = 1,
            Amount = 500m,
            UnallocatedAmount = 500m,
            Date = DateTime.UtcNow.AddDays(-4),
            Method = PaymentMethod.Cash,
            Kind = TransactionKind.Normal
        });

        // A cash refund entry (negative amount, Kind=Refund)
        context.CustomerReceipts.Add(new CustomerReceipt
        {
            CustomerId = customer.Id,
            CompanyId = 1,
            BranchId = 1,
            Amount = 300m,       // Refund: positive amount, Kind determines effect
            UnallocatedAmount = 300m,
            Date = DateTime.UtcNow.AddDays(-2),
            Method = PaymentMethod.Cash,
            Kind = TransactionKind.Refund,
            FinancialSourceType = FinancialSourceType.Return,
            FinancialSourceId = 44
        });
        await context.SaveChangesAsync();

        // Act
        var statement = await financeService.GetCustomerStatementAsync(
            customer.Id,
            new StatementQueryDto { PageSize = 100, PageNumber = 1 });

        // 1000 (invoice) - 500 (receipt) + 300 (refund makes balance go up) = 800
        statement.ClosingBalance.Should().Be(800m);

        var refundLine = statement.Items.FirstOrDefault(i => i.DocumentType == "Refund");
        refundLine.Should().NotBeNull("Cash Refund must appear in statement");
        refundLine!.Debit.Should().Be(300m, "Cash Refund appears in Debit column (increases partner's outstanding)");
        refundLine.Credit.Should().Be(0m);
    }

    // ===== Test 8: Amount validation — must be positive =====

    [Fact]
    public async Task CreateCustomerReturnSettlement_ZeroAmount_Should_Throw()
    {
        using var scope = _factory.Services.CreateScope();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();

        var dto = new CreateReceiptDto
        {
            PartnerId = 1,
            Amount = 0m,
            Date = DateTime.UtcNow,
            Method = PaymentMethod.Cash
        };

        var act = () => financeService.CreateCustomerReturnSettlementAsync(dto, explicitBranchId: 1);
        await act.Should().ThrowAsync<ArgumentException>("Amount must be positive");
    }

    [Fact]
    public async Task CreateCustomerReturnSettlement_NegativeAmount_Should_Throw()
    {
        using var scope = _factory.Services.CreateScope();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();

        var dto = new CreateReceiptDto
        {
            PartnerId = 1,
            Amount = -100m,
            Date = DateTime.UtcNow,
            Method = PaymentMethod.Cash
        };

        var act = () => financeService.CreateCustomerReturnSettlementAsync(dto, explicitBranchId: 1);
        await act.Should().ThrowAsync<ArgumentException>("Amount must be positive");
    }
}
