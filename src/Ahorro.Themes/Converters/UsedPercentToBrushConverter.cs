using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Ahorro.Themes.Converters;

public class UsedPercentToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var pct = value switch
        {
            decimal d => d,
            double dbl => (decimal)dbl,
            int i => i,
            _ => 0m
        };

        var hex = pct switch
        {
            > 100 => "#FF6B6B",
            >= 100 => "#27D3FF",
            >= 80 => "#FFB84D",
            _ => "#35E0A1"
        };

        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
        brush.Freeze();
        return brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
