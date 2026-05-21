using System.Collections.ObjectModel;
using Ahorro.Helpers;
using Ahorro.Models.Abstractions;
using Ahorro.Models.Dtos;
using Ahorro.Services.Abstractions;
using Ahorro.ViewModels.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
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
    [ObservableProperty] private string _accumulatedSavings = "$0";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasStatusMessage;
    [ObservableProperty] private PeriodOption? _selectedPeriodOption;

    public ObservableCollection<PeriodOption> PeriodOptions { get; } = [];
    public ObservableCollection<KpiCardModel> SummaryKpis { get; } = [];
    public ObservableCollection<ISeries> CategorySeries { get; } = [];
    public ObservableCollection<ISeries> TrendSeries { get; } = [];
    public ObservableCollection<ISeries> SavingsSeries { get; } = [];
    public ObservableCollection<ReportTopExpenseItem> TopExpenses { get; } = [];
    public ObservableCollection<ReportExceededItem> ExceededCategories { get; } = [];

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
        AccumulatedSavings = ClpFormatter.Format(data.AccumulatedSavings);

        SummaryKpis.Clear();
        SummaryKpis.Add(Kpi("Ingreso", ClpFormatter.Format(data.Summary.TotalIncome), "Líquido del periodo", "#27D3FF"));
        SummaryKpis.Add(Kpi("Gastos", ClpFormatter.Format(data.Summary.TotalExpenses), "Ejecutado", "#FF6B6B"));
        SummaryKpis.Add(Kpi("Ahorro periodo", ClpFormatter.Format(data.Summary.PeriodSavings), "Categoría ahorro", "#35E0A1"));
        SummaryKpis.Add(Kpi("Saldo libre", ClpFormatter.Format(data.Summary.FreeBalance), "Disponible", "#27D3FF"));
        SummaryKpis.Add(Kpi("Ejecución", ClpFormatter.FormatPercent(data.Summary.ExecutionPercent), "Del planificado", "#FFB84D"));
        SummaryKpis.Add(Kpi("En metas", ClpFormatter.Format(data.AccumulatedSavings), "Acumulado total", "#35E0A1"));

        CategorySeries.Clear();
        foreach (var c in data.ByCategory)
        {
            var color = string.IsNullOrWhiteSpace(c.Color) ? "#27D3FF" : c.Color;
            CategorySeries.Add(new PieSeries<decimal>
            {
                Name = c.Category,
                Values = [c.Amount],
                Fill = new SolidColorPaint(SKColor.Parse(color)),
                DataLabelsPaint = new SolidColorPaint(SKColors.White.WithAlpha(200))
            });
        }

        TrendSeries.Clear();
        TrendSeries.Add(new LineSeries<decimal>
        {
            Name = "Ingresos",
            Values = data.Trend.Select(t => t.Income).ToArray(),
            GeometryFill = new SolidColorPaint(SKColor.Parse("#27D3FF")),
            GeometryStroke = null,
            Stroke = new SolidColorPaint(SKColor.Parse("#27D3FF")) { StrokeThickness = 2 }
        });
        TrendSeries.Add(new LineSeries<decimal>
        {
            Name = "Gastos",
            Values = data.Trend.Select(t => t.Expense).ToArray(),
            GeometryFill = new SolidColorPaint(SKColor.Parse("#FF6B6B")),
            GeometryStroke = null,
            Stroke = new SolidColorPaint(SKColor.Parse("#FF6B6B")) { StrokeThickness = 2 }
        });

        SavingsSeries.Clear();
        SavingsSeries.Add(new ColumnSeries<decimal>
        {
            Name = "Ahorro acumulado",
            Values = data.Trend.Select(t => t.Savings).ToArray(),
            Fill = new SolidColorPaint(SKColor.Parse("#35E0A1")),
            MaxBarWidth = 28
        });

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
        StatusMessage = $"Reporte PDF: {path}";
    }

    [RelayCommand]
    private async Task ExportGoalsExcel()
    {
        var list = await _goals.GetActiveGoalsAsync();
        var path = await _excel.ExportGoalsAsync(list, ExportPaths.DefaultFolder);
        StatusMessage = $"Metas Excel: {path}";
    }

    [RelayCommand]
    private async Task ExportGoalsPdf()
    {
        var list = await _goals.GetActiveGoalsAsync();
        var path = await _pdf.ExportGoalsAsync(list, ExportPaths.DefaultFolder);
        StatusMessage = $"Metas PDF: {path}";
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
}
