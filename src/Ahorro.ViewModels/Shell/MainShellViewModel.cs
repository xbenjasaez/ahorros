using Ahorro.Models.Enums;
using Ahorro.Services.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Ahorro.ViewModels.Shell;

public partial class MainShellViewModel : ViewModelBase, IDisposable
{
    private readonly INavigationService _navigation;
    private readonly IServiceScopeFactory _scopeFactory;
    private IServiceScope? _pageScope;

    [ObservableProperty] private ViewModelBase? _currentViewModel;
    [ObservableProperty] private string _appTitle = "AHORRO";
    [ObservableProperty] private NavigationPage _selectedPage = NavigationPage.Dashboard;

    public MainShellViewModel(INavigationService navigation, IServiceScopeFactory scopeFactory)
    {
        _navigation = navigation;
        _scopeFactory = scopeFactory;
        _pageScope = _scopeFactory.CreateScope();
        SelectedPage = NavigationPage.Dashboard;
        _navigation.Navigate(NavigationPage.Dashboard);
        CurrentViewModel = _pageScope.ServiceProvider.GetRequiredService<Dashboard.DashboardViewModel>();
    }

    [RelayCommand]
    private async Task NavigateAsync(string page)
    {
        if (!Enum.TryParse<NavigationPage>(page, out var navPage)) return;
        await ShowPageAsync(navPage);
    }

    private async Task ShowPageAsync(NavigationPage page)
    {
        if (SelectedPage == page && CurrentViewModel is not null)
            return;

        _pageScope?.Dispose();
        _pageScope = _scopeFactory.CreateScope();

        SelectedPage = page;
        _navigation.Navigate(page);

        CurrentViewModel = _pageScope.ServiceProvider.GetRequiredService(GetVmType(page)) as ViewModelBase;

        if (CurrentViewModel is ILoadable loadable)
            await loadable.LoadAsync();
    }

    public void Dispose()
    {
        _pageScope?.Dispose();
        _pageScope = null;
    }

    private static Type GetVmType(NavigationPage page) => page switch
    {
        NavigationPage.Dashboard => typeof(Dashboard.DashboardViewModel),
        NavigationPage.Budget => typeof(Budget.BudgetViewModel),
        NavigationPage.Transactions => typeof(Transactions.TransactionsViewModel),
        NavigationPage.Goals => typeof(Goals.GoalsViewModel),
        NavigationPage.Payments => typeof(Payments.PaymentsViewModel),
        NavigationPage.Reports => typeof(Reports.ReportsViewModel),
        NavigationPage.Settings => typeof(Settings.SettingsViewModel),
        _ => typeof(Dashboard.DashboardViewModel)
    };
}
