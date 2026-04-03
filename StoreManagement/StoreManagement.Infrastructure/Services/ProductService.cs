using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Entities.Inventory;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ProductService(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task ValidateCanUpdateBarcodeAsync(int productId, string newBarcode)
    {
        var product = await _context.Products.FindAsync(productId)
            ?? throw new KeyNotFoundException("المنتج غير موجود");

        if (product.Barcode == newBarcode)
            return;

        // القاعدة 1: منع التغيير إذا كان الباركود القديم مستخدم في فواتير
        var usedInInvoices = await _context.InvoiceItems.AnyAsync(i => i.ProductId == productId);
        if (usedInInvoices)
            throw new InvalidOperationException("لا يمكن تغيير الباركود لأن هذا المنتج مستخدم بالفعل في فواتير سابقة");

        // القاعدة 2: منع التغيير إذا كان مستخدم في حركات مخزون سابقة (باستثناء الرصيد الافتتاحي)
        var usedInStock = await _context.StockTransactions.AnyAsync(s => 
            s.ProductId == productId && 
            s.ReferenceType != StoreManagement.Shared.Enums.StockReferenceType.InitialStock);
        
        if (usedInStock)
            throw new InvalidOperationException("لا يمكن تغيير الباركود لوجود حركات مخزون مسجلة على المنتج");

        // القاعدة 3: لا يمكن التبديل من باركود مصنع لباركود آخر إلا إذا كانت هناك سياسة صريحة
        if (product.BarcodeType == StoreManagement.Shared.Enums.BarcodeType.Factory)
            throw new InvalidOperationException("لا يمكن تغيير باركود المصنع بعد تعيينه");

        // القاعدة 4: التأكد من عدم فرادة الباركود مسجلة على منتج آخر لنفس الشركة
        // يتم التأكد عبر Level أعلى، لكن يمكن اختباره هنا أيضاً
        var duplicateExists = await _context.Products.AnyAsync(p => 
            p.Barcode == newBarcode && p.CompanyId == product.CompanyId && p.Id != productId);
            
        if (duplicateExists)
            throw new InvalidOperationException("هذا الباركود مستخدم بمنتج آخر");
    }

    public async Task UpdateCostPriceAsync(int productId, decimal newCost, string reason)
    {
        var product = await _context.Products.FindAsync(productId)
            ?? throw new KeyNotFoundException("المنتج غير موجود");

        if (product.CostPrice == newCost)
            return; // لم تتغير التكلفة

        var history = new ProductCostHistory
        {
            ProductId = productId,
            OldCost = product.CostPrice,
            NewCost = newCost,
            Reason = reason,
            Date = DateTime.UtcNow,
            UserId = (int)_currentUser.UserId!,
            CompanyId = (int)_currentUser.CompanyId!,
            CreatedByUserId = (int)_currentUser.UserId!,
        };

        product.CostPrice = newCost;

        _context.ProductCostHistories.Add(history);
        // نكتفي بالتعديل على product، سيتم الحفظ عبر السايكل العامة لـ (UnitOfWork / SaveChanges دتخل الـ InvoiceService)
        // إذا استُدعيت منفصلة يجب عمل SaveChanges.
        await _context.SaveChangesAsync();
    }
}
