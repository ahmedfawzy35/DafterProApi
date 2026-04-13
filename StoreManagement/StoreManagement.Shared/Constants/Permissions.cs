using System.Linq;

namespace StoreManagement.Shared.Constants;

/// <summary>
/// تعريف ثوابت الصلاحيات (Permissions) لكل وحدة وظيفية في النظام.
/// تُستخدم كـ Claims في JWT وفي إعداد قواعد التفويض (Authorization Policies).
/// </summary>
public static class Permissions
{
    // ===== إدارة المنصة (Platform) =====
    public static class Platform
    {
        public const string ViewCompanies       = "platform.companies.view";
        public const string ManageCompanies     = "platform.companies.manage";
        public const string ViewSubscriptions   = "platform.subscriptions.view";
        public const string ManageSubscriptions = "platform.subscriptions.manage";
        public const string ViewUsers = "platform.Users.view";
        public const string ManageUsers = "platform.Users.manage";


        public static readonly string[] All = new[] { ViewCompanies, ManageCompanies, ViewSubscriptions, ManageSubscriptions , ViewUsers, ManageUsers };
    }

    // ===== الموظفون =====
    public static class Employees
    {
        public const string View    = "employees.view";
        public const string Create  = "employees.create";
        public const string Edit    = "employees.edit";
        public const string Delete  = "employees.delete";
        public const string Payroll = "employees.payroll";
        public const string Loans   = "employees.loans";

        public static readonly string[] All = new[] { View, Create, Edit, Delete, Payroll, Loans };
    }

    // ===== الحضور والغياب =====
    public static class Attendance
    {
        public const string View   = "attendance.view";
        public const string Record = "attendance.record";

        public static readonly string[] All = new[] { View, Record };
    }

    // ===== المبيعات والفواتير =====
    public static class Sales
    {
        public const string View   = "sales.view";
        public const string Create = "sales.create";
        public const string Delete = "sales.delete";
        public const string Refund = "sales.refund";

        public static readonly string[] All = new[] { View, Create, Delete, Refund };
    }

    // ===== المشتريات =====
    public static class Purchases
    {
        public const string View   = "purchases.view";
        public const string Create = "purchases.create";
        public const string Return = "purchases.return";

        public static readonly string[] All = new[] { View, Create, Return };
    }

    // ===== المرتجعات (Returns) =====
    public static class Returns
    {
        public const string View    = "returns.view";
        public const string Process = "returns.process";
        public const string Approve = "returns.approve";

        public static readonly string[] All = new[] { View, Process, Approve };
    }

    // ===== التقسيط (Installments) =====
    public static class Installments
    {
        public const string View       = "installments.view";
        public const string Collect    = "installments.collect";
        public const string Reschedule = "installments.reschedule";

        public static readonly string[] All = new[] { View, Collect, Reschedule };
    }

    // ===== الموافقات (Approvals) =====
    public static class Approvals
    {
        public const string View   = "approvals.view";
        public const string Action = "approvals.action";

        public static readonly string[] All = new[] { View, Action };
    }

    // ===== المنتجات والمخزون =====
    public static class Inventory
    {
        public const string View             = "inventory.view";
        public const string Create           = "inventory.create";
        public const string Edit             = "inventory.edit";
        public const string Delete           = "inventory.delete";
        public const string StockAdjustment  = "inventory.stock_adjustment";

        public static readonly string[] All = new[] { View, Create, Edit, Delete, StockAdjustment };
    }

    // ===== العملاء =====
    public static class Customers
    {
        public const string View   = "customers.view";
        public const string Create = "customers.create";
        public const string Edit   = "customers.edit";
        public const string Delete = "customers.delete";

        public static readonly string[] All = new[] { View, Create, Edit, Delete };
    }

    // ===== الموردون =====
    public static class Suppliers
    {
        public const string View   = "suppliers.view";
        public const string Create = "suppliers.create";
        public const string Edit   = "suppliers.edit";
        public const string Delete = "suppliers.delete";

        public static readonly string[] All = new[] { View, Create, Edit, Delete };
    }

    // ===== التسويات المالية =====
    public static class Settlements
    {
        public const string View   = "settlements.view";
        public const string Create = "settlements.create";

        public static readonly string[] All = new[] { View, Create };
    }

