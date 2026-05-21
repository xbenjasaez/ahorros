using System.Windows.Media;

namespace Ahorro.Helpers;

public static class BrushHelper
{
    public static Brush FromHex(string hex, string fallback = "#EAF2FF")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hex))
                hex = fallback;

            var color = (Color)ColorConverter.ConvertFromString(hex.Trim())!;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
        catch
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fallback)!);
            brush.Freeze();
            return brush;
        }
    }
}
