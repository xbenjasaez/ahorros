using System.Windows.Media;
using Ahorro.Models.Enums;

namespace Ahorro.ViewModels.Models;

public class KpiCardModel
{
    public string Title { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string AccentColor { get; init; } = "#27D3FF";
    public Brush AccentBrush { get; init; } = Brushes.Cyan;
}

public class BudgetLineItem
{
    public Guid AllocationId { get; init; }
    public Guid CategoryId { get; init; }
    public Guid? SubcategoryId { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Subcategory { get; init; } = "—";
    public string AllocationMode { get; init; } = string.Empty;
    public string Planned { get; init; } = string.Empty;
    public string Actual { get; init; } = string.Empty;
    public string Difference { get; init; } = string.Empty;
    public decimal UsedPercent { get; init; }
    public string UsedPercentText { get; init; } = string.Empty;
    public BudgetLineStatus Status { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
    public Brush StatusBrush { get; init; } = Brushes.Gray;
    public Brush ProgressBrush { get; init; } = Brushes.Gray;
    public Brush CategoryBrush { get; init; } = Brushes.Cyan;
    public bool IsAlert { get; init; }
}

public class CategoryPickerItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public class BudgetGroupOption
{
    public BudgetGroup Group { get; init; }
    public string Label { get; init; } = string.Empty;
}

public class TransactionRowItem
{
    public Guid Id { get; init; }
    public DateTime DateValue { get; init; }
    public string Date { get; init; } = string.Empty;
    public TransactionType TypeValue { get; init; }
    public string Type { get; init; } = string.Empty;
    public string TypeColor { get; init; } = "#E8EDF5";
    public string Description { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public string Category { get; init; } = string.Empty;
    public Guid? SubcategoryId { get; init; }
    public string Subcategory { get; init; } = string.Empty;
    public Guid PaymentMethodId { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public decimal AmountValue { get; init; }
    public string Amount { get; init; } = string.Empty;
    public string AmountColor { get; init; } = "#E8EDF5";
    public TransactionStatus StatusValue { get; init; }
    public string Status { get; init; } = string.Empty;
    public string StatusColor { get; init; } = "#93A4BD";
    public string Tags { get; init; } = string.Empty;
    public bool IsRecurring { get; init; }
    public Guid? SavingsGoalId { get; init; }
    public string? GoalName { get; init; }
    public string? Note { get; init; }
    public string? Tag { get; init; }
    public Guid BudgetPeriodId { get; init; }
}

public class LookupItem
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public class EnumLookupItem<T> where T : struct, Enum
{
    public T? Value { get; init; }
    public string Label { get; init; } = string.Empty;
}

public class ActiveFilterChipItem
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
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
    public string IconGlyph { get; init; } = "◎";
    public string IconKey { get; init; } = "target";
    public string Accumulated { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public string Remaining { get; init; } = string.Empty;
    public string RemainingLabel { get; init; } = "Te faltan";
    public double Progress { get; init; }
    public string PercentText { get; init; } = string.Empty;
    public string TargetDate { get; init; } = string.Empty;
    public string DaysLeftLabel { get; init; } = string.Empty;
    public string Projection { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public bool HasCategoryLink { get; init; }
    public bool IsCompleted { get; init; }
    public string ColorHex { get; init; } = "#35E0A1";
    public Brush AccentBrush { get; init; } = Brushes.LimeGreen;
    public Brush GlowBrush { get; init; } = Brushes.LimeGreen;
    public Brush TrackBrush { get; init; } = Brushes.DarkGray;
}

public class GoalColorPreset
{
    public string Hex { get; init; } = "#35E0A1";
    public Brush Swatch { get; init; } = Brushes.LimeGreen;
}

public class PaymentListItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public string Amount { get; init; } = string.Empty;
    public DateTime DueDateValue { get; init; }
    public string DueDate { get; init; } = string.Empty;
    public string DaysLabel { get; init; } = string.Empty;
    public int DaysUntilDue { get; init; }
    public string FrequencyLabel { get; init; } = string.Empty;
    public string ReminderLabel { get; init; } = string.Empty;
    public string LastPaidLabel { get; init; } = string.Empty;
    public bool IsRecurring { get; init; }
    public ScheduledPaymentStatus Status { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
    public string StatusColor { get; init; } = "#93A4BD";
    public Brush StatusBrush { get; init; } = Brushes.Gray;
    public Brush CategoryBrush { get; init; } = Brushes.Cyan;
    public bool CanRegister { get; init; }
}

public class PaymentStatusFilterItem
{
    public ScheduledPaymentStatus? Status { get; init; }
    public string Label { get; init; } = string.Empty;
}

public class PaymentCalendarDayItem
{
    public DateTime Date { get; init; }
    public int DayNumber { get; init; }
    public string WeekdayShort { get; init; } = string.Empty;
    public bool IsToday { get; init; }
    public bool IsCurrentMonth { get; init; }
    public bool HasPayments { get; init; }
    public int PaymentCount { get; init; }
    public string Tooltip { get; init; } = string.Empty;
    public Brush AccentBrush { get; init; } = Brushes.Transparent;
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

public class ReportTopExpenseItem
{
    public int Rank { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Amount { get; init; } = string.Empty;
}

public class ReportExceededItem
{
    public string Category { get; init; } = string.Empty;
    public string Planned { get; init; } = string.Empty;
    public string Actual { get; init; } = string.Empty;
    public string UsedPercent { get; init; } = string.Empty;
    public Brush AccentBrush { get; init; } = Brushes.Transparent;
}

public class SettingsCategoryItem
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string GroupLabel { get; init; } = string.Empty;
    public BudgetGroup Group { get; init; }
    public string ColorHex { get; set; } = "#27D3FF";
    public Brush ColorBrush { get; init; } = Brushes.Cyan;
    public string IconKey { get; set; } = "folder";
    public bool AllowRollover { get; set; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public int SubcategoryCount { get; init; }
    public string StatusLabel => IsActive ? "Activa" : "Inactiva";
}

public class SettingsSubcategoryItem
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; init; }
    public string StatusLabel => IsActive ? "Activa" : "Inactiva";
}

public class SettingsPaymentMethodItem
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public PaymentMethodType Type { get; set; }
    public string TypeLabel { get; init; } = string.Empty;
    public Guid? CreditCardAccountId { get; set; }
    public string CreditCardName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public string StatusLabel => IsActive ? "Activo" : "Inactivo";
}

public class SettingsUserItem
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsLocal { get; init; }
    public string Badge => IsCurrent ? "Activo" : IsLocal ? "Local" : "Perfil";
}

public class SettingsExportHistoryItem
{
    public Guid Id { get; init; }
    public string TypeLabel { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string CreatedLabel { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
}

public class ThemeVariantOption
{
    public string Id { get; init; } = "dark-premium";
    public string Label { get; init; } = string.Empty;
}

public class AccentColorOption
{
    public string Hex { get; init; } = "#27D3FF";
    public Brush Swatch { get; init; } = Brushes.Cyan;
}

public class CurrencyOption
{
    public string Code { get; init; } = "CLP";
    public string Label { get; init; } = string.Empty;
}
