using System;
using System.Globalization;
using System.Windows.Data;

namespace MapleHomework.Converters
{
    /// <summary>
    /// 두 값이 같은지 비교하여 bool 반환
    /// </summary>
    public class EqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;

            return values[0]?.Equals(values[1]) ?? (values[1] == null);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

