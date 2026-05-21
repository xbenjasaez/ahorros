namespace Ahorro.Models.Abstractions;

public interface ICurrentUserContext
{
    Guid UserId { get; }
    Guid? ActivePeriodId { get; set; }
    void SetUser(Guid userId);
}
