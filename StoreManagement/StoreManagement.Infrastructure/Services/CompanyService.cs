using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

public class CompanyService : ICompanyService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CompanyService(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<CompanyReadDto> GetMyCompanyAsync()
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == (int)_currentUser.CompanyId!)
            ?? throw new KeyNotFoundException("الشركة غير موجودة");

        return new CompanyReadDto
        {
            Id = company.Id,
            Name = company.Name,
            HasBranches = company.HasBranches,
            ManageInventory = company.ManageInventory
        };
    }

    public async Task UpdateMyCompanyAsync(CompanyUpdateDto dto)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == (int)_currentUser.CompanyId!)
            ?? throw new KeyNotFoundException("الشركة غير موجودة");

        company.Name = dto.Name;
        company.HasBranches = dto.HasBranches;
        company.ManageInventory = dto.ManageInventory;

        await _context.SaveChangesAsync();
    }
}
