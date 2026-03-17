using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StoreManagement.Shared.Interfaces;
using StoreManagement.Shared.Settings;

namespace StoreManagement.Infrastructure.Services;

/// <summary>
/// خدمة تخزين الملفات محلياً بهيكل منظم (CompanyId/Entity/Date)
/// قابلة للتبديل بـ S3 أو Azure Blob لاحقاً
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly StorageSettings _settings;
    private readonly ILogger<LocalFileStorageService> _logger;

    // امتدادات محظورة لأسباب أمنية
    private static readonly string[] _blockedExtensions =
        [".exe", ".bat", ".cmd", ".sh", ".ps1", ".dll", ".msi", ".vbs"];

    public LocalFileStorageService(
        IOptions<StorageSettings> settings,
        ILogger<LocalFileStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> SaveFileAsync(
        Stream fileStream,
        string fileName,
        string entityFolder,
        int companyId)
    {
        // تنظيم مجلد الحفظ: uploads/{companyId}/{entity}/{yyyy-MM}
        var monthFolder = DateTime.UtcNow.ToString("yyyy-MM");
        var relativePath = Path.Combine(
            _settings.LocalStoragePath,
            companyId.ToString(),
            entityFolder,
            monthFolder);

        var fullPath = Path.GetFullPath(relativePath);
        Directory.CreateDirectory(fullPath);

        // اسم ملف فريد لمنع التعارضات
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(fullPath, uniqueFileName);

        await using var fileStreamOutput = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(fileStreamOutput);

        _logger.LogInformation("تم حفظ ملف: {FileName} للشركة: {CompanyId}", uniqueFileName, companyId);

        // إرجاع المسار النسبي للحفظ في قاعدة البيانات
        return Path.Combine(companyId.ToString(), entityFolder, monthFolder, uniqueFileName)
                   .Replace("\\", "/");
    }

    public async Task DeleteFileAsync(string filePath)
    {
        var fullPath = Path.Combine(_settings.LocalStoragePath, filePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("تم حذف ملف: {FilePath}", filePath);
        }

        await Task.CompletedTask;
    }

    public (bool IsValid, string? Error) ValidateFile(string fileName, long fileSizeBytes)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        // التحقق من الامتدادات المحظورة
        if (_blockedExtensions.Contains(extension))
            return (false, $"امتداد الملف '{extension}' غير مسموح به");

        // التحقق من الامتدادات المسموحة
        if (_settings.AllowedExtensions.Any() &&
            !_settings.AllowedExtensions.Contains(extension))
            return (false, $"الامتداد المسموح به: {string.Join(", ", _settings.AllowedExtensions)}");

        // التحقق من الحجم الأقصى
        var maxSizeBytes = _settings.MaxFileSizeMB * 1024 * 1024;
        if (fileSizeBytes > maxSizeBytes)
            return (false, $"حجم الملف يتجاوز الحد المسموح ({_settings.MaxFileSizeMB} MB)");

        return (true, null);
    }
}
