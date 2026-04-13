using System.Net.Http.Json;
using StoreManagement.IntegrationTests.Helpers;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using Xunit;

namespace StoreManagement.IntegrationTests.Controllers;

public class BootstrapControllerTests : IClassFixture<StoreManagementApiFactory>
{
    private readonly HttpClient _client;

    public BootstrapControllerTests(StoreManagementApiFactory factory)
    {
        _client = factory.CreateClient();
        factory.SeedDatabase();
    }

    [Fact]
    public async Task GetBootstrap_ReturnsSuccessAndCorrectStructure()
    {
        // Act - the factory seeder should have created a default company/user
        // We might need to authenticate, but for now let's see if it works with the default seeder
        var response = await _client.GetAsync("/api/v1/bootstrap");

        // Assert
        response.EnsureSuccessStatusCode();
        var bootstrap = await response.Content.ReadFromJsonAsync<ApiResponse<BootstrapDto>>();
        
        Assert.NotNull(bootstrap);
        Assert.True(bootstrap.Success);
        Assert.NotNull(bootstrap.Data.User);
        Assert.NotNull(bootstrap.Data.Company);
        Assert.NotNull(bootstrap.Data.Features);
        Assert.NotNull(bootstrap.Data.UI);
    }
}
