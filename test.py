
import sys

file_path = r"c:\Users\ahmed\source\repos\DafterProApi\StoreManagement\StoreManagement.Infrastructure\Services\CompanyService.cs"

try:
    with open(file_path, "r", encoding="utf-8") as f:
        lines = f.readlines()

    lines[40] = "        if (!companyId.HasValue) throw new UnauthorizedAccessException(\"المستخدم غير مرتبط بشركة\");\n"
    lines[53] = "            ?? throw new KeyNotFoundException(\"الشركة غير موجودة\");\n"
    lines[91] = "        if (!companyId.HasValue) throw new UnauthorizedAccessException(\"المستخدم غير مرتبط بشركة\");\n"
    lines[96] = "            ?? throw new KeyNotFoundException(\"الشركة غير موجودة\");\n"
    lines[104] = "        // بيانات إضافية\n"
    lines[120] = "        // التحقق من حجم الملف (الحد الأقصى 2 ميجابايت)\n"
    lines[122] = "            throw new ArgumentException(\"حجم الصورة يتخطى الحد المسموح به (2 ميجابايت)\");\n"

    with open(file_path, "w", encoding="utf-8") as f:
        f.writelines(lines)
    print("Success")
except Exception as e:
    print("Error:", e)

