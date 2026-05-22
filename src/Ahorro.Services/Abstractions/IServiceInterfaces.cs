using Ahorro.Models.Dtos;
using Ahorro.Models.Entities;
using Ahorro.Models.Enums;

namespace Ahorro.Services.Abstractions;

public interface IBudgetPeriodService
{
    Task<BudgetPeriod?> GetActivePeriodAsync(CancellationToken ct = default);
    Task<List<BudgetPeriod>> GetPeriodsAsync(CancellationToken ct = default);
    Task<BudgetPeriod> EnsureActivePeriodAsync(CancellationToken ct = default);
    Task<BudgetPeriod?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

public record BudgetPeriodSummary(
    decimal GrossIncome,
    decimal NetIncome,
    decimal TotalPlanned,
    decimal TotalActual,
    decimal Remaining,
    decimal ExecutionPercent);

public interface IBudgetService
{
    Task<List<BudgetAllocation>> GetAllocationsAsync(Guid periodId, CancellationToken ct = default);
    Task<BudgetPeriodSummary> GetSummaryAsync(Guid periodId, CancellationToken ct = default);
    Task<List<BudgetCategory>> GetCategoriesAsync(CancellationToken ct = default);
    Task<BudgetCategory> AddCategoryAsync(string name, BudgetGroup group, Guid periodId, CancellationToken ct = default);
    Task<BudgetSubcategory> AddSubcategoryAsync(Guid categoryId, string name, Guid periodId, CancellationToken ct = default);
    Task UpdatePeriodIncomeAsync(Guid periodId, decimal grossIncome, decimal netIncome, CancellationToken ct = default);
    Task DuplicatePreviousPeriodAsync(Guid periodId, CancellationToken ct = default);
    Task RecalculateStatusesAsync(Guid periodId, CancellationToken ct = default);
}

public interface IBudgetDistributionService
{
    Task ApplyRule503020Async(Guid periodId, decimal needsPercent, decimal wantsPercent, decimal savingsPercent, CancellationToken ct = default);
}

public interface ITransactionService
{
    Task<List<MoneyTransaction>> GetFilteredAsync(FilterCriteria criteria, CancellationToken ct = default);
    Task<MoneyTransaction?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<PaymentMethod>> GetPaymentMethodsAsync(CancellationToken ct = default);
    Task<MoneyTransaction> AddAsync(MoneyTransaction tx, CancellationToken ct = default);
    Task<MoneyTransaction> UpdateAsync(MoneyTransaction tx, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<MoneyTransaction?> DuplicateAsync(Guid id, CancellationToken ct = default);
    Task MarkPaidAsync(Guid id, CancellationToken ct = default);
    Task SetRecurringAsync(Guid id, bool isRecurring, CancellationToken ct = default);
}

public record GoalsDashboardSummary(
    decimal TotalSaved,
    int ActiveGoalsCount,
    decimal TotalTarget,
    decimal TotalRemaining,
    string ProjectionLabel);

public record SavingsGoalUpdate(
    string Name,
    decimal TargetAmount,
    DateTime? TargetDate,
    Guid? CategoryId,
    string ColorHex,
    string IconKey,
    bool AutoContributeFromBudget);

public interface ISavingsGoalService
{
    Task<List<SavingsGoal>> GetActiveGoalsAsync(CancellationToken ct = default);
    Task<GoalsDashboardSummary> GetSummaryAsync(CancellationToken ct = default);
    Task<SavingsGoal?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<GoalContribution>> GetRecentContributionsAsync(Guid goalId, int limit = 8, CancellationToken ct = default);
    Task<SavingsGoal> CreateAsync(SavingsGoalUpdate data, CancellationToken ct = default);
    Task UpdateAsync(Guid id, SavingsGoalUpdate data, CancellationToken ct = default);
    Task ContributeAsync(Guid goalId, decimal amount, CancellationToken ct = default);
    Task ArchiveAsync(Guid goalId, CancellationToken ct = default);
}

public record ScheduledPaymentSummary(
    int TotalActive,
    int OverdueCount,
    int UpcomingCount,
    int PendingCount,
    decimal TotalDueThisMonth);

public record ScheduledPaymentUpsert(
    string Name,
    Guid CategoryId,
    decimal EstimatedAmount,
    DateTime DueDate,
    IncomeFrequency Frequency,
    int ReminderDaysBefore,
    Guid PaymentMethodId);

public interface IScheduledPaymentService
{
    Task<ScheduledPaymentSummary> GetSummaryAsync(CancellationToken ct = default);
    Task<List<ScheduledPayment>> GetAllAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<List<ScheduledPayment>> GetUpcomingAsync(int days = 30, CancellationToken ct = default);
    Task<ScheduledPayment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ScheduledPayment> CreateAsync(ScheduledPaymentUpsert data, CancellationToken ct = default);
    Task UpdateAsync(Guid id, ScheduledPaymentUpsert data, CancellationToken ct = default);
    Task RegisterPaymentAsync(Guid paymentId, CancellationToken ct = default);
    Task RefreshStatusesAsync(CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<DashboardData> LoadAsync(Guid periodId, CancellationToken ct = default);
}

public interface IReportService
{
    Task<ReportData> LoadAsync(Guid periodId, CancellationToken ct = default);
}

public interface ISettingsService
{
    Task<UserProfile> GetProfileAsync(CancellationToken ct = default);
    Task SaveProfileAsync(UserProfile profile, CancellationToken ct = default);
    Task<List<UserProfile>> GetProfilesAsync(CancellationToken ct = default);
    Task SwitchProfileAsync(Guid userId, CancellationToken ct = default);
    Task<UserPreferences> GetPreferencesAsync(CancellationToken ct = default);
    Task SavePreferencesAsync(UserPreferences preferences, CancellationToken ct = default);
    Task<AlertRule> GetGlobalAlertRuleAsync(CancellationToken ct = default);
    Task SaveGlobalAlertRuleAsync(int attentionThreshold, int limitThreshold, bool isEnabled, CancellationToken ct = default);
    Task<List<BudgetCategory>> GetCategoriesAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<BudgetCategory> AddCategoryAsync(CategoryUpsert data, CancellationToken ct = default);
    Task UpdateCategoryAsync(Guid id, CategoryUpsert data, CancellationToken ct = default);
    Task SetCategoryActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
    Task ReorderCategoryAsync(Guid id, bool moveUp, CancellationToken ct = default);
    Task<BudgetSubcategory> AddSubcategoryAsync(Guid categoryId, string name, CancellationToken ct = default);
    Task UpdateSubcategoryAsync(Guid id, string name, CancellationToken ct = default);
    Task SetSubcategoryActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
    Task<List<PaymentMethod>> GetPaymentMethodsAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<List<CreditCardAccount>> GetCreditCardsAsync(CancellationToken ct = default);
    Task<PaymentMethod> SavePaymentMethodAsync(PaymentMethodUpsert data, CancellationToken ct = default);
    Task SetPaymentMethodActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
    Task<List<ExportHistory>> GetExportHistoryAsync(int take = 20, CancellationToken ct = default);
}

public record CategoryUpsert(
    string Name,
    BudgetGroup DefaultGroup,
    string ColorHex,
    string IconKey,
    bool AllowRollover,
    AllocationMode AllocationMode = AllocationMode.Percentage);

public record PaymentMethodUpsert(
    Guid? Id,
    string Name,
    PaymentMethodType Type,
    Guid? CreditCardAccountId,
    bool IsActive = true);

public interface IExcelExportService
{
    Task<string> ExportTransactionsAsync(IEnumerable<MoneyTransaction> items, string folder, CancellationToken ct = default);
    Task<string> ExportBudgetAsync(Guid periodId, string folder, CancellationToken ct = default);
    Task<string> ExportGoalsAsync(IEnumerable<SavingsGoal> goals, string folder, CancellationToken ct = default);
}

public interface IPdfExportService
{
    Task<string> ExportReportAsync(ReportData data, string folder, CancellationToken ct = default);
    Task<string> ExportBudgetAsync(Guid periodId, string folder, CancellationToken ct = default);
    Task<string> ExportGoalsAsync(IEnumerable<SavingsGoal> goals, string folder, CancellationToken ct = default);
}

public interface IFilterPresetService
{
    Task SavePresetAsync(string name, FilterCriteria criteria, CancellationToken ct = default);
    Task<List<FilterPreset>> GetPresetsAsync(CancellationToken ct = default);
}

public interface ISyncService { Task SyncAsync(CancellationToken ct = default); }
public interface IBackupService { Task BackupAsync(CancellationToken ct = default); }

public record DashboardAlert(string Title, string Message, string Severity);

public record DashboardData(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal TotalSavings,
    decimal FreeBalance,
    decimal DebtPaid,
    decimal ExecutionPercent,
    decimal TotalPlanned,
    decimal TotalActual,
    List<CategoryComparisonItem> CategoryComparisons,
    List<CategoryDistributionItem> Distribution,
    List<TrendPoint> Trend,
    List<ScheduledPayment> UpcomingPayments,
    List<SavingsGoal> ActiveGoals,
    List<CriticalCategoryItem> CriticalCategories,
    List<MoneyTransaction> RecentTransactions,
    List<DashboardAlert> Alerts);

public record CategoryComparisonItem(string Category, decimal Planned, decimal Actual);
public record CategoryDistributionItem(string Category, decimal Amount, string Color);
public record TrendPoint(string Label, decimal Income, decimal Expense, decimal Savings);
public record CriticalCategoryItem(string Category, decimal UsedPercent, BudgetLineStatus Status);
public record ReportSummary(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal PeriodSavings,
    decimal FreeBalance,
    decimal ExecutionPercent);

public record ExceededCategoryItem(
    string Category,
    decimal Planned,
    decimal Actual,
    decimal UsedPercent,
    string ColorHex);

public record ReportData(
    Guid PeriodId,
    string PeriodLabel,
    ReportSummary Summary,
    List<CategoryDistributionItem> ByCategory,
    List<TrendPoint> Trend,
    decimal AccumulatedSavings,
    List<(string Description, decimal Amount)> TopExpenses,
    List<ExceededCategoryItem> ExceededCategories);
