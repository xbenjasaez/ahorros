using System.Collections.ObjectModel;
using Ahorro.Helpers;
using Ahorro.Models.Abstractions;
using Ahorro.Models.Dtos;
using Ahorro.Services.Abstractions;
using Ahorro.ViewModels.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Ahorro.ViewModels.Reports;

public partial class ReportsViewModel : ViewModelBase, ILoadable
{
    private readonly IReportService _reports;
    private readonly IBudgetPeriodService _periods;
    private readonly ITransactionService _transactions;
    private readonly ISavingsGoalService _goals;
    private readonly IExcelExportService _excel;
    private readonly IPdfExportService _pdf;
    private readonly ICurrentUserContext _user;

    private Guid? _currentPeriodId;
    private ReportData? _lastReport;

    [ObservableProperty] private string _periodLabel = string.Empty;
    [ObservableProperty] private string _activePeriodBadge = "Periodo analizado";
    [ObservableProperty] private string _accumulatedSavings = "$0";
    [ObservableProperty] private string _accumulatedSavingsSubtitle = "Total en metas activas";
    [ObservableProperty] private string _categorySubtitle = string.Empty;
    [ObservableProperty] private string _trendSubtitle = string.Empty;
    [ObservableProperty] private string _savingsHistorySubtitle = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasStatusMessage;
    [ObservableProperty] private bool _hasTopExpenses;
    [ObservableProperty] private PeriodOption? _selectedPeriodOption;

    public ObservableCollection<PeriodOption> PeriodOptions { get; } = [];
    public ObservableCollection<KpiCardModel> SummaryKpis { get; } = [];
    public ObservableCollection<ISeries> CategorySeries { get; } = [];
    public ObservableCollection<ISeries> TrendSeries { get; } = [];
    public ObservableCollection<ISeries> SavingsSeries { get; } = [];
    public ObservableCollection<DistributionLegendItem> CategoryLegend { get; } = [];
    public ObservableCollection<ReportTopExpenseItem> TopExpenses { get; } = [];
    public ObservableCollection<ReportExceededItem> ExceededCategories { get; } = [];

    public Axis[] CategoryXAxes { get; private set; } = [];
    public Axis[] CategoryYAxes { get; private set; } = [];
    public Axis[] TrendXAxes { get; private set; } = [];
    public Axis[] TrendYAxes { get; private set; } = [];
    public Axis[] SavingsXAxes { get; private set; } = [];
    public Axis[] SavingsYAxes { get; private set; } = [];
    public LegendPosition CategoryLegendPosition { get; } = LegendPosition.Hidden;
    public LegendPosition TrendLegendPosition { get; } = LegendPosition.Top;
    public LegendPosition SavingsLegendPosition { get; } = LegendPosition.Hidden;

    partial void OnStatusMessageChanged(string value) => HasStatusMessage = !string.IsNullOrWhiteSpace(value);

