using System;
using System.Globalization;
using System.Windows.Data;

namespace MapleHomework.Converters
{
    /// <summary>
    /// total - base 차이를 계산해 +숫자 형태로 반환. 파싱 실패나 0 이하면 빈 문자열.
    /// </summary>
    public class StatDeltaConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return string.Empty;

            if (!TryParseInt(values[0], out var total)) return string.Empty;
            if (!TryParseInt(values[1], out var @base)) return string.Empty;

            var delta = total - @base;
            if (delta <= 0) return string.Empty;

            return $" (+{delta})";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        private bool TryParseInt(object? value, out int result)
        {
            if (value == null)
            {
                result = 0;
                return false;
            }

            var s = value.ToString();
            // , 제거 후 파싱
            if (s != null && s.Contains(","))
                s = s.Replace(",", "");

            return int.TryParse(s, out result);
        }
    }
}

