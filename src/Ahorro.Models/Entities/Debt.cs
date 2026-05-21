using Ahorro.Models.Enums;

namespace Ahorro.Models.Entities;

public class Debt : BaseEntity
{
    public Guid UserProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal EstimatedInstallment { get; set; }
    public DateTime DueDate { get; set; }
    public decimal? InterestRate { get; set; }
    public DebtStatus Status { get; set; } = DebtStatus.Active;
    public int Priority { get; set; } = 1;
    public decimal PaidThisMonth { get; set; }
    public decimal RemainingBalance { get; set; }

    public UserProfile? UserProfile { get; set; }
}
