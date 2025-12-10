using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using MapleHomework.Models;

namespace MapleHomework.Converters
{
    public class TasksToTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<HomeworkTask> tasks)
            {
                var incompleteTasks = tasks
                    .Where(t => t.IsActive && !t.IsChecked)
                    .Select(t => t.Name)
                    .ToList();

                if (incompleteTasks.Count == 0)
                {
                    return "모두 완료!";
                }

                var sb = new StringBuilder();
                sb.AppendLine("미완료 항목:");
                foreach (var name in incompleteTasks)
                {
                    sb.AppendLine($"- {name}");
                }
                return sb.ToString().TrimEnd();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

