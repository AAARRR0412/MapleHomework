using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MapleHomework.Models;
using MapleHomework.Rendering;
using MapleHomework.Rendering.Models;

namespace MapleHomework.Converters
{
    public class SkillTooltipImageConverter : IValueConverter
    {
        // 캐시 저장소 (Key: SkillName_Level_IconUrl)
        private static readonly ConcurrentDictionary<string, ImageSource> Cache = new();

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not HexaCoreItem item)
            {
                return null;
            }

            string key = $"{item.SkillName}_Lv{item.CoreLevel}_{item.SkillIcon}";

            // 1. 캐시 확인
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            try
            {
                // 2. SkillTooltipData 생성
                var skillData = CreateSkillData(item);

                // 3. 아이콘 이미지 로드 (동기 - 최초 1회만 발생하므로 허용)
                if (!string.IsNullOrEmpty(item.SkillIcon))
                {
                    try
                    {
                        // ImageHelper.LoadBitmapFromUrlAsync는 비동기지만, Converter는 동기여야 하므로 대기
                        // (ItemTooltipImageConverter와 동일한 패턴)
                        var bitmap = ImageHelper.LoadBitmapFromUrlAsync(item.SkillIcon).GetAwaiter().GetResult();
                        if (bitmap != null)
                        {
                            skillData.IconBitmap = bitmap;
                        }
                    }
                    catch
                    {
                        // 이미지 로드 실패 시 아이콘 없이 렌더링
                    }
                }

                // 4. 렌더링
                var renderer = new SkillTooltipRenderer(skillData);
                renderer.ShowProperties = false;

                using (var tooltipBitmap = renderer.Render())
                {
                    // 5. 비트맵 변환 (WpfBitmapConverter.ToBitmapSource는 DeleteObject 처리됨)
                    var imageSource = WpfBitmapConverter.ToBitmapSource(tooltipBitmap);

                    if (imageSource != null)
                    {
                        Cache[key] = imageSource;
                        return imageSource;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Skill Tooltip Render Error: {ex}");
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private SkillTooltipData CreateSkillData(HexaCoreItem item)
        {
            string description = $"{item.CoreType}\n";
            description += $"\n[다음 레벨 비용]\n솔 에르다: {item.NextSolErdaText}, 조각: {item.NextFragmentText}";
            description += $"\n[졸업까지 비용]\n솔 에르다: {item.RemainingSolErdaText}, 조각: {item.RemainingFragmentText}";

            return new SkillTooltipData
            {
                Name = item.SkillName,
                Level = item.CoreLevel,
                MaxLevel = 30,
                IsOrigin = item.CoreType == "스킬 코어",
                IsAscent = item.CoreType == "마스터리 코어" || item.CoreType == "강화 코어",
                IconUrl = item.SkillIcon,
                Description = description,
                ReqSkills = new Dictionary<int, int>()
            };
        }
    }
}
