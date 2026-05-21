using Ahorro.Data;
using Ahorro.Models.Dtos;
using Ahorro.Models.Entities;
using Ahorro.Repositories;
using Ahorro.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ahorro.Services.Transactions;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _db;
    private readonly TransactionRepository _repo;

    public TransactionService(AppDbContext db, TransactionRepository repo)
    {
        _db = db;
        _repo = repo;
    }

    public Task<List<MoneyTransaction>> GetFilteredAsync(FilterCriteria criteria, CancellationToken ct = default) =>
        _repo.GetFilteredAsync(criteria, ct);

    public async Task<MoneyTransaction> AddAsync(MoneyTransaction tx, CancellationToken ct = default)
    {
        _db.Transactions.Add(tx);
        await UpdateAllocationActualAsync(tx, ct);
        await _db.SaveChangesAsync(ct);
        return tx;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tx = await _db.Transactions.FindAsync([id], ct);
        if (tx != null)
        {
            _db.Transactions.Remove(tx);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<MoneyTransaction?> DuplicateAsync(Guid id, CancellationToken ct = default)
    {
        var original = await _db.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        if (original == null) return null;

        var copy = new MoneyTransaction
        {
            BudgetPeriodId = original.BudgetPeriodId,
            Date = DateTime.Today,
            Type = original.Type,
            Description = original.Description + " (copia)",
            CategoryId = original.CategoryId,
            SubcategoryId = original.SubcategoryId,
            Amount = original.Amount,
            PaymentMethodId = original.PaymentMethodId,
            Status = original.Status,
            Note = original.Note,
            Tag = original.Tag,
            IsRecurring = original.IsRecurring,
            SavingsGoalId = original.SavingsGoalId
        };
        return await AddAsync(copy, ct);
    }

    private async Task UpdateAllocationActualAsync(MoneyTransaction tx, CancellationToken ct)
    {
        if (tx.Type != Models.Enums.TransactionType.Expense) return;

        var alloc = await _db.BudgetAllocations
            .FirstOrDefaultAsync(a => a.BudgetPeriodId == tx.BudgetPeriodId && a.CategoryId == tx.CategoryId &&
                (a.SubcategoryId == null || a.SubcategoryId == tx.SubcategoryId), ct);
        if (alloc != null)
        {
            alloc.ActualAmount += tx.Amount;
            alloc.UsedPercent = alloc.PlannedAmount > 0 ? alloc.ActualAmount / alloc.PlannedAmount * 100 : 0;
            alloc.Difference = alloc.PlannedAmount - alloc.ActualAmount;
            alloc.Status = Helpers.BudgetStatusCalculator.FromUsedPercent(alloc.UsedPercent);
        }
    }
}
