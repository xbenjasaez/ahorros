using Ahorro.Models.Enums;

namespace Ahorro.Models.Entities;

public class PaymentMethod : BaseEntity
{
    public Guid UserProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PaymentMethodType Type { get; set; }
    public Guid? CreditCardAccountId { get; set; }
    public bool IsActive { get; set; } = true;

    public UserProfile? UserProfile { get; set; }
    public CreditCardAccount? CreditCardAccount { get; set; }
}
