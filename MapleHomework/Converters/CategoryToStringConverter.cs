using System;
using System.Globalization;
using System.Windows.Data;
using MapleHomework.Models;

namespace MapleHomework.Converters
{
    public class CategoryToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskCategory category)
            {
                return category switch
                {
                    TaskCategory.Daily => "일간",
                    TaskCategory.Weekly => "주간",
                    TaskCategory.Boss => "보스",
                    TaskCategory.Monthly => "월간",
                    _ => "기타"
                };
            }
            return "기타";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

