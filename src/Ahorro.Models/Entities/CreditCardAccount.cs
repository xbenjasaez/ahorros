namespace Ahorro.Models.Entities;

public class CreditCardAccount : BaseEntity
{
    public Guid UserProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal AvailableCredit { get; set; }
    public int BillingDueDay { get; set; } = 5;
    public decimal MinimumPayment { get; set; }
    public bool IsActive { get; set; } = true;

    public UserProfile? UserProfile { get; set; }
}
