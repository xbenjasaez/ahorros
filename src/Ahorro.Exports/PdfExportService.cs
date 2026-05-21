using Ahorro.Data;
using Ahorro.Helpers;
using Ahorro.Models.Entities;
using Ahorro.Models.Enums;
using Ahorro.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Ahorro.Exports;

public class PdfExportService : IPdfExportService
{
    private readonly AppDbContext _db;

    public PdfExportService(AppDbContext db) => _db = db;

    static PdfExportService() => QuestPDF.Settings.License = LicenseType.Community;

    public Task<string> ExportReportAsync(ReportData data, string folder, CancellationToken ct = default)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"Reporte_{DateTime.Now:yyyyMMdd_HHmm}.pdf");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Header().Element(c => PageHeader(c, "Reporte financiero general"));
                page.Content().Column(col =>
                {
                    col.Item().Text($"Periodo: {data.PeriodLabel}").SemiBold();
                    col.Item().PaddingTop(16).Element(c => SummaryBlock(c, data.Summary, data.AccumulatedSavings));

                    col.Item().PaddingTop(20).Text("Gasto por categoría").Bold().FontSize(12);
                    foreach (var cat in data.ByCategory.Take(12))
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text(cat.Category);
                            r.ConstantItem(100).AlignRight().Text(ClpFormatter.Format(cat.Amount));
                        });

                    col.Item().PaddingTop(16).Text("Tendencia mensual").Bold().FontSize(12);
                    foreach (var t in data.Trend)
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text(t.Label);
                            r.ConstantItem(80).AlignRight().Text($"G {ClpFormatter.FormatCompact(t.Expense)}");
                            r.ConstantItem(80).AlignRight().Text($"A {ClpFormatter.FormatCompact(t.Savings)}");
                        });

                    col.Item().PaddingTop(16).Text("Top gastos").Bold().FontSize(12);
                    foreach (var (desc, amount) in data.TopExpenses)
                        col.Item().Text($"• {desc}: {ClpFormatter.Format(amount)}");

                    if (data.ExceededCategories.Count > 0)
                    {
                        col.Item().PaddingTop(16).Text("Categorías excedidas").Bold().FontSize(12).FontColor(Colors.Red.Medium);
                        foreach (var ex in data.ExceededCategories)
                            col.Item().Text($"• {ex.Category}: {ex.UsedPercent:0.#}% — real {ClpFormatter.Format(ex.Actual)} / plan {ClpFormatter.Format(ex.Planned)}");
                    }
                });
                page.Footer().AlignCenter().Text($"Generado {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf(path);

        return Task.FromResult(path);
    }

    public async Task<string> ExportBudgetAsync(Guid periodId, string folder, CancellationToken ct = default)
    {
        Directory.CreateDirectory(folder);
        var period = await _db.BudgetPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == periodId, ct);
        var allocations = await _db.BudgetAllocations.AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Subcategory)
            .Where(a => a.BudgetPeriodId == periodId)
            .OrderBy(a => a.Category!.SortOrder)
            .ToListAsync(ct);

        var label = period == null ? "periodo" : $"{period.StartDate:yyyy-MM}";
        var path = Path.Combine(folder, $"Presupuesto_{label}_{DateTime.Now:yyyyMMdd}.pdf");

        var summary = period == null
            ? new ReportSummary(0, 0, 0, 0, 0)
            : new ReportSummary(period.TotalNetIncome, period.ActualSpent,
                allocations.Where(a => a.Category?.DefaultGroup == BudgetGroup.Savings).Sum(a => a.ActualAmount),
                period.TotalNetIncome - period.ActualSpent, period.ExecutionPercent);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(9));
                page.Header().Element(c => PageHeader(c, "Presupuesto mensual"));
                page.Content().Column(col =>
                {
                    col.Item().Text(period == null ? "—" : $"{period.StartDate:dd/MM/yyyy} – {period.EndDate:dd/MM/yyyy}").SemiBold();
                    col.Item().PaddingTop(12).Element(c => SummaryBlock(c, summary, 0));

                    col.Item().PaddingTop(16).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.ConstantColumn(50);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Element(Th).Text("Categoría");
                            h.Cell().Element(Th).AlignRight().Text("Plan");
                            h.Cell().Element(Th).AlignRight().Text("Real");
                            h.Cell().Element(Th).AlignRight().Text("Dif.");
                            h.Cell().Element(Th).AlignRight().Text("%");
                        });
                        foreach (var a in allocations)
                        {
                            table.Cell().Element(Td).Text(a.Subcategory == null ? a.Category?.Name ?? "" : $"{a.Category?.Name} / {a.Subcategory.Name}");
                            table.Cell().Element(Td).AlignRight().Text(ClpFormatter.FormatCompact(a.PlannedAmount));
                            table.Cell().Element(Td).AlignRight().Text(ClpFormatter.FormatCompact(a.ActualAmount));
                            table.Cell().Element(Td).AlignRight().Text(ClpFormatter.FormatCompact(a.Difference));
                            table.Cell().Element(Td).AlignRight().Text($"{a.UsedPercent:0.#}");
                        }
                    });
                });
                page.Footer().AlignCenter().Text($"Generado {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
            });
        }).GeneratePdf(path);

        return path;
    }

    public Task<string> ExportGoalsAsync(IEnumerable<SavingsGoal> goals, string folder, CancellationToken ct = default)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"Metas_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        var list = goals.ToList();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Header().Element(c => PageHeader(c, "Metas de ahorro"));
                page.Content().Column(col =>
                {
                    col.Item().Text($"{list.Count} meta(s) activa(s)").SemiBold();
                    col.Item().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.ConstantColumn(55);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Element(Th).Text("Meta");
                            h.Cell().Element(Th).AlignRight().Text("Objetivo");
                            h.Cell().Element(Th).AlignRight().Text("Acumulado");
                            h.Cell().Element(Th).AlignRight().Text("%");
                        });
                        foreach (var g in list)
                        {
                            var pct = g.TargetAmount > 0 ? g.AccumulatedAmount / g.TargetAmount * 100 : 0;
                            table.Cell().Element(Td).Text(g.Name);
                            table.Cell().Element(Td).AlignRight().Text(ClpFormatter.Format(g.TargetAmount));
                            table.Cell().Element(Td).AlignRight().Text(ClpFormatter.Format(g.AccumulatedAmount));
                            table.Cell().Element(Td).AlignRight().Text($"{pct:0.#}");
                        }
                    });
                });
                page.Footer().AlignCenter().Text($"Generado {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
            });
        }).GeneratePdf(path);

        return Task.FromResult(path);
    }

    private static void PageHeader(IContainer container, string title)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("Ahorro").Bold().FontSize(20).FontColor(Colors.Teal.Medium);
                c.Item().Text(title).FontSize(14);
            });
            row.ConstantItem(120).AlignRight().Text(DateTime.Now.ToString("dd MMM yyyy")).FontSize(9).FontColor(Colors.Grey.Medium);
        });
    }

    private static void SummaryBlock(IContainer container, ReportSummary summary, decimal accumulatedGoals)
    {
        container.Background(Colors.Grey.Lighten4).Padding(12).Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().Text($"Ingresos: {ClpFormatter.Format(summary.TotalIncome)}");
                r.RelativeItem().Text($"Gastos: {ClpFormatter.Format(summary.TotalExpenses)}");
            });
            col.Item().PaddingTop(6).Row(r =>
            {
                r.RelativeItem().Text($"Ahorro periodo: {ClpFormatter.Format(summary.PeriodSavings)}");
                r.RelativeItem().Text($"Saldo libre: {ClpFormatter.Format(summary.FreeBalance)}");
            });
            col.Item().PaddingTop(6).Text($"Ejecución presupuesto: {summary.ExecutionPercent:0.#}%");
            if (accumulatedGoals > 0)
                col.Item().PaddingTop(6).Text($"Ahorro acumulado en metas: {ClpFormatter.Format(accumulatedGoals)}").SemiBold();
        });
    }

    private static IContainer Th(IContainer c) =>
        c.DefaultTextStyle(x => x.SemiBold().FontSize(9)).PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

    private static IContainer Td(IContainer c) =>
        c.PaddingVertical(3).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3);
}
