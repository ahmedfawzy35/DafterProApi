using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Entities;

namespace StoreManagement.Shared.DTOs;

// ===== DTOs خاصة بالعميل =====

/// <summary>
/// بيانات إنشاء عميل جديد
/// </summary>
public class CreateCustomerDto
{
    public string Name { get; set; } = string.Empty;
    public List<string> Phones { get; set; } = [];
}

/// <summary>
/// بيانات تعديل عميل موجود
/// </summary>
public class UpdateCustomerDto
{
    public string Name { get; set; } = string.Empty;
    public List<string> Phones { get; set; } = [];
}

/// <summary>
/// بيانات عرض العميل (استجابة القراءة)
/// </summary>
public class CustomerReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CashBalance { get; set; }
    public List<string> Phones { get; set; } = [];
    public DateTime CreatedDate { get; set; }
}

// ===== DTOs خاصة بالمورد =====

public class CreateSupplierDto
{
    public string Name { get; set; } = string.Empty;
    public List<string> Phones { get; set; } = [];
}

public class UpdateSupplierDto
{
    public string Name { get; set; } = string.Empty;
    public List<string> Phones { get; set; } = [];
}

public class SupplierReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CashBalance { get; set; }
    public List<string> Phones { get; set; } = [];
    public DateTime CreatedDate { get; set; }
}

// ===== DTOs خاصة بتصنيف المنتج (Category) =====

public class CreateProductCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateProductCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ProductCategoryReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// ===== DTOs خاصة بالمنتج =====

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public string Unit { get; set; } = "قطعة";
    public string? SKU { get; set; }
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public int? CategoryId { get; set; }
    public double MinimumStock { get; set; }
    public double ReorderLevel { get; set; }
    public bool IsSellable { get; set; } = true;
    public bool IsPurchasable { get; set; } = true;

    // باركود اختياري — إذا أُرسل يُعامَل كباركود مصنعي
    public string? Barcode { get; set; }
    public BarcodeFormat BarcodeFormat { get; set; } = BarcodeFormat.EAN13;
}

public class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public string Unit { get; set; } = "قطعة";
    public string? SKU { get; set; }
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public int? CategoryId { get; set; }
    public double MinimumStock { get; set; }
    public double ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSellable { get; set; } = true;
    public bool IsPurchasable { get; set; } = true;

    // تحديث الباركود — مقيَّد بسياسة (Generated → Factory فقط)
    public string? Barcode { get; set; }
    public BarcodeFormat? BarcodeFormat { get; set; }
}

public class ProductReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public double StockQuantity { get; set; }
    public double MinimumStock { get; set; }
    public double ReorderLevel { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? Description { get; set; }
    public string? Brand { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool IsActive { get; set; }
    public bool IsSellable { get; set; }
    public bool IsPurchasable { get; set; }
    public string? ThumbnailUrl { get; set; }

    // بيانات الباركود
    public string Barcode { get; set; } = string.Empty;
    public string BarcodeType { get; set; } = string.Empty;
    public string BarcodeFormat { get; set; } = string.Empty;
}

public class ProductSummaryDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double CurrentStock { get; set; }
    public decimal CurrentCost { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime? LastMovementDate { get; set; }
    public string? CategoryName { get; set; }
    public string? Brand { get; set; }
}

/// <summary>
/// بيانات ملصق الطباعة — يُعاد من endpoint /label
/// </summary>
public class ProductLabelDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string BarcodeFormat { get; set; } = string.Empty; // "EAN13" أو "CODE128"
}

// ===== DTOs خاصة بالفاتورة =====

public class CreateInvoiceDto
{
    public int InvoiceType { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public int BranchId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public decimal Discount { get; set; }
    public decimal Paid { get; set; }
    public bool IsInstallment { get; set; }
    public string? Notes { get; set; }
    public List<CreateInvoiceItemDto> Items { get; set; } = [];
}

public class CreateInvoiceItemDto
{
    public int ProductId { get; set; }
    public double Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class InvoiceReadDto
{
    public int Id { get; set; }
    public string InvoiceType { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? SupplierName { get; set; }
    public string MerchantName { get; set; } = string.Empty; // اسم الشركة مالكة الدفتر
    public DateTime Date { get; set; }
    public decimal TotalValue { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal NetTotal => TotalValue - Discount + Tax;
    public decimal Paid { get; set; } // Legacy
    public decimal AllocatedAmount { get; set; }
    public decimal Remaining => NetTotal - AllocatedAmount;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public bool IsInstallment { get; set; }
    public List<InvoiceItemReadDto> Items { get; set; } = [];
}

public class InvoiceItemReadDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}

// ===== DTOs خاصة بالموظف =====

public class CreateEmployeeDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public string? Phone { get; set; }
    public EmployeeType Type { get; set; }
    // الفرع الذي سيُسجل الموظف فيه (اختياري - يأخذ الفرع الحالي)
    public int? CurrentBranchId { get; set; }
}

public class UpdateEmployeeDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public bool IsEnabled { get; set; }
    public string? Phone { get; set; }
    public EmployeeType Type { get; set; }
    public int? CurrentBranchId { get; set; }
}

public class EmployeeReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public bool IsEnabled { get; set; }
    public string? Phone { get; set; }
    public EmployeeType Type { get; set; }
    public int? CurrentBranchId { get; set; }
    public string? CurrentBranchName { get; set; }
}
// ===== DTOs خاصة بالعمليات المالية =====

