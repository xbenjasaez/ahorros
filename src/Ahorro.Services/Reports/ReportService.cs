using Ahorro.Data;
using Ahorro.Models.Enums;
using Ahorro.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ahorro.Services.Reports;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db) => _db = db;

    public async Task<ReportData> LoadAsync(Guid periodId, CancellationToken ct = default)
    {
        var period = await _db.BudgetPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == periodId, ct);
        var label = period == null ? "—" : $"{period.StartDate:dd MMM} – {period.EndDate:dd MMM yyyy}";

        if (period == null)
            return new ReportData(periodId, label, new ReportSummary(0, 0, 0, 0, 0), [], [], 0, [], []);

        var allocations = await _db.BudgetAllocations.AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Subcategory)
            .Where(a => a.BudgetPeriodId == periodId)
            .ToListAsync(ct);

        var expenses = await _db.Transactions.AsNoTracking()
            .Where(t => t.BudgetPeriodId == periodId && t.Type == TransactionType.Expense)
            .SumAsync(t => t.Amount, ct);

        var savingsAlloc = allocations
            .Where(a => a.Category?.DefaultGroup == BudgetGroup.Savings)
            .Sum(a => a.ActualAmount);

        var byCategory = allocations
            .GroupBy(a => a.Category!.Name)
            .Select(g => new CategoryDistributionItem(g.Key, g.Sum(x => x.ActualAmount), g.First().Category!.ColorHex))
            .Where(x => x.Amount > 0)
            .OrderByDescending(x => x.Amount)
            .ToList();

        var periods = await _db.BudgetPeriods.AsNoTracking()
            .Where(p => p.UserProfileId == period.UserProfileId)
            .OrderByDescending(p => p.StartDate)
            .Take(6)
            .ToListAsync(ct);

        var trend = periods
            .OrderBy(p => p.StartDate)
            .Select(p => new TrendPoint($"{p.StartDate:MMM yy}", p.TotalNetIncome, p.ActualSpent, p.TotalNetIncome - p.ActualSpent))
            .ToList();

        var top = await _db.Transactions.AsNoTracking()
            .Where(t => t.BudgetPeriodId == periodId && t.Type == TransactionType.Expense)
            .OrderByDescending(t => t.Amount)
            .Take(10)
            .Select(t => new ValueTuple<string, decimal>(t.Description, t.Amount))
            .ToListAsync(ct);

        var accumulated = await _db.SavingsGoals.AsNoTracking()
            .Where(g => g.UserProfileId == period.UserProfileId && g.Status == GoalStatus.Active)
            .SumAsync(g => g.AccumulatedAmount, ct);

        var exceeded = allocations
            .Where(a => a.Status == BudgetLineStatus.Exceeded || a.UsedPercent > 100)
            .GroupBy(a => a.Category!.Name)
            .Select(g =>
            {
                var first = g.First();
                var planned = g.Sum(x => x.PlannedAmount);
                var actual = g.Sum(x => x.ActualAmount);
                var used = planned > 0 ? Math.Round(actual / planned * 100, 1) : 100;
                return new ExceededCategoryItem(g.Key, planned, actual, used, first.Category!.ColorHex);
            })
            .OrderByDescending(x => x.UsedPercent)
            .ToList();

        var execution = period.PlannedBudget > 0
            ? Math.Round(period.ActualSpent / period.PlannedBudget * 100, 1)
            : 0;

        var summary = new ReportSummary(
            period.TotalNetIncome,
            expenses,
            savingsAlloc,
            period.TotalNetIncome - expenses,
            execution);

        return new ReportData(periodId, label, summary, byCategory, trend, accumulated, top, exceeded);
    }
}
