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

    public async Task<List<ScheduledPayment>> GetUpcomingAsync(int days = 30, CancellationToken ct = default)
    {
        await RefreshStatusesAsync(ct);
        var limit = DateTime.Today.AddDays(days);
        return await _db.ScheduledPayments.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.UserProfileId == _user.UserId && p.DueDate <= limit && p.Status != ScheduledPaymentStatus.Paid)
            .OrderBy(p => p.DueDate)
            .ToListAsync(ct);
    }

    public async Task RegisterPaymentAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _db.ScheduledPayments.FindAsync([paymentId], ct);
        if (payment == null) return;

        payment.Status = ScheduledPaymentStatus.Paid;
        payment.LastPaidDate = DateTime.Today;
        payment.DueDate = payment.DueDate.AddMonths(1);
        payment.Status = ScheduledPaymentStatus.Pending;
        await _db.SaveChangesAsync(ct);
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
}
