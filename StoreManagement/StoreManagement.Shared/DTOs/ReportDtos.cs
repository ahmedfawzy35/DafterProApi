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
    public decimal Total { get; set; }
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
    public int OpenCustomerInvoices { get; set; }
}

