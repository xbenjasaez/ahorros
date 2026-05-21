using Ahorro.Models.Enums;

namespace Ahorro.Helpers;

public static class SettingsLabels
{
    public static string LabelPeriodFrequency(PeriodFrequency f) => f switch
    {
        PeriodFrequency.Monthly => "Mensual",
        PeriodFrequency.Biweekly => "Quincenal",
        _ => f.ToString()
    };

    public static string LabelBudgetGroup(BudgetGroup g) => g switch
    {
        BudgetGroup.Needs => "Necesidades",
        BudgetGroup.Wants => "Deseos",
        BudgetGroup.Savings => "Ahorro",
        BudgetGroup.Other => "Otros",
        _ => g.ToString()
    };

    public static string LabelPaymentMethodType(PaymentMethodType t) => t switch
    {
        PaymentMethodType.Cash => "Efectivo",
        PaymentMethodType.Debit => "Débito",
        PaymentMethodType.Credit => "Crédito",
        PaymentMethodType.Transfer => "Transferencia",
        _ => t.ToString()
    };

    public static string LabelThemeVariant(string id) => id switch
    {
        "dark-premium" => "Oscuro premium",
        "dark-midnight" => "Medianoche",
        "dark-emerald" => "Esmeralda",
        _ => "Oscuro premium"
    };
}
