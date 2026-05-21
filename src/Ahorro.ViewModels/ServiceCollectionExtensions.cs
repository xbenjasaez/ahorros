using Ahorro.ViewModels.Budget;
using Ahorro.ViewModels.Dashboard;
using Ahorro.ViewModels.Goals;
using Ahorro.ViewModels.Payments;
using Ahorro.ViewModels.Reports;
using Ahorro.ViewModels.Settings;
using Ahorro.ViewModels.Shell;
using Ahorro.ViewModels.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Ahorro.ViewModels;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAhorroViewModels(this IServiceCollection services)
    {
        services.AddSingleton<MainShellViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<BudgetViewModel>();
        services.AddTransient<TransactionsViewModel>();
        services.AddTransient<GoalsViewModel>();
        services.AddTransient<PaymentsViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<SettingsViewModel>();
        return services;
    }
}
