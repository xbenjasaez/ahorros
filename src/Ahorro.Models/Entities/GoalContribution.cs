namespace Ahorro.Models.Entities;

public class GoalContribution : BaseEntity
{
    public Guid GoalId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public bool IsAutomatic { get; set; }
    public string? Note { get; set; }

    public SavingsGoal? Goal { get; set; }
}
