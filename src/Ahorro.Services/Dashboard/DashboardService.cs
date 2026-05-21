using Ahorro.Data;
using Ahorro.Helpers;
using Ahorro.Models.Enums;
using Ahorro.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ahorro.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardData> LoadAsync(Guid periodId, CancellationToken ct = default)
    {
        var period = await _db.BudgetPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == periodId, ct);
        if (period == null)
            return new DashboardData(0, 0, 0, 0, 0, 0, [], [], [], [], [], [], [], []);

        var allocations = await _db.BudgetAllocations.AsNoTracking()
            .Include(a => a.Category)
            .Where(a => a.BudgetPeriodId == periodId)
            .ToListAsync(ct);

        var transactions = await _db.Transactions.AsNoTracking()
            .Where(t => t.BudgetPeriodId == periodId)
            .OrderByDescending(t => t.Date)
            .ToListAsync(ct);

        var expenses = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var savings = allocations.Where(a => a.Category?.DefaultGroup == BudgetGroup.Savings).Sum(a => a.ActualAmount);
        var debtPaid = transactions.Where(t => t.Type == TransactionType.DebtPayment).Sum(t => t.Amount);

        var comparisons = allocations
            .GroupBy(a => a.Category!.Name)
            .Select(g => new CategoryComparisonItem(g.Key, g.Sum(x => x.PlannedAmount), g.Sum(x => x.ActualAmount)))
            .ToList();

        var distribution = allocations
            .GroupBy(a => a.Category!.Name)
            .Select(g => new CategoryDistributionItem(g.Key, g.Sum(x => x.ActualAmount), g.First().Category!.ColorHex))
            .Where(x => x.Amount > 0)
            .ToList();

        var periods = await _db.BudgetPeriods.AsNoTracking()
            .Where(p => p.UserProfileId == period.UserProfileId)
            .OrderByDescending(p => p.StartDate)
            .Take(6)
            .ToListAsync(ct);

        var trend = periods.OrderBy(p => p.StartDate).Select(p =>
            new TrendPoint($"{p.StartDate:MMM yy}", p.TotalNetIncome, p.ActualSpent,
                p.TotalNetIncome - p.ActualSpent)).ToList();

        var upcoming = await _db.ScheduledPayments.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.UserProfileId == period.UserProfileId && p.Status != ScheduledPaymentStatus.Paid)
            .OrderBy(p => p.DueDate)
            .Take(6)
            .ToListAsync(ct);

        var goals = await _db.SavingsGoals.AsNoTracking()
            .Where(g => g.UserProfileId == period.UserProfileId && g.Status == GoalStatus.Active)
            .Take(4)
            .ToListAsync(ct);

        var critical = allocations
            .Where(a => a.UsedPercent >= 80)
            .Select(a => new CriticalCategoryItem(a.Category!.Name, a.UsedPercent, a.Status))
            .ToList();

        var alerts = critical.Select(c =>
            c.Status == BudgetLineStatus.Exceeded
                ? $"{c.Category} excedió el presupuesto ({c.UsedPercent:0}%)"
                : $"{c.Category} cerca del límite ({c.UsedPercent:0}%)").ToList();

        var execution = period.PlannedBudget > 0 ? period.ActualSpent / period.PlannedBudget * 100 : 0;

        return new DashboardData(
            period.TotalNetIncome,
            expenses,
            savings,
            period.TotalNetIncome - expenses,
            debtPaid,
            execution,
            comparisons,
            distribution,
            trend,
            upcoming,
            goals,
            critical,
            transactions.Take(8).ToList(),
            alerts);
    }
}
