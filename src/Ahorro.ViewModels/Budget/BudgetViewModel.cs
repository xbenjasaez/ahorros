using System.Collections.ObjectModel;
using Ahorro.Helpers;
using Ahorro.Models.Abstractions;
using Ahorro.Services.Abstractions;
using Ahorro.ViewModels.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ahorro.ViewModels.Budget;

public partial class BudgetViewModel : ViewModelBase, ILoadable
{
    private readonly IBudgetService _budget;
    private readonly IBudgetDistributionService _distribution;
    private readonly IBudgetPeriodService _periods;
    private readonly ICurrentUserContext _user;

    [ObservableProperty] private string _grossIncome = "$0";
    [ObservableProperty] private string _netIncome = "$0";
    [ObservableProperty] private decimal _needsPercent = 50;
    [ObservableProperty] private decimal _wantsPercent = 30;
    [ObservableProperty] private decimal _savingsPercent = 20;
    public ObservableCollection<BudgetLineItem> Lines { get; } = [];

    public BudgetViewModel(IBudgetService budget, IBudgetDistributionService distribution, IBudgetPeriodService periods, ICurrentUserContext user)
    {
        Title = "Presupuesto";
        _budget = budget;
        _distribution = distribution;
        _periods = periods;
        _user = user;
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        var period = await _periods.GetActivePeriodAsync();
        if (period == null) { IsBusy = false; return; }

        GrossIncome = ClpFormatter.Format(period.TotalGrossIncome);
        NetIncome = ClpFormatter.Format(period.TotalNetIncome);

        var allocations = await _budget.GetAllocationsAsync(period.Id);
        Lines.Clear();
        foreach (var a in allocations)
        {
            Lines.Add(new BudgetLineItem
            {
                AllocationId = a.Id,
                Category = a.Category?.Name ?? "—",
                Subcategory = a.Subcategory?.Name ?? "—",
                AllocationMode = a.AllocationMode.ToString(),
                Planned = ClpFormatter.Format(a.PlannedAmount),
                Actual = ClpFormatter.Format(a.ActualAmount),
                Difference = ClpFormatter.Format(a.Difference),
                UsedPercent = a.UsedPercent,
                Status = a.Status,
                StatusLabel = BudgetStatusCalculator.StatusLabel(a.Status)
            });
        }
        IsBusy = false;
    }

    [RelayCommand]
    private async Task RecalculateDistribution()
    {
        var period = await _periods.GetActivePeriodAsync();
        if (period == null) return;
        await _distribution.ApplyRule503020Async(period.Id, NeedsPercent, WantsPercent, SavingsPercent);
        await _budget.RecalculateStatusesAsync(period.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DuplicatePreviousMonth()
    {
        var period = await _periods.GetActivePeriodAsync();
        if (period == null) return;
        await _budget.DuplicatePreviousPeriodAsync(period.Id);
        await LoadAsync();
    }
}
