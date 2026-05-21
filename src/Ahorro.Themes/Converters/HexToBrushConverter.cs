using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Ahorro.Themes.Converters;

public class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string hex || string.IsNullOrWhiteSpace(hex))
            return FallbackBrush();

        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex.Trim())!;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
        catch
        {
            return FallbackBrush();
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static SolidColorBrush FallbackBrush()
    {
        var brush = new SolidColorBrush(Color.FromRgb(0xEA, 0xF2, 0xFF));
        brush.Freeze();
        return brush;
    }
}