/// <summary>
/// بيانات إنشاء معاملة نقدية جديدة (مصروف، قبض، دفع، إلخ)
/// </summary>
public class CreateCashTransactionDto
{
    public int Type { get; set; }           // TransactionType (1: In, 2: Out)
    public int SourceType { get; set; }     // TransactionSource (1: Customer, 2: Supplier, आदि)
    public decimal Value { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public int? RelatedEntityId { get; set; }
}

public class CashTransactionReadDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int? RelatedEntityId { get; set; }
    public string? RelatedEntityName { get; set; }
    public string MerchantName { get; set; } = string.Empty;
}
// ===== DTOs خاصة بلوحة التحكم (Dashboard) =====

/// <summary>
/// إحصائيات لوحة التحكم الأساسية
/// </summary>
public class DashboardStatsDto
{
    public decimal TodaySalesToal { get; set; }
    public decimal TodayExpensesTotal { get; set; }
    public double TodayInvoicesCount { get; set; }
    public decimal MonthlySalesTotal { get; set; }
    public decimal TotalCustomerDebts { get; set; }
    public decimal TotalSupplierDebts { get; set; }
    public List<TopProductDto> TopSellingProducts { get; set; } = [];
    public List<RecentInvoiceDto> RecentInvoices { get; set; } = [];
}

public class TopProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class FinancialSummaryDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit => TotalIncome - TotalExpenses;
}

public class DebtAlertDto
{
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class RecentInvoiceDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? PartnerName { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime Date { get; set; }
}
// ===== DTOs خاصة بالفروع (Branches) =====

public class CreateBranchDto
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateBranchDto
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

public class BranchReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

// ===== DTOs خاصة بالمخزون (Inventory / Stock Adjustments) =====

public class CreateStockAdjustmentDto
{
    public int ProductId { get; set; }
    public double Quantity { get; set; }
    public string Type { get; set; } = "In"; // (In: توريد/جرد، Out: صرف/توالف)
    public int? ReasonType { get; set; }
    public string? Notes { get; set; }
    public int BranchId { get; set; } // ضروري جداً
}

public class StockTransactionReadDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public double BeforeQuantity { get; set; }
    public double AfterQuantity { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public string? ReasonType { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
}

// ===== DTOs خاصة بالموارد البشرية (HR: Attendance & Payroll) =====

public class AttendanceCreateDto
{
    public int EmployeeId { get; set; }
    public AttendanceStatus Status { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public decimal WorkingHours { get; set; }
    public string? Notes { get; set; }
}

public class AttendanceReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal WorkingHours { get; set; }
    public string? Notes { get; set; }
}

public class PayrollRunReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal NetSalary { get; set; }
    public bool IsLocked { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class PayrollRunDetailsDto : PayrollRunReadDto
{
    public decimal TotalAllowances { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal LoanDeductions { get; set; }
    public List<PayrollRunItemReadDto> Items { get; set; } = [];
}

public class PayrollRunItemReadDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Category { get; set; }
}

public class CreateLoanDto
{
    public int EmployeeId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public int NumberOfMonths { get; set; }
    public DateTime StartDate { get; set; }
    public string? Notes { get; set; }
}

public class LoanReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class EmployeeStatusResult
{
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive => Status == "Active" || Status == "Present";
}

// ===== DTOs خاصة بمسؤول النظام (System Admin: Audit & Plugins) =====

public class AuditLogReadDto
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Changes { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string UserName { get; set; } = string.Empty;
}

public class PluginReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

// ===== DTOs خاصة بالشركة (Company) =====

// ===== DTOs خاصة بالشركة (Company) =====

public class CompanyCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? BusinessType { get; set; }
    public bool HasBranches { get; set; }
    public bool ManageInventory { get; set; }
    public List<CompanyPhoneNumberDto> PhoneNumbers { get; set; } = [];
}

public class CompanyUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? BusinessType { get; set; }
    public bool HasBranches { get; set; }
    public bool ManageInventory { get; set; }
    
    // بيانات إضافية للتحديث فقط
    public string? TaxId { get; set; }
    public string? CommercialRegistry { get; set; }
    public string? OfficialEmail { get; set; }
    public string? Website { get; set; }
    public int? Currency { get; set; }
    public string? Description { get; set; }
}

public class CompanyReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? Address { get; set; }
    public string? BusinessType { get; set; }
    public bool HasBranches { get; set; }
    public bool ManageInventory { get; set; }
    
