using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StoreManagement.IntegrationTests.Helpers;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace StoreManagement.IntegrationTests.Security;

public class CrossCompanySecurityTests : IClassFixture<StoreManagementApiFactory>
{
    private readonly HttpClient _client;
    private readonly StoreManagementApiFactory _factory;

    public CrossCompanySecurityTests(StoreManagementApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.SeedDatabase();
    }

    [Fact]
    public async Task Bootstrap_Should_SwitchToRequestedCompany_IfMultipleCompaniesExist()
    {
        // Arrange
        // 1. Manually seed a second company in the database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StoreManagement.Data.StoreDbContext>();
            if (!db.Companies.IgnoreQueryFilters().Any(c => c.Id == 2))
            {
                db.Companies.Add(new StoreManagement.Shared.Entities.Configuration.Company { Id = 2, Name = "Other Company", CompanyCode = "OTHER" });
                db.SaveChanges();
            }
        }

        // 2. Simulate User from Company 2
        _client.DefaultRequestHeaders.Add("X-Test-CompanyId", "2");
        _client.DefaultRequestHeaders.Add("X-Test-Role", "admin");

        // Act
        var response = await _client.GetAsync("/api/v1/bootstrap");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<BootstrapDto>>();

        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Company.Id.Should().Be(2); 
        result.Data.Company.Name.Should().Be("Other Company");
    }
}
