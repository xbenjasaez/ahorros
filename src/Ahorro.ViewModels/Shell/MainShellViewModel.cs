using Ahorro.Models.Enums;
using Ahorro.Services.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ahorro.ViewModels.Shell;

public partial class MainShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly IServiceProvider _services;
    private readonly Func<NavigationPage, ViewModelBase> _vmFactory;

    [ObservableProperty] private ViewModelBase? _currentViewModel;
    [ObservableProperty] private string _appTitle = "AHORRO";
    [ObservableProperty] private NavigationPage _selectedPage = NavigationPage.Dashboard;

    public MainShellViewModel(
        INavigationService navigation,
        IServiceProvider services,
        Dashboard.DashboardViewModel dashboard)
    {
        _navigation = navigation;
        _services = services;
        _vmFactory = page => (ViewModelBase)_services.GetRequiredService(GetVmType(page));
        CurrentViewModel = dashboard;
        _navigation.PageChanged += OnPageChanged;
    }

    [RelayCommand]
    private void Navigate(string page)
    {
        if (!Enum.TryParse<NavigationPage>(page, out var navPage)) return;
        SelectedPage = navPage;
        _navigation.Navigate(navPage);
        CurrentViewModel = _vmFactory(navPage);
        if (CurrentViewModel is ILoadable loadable)
            _ = loadable.LoadAsync();
    }

    private void OnPageChanged(NavigationPage page)
    {
        SelectedPage = page;
        CurrentViewModel = _vmFactory(page);
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

public interface ILoadable
{
    Task LoadAsync();
}