    public ReportsViewModel(
        IReportService reports,
        IBudgetPeriodService periods,
        ITransactionService transactions,
        ISavingsGoalService goals,
        IExcelExportService excel,
        IPdfExportService pdf,
        ICurrentUserContext user)
    {
        Title = "Reportes";
        _reports = reports;
        _periods = periods;
        _transactions = transactions;
        _goals = goals;
        _excel = excel;
        _pdf = pdf;
        _user = user;
        CategoryXAxes = [CreateValueAxis()];
        TrendYAxes = [CreateValueAxis()];
        SavingsYAxes = [CreateValueAxis()];
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
        _currentPeriodId = periodId;
        _user.ActivePeriodId = periodId;
        var data = await _reports.LoadAsync(periodId);
        _lastReport = data;

        PeriodLabel = data.PeriodLabel;
        ActivePeriodBadge = "Periodo analizado";
        AccumulatedSavings = ClpFormatter.Format(data.AccumulatedSavings);
        AccumulatedSavingsSubtitle = "Suma de metas activas · independiente del periodo";

        SummaryKpis.Clear();
        SummaryKpis.Add(Kpi("Ingreso", ClpFormatter.Format(data.Summary.TotalIncome), "Líquido del periodo", "#27D3FF"));
        SummaryKpis.Add(Kpi("Gastos", ClpFormatter.Format(data.Summary.TotalExpenses), "Ejecutado en presupuesto", "#FF6B6B"));
        SummaryKpis.Add(Kpi("Ahorro periodo", ClpFormatter.Format(data.Summary.PeriodSavings), "Asignación categoría ahorro", "#35E0A1"));
        SummaryKpis.Add(Kpi("Saldo libre", ClpFormatter.Format(data.Summary.FreeBalance), "Ingreso − gastos", "#27D3FF"));
        SummaryKpis.Add(Kpi("Ejecución", ClpFormatter.FormatPercent(data.Summary.ExecutionPercent), "Gasto / planificado", "#FFB84D"));
        SummaryKpis.Add(Kpi("En metas", ClpFormatter.Format(data.AccumulatedSavings), "Acumulado histórico", "#35E0A1"));

        var categoryItems = BuildCategoryTop(data.ByCategory);
        var categoryTotal = categoryItems.Sum(c => c.Amount);
        CategorySubtitle = categoryItems.Count > 0
            ? $"Top {categoryItems.Count} · {ClpFormatter.Format(categoryTotal)} en gastos del periodo"
            : "Sin gastos clasificados en el periodo";

        CategorySeries.Clear();
        CategoryLegend.Clear();
        var labels = categoryItems.Select(c => c.Category).ToArray();
        CategoryYAxes = [CreateCategoryAxis(labels)];
        OnPropertyChanged(nameof(CategoryYAxes));

        if (categoryItems.Count > 0)
        {
            CategorySeries.Add(new RowSeries<decimal>
            {
                Name = "Gasto",
                Values = categoryItems.Select(c => c.Amount).ToArray(),
                Fill = ChartFill("#FF6B6B"),
                MaxBarWidth = 22,
                Rx = 4,
                Ry = 4,
                DataLabelsPaint = null
            });

            foreach (var c in categoryItems)
            {
                var color = string.IsNullOrWhiteSpace(c.Color) ? "#FF6B6B" : c.Color;
                var pct = categoryTotal > 0 ? c.Amount / categoryTotal * 100 : 0;
                CategoryLegend.Add(new DistributionLegendItem
                {
                    Category = c.Category,
                    Amount = ClpFormatter.Format(c.Amount),
                    PercentLabel = $"{pct:0.#}%",
                    AccentBrush = BrushHelper.FromHex(color)
                });
            }
        }

        var trendLabels = data.Trend.Select(t => t.Label).ToArray();
        TrendXAxes = [CreateCategoryAxis(trendLabels)];
        OnPropertyChanged(nameof(TrendXAxes));
        TrendSubtitle = data.Trend.Count > 0
            ? $"Últimos {data.Trend.Count} periodos · ingresos, gastos y ahorro"
            : "Sin historial de periodos";

        TrendSeries.Clear();
        if (data.Trend.Count == 0)
        {
            TrendXAxes = [CreateCategoryAxis([])];
            OnPropertyChanged(nameof(TrendXAxes));
        }
        else
        {
            TrendSeries.Add(new LineSeries<decimal>
            {
                Name = "Ingresos",
                Values = data.Trend.Select(t => t.Income).ToArray(),
                GeometryFill = ChartFill("#27D3FF"),
                GeometryStroke = null,
                Stroke = ChartStroke("#27D3FF"),
                GeometrySize = 5,
                LineSmoothness = 0.25
            });
            TrendSeries.Add(new LineSeries<decimal>
            {
                Name = "Gastos",
                Values = data.Trend.Select(t => t.Expense).ToArray(),
                GeometryFill = ChartFill("#FF6B6B"),
                GeometryStroke = null,
                Stroke = ChartStroke("#FF6B6B"),
                GeometrySize = 5,
                LineSmoothness = 0.25
            });
            TrendSeries.Add(new LineSeries<decimal>
            {
                Name = "Ahorro",
                Values = data.Trend.Select(t => t.Savings).ToArray(),
                GeometryFill = ChartFill("#35E0A1"),
                GeometryStroke = null,
                Stroke = ChartStroke("#35E0A1"),
                GeometrySize = 5,
                LineSmoothness = 0.25
            });
        }

        var cumulative = 0m;
        var cumulativeValues = data.Trend.Select(t =>
        {
            cumulative += t.Savings;
            return cumulative;
        }).ToArray();

        SavingsXAxes = [CreateCategoryAxis(trendLabels)];
        OnPropertyChanged(nameof(SavingsXAxes));
        SavingsHistorySubtitle = data.Trend.Count > 0
            ? "Acumulado progresivo del ahorro (ingreso − gasto) por periodo"
            : "Sin datos históricos";

        SavingsSeries.Clear();
        if (cumulativeValues.Length == 0)
        {
            SavingsXAxes = [CreateCategoryAxis([])];
            OnPropertyChanged(nameof(SavingsXAxes));
        }
        else
        {
            SavingsSeries.Add(new ColumnSeries<decimal>
            {
                Name = "Ahorro acumulado",
                Values = cumulativeValues,
                Fill = ChartFill("#35E0A1"),
                MaxBarWidth = 36,
                Rx = 4,
                Ry = 4
            });
        }

        TopExpenses.Clear();
        var rank = 1;
        foreach (var (desc, amount) in data.TopExpenses)
        {
            TopExpenses.Add(new ReportTopExpenseItem
            {
                Rank = rank++,
                Description = desc,
                Amount = ClpFormatter.Format(amount)
            });
        }
        HasTopExpenses = TopExpenses.Count > 0;

        ExceededCategories.Clear();
        foreach (var ex in data.ExceededCategories)
        {
            ExceededCategories.Add(new ReportExceededItem
            {
                Category = ex.Category,
                Planned = ClpFormatter.Format(ex.Planned),
                Actual = ClpFormatter.Format(ex.Actual),
                UsedPercent = $"{ex.UsedPercent:0.#}%",
                AccentBrush = BrushHelper.FromHex(ex.ColorHex)
            });
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task ExportTransactionsExcel()
    {
        if (_currentPeriodId == null) return;
        var criteria = new FilterCriteria { BudgetPeriodId = _currentPeriodId };
        var items = await _transactions.GetFilteredAsync(criteria);
        var path = await _excel.ExportTransactionsAsync(items, ExportPaths.DefaultFolder);
        StatusMessage = $"Movimientos exportados: {path}";
    }

    [RelayCommand]
    private async Task ExportBudgetExcel()
    {
        if (_currentPeriodId == null) return;
        var path = await _excel.ExportBudgetAsync(_currentPeriodId.Value, ExportPaths.DefaultFolder);
        StatusMessage = $"Presupuesto Excel: {path}";
    }

    [RelayCommand]
    private async Task ExportBudgetPdf()
    {
        if (_currentPeriodId == null) return;
        var path = await _pdf.ExportBudgetAsync(_currentPeriodId.Value, ExportPaths.DefaultFolder);
        StatusMessage = $"Presupuesto PDF: {path}";
    }

    [RelayCommand]
    private async Task ExportReportPdf()
    {
        if (_lastReport == null && _currentPeriodId.HasValue)
            _lastReport = await _reports.LoadAsync(_currentPeriodId.Value);
        if (_lastReport == null) return;
        var path = await _pdf.ExportReportAsync(_lastReport, ExportPaths.DefaultFolder);
        StatusMessage = $"Reporte del periodo exportado: {path}";
    }

    [RelayCommand]
    private async Task ExportGoalsExcel()
    {
        var list = await _goals.GetActiveGoalsAsync();
        var path = await _excel.ExportGoalsAsync(list, ExportPaths.DefaultFolder);
        StatusMessage = $"Metas de ahorro (Excel): {path}";
    }

    [RelayCommand]
    private async Task ExportGoalsPdf()
    {
        var list = await _goals.GetActiveGoalsAsync();
        var path = await _pdf.ExportGoalsAsync(list, ExportPaths.DefaultFolder);
        StatusMessage = $"Metas de ahorro (PDF): {path}";
    }

    private static List<CategoryDistributionItem> BuildCategoryTop(List<CategoryDistributionItem> items)
    {
        var top = items.Take(6).ToList();
        var others = items.Skip(6).Sum(x => x.Amount);
        if (others > 0)
            top.Add(new CategoryDistributionItem("Otros", others, "#93A4BD"));
        return top;
    }

    private static KpiCardModel Kpi(string title, string value, string subtitle, string hex) =>
        new()
        {
            Title = title,
            Value = value,
            Subtitle = subtitle,
            AccentColor = hex,
            AccentBrush = BrushHelper.FromHex(hex)
        };

    private static Axis CreateCategoryAxis(string[] labels) => new()
    {
        Labels = labels,
        LabelsPaint = ChartLabelPaint(),
        SeparatorsPaint = ChartSeparatorPaint(),
        TextSize = 11,
        LabelsRotation = 0
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
