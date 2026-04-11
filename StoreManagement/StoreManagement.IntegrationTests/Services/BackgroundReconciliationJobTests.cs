using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StoreManagement.Data;
using StoreManagement.Shared.Entities.Inventory;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;
using StoreManagement.IntegrationTests.Helpers;
using Xunit;

namespace StoreManagement.IntegrationTests.Services;

[Collection("SequentialDB")]
public class BackgroundReconciliationJobTests : IClassFixture<StoreManagementApiFactory>
{
    private readonly StoreManagementApiFactory _factory;

    public BackgroundReconciliationJobTests(StoreManagementApiFactory factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
    }

    [Fact]
    public async Task E2E_Reconciliation_Should_Detect_Deduplicate_And_AutoResolve_Anomaly()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var reconciliationService = scope.ServiceProvider.GetRequiredService<IReconciliationService>();
        
        var companyId = 1;
        var branchId = 1;

        // 1. Inject Negative Stock Anomaly (Drift)
        var brokenProduct = new Product { CompanyId = companyId, Name = "Broken Product", IsActive = true, Price = 10, CostPrice = 5 };
        context.Products.Add(brokenProduct);
        await context.SaveChangesAsync();

        var negativeStock = new BranchProductStock { CompanyId = companyId, BranchId = branchId, ProductId = brokenProduct.Id, Quantity = -15m };
        context.BranchProductStocks.Add(negativeStock);
        await context.SaveChangesAsync();

        // 2. Run Reconciliation (Detect)
        await reconciliationService.RunCompanyReconciliationAsync(companyId);

        var findingsRound1 = await context.ReconciliationFindings.Where(f => f.CompanyId == companyId).ToListAsync();
        
        findingsRound1.Should().ContainSingle(f => f.EntityId == negativeStock.Id && f.RuleCode == "INV_NEGATIVE_STOCK");
        var primaryFinding = findingsRound1.Single(f => f.EntityId == negativeStock.Id && f.RuleCode == "INV_NEGATIVE_STOCK");
        
        primaryFinding.Status.Should().Be(FindingStatus.Open);
        primaryFinding.Message.Should().Contain("-15");

        // 3. Run Reconciliation Again (Deduplicate)
        // Should NOT create a duplicate, but rather update the LastSeenAt
        var initialLastSeen = primaryFinding.LastSeenAt;
        
        // Wait a tiny bit (mock passage of time)
        await Task.Delay(100); 

        await reconciliationService.RunCompanyReconciliationAsync(companyId);

        var findingsRound2 = await context.ReconciliationFindings.Where(f => f.CompanyId == companyId).ToListAsync();
        findingsRound2.Count(f => f.EntityId == negativeStock.Id && f.RuleCode == "INV_NEGATIVE_STOCK").Should().Be(1, "Deduplication failed, generated multiple findings for the same anomaly.");

        var persistedFinding = findingsRound2.Single(f => f.EntityId == negativeStock.Id && f.RuleCode == "INV_NEGATIVE_STOCK");
        persistedFinding.Status.Should().Be(FindingStatus.Open);
        persistedFinding.LastSeenAt.Should().BeAfter(initialLastSeen, "LastSeenAt should be updated on recurrence");

        // 4. Fix Anomaly
        negativeStock.Quantity = 10m; // Recovered stock manually somehow
        await context.SaveChangesAsync();

        // 5. Run Reconciliation (Auto-Resolve)
        await reconciliationService.RunCompanyReconciliationAsync(companyId);

        var findingsRound3 = await context.ReconciliationFindings.Where(f => f.CompanyId == companyId).ToListAsync();
        var resolvedFinding = findingsRound3.Single(f => f.EntityId == negativeStock.Id && f.RuleCode == "INV_NEGATIVE_STOCK");

        resolvedFinding.Status.Should().Be(FindingStatus.Resolved, "Because the anomaly was not detected in the latest scan, it successfully auto-resolved");
        resolvedFinding.ResolvedAt.Should().NotBeNull();
        resolvedFinding.ResolutionSource.Should().Be("AutoReconciledScan");
    }
}
