using System.Collections.ObjectModel;
using Ahorro.Helpers;
using Ahorro.Services.Abstractions;
using Ahorro.ViewModels.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ahorro.ViewModels.Goals;

public partial class GoalsViewModel : ViewModelBase, ILoadable
{
    private readonly ISavingsGoalService _goals;

    [ObservableProperty] private GoalCardItem? _selectedGoal;
    [ObservableProperty] private string _contributeAmount = "50000";
    public ObservableCollection<GoalCardItem> GoalCards { get; } = [];

    public GoalsViewModel(ISavingsGoalService goals)
    {
        Title = "Metas de ahorro";
        _goals = goals;
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        var list = await _goals.GetActiveGoalsAsync();
        GoalCards.Clear();
        foreach (var g in list)
        {
            var remaining = Math.Max(0, g.TargetAmount - g.AccumulatedAmount);
            var pct = g.TargetAmount > 0 ? (double)(g.AccumulatedAmount / g.TargetAmount * 100) : 0;
            var months = remaining > 0 ? Math.Ceiling(remaining / 50_000m) : 0;
            GoalCards.Add(new GoalCardItem
            {
                Id = g.Id,
                Name = g.Name,
                Accumulated = ClpFormatter.Format(g.AccumulatedAmount),
                Target = ClpFormatter.Format(g.TargetAmount),
                Remaining = ClpFormatter.Format(remaining),
                Progress = Math.Min(1, pct / 100),
                PercentText = $"{pct:0.#}%",
                TargetDate = g.TargetDate?.ToString("dd MMM yyyy") ?? "Sin fecha",
                Projection = months > 0 ? $"~{months:0} meses a $50.000/mes" : "Completada",
                ColorHex = g.ColorHex
            });
        }
        IsBusy = false;
    }

    [RelayCommand]
    private async Task Contribute(GoalCardItem? goal)
    {
        if (goal == null) return;
        if (!decimal.TryParse(ContributeAmount, out var amount)) amount = 50_000;
        await _goals.ContributeAsync(goal.Id, amount);
        await LoadAsync();
    }
}
