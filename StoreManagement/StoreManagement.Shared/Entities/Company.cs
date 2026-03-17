namespace StoreManagement.Shared.Entities;

/// <summary>
/// كيان الشركة - الوحدة الرئيسية في النظام
/// </summary>
public class Company
{
    public int Id { get; set; }

    // اسم الشركة
    public string Name { get; set; } = string.Empty;

    // هل للشركة فروع متعددة
    public bool HasBranches { get; set; } = false;

    // هل تدير الشركة المخزون
    public bool ManageInventory { get; set; } = false;

    // علاقة الشركة بالفروع
    public ICollection<Branch> Branches { get; set; } = [];

    // علاقة الشركة بالمستخدمين
    public ICollection<User> Users { get; set; } = [];
}

/// <summary>
/// كيان الفرع التابع للشركة
/// </summary>
public class Branch
{
    public int Id { get; set; }

    // اسم الفرع
    public string Name { get; set; } = string.Empty;

    // معرف الشركة الأم
    public int CompanyId { get; set; }

    // علاقة الفرع بالشركة
    public Company Company { get; set; } = null!;
}
