using System;
using System.Globalization;
using System.Text.Json;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using MapleHomework.Rendering;
using MapleHomework.Models;
using MapleHomework.Services;

namespace MapleHomework.Converters
{
    /// <summary>
    /// ItemInfoJson을 GDI+ 렌더링된 툴팁 이미지로 변환하는 컨버터
    /// MapleTooltipRenderer를 사용하여 메이플스토리 UI와 동일한 툴팁을 생성합니다.
    /// </summary>
    public class ItemTooltipImageConverter : IValueConverter
    {
        private static readonly ConcurrentDictionary<string, ImageSource> Cache = new();
        private static readonly bool EnableDebugLog = true;

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string json;
            string? jobClass = null;

            if (value is string s)
            {
                json = s;
            }
            else if (value is ItemChangeRecord record)
            {
                json = record.ItemInfoJson;
                jobClass = record.JobClass;
            }
            else
            {
                return null;
            }

            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var itemInfo = JsonSerializer.Deserialize<ItemEquipmentInfo>(json, options);
                if (itemInfo == null)
                {
                    Log("Deserialize failed: itemInfo is null");
                    return null;
                }

                // 캐시 키: json 해시
                var key = GetHash(json);
                if (Cache.TryGetValue(key, out var cached))
                {
                    return cached;
                }

                Log($"Render start: {itemInfo.ItemName}");

                // MapleTooltipRenderer로 렌더링
                // parameter에서 직업 정보 추출 (JSON 또는 문자열)
                if (parameter is string jobClassStr)
                {
                    jobClass = jobClassStr;
                }
                else if (parameter is System.Text.Json.JsonElement jobClassJson && jobClassJson.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    jobClass = jobClassJson.GetString();
                }
                // JSON에서 character_class 추출 시도
                if (string.IsNullOrEmpty(jobClass))
                {
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(json);
                        if (jsonDoc.RootElement.TryGetProperty("character_class", out var charClass))
                        {
                            jobClass = charClass.GetString();
                        }
                    }
                    catch { }
                }

                var tooltipBitmap = MapleTooltipRenderer.RenderEquipmentTooltip(itemInfo, jobClass);
                var tooltipImage = WpfBitmapConverter.ToImageSource(tooltipBitmap);
                if (tooltipImage != null)
                {
                    Log($"Render success via MapleTooltipRenderer: {itemInfo.ItemName} (size {tooltipBitmap.Width}x{tooltipBitmap.Height})");
                    Cache[key] = tooltipImage;
                    return tooltipImage;
                }
                Log($"MapleTooltipRenderer returned null: {itemInfo.ItemName}");

                // 렌더 실패 시 빈 값 대신 투명 1px 반환 (툴팁이 검은 사각형으로 뜨는 것 방지)
                var fallback = new BitmapImage();
                fallback.BeginInit();
                fallback.StreamSource = new MemoryStream(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82, 0, 0, 0, 1, 0, 0, 0, 1, 8, 6, 0, 0, 0, 31, 21, 196, 137, 0, 0, 0, 12, 73, 68, 65, 84, 120, 156, 99, 248, 15, 4, 0, 9, 251, 3, 253, 167, 176, 23, 120, 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 });
                fallback.CacheOption = BitmapCacheOption.OnLoad;
                fallback.EndInit();
                fallback.Freeze();
                Log($"Render fallback (1px) for item: {itemInfo.ItemName}");
                Cache[key] = fallback;
                return fallback;
            }
            catch (Exception ex)
            {
                Log("Render exception", ex);
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static void Log(string message, Exception? ex = null)
        {
            if (!EnableDebugLog) return;
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var logPath = Path.Combine(baseDir, "tooltip_render.log");
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                if (ex != null) line += $" | EX: {ex.Message}";
                File.AppendAllText(logPath, line + Environment.NewLine);
                Debug.WriteLine(line);
                if (ex?.StackTrace != null)
                {
                    File.AppendAllText(logPath, ex.StackTrace + Environment.NewLine);
                }
            }
            catch
            {
                // ignore logging errors
            }
        }

        private static string GetHash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return System.Convert.ToHexString(hash);
        }
    }
}
