using System.Globalization;

namespace Ahorro.Helpers;

public static class ClpFormatter
{
    private static readonly CultureInfo Chile = CultureInfo.GetCultureInfo("es-CL");

    public static string Format(decimal amount) =>
        amount.ToString("C0", Chile);

    public static string FormatCompact(decimal amount)
    {
        if (amount >= 1_000_000)
            return $"${amount / 1_000_000m:0.#}M";
        if (amount >= 1_000)
            return $"${amount / 1_000m:0.#}K";
        return Format(amount);
    }

    public static string FormatPercent(decimal value) => $"{value:0.#}%";
}
