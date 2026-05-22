using Ahorro.Models.Enums;

namespace Ahorro.Models.Entities;

public class MoneyTransaction : BaseEntity
{
    public Guid BudgetPeriodId { get; set; }
    public DateTime Date { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid? SubcategoryId { get; set; }
    public decimal Amount { get; set; }
    public Guid PaymentMethodId { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Paid;
    public string? Note { get; set; }
    public string? Tag { get; set; }
    public bool IsRecurring { get; set; }
    public Guid? SavingsGoalId { get; set; }
    public Guid? IncomeSourceId { get; set; }
    public Guid? DebtId { get; set; }
    public Guid? CreditCardAccountId { get; set; }

    public BudgetPeriod? BudgetPeriod { get; set; }
    public BudgetCategory? Category { get; set; }
    public BudgetSubcategory? Subcategory { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public SavingsGoal? SavingsGoal { get; set; }
    public Debt? Debt { get; set; }
    public IncomeSource? IncomeSource { get; set; }
}
