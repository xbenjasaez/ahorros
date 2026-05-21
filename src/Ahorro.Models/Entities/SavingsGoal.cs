using Ahorro.Models.Enums;

namespace Ahorro.Models.Entities;

public class SavingsGoal : BaseEntity
{
    public Guid UserProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal AccumulatedAmount { get; set; }
    public DateTime? TargetDate { get; set; }
    public Guid? CategoryId { get; set; }
    public string ColorHex { get; set; } = "#35E0A1";
    public string IconKey { get; set; } = "target";
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    public bool AutoContributeFromBudget { get; set; }

    public UserProfile? UserProfile { get; set; }
    public BudgetCategory? Category { get; set; }
    public ICollection<GoalContribution> Contributions { get; set; } = [];
}
