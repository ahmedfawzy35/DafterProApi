using Microsoft.EntityFrameworkCore;
using StoreManagement.Data;
using StoreManagement.Shared.Entities.Diagnostics;
using StoreManagement.Shared.Enums;
using StoreManagement.Shared.Interfaces;

namespace StoreManagement.Infrastructure.Services;

public class ReconciliationService : IReconciliationService
{
    private readonly StoreDbContext _context;
    private readonly IFinanceService _financeService;

    public ReconciliationService(StoreDbContext context, IFinanceService financeService)
    {
        _context = context;
        _financeService = financeService;
    }

    public async Task RunCompanyReconciliationAsync(int companyId)
    {
        var findings = new List<ReconciliationFinding>();

        // 1. Inventory Drift Checks
        findings.AddRange(await CheckInventoryDriftAsync(companyId));

        // 2. Financial Checks
        findings.AddRange(await CheckFinancialDriftAsync(companyId));

        // 3. Allocation Checks
        findings.AddRange(await CheckAllocationIntegrityAsync(companyId));

        // 4. Return Integrity Checks
        findings.AddRange(await CheckReturnIntegrityAsync(companyId));

        // 5. Period Lock Checks
        findings.AddRange(await CheckPeriodLocksAsync(companyId));

        await PersistAndDedupFindingsAsync(companyId, findings);
    }

    private async Task<List<ReconciliationFinding>> CheckInventoryDriftAsync(int companyId)
    {
        var anomalies = new List<ReconciliationFinding>();

        // Rule: Negative Stock
        var negativeStocks = await _context.BranchProductStocks
            .Where(b => b.CompanyId == companyId && b.Quantity < 0)
            .ToListAsync();

        foreach (var ns in negativeStocks)
        {
            anomalies.Add(new ReconciliationFinding
            {
                CompanyId = companyId,
                Category = "Inventory",
                RuleCode = "INV_NEGATIVE_STOCK",
                Severity = "High",
                EntityType = "BranchProductStock",
                EntityId = ns.Id,
                Message = $"Product {ns.ProductId} in Branch {ns.BranchId} has a negative stock quantity of {ns.Quantity}.",
                AnomalySignature = $"INV_NEGATIVE_STOCK-BranchProductStock-{ns.Id}"
            });
        }

        return anomalies;
    }

    private async Task<List<ReconciliationFinding>> CheckFinancialDriftAsync(int companyId)
    {
        var anomalies = new List<ReconciliationFinding>();

        // Rule: Voided Receipt Allocated
        var voidedWithAllocations = await _context.CustomerReceipts
            .Include(r => r.Allocations)
            .Where(r => r.CompanyId == companyId && r.FinancialStatus == FinancialStatus.Voided && r.Allocations.Any())
            .ToListAsync();

        foreach (var r in voidedWithAllocations)
        {
            anomalies.Add(new ReconciliationFinding
            {
                CompanyId = companyId,
                Category = "Financial",
                RuleCode = "FIN_VOIDED_RECEIPT_ALLOCATED",
                Severity = "Critical",
                EntityType = "CustomerReceipt",
                EntityId = r.Id,
                Message = $"CustomerReceipt {r.Id} is Voided but has {r.Allocations.Count} active allocations.",
                AnomalySignature = $"FIN_VOIDED_RECEIPT_ALLOCATED-CustomerReceipt-{r.Id}"
            });
        }

        return anomalies;
    }

    private async Task<List<ReconciliationFinding>> CheckAllocationIntegrityAsync(int companyId)
    {
        var anomalies = new List<ReconciliationFinding>();

        var invoices = await _context.Invoices
            .Include(i => i.CustomerAllocations)
            .Where(i => i.CompanyId == companyId && i.Status != InvoiceStatus.Cancelled)
            .ToListAsync();

        foreach (var invoice in invoices)
        {
            var totalAllocated = invoice.CustomerAllocations.Sum(a => a.Amount);
            if (totalAllocated > invoice.NetTotal)
            {
                anomalies.Add(new ReconciliationFinding
                {
                    CompanyId = companyId,
                    Category = "Allocation",
                    RuleCode = "ALLOC_OVERAPPLIED",
                    Severity = "Critical",
                    EntityType = "Invoice",
                    EntityId = invoice.Id,
                    Message = $"Invoice {invoice.Id} has allocations totaling {totalAllocated} which exceeds its NetTotal of {invoice.NetTotal}.",
                    AnomalySignature = $"ALLOC_OVERAPPLIED-Invoice-{invoice.Id}"
                });
            }
        }

        return anomalies;
    }

