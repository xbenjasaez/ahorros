namespace Ahorro.Models.Entities;

public class BudgetSubcategory : BaseEntity
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public BudgetCategory? Category { get; set; }
}
