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

public class BranchService : IBranchService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public BranchService(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<BranchReadDto>> GetAllAsync()
    {
        return await _context.Branches
            .Where(b => b.CompanyId == (int)_currentUser.CompanyId!)
            .Select(b => new BranchReadDto
            {
                Id = b.Id,
                Name = b.Name
            })
            .ToListAsync();
    }

    public async Task<BranchReadDto?> GetByIdAsync(int id)
    {
        var b = await _context.Branches
            .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == (int)_currentUser.CompanyId!);

        if (b == null) return null;

        return new BranchReadDto
        {
            Id = b.Id,
            Name = b.Name
        };
    }

    public async Task<BranchReadDto> CreateAsync(CreateBranchDto dto)
    {
        var branch = new Branch
        {
            Name = dto.Name,
            CompanyId = (int)_currentUser.CompanyId!
        };

        _context.Branches.Add(branch);
        await _context.SaveChangesAsync();

        return new BranchReadDto
        {
            Id = branch.Id,
            Name = branch.Name
        };
    }

    public async Task UpdateAsync(int id, UpdateBranchDto dto)
    {
        var branch = await _context.Branches
            .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == (int)_currentUser.CompanyId!)
            ?? throw new KeyNotFoundException($"الفرع رقم {id} غير موجود");

        branch.Name = dto.Name;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var branch = await _context.Branches
            .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == (int)_currentUser.CompanyId!)
            ?? throw new KeyNotFoundException($"الفرع رقم {id} غير موجود");

        // منع حذف الفرع إذا كان مرتبطاً بمستخدمين
        var hasUsers = await _context.Users.AnyAsync(u => u.BranchId == id);
        if (hasUsers)
            throw new InvalidOperationException("لا يمكن حذف الفرع لأنه مرتبط بمستخدمين");

        _context.Branches.Remove(branch);
        await _context.SaveChangesAsync();
    }

    public async Task<string> GetBranchStatusAsync(int id)
    {
        var branch = await _context.Branches
            .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == (int)_currentUser.CompanyId!)
            ?? throw new KeyNotFoundException($"الفرع رقم {id} غير موجود");

        var usersCount = await _context.Users.CountAsync(u => u.BranchId == id);
        return $"الفرع {branch.Name} يحتوي على {usersCount} مستخدمين نشطين.";
    }
}
