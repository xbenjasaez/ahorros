namespace Ahorro.Models.Entities;

public class FilterPreset : BaseEntity
{
    public Guid UserProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilterJson { get; set; } = "{}";
}
