using Ahorro.Models.Enums;

namespace Ahorro.Helpers;

public static class TransactionLabels
{
    public static string Type(TransactionType type) => type switch
    {
        TransactionType.Income => "Ingreso",
        TransactionType.Expense => "Gasto",
        TransactionType.DebtPayment => "Pago deuda",
        TransactionType.Adjustment => "Ajuste",
        TransactionType.InternalTransfer => "Transferencia",
        _ => type.ToString()
    };

    public static string Status(TransactionStatus status) => status switch
    {
        TransactionStatus.Pending => "Pendiente",
        TransactionStatus.Paid => "Pagado",
        TransactionStatus.Cancelled => "Cancelado",
        _ => status.ToString()
    };

    public static string TypeColor(TransactionType type) => type switch
    {
        TransactionType.Income => "#35E0A1",
        TransactionType.Expense => "#E8EDF5",
        TransactionType.DebtPayment => "#FFB84D",
        TransactionType.Adjustment => "#9B7AFF",
        TransactionType.InternalTransfer => "#27D3FF",
        _ => "#93A4BD"
    };

    public static string StatusColor(TransactionStatus status) => status switch
    {
        TransactionStatus.Pending => "#FFB84D",
        TransactionStatus.Paid => "#35E0A1",
        TransactionStatus.Cancelled => "#93A4BD",
        _ => "#93A4BD"
    };

    public static string TypeBadgeBackground(TransactionType type) => type switch
    {
        TransactionType.Income => "#1A2E28",
        TransactionType.Expense => "#1A2430",
        TransactionType.DebtPayment => "#2A2418",
        TransactionType.Adjustment => "#221A30",
        TransactionType.InternalTransfer => "#1A2A35",
        _ => "#1A2430"
    };

    public static string StatusBadgeBackground(TransactionStatus status) => status switch
    {
        TransactionStatus.Pending => "#2A2418",
        TransactionStatus.Paid => "#1A2E28",
        TransactionStatus.Cancelled => "#1A2430",
        _ => "#1A2430"
    };
}
