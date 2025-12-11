using System;
using System.Globalization;
using System.Windows.Data;

namespace MapleHomework.Converters
{
    /// <summary>
    /// 스타포스 별을 그룹별로 반환하는 컨버터
    /// Parameter: "green" (1-5), "yellow" (6-15), "red" (16-25), "all" (전체 노란별)
    /// </summary>
    public class StarRepeatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            if (string.IsNullOrWhiteSpace(str)) return string.Empty;
            if (!int.TryParse(str, out var star)) return string.Empty;
            star = Math.Max(0, Math.Min(star, 25));
            
            var group = parameter as string ?? "all";
            
            switch (group.ToLower())
            {
                case "green":
                    // 1-5성: 초록
                    var greenCount = Math.Min(star, 5);
                    return greenCount > 0 ? new string('★', greenCount) : string.Empty;
                    
                case "yellow":
                    // 6-15성: 노랑
                    if (star <= 5) return string.Empty;
                    var yellowCount = Math.Min(star - 5, 10);
                    return yellowCount > 0 ? new string('★', yellowCount) : string.Empty;
                    
                case "red":
                    // 16-25성: 빨강
                    if (star <= 15) return string.Empty;
                    var redCount = Math.Min(star - 15, 10);
                    return redCount > 0 ? new string('★', redCount) : string.Empty;
                    
                case "all":
                default:
                    return star > 0 ? new string('★', star) : string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

