using Ahorro.Repositories;
using Ahorro.Repositories.Abstractions;
using Ahorro.Models.Abstractions;
using Ahorro.Services.Abstractions;
using Ahorro.Services.Budget;
using Ahorro.Services.Dashboard;
using Ahorro.Services.Filters;
using Ahorro.Services.Goals;
using Ahorro.Services.Infrastructure;
using Ahorro.Services.Payments;
using Ahorro.Services.Reports;
using Ahorro.Services.Settings;
using Ahorro.Services.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Ahorro.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAhorroServices(this IServiceCollection services)
    {
        services.AddSingleton<ICurrentUserContext, LocalUserContext>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ISyncService, NoOpSyncService>();
        services.AddSingleton<IBackupService, NoOpBackupService>();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<TransactionRepository>();

        services.AddScoped<IBudgetPeriodService, BudgetPeriodService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IBudgetDistributionService, BudgetDistributionService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ISavingsGoalService, SavingsGoalService>();
        services.AddScoped<IScheduledPaymentService, ScheduledPaymentService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IFilterPresetService, FilterPresetService>();

        return services;
    }

    public static IServiceCollection AddAhorroViewModels(this IServiceCollection services)
    {
        services.AddSingleton<ViewModels.Shell.MainShellViewModel>();
        services.AddTransient<ViewModels.Dashboard.DashboardViewModel>();
        services.AddTransient<ViewModels.Budget.BudgetViewModel>();
        services.AddTransient<ViewModels.Transactions.TransactionsViewModel>();
        services.AddTransient<ViewModels.Goals.GoalsViewModel>();
        services.AddTransient<ViewModels.Payments.PaymentsViewModel>();
        services.AddTransient<ViewModels.Reports.ReportsViewModel>();
        services.AddTransient<ViewModels.Settings.SettingsViewModel>();
        return services;
    }
}
