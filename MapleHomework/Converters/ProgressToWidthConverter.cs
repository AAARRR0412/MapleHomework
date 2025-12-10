using System;
using System.Globalization;
using System.Windows.Data;

namespace MapleHomework.Converters
{
    /// <summary>
    /// 진행률(0-100)을 너비로 변환 (대시보드용)
    /// 부모 컨테이너의 실제 너비를 기준으로 계산
    /// </summary>
    public class ProgressToWidthConverter : IValueConverter
    {
        // 기본 최대 너비 (진행률 바의 실제 너비)
        private const double MaxWidth = 120;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress)
            {
                // 0-100 범위를 0-MaxWidth로 변환
                double clampedProgress = Math.Clamp(progress, 0, 100);
                return clampedProgress / 100 * MaxWidth;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

