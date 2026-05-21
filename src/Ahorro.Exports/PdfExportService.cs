using Ahorro.Services.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Ahorro.Exports;

public class PdfExportService : IPdfExportService
{
    public Task<string> ExportReportAsync(ReportData data, string folder, CancellationToken ct = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"Reporte_{DateTime.Now:yyyyMMdd_HHmm}.pdf");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));
                page.Header().Text("Ahorro — Reporte financiero").Bold().FontSize(18);
                page.Content().Column(col =>
                {
                    col.Item().Text($"Periodo: {data.PeriodLabel}");
                    col.Item().PaddingTop(12).Text($"Ahorro acumulado: {data.AccumulatedSavings:N0} CLP");
                    col.Item().PaddingTop(8).Text("Top gastos:").Bold();
                    foreach (var (desc, amount) in data.TopExpenses.Take(8))
                        col.Item().Text($"• {desc}: ${amount:N0}");
                });
                page.Footer().AlignCenter().Text($"Generado {DateTime.Now:dd/MM/yyyy HH:mm}");
            });
        }).GeneratePdf(path);

        return Task.FromResult(path);
    }
}
