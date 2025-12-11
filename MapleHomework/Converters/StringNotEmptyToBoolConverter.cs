using System;
using System.Globalization;
using System.Windows.Data;

namespace MapleHomework.Converters
{
    /// <summary>
    /// 문자열이 null/empty/whitespace가 아니면 true, 아니면 false를 반환
    /// ToolTip 활성화 여부 등에 사용
    /// </summary>
    public class StringNotEmptyToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string s && !string.IsNullOrWhiteSpace(s);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

