using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.Inventory;
using StoreManagement.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StoreManagement.Shared.Constants;
using StoreManagement.Shared.Enums;

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace StoreManagement.Infrastructure.Services;

public class BranchInventoryService : IBranchInventoryService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<BranchInventoryService> _logger;

    private static readonly ConcurrentDictionary<int, bool> _activeBackfillLocks = new();

    public BranchInventoryService(StoreDbContext context, ICurrentUserService currentUser, ILogger<BranchInventoryService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    private int GetCompanyId()
    {
        if (!_currentUser.CompanyId.HasValue)
            throw new UnauthorizedAccessException("المستخدم لا يتبع لأي شركة.");
        return _currentUser.CompanyId.Value;
    }

    public async Task<BranchProductStock> GetOrCreateStockAsync(int productId, int branchId)
    {
        if (branchId <= 0) throw new InvalidOperationException("BranchId مطلوب لكل عملية مخزون");

        var companyId = GetCompanyId();

        var stock = await _context.BranchProductStocks
            .FirstOrDefaultAsync(s => s.ProductId == productId && s.BranchId == branchId);

        if (stock != null) return stock;

        stock = new BranchProductStock
        {
            ProductId = productId,
            BranchId = branchId,
            CompanyId = companyId,
            Quantity = 0,
            ReservedQuantity = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastTransactionAt = DateTime.UtcNow
        };

        _context.BranchProductStocks.Add(stock);

        try
        {
            await _context.SaveChangesAsync();
            return stock;
        }
        catch (DbUpdateException)
        {
            // تم إنشاؤه بواسطة عملية أخرى في نفس اللحظة
            _context.Entry(stock).State = EntityState.Detached;
            
            stock = await _context.BranchProductStocks
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.BranchId == branchId);

            if (stock == null)
            {
                throw new InvalidOperationException("فشل في استرجاع سجل المخزون بعد خطأ التزامن.");
            }

            return stock;
        }
    }

    public async Task IncreaseStockAsync(int productId, int branchId, double qty)
    {
        if (qty < 0) throw new ArgumentException("الكمية يجب أن تكون موجبة عند زيادة المخزون.");

        var stock = await GetOrCreateStockAsync(productId, branchId);

        stock.Quantity += qty;
        stock.UpdatedAt = DateTime.UtcNow;
        stock.LastTransactionAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DecreaseStockAsync(int productId, int branchId, double qty, bool allowNegative = false)
    {
        if (qty < 0) throw new ArgumentException("الكمية يجب أن تكون موجبة للاستخدام في ميثود الخصم.");

        var stock = await GetOrCreateStockAsync(productId, branchId);

        if (!allowNegative && (stock.Quantity < qty))
        {
            throw new InvalidOperationException($"الكمية المتاحة بالفرع غير كافية. المتاح: {stock.Quantity}، المطلوب: {qty}");
        }

        stock.Quantity -= qty;
        stock.UpdatedAt = DateTime.UtcNow;
        stock.LastTransactionAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task TransferStockAsync(int productId, int fromBranchId, int toBranchId, double qty)
    {
        if (fromBranchId == toBranchId) throw new InvalidOperationException("لا يمكن التحويل لنفس الفرع.");
        if (qty <= 0) throw new InvalidOperationException("الكمية المحولة يجب أن تكون موجبة وأكبر من صفر.");

        bool ownsTx = _context.Database.CurrentTransaction == null;
        var tx = ownsTx ? await _context.Database.BeginTransactionAsync() : null;
        try
        {
            // لا يسمح بالسالب في التحويل إطلاقاً
            await DecreaseStockAsync(productId, fromBranchId, qty, allowNegative: false);
            await IncreaseStockAsync(productId, toBranchId, qty);

            if (ownsTx) await tx!.CommitAsync();
        }
        catch
        {
            if (ownsTx) await tx!.RollbackAsync();
            throw;
        }
        finally
        {
            if (ownsTx) await tx!.DisposeAsync();
        }
    }

    public async Task<double> GetAvailableQtyAsync(int productId, int branchId)
    {
        if (branchId <= 0) return 0;
        
        var stock = await _context.BranchProductStocks
            .FirstOrDefaultAsync(s => s.ProductId == productId && s.BranchId == branchId);

        // المتاح هو الكمية الحالية مطروحاً منها الكميات المحجوزة
        return stock == null ? 0 : stock.Quantity - stock.ReservedQuantity;
    }

    public async Task<double> GetTotalStockAsync(int productId)
    {
        // حساب إجمالي كل الأرصدة لهذا المنتج عبر كل الفروع
        return await _context.BranchProductStocks
            .Where(s => s.ProductId == productId)
            .SumAsync(s => s.Quantity);
    }

    public async Task<List<BranchStockDto>> GetStockByProductAsync(int productId)
    {
        return await _context.BranchProductStocks
            .Include(s => s.Branch)
            .Where(s => s.ProductId == productId)
            .Select(s => new BranchStockDto
            {
                BranchId = s.BranchId,
                BranchName = s.Branch.Name,
                ProductId = s.ProductId,
                ProductName = s.Product.Name,
                Quantity = s.Quantity,
                ReservedQuantity = s.ReservedQuantity
            })
            .ToListAsync();
    }

    public async Task<List<BranchStockDto>> GetStockByBranchAsync(int branchId)
    {
        return await _context.BranchProductStocks
            .Include(s => s.Product)
            .Where(s => s.BranchId == branchId)
            .Select(s => new BranchStockDto
            {
                BranchId = s.BranchId,
                ProductId = s.ProductId,
                ProductName = s.Product.Name,
                Quantity = s.Quantity,
                ReservedQuantity = s.ReservedQuantity
            })
            .ToListAsync();
    }

    public async Task<BranchStockInitializationResultDto> InitializeFromTransactionsAsync(int? companyId = null, bool forceReset = false, bool dryRun = false)
    {
        var targetCompanyId = companyId ?? _currentUser.CompanyId;
        if (targetCompanyId == null) throw new InvalidOperationException("CompanyId مطلوب لفهرسة الأرصدة.");

        var result = new BranchStockInitializationResultDto { CompaniesProcessed = 1 };

        // 1. منع التشغيل المتوازي
        if (!_activeBackfillLocks.TryAdd(targetCompanyId.Value, true))
        {
            var msg = "عملية التهيئة (Backfill) قيد التشغيل بالفعل لهذه الشركة. يرجى المحاولة لاحقاً.";
            _logger.LogWarning(msg);
            result.Warnings.Add(msg);
            return result;
        }

        try
        {
            var existingStocks = await _context.BranchProductStocks
                .Where(b => b.CompanyId == targetCompanyId)
                .ToListAsync();

            if (existingStocks.Any() && !forceReset)
            {
                var msg = "الأرصدة متواجدة مسبقاً. لم يتم البناء لعدم تفعيل forceReset.";
                _logger.LogWarning(msg);
                result.Warnings.Add(msg);
                return result;
            }

            var start = DateTime.UtcNow;

            var tx = dryRun ? null : await _context.Database.BeginTransactionAsync();
            try
            {
                if (!dryRun && existingStocks.Any())
                {
                    _context.BranchProductStocks.RemoveRange(existingStocks);
                    await _context.SaveChangesAsync();
                }

                var validProducts = await _context.Products.Select(p => p.Id).ToHashSetAsync();
                var validBranches = await _context.Branches.Select(b => b.Id).ToHashSetAsync();

                var hasTransactions = await _context.StockTransactions.AnyAsync(st => st.CompanyId == targetCompanyId);
                if (!hasTransactions)
                {
                    var msg = "لا توجد أي حركات مخزون سابقة لمعالجتها.";
                    _logger.LogWarning(msg);
                    result.Warnings.Add(msg);
                    result.DurationMs = (DateTime.UtcNow - start).TotalMilliseconds;
                    return result;
                }

                var stockDict = new Dictionary<(int ProductId, int BranchId), BranchProductStock>();
                var expectedImpactPerProduct = new Dictionary<int, double>();

                await foreach (var txItem in _context.StockTransactions
                    .Where(st => st.CompanyId == targetCompanyId)
                    .OrderBy(st => st.Date)
                    .ThenBy(st => st.Id)
                    .AsAsyncEnumerable())
                {
                    result.TransactionsProcessed++;

                    if (!validProducts.Contains(txItem.ProductId))
                    {
                        var w = $"تخطي الحركة رقم {txItem.Id} بسبب عدم وجود المنتج (محذوف) (={txItem.ProductId}).";
                        _logger.LogWarning(w);
                        result.Warnings.Add(w);
                        result.SkippedTransactions++;
                        continue;
                    }
                    
                    if (!validBranches.Contains(txItem.BranchId))
                    {
                        var w = $"تخطي الحركة رقم {txItem.Id} بسبب عدم وجود الفرع (محذوف أو خطأ) (={txItem.BranchId}).";
                        _logger.LogWarning(w);
                        result.Warnings.Add(w);
                        result.SkippedTransactions++;
                        continue;
                    }

                    var key = (txItem.ProductId, txItem.BranchId);
                    if (!stockDict.TryGetValue(key, out var stock))
                    {
                        stock = new BranchProductStock
                        {
                            ProductId = txItem.ProductId,
                            BranchId = txItem.BranchId,
                            CompanyId = targetCompanyId.Value,
                            Quantity = 0,
                            ReservedQuantity = 0,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        stockDict[key] = stock;
                        result.BranchStockRowsCreated++;
                        if (!dryRun)
                        {
                            _context.BranchProductStocks.Add(stock);
                        }
                    }
                    else
                    {
                        result.BranchStockRowsUpdated++;
                    }

                    if (txItem.MovementType == StockMovementType.In || txItem.MovementType == StockMovementType.TransferIn)
                    {
                        stock.Quantity += txItem.Quantity;
                        expectedImpactPerProduct[txItem.ProductId] = expectedImpactPerProduct.GetValueOrDefault(txItem.ProductId) + txItem.Quantity;
                    }
                    else if (txItem.MovementType == StockMovementType.Out || txItem.MovementType == StockMovementType.TransferOut)
                    {
                        stock.Quantity -= txItem.Quantity;
                        expectedImpactPerProduct[txItem.ProductId] = expectedImpactPerProduct.GetValueOrDefault(txItem.ProductId) - txItem.Quantity;
                    }
                    else
                    {
                        var w = $"حركة رقم {txItem.Id} نوع الحركة غير مدعوم ({txItem.MovementType}). تم التجاهل.";
                        _logger.LogWarning(w);
                        result.Warnings.Add(w);
                        result.SkippedTransactions++;
                        continue;
                    }

                    stock.LastTransactionAt = stock.LastTransactionAt == null 
                        ? txItem.Date 
                        : (stock.LastTransactionAt.Value > txItem.Date ? stock.LastTransactionAt.Value : txItem.Date);
                    stock.UpdatedAt = DateTime.UtcNow;

                    if (stock.Quantity < 0)
                    {
                        var w = $"الرصيد للمنتج {txItem.ProductId} في الفرع {txItem.BranchId} انتهى بالسالب ({stock.Quantity}) في الحركة {txItem.Id}.";
                        _logger.LogWarning(w);
                        result.Warnings.Add(w);
                    }
                }

                if (!dryRun)
                {
                    await _context.SaveChangesAsync();
                    if (tx != null) await tx.CommitAsync();
                }

                // --- تحقق نهائي بعد الـ Rebuild ---
                // مقارنة مجموع أرصدة BranchProductStocks لكل منتج مع مجموع أثر الحركات التاريخية المحسوب
                var computedStocksPerProduct = stockDict.Values
                    .GroupBy(s => s.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(s => s.Quantity));

                foreach (var expected in expectedImpactPerProduct)
                {
                    computedStocksPerProduct.TryGetValue(expected.Key, out var computedTotal);
                    // TODO: migrate to decimal later for better precision guarantees
                    if (Math.Abs(computedTotal - expected.Value) > 0.001) // دقة الفواصل
                    {
                        var w = $"تحذير: عدم تطابق في رصيد المنتج {expected.Key}. أثر الحركات الإجمالي ({expected.Value}) لا يطابق الرصيد المحسوب للفروع ({computedTotal}).";
                        _logger.LogWarning(w);
                        result.Warnings.Add(w);
                    }
                }

                result.DurationMs = (DateTime.UtcNow - start).TotalMilliseconds;
                return result;
            }
            catch (Exception ex)
            {
                if (tx != null) await tx.RollbackAsync();
                _logger.LogError(ex, "فشل أثناء البناء للشركة {CompanyId}", targetCompanyId);
                throw;
            }
        }
        finally
        {
            _activeBackfillLocks.TryRemove(targetCompanyId.Value, out _);
        }
    }
}
