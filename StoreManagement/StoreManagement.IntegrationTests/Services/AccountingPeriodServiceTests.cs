using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StoreManagement.Data;
using StoreManagement.Infrastructure.Services;
using StoreManagement.IntegrationTests.Helpers;
using StoreManagement.Shared.Entities.Finance;
using StoreManagement.Shared.Exceptions;
using StoreManagement.Shared.Interfaces;
using Xunit;

namespace StoreManagement.IntegrationTests.Services;

public class AccountingPeriodServiceTests : IClassFixture<StoreManagementApiFactory>
{
    private readonly StoreManagementApiFactory _factory;

    public AccountingPeriodServiceTests(StoreManagementApiFactory factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
    }

    [Fact]
    public async Task EnsureDateIsOpenAsync_WhenPeriodIsOpen_ShouldNotThrow()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IAccountingPeriodService>();
        var companyId = 1;
        var date = new DateTime(2025, 5, 10);

        context.AccountingPeriods.Add(new AccountingPeriod
        {
            CompanyId = companyId,
            StartDate = new DateTime(2025, 5, 1),
            EndDate = new DateTime(2025, 5, 31),
            IsClosed = false
        });
        await context.SaveChangesAsync();

        // Act
        var act = async () => await service.EnsureDateIsOpenAsync(companyId, date);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureDateIsOpenAsync_WhenPeriodIsClosed_ShouldThrowException()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IAccountingPeriodService>();
        var companyId = 2; // Use different company to avoid conflict in testing DB
        var date = new DateTime(2025, 4, 15);

        context.AccountingPeriods.Add(new AccountingPeriod
        {
            CompanyId = companyId,
            StartDate = new DateTime(2025, 4, 1),
            EndDate = new DateTime(2025, 4, 30),
            IsClosed = true,
            ClosedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var act = async () => await service.EnsureDateIsOpenAsync(companyId, date);

        // Assert
        await act.Should().ThrowAsync<ClosedAccountingPeriodException>()
            .WithMessage($"*Operation Date: {date:yyyy-MM-dd}*");
    }

    [Fact]
    public async Task EnsureDateIsOpenAsync_WhenDateIsOutsideAnyPeriod_ShouldNotThrow()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IAccountingPeriodService>();
        var companyId = 3;
        var date = new DateTime(2025, 6, 1);

        // Act
        var act = async () => await service.EnsureDateIsOpenAsync(companyId, date);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
