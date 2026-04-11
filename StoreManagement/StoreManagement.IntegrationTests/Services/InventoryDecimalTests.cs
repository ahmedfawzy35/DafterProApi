using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StoreManagement.Data;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.Inventory;
using StoreManagement.Shared.Entities.Sales;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;
using StoreManagement.IntegrationTests.Helpers;

namespace StoreManagement.IntegrationTests.Services;

/// <summary>
/// Step 7 — التحقق من دقة الكميات الكسرية بعد ترحيل double → decimal(18,4).
/// الهدف: التأكد أنه لا توجد Ghost Values أو فوارق تقريبية في أي عملية مخزونية.
/// </summary>
[Collection("SequentialDB")]
public class InventoryDecimalTests : IClassFixture<StoreManagementApiFactory>
{
    private readonly StoreManagementApiFactory _factory;

    public InventoryDecimalTests(StoreManagementApiFactory factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
    }

    // =====================================================================
    // Test 1: شراء بكمية كسرية
    // =====================================================================
    [Fact]
    public async Task Purchase_FractionalQuantity_ShouldStore_Precisely()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var branchInventory = scope.ServiceProvider.GetRequiredService<IBranchInventoryService>();

        // منتج في فرع 1 برصيد صفر ابتداءً
        var productId = 1;
        var branchId = 1;
        var fractionalQty = 10.125m; // كمية كسرية دقيقة

        // Act: زيادة مخزون بكمية كسرية
        await branchInventory.IncreaseStockAsync(productId, branchId, fractionalQty);

