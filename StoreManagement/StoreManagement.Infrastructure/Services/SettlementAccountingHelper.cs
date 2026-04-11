using StoreManagement.Shared.Entities.Finance;
using StoreManagement.Shared.Enums;

namespace StoreManagement.Infrastructure.Services;

/// <summary>
/// Centralized mapping logic for Settlement rules.
/// Statement Debit/Credit columns are presented from the partner-ledger view (current UI design),
/// not from general ledger journal-entry terminology.
/// </summary>
public static class SettlementAccountingHelper
{
    private static string GetDescription(string baseText, string? notes)
    {
        return string.IsNullOrWhiteSpace(notes) ? baseText : $"{baseText} - {notes}";
    }

    /// <summary>
    /// Maps a customer settlement to its financial effects.
    /// Positive BalanceEffect = Increases Customer Debt.
    /// Negative BalanceEffect = Decreases Customer Debt.
    /// </summary>
    public static (decimal DisplayDebit, decimal DisplayCredit, decimal BalanceEffect, string Description) MapCustomerSettlement(AccountSettlement s)
    {
        if (s.Type == SettlementType.Add)
        {
            // Add means Settlement increases what Customer owes us.
            // In UI partner-ledger view, Increasing Debt is a Debit.
            string baseDesc = s.Reason switch
            {
                SettlementReason.Discount => "إلغاء خصم مسموح به", // rare
                SettlementReason.Commission => "تسوية عمولات (زيادة دين)",
                SettlementReason.Error => "تصحيح خطأ (زيادة دين)",
                SettlementReason.Other => "تسوية زيادة رصيد",
                _ => "تسوية زيادة رصيد"
            };
            return (s.Amount, 0m, s.Amount, GetDescription(baseDesc, s.Notes));
        }
        else // SettlementType.Subtract
        {
            // Subtract means Settlement decreases what Customer owes us.
            // In UI partner-ledger view, Decreasing Debt is a Credit (like a receipt).
            string baseDesc = s.Reason switch
            {
                SettlementReason.Discount => "تسوية خصم مسموح به",
                SettlementReason.WriteOff => "تسوية إعدام دين",
                SettlementReason.Error => "تصحيح خطأ (خفض دين)",
                SettlementReason.Commission => "تسوية عمولات (خفض دين)",
                SettlementReason.Other => "تسوية خفض رصيد",
                _ => "تسوية خفض رصيد"
            };
            return (0m, s.Amount, -s.Amount, GetDescription(baseDesc, s.Notes));
        }
    }

    /// <summary>
    /// Maps a supplier settlement to its financial effects.
    /// Positive BalanceEffect = Increases What we owe to Supplier.
    /// Negative BalanceEffect = Decreases What we owe to Supplier.
    /// </summary>
    public static (decimal DisplayDebit, decimal DisplayCredit, decimal BalanceEffect, string Description) MapSupplierSettlement(AccountSettlement s)
    {
        if (s.Type == SettlementType.Add)
        {
            // Add means Settlement increases what we owe the Supplier.
            // In UI partner-ledger view, Increasing Liability is a Credit.
            string baseDesc = s.Reason switch
            {
                SettlementReason.Discount => "إلغاء خصم مكتسب", // rare
                SettlementReason.Error => "تصحيح خطأ (زيادة التزام)",
                SettlementReason.Commission => "تسوية عمولات (زيادة التزام)",
                SettlementReason.Other => "تسوية زيادة رصيد للمورد",
                _ => "تسوية زيادة رصيد للمورد"
            };
            return (0m, s.Amount, s.Amount, GetDescription(baseDesc, s.Notes));
        }
        else // SettlementType.Subtract
        {
            // Subtract means Settlement decreases what we owe the Supplier.
            // In UI partner-ledger view, Decreasing Liability is a Debit.
            string baseDesc = s.Reason switch
            {
                SettlementReason.Discount => "تسوية خصم مكتسب",
                SettlementReason.WriteOff => "تسوية إعفاء/إسقاط دين",
                SettlementReason.Error => "تصحيح خطأ (خفض التزام)",
                SettlementReason.Commission => "تسوية عمولات (خفض التزام)",
                SettlementReason.Other => "تسوية خفض رصيد للمورد",
                _ => "تسوية خفض رصيد للمورد"
            };
            return (s.Amount, 0m, -s.Amount, GetDescription(baseDesc, s.Notes));
        }
    }
}
