using System;

namespace StoreManagement.Shared.DTOs;

/// <summary>
/// صف واحد في تقرير أعمار الديون (Aging Report)
/// </summary>
public class AgingReportRowDto
{
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public string? PartnerCode { get; set; }
    public string? PrimaryPhone { get; set; }
    
    // الفترات الزمنية للمبالغ المستحقة
    public decimal Current { get; set; }     // 0-30 يوم
    public decimal Days31_60 { get; set; }   // 31-60 يوم
    public decimal Days61_90 { get; set; }   // 61-90 يوم
    public decimal Over90 { get; set; }      // أكثر من 90 يوم
    
    // إجمالي الدين المتبقي
    public decimal Total => Current + Days31_60 + Days61_90 + Over90;
    
    // الرصيد غير المخصص
    public decimal UnallocatedCredit { get; set; }
    
    // صافي الرصيد
    public decimal NetBalance => Total - UnallocatedCredit;
}

/// <summary>
/// تقرير ملخص المبيعات والأرباح خلال فترة
/// </summary>
public class SalesSummaryDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    
    public int InvoiceCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal NetRevenue { get; set; }
    
    // الأرباح - تُحسب من CostPriceAtSale
    public decimal TotalCost { get; set; }  
    public decimal GrossProfit { get; set; } 
    public decimal GrossMargin { get; set; } // نسبة مئوية
    
    public decimal AverageInvoiceValue => InvoiceCount > 0 ? NetRevenue / InvoiceCount : 0;
}

/// <summary>
/// تقرير ربحية الفاتورة الواحدة
/// </summary>
public class InvoiceProfitDto
{
    public int InvoiceId { get; set; }
    public DateTime Date { get; set; }
    public string? CustomerName { get; set; }
    
    public decimal TotalSales { get; set; }
    public decimal TotalCost { get; set; }
    public decimal Profit { get; set; }
    public decimal ProfitMargin { get; set; } // كنسبة مئوية
}

/// <summary>
/// مؤشرات الأداء الرئيسية للوحة التحكم الحديثة
/// </summary>
public class DashboardKpiDto
{
    // المبيعات
    public decimal TodaySales { get; set; }
    public decimal MonthSales { get; set; }
    public decimal MonthSalesVsPrevious { get; set; } // النسبة المئوية للمقارنة بالشهر السابق
    
    // الأرباح
    public decimal MonthProfit { get; set; }
    public decimal MonthMargin { get; set; }

    // الذمم من النظام المالي (Finance)
    public decimal TotalReceivables { get; set; } // ديون للشركة عند العملاء
    public decimal TotalPayables { get; set; }    // ديون على الشركة للموردين
    
    // التنبيهات
    public int LowStockItemsCount { get; set; }
}

// ===== تقارير المخزون (Inventory Reports) =====

public class StockPerBranchReportDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
}

public class BranchInventoryMovementReportDto
{
    public DateTime Date { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public StoreManagement.Shared.Enums.StockMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal BeforeQuantity { get; set; }
    public decimal AfterQuantity { get; set; }
    public int? ReferenceId { get; set; }
}

public class ProductStockDistributionDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public List<BranchStockAllocationDto> Branches { get; set; } = new();
}

public class BranchStockAllocationDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

// ===== لوحة التحكم الفرعية (Branch Dashboard) =====

public class BranchDashboardKpiDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int LowStockItemsCount { get; set; }
    public decimal TotalStockQuantity { get; set; }
    public int RecentMovementsCount { get; set; } 
    public List<LowStockAlertDto> TopLowStockItems { get; set; } = new();
    
    // يظهر للأدمن فقط أو المالك لمعرفة إجمالي توزيع المخزون
    public List<BranchStockSummaryDto> StockDistribution { get; set; } = new();
}

public class BranchStockSummaryDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
}
