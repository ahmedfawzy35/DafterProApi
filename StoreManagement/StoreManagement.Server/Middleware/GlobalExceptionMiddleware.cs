using System.Net;
using System.Text.Json;
using StoreManagement.Shared.Common;

namespace StoreManagement.Server.Middleware;

/// <summary>
/// Middleware للتعامل العالمي مع الاستثناءات وإرجاع استجابة موحدة
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // تسجيل الخطأ
            _logger.LogError(ex, "خطأ غير معالج أثناء معالجة الطلب: {Path}", context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "غير مصرح بالوصول"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "العنصر المطلوب غير موجود"),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            _ => (HttpStatusCode.InternalServerError, "حدث خطأ داخلي في الخادم")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.Failure(message);
        
#if DEBUG
        response.Errors.Add(exception.Message);
        if (exception.StackTrace != null)
            response.Errors.Add(exception.StackTrace);
#endif

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
