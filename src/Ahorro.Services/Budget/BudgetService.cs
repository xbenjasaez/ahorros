using Ahorro.Data;
using Ahorro.Helpers;
using Ahorro.Models.Abstractions;
using Ahorro.Models.Entities;
using Ahorro.Models.Enums;
using Ahorro.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ahorro.Services.Budget;

public class BudgetService : IBudgetService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserContext _user;

    public BudgetService(AppDbContext db, ICurrentUserContext user)
    {
        _db = db;
        _user = user;
    }

    public Task<List<BudgetAllocation>> GetAllocationsAsync(Guid periodId, CancellationToken ct = default) =>
        _db.BudgetAllocations.AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Subcategory)
            .Where(a => a.BudgetPeriodId == periodId)
            .OrderBy(a => a.Category!.SortOrder)
            .ThenBy(a => a.Subcategory == null ? 0 : 1)
            .ThenBy(a => a.Subcategory!.SortOrder)
            .ToListAsync(ct);

    public async Task<BudgetPeriodSummary> GetSummaryAsync(Guid periodId, CancellationToken ct = default)
    {
        var period = await _db.BudgetPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == periodId, ct);
        if (period == null)
            return new BudgetPeriodSummary(0, 0, 0, 0, 0, 0);

        var allocations = await _db.BudgetAllocations.AsNoTracking()
            .Where(a => a.BudgetPeriodId == periodId)
            .ToListAsync(ct);

        var planned = allocations.Sum(a => a.PlannedAmount);
        var actual = allocations.Sum(a => a.ActualAmount);
        var remaining = period.TotalNetIncome - actual;
        var execution = planned > 0 ? Math.Round(actual / planned * 100, 1) : 0;

        return new BudgetPeriodSummary(
            period.TotalGrossIncome,
            period.TotalNetIncome,
            planned,
            actual,
            remaining,
            execution);
    }

    public Task<List<BudgetCategory>> GetCategoriesAsync(CancellationToken ct = default) =>
        _db.BudgetCategories.AsNoTracking()
            .Where(c => c.UserProfileId == _user.UserId && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

    public async Task<BudgetCategory> AddCategoryAsync(string name, BudgetGroup group, Guid periodId, CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ArgumentException("El nombre de la categoría es obligatorio.");

        var maxOrder = await _db.BudgetCategories
            .Where(c => c.UserProfileId == _user.UserId)
            .Select(c => (int?)c.SortOrder)
            .MaxAsync(ct) ?? 0;

        var category = new BudgetCategory
        {
            UserProfileId = _user.UserId,
            Name = trimmed,
            DefaultGroup = group,
            SortOrder = maxOrder + 1,
            ColorHex = group switch
            {
                BudgetGroup.Needs => "#27D3FF",
                BudgetGroup.Wants => "#FFB84D",
                BudgetGroup.Savings => "#35E0A1",
                _ => "#9B7AFF"
            }
        };

        _db.BudgetCategories.Add(category);
        _db.BudgetAllocations.Add(new BudgetAllocation
        {
            BudgetPeriodId = periodId,
            CategoryId = category.Id,
            AllocationMode = AllocationMode.Manual,
            PlannedAmount = 0,
            ActualAmount = 0,
            Difference = 0,
            UsedPercent = 0,
            Status = BudgetLineStatus.Normal
        });

        await _db.SaveChangesAsync(ct);
        return category;
    }

    public async Task<BudgetSubcategory> AddSubcategoryAsync(Guid categoryId, string name, Guid periodId, CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ArgumentException("El nombre de la subcategoría es obligatorio.");

        var category = await _db.BudgetCategories
            .Include(c => c.Subcategories)
            .FirstOrDefaultAsync(c => c.Id == categoryId && c.UserProfileId == _user.UserId, ct)
            ?? throw new InvalidOperationException("Categoría no encontrada.");

        var maxOrder = category.Subcategories.Select(s => s.SortOrder).DefaultIfEmpty(0).Max();
        var sub = new BudgetSubcategory
        {
            CategoryId = categoryId,
            Name = trimmed,
            SortOrder = maxOrder + 1
        };

        _db.BudgetSubcategories.Add(sub);
        _db.BudgetAllocations.Add(new BudgetAllocation
        {
            BudgetPeriodId = periodId,
            CategoryId = categoryId,
            SubcategoryId = sub.Id,
            AllocationMode = AllocationMode.Manual,
            PlannedAmount = 0,
            ActualAmount = 0,
            Difference = 0,
            UsedPercent = 0,
            Status = BudgetLineStatus.Normal
        });

        await _db.SaveChangesAsync(ct);
        return sub;
    }

    public async Task UpdatePeriodIncomeAsync(Guid periodId, decimal grossIncome, decimal netIncome, CancellationToken ct = default)
    {
        var period = await _db.BudgetPeriods.FirstOrDefaultAsync(p => p.Id == periodId, ct);
        if (period == null) return;

        period.TotalGrossIncome = Math.Max(0, grossIncome);
        period.TotalNetIncome = Math.Max(0, netIncome);
        period.PlannedBudget = period.TotalNetIncome;
        await _db.SaveChangesAsync(ct);
    }

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

        var period = await _db.BudgetPeriods.FirstOrDefaultAsync(p => p.Id == periodId, ct);
        if (period != null)
        {
            period.ActualSpent = allocations.Sum(x => x.ActualAmount);
            period.ExecutionPercent = period.PlannedBudget > 0
                ? Math.Round(period.ActualSpent / period.PlannedBudget * 100, 1)
                : 0;
            period.Difference = period.PlannedBudget - period.ActualSpent;
        }

        await _db.SaveChangesAsync(ct);
    }
}
