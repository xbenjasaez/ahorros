using Ahorro.Data;
using Ahorro.Models.Dtos;
using Ahorro.Models.Entities;
using Ahorro.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ahorro.Repositories;

public class TransactionRepository
{
    private readonly AppDbContext _context;

    public TransactionRepository(AppDbContext context) => _context = context;

    public async Task<List<MoneyTransaction>> GetFilteredAsync(FilterCriteria criteria, CancellationToken ct = default)
    {
        var query = _context.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Subcategory)
            .Include(t => t.PaymentMethod)
            .AsQueryable();

        if (criteria.BudgetPeriodId.HasValue)
            query = query.Where(t => t.BudgetPeriodId == criteria.BudgetPeriodId);
        if (criteria.DateFrom.HasValue)
            query = query.Where(t => t.Date >= criteria.DateFrom);
        if (criteria.DateTo.HasValue)
            query = query.Where(t => t.Date <= criteria.DateTo);
        if (criteria.Type.HasValue)
            query = query.Where(t => t.Type == criteria.Type);
        if (criteria.CategoryId.HasValue)
            query = query.Where(t => t.CategoryId == criteria.CategoryId);
        if (criteria.SubcategoryId.HasValue)
            query = query.Where(t => t.SubcategoryId == criteria.SubcategoryId);
        if (criteria.PaymentMethodId.HasValue)
            query = query.Where(t => t.PaymentMethodId == criteria.PaymentMethodId);
        if (criteria.Status.HasValue)
            query = query.Where(t => t.Status == criteria.Status);
        if (criteria.MinAmount.HasValue)
            query = query.Where(t => t.Amount >= criteria.MinAmount);
        if (criteria.MaxAmount.HasValue)
            query = query.Where(t => t.Amount <= criteria.MaxAmount);
        if (criteria.IsRecurring.HasValue)
            query = query.Where(t => t.IsRecurring == criteria.IsRecurring);
        if (criteria.HasGoal.HasValue)
            query = criteria.HasGoal.Value
                ? query.Where(t => t.SavingsGoalId != null)
                : query.Where(t => t.SavingsGoalId == null);
        if (criteria.IncomeSourceId.HasValue)
            query = query.Where(t => t.IncomeSourceId == criteria.IncomeSourceId);
        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var s = criteria.SearchText.Trim().ToLower();
            query = query.Where(t =>
                t.Description.ToLower().Contains(s) ||
                (t.Note != null && t.Note.ToLower().Contains(s)) ||
                (t.Tag != null && t.Tag.ToLower().Contains(s)));
        }

        return await query.OrderByDescending(t => t.Date).ToListAsync(ct);
    }
}
