using Ahorro.Data;
using Ahorro.Helpers;
using Ahorro.Models.Entities;
using Ahorro.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ahorro.Services.Budget;

public class BudgetService : IBudgetService
{
    private readonly AppDbContext _db;

    public BudgetService(AppDbContext db) => _db = db;

    public Task<List<BudgetAllocation>> GetAllocationsAsync(Guid periodId, CancellationToken ct = default) =>
        _db.BudgetAllocations.AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Subcategory)
            .Where(a => a.BudgetPeriodId == periodId)
            .OrderBy(a => a.Category!.SortOrder)
            .ThenBy(a => a.Subcategory!.SortOrder)
            .ToListAsync(ct);

    public async Task DuplicatePreviousPeriodAsync(Guid periodId, CancellationToken ct = default)
    {
        var current = await _db.BudgetPeriods.Include(p => p.Allocations).FirstOrDefaultAsync(p => p.Id == periodId, ct);
        if (current == null) return;

        var previous = await _db.BudgetPeriods
            .Where(p => p.UserProfileId == current.UserProfileId && p.EndDate < current.StartDate)
            .OrderByDescending(p => p.EndDate)
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(ct);

        if (previous == null) return;

        _db.BudgetAllocations.RemoveRange(current.Allocations);
        foreach (var a in previous.Allocations)
        {
            _db.BudgetAllocations.Add(new BudgetAllocation
            {
                BudgetPeriodId = periodId,
                CategoryId = a.CategoryId,
                SubcategoryId = a.SubcategoryId,
                AllocationMode = a.AllocationMode,
                PlannedAmount = a.PlannedAmount,
                PlannedPercent = a.PlannedPercent,
                ActualAmount = 0,
                Difference = a.PlannedAmount,
                UsedPercent = 0,
                Status = BudgetStatusCalculator.FromUsedPercent(0)
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task RecalculateStatusesAsync(Guid periodId, CancellationToken ct = default)
    {
        var allocations = await _db.BudgetAllocations.Where(a => a.BudgetPeriodId == periodId).ToListAsync(ct);
        foreach (var a in allocations)
        {
            a.UsedPercent = a.PlannedAmount > 0 ? Math.Round(a.ActualAmount / a.PlannedAmount * 100, 1) : 0;
            a.Difference = a.PlannedAmount - a.ActualAmount;
            a.Status = BudgetStatusCalculator.FromUsedPercent(a.UsedPercent);
        }
        await _db.SaveChangesAsync(ct);
    }
}
