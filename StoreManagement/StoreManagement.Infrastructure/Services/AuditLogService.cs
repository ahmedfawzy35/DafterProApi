using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.HR;
using StoreManagement.Shared.Entities.Inventory;
using StoreManagement.Shared.Entities.Sales;
using StoreManagement.Shared.Entities.Finance;
using StoreManagement.Shared.Entities.Identity;
using StoreManagement.Shared.Entities.Partners;
using StoreManagement.Shared.Entities.Configuration;
using StoreManagement.Shared.Entities.Core;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AuditLogService(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<AuditLogReadDto>> GetAllAsync(
        PaginationQueryDto query, string? entityName = null, int? userId = null, string? entityId = null)
    {
        var logsQuery = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
            logsQuery = logsQuery.Where(l => l.EntityName == entityName);
            
        if (!string.IsNullOrWhiteSpace(entityId))
            logsQuery = logsQuery.Where(l => l.EntityId == entityId);
        
        if (userId.HasValue)
            logsQuery = logsQuery.Where(l => l.UserId == userId.Value);
        
        logsQuery = logsQuery.Where(l => l.CompanyId == (int)_currentUser.CompanyId!);

        if (!string.IsNullOrWhiteSpace(query.Search))
            logsQuery = logsQuery.Where(l => l.EntityName.Contains(query.Search) || (l.UserName != null && l.UserName.Contains(query.Search)));

        var total = await logsQuery.CountAsync();
        var items = await logsQuery
            .OrderByDescending(l => l.Timestamp)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => new AuditLogReadDto
            {
                Id = l.Id,
                EntityName = l.EntityName,
                Action = l.Action,
                Changes = $"Old: {l.OldValues} | New: {l.NewValues}",
                Timestamp = l.Timestamp,
                UserName = l.UserName ?? "System"
            })
            .ToListAsync();

        return new PagedResult<AuditLogReadDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }
}
