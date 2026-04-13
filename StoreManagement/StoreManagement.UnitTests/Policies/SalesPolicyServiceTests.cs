using Moq;
using StoreManagement.Services.Services.Policies;
using StoreManagement.Shared.Entities.Configuration;
using StoreManagement.Shared.DTOs.Settings;
using StoreManagement.Shared.Interfaces;
using Xunit;

namespace StoreManagement.UnitTests.Policies;

public class SalesPolicyServiceTests
{
    private readonly Mock<ICompanySettingsService> _settingsMock;
    private readonly Mock<IBranchInventoryService> _inventoryMock;
    private readonly SalesPolicyService _service;

    public SalesPolicyServiceTests()
    {
        _settingsMock = new Mock<ICompanySettingsService>();
        _inventoryMock = new Mock<IBranchInventoryService>();
        _service = new SalesPolicyService(_settingsMock.Object, _inventoryMock.Object);
    }

    [Fact]
    public async Task EnsureCanSellAsync_ShouldThrow_WhenSalesDisabled()
    {
        // Arrange
        var settings = new CompanySettingsDto { EnableSales = false };
        _settingsMock.Setup(x => x.GetCompanySettingsAsync(default)).ReturnsAsync(settings);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.EnsureCanSellAsync(100));
        
        Assert.Contains("معطلة", exception.Message);
    }

    [Fact]
    public async Task EnsureCanSellAsync_ShouldThrow_WhenAmountNegative()
    {
        // Arrange
        var settings = new CompanySettingsDto { EnableSales = true };
        _settingsMock.Setup(x => x.GetCompanySettingsAsync(default)).ReturnsAsync(settings);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.EnsureCanSellAsync(-10));
        
        Assert.Contains("أكبر من أو يساوي صفر", exception.Message);
    }

    [Fact]
    public async Task EnsureCanSellAsync_ShouldSucceed_WhenSettingsAllow()
    {
        // Arrange
        var settings = new CompanySettingsDto { EnableSales = true, AllowCashSales = true };
        _settingsMock.Setup(x => x.GetCompanySettingsAsync(default)).ReturnsAsync(settings);

        // Act
        await _service.EnsureCanSellAsync(500);

        // Assert
        // No exception thrown
    }

    [Fact]
    public async Task EnsureItemInventoryAvailableAsync_ShouldThrow_WhenStockInsufficient_AndNegativeStockNotAllowed()
    {
        // Arrange
        var settings = new CompanySettingsDto { AllowNegativeStock = false };
        _settingsMock.Setup(x => x.GetCompanySettingsAsync(default)).ReturnsAsync(settings);
        _inventoryMock.Setup(x => x.GetAvailableQtyAsync(1, 1)).ReturnsAsync(5);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.EnsureItemInventoryAvailableAsync(1, 1, 10));
        
        Assert.Contains("غير متوفرة", exception.Message);
    }

    [Fact]
    public async Task EnsureItemInventoryAvailableAsync_ShouldSucceed_WhenNegativeStockAllowed()
    {
        // Arrange
        var settings = new CompanySettingsDto { AllowNegativeStock = true };
        _settingsMock.Setup(x => x.GetCompanySettingsAsync(default)).ReturnsAsync(settings);
        _inventoryMock.Setup(x => x.GetAvailableQtyAsync(1, 1)).ReturnsAsync(0);

        // Act
        await _service.EnsureItemInventoryAvailableAsync(1, 1, 10);

        // Assert
        // No exception thrown
    }
}
