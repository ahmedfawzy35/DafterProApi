namespace StoreManagement.Shared.Common;

/// <summary>
/// نموذج الاستجابة الموحدة لجميع نقاط النهاية في الـ API
/// </summary>
public class ApiResponse<T>
{
    // حالة النجاح
    public bool Success { get; set; }

    // الرسالة الوصفية
    public string Message { get; set; } = string.Empty;

    // البيانات المُرجعة
    public T? Data { get; set; }

    // قائمة الأخطاء (إن وجدت)
    public List<string> Errors { get; set; } = [];

    // استجابة نجاح مع بيانات
    public static ApiResponse<T> SuccessResult(T data, string message = "تمت العملية بنجاح")
        => new() { Success = true, Message = message, Data = data };

    // استجابة نجاح بدون بيانات
    public static ApiResponse<T> SuccessResult(string message = "تمت العملية بنجاح")
        => new() { Success = true, Message = message };

    // استجابة فشل مع قائمة أخطاء
    public static ApiResponse<T> Failure(string message, List<string>? errors = null)
        => new() { Success = false, Message = message, Errors = errors ?? [] };

    // استجابة فشل بخطأ واحد
    public static ApiResponse<T> Failure(string message, string error)
        => new() { Success = false, Message = message, Errors = [error] };
}
