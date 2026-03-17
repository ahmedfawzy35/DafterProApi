using Microsoft.EntityFrameworkCore;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Repositories;

/// <summary>
/// تنفيذ المستودع العام (Generic Repository) باستخدام EF Core
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    // استرجاع سجل بواسطة المعرف
    public async Task<T?> GetByIdAsync(int id)
        => await _dbSet.FindAsync(id);

    // استرجاع جميع السجلات (مع دعم Global Query Filters)
    public async Task<List<T>> GetAllAsync()
        => await _dbSet.ToListAsync();

    // إضافة سجل جديد
    public async Task AddAsync(T entity)
        => await _dbSet.AddAsync(entity);

    // تحديث سجل موجود
    public void Update(T entity)
        => _dbSet.Update(entity);

    // حذف مؤقت - يُعالج بواسطة المعترض SoftDeleteAndAuditInterceptor
    public void Delete(T entity)
        => _dbSet.Remove(entity);
}
