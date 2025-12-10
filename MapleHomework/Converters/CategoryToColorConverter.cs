using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MapleHomework.Models;
using Color = System.Windows.Media.Color;

namespace MapleHomework.Converters
{
    public class CategoryToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskCategory category)
            {
                return category switch
                {
                    TaskCategory.Daily => new SolidColorBrush(Color.FromRgb(90, 200, 250)),    // 시안 (일간)
                    TaskCategory.Weekly => new SolidColorBrush(Color.FromRgb(255, 149, 0)),   // 오렌지 (주간)
                    TaskCategory.Boss => new SolidColorBrush(Color.FromRgb(255, 59, 48)),     // 레드 (보스)
                    TaskCategory.Monthly => new SolidColorBrush(Color.FromRgb(175, 82, 222)), // 퍼플 (월간)
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

