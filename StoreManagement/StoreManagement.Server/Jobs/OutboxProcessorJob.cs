using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;

namespace StoreManagement.Server.Jobs;

/// <summary>
/// Hangfire Job لمعالجة رسائل Outbox بشكل Batch مع Retry Policy
/// يتم جدولته كل دقيقة
/// </summary>
public class OutboxProcessorJob
{
    private readonly StoreDbContext _context;
    private readonly ILogger<OutboxProcessorJob> _logger;
    private const int MaxRetries = 3;
    private const int BatchSize = 50;

    public OutboxProcessorJob(StoreDbContext context, ILogger<OutboxProcessorJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// معالجة الرسائل غير المعالجة - يُستدعى من Hangfire
    /// </summary>
    public async Task ProcessAsync()
    {
        // جلب دفعة من الرسائل غير المعالجة مع مراعاة Retry Policy
        var messages = await _context.OutboxMessages
            .Where(m => !m.Processed && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedDate)
            .Take(BatchSize)
            .ToListAsync();

        if (!messages.Any())
            return;

        _logger.LogInformation("بدء معالجة {Count} رسائل Outbox", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                // معالجة الرسالة حسب نوعها
                await HandleMessageAsync(message.Type, message.Payload);

                message.Processed = true;
                message.ProcessedAt = DateTime.UtcNow;

                _logger.LogInformation("تمت معالجة رسالة Outbox: {Type} ({Id})", message.Type, message.Id);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;

                _logger.LogError(ex,
                    "فشل معالجة رسالة Outbox: {Type} ({Id}) - المحاولة {Attempt}/{Max}",
                    message.Type, message.Id, message.RetryCount, MaxRetries);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task HandleMessageAsync(string type, string payload)
    {
        // هنا يمكن إضافة معالجات لكل نوع من Events (Integration Events)
        switch (type)
        {
            case "InvoiceCreated":
                _logger.LogDebug("معالجة حدث InvoiceCreated: {Payload}", payload);
                // يمكن إرسال Notification أو تحديث Analytics هنا
                break;

            case "StockUpdated":
                _logger.LogDebug("معالجة حدث StockUpdated: {Payload}", payload);
                // يمكن إرسال تنبيه مخزون منخفض هنا
                break;

            default:
                _logger.LogWarning("نوع رسالة Outbox غير معروف: {Type}", type);
                break;
        }

        await Task.CompletedTask;
    }
}
