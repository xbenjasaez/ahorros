using Ahorro.Models.Abstractions;

namespace Ahorro.Services.Infrastructure;

public class LocalUserContext : ICurrentUserContext
{
    public Guid UserId { get; private set; }
    public Guid? ActivePeriodId { get; set; }

    public void SetUser(Guid userId) => UserId = userId;
}
