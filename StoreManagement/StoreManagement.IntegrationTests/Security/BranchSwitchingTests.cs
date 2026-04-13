using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StoreManagement.IntegrationTests.Helpers;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using Xunit;

namespace StoreManagement.IntegrationTests.Security;

public class BranchSwitchingTests : IClassFixture<StoreManagementApiFactory>
{
    private readonly HttpClient _client;
    private readonly StoreManagementApiFactory _factory;

    public BranchSwitchingTests(StoreManagementApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.SeedDatabase();
    }

    [Fact]
    public async Task Admin_Should_BeAbleToSwitchBranch_InBootstrap()
    {
        // Arrange: Admin role + requesting Branch 2
        _client.DefaultRequestHeaders.Add("X-Test-Role", "admin");
        _client.DefaultRequestHeaders.Add("X-Test-BranchId", "2");

        // Act
        var response = await _client.GetAsync("/api/v1/bootstrap");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<BootstrapDto>>();

        result.Should().NotBeNull();
        // Since it's an admin, we expect them to be allowed (in a real system they see all)
        // In our current BootstrapService, it returns ALL branches for the company.
        result!.Data.Branches.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Dashboard_Should_ReflectBranchOverride_ForAdmin()
    {
        // Arrange: Admin role requesting Alex Branch (ID 2)
        _client.DefaultRequestHeaders.Add("X-Test-Role", "admin");
        
        // Act
        var response = await _client.GetAsync("/api/v1/dashboard/branch?branchId=2");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<BranchDashboardKpiDto>>();
        
        result!.Data!.BranchId.Should().Be(2);
        result.Data.BranchName.Should().Be("Alex Branch");
    }

    [Fact]
    public async Task NormalUser_Should_BeLockedToTheirAssignedBranch_InDashboard()
    {
        // Arrange: User assigned to Branch 1, trying to see Branch 2
        _client.DefaultRequestHeaders.Add("X-Test-Role", "user");
        _client.DefaultRequestHeaders.Add("X-Test-BranchId", "1");
        
        // Act: Requesting branchId=2 in query
        var response = await _client.GetAsync("/api/v1/dashboard/branch?branchId=2");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<BranchDashboardKpiDto>>();
        
        // System should force them back to Branch 1
        result!.Data!.BranchId.Should().Be(1);
        result.Data.BranchName.Should().Be("Main Branch");
    }
}
