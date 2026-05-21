namespace Ahorro.Models.Entities;

public class AlertRule : BaseEntity
{
    public Guid UserProfileId { get; set; }
    public Guid? CategoryId { get; set; }
    public int AttentionThreshold { get; set; } = 80;
    public int LimitThreshold { get; set; } = 100;
    public bool IsEnabled { get; set; } = true;

    public UserProfile? UserProfile { get; set; }
    public BudgetCategory? Category { get; set; }
}
