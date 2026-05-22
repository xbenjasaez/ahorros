using System.Collections.ObjectModel;
using Ahorro.Helpers;
using Ahorro.Models.Abstractions;
using Ahorro.Models.Entities;
using Ahorro.Models.Enums;
using Ahorro.Services.Abstractions;
using Ahorro.ViewModels.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Measure;
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
    [ObservableProperty] private string _activePeriodBadge = "Periodo activo";
    [ObservableProperty] private string _comparisonSubtitle = string.Empty;
    [ObservableProperty] private string _executionSubtitle = string.Empty;
    [ObservableProperty] private string _distributionSubtitle = string.Empty;
    [ObservableProperty] private bool _hasAlerts = true;
    [ObservableProperty] private bool _hasUpcomingPayments = true;
    [ObservableProperty] private PeriodOption? _selectedPeriodOption;

    public ObservableCollection<PeriodOption> PeriodOptions { get; } = [];
    public ObservableCollection<KpiCardModel> Kpis { get; } = [];
    public ObservableCollection<ISeries> ComparisonSeries { get; } = [];
    public ObservableCollection<ISeries> DistributionSeries { get; } = [];
    public ObservableCollection<ISeries> TrendSeries { get; } = [];
    public ObservableCollection<PaymentListItem> UpcomingPayments { get; } = [];
    public ObservableCollection<GoalCardItem> ActiveGoals { get; } = [];
    public ObservableCollection<Models.CriticalCategoryItem> CriticalCategories { get; } = [];
    public ObservableCollection<RecentTransactionItem> RecentTransactions { get; } = [];
    public ObservableCollection<AlertItem> Alerts { get; } = [];
    public ObservableCollection<DistributionLegendItem> DistributionLegend { get; } = [];

    public Axis[] ComparisonXAxes { get; private set; } = [];
    public Axis[] ComparisonYAxes { get; private set; } = [];
    public Axis[] TrendXAxes { get; private set; } = [];
    public Axis[] TrendYAxes { get; private set; } = [];
    public LegendPosition ComparisonLegendPosition { get; } = LegendPosition.Top;
    public LegendPosition TrendLegendPosition { get; } = LegendPosition.Top;

    public DashboardViewModel(IDashboardService dashboard, IBudgetPeriodService periods, ICurrentUserContext user)
    {
        Title = "Dashboard";
        _dashboard = dashboard;
        _periods = periods;
        _user = user;
        ComparisonYAxes = [CreateValueAxis()];
        TrendYAxes = [CreateValueAxis()];
    }

    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var allPeriods = await _periods.GetPeriodsAsync();
            PeriodOptions.Clear();
            foreach (var p in allPeriods)
                PeriodOptions.Add(new PeriodOption { Id = p.Id, Label = $"{p.StartDate:dd MMM} – {p.EndDate:dd MMM yyyy}" });

            var targetId = _user.ActivePeriodId ?? PeriodOptions.FirstOrDefault()?.Id;
            SelectedPeriodOption = PeriodOptions.FirstOrDefault(p => p.Id == targetId) ?? PeriodOptions.FirstOrDefault();
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

    private async Task LoadPeriodAsync(Guid periodId)
    {
        _user.ActivePeriodId = periodId;
        var data = await _dashboard.LoadAsync(periodId);
        PeriodLabel = PeriodOptions.FirstOrDefault(p => p.Id == periodId)?.Label ?? string.Empty;
        ActivePeriodBadge = "Periodo activo";
        ExecutionSubtitle = $"{ClpFormatter.Format(data.TotalActual)} de {ClpFormatter.Format(data.TotalPlanned)} planificado";
        ComparisonSubtitle = $"Top {data.CategoryComparisons.Count} categorías · planificado vs ejecutado";
        DistributionSubtitle = "Presupuesto planificado por categoría";

        Kpis.Clear();
        Kpis.Add(CreateKpi("Ingreso total", ClpFormatter.Format(data.TotalIncome), "Líquido del periodo", "#27D3FF"));
        Kpis.Add(CreateKpi("Gasto real", ClpFormatter.Format(data.TotalExpenses), "Ejecutado en presupuesto", "#FFB84D"));
        Kpis.Add(CreateKpi("Ahorro acumulado", ClpFormatter.Format(data.TotalSavings), "En metas y categoría", "#35E0A1"));
        Kpis.Add(CreateKpi("Saldo libre", ClpFormatter.Format(data.FreeBalance), "Disponible estimado", "#27D3FF"));
        Kpis.Add(CreateKpi("Deuda pagada", ClpFormatter.Format(data.DebtPaid), "Este periodo", "#93A4BD"));
        Kpis.Add(CreateKpi("Ejecución presupuesto", ClpFormatter.FormatPercent(data.ExecutionPercent), ExecutionSubtitle, "#FFB84D"));

        var labels = data.CategoryComparisons.Select(c => c.Category).ToArray();
        ComparisonXAxes = [CreateCategoryAxis(labels)];
        OnPropertyChanged(nameof(ComparisonXAxes));

        ComparisonSeries.Clear();
        ComparisonSeries.Add(new ColumnSeries<decimal>
        {
            Name = "Planificado",
            Values = data.CategoryComparisons.Select(c => c.Planned).ToArray(),
            Fill = ChartFill("#27D3FF"),
            MaxBarWidth = 32,
            Rx = 4,
            Ry = 4
        });
        ComparisonSeries.Add(new ColumnSeries<decimal>
        {
            Name = "Real",
            Values = data.CategoryComparisons.Select(c => c.Actual).ToArray(),
            Fill = ChartFill("#35E0A1"),
            MaxBarWidth = 32,
            Rx = 4,
            Ry = 4
        });

        DistributionSeries.Clear();
        DistributionLegend.Clear();
        var distTotal = data.Distribution.Sum(d => d.Amount);
        foreach (var d in data.Distribution)
        {
            var color = string.IsNullOrWhiteSpace(d.Color) ? "#27D3FF" : d.Color;
            DistributionSeries.Add(new PieSeries<decimal>
            {
                Name = d.Category,
                Values = [d.Amount],
                Fill = ChartFill(color),
                DataLabelsPaint = ChartLabelPaint(),
                DataLabelsSize = 10
            });
            var pct = distTotal > 0 ? d.Amount / distTotal * 100 : 0;
            DistributionLegend.Add(new DistributionLegendItem
            {
                Category = d.Category,
                Amount = ClpFormatter.Format(d.Amount),
                PercentLabel = $"{pct:0.#}%",
                AccentBrush = BrushHelper.FromHex(color)
            });
        }

        var trendLabels = data.Trend.Select(t => t.Label).ToArray();
        TrendXAxes = [CreateCategoryAxis(trendLabels)];
        OnPropertyChanged(nameof(TrendXAxes));

        TrendSeries.Clear();
        TrendSeries.Add(new LineSeries<decimal>
        {
            Name = "Ingresos",
            Values = data.Trend.Select(t => t.Income).ToArray(),
            GeometryFill = ChartFill("#27D3FF"),
            GeometryStroke = null,
            Stroke = ChartStroke("#27D3FF"),
            GeometrySize = 6,
            LineSmoothness = 0.3
        });
        TrendSeries.Add(new LineSeries<decimal>
        {
            Name = "Gastos",
            Values = data.Trend.Select(t => t.Expense).ToArray(),
            GeometryFill = ChartFill("#FF6B6B"),
            GeometryStroke = null,
            Stroke = ChartStroke("#FF6B6B"),
            GeometrySize = 6,
            LineSmoothness = 0.3
        });
        TrendSeries.Add(new LineSeries<decimal>
        {
            Name = "Ahorro",
            Values = data.Trend.Select(t => t.Savings).ToArray(),
            GeometryFill = ChartFill("#35E0A1"),
            GeometryStroke = null,
            Stroke = ChartStroke("#35E0A1"),
            GeometrySize = 6,
            LineSmoothness = 0.3
        });

        UpcomingPayments.Clear();
        foreach (var p in data.UpcomingPayments)
            UpcomingPayments.Add(MapPayment(p));
        HasUpcomingPayments = UpcomingPayments.Count > 0;

        ActiveGoals.Clear();
        foreach (var g in data.ActiveGoals)
        {
            var pct = g.TargetAmount > 0 ? (double)(g.AccumulatedAmount / g.TargetAmount * 100) : 0;
            var remaining = Math.Max(0, g.TargetAmount - g.AccumulatedAmount);
            ActiveGoals.Add(new GoalCardItem
            {
                Id = g.Id,
                Name = g.Name,
                Accumulated = ClpFormatter.Format(g.AccumulatedAmount),
                Target = ClpFormatter.Format(g.TargetAmount),
                Remaining = ClpFormatter.Format(remaining),
                PercentText = $"{pct:0.#}%",
                Progress = Math.Min(1, pct / 100),
                ColorHex = g.ColorHex,
                AccentBrush = BrushHelper.FromHex(g.ColorHex),
                TrackBrush = BrushHelper.FromHex("#1A2430")
            });
        }

        CriticalCategories.Clear();
        foreach (var c in data.CriticalCategories)
            CriticalCategories.Add(new Models.CriticalCategoryItem { Category = c.Category, UsedPercent = $"{c.UsedPercent:0.#}%", Status = c.Status });

        RecentTransactions.Clear();
        foreach (var t in data.RecentTransactions)
            RecentTransactions.Add(new RecentTransactionItem { Date = t.Date.ToString("dd/MM"), Description = t.Description, Amount = ClpFormatter.Format(t.Amount), Category = t.Category?.Name ?? "—" });

        Alerts.Clear();
        foreach (var a in data.Alerts)
            Alerts.Add(MapAlert(a));
        HasAlerts = Alerts.Count > 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    private static KpiCardModel CreateKpi(string title, string value, string subtitle, string accentHex) =>
        new()
        {
            Title = title,
            Value = value,
            Subtitle = subtitle,
            AccentColor = accentHex,
            AccentBrush = BrushHelper.FromHex(accentHex)
        };

    private static AlertItem MapAlert(DashboardAlert alert)
    {
        var brush = alert.Severity switch
        {
            "danger" => BrushHelper.FromHex("#FF6B6B"),
            "warning" => BrushHelper.FromHex("#FFB84D"),
            _ => BrushHelper.FromHex("#27D3FF")
        };
        return new AlertItem
        {
            Title = alert.Title,
            Message = alert.Message,
            Severity = alert.Severity,
            AccentBrush = brush
        };
    }

    private static PaymentListItem MapPayment(ScheduledPayment p)
    {
        var days = (p.DueDate.Date - DateTime.Today).Days;
        var daysLabel = days switch
        {
            < 0 => $"Hace {Math.Abs(days)} días",
            0 => "Vence hoy",
            1 => "Mañana",
            _ => $"En {days} días"
        };
        var color = p.Category?.ColorHex ?? "#27D3FF";

        return new PaymentListItem
        {
            Id = p.Id,
            Name = p.Name,
            Category = p.Category?.Name ?? "—",
            Amount = ClpFormatter.Format(p.EstimatedAmount),
            DueDate = p.DueDate.ToString("dd MMM"),
            DaysLabel = daysLabel,
            Status = p.Status,
            StatusLabel = ScheduledPaymentLabels.Status(p.Status),
            StatusColor = ScheduledPaymentLabels.StatusColor(p.Status),
            StatusBrush = BrushHelper.FromHex(ScheduledPaymentLabels.StatusColor(p.Status)),
            CategoryBrush = BrushHelper.FromHex(color)
        };
    }

    private static Axis CreateCategoryAxis(string[] labels) => new()
    {
        Labels = labels,
        LabelsPaint = ChartLabelPaint(),
        SeparatorsPaint = ChartSeparatorPaint(),
        TextSize = 11,
        LabelsRotation = -15
    };

    private static Axis CreateValueAxis() => new()
    {
        Labeler = v => ClpFormatter.FormatCompact((decimal)v),
        LabelsPaint = ChartLabelPaint(),
        SeparatorsPaint = ChartSeparatorPaint(),
        TextSize = 10
    };

    private static SolidColorPaint ChartFill(string hex) => new(SKColor.Parse(hex));
    private static SolidColorPaint ChartStroke(string hex) => new(SKColor.Parse(hex)) { StrokeThickness = 2 };
    private static SolidColorPaint ChartLabelPaint() => new(SKColor.Parse("#93A4BD"));
    private static SolidColorPaint ChartSeparatorPaint() => new(SKColor.Parse("#243244")) { StrokeThickness = 1 };
}
