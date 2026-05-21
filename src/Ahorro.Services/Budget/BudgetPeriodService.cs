using Ahorro.Data;
using Ahorro.Models.Entities;
using Ahorro.Models.Enums;
using Ahorro.Models.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ahorro.Services.Budget;

public class BudgetPeriodService : IBudgetPeriodService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserContext _user;

    public BudgetPeriodService(AppDbContext db, ICurrentUserContext user)
    {
        _db = db;
        _user = user;
    }

    public async Task<BudgetPeriod?> GetActivePeriodAsync(CancellationToken ct = default)
    {
        if (_user.ActivePeriodId.HasValue)
            return await GetByIdAsync(_user.ActivePeriodId.Value, ct);

        return await EnsureActivePeriodAsync(ct);
    }

    public Task<List<BudgetPeriod>> GetPeriodsAsync(CancellationToken ct = default) =>
        _db.BudgetPeriods.AsNoTracking()
            .Where(p => p.UserProfileId == _user.UserId)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(ct);

    public async Task<BudgetPeriod?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.BudgetPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<BudgetPeriod> EnsureActivePeriodAsync(CancellationToken ct = default)
    {
        var profile = await _db.UserProfiles.FindAsync([_user.UserId], ct)
            ?? throw new InvalidOperationException("Perfil no encontrado.");

        var today = DateTime.Today;
        var existing = await _db.BudgetPeriods
            .Where(p => p.UserProfileId == _user.UserId && p.StartDate <= today && p.EndDate >= today)
            .FirstOrDefaultAsync(ct);

        if (existing != null)
        {
            _user.ActivePeriodId = existing.Id;
            return existing;
        }

        var (start, end) = CalculatePeriodBounds(today, profile.CutoffDay, profile.DefaultFrequency);
        var previous = await _db.BudgetPeriods
            .Where(p => p.UserProfileId == _user.UserId)
            .OrderByDescending(p => p.EndDate)
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(ct);

        var period = new BudgetPeriod
        {
            UserProfileId = _user.UserId,
            StartDate = start,
            EndDate = end,
            Frequency = profile.DefaultFrequency,
            TotalGrossIncome = previous?.TotalGrossIncome ?? 0,
            TotalNetIncome = previous?.TotalNetIncome ?? 0,
            PlannedBudget = previous?.PlannedBudget ?? 0
        };

        _db.BudgetPeriods.Add(period);

        if (previous != null)
        {
            foreach (var alloc in previous.Allocations)
            {
                _db.BudgetAllocations.Add(new BudgetAllocation
                {
                    BudgetPeriodId = period.Id,
                    CategoryId = alloc.CategoryId,
                    SubcategoryId = alloc.SubcategoryId,
                    AllocationMode = alloc.AllocationMode,
                    PlannedAmount = alloc.PlannedAmount,
                    PlannedPercent = alloc.PlannedPercent,
                    RolloverFromPrevious = Math.Max(0, alloc.PlannedAmount - alloc.ActualAmount)
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        _user.ActivePeriodId = period.Id;
        return period;
    }

    private static (DateTime start, DateTime end) CalculatePeriodBounds(DateTime today, int cutoffDay, PeriodFrequency frequency)
    {
        cutoffDay = Math.Clamp(cutoffDay, 1, 28);
        var end = new DateTime(today.Year, today.Month, Math.Min(cutoffDay, DateTime.DaysInMonth(today.Year, today.Month)));
        if (today > end)
            end = end.AddMonths(1);
        var start = end.AddMonths(-1).AddDays(1);
        if (frequency == PeriodFrequency.Biweekly)
            end = start.AddDays(13);
        return (start, end);
    }
}
