using Ahorro.Models.Enums;

namespace Ahorro.Models.Entities;

public class BudgetAllocation : BaseEntity
{
    public Guid BudgetPeriodId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? SubcategoryId { get; set; }
    public AllocationMode AllocationMode { get; set; }
    public decimal PlannedAmount { get; set; }
    public decimal PlannedPercent { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Difference { get; set; }
    public decimal UsedPercent { get; set; }
    public decimal RolloverFromPrevious { get; set; }
    public BudgetLineStatus Status { get; set; } = BudgetLineStatus.Normal;

    public BudgetPeriod? BudgetPeriod { get; set; }
    public BudgetCategory? Category { get; set; }
    public BudgetSubcategory? Subcategory { get; set; }
}
