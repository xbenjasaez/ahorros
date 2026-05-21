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

        var allocations = await _db.BudgetAllocations.AsNoTracking()
            .Include(a => a.Category)
            .Where(a => a.BudgetPeriodId == periodId)
            .ToListAsync(ct);

        var byCategory = allocations
            .GroupBy(a => a.Category!.Name)
            .Select(g => new CategoryDistributionItem(g.Key, g.Sum(x => x.ActualAmount), g.First().Category!.ColorHex))
            .Where(x => x.Amount > 0)
            .ToList();

        var periods = await _db.BudgetPeriods.AsNoTracking()
            .Where(p => period != null && p.UserProfileId == period.UserProfileId)
            .OrderBy(p => p.StartDate)
            .Take(6)
            .ToListAsync(ct);

        var trend = periods.Select(p => new TrendPoint($"{p.StartDate:MMM}", p.TotalNetIncome, p.ActualSpent, p.TotalNetIncome - p.ActualSpent)).ToList();

        var top = await _db.Transactions.AsNoTracking()
            .Where(t => t.BudgetPeriodId == periodId && t.Type == TransactionType.Expense)
            .OrderByDescending(t => t.Amount)
            .Take(10)
            .Select(t => new ValueTuple<string, decimal>(t.Description, t.Amount))
            .ToListAsync(ct);

        var savings = await _db.SavingsGoals.AsNoTracking()
            .Where(g => period != null && g.UserProfileId == period.UserProfileId)
            .SumAsync(g => g.AccumulatedAmount, ct);

        return new ReportData(label, byCategory, trend, savings, top);
    }
}
