using Ahorro.Data;
using Ahorro.Helpers;
using Ahorro.Models.Abstractions;
using Ahorro.Models.Entities;
using Ahorro.Models.Enums;
using Ahorro.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ahorro.Services.Goals;

public class SavingsGoalService : ISavingsGoalService
{
    private const decimal DefaultMonthlyPace = 50_000m;
    private readonly AppDbContext _db;
    private readonly ICurrentUserContext _user;

    public SavingsGoalService(AppDbContext db, ICurrentUserContext user)
    {
        _db = db;
        _user = user;
    }

    public async Task<List<SavingsGoal>> GetActiveGoalsAsync(CancellationToken ct = default)
    {
        var goals = await _db.SavingsGoals.AsNoTracking()
            .Include(g => g.Category)
            .Where(g => g.UserProfileId == _user.UserId && g.Status == GoalStatus.Active)
            .ToListAsync(ct);

        return goals
            .OrderByDescending(g => g.TargetAmount > 0 ? g.AccumulatedAmount / g.TargetAmount : 0)
            .ThenBy(g => g.TargetDate)
            .ToList();
    }

    public async Task<GoalsDashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var goals = await GetActiveGoalsAsync(ct);
        var totalSaved = goals.Sum(g => g.AccumulatedAmount);
        var totalTarget = goals.Sum(g => g.TargetAmount);
        var totalRemaining = goals.Sum(g => Math.Max(0, g.TargetAmount - g.AccumulatedAmount));
        var projection = BuildPortfolioProjection(goals, totalRemaining);
        return new GoalsDashboardSummary(totalSaved, goals.Count, totalTarget, totalRemaining, projection);
    }

    public Task<SavingsGoal?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.SavingsGoals.AsNoTracking()
            .Include(g => g.Category)
            .FirstOrDefaultAsync(g => g.Id == id && g.UserProfileId == _user.UserId, ct);

    public async Task<List<GoalContribution>> GetRecentContributionsAsync(Guid goalId, int limit = 8, CancellationToken ct = default)
    {
        var rows = await _db.GoalContributions.AsNoTracking()
            .Where(c => c.GoalId == goalId)
            .ToListAsync(ct);

        return rows
            .OrderByDescending(c => c.Date)
            .ThenByDescending(c => c.Amount)
            .Take(limit)
            .ToList();
    }

    public async Task<SavingsGoal> CreateAsync(SavingsGoalUpdate data, CancellationToken ct = default)
    {
        var goal = MapToEntity(new SavingsGoal { UserProfileId = _user.UserId }, data);
        _db.SavingsGoals.Add(goal);
        await _db.SaveChangesAsync(ct);
        return goal;
    }

    public async Task UpdateAsync(Guid id, SavingsGoalUpdate data, CancellationToken ct = default)
    {
        var goal = await _db.SavingsGoals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserProfileId == _user.UserId, ct);
        if (goal == null) return;

        MapToEntity(goal, data);
        if (goal.AccumulatedAmount >= goal.TargetAmount && goal.TargetAmount > 0)
            goal.Status = GoalStatus.Completed;
        else if (goal.Status == GoalStatus.Completed && goal.AccumulatedAmount < goal.TargetAmount)
            goal.Status = GoalStatus.Active;

        await _db.SaveChangesAsync(ct);
    }

    public async Task ContributeAsync(Guid goalId, decimal amount, CancellationToken ct = default)
    {
        if (amount <= 0) return;

        var goal = await _db.SavingsGoals
            .FirstOrDefaultAsync(g => g.Id == goalId && g.UserProfileId == _user.UserId, ct);
        if (goal == null) return;

        goal.AccumulatedAmount += amount;
        if (goal.TargetAmount > 0 && goal.AccumulatedAmount >= goal.TargetAmount)
            goal.Status = GoalStatus.Completed;

        _db.GoalContributions.Add(new GoalContribution
        {
            GoalId = goalId,
            Amount = amount,
            Date = DateTime.Today,
            IsAutomatic = false,
            Note = "Aporte manual"
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task ArchiveAsync(Guid goalId, CancellationToken ct = default)
    {
        var goal = await _db.SavingsGoals
            .FirstOrDefaultAsync(g => g.Id == goalId && g.UserProfileId == _user.UserId, ct);
        if (goal == null) return;

        goal.Status = GoalStatus.Archived;
        await _db.SaveChangesAsync(ct);
    }

    private static SavingsGoal MapToEntity(SavingsGoal goal, SavingsGoalUpdate data)
    {
        goal.Name = data.Name.Trim();
        goal.TargetAmount = Math.Max(0, data.TargetAmount);
        goal.TargetDate = data.TargetDate;
        goal.CategoryId = data.CategoryId;
        goal.ColorHex = string.IsNullOrWhiteSpace(data.ColorHex) ? "#35E0A1" : data.ColorHex.Trim();
        goal.IconKey = string.IsNullOrWhiteSpace(data.IconKey) ? "target" : data.IconKey.Trim();
        goal.AutoContributeFromBudget = data.AutoContributeFromBudget;
        return goal;
    }

    private static string BuildPortfolioProjection(IReadOnlyList<SavingsGoal> goals, decimal totalRemaining)
    {
        if (goals.Count == 0)
            return "Sin metas activas";

        if (totalRemaining <= 0)
            return "Todas las metas alcanzadas";

        var dated = goals
            .Where(g => g.TargetDate.HasValue && g.TargetAmount > g.AccumulatedAmount)
            .Select(g => (Remaining: g.TargetAmount - g.AccumulatedAmount, Months: MonthsUntil(g.TargetDate!.Value)))
            .Where(x => x.Months > 0)
            .ToList();

        if (dated.Count > 0)
        {
            var requiredMonthly = dated.Sum(x => x.Remaining / x.Months);
            return $"Ritmo sugerido {ClpFormatter.FormatCompact(requiredMonthly)}/mes para fechas objetivo";
        }

        var months = (int)Math.Ceiling(totalRemaining / (DefaultMonthlyPace * goals.Count));
        return $"~{months} meses a {ClpFormatter.FormatCompact(DefaultMonthlyPace)}/mes por meta";
    }

    private static int MonthsUntil(DateTime targetDate)
    {
        var today = DateTime.Today;
        return Math.Max(1, (targetDate.Year - today.Year) * 12 + targetDate.Month - today.Month);
    }
}
