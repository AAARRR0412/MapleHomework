using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MapleHomework.Converters
{
    public class ProgressToDashArrayConverter : IValueConverter
    {
        // 원형 게이지 둘레 (Width=54, StrokeThickness=6 기준: π × (54-6) ≈ 150.8)
        private const double CircleCircumference = Math.PI * 48;
        private const double StrokeThickness = 6.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress)
            {
                // 진행률을 0-100 사이로 제한
                progress = Math.Max(0, Math.Min(100, progress));
                
                // dashLength: 진행률만큼의 길이 (픽셀 단위)
                double dashLengthPixel = CircleCircumference * progress / 100;
                
                // WPF의 StrokeDashArray는 StrokeThickness에 대한 비율로 값을 받습니다.
                // 따라서 픽셀 길이를 StrokeThickness로 나누어 주어야 합니다.
                double dashLength = dashLengthPixel / StrokeThickness;
                double gapLength = CircleCircumference / StrokeThickness;

                return new DoubleCollection(new[] { dashLength, gapLength });
            }
            return new DoubleCollection(new[] { 0.0, CircleCircumference / StrokeThickness });
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

