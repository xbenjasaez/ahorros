using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Ahorro.Models.Enums;

namespace Ahorro.Themes.Converters;

public class BudgetStatusToRowBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not BudgetLineStatus status)
            return Brushes.Transparent;

        var hex = status switch
        {
            BudgetLineStatus.Exceeded => "#22FF6B6B",
            BudgetLineStatus.Limit => "#1A27D3FF",
            BudgetLineStatus.Attention => "#1AFFB84D",
            _ => "Transparent"
        };

        if (hex == "Transparent")
            return Brushes.Transparent;

        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
        brush.Freeze();
        return brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
