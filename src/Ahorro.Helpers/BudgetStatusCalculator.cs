using System.Windows.Media;
using Ahorro.Models.Enums;

namespace Ahorro.Helpers;

public static class BudgetStatusCalculator
{
    public static BudgetLineStatus FromUsedPercent(decimal usedPercent)
    {
        if (usedPercent > 100) return BudgetLineStatus.Exceeded;
        if (usedPercent >= 100) return BudgetLineStatus.Limit;
        if (usedPercent >= 80) return BudgetLineStatus.Attention;
        return BudgetLineStatus.Normal;
    }

    public static string StatusLabel(BudgetLineStatus status) => status switch
    {
        BudgetLineStatus.Normal => "Normal",
        BudgetLineStatus.Attention => "Atención",
        BudgetLineStatus.Limit => "Límite",
        BudgetLineStatus.Exceeded => "Excedido",
        _ => "—"
    };

    public static Brush StatusBrush(BudgetLineStatus status) => BrushHelper.FromHex(status switch
    {
        BudgetLineStatus.Normal => "#35E0A1",
        BudgetLineStatus.Attention => "#FFB84D",
        BudgetLineStatus.Limit => "#27D3FF",
        BudgetLineStatus.Exceeded => "#FF6B6B",
        _ => "#93A4BD"
    });

    public static Brush ProgressBrush(BudgetLineStatus status) => StatusBrush(status);

    public static string AllocationModeLabel(AllocationMode mode) => mode switch
    {
        AllocationMode.Percentage => "Porcentaje",
        AllocationMode.FixedAmount => "Monto fijo",
        AllocationMode.Manual => "Manual",
        _ => mode.ToString()
    };
}
