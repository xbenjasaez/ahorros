using Ahorro.Models.Enums;

namespace Ahorro.Models.Entities;

public class ExportHistory : BaseEntity
{
    public Guid UserProfileId { get; set; }
    public ExportType ExportType { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? FilterSnapshotJson { get; set; }

    public UserProfile? UserProfile { get; set; }
}