        // Assert
        var stock = await context.BranchProductStocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ProductId == productId && s.BranchId == branchId);

        stock.Should().NotBeNull();
        // يجب ألا يكون هناك أي Ghost Value — القيمة exact
        stock!.Quantity.Should().BeGreaterThan(0);
        // التحقق بدون tolerance: decimal مضمون
        (stock.Quantity % 0.001m).Should().Be(0m,
            "Decimal(18,4) cannot have ghost fractions beyond 4 decimal places");
    }

    // =====================================================================
    // Test 2: بيع بكمية كسرية يُخصم بدقة
    // =====================================================================
    [Fact]
    public async Task Sale_FractionalQuantity_ShouldDeduct_Precisely()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var branchInventory = scope.ServiceProvider.GetRequiredService<IBranchInventoryService>();

        var productId = 2; // Mouse — رصيده 50 في branchId=1
        var branchId = 1;
        var fractionalSale = 0.055m;

        var before = await branchInventory.GetAvailableQtyAsync(productId, branchId);

        // Act: خصم كمية كسرية دقيقة
        await branchInventory.DecreaseStockAsync(productId, branchId, fractionalSale);

        // Assert
        var after = await branchInventory.GetAvailableQtyAsync(productId, branchId);
        var diff = before - after;

        diff.Should().Be(fractionalSale,
            "decimal subtraction is exact — no floating point ghost difference");

        // تأكيد: لا يوجد فرق خفي
        (diff - fractionalSale).Should().Be(0m, "No ghost fractions after decimal subtraction");
    }

    // =====================================================================
    // Test 3: مرتجع يُعيد الكمية الكسرية بدقة
    // =====================================================================
    [Fact]
    public async Task Return_FractionalQuantity_ShouldRestore_Precisely()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var branchInventory = scope.ServiceProvider.GetRequiredService<IBranchInventoryService>();

        var productId = 2;
        var branchId = 1;
        var saleQty = 3.333m;

        var balanceBefore = await branchInventory.GetAvailableQtyAsync(productId, branchId);

        // Simulate Sale
        await branchInventory.DecreaseStockAsync(productId, branchId, saleQty);
        var balanceAfterSale = await branchInventory.GetAvailableQtyAsync(productId, branchId);

        // Simulate Return (restore)
        await branchInventory.IncreaseStockAsync(productId, branchId, saleQty);
        var balanceAfterReturn = await branchInventory.GetAvailableQtyAsync(productId, branchId);

        // Assert: الرصيد يعود بالضبط لما كان
        balanceAfterReturn.Should().Be(balanceBefore,
            "Return of exact quantity must restore the original balance with zero rounding error");
    }

    // =====================================================================
    // Test 4: تحويل كسري بين فرعين يتوازن بدقة (صفرية غوستية)
    // =====================================================================
    [Fact]
    public async Task Transfer_FractionalQuantity_ShouldBalance_BothBranches()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var branchInventory = scope.ServiceProvider.GetRequiredService<IBranchInventoryService>();

        var productId = 1;  // productId=1 موجود في كلا الفرعين
        var fromBranch = 1;
        var toBranch = 2;
        var transferQty = 7.333m;

        // نضمن وجود رصيد كافٍ في fromBranch
        await branchInventory.IncreaseStockAsync(productId, fromBranch, 50m);

        var fromBefore = await branchInventory.GetAvailableQtyAsync(productId, fromBranch);
        var toBefore = await branchInventory.GetAvailableQtyAsync(productId, toBranch);

        // Act
        await branchInventory.TransferStockAsync(productId, fromBranch, toBranch, transferQty);

        var fromAfter = await branchInventory.GetAvailableQtyAsync(productId, fromBranch);
        var toAfter = await branchInventory.GetAvailableQtyAsync(productId, toBranch);

        // Assert: المجموع الكلي ثابت
        var totalBefore = fromBefore + toBefore;
        var totalAfter = fromAfter + toAfter;

        totalAfter.Should().Be(totalBefore,
            "Transfer conserves total inventory — no ghost values");

        // التحقق من كل فرع على حدة
        (fromBefore - fromAfter).Should().Be(transferQty, "Source branch decreased by exact qty");
        (toAfter - toBefore).Should().Be(transferQty, "Destination branch increased by exact qty");
    }

    // =====================================================================
    // Test 5: تسوية كسرية تُطبَّق بدقة
    // =====================================================================
    [Fact]
    public async Task Adjustment_FractionalQuantity_ShouldApply_Precisely()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        var branchInventory = scope.ServiceProvider.GetRequiredService<IBranchInventoryService>();

        var productId = 3; // Keyboard — رصيده 0 في branch 1
        var branchId = 1;

        // نبدأ بكمية معروفة
        await branchInventory.IncreaseStockAsync(productId, branchId, 100m);
        var before = await branchInventory.GetAvailableQtyAsync(productId, branchId);

        // Act: تسوية بكمية كسرية سالبة (خصم)
        var adjustmentDto = new CreateStockAdjustmentDto
        {
            BranchId = branchId,
            Notes = "Decimal Precision Test Adjustment",
            Items = new List<CreateStockAdjustmentItemDto>
            {
                new CreateStockAdjustmentItemDto
                {
                    ProductId = productId,
                    Quantity = -12.375m, // كسري دقيق
                    ReasonType = (int)StockAdjustmentReason.Lost
                }
            }
        };

        await inventoryService.CreateStockAdjustmentAsync(adjustmentDto);

        var after = await branchInventory.GetAvailableQtyAsync(productId, branchId);

        // Assert
        (before - after).Should().Be(12.375m,
            "Fractional adjustment must be applied without rounding error");
        after.Should().Be(87.625m, "Exact result: 100 - 12.375 = 87.625");
    }

    // =====================================================================
    // Test 6: سلسلة عمليات مختلطة — لا Ghost Values في النهاية
    // =====================================================================
    [Fact]
    public async Task Balance_AfterMixedOperations_ShouldHave_NoGhostValues()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var branchInventory = scope.ServiceProvider.GetRequiredService<IBranchInventoryService>();

        var productId = 2;
        var branchId = 1;

        // Set a known starting point
        // Product 2 (Mouse) starts with 50 in branch 1 from seeder
        var startBalance = await branchInventory.GetAvailableQtyAsync(productId, branchId);

        // Act: سلسلة عمليات كسرية
        await branchInventory.IncreaseStockAsync(productId, branchId, 10.100m);  // +10.1
        await branchInventory.DecreaseStockAsync(productId, branchId, 3.333m);   // -3.333
        await branchInventory.IncreaseStockAsync(productId, branchId, 2.233m);   // +2.233
        await branchInventory.DecreaseStockAsync(productId, branchId, 9.000m);   // -9.0

        // المجموع الرياضي مع decimal:
        // +10.1 - 3.333 + 2.233 - 9.0 = 0.0m
        var expectedNet = 10.100m - 3.333m + 2.233m - 9.000m; // = 0.0m بالضبط
        var finalBalance = await branchInventory.GetAvailableQtyAsync(productId, branchId);

        // Assert
        finalBalance.Should().Be(startBalance + expectedNet,
            "Decimal arithmetic is exact — no floating point ghost accumulation");

        // لو كانت double لكان هناك فارق خفي مثل 0.0000000000000001
        (finalBalance - (startBalance + expectedNet)).Should().Be(0m,
            "Zero ghost residual: decimal has no accumulated floating point error");
    }

    // =====================================================================
    // Test 7: GetTotalStockAsync يُجمع decimal بدقة
    // =====================================================================
    [Fact]
    public async Task GetTotalStockAsync_ShouldReturn_PreciseDecimalSum()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        var branchInventory = scope.ServiceProvider.GetRequiredService<IBranchInventoryService>();

        // Product 1 موجود في Branch 1 (5) + Branch 2 (15) = 20 من البصمة
        // نجعله رصيداً معروفاً تماماً
        var productId = 1;

        // Set exact stocks
        var stock1 = await context.BranchProductStocks
            .FirstOrDefaultAsync(s => s.ProductId == productId && s.BranchId == 1);
        var stock2 = await context.BranchProductStocks
            .FirstOrDefaultAsync(s => s.ProductId == productId && s.BranchId == 2);

        if (stock1 != null) stock1.Quantity = 10.125m;
        if (stock2 != null) stock2.Quantity = 4.875m;
        await context.SaveChangesAsync();

        // Act
        var total = await branchInventory.GetTotalStockAsync(productId);

        // Assert: 10.125 + 4.875 = 15.000 بالضبط
        total.Should().Be(15.000m,
            "SumAsync on decimal columns returns precise result without floating point error");
    }
}
