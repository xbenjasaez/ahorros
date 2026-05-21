using System.Windows;
using System.Windows.Media;
using Ahorro.Services.Abstractions;

namespace Ahorro.Services.Infrastructure;

public class ThemeService : IThemeService
{
    private static readonly Dictionary<string, (string Background, string Panel, string Card)> Variants = new()
    {
        ["dark-premium"] = ("#0D1117", "#131A22", "#18222D"),
        ["dark-midnight"] = ("#080B10", "#0E141C", "#151C26"),
        ["dark-emerald"] = ("#0A1210", "#111A17", "#172420")
    };

    public void Apply(string themeVariant, string accentHex)
    {
        if (!Variants.TryGetValue(themeVariant, out var palette))
            palette = Variants["dark-premium"];

        var app = Application.Current;
        if (app == null) return;

        SetColor(app, "Color.BackgroundApp", palette.Background);
        SetColor(app, "Color.Panel", palette.Panel);
        SetColor(app, "Color.Card", palette.Card);
        SetBrush(app, "Brush.BackgroundApp", palette.Background);
        SetBrush(app, "Brush.Panel", palette.Panel);
        SetBrush(app, "Brush.Card", palette.Card);

        if (!TryParseHex(accentHex, out var accent))
            TryParseHex("#27D3FF", out accent);

        SetColor(app, "Color.AccentCyan", accent);
        SetBrush(app, "Brush.AccentCyan", accent);
    }

    private static void SetColor(Application app, string key, string hex) =>
        app.Resources[key] = (Color)ColorConverter.ConvertFromString(hex)!;

    private static void SetBrush(Application app, string key, string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
        brush.Freeze();
        app.Resources[key] = brush;
    }

    private static bool TryParseHex(string hex, out string normalized)
    {
        normalized = "#27D3FF";
        try
        {
            if (string.IsNullOrWhiteSpace(hex)) return false;
            var c = (Color)ColorConverter.ConvertFromString(hex.Trim())!;
            normalized = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            return true;
        }
        catch
        {
            return false;
        }
    }
}
