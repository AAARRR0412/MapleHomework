using System;
using System.Globalization;
using System.Text.Json;
using System.Windows.Data;
using MapleHomework.Models;

namespace MapleHomework.Converters
{
    public class JsonToItemInfoConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string json && !string.IsNullOrEmpty(json))
            {
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var item = JsonSerializer.Deserialize<ItemEquipmentInfo>(json, options);

                    // 파라미터로 특정 필드를 바로 꺼낼 수 있도록 지원
                    if (parameter is string mode && mode.Equals("Icon", StringComparison.OrdinalIgnoreCase))
                    {
                        return item?.ItemIcon;
                    }

                    return item;
                }
                catch
                {
                    // 실패해도 기본 객체를 반환하여 텍스트 표시가 되도록 함
                    return new ItemEquipmentInfo { ItemName = "정보 없음" };
                }
            }
            // 빈 문자열인 경우에도 기본 객체 제공
            return new ItemEquipmentInfo { ItemName = "정보 없음" };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

