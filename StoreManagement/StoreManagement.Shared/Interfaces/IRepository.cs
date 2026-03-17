namespace StoreManagement.Shared.Interfaces;

/// <summary>
/// واجهة عامة لنمط المستودع العام (Generic Repository)
/// </summary>
public interface IRepository<T> where T : class
{
    // استرجاع سجل بواسطة المعرف
    Task<T?> GetByIdAsync(int id);

    // استرجاع جميع السجلات
    Task<List<T>> GetAllAsync();

    // إضافة سجل جديد
    Task AddAsync(T entity);

    // تحديث سجل موجود
    void Update(T entity);

    // حذف مؤقت (Soft Delete)
    void Delete(T entity);
}

/// <summary>
/// واجهة وحدة العمل (Unit of Work) لدعم المعاملات المترابطة
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // الوصول إلى المستودعات
    IRepository<T> Repository<T>() where T : class;

    // حفظ التغييرات
    Task<int> SaveChangesAsync();

    // بدء معاملة قاعدة البيانات
    Task BeginTransactionAsync();

    // تأكيد المعاملة
    Task CommitTransactionAsync();

    // التراجع عن المعاملة
    Task RollbackTransactionAsync();
}
