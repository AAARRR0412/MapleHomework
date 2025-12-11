using System;
using System.Globalization;
using System.Windows.Data;

namespace MapleHomework.Converters
{
    public class RateToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double rate && double.TryParse(parameter?.ToString(), out double maxWidth))
            {
                // 최소 5px 보장
                return Math.Max(5, rate * maxWidth);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

