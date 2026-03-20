using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Services.Services;

/// <summary>
/// خدمة الباركود — توليد EAN-13 حتمي، تحقق، اكتشاف الصيغة
/// </summary>
public class BarcodeService : IBarcodeService
{
    // Prefix داخلي (200–299 محجوز للأنظمة الداخلية في GS1)
    private const int InternalPrefix = 200;

    /// <inheritdoc/>
    public string GenerateEan13(int companyId, long sequence)
    {
        // هيكل: 200 (3 أرقام) + companyId (4 أرقام) + sequence (5 أرقام) = 12 رقم + checksum
        var prefixStr = InternalPrefix.ToString("D3");       // "200"
        var companyStr = (companyId % 10000).ToString("D4"); // "0001"
        var seqStr = (sequence % 100000).ToString("D5");     // "00042"

        var twelveDigits = $"{prefixStr}{companyStr}{seqStr}";
        var checkDigit = CalculateEan13Checksum(twelveDigits);

        return $"{twelveDigits}{checkDigit}";
    }

    /// <inheritdoc/>
    public int CalculateEan13Checksum(string twelveDigits)
    {
        if (twelveDigits.Length != 12 || !twelveDigits.All(char.IsDigit))
            throw new ArgumentException("يجب أن تتكون السلسلة من 12 رقم بالضبط.", nameof(twelveDigits));

        // خوارزمية EAN-13 Checksum (Modulo 10 - أوزان 1 و 3):
        // مجموع (الرقم في المكان الفردي × 1) + (الرقم في المكان الزوجي × 3)
        // ملاحظة: البرمجة تبدأ من الـ index 0، لذا index 0 و 2 إلخ هي الأماكن الفردية (وزن 1)
        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var digit = twelveDigits[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }

        return (10 - (sum % 10)) % 10;
    }

    /// <inheritdoc/>
    public bool ValidateEan13(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode) || barcode.Length != 13)
            return false;

        if (!barcode.All(char.IsDigit))
            return false;

        var expectedCheck = CalculateEan13Checksum(barcode[..12]);
        return (barcode[12] - '0') == expectedCheck;
    }

    /// <inheritdoc/>
    public BarcodeFormat DetectFormat(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return BarcodeFormat.CODE128;

        // EAN-13: 13 رقم بالضبط وكلها أرقام مع تحقق فعلي من الـ Checksum
        if (barcode.Length == 13 && barcode.All(char.IsDigit) && ValidateEan13(barcode))
            return BarcodeFormat.EAN13;

        // بخلاف ذلك نعتبره CODE128 كوضع افتراضي داخلي (حتى لو كان مجرد نص)
        return BarcodeFormat.CODE128;
    }
}
