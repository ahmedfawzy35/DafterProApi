using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StoreManagement.IntegrationTests.Helpers;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using Xunit;

namespace StoreManagement.IntegrationTests.Controllers;

public class DashboardControllerTests : IClassFixture<StoreManagementApiFactory>
{
    private readonly HttpClient _client;

    public DashboardControllerTests(StoreManagementApiFactory factory)
    {
        _client = factory.CreateClient();
        factory.SeedDatabase();
    }

    [Fact]
    public async Task Test1_Admin_NoBranchId_SeesAllBranchesAndDistribution()
    {
        // Assert: Admin role, no BranchId query config
        _client.DefaultRequestHeaders.Add("X-Test-Role", "admin");

        var response = await _client.GetAsync("/api/v1/dashboard/branch");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<BranchDashboardKpiDto>>();
        
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        
        // "كل الفروع"
        result.Data!.BranchName.Should().Be("كل الفروع");
        result.Data.BranchId.Should().Be(0);

        // Should see all branches distribution since it's an admin
        result.Data.StockDistribution.Should().NotBeEmpty();
        result.Data.StockDistribution.Count.Should().Be(2); // Branch 1 & Branch 2

        // Branch 1 has 5 + 50 + 0 = 55. Branch 2 has 15 + 2 = 17. Total = 72.
        result.Data.TotalStockQuantity.Should().Be(72);

        // Low stock items: B1(Product 1, Product 3) = 2. B2(Product 2) = 1. Total = 3
        result.Data.LowStockItemsCount.Should().Be(3);
    }

    [Fact]
    public async Task Test2_Admin_SpecificBranchId_RestrictsToSpecificBranch()
    {
        // Admin user forcing BranchId = 2
        _client.DefaultRequestHeaders.Add("X-Test-Role", "admin");
        
        var response = await _client.GetAsync("/api/v1/dashboard/branch?branchId=2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<BranchDashboardKpiDto>>();
        
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();

        result.Data!.BranchName.Should().Be("Alex Branch");
        result.Data.BranchId.Should().Be(2);

        // Stock quantity for Branch 2 is 17
        result.Data.TotalStockQuantity.Should().Be(17);

        // Low stock items for Branch 2 is just 1 (Product 2)
        result.Data.LowStockItemsCount.Should().Be(1);
    }

    [Fact]
    public async Task Test3_NormalUser_NoBranchId_SeesTheirOwnBranchOnly()
    {
        // Normal user assigned to Branch 1
        _client.DefaultRequestHeaders.Add("X-Test-Role", "user");
        _client.DefaultRequestHeaders.Add("X-Test-BranchId", "1");
        
        var response = await _client.GetAsync("/api/v1/dashboard/branch");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<BranchDashboardKpiDto>>();
        
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();

        result.Data!.BranchName.Should().Be("Main Branch");
        result.Data.BranchId.Should().Be(1);

        // Shouldn't see distribution!
        result.Data.StockDistribution.Should().BeEmpty();

        // Stock quantity for Branch 1 is 55
        result.Data.TotalStockQuantity.Should().Be(55);

        // Low stock items for Branch 1 is 2 (Product 1 + Product 3)
        result.Data.LowStockItemsCount.Should().Be(2);
    }

    [Fact]
    public async Task Test4_NormalUser_OverridesBranchId_SystemForcesTheirOwnBranch()
    {
        // Normal user assigned to Branch 1, but maliciously queries branchId=2
        _client.DefaultRequestHeaders.Add("X-Test-Role", "user");
        _client.DefaultRequestHeaders.Add("X-Test-BranchId", "1");

        var response = await _client.GetAsync("/api/v1/dashboard/branch?branchId=2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<BranchDashboardKpiDto>>();
        
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();

        // The system should lock them to Branch 1!
        result.Data!.BranchId.Should().Be(1);
        result.Data.BranchName.Should().Be("Main Branch");
        result.Data.StockDistribution.Should().BeEmpty();
        result.Data.TotalStockQuantity.Should().Be(55);
    }

    [Fact]
    public async Task Test5_CachingCheck_SuccessiveCalls()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Role", "admin");

        var response1 = await _client.GetAsync("/api/v1/dashboard/branch");
        var response2 = await _client.GetAsync("/api/v1/dashboard/branch");

        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var res1 = await response1.Content.ReadFromJsonAsync<ApiResponse<BranchDashboardKpiDto>>();
        var res2 = await response2.Content.ReadFromJsonAsync<ApiResponse<BranchDashboardKpiDto>>();

        res1!.Data!.TotalStockQuantity.Should().Be(res2!.Data!.TotalStockQuantity);
    }
}
