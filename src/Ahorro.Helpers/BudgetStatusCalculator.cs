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
}
