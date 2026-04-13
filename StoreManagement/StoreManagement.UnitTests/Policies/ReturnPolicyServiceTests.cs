using Moq;
using StoreManagement.Services.Services.Policies;
using StoreManagement.Shared.Entities.Configuration;
using StoreManagement.Shared.DTOs.Settings;
using StoreManagement.Shared.Interfaces;
using Xunit;

namespace StoreManagement.UnitTests.Policies;

public class ReturnPolicyServiceTests
{
    private readonly Mock<ICompanySettingsService> _settingsMock;
    private readonly ReturnPolicyService _service;

    public ReturnPolicyServiceTests()
    {
        _settingsMock = new Mock<ICompanySettingsService>();
        _service = new ReturnPolicyService(_settingsMock.Object);
    }

    [Fact]
    public async Task EnsureReturnIsAllowedAsync_ShouldThrow_WhenReturnsDisabled()
    {
        // Arrange
        var settings = new CompanySettingsDto { EnableReturns = false };
        _settingsMock.Setup(x => x.GetCompanySettingsAsync(default)).ReturnsAsync(settings);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.EnsureReturnIsAllowedAsync(DateTime.UtcNow, 100));
        
        Assert.Contains("معطل حالياً", exception.Message);
    }

    [Fact]
    public async Task EnsureReturnIsAllowedAsync_ShouldThrow_WhenDaysExceeded()
    {
        // Arrange
        var settings = new CompanySettingsDto { EnableReturns = true, MaxReturnDays = 14 };
        _settingsMock.Setup(x => x.GetCompanySettingsAsync(default)).ReturnsAsync(settings);
        var oldInvoiceDate = DateTime.UtcNow.AddDays(-20);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.EnsureReturnIsAllowedAsync(oldInvoiceDate, 100));
        
        Assert.Contains("تجاوزت فترة الإرجاع", exception.Message);
    }

    [Fact]
    public async Task EnsureReturnIsAllowedAsync_ShouldSucceed_WhenWithinRange()
    {
        // Arrange
        var settings = new CompanySettingsDto { EnableReturns = true, MaxReturnDays = 30 };
        _settingsMock.Setup(x => x.GetCompanySettingsAsync(default)).ReturnsAsync(settings);
        var recentDate = DateTime.UtcNow.AddDays(-5);

        // Act
        await _service.EnsureReturnIsAllowedAsync(recentDate, 500);

        // Assert
        // No exception
    }
}
