using Ahorro.Models.Enums;

namespace Ahorro.Helpers;

public static class ScheduledPaymentLabels
{
    public static string Status(ScheduledPaymentStatus status) => status switch
    {
        ScheduledPaymentStatus.Pending => "Pendiente",
        ScheduledPaymentStatus.Upcoming => "Próximo",
        ScheduledPaymentStatus.Paid => "Pagado",
        ScheduledPaymentStatus.Overdue => "Vencido",
        _ => status.ToString()
    };

    public static string StatusColor(ScheduledPaymentStatus status) => status switch
    {
        ScheduledPaymentStatus.Pending => "#93A4BD",
        ScheduledPaymentStatus.Upcoming => "#27D3FF",
        ScheduledPaymentStatus.Paid => "#35E0A1",
        ScheduledPaymentStatus.Overdue => "#FF6B6B",
        _ => "#93A4BD"
    };

    public static string Frequency(IncomeFrequency frequency) => frequency switch
    {
        IncomeFrequency.Monthly => "Mensual",
        IncomeFrequency.Biweekly => "Quincenal",
        IncomeFrequency.OneTime => "Único",
        _ => frequency.ToString()
    };
}
