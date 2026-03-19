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
    public double CashBalance { get; set; }
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
    public double CashBalance { get; set; }
    public List<string> Phones { get; set; } = [];
    public DateTime CreatedDate { get; set; }
}

// ===== DTOs خاصة بالمنتج =====

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
    public double CostPrice { get; set; }
    public string Unit { get; set; } = "قطعة";
}

public class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
    public double CostPrice { get; set; }
    public string Unit { get; set; } = "قطعة";
}

public class ProductReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
    public double CostPrice { get; set; }
    public double StockQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
}

// ===== DTOs خاصة بالفاتورة =====

public class CreateInvoiceDto
{
    public int InvoiceType { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public double Discount { get; set; }
    public double Paid { get; set; }
    public bool IsInstallment { get; set; }
    public string? Notes { get; set; }
    public List<CreateInvoiceItemDto> Items { get; set; } = [];
}

public class CreateInvoiceItemDto
{
    public int ProductId { get; set; }
    public double Quantity { get; set; }
    public double UnitPrice { get; set; }
}

public class InvoiceReadDto
{
    public int Id { get; set; }
    public string InvoiceType { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? SupplierName { get; set; }
    public string MerchantName { get; set; } = string.Empty; // اسم الشركة مالكة الدفتر
    public DateTime Date { get; set; }
    public double TotalValue { get; set; }
    public double Discount { get; set; }
    public double Paid { get; set; }
    public double Remaining => TotalValue - Discount - Paid;
    public bool IsInstallment { get; set; }
    public List<InvoiceItemReadDto> Items { get; set; } = [];
}

public class InvoiceItemReadDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public double UnitPrice { get; set; }
    public double Subtotal { get; set; }
}

// ===== DTOs خاصة بالموظف =====

public class CreateEmployeeDto
{
    public string Name { get; set; } = string.Empty;
    public double Salary { get; set; }
    public double Allowances { get; set; }
    public double Deductions { get; set; }
    public string? Phone { get; set; }
}

public class UpdateEmployeeDto
{
    public string Name { get; set; } = string.Empty;
    public double Salary { get; set; }
    public double Allowances { get; set; }
    public double Deductions { get; set; }
    public bool IsEnabled { get; set; }
    public string? Phone { get; set; }
}

public class EmployeeReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Salary { get; set; }
    public double Allowances { get; set; }
    public double Deductions { get; set; }
    public bool IsEnabled { get; set; }
    public string? Phone { get; set; }
}
// ===== DTOs خاصة بالعمليات المالية =====

/// <summary>
/// بيانات إنشاء معاملة نقدية جديدة (مصروف، قبض، دفع، إلخ)
/// </summary>
public class CreateCashTransactionDto
{
    public int Type { get; set; }           // (1: وارد، 2: صادر)
    public int SourceType { get; set; }     // (1: عميل، 2: مورد، 3: بنك، 4: راتب، 5: مصروف، 6: أخرى)
    public double Value { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public int? RelatedEntityId { get; set; } // (معرف العميل أو المورد إذا وجد)
}

/// <summary>
/// بيانات عرض المعاملة النقدية
/// </summary>
public class CashTransactionReadDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int? RelatedEntityId { get; set; }
    public string? RelatedEntityName { get; set; } // اسم العميل أو المورد
    public string MerchantName { get; set; } = string.Empty;
}
// ===== DTOs خاصة بلوحة التحكم (Dashboard) =====

/// <summary>
/// إحصائيات لوحة التحكم الأساسية
/// </summary>
public class DashboardStatsDto
{
    public double TodaySalesToal { get; set; }           // إجمالي مبيعات اليوم
    public double TodayExpensesTotal { get; set; }        // إجمالي مصروفات اليوم
    public double TodayInvoicesCount { get; set; }        // عدد فواتير اليوم
    public double MonthlySalesTotal { get; set; }          // إجمالي مبيعات الشهر
    public double TotalCustomerDebts { get; set; }        // إجمالي ديون العملاء (لنا)
    public double TotalSupplierDebts { get; set; }        // إجمالي ديون الموردين (علينا)
    public List<TopProductDto> TopSellingProducts { get; set; } = [];
    public List<RecentInvoiceDto> RecentInvoices { get; set; } = [];
}

public class TopProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double QuantitySold { get; set; }
    public double Revenue { get; set; }
}

public class FinancialSummaryDto
{
    public double TotalIncome { get; set; }
    public double TotalExpenses { get; set; }
    public double NetProfit => TotalIncome - TotalExpenses;
}

public class DebtAlertDto
{
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string Type { get; set; } = string.Empty; // Customer/Supplier
}

public class RecentInvoiceDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? PartnerName { get; set; }
    public double TotalValue { get; set; }
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
}

public class BranchReadDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ===== DTOs خاصة بالمخزون (Inventory / Stock Adjustments) =====

public class CreateStockAdjustmentDto
{
    public int ProductId { get; set; }
    public double Quantity { get; set; }
    public string Type { get; set; } = "In"; // (In: توريد/جرد، Out: صرف/توالف)
    public string? Notes { get; set; }
}

public class StockTransactionReadDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
}

// ===== DTOs خاصة بالموارد البشرية (HR: Attendance & Payroll) =====

public class AttendanceRecordDto
{
    public int EmployeeId { get; set; }
    public int Status { get; set; } // (1: حاضر، 2: غائب، 3: متأخر، 4: إجازة)
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}

public class AttendanceReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}

public class PayrollReadDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime Month { get; set; }
    public double Salary { get; set; }
    public double Allowances { get; set; }
    public double Deductions { get; set; }
    public double NetSalary => Salary + Allowances - Deductions;
    public bool IsPaid { get; set; }
    public DateTime? PaymentDate { get; set; }
}

public class CreatePayrollDto
{
    public int EmployeeId { get; set; }
    public DateTime Month { get; set; }
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
    public double Amount { get; set; }
    public string? Notes { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
}

public class SettlementReadDto
{
    public int Id { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string? RelatedEntityName { get; set; }
    public string Type { get; set; } = string.Empty;
    public double Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
}
