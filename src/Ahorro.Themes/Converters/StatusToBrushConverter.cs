using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Ahorro.Models.Enums;

namespace Ahorro.Themes.Converters;

public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not BudgetLineStatus status)
            return new SolidColorBrush(Color.FromRgb(0x93, 0xA4, 0xBD));

        var hex = status switch
        {
            BudgetLineStatus.Normal => "#35E0A1",
            BudgetLineStatus.Attention => "#FFB84D",
            BudgetLineStatus.Limit => "#27D3FF",
            BudgetLineStatus.Exceeded => "#FF6B6B",
            _ => "#93A4BD"
        };
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
