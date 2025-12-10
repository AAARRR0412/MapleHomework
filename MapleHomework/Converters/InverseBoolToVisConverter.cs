using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MapleHomework.Converters
{
    /// <summary>
    /// Boolean 값을 반전하여 Visibility로 변환
    /// True → Collapsed, False → Visible
    /// </summary>
    public class InverseBoolToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }
            return false;
        }
    }
}

