using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
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

namespace StoreManagement.Infrastructure.Services;

public class PolicyService : IPolicyService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public PolicyService(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<string> GetPolicyValueAsync(string key, string defaultValue = "")
    {
        var policy = await _context.CompanyPolicies
            .FirstOrDefaultAsync(p => p.PolicyKey == key && p.CompanyId == (int)_currentUser.CompanyId!);
        
        return policy?.PolicyValue ?? defaultValue;
    }

    public async Task<T> GetPolicyValueAsync<T>(string key, T defaultValue = default!)
    {
        var value = await GetPolicyValueAsync(key, "");
        if (string.IsNullOrEmpty(value)) return defaultValue;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    public async Task SetPolicyValueAsync(string key, string value, PolicyDataType dataType)
    {
        var policy = await _context.CompanyPolicies
            .FirstOrDefaultAsync(p => p.PolicyKey == key && p.CompanyId == (int)_currentUser.CompanyId!);

        if (policy == null)
        {
            policy = new CompanyPolicy
            {
                PolicyKey = key,
                PolicyValue = value,
                DataType = dataType,
                CompanyId = (int)_currentUser.CompanyId!
            };
            _context.CompanyPolicies.Add(policy);
        }
        else
        {
            policy.PolicyValue = value;
            policy.DataType = dataType;
        }

        await _context.SaveChangesAsync();
    }

    public async Task SeedDefaultPoliciesAsync(int companyId)
    {
        var defaults = new List<CompanyPolicy>
        {
            new() { CompanyId = companyId, PolicyKey = "MonthlyWorkingDays", PolicyValue = "30", DataType = PolicyDataType.Int, Description = "عدد أيام العمل الشهرية الافتراضية للحساب" },
            new() { CompanyId = companyId, PolicyKey = "MinimumSalaryProtection", PolicyValue = "0", DataType = PolicyDataType.Decimal, Description = "الحد الأدنى للصافي المحمي من استقطاعات القروض" },
            new() { CompanyId = companyId, PolicyKey = "DeductLoansDuringUnpaidLeave", PolicyValue = "false", DataType = PolicyDataType.Boolean, Description = "هل يتم استقطاع القروض خلال الإجازات بدون راتب" }
        };

        foreach (var p in defaults)
        {
            if (!await _context.CompanyPolicies.AnyAsync(x => x.CompanyId == companyId && x.PolicyKey == p.PolicyKey))
            {
                _context.CompanyPolicies.Add(p);
            }
        }

        await _context.SaveChangesAsync();
    }
}
