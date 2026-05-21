namespace Ahorro.Models.Entities;

public class AppSetting : BaseEntity
{
    public Guid UserProfileId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public UserProfile? UserProfile { get; set; }
}
