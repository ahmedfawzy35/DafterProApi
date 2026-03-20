using StoreManagement.Shared.Enums;

namespace StoreManagement.Shared.Interfaces;

/// <summary>
/// واجهة خدمة الباركود — مسؤولة عن التوليد والتحقق واكتشاف الصيغة
/// </summary>
public interface IBarcodeService
{
    /// <summary>
    /// يولّد باركود EAN-13 حتمي (deterministic) بناءً على CompanyId وتسلسل المنتج.
    /// الهيكل: 200 (Prefix داخلي) + CompanyId (4 أرقام) + Sequence (5 أرقام) + Checksum (1 رقم)
    /// </summary>
    string GenerateEan13(int companyId, long sequence);

    /// <summary>
    /// يحسب رقم التحقق (Check Digit) لـ EAN-13 باستخدام خوارزمية Modulo 10 (أوزان 1 و 3)
    /// </summary>
    int CalculateEan13Checksum(string twelveDigits);

    /// <summary>
    /// يتحقق من صحة باركود EAN-13 (13 رقم مع checksum صحيح)
    /// </summary>
    bool ValidateEan13(string barcode);

    /// <summary>
    /// يكتشف صيغة الباركود تلقائياً بناءً على محتواه وطوله
    /// </summary>
    BarcodeFormat DetectFormat(string barcode);
}
