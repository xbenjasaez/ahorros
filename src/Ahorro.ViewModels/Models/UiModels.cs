using Ahorro.Models.Enums;

namespace Ahorro.ViewModels.Models;

public class KpiCardModel
{
    public string Title { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string AccentColor { get; init; } = "#27D3FF";
}

public class BudgetLineItem
{
    public Guid AllocationId { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Subcategory { get; init; } = "—";
    public string AllocationMode { get; init; } = string.Empty;
    public string Planned { get; init; } = string.Empty;
    public string Actual { get; init; } = string.Empty;
    public string Difference { get; init; } = string.Empty;
    public decimal UsedPercent { get; init; }
    public BudgetLineStatus Status { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
}

public class TransactionRowItem
{
    public Guid Id { get; init; }
    public string Date { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Subcategory { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public string Amount { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Tags { get; init; } = string.Empty;
    public MoneyTransactionEntity? Entity { get; init; }
}

public class MoneyTransactionEntity
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionStatus Status { get; set; }
    public string? Note { get; set; }
    public string? Tag { get; set; }
    public bool IsRecurring { get; set; }
}

public class GoalCardItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Accumulated { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public string Remaining { get; init; } = string.Empty;
    public double Progress { get; init; }
    public string PercentText { get; init; } = string.Empty;
    public string TargetDate { get; init; } = string.Empty;
    public string Projection { get; init; } = string.Empty;
    public string ColorHex { get; init; } = "#35E0A1";
}

public class PaymentListItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Amount { get; init; } = string.Empty;
    public string DueDate { get; init; } = string.Empty;
    public ScheduledPaymentStatus Status { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
}

public class FilterChipItem
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public bool IsActive { get; set; }
}

public class PeriodOption
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
}

public class AlertItem
{
    public string Message { get; init; } = string.Empty;
}

public class RecentTransactionItem
{
    public string Date { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Amount { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
}

public class CriticalCategoryItem
{
    public string Category { get; init; } = string.Empty;
    public string UsedPercent { get; init; } = string.Empty;
    public BudgetLineStatus Status { get; init; }
}
