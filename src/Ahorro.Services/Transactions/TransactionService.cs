using Ahorro.Data;
using Ahorro.Models.Abstractions;
using Ahorro.Models.Dtos;
using Ahorro.Models.Entities;
using Ahorro.Models.Enums;
using Ahorro.Repositories;
using Ahorro.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ahorro.Services.Transactions;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _db;
    private readonly TransactionRepository _repo;
    private readonly IBudgetService _budget;
    private readonly ICurrentUserContext _user;

    public TransactionService(
        AppDbContext db,
        TransactionRepository repo,
        IBudgetService budget,
        ICurrentUserContext user)
    {
        _db = db;
        _repo = repo;
        _budget = budget;
        _user = user;
    }

    public Task<List<MoneyTransaction>> GetFilteredAsync(FilterCriteria criteria, CancellationToken ct = default) =>
        _repo.GetFilteredAsync(criteria, ct);

    public Task<MoneyTransaction?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Transactions.AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Subcategory)
            .Include(t => t.PaymentMethod)
            .Include(t => t.SavingsGoal)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<List<PaymentMethod>> GetPaymentMethodsAsync(CancellationToken ct = default) =>
        _db.PaymentMethods.AsNoTracking()
            .Where(m => m.UserProfileId == _user.UserId)
            .OrderBy(m => m.Name)
            .ToListAsync(ct);

    public async Task<MoneyTransaction> AddAsync(MoneyTransaction tx, CancellationToken ct = default)
    {
        _db.Transactions.Add(tx);
        await _db.SaveChangesAsync(ct);
        await _budget.RecalculateStatusesAsync(tx.BudgetPeriodId, ct);
        return tx;
    }

    public async Task<MoneyTransaction> UpdateAsync(MoneyTransaction tx, CancellationToken ct = default)
    {
        var existing = await _db.Transactions.FindAsync([tx.Id], ct);
        if (existing == null) return tx;

        var previousPeriodId = existing.BudgetPeriodId;
        existing.Date = tx.Date;
        existing.Type = tx.Type;
        existing.Description = tx.Description;
        existing.CategoryId = tx.CategoryId;
        existing.SubcategoryId = tx.SubcategoryId;
        existing.Amount = tx.Amount;
        existing.PaymentMethodId = tx.PaymentMethodId;
        existing.Status = tx.Status;
        existing.Note = tx.Note;
        existing.Tag = tx.Tag;
        existing.IsRecurring = tx.IsRecurring;
        existing.SavingsGoalId = tx.SavingsGoalId;
        existing.BudgetPeriodId = tx.BudgetPeriodId;

        await _db.SaveChangesAsync(ct);
        await _budget.RecalculateStatusesAsync(previousPeriodId, ct);
        if (existing.BudgetPeriodId != previousPeriodId)
            await _budget.RecalculateStatusesAsync(existing.BudgetPeriodId, ct);

        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tx = await _db.Transactions.FindAsync([id], ct);
        if (tx == null) return;

        var periodId = tx.BudgetPeriodId;
        _db.Transactions.Remove(tx);
        await _db.SaveChangesAsync(ct);
        await _budget.RecalculateStatusesAsync(periodId, ct);
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

    public async Task MarkPaidAsync(Guid id, CancellationToken ct = default)
    {
        var tx = await _db.Transactions.FindAsync([id], ct);
        if (tx == null) return;
        tx.Status = TransactionStatus.Paid;
        await _db.SaveChangesAsync(ct);
        await _budget.RecalculateStatusesAsync(tx.BudgetPeriodId, ct);
    }

    public async Task SetRecurringAsync(Guid id, bool isRecurring, CancellationToken ct = default)
    {
        var tx = await _db.Transactions.FindAsync([id], ct);
        if (tx == null) return;
        tx.IsRecurring = isRecurring;
        await _db.SaveChangesAsync(ct);
    }
}
