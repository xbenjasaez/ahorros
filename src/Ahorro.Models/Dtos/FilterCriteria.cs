using Ahorro.Models.Enums;

namespace Ahorro.Models.Dtos;

public class FilterCriteria
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public TransactionType? Type { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? SubcategoryId { get; set; }
    public Guid? PaymentMethodId { get; set; }
    public TransactionStatus? Status { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? SearchText { get; set; }
    public bool? IsRecurring { get; set; }
    public bool? HasGoal { get; set; }
    public Guid? SavingsGoalId { get; set; }
    public Guid? IncomeSourceId { get; set; }
    public bool? OverdueOnly { get; set; }
    public bool? ExceededOnly { get; set; }
    public Guid? BudgetPeriodId { get; set; }
}
