using Ahorro.Models.Enums;

namespace Ahorro.Models.Entities;

public class ScheduledPayment : BaseEntity
{
    public Guid UserProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public decimal EstimatedAmount { get; set; }
    public DateTime DueDate { get; set; }
    public IncomeFrequency Frequency { get; set; } = IncomeFrequency.Monthly;
    public int ReminderDaysBefore { get; set; } = 3;
    public Guid PaymentMethodId { get; set; }
    public ScheduledPaymentStatus Status { get; set; } = ScheduledPaymentStatus.Pending;
    public DateTime? LastPaidDate { get; set; }

    public UserProfile? UserProfile { get; set; }
    public BudgetCategory? Category { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
}
