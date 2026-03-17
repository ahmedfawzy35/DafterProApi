using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

/// <summary>
/// طبقة Business Logic للعملاء - يُحقن في Controllers
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CustomerService(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<CustomerReadDto>> GetAllAsync(PaginationQueryDto query)
    {
        var baseQuery = _context.Customers
            .Include(c => c.Phones)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(c => c.Name.Contains(query.Search));

        var total = await baseQuery.CountAsync();

        var items = await baseQuery
            .OrderBy(c => c.Name)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new CustomerReadDto
            {
                Id = c.Id, Name = c.Name, CashBalance = c.CashBalance,
                Phones = c.Phones.Select(p => p.PhoneNumber).ToList(),
                CreatedDate = c.CreatedDate
            })
            .ToListAsync();

        return new PagedResult<CustomerReadDto>
        {
            Items = items, PageNumber = query.PageNumber,
            PageSize = query.PageSize, TotalCount = total
        };
    }

    public async Task<CustomerReadDto?> GetByIdAsync(int id)
    {
        var customer = await _context.Customers
            .Include(c => c.Phones)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer is null) return null;

        return new CustomerReadDto
        {
            Id = customer.Id, Name = customer.Name,
            CashBalance = customer.CashBalance,
            Phones = customer.Phones.Select(p => p.PhoneNumber).ToList(),
            CreatedDate = customer.CreatedDate
        };
    }

    public async Task<CustomerReadDto> CreateAsync(CreateCustomerDto dto)
    {
        var customer = new Customer
        {
            Name = dto.Name,
            CompanyId = _currentUser.CompanyId
        };

        foreach (var phone in dto.Phones)
            customer.Phones.Add(new CustomerPhone { PhoneNumber = phone });

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return new CustomerReadDto
        {
            Id = customer.Id, Name = customer.Name,
            CashBalance = customer.CashBalance, Phones = dto.Phones,
            CreatedDate = customer.CreatedDate
        };
    }

    public async Task UpdateAsync(int id, UpdateCustomerDto dto)
    {
        var customer = await _context.Customers
            .Include(c => c.Phones)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException($"العميل رقم {id} غير موجود");

        customer.Name = dto.Name;
        _context.CustomerPhones.RemoveRange(customer.Phones);
        foreach (var phone in dto.Phones)
            customer.Phones.Add(new CustomerPhone { PhoneNumber = phone, CustomerId = id });

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException($"العميل رقم {id} غير موجود");

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
    }
}