    private async Task<List<ReconciliationFinding>> CheckReturnIntegrityAsync(int companyId)
    {
        var anomalies = new List<ReconciliationFinding>();

        // Cancelled Returns shouldn't leave an active CashTransaction with Notes 'SalesReturnRefund'
        // This is a minimal heuristic. Advanced rule checks can be added later.
        var cancelledReturns = await _context.Invoices
            .Where(i => i.CompanyId == companyId && i.Type == InvoiceType.SalesReturn && i.Status == InvoiceStatus.Cancelled)
            .ToListAsync();

        foreach(var ret in cancelledReturns)
        {
             // Check if an active Refund Receipt exists for this Return Id (matching Amount and Date nearby, or if idempotency key used).
             // To simplify, we rely on the specific Refund CustomerReceipt checks.
             // We can expand this later.
        }

        return anomalies;
    }

    private async Task<List<ReconciliationFinding>> CheckPeriodLocksAsync(int companyId)
    {
        var anomalies = new List<ReconciliationFinding>();
        
        var periods = await _context.AccountingPeriods
            .Where(p => p.CompanyId == companyId && p.IsClosed)
            .ToListAsync();

        foreach(var period in periods)
        {
            // Detect modified invoices inside closed period, where UpdateDate (if existed) > period.ClosedAt
            // For now, minimal placeholder if Entity has Audit fields
        }

        return anomalies;
    }

    private async Task PersistAndDedupFindingsAsync(int companyId, List<ReconciliationFinding> newFindings)
    {
        var existingOpenFindings = await _context.ReconciliationFindings
            .Where(f => f.CompanyId == companyId && f.Status == FindingStatus.Open)
            .ToListAsync();

        foreach (var newFinding in newFindings)
        {
            var existing = existingOpenFindings.FirstOrDefault(f => f.AnomalySignature == newFinding.AnomalySignature);
            
            if (existing != null)
            {
                // Update LastSeenAt
                existing.LastSeenAt = DateTime.UtcNow;
                _context.ReconciliationFindings.Update(existing);
                
                // Remove from tracking list because it's still actively an issue
                existingOpenFindings.Remove(existing);
            }
            else
            {
                // Truly new anomaly
                await _context.ReconciliationFindings.AddAsync(newFinding);
            }
        }

        // Auto-Resolve remaining Open findings that were NOT detected in the latest scan
        foreach (var resolved in existingOpenFindings)
        {
            resolved.Status = FindingStatus.Resolved;
            resolved.ResolvedAt = DateTime.UtcNow;
            resolved.ResolutionSource = "AutoReconciledScan";
            _context.ReconciliationFindings.Update(resolved);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ReconciliationFinding>> GetStoredFindingsAsync(
        int companyId, 
        FindingStatus? status = null, 
        string? category = null, 
        string? severity = null, 
        DateTime? from = null, 
        DateTime? to = null)
    {
        var query = _context.ReconciliationFindings.Where(f => f.CompanyId == companyId).AsQueryable();

        if (status.HasValue) query = query.Where(f => f.Status == status.Value);
        if (!string.IsNullOrEmpty(category)) query = query.Where(f => f.Category == category);
        if (!string.IsNullOrEmpty(severity)) query = query.Where(f => f.Severity == severity);
        if (from.HasValue) query = query.Where(f => f.DetectedAt >= from.Value);
        if (to.HasValue) query = query.Where(f => f.DetectedAt <= to.Value);

        return await query.OrderByDescending(f => f.DetectedAt).ToListAsync();
    }

    public async Task<ReconciliationFinding?> GetFindingByIdAsync(int id, int companyId)
    {
        return await _context.ReconciliationFindings
            .FirstOrDefaultAsync(f => f.Id == id && f.CompanyId == companyId);
    }
}
