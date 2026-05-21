using Ahorro.Data;
using Ahorro.Helpers;
using Ahorro.Models.Entities;
using Ahorro.Models.Enums;
using Ahorro.Services.Abstractions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace Ahorro.Exports;

public class ExcelExportService : IExcelExportService
{
    private readonly AppDbContext _db;

    public ExcelExportService(AppDbContext db) => _db = db;

    public Task<string> ExportTransactionsAsync(IEnumerable<MoneyTransaction> items, string folder, CancellationToken ct = default)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"Movimientos_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Movimientos");
        WriteHeaderRow(ws, ["Fecha", "Tipo", "Descripción", "Categoría", "Subcategoría", "Método", "Monto CLP", "Estado", "Etiquetas"]);
        var row = 2;
        foreach (var t in items)
        {
            ws.Cell(row, 1).Value = t.Date.ToString("dd/MM/yyyy");
            ws.Cell(row, 2).Value = TransactionLabels.Type(t.Type);
            ws.Cell(row, 3).Value = t.Description;
            ws.Cell(row, 4).Value = t.Category?.Name ?? "";
            ws.Cell(row, 5).Value = t.Subcategory?.Name ?? "";
            ws.Cell(row, 6).Value = t.PaymentMethod?.Name ?? "";
            ws.Cell(row, 7).Value = t.Amount;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 8).Value = TransactionLabels.Status(t.Status);
            ws.Cell(row, 9).Value = string.Join(", ", new[] { t.Tag, t.IsRecurring ? "recurrente" : null }.Where(x => x != null)!);
            row++;
        }
        StyleSheet(ws, row - 1, 9);
        wb.SaveAs(path);
        return Task.FromResult(path);
    }

    public async Task<string> ExportBudgetAsync(Guid periodId, string folder, CancellationToken ct = default)
    {
        Directory.CreateDirectory(folder);
        var period = await _db.BudgetPeriods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == periodId, ct);
        var label = period == null ? periodId.ToString() : $"{period.StartDate:yyyy-MM}";
        var path = Path.Combine(folder, $"Presupuesto_{label}_{DateTime.Now:yyyyMMdd}.xlsx");

        var allocations = await _db.BudgetAllocations.AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Subcategory)
            .Where(a => a.BudgetPeriodId == periodId)
            .OrderBy(a => a.Category!.SortOrder)
            .ThenBy(a => a.Subcategory == null ? 0 : 1)
            .ToListAsync(ct);

        using var wb = new XLWorkbook();
        var summary = wb.Worksheets.Add("Resumen");
        summary.Cell(1, 1).Value = "Periodo";
        summary.Cell(1, 2).Value = period == null ? "—" : $"{period.StartDate:dd/MM/yyyy} – {period.EndDate:dd/MM/yyyy}";
        summary.Cell(2, 1).Value = "Ingreso líquido";
        summary.Cell(2, 2).Value = period?.TotalNetIncome ?? 0;
        summary.Cell(3, 1).Value = "Gasto real";
        summary.Cell(3, 2).Value = period?.ActualSpent ?? 0;
        summary.Cell(4, 1).Value = "Presupuesto planificado";
        summary.Cell(4, 2).Value = period?.PlannedBudget ?? 0;
        summary.Cell(5, 1).Value = "Ejecución %";
        summary.Cell(5, 2).Value = period?.ExecutionPercent ?? 0;

        var ws = wb.Worksheets.Add("Detalle");
        WriteHeaderRow(ws, ["Categoría", "Subcategoría", "Planificado", "Real", "Diferencia", "% usado", "Estado"]);
        var row = 2;
        foreach (var a in allocations)
        {
            ws.Cell(row, 1).Value = a.Category?.Name ?? "";
            ws.Cell(row, 2).Value = a.Subcategory?.Name ?? "—";
            ws.Cell(row, 3).Value = a.PlannedAmount;
            ws.Cell(row, 4).Value = a.ActualAmount;
            ws.Cell(row, 5).Value = a.Difference;
            ws.Cell(row, 6).Value = a.UsedPercent;
            ws.Cell(row, 7).Value = BudgetStatusCalculator.StatusLabel(a.Status);
            row++;
        }
        StyleSheet(ws, row - 1, 7);
        wb.SaveAs(path);
        return path;
    }

    public Task<string> ExportGoalsAsync(IEnumerable<SavingsGoal> goals, string folder, CancellationToken ct = default)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"Metas_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Metas");
        WriteHeaderRow(ws, ["Meta", "Objetivo CLP", "Acumulado CLP", "Restante CLP", "% avance", "Fecha objetivo", "Estado", "Auto desde presupuesto"]);
        var row = 2;
        foreach (var g in goals)
        {
            var remaining = Math.Max(0, g.TargetAmount - g.AccumulatedAmount);
            var pct = g.TargetAmount > 0 ? Math.Round(g.AccumulatedAmount / g.TargetAmount * 100, 1) : 0;
            ws.Cell(row, 1).Value = g.Name;
            ws.Cell(row, 2).Value = g.TargetAmount;
            ws.Cell(row, 3).Value = g.AccumulatedAmount;
            ws.Cell(row, 4).Value = remaining;
            ws.Cell(row, 5).Value = pct;
            ws.Cell(row, 6).Value = g.TargetDate?.ToString("dd/MM/yyyy") ?? "—";
            ws.Cell(row, 7).Value = g.Status.ToString();
            ws.Cell(row, 8).Value = g.AutoContributeFromBudget ? "Sí" : "No";
            row++;
        }
        StyleSheet(ws, row - 1, 8);
        wb.SaveAs(path);
        return Task.FromResult(path);
    }

    private static void WriteHeaderRow(IXLWorksheet ws, string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1A2433");
            cell.Style.Font.FontColor = XLColor.FromHtml("#E8EDF4");
        }
    }

    private static void StyleSheet(IXLWorksheet ws, int lastRow, int cols)
    {
        if (lastRow < 1) return;
        ws.Columns(1, cols).AdjustToContents();
        ws.Range(1, 1, lastRow, cols).SetAutoFilter();
    }
}
