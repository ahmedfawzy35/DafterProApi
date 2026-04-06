using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StoreManagement.IntegrationTests.Helpers;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using Xunit;

namespace StoreManagement.IntegrationTests.Controllers;

public class AlertsControllerTests : IClassFixture<StoreManagementApiFactory>
{
    private readonly HttpClient _client;

    public AlertsControllerTests(StoreManagementApiFactory factory)
    {
        _client = factory.CreateClient();
        factory.SeedDatabase();
    }

    [Fact]
    public async Task Test1_Admin_NoBranchId_SeesAllLowStockItems()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Role", "admin");
        
        var response = await _client.GetAsync("/api/v1/alerts/low-stock?pageNumber=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<LowStockAlertDto>>>();
        
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        
        // We have 3 low stock items overall
        result.Data!.TotalCount.Should().Be(3);
        result.Data.Items.Count.Should().Be(3);
    }

    [Fact]
    public async Task Test2_NormalUser_NoBranchId_SeesTheirOwnBranchOnly()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Role", "user");
        _client.DefaultRequestHeaders.Add("X-Test-BranchId", "1");
        
        var response = await _client.GetAsync("/api/v1/alerts/low-stock?pageNumber=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<LowStockAlertDto>>>();
        
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        
        // Branch 1 has exactly 2 low stock items
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Items.All(i => i.BranchId == 1).Should().BeTrue();
    }

    [Fact]
    public async Task Test3_NormalUser_OverridesBranchId_SystemForcesTheirOwnBranch()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Role", "user");
        _client.DefaultRequestHeaders.Add("X-Test-BranchId", "1");
        
        // Trying to view branch 2 maliciously
        var response = await _client.GetAsync("/api/v1/alerts/low-stock?branchId=2&pageNumber=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<LowStockAlertDto>>>();
        
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        
        // The service should lock them down to Branch 1
        result.Data!.TotalCount.Should().Be(2);
        result.Data.Items.All(i => i.BranchId == 1).Should().BeTrue();
    }
}