    // ===== المعاملات النقدية =====
    public static class Finance
    {
        public const string View   = "finance.view";
        public const string Create = "finance.create";
        public const string Delete = "finance.delete";

        public static readonly string[] All = new[] { View, Create, Delete };
    }

    // ===== التقارير =====
    public static class Reports
    {
        public const string View   = "reports.view";
        public const string Export = "reports.export";

        public static readonly string[] All = new[] { View, Export };
    }

    // ===== إعدادات النظام (Admin فقط) =====
    public static class Settings
    {
        public const string General  = "settings.general";
        public const string Users    = "settings.users";
        public const string Roles    = "settings.roles";
        public const string Billing  = "settings.billing";
        public const string Branches = "settings.branches";
        public const string CompanyAdmin = "settings.company_admin"; // New permission for Company Settings Management

        public static readonly string[] All = new[] { General, Users, Roles, Billing, Branches, CompanyAdmin };
    }

    // ===== جميع صلاحيات المستأجر (Tenant) =====
    public static string[] GetAllTenant() =>
        Employees.All
            .Concat(Attendance.All)
            .Concat(Sales.All)
            .Concat(Purchases.All)
            .Concat(Returns.All)
            .Concat(Installments.All)
            .Concat(Approvals.All)
            .Concat(Inventory.All)
            .Concat(Customers.All)
            .Concat(Suppliers.All)
            .Concat(Settlements.All)
            .Concat(Finance.All)
            .Concat(Reports.All)
            .Concat(Settings.All)
            .ToArray();

    // ===== قائمة كل الصلاحيات في النظام (Platform + Tenant) =====
    public static string[] GetAll() =>
        Platform.All
            .Concat(GetAllTenant())
            .ToArray();

    // ===== الصلاحيات الافتراضية لكل دور =====
    public static string[] GetForRole(string roleName) => roleName switch
    {
        DefaultRoles.SuperAdmin => Platform.All, // مدير النظام الأساسي — صلاحيات المنصة فقط
        DefaultRoles.Owner => GetAllTenant(), // المالك — كل صلاحيات المستأجر
        DefaultRoles.Manager =>
            Employees.All
            .Concat(Attendance.All)
            .Concat(Sales.All)
            .Concat(Purchases.All)
            .Concat(Returns.All)
            .Concat(Installments.All)
            .Concat(Approvals.All)
            .Concat(Inventory.All)
            .Concat(Customers.All)
            .Concat(Suppliers.All)
            .Concat(Settlements.All)
            .Concat(Finance.All)
            .Concat(Reports.All)
            .Concat(new[] { Settings.General, Settings.Branches, Settings.CompanyAdmin })
            .ToArray(),
        DefaultRoles.Accountant =>
            Settlements.All
            .Concat(Finance.All)
            .Concat(Reports.All)
            .Concat(Approvals.All)
            .Concat(Installments.All)
            .Concat(new[] { Sales.View, Purchases.View, Employees.Payroll, Employees.Loans, Employees.View, Returns.Approve, Returns.Process })
            .ToArray(),
        DefaultRoles.Sales =>
            new[] { Sales.View, Sales.Create, Customers.View, Customers.Create, Customers.Edit, Inventory.View, Returns.View, Returns.Process, Installments.Collect },
        DefaultRoles.InventoryClerk =>
            new[] { Inventory.View, Inventory.Create, Inventory.Edit, Inventory.StockAdjustment, Purchases.View, Purchases.Create, Returns.Process },
        _ => new string[0]
    };
}

/// <summary>
/// الأدوار الافتراضية في النظام
/// </summary>
public static class DefaultRoles
{
    public const string SuperAdmin     = "SuperAdmin";
    public const string Owner          = "Owner";
    public const string Manager        = "Manager";
    public const string Accountant     = "Accountant";
    public const string Sales          = "Sales";
    public const string InventoryClerk = "InventoryClerk";

    public static readonly string[] PlatformRoles = new[] { SuperAdmin };
    public static readonly string[] TenantRoles = new[] { Owner, Manager, Accountant, Sales, InventoryClerk };
    
    public static readonly string[] All = PlatformRoles.Concat(TenantRoles).ToArray();
}

public class PermissionDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPlatform { get; set; }
}
