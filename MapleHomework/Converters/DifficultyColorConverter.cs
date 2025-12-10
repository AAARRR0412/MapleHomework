using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MapleHomework.Models;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;

namespace MapleHomework.Converters
{
    public class DifficultyColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BossDifficulty difficulty)
            {
                return difficulty switch
                {
                    // 메이플스토리 난이도 색상 테마 반영
                    BossDifficulty.Easy => new SolidColorBrush(Color.FromRgb(160, 160, 160)),    // 회색 (Easy)
                    BossDifficulty.Normal => new SolidColorBrush(Color.FromRgb(60, 140, 230)),   // 파랑 (Normal)
                    BossDifficulty.Hard => new SolidColorBrush(Color.FromRgb(230, 80, 80)),      // 빨강 (Hard)
                    BossDifficulty.Chaos => new SolidColorBrush(Color.FromRgb(50, 50, 60)),      // 진한 검정 (Chaos)
                    BossDifficulty.Extreme => new SolidColorBrush(Color.FromRgb(0, 0, 0)),       // 완전 검정 (Extreme)
                    _ => new SolidColorBrush(Color.FromRgb(100, 100, 100))
                };
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}