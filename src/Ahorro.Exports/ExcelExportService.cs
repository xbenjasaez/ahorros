using Ahorro.Helpers;
using Ahorro.Models.Enums;
using Ahorro.Models.Entities;
using Ahorro.Services.Abstractions;
using ClosedXML.Excel;

namespace Ahorro.Exports;

public class ExcelExportService : IExcelExportService
{
    public Task<string> ExportTransactionsAsync(IEnumerable<MoneyTransaction> items, string folder, CancellationToken ct = default)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"Movimientos_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Movimientos");
        ws.Cell(1, 1).Value = "Fecha";
        ws.Cell(1, 2).Value = "Tipo";
        ws.Cell(1, 3).Value = "Descripción";
        ws.Cell(1, 4).Value = "Categoría";
        ws.Cell(1, 5).Value = "Subcategoría";
        ws.Cell(1, 6).Value = "Método";
        ws.Cell(1, 7).Value = "Monto CLP";
        ws.Cell(1, 8).Value = "Estado";
        ws.Cell(1, 9).Value = "Etiquetas";
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
            ws.Cell(row, 8).Value = TransactionLabels.Status(t.Status);
            ws.Cell(row, 9).Value = string.Join(", ", new[] { t.Tag, t.IsRecurring ? "recurrente" : null }.Where(x => x != null)!);
            row++;
        }
        wb.SaveAs(path);
        return Task.FromResult(path);
    }

    public Task<string> ExportBudgetAsync(Guid periodId, string folder, CancellationToken ct = default)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"Presupuesto_{periodId:N}_{DateTime.Now:yyyyMMdd}.xlsx");
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Presupuesto");
        ws.Cell(1, 1).Value = "Periodo";
        ws.Cell(1, 2).Value = periodId.ToString();
        wb.SaveAs(path);
        return Task.FromResult(path);
    }
}
