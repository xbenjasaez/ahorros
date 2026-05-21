using System.Windows.Media;

namespace Ahorro.Helpers;

public static class GoalGlowHelper
{
    public static Brush GlowFromHex(string hex, double opacity = 0.22)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hex))
                hex = "#35E0A1";

            var baseColor = (Color)ColorConverter.ConvertFromString(hex.Trim())!;
            var glow = Color.FromArgb((byte)(opacity * 255), baseColor.R, baseColor.G, baseColor.B);
            var brush = new SolidColorBrush(glow);
            brush.Freeze();
            return brush;
        }
        catch
        {
            return BrushHelper.FromHex("#35E0A1");
        }
    }

    public static Brush TrackBrush() => BrushHelper.FromHex("#141C26");
}
