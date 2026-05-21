using Ahorro.Data;
using Ahorro.Models.Entities;
using Ahorro.Models.Enums;
using Ahorro.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ahorro.Services.Goals;

public class SavingsGoalService : ISavingsGoalService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserContext _user;

    public SavingsGoalService(AppDbContext db, ICurrentUserContext user)
    {
        _db = db;
        _user = user;
    }

    public Task<List<SavingsGoal>> GetActiveGoalsAsync(CancellationToken ct = default) =>
        _db.SavingsGoals.AsNoTracking()
            .Where(g => g.UserProfileId == _user.UserId && g.Status == GoalStatus.Active)
            .OrderByDescending(g => g.AccumulatedAmount / g.TargetAmount)
            .ToListAsync(ct);

    public async Task ContributeAsync(Guid goalId, decimal amount, CancellationToken ct = default)
    {
        var goal = await _db.SavingsGoals.FindAsync([goalId], ct);
        if (goal == null) return;

        goal.AccumulatedAmount += amount;
        if (goal.AccumulatedAmount >= goal.TargetAmount)
            goal.Status = GoalStatus.Completed;

        _db.GoalContributions.Add(new GoalContribution
        {
            GoalId = goalId,
            Amount = amount,
            Date = DateTime.Today,
            IsAutomatic = false
        });
        await _db.SaveChangesAsync(ct);
    }
}
