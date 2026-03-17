namespace StoreManagement.Server.Middleware;

/// <summary>
/// Middleware لإضافة CorrelationId لكل Request
/// يُنشئ CorrelationId جديد إذا لم يكن موجوداً في الـ Header
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // استخراج أو إنشاء CorrelationId
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        // إضافته في HttpContext.Items
        context.Items["CorrelationId"] = correlationId;

        // إضافته في Response Headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // إضافته في Serilog LogContext لكل logs في هذا الـ Request
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        using (Serilog.Context.LogContext.PushProperty("RequestPath", context.Request.Path))
            await _next(context);
    }
}
