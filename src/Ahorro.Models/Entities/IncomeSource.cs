using Ahorro.Models.Enums;

namespace Ahorro.Models.Entities;

public class IncomeSource : BaseEntity
{
    public Guid UserProfileId { get; set; }
    public Guid? BudgetPeriodId { get; set; }
    public string Name { get; set; } = string.Empty;
    public IncomeType Type { get; set; } = IncomeType.Salary;
    public decimal GrossAmount { get; set; }
    public decimal NetAmount { get; set; }
    public DateTime Date { get; set; }
    public IncomeFrequency Frequency { get; set; } = IncomeFrequency.Monthly;
    public string? Notes { get; set; }

    public UserProfile? UserProfile { get; set; }
    public BudgetPeriod? BudgetPeriod { get; set; }
}
