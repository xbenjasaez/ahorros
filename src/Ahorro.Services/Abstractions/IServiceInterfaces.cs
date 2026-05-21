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

public interface IBudgetService
{
    Task<List<BudgetAllocation>> GetAllocationsAsync(Guid periodId, CancellationToken ct = default);
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
    Task<MoneyTransaction> AddAsync(MoneyTransaction tx, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<MoneyTransaction?> DuplicateAsync(Guid id, CancellationToken ct = default);
}

public interface ISavingsGoalService
{
    Task<List<SavingsGoal>> GetActiveGoalsAsync(CancellationToken ct = default);
    Task ContributeAsync(Guid goalId, decimal amount, CancellationToken ct = default);
}

public interface IScheduledPaymentService
{
    Task<List<ScheduledPayment>> GetUpcomingAsync(int days = 30, CancellationToken ct = default);
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
    Task<List<BudgetCategory>> GetCategoriesAsync(CancellationToken ct = default);
}

public interface IExcelExportService
{
    Task<string> ExportTransactionsAsync(IEnumerable<MoneyTransaction> items, string folder, CancellationToken ct = default);
    Task<string> ExportBudgetAsync(Guid periodId, string folder, CancellationToken ct = default);
}

public interface IPdfExportService
{
    Task<string> ExportReportAsync(ReportData data, string folder, CancellationToken ct = default);
}

public interface IFilterPresetService
{
    Task SavePresetAsync(string name, FilterCriteria criteria, CancellationToken ct = default);
    Task<List<FilterPreset>> GetPresetsAsync(CancellationToken ct = default);
}

public interface ISyncService { Task SyncAsync(CancellationToken ct = default); }
public interface IBackupService { Task BackupAsync(CancellationToken ct = default); }

public record DashboardData(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal TotalSavings,
    decimal FreeBalance,
    decimal DebtPaid,
    decimal ExecutionPercent,
    List<CategoryComparisonItem> CategoryComparisons,
    List<CategoryDistributionItem> Distribution,
    List<TrendPoint> Trend,
    List<ScheduledPayment> UpcomingPayments,
    List<SavingsGoal> ActiveGoals,
    List<CriticalCategoryItem> CriticalCategories,
    List<MoneyTransaction> RecentTransactions,
    List<string> Alerts);

public record CategoryComparisonItem(string Category, decimal Planned, decimal Actual);
public record CategoryDistributionItem(string Category, decimal Amount, string Color);
public record TrendPoint(string Label, decimal Income, decimal Expense, decimal Savings);
public record CriticalCategoryItem(string Category, decimal UsedPercent, BudgetLineStatus Status);
public record ReportData(
    string PeriodLabel,
    List<CategoryDistributionItem> ByCategory,
    List<TrendPoint> Trend,
    decimal AccumulatedSavings,
    List<(string Description, decimal Amount)> TopExpenses);
