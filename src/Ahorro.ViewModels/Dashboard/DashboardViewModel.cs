using System.Collections.ObjectModel;
using Ahorro.Helpers;
using Ahorro.Services.Abstractions;
using Ahorro.ViewModels.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Ahorro.ViewModels.Dashboard;

public partial class DashboardViewModel : ViewModelBase, ILoadable
{
    private readonly IDashboardService _dashboard;
    private readonly IBudgetPeriodService _periods;
    private readonly ICurrentUserContext _user;

    [ObservableProperty] private string _periodLabel = string.Empty;
    public ObservableCollection<PeriodOption> PeriodOptions { get; } = [];
    [ObservableProperty] private PeriodOption? _selectedPeriodOption;
    public ObservableCollection<KpiCardModel> Kpis { get; } = [];
    public ObservableCollection<ISeries> ComparisonSeries { get; } = [];
    public ObservableCollection<ISeries> DistributionSeries { get; } = [];
    public ObservableCollection<ISeries> TrendSeries { get; } = [];
    public ObservableCollection<PaymentListItem> UpcomingPayments { get; } = [];
    public ObservableCollection<GoalCardItem> ActiveGoals { get; } = [];
    public ObservableCollection<CriticalCategoryItem> CriticalCategories { get; } = [];
    public ObservableCollection<RecentTransactionItem> RecentTransactions { get; } = [];
    public ObservableCollection<AlertItem> Alerts { get; } = [];
    public string[] ComparisonLabels { get; private set; } = [];

    public DashboardViewModel(IDashboardService dashboard, IBudgetPeriodService periods, ICurrentUserContext user)
    {
        Title = "Dashboard";
        _dashboard = dashboard;
        _periods = periods;
        _user = user;
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        var allPeriods = await _periods.GetPeriodsAsync();
        PeriodOptions.Clear();
        foreach (var p in allPeriods)
            PeriodOptions.Add(new PeriodOption { Id = p.Id, Label = $"{p.StartDate:dd MMM} – {p.EndDate:dd MMM yyyy}" });

        SelectedPeriodOption = PeriodOptions.FirstOrDefault() ?? SelectedPeriodOption;
        if (SelectedPeriodOption != null)
            await LoadPeriodAsync(SelectedPeriodOption.Id);
        IsBusy = false;
    }

    partial void OnSelectedPeriodOptionChanged(PeriodOption? value)
    {
        if (value != null)
            _ = LoadPeriodAsync(value.Id);
    }

    private async Task LoadPeriodAsync(Guid periodId)
    {
        _user.ActivePeriodId = periodId;
        var data = await _dashboard.LoadAsync(periodId);
        PeriodLabel = PeriodOptions.FirstOrDefault(p => p.Id == periodId)?.Label ?? string.Empty;

        Kpis.Clear();
        Kpis.Add(new KpiCardModel { Title = "Ingreso total", Value = ClpFormatter.Format(data.TotalIncome), Subtitle = "Líquido del periodo", AccentColor = "#27D3FF" });
        Kpis.Add(new KpiCardModel { Title = "Gasto real", Value = ClpFormatter.Format(data.TotalExpenses), Subtitle = "Ejecutado", AccentColor = "#FFB84D" });
        Kpis.Add(new KpiCardModel { Title = "Ahorro acumulado", Value = ClpFormatter.Format(data.TotalSavings), Subtitle = "En metas y categoría", AccentColor = "#35E0A1" });
        Kpis.Add(new KpiCardModel { Title = "Saldo libre", Value = ClpFormatter.Format(data.FreeBalance), Subtitle = "Disponible estimado", AccentColor = "#27D3FF" });
        Kpis.Add(new KpiCardModel { Title = "Deuda pagada", Value = ClpFormatter.Format(data.DebtPaid), Subtitle = "Este periodo", AccentColor = "#93A4BD" });
        Kpis.Add(new KpiCardModel { Title = "Ejecución presupuesto", Value = ClpFormatter.FormatPercent(data.ExecutionPercent), Subtitle = "Del planificado", AccentColor = "#FFB84D" });

        ComparisonLabels = data.CategoryComparisons.Select(c => c.Category).ToArray();
        ComparisonSeries.Clear();
        ComparisonSeries.Add(new ColumnSeries<decimal> { Name = "Planificado", Values = data.CategoryComparisons.Select(c => c.Planned).ToArray(), Fill = new SolidColorPaint(SKColor.Parse("#27D3FF")) });
        ComparisonSeries.Add(new ColumnSeries<decimal> { Name = "Real", Values = data.CategoryComparisons.Select(c => c.Actual).ToArray(), Fill = new SolidColorPaint(SKColor.Parse("#35E0A1")) });

        DistributionSeries.Clear();
        DistributionSeries.Add(new PieSeries<decimal>
        {
            Values = data.Distribution.Select(d => d.Amount).ToArray(),
            Name = "Distribución",
            DataLabelsPaint = new SolidColorPaint(SKColors.White)
        });

        TrendSeries.Clear();
        TrendSeries.Add(new LineSeries<decimal> { Name = "Ingresos", Values = data.Trend.Select(t => t.Income).ToArray(), GeometryFill = new SolidColorPaint(SKColor.Parse("#27D3FF")), GeometryStroke = null });
        TrendSeries.Add(new LineSeries<decimal> { Name = "Gastos", Values = data.Trend.Select(t => t.Expense).ToArray(), GeometryFill = new SolidColorPaint(SKColor.Parse("#FF6B6B")), GeometryStroke = null });
        TrendSeries.Add(new LineSeries<decimal> { Name = "Ahorro", Values = data.Trend.Select(t => t.Savings).ToArray(), GeometryFill = new SolidColorPaint(SKColor.Parse("#35E0A1")), GeometryStroke = null });

        UpcomingPayments.Clear();
        foreach (var p in data.UpcomingPayments)
            UpcomingPayments.Add(new PaymentListItem { Id = p.Id, Name = p.Name, Amount = ClpFormatter.Format(p.EstimatedAmount), DueDate = p.DueDate.ToString("dd MMM"), Status = p.Status, StatusLabel = p.Status.ToString() });

        ActiveGoals.Clear();
        foreach (var g in data.ActiveGoals)
        {
            var pct = g.TargetAmount > 0 ? (double)(g.AccumulatedAmount / g.TargetAmount * 100) : 0;
            ActiveGoals.Add(new GoalCardItem { Id = g.Id, Name = g.Name, Accumulated = ClpFormatter.Format(g.AccumulatedAmount), Target = ClpFormatter.Format(g.TargetAmount), PercentText = $"{pct:0.#}%", Progress = Math.Min(1, pct / 100), ColorHex = g.ColorHex });
        }

        CriticalCategories.Clear();
        foreach (var c in data.CriticalCategories)
            CriticalCategories.Add(new CriticalCategoryItem { Category = c.Category, UsedPercent = $"{c.UsedPercent:0.#}%", Status = c.Status });

        RecentTransactions.Clear();
        foreach (var t in data.RecentTransactions)
            RecentTransactions.Add(new RecentTransactionItem { Date = t.Date.ToString("dd/MM"), Description = t.Description, Amount = ClpFormatter.Format(t.Amount), Category = t.Category?.Name ?? "—" });

        Alerts.Clear();
        foreach (var a in data.Alerts)
            Alerts.Add(new AlertItem { Message = a });
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
