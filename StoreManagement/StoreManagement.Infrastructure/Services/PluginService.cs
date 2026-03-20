using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
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

public class PluginService : IPluginService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public PluginService(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<PluginReadDto>> GetAllAsync()
    {
        return await _context.Plugins
            .Where(p => p.CompanyId == (int)_currentUser.CompanyId!)
            .Select(p => new PluginReadDto
            {
                Id = p.Id,
                Name = p.Name,
                DisplayName = p.DisplayName,
                IsEnabled = p.IsEnabled
            })
            .ToListAsync();
    }

    public async Task TogglePluginAsync(int pluginId, bool enabled)
    {
        var plugin = await _context.Plugins
            .FirstOrDefaultAsync(p => p.Id == pluginId && p.CompanyId == (int)_currentUser.CompanyId!)
            ?? throw new KeyNotFoundException($"الإضافة رقم {pluginId} غير موجودة");

        plugin.IsEnabled = enabled;
        if (enabled) plugin.EnabledAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}
