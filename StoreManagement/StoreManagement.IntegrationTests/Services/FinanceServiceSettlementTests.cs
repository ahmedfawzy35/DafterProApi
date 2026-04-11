using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StoreManagement.Data;
using StoreManagement.Infrastructure.Services;
using StoreManagement.IntegrationTests.Helpers;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.Finance;
using StoreManagement.Shared.Entities.Partners;
using StoreManagement.Shared.Entities.Sales;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;
using Xunit;

namespace StoreManagement.IntegrationTests.Services;

public class FinanceServiceSettlementTests : IClassFixture<StoreManagementApiFactory>
{
    private readonly StoreManagementApiFactory _factory;

    public FinanceServiceSettlementTests(StoreManagementApiFactory factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
    }

    [Fact]
    public async Task Customer_Statement_ShouldMatch_CurrentBalance()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();

        var customer = new Customer { CompanyId = 1, Name = "Test Customer For Settlement", OpeningBalance = 0 };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Sale Invoice = 1000
        context.Invoices.Add(new Invoice
        {
            CustomerId = customer.Id,
            CompanyId = 1,
            BranchId = 1,
            Type = InvoiceType.Sale,
            Status = InvoiceStatus.Confirmed,
            TotalValue = 1000,
            Date = DateTime.UtcNow.AddDays(-5),
            PaymentStatus = PaymentStatus.Unpaid
        });

        // Receipt = 300
        context.CustomerReceipts.Add(new CustomerReceipt
        {
            CustomerId = customer.Id,
            CompanyId = 1,
            BranchId = 1,
            Amount = 300,
            UnallocatedAmount = 300,
            Date = DateTime.UtcNow.AddDays(-4),
            Method = PaymentMethod.Cash
        });

        // Settlement Discount = 100
        context.AccountSettlements.Add(new AccountSettlement
        {
            RelatedEntityId = customer.Id,
            CompanyId = 1,
            BranchId = 1,
            UserId = 999,
            SourceType = SettlementSource.Customer,
            Type = SettlementType.Subtract,
            Reason = SettlementReason.Discount,
            Amount = 100,
            Date = DateTime.UtcNow.AddDays(-3),
            Notes = "Discount 100"
        });
        await context.SaveChangesAsync();

        // Settlement Adjustment Increase = 50
        context.AccountSettlements.Add(new AccountSettlement
        {
            RelatedEntityId = customer.Id,
            CompanyId = 1,
            BranchId = 1,
            UserId = 999,
            SourceType = SettlementSource.Customer,
            Type = SettlementType.Add,
            Reason = SettlementReason.Other,
            Amount = 50,
            Date = DateTime.UtcNow.AddDays(-2),
            Notes = "Increase 50"
        });

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var entries = string.Join(", ", ex.Entries.Select(e => e.Entity.GetType().Name));
            throw new Exception($"DbUpdateException on {entries}: {ex.InnerException?.Message}", ex);
        }

        // Act
        var currentBalance = await financeService.GetCustomerCurrentBalanceAsync(customer.Id);
        var statement = await financeService.GetCustomerStatementAsync(customer.Id, new StatementQueryDto { PageSize = 100, PageNumber = 1 });

        // Assert
        // Logic: 1000 (Inv) - 300 (Rec) - 100 (Discount) + 50 (Increase) = 650
        currentBalance.Should().Be(650m);
        statement.ClosingBalance.Should().Be(650m);
        
        // Assert sorting and settlement injection
        var eventTypes = statement.Items.Select(i => i.DocumentType).ToList();
        eventTypes.Should().ContainInOrder("Sale Invoice", "Receipt", "Settlement", "Settlement");
        
        var discountLine = statement.Items.First(i => i.Description.Contains("خصم مسموح به"));
        discountLine.Credit.Should().Be(100m); // Credit reduces customer debt
        discountLine.Debit.Should().Be(0m);

        var increaseLine = statement.Items.First(i => i.Description.Contains("زيادة رصيد"));
        increaseLine.Debit.Should().Be(50m); // Debit increases customer debt
        increaseLine.Credit.Should().Be(0m);
    }
    
    [Fact]
    public async Task Supplier_Statement_ShouldMatch_CurrentBalance()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var financeService = scope.ServiceProvider.GetRequiredService<IFinanceService>();

        var supplier = new Supplier { CompanyId = 1, Name = "Test Supplier For Settlement", OpeningBalance = 0 };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        // Purchase Invoice = 800
        context.Invoices.Add(new Invoice
        {
            SupplierId = supplier.Id,
            CompanyId = 1,
            BranchId = 1,
            Type = InvoiceType.Purchase,
            Status = InvoiceStatus.Confirmed,
            TotalValue = 800,
            Date = DateTime.UtcNow.AddDays(-5),
            PaymentStatus = PaymentStatus.Unpaid
        });

        // Payment = 200
        context.SupplierPayments.Add(new SupplierPayment
        {
            SupplierId = supplier.Id,
            CompanyId = 1,
            BranchId = 1,
            Amount = 200,
            UnallocatedAmount = 200,
            Date = DateTime.UtcNow.AddDays(-4),
            Method = PaymentMethod.Cash
        });

        // Settlement Discount = 50
        context.AccountSettlements.Add(new AccountSettlement
        {
            RelatedEntityId = supplier.Id,
            CompanyId = 1,
            BranchId = 1,
            UserId = 999,
            SourceType = SettlementSource.Supplier,
            Type = SettlementType.Subtract,
            Reason = SettlementReason.Discount,
            Amount = 50,
            Date = DateTime.UtcNow.AddDays(-3),
            Notes = "Discount 50"
        });

        await context.SaveChangesAsync();

        // Act
        var currentBalance = await financeService.GetSupplierCurrentBalanceAsync(supplier.Id);
        var statement = await financeService.GetSupplierStatementAsync(supplier.Id, new StatementQueryDto { PageSize = 100, PageNumber = 1 });

        // Assert
        // Logic: 800 (Inv) - 200 (Pay) - 50 (Discount) = 550
        currentBalance.Should().Be(550m);
        statement.ClosingBalance.Should().Be(550m);

        // Assert sorting and settlement injection
        var eventTypes = statement.Items.Select(i => i.DocumentType).ToList();
        eventTypes.Should().ContainInOrder("Purchase Invoice", "Payment", "Settlement");
        
        var discountLine = statement.Items.First(i => i.Description.Contains("خصم مكتسب"));
        discountLine.Debit.Should().Be(50m); // Debit reduces what we owe to supplier
        discountLine.Credit.Should().Be(0m);
    }
}
