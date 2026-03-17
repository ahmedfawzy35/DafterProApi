namespace StoreManagement.Shared.Common;

/// <summary>
/// نموذج النتائج المقسّمة للصفحات (Pagination)
/// </summary>
public class PagedResult<T>
{
    // البيانات الحالية في الصفحة
    public List<T> Items { get; set; } = [];

    // رقم الصفحة الحالية
    public int PageNumber { get; set; }

    // حجم الصفحة
    public int PageSize { get; set; }

    // إجمالي عدد السجلات
    public int TotalCount { get; set; }

    // إجمالي عدد الصفحات
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // هل هناك صفحة سابقة
    public bool HasPreviousPage => PageNumber > 1;

    // هل هناك صفحة تالية
    public bool HasNextPage => PageNumber < TotalPages;
}
