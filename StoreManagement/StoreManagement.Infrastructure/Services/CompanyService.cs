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
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace StoreManagement.Infrastructure.Services;

public class CompanyService : ICompanyService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly UserManager<User> _userManager;

    public CompanyService(StoreDbContext context, ICurrentUserService currentUser, UserManager<User> userManager)
    {
        _context = context;
        _currentUser = currentUser;
        _userManager = userManager;
    }

    public async Task<List<CompanyReadDto>> GetAllAsync()
    {
        var companies = await _context.Companies
            .Include(c => c.PhoneNumbers)
            .Include(c => c.Logo)
            .ToListAsync();

        return companies.Select(MapToDto).ToList();
    }

    public async Task<CompanyReadDto?> GetMyCompanyAsync(bool includeLogo = false)
    {
        var companyId = _currentUser.CompanyId;
        if (!companyId.HasValue) return null;

        var query = _context.Companies
            .Include(c => c.PhoneNumbers)
            .AsQueryable();

        if (includeLogo)
        {
            query = query.Include(c => c.Logo);
        }

        var company = await query
            .FirstOrDefaultAsync(c => c.Id == (int)companyId);

        if (company == null) return null;

        return MapToDto(company);
    }

    public async Task<CompanyReadDto> CreateAsync(CompanyCreateDto dto)
    {
        var company = new Company
        {
            Name = dto.Name,
            Address = dto.Address,
            BusinessType = dto.BusinessType,
            HasBranches = dto.HasBranches,
            ManageInventory = dto.ManageInventory,
            CreatedDate = DateTime.UtcNow
        };

        if (dto.PhoneNumbers != null)
        {
            foreach (var phone in dto.PhoneNumbers)
            {
                company.PhoneNumbers.Add(new CompanyPhoneNumber
                {
                    PhoneNumber = phone.PhoneNumber,
                    IsWhatsApp = phone.IsWhatsApp
                });
            }
        }

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        return MapToDto(company);
    }

    public async Task UpdateMyCompanyAsync(CompanyUpdateDto dto)
    {
        var companyId = _currentUser.CompanyId;
        if (!companyId.HasValue) throw new UnauthorizedAccessException("المستخدم غير مرتبط بشركة");

        var company = await _context.Companies
            .Include(c => c.PhoneNumbers)
            .FirstOrDefaultAsync(c => c.Id == (int)companyId)
            ?? throw new KeyNotFoundException("الشركة غير موجودة");

        company.Name = dto.Name;
        company.Address = dto.Address;
        company.BusinessType = dto.BusinessType;
        company.HasBranches = dto.HasBranches;
        company.ManageInventory = dto.ManageInventory;
        
        // بيانات إضافية
        company.TaxId = dto.TaxId;
        company.CommercialRegistry = dto.CommercialRegistry;
        company.OfficialEmail = dto.OfficialEmail;
        company.Website = dto.Website;
        company.Currency = dto.Currency.HasValue ? (Currency)dto.Currency.Value : null;
        company.Description = dto.Description;

        await _context.SaveChangesAsync();
    }

    public async Task UploadLogoAsync(byte[] logoContent, string contentType)
    {
        var companyId = _currentUser.CompanyId;
        if (!companyId.HasValue) throw new UnauthorizedAccessException("المستخدم غير مرتبط بشركة");

        // التحقق من حجم الملف (الحد الأقصى 2 ميجابايت)
        if (logoContent.Length > 2 * 1024 * 1024)
            throw new ArgumentException("حجم الصورة يتخطى الحد المسموح به (2 ميجابايت)");

        var logo = await _context.CompanyLogos
            .FirstOrDefaultAsync(l => l.CompanyId == (int)companyId);

        if (logo == null)
        {
            logo = new CompanyLogo { CompanyId = (int)companyId };
            _context.CompanyLogos.Add(logo);
        }

        logo.Content = logoContent;
        logo.ContentType = contentType;
        logo.FileSize = logoContent.Length;

        await _context.SaveChangesAsync();
    }

    public async Task<List<UserReadDto>> GetCompanyUsersAsync(int companyId)
    {
        var users = await _userManager.Users
            .Where(u => u.CompanyId == companyId)
            .ToListAsync();

        var result = new List<UserReadDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            result.Add(new UserReadDto
            {
                Id = u.Id,
                Email = u.Email ?? "",
                UserName = u.UserName ?? "",
                Roles = roles.ToList()
            });
        }

        return result;
    }

    private static CompanyReadDto MapToDto(Company company)
    {
        return new CompanyReadDto
        {
            Id = company.Id,
            Name = company.Name,
            Address = company.Address,
            BusinessType = company.BusinessType,
            HasBranches = company.HasBranches,
            ManageInventory = company.ManageInventory,
            TaxId = company.TaxId,
            CommercialRegistry = company.CommercialRegistry,
            OfficialEmail = company.OfficialEmail,
            Website = company.Website,
            Currency = company.Currency?.ToString(),
            Description = company.Description,
            PhoneNumbers = company.PhoneNumbers.Select(p => new CompanyPhoneNumberDto
            {
                PhoneNumber = p.PhoneNumber,
                IsWhatsApp = p.IsWhatsApp
            }).ToList(),
            Logo = company.Logo != null ? new CompanyLogoDto
            {
                Content = company.Logo.Content,
                ContentType = company.Logo.ContentType
            } : null
        };
    }
}
