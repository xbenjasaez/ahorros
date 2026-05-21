using Ahorro.Models.Enums;

namespace Ahorro.Models.Entities;

public class BudgetPeriod : BaseEntity
{
    public Guid UserProfileId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public PeriodFrequency Frequency { get; set; } = PeriodFrequency.Monthly;
    public decimal TotalGrossIncome { get; set; }
    public decimal TotalNetIncome { get; set; }
    public decimal PlannedBudget { get; set; }
    public decimal ActualSpent { get; set; }
    public decimal Difference { get; set; }
    public decimal ExecutionPercent { get; set; }
    public bool IsClosed { get; set; }

    public UserProfile? UserProfile { get; set; }
    public ICollection<IncomeSource> Incomes { get; set; } = [];
    public ICollection<BudgetAllocation> Allocations { get; set; } = [];
    public ICollection<MoneyTransaction> Transactions { get; set; } = [];
}
