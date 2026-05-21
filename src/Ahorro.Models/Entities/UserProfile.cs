using Ahorro.Models.Enums;

namespace Ahorro.Models.Entities;

public class UserProfile : BaseEntity
{
    public string DisplayName { get; set; } = "Usuario Local";
    public string? Email { get; set; }
    public bool IsLocal { get; set; } = true;
    public int CutoffDay { get; set; } = 25;
    public PeriodFrequency DefaultFrequency { get; set; } = PeriodFrequency.Monthly;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BudgetPeriod> BudgetPeriods { get; set; } = [];
    public ICollection<BudgetCategory> Categories { get; set; } = [];
    public ICollection<IncomeSource> IncomeSources { get; set; } = [];
    public ICollection<SavingsGoal> SavingsGoals { get; set; } = [];
    public ICollection<ScheduledPayment> ScheduledPayments { get; set; } = [];
    public ICollection<Debt> Debts { get; set; } = [];
    public ICollection<CreditCardAccount> CreditCards { get; set; } = [];
    public ICollection<PaymentMethod> PaymentMethods { get; set; } = [];
    public ICollection<AlertRule> AlertRules { get; set; } = [];
    public ICollection<AppSetting> Settings { get; set; } = [];
    public ICollection<ExportHistory> ExportHistories { get; set; } = [];
}
