using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;
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
    private decimal _netIncomeValue;

    [ObservableProperty] private string _periodLabel = string.Empty;
    [ObservableProperty] private string _activePeriodBadge = string.Empty;
    [ObservableProperty] private PeriodOption? _selectedPeriodOption;
    [ObservableProperty] private string _grossIncome = "$0";
    [ObservableProperty] private string _netIncome = "$0";
    [ObservableProperty] private string _grossIncomeInput = "0";
    [ObservableProperty] private string _netIncomeInput = "0";
    [ObservableProperty] private string _totalPlanned = "$0";
    [ObservableProperty] private string _totalActual = "$0";
    [ObservableProperty] private string _remainingBudget = "$0";
    [ObservableProperty] private string _availableDifference = "$0";
    [ObservableProperty] private Brush _availableDifferenceBrush = Brushes.White;
    [ObservableProperty] private string _executionPercent = "0%";
    [ObservableProperty] private double _executionRatio;
    [ObservableProperty] private decimal _needsPercent = 50;
    [ObservableProperty] private decimal _wantsPercent = 30;
    [ObservableProperty] private decimal _savingsPercent = 20;
    [ObservableProperty] private string _ruleTotalLabel = "100%";
    [ObservableProperty] private bool _ruleTotalValid = true;
    [ObservableProperty] private bool _isRulePanelExpanded = true;
    [ObservableProperty] private string _newCategoryName = string.Empty;
    [ObservableProperty] private string _newSubcategoryName = string.Empty;
    [ObservableProperty] private CategoryPickerItem? _selectedCategory;
    [ObservableProperty] private BudgetGroupOption? _selectedGroupOption;
    [ObservableProperty] private BudgetLineItem? _selectedLine;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _lineCountLabel = "0 asignaciones";

    public ObservableCollection<BudgetLineItem> Lines { get; } = [];
    public ObservableCollection<PeriodOption> PeriodOptions { get; } = [];
    public ObservableCollection<CategoryPickerItem> Categories { get; } = [];
    public ObservableCollection<BudgetGroupOption> GroupOptions { get; } = [];
    public ObservableCollection<AlertItem> Alerts { get; } = [];
    public ObservableCollection<KpiCardModel> SummaryKpis { get; } = [];
    public ObservableCollection<BudgetRuleBucketModel> RuleBuckets { get; } = [];
    public ObservableCollection<BudgetAlertInsight> BudgetInsights { get; } = [];

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
        AvailableDifferenceBrush = BrushHelper.FromHex("#35E0A1");

        GroupOptions.Add(new BudgetGroupOption { Group = BudgetGroup.Needs, Label = "Necesidades" });
        GroupOptions.Add(new BudgetGroupOption { Group = BudgetGroup.Wants, Label = "Deseos" });
        GroupOptions.Add(new BudgetGroupOption { Group = BudgetGroup.Savings, Label = "Ahorro / deuda" });
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
        if (_activePeriodId != null)
            BuildRuleBuckets(_netIncomeValue);
    }

    private async Task LoadPeriodAsync(Guid periodId)
    {
        _activePeriodId = periodId;
        _user.ActivePeriodId = periodId;

        var period = await _periods.GetByIdAsync(periodId);
        if (period == null) return;

        PeriodLabel = PeriodOptions.FirstOrDefault(p => p.Id == periodId)?.Label ?? string.Empty;
        ActivePeriodBadge = PeriodLabel;
        GrossIncome = ClpFormatter.Format(period.TotalGrossIncome);
        NetIncome = ClpFormatter.Format(period.TotalNetIncome);
        _netIncomeValue = period.TotalNetIncome;
        GrossIncomeInput = ((long)period.TotalGrossIncome).ToString();
        NetIncomeInput = ((long)period.TotalNetIncome).ToString();

        await _budget.RecalculateStatusesAsync(periodId);
        var summary = await _budget.GetSummaryAsync(periodId);
        TotalPlanned = ClpFormatter.Format(summary.TotalPlanned);
        TotalActual = ClpFormatter.Format(summary.TotalActual);
        RemainingBudget = ClpFormatter.Format(summary.Remaining);
        AvailableDifference = ClpFormatter.Format(summary.Remaining);
        AvailableDifferenceBrush = summary.Remaining >= 0
            ? BrushHelper.FromHex("#35E0A1")
            : BrushHelper.FromHex("#FF6B6B");
        ExecutionPercent = ClpFormatter.FormatPercent(summary.ExecutionPercent);
        ExecutionRatio = summary.ExecutionPercent > 0
            ? Math.Min(1.2, (double)summary.ExecutionPercent / 100d)
            : 0;

        await ReloadCategoriesAsync();
        await ReloadLinesAsync(periodId);
        BuildSummaryKpis(summary);
        BuildRuleBuckets(summary.NetIncome);
        BuildAlerts();
        BuildBudgetInsights(summary);
        LineCountLabel = Lines.Count == 1 ? "1 asignación" : $"{Lines.Count} asignaciones";
        UpdateRuleTotal();
    }

    private void BuildSummaryKpis(BudgetPeriodSummary summary)
    {
        SummaryKpis.Clear();
        SummaryKpis.Add(CreateKpi("Periodo activo", ActivePeriodBadge, "Rango presupuestario vigente", "#93A4BD"));
        SummaryKpis.Add(CreateKpi("Ingreso disponible", NetIncome, "Líquido registrado en el periodo", "#35E0A1"));
        SummaryKpis.Add(CreateKpi("Total presupuestado", TotalPlanned, "Asignado en categorías", "#27D3FF"));
        SummaryKpis.Add(CreateKpi("Gasto real", TotalActual, "Ejecutado en el periodo", "#FFB84D"));
        SummaryKpis.Add(CreateKpi("Diferencia disponible", AvailableDifference, "Ingreso líquido menos gasto real", summary.Remaining >= 0 ? "#35E0A1" : "#FF6B6B"));
        SummaryKpis.Add(CreateKpi("Ejecución presupuesto", ExecutionPercent, "Gasto real sobre planificado", "#27D3FF"));
    }

    private void BuildRuleBuckets(decimal netIncome)
    {
        var groupActuals = Lines
            .GroupBy(l => l.Group)
            .ToDictionary(g => g.Key, g => g.Sum(l => ParseAmount(l.Actual)));

        RuleBuckets.Clear();
        RuleBuckets.Add(CreateRuleBucket(
            BudgetGroup.Needs, "Necesidades", "Gastos esenciales", NeedsPercent, netIncome, groupActuals));
        RuleBuckets.Add(CreateRuleBucket(
            BudgetGroup.Wants, "Deseos", "Gastos discrecionales", WantsPercent, netIncome, groupActuals));
        RuleBuckets.Add(CreateRuleBucket(
            BudgetGroup.Savings, "Ahorro / deuda", "Reserva y amortización", SavingsPercent, netIncome, groupActuals));
    }

    private static decimal ParseAmount(string formatted) =>
        decimal.TryParse(formatted, NumberStyles.Currency, CultureInfo.GetCultureInfo("es-CL"), out var v) ? v : 0;

    private BudgetRuleBucketModel CreateRuleBucket(
        BudgetGroup group,
        string label,
        string hint,
        decimal percent,
        decimal netIncome,
        Dictionary<BudgetGroup, decimal> groupActuals)
    {
        var target = netIncome * (percent / 100m);
        var actual = groupActuals.GetValueOrDefault(group);
        var usage = target > 0 ? Math.Min(1.2, (double)(actual / target)) : 0;
        var delta = target - actual;
        var accent = group switch
        {
            BudgetGroup.Needs => "#27D3FF",
            BudgetGroup.Wants => "#FFB84D",
            BudgetGroup.Savings => "#35E0A1",
            _ => "#9B7AFF"
        };

        return new BudgetRuleBucketModel
        {
            Group = group,
            Label = label,
            Hint = hint,
            PercentLabel = $"{percent:0.#}%",
            TargetAmount = ClpFormatter.Format(target),
            ActualAmount = ClpFormatter.Format(actual),
            DeltaLabel = delta >= 0
                ? $"Disponible {ClpFormatter.Format(delta)}"
                : $"Excedido {ClpFormatter.Format(Math.Abs(delta))}",
            UsageRatio = usage,
            AccentColor = accent,
            AccentBrush = BrushHelper.FromHex(accent)
        };
    }

    private void BuildBudgetInsights(BudgetPeriodSummary summary)
    {
        BudgetInsights.Clear();

        var exceeded = Lines.Where(l => l.Status == BudgetLineStatus.Exceeded).ToList();
        var nearLimit = Lines.Where(l => l.Status is BudgetLineStatus.Attention or BudgetLineStatus.Limit).ToList();
        var savingsBucket = RuleBuckets.FirstOrDefault(b => b.Group == BudgetGroup.Savings);

        if (exceeded.Count > 0)
        {
            BudgetInsights.Add(new BudgetAlertInsight
            {
                Title = "Categorías excedidas",
                Message = $"{exceeded.Count} línea(s) superaron el presupuesto asignado.",
                Severity = "danger",
                AccentBrush = BrushHelper.FromHex("#FF6B6B")
            });
            foreach (var line in exceeded.Take(3))
            {
                BudgetInsights.Add(new BudgetAlertInsight
                {
                    Title = line.Category,
                    Message = $"{line.Subcategory}: {line.UsedPercentText} usado",
                    Severity = "danger",
                    AccentBrush = BrushHelper.FromHex("#FF6B6B")
                });
            }
        }

        if (nearLimit.Count > 0)
        {
            BudgetInsights.Add(new BudgetAlertInsight
            {
                Title = "Cerca del límite",
                Message = $"{nearLimit.Count} categoría(s) entre 80% y 100% del presupuesto.",
                Severity = "warning",
                AccentBrush = BrushHelper.FromHex("#FFB84D")
            });
        }

        if (savingsBucket != null && savingsBucket.UsageRatio < 0.85 && _netIncomeValue > 0)
        {
            BudgetInsights.Add(new BudgetAlertInsight
            {
                Title = "Ahorro bajo objetivo",
                Message = $"El bloque de ahorro/deuda ejecuta al {savingsBucket.UsageRatio * 100:0.#}% del objetivo ({savingsBucket.PercentLabel}).",
                Severity = "warning",
                AccentBrush = BrushHelper.FromHex("#35E0A1")
            });
        }

        if (summary.ExecutionPercent >= 100 && summary.TotalPlanned > 0)
        {
            BudgetInsights.Add(new BudgetAlertInsight
            {
                Title = "Presupuesto agotado",
                Message = "El gasto real alcanzó o superó lo planificado en el periodo.",
                Severity = "warning",
                AccentBrush = BrushHelper.FromHex("#FFB84D")
            });
        }

        if (BudgetInsights.Count == 0)
        {
            BudgetInsights.Add(new BudgetAlertInsight
            {
                Title = "Control estable",
                Message = "No hay alertas críticas en este periodo.",
                Severity = "info",
                AccentBrush = BrushHelper.FromHex("#27D3FF")
            });
        }
    }

    private async Task ReloadCategoriesAsync()
    {
        var cats = await _budget.GetCategoriesAsync();
        Categories.Clear();
        foreach (var c in cats)
            Categories.Add(new CategoryPickerItem { Id = c.Id, Name = c.Name, Group = c.DefaultGroup });

        if (SelectedCategory == null || !Categories.Any(c => c.Id == SelectedCategory.Id))
            SelectedCategory = Categories.FirstOrDefault();
    }

    private async Task ReloadLinesAsync(Guid periodId)
    {
        var allocations = await _budget.GetAllocationsAsync(periodId);
        var groupByCategory = Categories.ToDictionary(c => c.Id, c => c.Group);

        Lines.Clear();
        foreach (var a in allocations)
        {
            var status = a.Status;
            var group = a.CategoryId != Guid.Empty && groupByCategory.TryGetValue(a.CategoryId, out var g)
                ? g
                : BudgetGroup.Other;

            Lines.Add(new BudgetLineItem
            {
                AllocationId = a.Id,
                CategoryId = a.CategoryId,
                SubcategoryId = a.SubcategoryId,
                Category = a.Category?.Name ?? "—",
                Subcategory = a.Subcategory?.Name ?? "—",
                Group = group,
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
    private void ToggleRulePanel() => IsRulePanelExpanded = !IsRulePanelExpanded;

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

    private static KpiCardModel CreateKpi(string title, string value, string subtitle, string accentHex) =>
        new()
        {
            Title = title,
            Value = value,
            Subtitle = subtitle,
            AccentColor = accentHex,
            AccentBrush = BrushHelper.FromHex(accentHex)
        };
}
