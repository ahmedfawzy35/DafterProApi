using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using StoreManagement.Data;
using StoreManagement.Infrastructure.Repositories;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure;

/// <summary>
/// تنفيذ وحدة العمل (Unit of Work) مع دعم كامل للمعاملات
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly StoreDbContext _context;
    private IDbContextTransaction? _transaction;

    // قاموس لتخزين المستودعات المُنشأة (Lazy initialization)
    private readonly Dictionary<string, object> _repositories = [];

    public UnitOfWork(StoreDbContext context)
    {
        _context = context;
    }

    // الحصول على مستودع لنوع معين أو إنشاؤه إن لم يوجد
    public IRepository<T> Repository<T>() where T : class
    {
        var typeName = typeof(T).Name;

        if (!_repositories.TryGetValue(typeName, out var repo))
        {
            repo = new Repository<T>(_context);
            _repositories[typeName] = repo;
        }

        return (IRepository<T>)repo;
    }

    // حفظ التغييرات
    public async Task<int> SaveChangesAsync()
        => await _context.SaveChangesAsync();

    // بدء معاملة قاعدة البيانات
    public async Task BeginTransactionAsync()
        => _transaction = await _context.Database.BeginTransactionAsync();

    // تأكيد المعاملة وتطبيق التغييرات
    public async Task CommitTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    // التراجع في حالة الفشل
    public async Task RollbackTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    // تحرير الموارد
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
