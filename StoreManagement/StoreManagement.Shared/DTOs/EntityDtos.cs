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
