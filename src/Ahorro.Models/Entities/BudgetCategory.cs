using Ahorro.Models.Enums;

namespace Ahorro.Models.Entities;

public class BudgetCategory : BaseEntity
{
    public Guid UserProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#27D3FF";
    public string IconKey { get; set; } = "folder";
    public AllocationMode AllocationMode { get; set; } = AllocationMode.Percentage;
    public decimal? LimitAmount { get; set; }
    public bool AllowRollover { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public BudgetGroup DefaultGroup { get; set; } = BudgetGroup.Other;

    public UserProfile? UserProfile { get; set; }
    public ICollection<BudgetSubcategory> Subcategories { get; set; } = [];
    public ICollection<BudgetAllocation> Allocations { get; set; } = [];
}
