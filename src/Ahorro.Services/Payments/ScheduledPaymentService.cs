using Ahorro.Data;
using Ahorro.Models.Entities;
using Ahorro.Models.Enums;
using Ahorro.Models.Abstractions;
using Ahorro.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Ahorro.Services.Payments;

public class ScheduledPaymentService : IScheduledPaymentService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserContext _user;

    public ScheduledPaymentService(AppDbContext db, ICurrentUserContext user)
    {
        _db = db;
        _user = user;
    }

    public async Task<ScheduledPaymentSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        await RefreshStatusesAsync(ct);
        var monthEnd = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
        var payments = await _db.ScheduledPayments.AsNoTracking()
            .Where(p => p.UserProfileId == _user.UserId && p.Status != ScheduledPaymentStatus.Paid)
            .ToListAsync(ct);

        var dueThisMonth = payments
            .Where(p => p.DueDate.Date >= DateTime.Today && p.DueDate.Date <= monthEnd)
            .Sum(p => p.EstimatedAmount);

        return new ScheduledPaymentSummary(
            payments.Count,
            payments.Count(p => p.Status == ScheduledPaymentStatus.Overdue),
            payments.Count(p => p.Status == ScheduledPaymentStatus.Upcoming),
            payments.Count(p => p.Status == ScheduledPaymentStatus.Pending),
            dueThisMonth);
    }

    public async Task<List<ScheduledPayment>> GetAllAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        await RefreshStatusesAsync(ct);
        var query = _db.ScheduledPayments.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.PaymentMethod)
            .Where(p => p.UserProfileId == _user.UserId);

        if (from.HasValue)
            query = query.Where(p => p.DueDate.Date >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(p => p.DueDate.Date <= to.Value.Date);

        return await query.OrderBy(p => p.DueDate).ToListAsync(ct);
    }

    public async Task<List<ScheduledPayment>> GetUpcomingAsync(int days = 30, CancellationToken ct = default)
    {
        await RefreshStatusesAsync(ct);
        var limit = DateTime.Today.AddDays(days);
        return await _db.ScheduledPayments.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.PaymentMethod)
            .Where(p => p.UserProfileId == _user.UserId && p.DueDate <= limit && p.Status != ScheduledPaymentStatus.Paid)
            .OrderBy(p => p.DueDate)
            .ToListAsync(ct);
    }

    public Task<ScheduledPayment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.ScheduledPayments.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.PaymentMethod)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserProfileId == _user.UserId, ct);

    public async Task<ScheduledPayment> CreateAsync(ScheduledPaymentUpsert data, CancellationToken ct = default)
    {
        var payment = new ScheduledPayment
        {
            UserProfileId = _user.UserId,
            Name = data.Name.Trim(),
            CategoryId = data.CategoryId,
            EstimatedAmount = data.EstimatedAmount,
            DueDate = data.DueDate.Date,
            Frequency = data.Frequency,
            ReminderDaysBefore = Math.Max(0, data.ReminderDaysBefore),
            PaymentMethodId = data.PaymentMethodId,
            Status = ScheduledPaymentStatus.Pending
        };
        _db.ScheduledPayments.Add(payment);
        await _db.SaveChangesAsync(ct);
        await RefreshStatusesAsync(ct);
        return payment;
    }

    public async Task UpdateAsync(Guid id, ScheduledPaymentUpsert data, CancellationToken ct = default)
    {
        var payment = await _db.ScheduledPayments
            .FirstOrDefaultAsync(p => p.Id == id && p.UserProfileId == _user.UserId, ct);
        if (payment == null) return;

        payment.Name = data.Name.Trim();
        payment.CategoryId = data.CategoryId;
        payment.EstimatedAmount = data.EstimatedAmount;
        payment.DueDate = data.DueDate.Date;
        payment.Frequency = data.Frequency;
        payment.ReminderDaysBefore = Math.Max(0, data.ReminderDaysBefore);
        payment.PaymentMethodId = data.PaymentMethodId;
        await _db.SaveChangesAsync(ct);
        await RefreshStatusesAsync(ct);
    }

    public async Task RegisterPaymentAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _db.ScheduledPayments
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserProfileId == _user.UserId, ct);
        if (payment == null) return;

        payment.LastPaidDate = DateTime.Today;

        if (payment.Frequency == IncomeFrequency.OneTime)
        {
            payment.Status = ScheduledPaymentStatus.Paid;
        }
        else
        {
            payment.DueDate = AdvanceDueDate(payment.DueDate, payment.Frequency);
            payment.Status = ScheduledPaymentStatus.Pending;
        }

        await _db.SaveChangesAsync(ct);
        await RefreshStatusesAsync(ct);
    }

    public async Task RefreshStatusesAsync(CancellationToken ct = default)
    {
        var payments = await _db.ScheduledPayments
            .Where(p => p.UserProfileId == _user.UserId && p.Status != ScheduledPaymentStatus.Paid)
            .ToListAsync(ct);

        foreach (var p in payments)
        {
            if (p.DueDate.Date < DateTime.Today)
                p.Status = ScheduledPaymentStatus.Overdue;
            else if (p.DueDate.Date <= DateTime.Today.AddDays(p.ReminderDaysBefore))
                p.Status = ScheduledPaymentStatus.Upcoming;
            else
                p.Status = ScheduledPaymentStatus.Pending;
        }
        await _db.SaveChangesAsync(ct);
    }

    private static DateTime AdvanceDueDate(DateTime due, IncomeFrequency frequency) => frequency switch
    {
        IncomeFrequency.Biweekly => due.AddDays(14),
        IncomeFrequency.Monthly => due.AddMonths(1),
        _ => due
    };
}