    public string? TaxId { get; set; }
    public string? CommercialRegistry { get; set; }
    public string? OfficialEmail { get; set; }
    public string? Website { get; set; }
    public string? Currency { get; set; }
    public string? Description { get; set; }
    
    public List<CompanyPhoneNumberDto> PhoneNumbers { get; set; } = [];
    public CompanyLogoDto? Logo { get; set; }

    // حقول إضافية تُعاد فقط عند إنشاء شركة جديدة
    public string? OwnerUserName { get; set; }
    public string? OwnerTempPassword { get; set; }
    public int? MainBranchId { get; set; }
}

public class CompanyPhoneNumberDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsWhatsApp { get; set; }
}

public class CompanyLogoDto
{
    public byte[] Content { get; set; } = [];
    public string? ContentType { get; set; }
}

// ===== DTOs خاصة بالتسويات (Settlements) =====

public class CreateSettlementDto
{
    public int SourceType { get; set; }     // (1: عميل، 2: مورد)
    public int RelatedEntityId { get; set; }
    public int Type { get; set; }           // (1: إضافة، 2: خصم)
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
}

public class SettlementReadDto
{
    public int Id { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string? RelatedEntityName { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
}

// ===== DTOs خاصة بإدارة الأدوار والصلاحيات =====

/// <summary>DTO لإنشاء دور جديد</summary>
public class CreateRoleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>DTO لتعديل دور موجود</summary>
public class UpdateRoleDto
{
    public string? Description { get; set; }
}

/// <summary>DTO لعرض الدور</summary>
public class RoleReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = [];
    public int UsersCount { get; set; }
}

/// <summary>DTO لتحديث صلاحيات الدور</summary>
public class UpdateRolePermissionsDto
{
    public List<string> Permissions { get; set; } = [];
}

/// <summary>DTO لعرض بيانات مستخدم في إدارة المستخدمين</summary>
public class UserReadDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
}

/// <summary>DTO لإنشاء مستخدم جديد</summary>
public class CreateUserDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? BranchId { get; set; }
}

/// <summary>DTO لتعيين أدوار للمستخدم</summary>
public class AssignRolesDto
{
    public List<string> Roles { get; set; } = [];
}

/// <summary>DTO لبيانات المستخدم الحالي (GET /auth/me)</summary>
public class CurrentUserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
    public CompanyReadDto? Company { get; set; }
}

// ===== DTOs خاصة بالمقبوضات والمدفوعات (Finance Services) =====

public class CreateReceiptDto
{
    public int PartnerId { get; set; } // CustomerId أو SupplierId حسَب نوع الخدمة
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public PaymentMethod Method { get; set; }
    public string? Notes { get; set; }
    // إذا كانت true يتم عمل Auto-Allocate على أقدم الفواتير المفتوحة مباشرة
    public bool AutoAllocate { get; set; } = false;
}

public class ManualAllocationDto
{
    public int InvoiceId { get; set; }
    public decimal Amount { get; set; }
}

public class AllocateReceiptDto
{
    public int ReceiptId { get; set; }
    public List<ManualAllocationDto> Allocations { get; set; } = [];
}

public class ReceiptReadDto
{
    public int Id { get; set; }
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public decimal UnallocatedAmount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<AllocationReadDto> Allocations { get; set; } = [];
}

public class AllocationReadDto
{
    public int InvoiceId { get; set; }
    public decimal Amount { get; set; }
}

public class CustomerStatementDto
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public int DocumentId { get; set; }
    public decimal Debit { get; set; }   // عليه (مثلُا فاتورة مبيعات)
    public decimal Credit { get; set; }  // له (مثلاً سند قبض أو مرتجع)
    public decimal Balance { get; set; }
}
