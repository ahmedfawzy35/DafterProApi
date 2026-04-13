using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StoreManagement.Data;
using StoreManagement.Shared.Interfaces;
using StoreManagement.IntegrationTests.Helpers;

namespace StoreManagement.IntegrationTests.Services;

[Collection("SequentialDB")]
public class InstallmentPolicyTests : IClassFixture<StoreManagementApiFactory>
{
    private readonly StoreManagementApiFactory _factory;

    public InstallmentPolicyTests(StoreManagementApiFactory factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
    }

    [Fact]
    public async Task EnsureInstallmentIsValid_ShouldThrow_IfMonthsExceedsMax()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var policyService = scope.ServiceProvider.GetRequiredService<IInstallmentPolicyService>();

        var settings = await context.CompanySettings.FirstAsync();
        settings.EnableInstallments = true; // Enable first
        settings.DefaultInstallmentCount = 12; // Allowed is 12 * 3 = 36 max
        await context.SaveChangesAsync();

        var totalAmount = 1000m;
        var downPayment = 100m;
        var months = 40; // Exceeds 36

        // Act
        Func<Task> act = async () => await policyService.EnsureInstallmentIsValidAsync(totalAmount, downPayment, months);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*تتجاوز الحد الأقصى*");
    }

    [Fact]
    public async Task EnsureInstallmentIsValid_ShouldSucceed_IfRulesMet()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var policyService = scope.ServiceProvider.GetRequiredService<IInstallmentPolicyService>();

        var settings = await context.CompanySettings.FirstAsync();
        settings.EnableInstallments = true; // Enable first
        settings.DefaultInstallmentCount = 12; // max is 12*3=36
        await context.SaveChangesAsync();

        var totalAmount = 1000m;
        var downPayment = 100m;
        var months = 24; // Less than 36

        // Act
        Func<Task> act = async () => await policyService.EnsureInstallmentIsValidAsync(totalAmount, downPayment, months);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
