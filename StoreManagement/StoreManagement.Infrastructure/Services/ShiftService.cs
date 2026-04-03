using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Common;
using StoreManagement.Shared.DTOs;
using StoreManagement.Shared.Entities.Finance;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

public class ShiftService : IShiftService
{
    private readonly StoreDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ShiftService(StoreDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ShiftReadDto> OpenShiftAsync(OpenShiftDto dto)
    {
        var branchId = _currentUser.BranchId ?? throw new InvalidOperationException("رقم الفرع غير محدد.");
        
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var openShiftExists = await _context.CashRegisterShifts
                .AnyAsync(s => s.BranchId == branchId && s.Status == ShiftStatus.Open);
                
            if (openShiftExists)
                throw new InvalidOperationException("يوجد وردية مفتوحة بالفعل في هذا الفرع. يجب إغلاقها أولاً.");

            var shift = new CashRegisterShift
            {
                BranchId = branchId,
                UserId = _currentUser.UserId!.Value,
                OpeningBalance = dto.OpeningBalance,
                Status = ShiftStatus.Open,
                OpenedAt = DateTime.UtcNow,
                CompanyId = _currentUser.CompanyId!.Value
            };

            _context.CashRegisterShifts.Add(shift);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetShiftByIdAsync(shift.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ShiftReadDto> CloseShiftAsync(int shiftId, CloseShiftDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var shift = await _context.CashRegisterShifts
                .Include(s => s.CashTransactions)
                .FirstOrDefaultAsync(s => s.Id == shiftId);

            if (shift == null)
                throw new KeyNotFoundException("الوردية غير موجودة.");

            if (shift.Status == ShiftStatus.Closed)
                throw new InvalidOperationException("الوردية مغلقة بالفعل.");

            // حساب الإجماليات بدقة (Snapshots)
            var totalIn = shift.CashTransactions.Where(t => t.Type == TransactionType.In).Sum(t => t.Value);
            var totalOut = shift.CashTransactions.Where(t => t.Type == TransactionType.Out).Sum(t => t.Value);

            shift.TotalCashIn = totalIn;
            shift.TotalCashOut = totalOut;
            shift.ClosingBalance = shift.OpeningBalance + totalIn - totalOut;
            
            shift.ActualClosingBalance = dto.ActualClosingBalance;
            shift.Difference = dto.ActualClosingBalance - shift.ClosingBalance;
            
            shift.Status = ShiftStatus.Closed;
            shift.ClosedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetShiftByIdAsync(shift.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ShiftReadDto?> GetCurrentShiftAsync()
    {
        var branchId = _currentUser.BranchId;
        if (!branchId.HasValue) return null;

        var shift = await _context.CashRegisterShifts
            .Include(s => s.User)
            .OrderByDescending(s => s.OpenedAt)
            .FirstOrDefaultAsync(s => s.BranchId == branchId.Value && s.Status == ShiftStatus.Open);

        if (shift == null) return null;

        return await GetShiftByIdAsync(shift.Id);
    }

    public async Task<int?> GetCurrentShiftIdAsync()
    {
        var branchId = _currentUser.BranchId;
        if (!branchId.HasValue) return null;

        var shift = await _context.CashRegisterShifts
            .OrderByDescending(s => s.OpenedAt)
            .FirstOrDefaultAsync(s => s.BranchId == branchId.Value && s.Status == ShiftStatus.Open);

        return shift?.Id;
    }

    public async Task<PagedResult<ShiftReadDto>> GetAllShiftsAsync(PaginationQueryDto query)
    {
        var branchId = _currentUser.BranchId;
        var baseQuery = _context.CashRegisterShifts
            .Include(s => s.User)
            .AsQueryable();

        if (branchId.HasValue)
            baseQuery = baseQuery.Where(s => s.BranchId == branchId.Value);

        var total = await baseQuery.CountAsync();
        var rawItems = await baseQuery
            .OrderByDescending(s => s.OpenedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<ShiftReadDto>
        {
            Items = rawItems.Select(MapToDto).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task<ShiftReadDto> GetShiftByIdAsync(int id)
    {
        var shift = await _context.CashRegisterShifts
            .Include(s => s.User)
            .Include(s => s.CashTransactions)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException("الوردية غير موجودة.");

        var dto = MapToDto(shift);
        
        // إذا كانت الوردية ما زالت مفتوحة، نحسب الـ Snapshots لحظياً للـ Preview
        if (shift.Status == ShiftStatus.Open)
        {
            dto.TotalCashIn = shift.CashTransactions.Where(t => t.Type == TransactionType.In).Sum(t => t.Value);
            dto.TotalCashOut = shift.CashTransactions.Where(t => t.Type == TransactionType.Out).Sum(t => t.Value);
            dto.ClosingBalance = dto.OpeningBalance + dto.TotalCashIn - dto.TotalCashOut;
        }

        return dto;
    }

    private ShiftReadDto MapToDto(CashRegisterShift shift)
    {
        return new ShiftReadDto
        {
            Id = shift.Id,
            UserId = shift.UserId,
            UserName = shift.User?.UserName ?? "Unknown",
            BranchId = shift.BranchId,
            OpeningBalance = shift.OpeningBalance,
            TotalCashIn = shift.TotalCashIn,
            TotalCashOut = shift.TotalCashOut,
            ClosingBalance = shift.ClosingBalance,
            ActualClosingBalance = shift.ActualClosingBalance,
            Difference = shift.Difference,
            OpenedAt = shift.OpenedAt,
            ClosedAt = shift.ClosedAt,
            Status = shift.Status.ToString()
        };
    }
}
