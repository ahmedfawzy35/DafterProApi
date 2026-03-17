using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Entities;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

/// <summary>
/// خدمة Outbox لحفظ Events المهمة داخل الـ Transaction الأصلي
/// </summary>
public class OutboxService : IOutboxService
{
    private readonly StoreDbContext _context;

    public OutboxService(StoreDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// حفظ Event في الـ Outbox ضمن نفس الـ Transaction
    /// </summary>
    public async Task PublishAsync(string eventType, object payload)
    {
        var message = new OutboxMessage
        {
            Type = eventType,
            Payload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }),
            CreatedDate = DateTime.UtcNow
        };

        _context.OutboxMessages.Add(message);
        // ملاحظة: لا يتم استدعاء SaveChanges هنا - يتم حفظه مع Transaction الأصلية
    }
}
