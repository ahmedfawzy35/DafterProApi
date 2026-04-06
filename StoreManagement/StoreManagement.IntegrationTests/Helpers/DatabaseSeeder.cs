using StoreManagement.Data;
using StoreManagement.Shared.Entities;
using StoreManagement.Shared.Entities.Configuration;
using StoreManagement.Shared.Entities.Inventory;
using StoreManagement.Shared.Entities.Identity;
using StoreManagement.Shared.Enums;
using System;

namespace StoreManagement.IntegrationTests.Helpers;

public static class DatabaseSeeder
{
    public static void SeedForTests(StoreDbContext context)
    {
        // 1. Company
        var company = new Company
        {
            Id = 1,
            Name = "Test Company",
            CompanyCode = "TEST"
        };
        context.Companies.Add(company);
        context.SaveChanges();

        // 2. SaaS Subscription (Fix for 403 Forbidden for non-admins)
        var plan = new Plan
        {
            Id = 1,
            Name = "Pro",
            DisplayName = "Pro Plan",
            IsActive = true,
            MaxBranches = 10,
            MaxUsers = 50
        };
        context.Plans.Add(plan);
        context.SaveChanges();

        var subscription = new CompanySubscription
        {
            Id = 1,
            CompanyId = 1,
            PlanId = 1,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow.AddYears(1),
            IsActive = true
        };
        context.CompanySubscriptions.Add(subscription);
        context.SaveChanges();

        // 3. Branches
        var branch1 = new Branch { Id = 1, CompanyId = 1, Name = "Main Branch", Enabled = true };
        var branch2 = new Branch { Id = 2, CompanyId = 1, Name = "Alex Branch", Enabled = true };
        context.Branches.AddRange(branch1, branch2);
        context.SaveChanges();

        // 4. User (Required for StockTransaction FK)
        var user = new User
        {
            Id = 999,
            CompanyId = 1,
            UserName = "testuser",
            Email = "test@example.com",
            Enabled = true
        };
        context.Users.Add(user);
        context.SaveChanges();

        // 5. Products
        var product1 = new Product
        {
            Id = 1,
            CompanyId = 1,
            Name = "Laptop",
            SKU = "SKU-001",
            MinimumStock = 10,
            Price = 100,
            CostPrice = 80,
            Barcode = "1234567890010",
            IsActive = true
        };
        var product2 = new Product
        {
            Id = 2,
            CompanyId = 1,
            Name = "Mouse",
            SKU = "SKU-002",
            MinimumStock = 20,
            Price = 50,
            CostPrice = 30,
            Barcode = "1234567890011",
            IsActive = true
        };
        var product3 = new Product
        {
            Id = 3,
            CompanyId = 1,
            Name = "Keyboard",
            SKU = "SKU-003",
            MinimumStock = 15,
            Price = 75,
            CostPrice = 50,
            Barcode = "1234567890012",
            IsActive = true
        };
        context.Products.AddRange(product1, product2, product3);
        context.SaveChanges();

        // 6. Branch Product Stocks
        context.BranchProductStocks.AddRange(
            new BranchProductStock { CompanyId = 1, BranchId = 1, ProductId = 1, Quantity = 5 },
            new BranchProductStock { CompanyId = 1, BranchId = 1, ProductId = 2, Quantity = 50 },
            new BranchProductStock { CompanyId = 1, BranchId = 1, ProductId = 3, Quantity = 0 },
            new BranchProductStock { CompanyId = 1, BranchId = 2, ProductId = 1, Quantity = 15 },
            new BranchProductStock { CompanyId = 1, BranchId = 2, ProductId = 2, Quantity = 2 }
        );
        context.SaveChanges();

        // 7. Stock Transactions
        context.StockTransactions.AddRange(
            new StockTransaction
            {
                CompanyId = 1, BranchId = 1, ProductId = 1,
                MovementType = StockMovementType.In,
                Quantity = 5, Date = DateTime.UtcNow, UserId = 999,
                BeforeQuantity = 0, AfterQuantity = 5
            },
            new StockTransaction
            {
                CompanyId = 1, BranchId = 2, ProductId = 2,
                MovementType = StockMovementType.Out,
                Quantity = 1, Date = DateTime.UtcNow, UserId = 999,
                BeforeQuantity = 3, AfterQuantity = 2
            }
        );

        context.SaveChanges();
    }
}
