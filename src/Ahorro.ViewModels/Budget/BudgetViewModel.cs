using System.Collections.ObjectModel;
using Ahorro.Helpers;
using Ahorro.Models.Abstractions;
using Ahorro.Models.Entities;
using Ahorro.Models.Enums;
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
    private Guid? _activePeriodId;

    [ObservableProperty] private string _periodLabel = string.Empty;
    [ObservableProperty] private PeriodOption? _selectedPeriodOption;
    [ObservableProperty] private string _grossIncome = "$0";
    [ObservableProperty] private string _netIncome = "$0";
    [ObservableProperty] private string _grossIncomeInput = "0";
    [ObservableProperty] private string _netIncomeInput = "0";
    [ObservableProperty] private string _totalPlanned = "$0";
    [ObservableProperty] private string _totalActual = "$0";
    [ObservableProperty] private string _remainingBudget = "$0";
    [ObservableProperty] private string _executionPercent = "0%";
    [ObservableProperty] private decimal _needsPercent = 50;
    [ObservableProperty] private decimal _wantsPercent = 30;
    [ObservableProperty] private decimal _savingsPercent = 20;
    [ObservableProperty] private string _ruleTotalLabel = "100%";
    [ObservableProperty] private bool _ruleTotalValid = true;
    [ObservableProperty] private string _newCategoryName = string.Empty;
    [ObservableProperty] private string _newSubcategoryName = string.Empty;
    [ObservableProperty] private CategoryPickerItem? _selectedCategory;
    [ObservableProperty] private BudgetGroupOption? _selectedGroupOption;
    [ObservableProperty] private BudgetLineItem? _selectedLine;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ObservableCollection<BudgetLineItem> Lines { get; } = [];
    public ObservableCollection<PeriodOption> PeriodOptions { get; } = [];
    public ObservableCollection<CategoryPickerItem> Categories { get; } = [];
    public ObservableCollection<BudgetGroupOption> GroupOptions { get; } = [];
    public ObservableCollection<AlertItem> Alerts { get; } = [];

    public BudgetViewModel(
        IBudgetService budget,
        IBudgetDistributionService distribution,
        IBudgetPeriodService periods,
        ICurrentUserContext user)
    {
        Title = "Presupuesto";
        _budget = budget;
        _distribution = distribution;
        _periods = periods;
        _user = user;

        GroupOptions.Add(new BudgetGroupOption { Group = BudgetGroup.Needs, Label = "Necesidades" });
        GroupOptions.Add(new BudgetGroupOption { Group = BudgetGroup.Wants, Label = "Deseos" });
        GroupOptions.Add(new BudgetGroupOption { Group = BudgetGroup.Savings, Label = "Ahorro" });
        GroupOptions.Add(new BudgetGroupOption { Group = BudgetGroup.Other, Label = "Otros" });
        SelectedGroupOption = GroupOptions[0];
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            await _periods.EnsureActivePeriodAsync();
            var allPeriods = await _periods.GetPeriodsAsync();
            PeriodOptions.Clear();
            foreach (var p in allPeriods)
                PeriodOptions.Add(new PeriodOption { Id = p.Id, Label = $"{p.StartDate:dd MMM} – {p.EndDate:dd MMM yyyy}" });

            if (SelectedPeriodOption == null || !PeriodOptions.Any(o => o.Id == SelectedPeriodOption.Id))
                SelectedPeriodOption = PeriodOptions.FirstOrDefault(o => o.Id == _user.ActivePeriodId)
                    ?? PeriodOptions.FirstOrDefault();

            if (SelectedPeriodOption != null)
                await LoadPeriodAsync(SelectedPeriodOption.Id);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedPeriodOptionChanged(PeriodOption? value)
    {
        if (value != null)
            _ = LoadPeriodAsync(value.Id);
    }

    partial void OnNeedsPercentChanged(decimal value) => UpdateRuleTotal();
    partial void OnWantsPercentChanged(decimal value) => UpdateRuleTotal();
    partial void OnSavingsPercentChanged(decimal value) => UpdateRuleTotal();

    private void UpdateRuleTotal()
    {
        var total = NeedsPercent + WantsPercent + SavingsPercent;
        RuleTotalValid = total == 100;
        RuleTotalLabel = $"{total:0.#}%";
    }

    private async Task LoadPeriodAsync(Guid periodId)
    {
        _activePeriodId = periodId;
        _user.ActivePeriodId = periodId;

        var period = await _periods.GetByIdAsync(periodId);
        if (period == null) return;

        PeriodLabel = PeriodOptions.FirstOrDefault(p => p.Id == periodId)?.Label ?? string.Empty;
        GrossIncome = ClpFormatter.Format(period.TotalGrossIncome);
        NetIncome = ClpFormatter.Format(period.TotalNetIncome);
        GrossIncomeInput = ((long)period.TotalGrossIncome).ToString();
        NetIncomeInput = ((long)period.TotalNetIncome).ToString();

        await _budget.RecalculateStatusesAsync(periodId);
        var summary = await _budget.GetSummaryAsync(periodId);
        TotalPlanned = ClpFormatter.Format(summary.TotalPlanned);
        TotalActual = ClpFormatter.Format(summary.TotalActual);
        RemainingBudget = ClpFormatter.Format(summary.Remaining);
        ExecutionPercent = ClpFormatter.FormatPercent(summary.ExecutionPercent);

        await ReloadCategoriesAsync();
        await ReloadLinesAsync(periodId);
        BuildAlerts();
        UpdateRuleTotal();
    }

    private async Task ReloadCategoriesAsync()
    {
        var cats = await _budget.GetCategoriesAsync();
        Categories.Clear();
        foreach (var c in cats)
            Categories.Add(new CategoryPickerItem { Id = c.Id, Name = c.Name });

        if (SelectedCategory == null || !Categories.Any(c => c.Id == SelectedCategory.Id))
            SelectedCategory = Categories.FirstOrDefault();
    }

    private async Task ReloadLinesAsync(Guid periodId)
    {
        var allocations = await _budget.GetAllocationsAsync(periodId);
        Lines.Clear();
        foreach (var a in allocations)
        {
            var status = a.Status;
            if (a.PlannedAmount > 0 && a.Status == BudgetLineStatus.Normal && a.UsedPercent >= 80)
                status = BudgetStatusCalculator.FromUsedPercent(a.UsedPercent);

            Lines.Add(new BudgetLineItem
            {
                AllocationId = a.Id,
                CategoryId = a.CategoryId,
                SubcategoryId = a.SubcategoryId,
                Category = a.Category?.Name ?? "—",
                Subcategory = a.Subcategory?.Name ?? "—",
                AllocationMode = BudgetStatusCalculator.AllocationModeLabel(a.AllocationMode),
                Planned = ClpFormatter.Format(a.PlannedAmount),
                Actual = ClpFormatter.Format(a.ActualAmount),
                Difference = ClpFormatter.Format(a.Difference),
                UsedPercent = a.UsedPercent,
                UsedPercentText = ClpFormatter.FormatPercent(a.UsedPercent),
                Status = status,
                StatusLabel = BudgetStatusCalculator.StatusLabel(status),
                StatusBrush = BudgetStatusCalculator.StatusBrush(status),
                ProgressBrush = BudgetStatusCalculator.ProgressBrush(status),
                CategoryBrush = BrushHelper.FromHex(a.Category?.ColorHex ?? "#27D3FF"),
                IsAlert = status is BudgetLineStatus.Attention or BudgetLineStatus.Limit or BudgetLineStatus.Exceeded
            });
        }
    }

    private void BuildAlerts()
    {
        Alerts.Clear();
        foreach (var line in Lines.Where(l => l.IsAlert))
        {
            var msg = line.Status switch
            {
                BudgetLineStatus.Exceeded => $"{line.Category} / {line.Subcategory}: presupuesto excedido ({line.UsedPercentText})",
                BudgetLineStatus.Limit => $"{line.Category} / {line.Subcategory}: en el límite ({line.UsedPercentText})",
                _ => $"{line.Category} / {line.Subcategory}: atención ({line.UsedPercentText})"
            };
            Alerts.Add(new AlertItem { Message = msg });
        }

        if (Alerts.Count == 0)
            Alerts.Add(new AlertItem { Message = "Sin alertas activas en este periodo." });
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task SaveIncome()
    {
        if (_activePeriodId == null) return;
        if (!decimal.TryParse(GrossIncomeInput.Replace(".", "").Replace(",", ""), out var gross))
            gross = 0;
        if (!decimal.TryParse(NetIncomeInput.Replace(".", "").Replace(",", ""), out var net))
            net = 0;

        await _budget.UpdatePeriodIncomeAsync(_activePeriodId.Value, gross, net);
        StatusMessage = "Ingresos actualizados.";
        await LoadPeriodAsync(_activePeriodId.Value);
    }

    [RelayCommand]
    private async Task RecalculateDistribution()
    {
        if (_activePeriodId == null || !RuleTotalValid) return;
        await _distribution.ApplyRule503020Async(_activePeriodId.Value, NeedsPercent, WantsPercent, SavingsPercent);
        await _budget.RecalculateStatusesAsync(_activePeriodId.Value);
        StatusMessage = "Distribución recalculada.";
        await LoadPeriodAsync(_activePeriodId.Value);
    }

    [RelayCommand]
    private async Task DuplicatePreviousMonth()
    {
        if (_activePeriodId == null) return;
        await _budget.DuplicatePreviousPeriodAsync(_activePeriodId.Value);
        await _budget.RecalculateStatusesAsync(_activePeriodId.Value);
        StatusMessage = "Estructura duplicada del mes anterior.";
        await LoadPeriodAsync(_activePeriodId.Value);
    }

    [RelayCommand]
    private async Task AddCategory()
    {
        if (_activePeriodId == null || SelectedGroupOption == null) return;
        if (string.IsNullOrWhiteSpace(NewCategoryName))
        {
            StatusMessage = "Indica un nombre para la categoría.";
            return;
        }

        await _budget.AddCategoryAsync(NewCategoryName, SelectedGroupOption.Group, _activePeriodId.Value);
        NewCategoryName = string.Empty;
        StatusMessage = "Categoría agregada.";
        await LoadPeriodAsync(_activePeriodId.Value);
    }

    [RelayCommand]
    private async Task AddSubcategory()
    {
        if (_activePeriodId == null || SelectedCategory == null) return;
        if (string.IsNullOrWhiteSpace(NewSubcategoryName))
        {
            StatusMessage = "Indica un nombre para la subcategoría.";
            return;
        }

        await _budget.AddSubcategoryAsync(SelectedCategory.Id, NewSubcategoryName, _activePeriodId.Value);
        NewSubcategoryName = string.Empty;
        StatusMessage = "Subcategoría agregada.";
        await LoadPeriodAsync(_activePeriodId.Value);
    }

    [RelayCommand]
    private void SelectLineForSubcategory(BudgetLineItem? line)
    {
        if (line == null) return;
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == line.CategoryId) ?? SelectedCategory;
        SelectedLine = line;
    }
}
