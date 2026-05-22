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
            return new DashboardData(0, 0, 0, 0, 0, 0, 0, 0, [], [], [], [], [], [], [], []);

        var allocations = await _db.BudgetAllocations.AsNoTracking()
            .Include(a => a.Category)
            .Where(a => a.BudgetPeriodId == periodId)
            .ToListAsync(ct);

        var transactions = await _db.Transactions.AsNoTracking()
            .Where(t => t.BudgetPeriodId == periodId)
            .OrderByDescending(t => t.Date)
            .ToListAsync(ct);

        var totalPlanned = allocations.Sum(a => a.PlannedAmount);
        var totalActual = allocations.Sum(a => a.ActualAmount);
        var savings = allocations.Where(a => a.Category?.DefaultGroup == BudgetGroup.Savings).Sum(a => a.ActualAmount);
        var debtPaid = transactions.Where(t => t.Type == TransactionType.DebtPayment).Sum(t => t.Amount);
        var execution = totalPlanned > 0 ? Math.Round(totalActual / totalPlanned * 100, 1) : 0;

        var comparisons = allocations
            .GroupBy(a => a.Category!.Name)
            .Select(g => new CategoryComparisonItem(g.Key, g.Sum(x => x.PlannedAmount), g.Sum(x => x.ActualAmount)))
            .OrderByDescending(c => c.Planned)
            .Take(6)
            .ToList();

        var distributionGroups = allocations
            .GroupBy(a => a.Category!.Name)
            .Select(g => new CategoryDistributionItem(g.Key, g.Sum(x => x.PlannedAmount), g.First().Category!.ColorHex))
            .Where(x => x.Amount > 0)
            .OrderByDescending(x => x.Amount)
            .ToList();

        var distribution = distributionGroups.Count <= 6
            ? distributionGroups
            : BuildDistributionTopSix(distributionGroups);

        var periods = await _db.BudgetPeriods.AsNoTracking()
            .Where(p => p.UserProfileId == period.UserProfileId)
            .OrderByDescending(p => p.StartDate)
            .Take(6)
            .ToListAsync(ct);

        var trendPeriodIds = periods.Select(p => p.Id).ToList();
        var trendAllocations = await _db.BudgetAllocations.AsNoTracking()
            .Where(a => trendPeriodIds.Contains(a.BudgetPeriodId))
            .Select(a => new { a.BudgetPeriodId, a.ActualAmount })
            .ToListAsync(ct);
        var actualByPeriod = trendAllocations
            .GroupBy(a => a.BudgetPeriodId)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.ActualAmount));

        var trend = periods.OrderBy(p => p.StartDate).Select(p =>
        {
            var spent = actualByPeriod.GetValueOrDefault(p.Id, p.ActualSpent);
            return new TrendPoint($"{p.StartDate:MMM yy}", p.TotalNetIncome, spent, p.TotalNetIncome - spent);
        }).ToList();

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
            .GroupBy(a => a.Category!.Name)
            .Select(g =>
            {
                var first = g.First();
                var planned = g.Sum(x => x.PlannedAmount);
                var actual = g.Sum(x => x.ActualAmount);
                var used = planned > 0 ? Math.Round(actual / planned * 100, 1) : first.UsedPercent;
                var status = g.Any(x => x.Status == BudgetLineStatus.Exceeded)
                    ? BudgetLineStatus.Exceeded
                    : first.Status;
                return new CriticalCategoryItem(g.Key, used, status);
            })
            .OrderByDescending(c => c.UsedPercent)
            .Take(6)
            .ToList();

        var alerts = BuildAlerts(allocations, upcoming, goals);

        return new DashboardData(
            period.TotalNetIncome,
            totalActual,
            savings,
            period.TotalNetIncome - totalActual,
            debtPaid,
            execution,
            totalPlanned,
            totalActual,
            comparisons,
            distribution,
            trend,
            upcoming,
            goals,
            critical,
            transactions.Take(8).ToList(),
            alerts);
    }

    private static List<CategoryDistributionItem> BuildDistributionTopSix(List<CategoryDistributionItem> items)
    {
        var top = items.Take(5).ToList();
        var others = items.Skip(5).Sum(x => x.Amount);
        if (others > 0)
            top.Add(new CategoryDistributionItem("Otros", others, "#93A4BD"));
        return top;
    }

    private static List<DashboardAlert> BuildAlerts(
        List<Models.Entities.BudgetAllocation> allocations,
        List<Models.Entities.ScheduledPayment> upcoming,
        List<Models.Entities.SavingsGoal> goals)
    {
        var alerts = new List<DashboardAlert>();

        foreach (var g in allocations
                     .GroupBy(a => a.Category!.Name)
                     .Select(grp =>
                     {
                         var planned = grp.Sum(x => x.PlannedAmount);
                         var actual = grp.Sum(x => x.ActualAmount);
                         var used = planned > 0 ? Math.Round(actual / planned * 100, 1) : 0;
                         var status = grp.Any(x => x.Status == BudgetLineStatus.Exceeded)
                             ? BudgetLineStatus.Exceeded
                             : grp.First().Status;
                         return (Category: grp.Key, Used: used, Status: status);
                     })
                     .Where(x => x.Status == BudgetLineStatus.Exceeded)
                     .OrderByDescending(x => x.Used)
                     .Take(3))
        {
            alerts.Add(new DashboardAlert(
                g.Category,
                $"Presupuesto excedido ({g.Used:0.#}%)",
                "danger"));
        }

        foreach (var g in allocations
                     .GroupBy(a => a.Category!.Name)
                     .Select(grp =>
                     {
                         var planned = grp.Sum(x => x.PlannedAmount);
                         var actual = grp.Sum(x => x.ActualAmount);
                         var used = planned > 0 ? Math.Round(actual / planned * 100, 1) : 0;
                         var status = grp.Any(x => x.Status == BudgetLineStatus.Exceeded)
                             ? BudgetLineStatus.Exceeded
                             : grp.First().Status;
                         return (Category: grp.Key, Used: used, Status: status);
                     })
                     .Where(x => x.Status is BudgetLineStatus.Attention or BudgetLineStatus.Limit)
                     .OrderByDescending(x => x.Used)
                     .Take(2))
        {
            alerts.Add(new DashboardAlert(
                g.Category,
                $"Cerca del límite ({g.Used:0.#}%)",
                "warning"));
        }

        foreach (var p in upcoming.Where(p => p.Status == ScheduledPaymentStatus.Overdue).Take(2))
        {
            alerts.Add(new DashboardAlert(
                "Pago vencido",
                $"{p.Name} · {ClpFormatter.Format(p.EstimatedAmount)} · venció {p.DueDate:dd MMM}",
                "danger"));
        }

        foreach (var p in upcoming
                     .Where(p => p.Status != ScheduledPaymentStatus.Overdue
                                 && (p.DueDate.Date - DateTime.Today).Days <= 7)
                     .OrderBy(p => p.DueDate)
                     .Take(2))
        {
            var days = (p.DueDate.Date - DateTime.Today).Days;
            var when = days switch
            {
                0 => "vence hoy",
                1 => "mañana",
                _ => $"en {days} días"
            };
            alerts.Add(new DashboardAlert(
                "Pago próximo",
                $"{p.Name} · {ClpFormatter.Format(p.EstimatedAmount)} · {when}",
                p.EstimatedAmount >= 100_000 ? "warning" : "info"));
        }

        foreach (var g in goals
                     .Where(g => g.TargetDate.HasValue
                                 && g.TargetDate.Value.Date >= DateTime.Today
                                 && g.TargetDate.Value.Date <= DateTime.Today.AddDays(90)
                                 && g.TargetAmount > 0
                                 && g.AccumulatedAmount / g.TargetAmount < 0.5m)
                     .Take(2))
        {
            alerts.Add(new DashboardAlert(
                "Meta atrasada",
                $"{g.Name} al {(g.AccumulatedAmount / g.TargetAmount * 100):0.#}% · objetivo {g.TargetDate:dd MMM yyyy}",
                "warning"));
        }

        if (alerts.Count == 0)
        {
            alerts.Add(new DashboardAlert(
                "Control estable",
                "No hay alertas críticas en este periodo.",
                "info"));
        }

        return alerts.Take(8).ToList();
    }
}
