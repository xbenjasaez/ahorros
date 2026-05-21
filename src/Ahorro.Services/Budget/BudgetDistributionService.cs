using Ahorro.Data;
using Ahorro.Models.Enums;
using Ahorro.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ahorro.Services.Budget;

public class BudgetDistributionService : IBudgetDistributionService
{
    private readonly AppDbContext _db;

    public BudgetDistributionService(AppDbContext db) => _db = db;

    public async Task ApplyRule503020Async(Guid periodId, decimal needsPercent, decimal wantsPercent, decimal savingsPercent, CancellationToken ct = default)
    {
        var period = await _db.BudgetPeriods.FindAsync([periodId], ct);
        if (period == null) return;

        var net = period.TotalNetIncome;
        var categories = await _db.BudgetCategories.Where(c => c.UserProfileId == period.UserProfileId && c.IsActive).ToListAsync(ct);
        var allocations = await _db.BudgetAllocations.Where(a => a.BudgetPeriodId == periodId).ToListAsync(ct);

        foreach (var cat in categories)
        {
            var pct = cat.DefaultGroup switch
            {
                BudgetGroup.Needs => needsPercent,
                BudgetGroup.Wants => wantsPercent,
                BudgetGroup.Savings => savingsPercent,
                _ => 0m
            };
            if (pct <= 0) continue;

            var groupTotal = net * (pct / 100m);
            var catAllocs = allocations.Where(a => a.CategoryId == cat.Id).ToList();
            if (catAllocs.Count == 0)
            {
                _db.BudgetAllocations.Add(new BudgetAllocation
                {
                    BudgetPeriodId = periodId,
                    CategoryId = cat.Id,
                    AllocationMode = AllocationMode.Percentage,
                    PlannedPercent = pct / Math.Max(1, categories.Count(c => c.DefaultGroup == cat.DefaultGroup)),
                    PlannedAmount = groupTotal / Math.Max(1, categories.Count(c => c.DefaultGroup == cat.DefaultGroup))
                });
            }
            else
            {
                foreach (var a in catAllocs)
                {
                    a.PlannedAmount = groupTotal / catAllocs.Count;
                    a.PlannedPercent = pct;
                    a.AllocationMode = AllocationMode.Percentage;
                }
            }
        }

        period.PlannedBudget = net;
        await _db.SaveChangesAsync(ct);
    }
}
