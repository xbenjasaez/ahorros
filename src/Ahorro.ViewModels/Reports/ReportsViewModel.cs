using System.Collections.ObjectModel;
using Ahorro.Helpers;
using Ahorro.Models.Abstractions;
using Ahorro.Services.Abstractions;
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
    private readonly IExcelExportService _excel;
    private readonly IPdfExportService _pdf;
    private readonly ICurrentUserContext _user;

    [ObservableProperty] private string _periodLabel = string.Empty;
    [ObservableProperty] private string _accumulatedSavings = "$0";
    public ObservableCollection<ISeries> CategorySeries { get; } = [];
    public ObservableCollection<ISeries> TrendSeries { get; } = [];
    public ObservableCollection<string> TopExpenses { get; } = [];

    public ReportsViewModel(IReportService reports, IExcelExportService excel, IPdfExportService pdf, ICurrentUserContext user)
    {
        Title = "Reportes";
        _reports = reports;
        _excel = excel;
        _pdf = pdf;
        _user = user;
    }

    public async Task LoadAsync()
    {
        if (!_user.ActivePeriodId.HasValue) return;
        var data = await _reports.LoadAsync(_user.ActivePeriodId.Value);
        PeriodLabel = data.PeriodLabel;
        AccumulatedSavings = ClpFormatter.Format(data.AccumulatedSavings);

        CategorySeries.Clear();
        CategorySeries.Add(new PieSeries<decimal>
        {
            Values = data.ByCategory.Select(c => c.Amount).ToArray(),
            Fill = new SolidColorPaint(SKColor.Parse("#27D3FF"))
        });

        TrendSeries.Clear();
        TrendSeries.Add(new LineSeries<decimal> { Name = "Gastos", Values = data.Trend.Select(t => t.Expense).ToArray() });

        TopExpenses.Clear();
        foreach (var (desc, amount) in data.TopExpenses)
            TopExpenses.Add($"{desc} — {ClpFormatter.Format(amount)}");
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Ahorro");
        if (_user.ActivePeriodId.HasValue)
            await _excel.ExportBudgetAsync(_user.ActivePeriodId.Value, folder);
    }

    [RelayCommand]
    private async Task ExportPdf()
    {
        if (!_user.ActivePeriodId.HasValue) return;
        var data = await _reports.LoadAsync(_user.ActivePeriodId.Value);
        var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Ahorro");
        await _pdf.ExportReportAsync(data, folder);
    }
}
