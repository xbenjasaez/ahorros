using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Ahorro.Models.Enums;

namespace Ahorro.Themes.Converters;

public class TransactionStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not TransactionStatus status)
            return new SolidColorBrush(Color.FromRgb(0x93, 0xA4, 0xBD));
        var hex = status switch
        {
            TransactionStatus.Pending => "#FFB84D",
            TransactionStatus.Paid => "#35E0A1",
            TransactionStatus.Cancelled => "#93A4BD",
            _ => "#93A4BD"
        };
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
