using System;
using System.Globalization;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using MapleHomework.CharaSimResource;
using MapleHomework.Models;
using MapleHomework.Rendering;
using MapleHomework.Rendering.Core;
using MapleHomework.Rendering.Models;

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
            if (value is not string json || string.IsNullOrEmpty(json))
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

                // 1차: MapleTooltipRenderer (WzComparerR2 포팅)로 풀 UI 렌더
                var tooltipBitmap = MapleTooltipRenderer.RenderEquipmentTooltip(itemInfo);
                var tooltipImage = WpfBitmapConverter.ToImageSource(tooltipBitmap);
                if (tooltipImage != null)
                {
                    Log($"Render success via MapleTooltipRenderer: {itemInfo.ItemName} (size {tooltipBitmap.Width}x{tooltipBitmap.Height})");
                    Cache[key] = tooltipImage;
                    return tooltipImage;
                }
                Log($"MapleTooltipRenderer returned null: {itemInfo.ItemName}");

                // 2차: 기존 GearTooltipRenderer 경량 렌더 (아이콘은 비동기 로드하지 않으므로 UI만)
                var gear = ToGearData(itemInfo);
                if (gear != null)
                {
                var img = MapleRendererFactory.RenderGearTooltipAsImageSource(gear, use22Style: true);
                if (img != null)
                    {
                        Log($"Render success via GearTooltipRenderer: {itemInfo.ItemName}");
                        Cache[key] = img;
                    return img;
                    }
                    Log($"GearTooltipRenderer returned null: {itemInfo.ItemName}");
                }
                else
                {
                    Log($"ToGearData returned null for item: {itemInfo.ItemName}");
                }

                // 렌더 실패 시 빈 값 대신 투명 1px 반환 (툴팁이 검은 사각형으로 뜨는 것 방지)
                var fallback = new BitmapImage();
                fallback.BeginInit();
                fallback.StreamSource = new MemoryStream(new byte[] { 137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82,0,0,0,1,0,0,0,1,8,6,0,0,0,31,21,196,137,0,0,0,12,73,68,65,84,120,156,99,248,15,4,0,9,251,3,253,167,176,23,120,0,0,0,0,73,69,78,68,174,66,96,130 });
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

        private static GearData? ToGearData(ItemEquipmentInfo item)
        {
            try
            {
                // 옵션 파싱
                var baseOpt = ToStatOption(item.ItemBaseOption);
                var totalOpt = ToStatOption(item.ItemTotalOption);
                var addOptApi = ToStatOption(item.ItemAddOption);
                var etcOpt = ToStatOption(item.ItemEtcOption);
                var starOpt = ToStatOption(item.ItemStarforceOption);
                var excOpt = ToStatOption(item.ItemExceptionalOption);

                // 추가옵션: API add_option이 없을 때는 (total - base - etc - star - exceptional)로 분리해야
                // 주문서/스타포스가 추옵에 섞여 들어가는 것을 방지
                GearStatOption SubtractEnhance(GearStatOption total, GearStatOption baseStat, GearStatOption etcStat, GearStatOption starStat, GearStatOption excStat)
                {
                    int diff(int t, int b, int e, int s, int c = 0) => Math.Max(0, t - b - e - s - c);
                    return new GearStatOption
                    {
                        Str = diff(total.Str, baseStat.Str, etcStat.Str, starStat.Str, excStat.Str),
                        Dex = diff(total.Dex, baseStat.Dex, etcStat.Dex, starStat.Dex, excStat.Dex),
                        Int = diff(total.Int, baseStat.Int, etcStat.Int, starStat.Int, excStat.Int),
                        Luk = diff(total.Luk, baseStat.Luk, etcStat.Luk, starStat.Luk, excStat.Luk),
                        MaxHp = diff(total.MaxHp, baseStat.MaxHp, etcStat.MaxHp, starStat.MaxHp, excStat.MaxHp),
                        MaxMp = diff(total.MaxMp, baseStat.MaxMp, etcStat.MaxMp, starStat.MaxMp, excStat.MaxMp),
                        AttackPower = diff(total.AttackPower, baseStat.AttackPower, etcStat.AttackPower, starStat.AttackPower, excStat.AttackPower),
                        MagicPower = diff(total.MagicPower, baseStat.MagicPower, etcStat.MagicPower, starStat.MagicPower, excStat.MagicPower),
                        Armor = diff(total.Armor, baseStat.Armor, etcStat.Armor, starStat.Armor, excStat.Armor),
                        BossDamage = diff(total.BossDamage, baseStat.BossDamage, etcStat.BossDamage, starStat.BossDamage, excStat.BossDamage),
                        IgnoreMonsterArmor = diff(total.IgnoreMonsterArmor, baseStat.IgnoreMonsterArmor, etcStat.IgnoreMonsterArmor, starStat.IgnoreMonsterArmor, excStat.IgnoreMonsterArmor),
                        AllStat = diff(total.AllStat, baseStat.AllStat, etcStat.AllStat, starStat.AllStat, excStat.AllStat),
                        Damage = diff(total.Damage, baseStat.Damage, etcStat.Damage, starStat.Damage, excStat.Damage),
                        EquipmentLevelDecrease = diff(total.EquipmentLevelDecrease, baseStat.EquipmentLevelDecrease, etcStat.EquipmentLevelDecrease, starStat.EquipmentLevelDecrease, excStat.EquipmentLevelDecrease),
                        MaxHpRate = diff(total.MaxHpRate, baseStat.MaxHpRate, etcStat.MaxHpRate, starStat.MaxHpRate, excStat.MaxHpRate),
                        MaxMpRate = diff(total.MaxMpRate, baseStat.MaxMpRate, etcStat.MaxMpRate, starStat.MaxMpRate, excStat.MaxMpRate),
                        Speed = diff(total.Speed, baseStat.Speed, etcStat.Speed, starStat.Speed, excStat.Speed),
                        Jump = diff(total.Jump, baseStat.Jump, etcStat.Jump, starStat.Jump, excStat.Jump),
                    };
                }

                var addOpt = addOptApi ?? SubtractEnhance(totalOpt, baseOpt, etcOpt ?? new GearStatOption(), starOpt ?? new GearStatOption(), excOpt ?? new GearStatOption());

                return new GearData
                {
                    ItemName = item.ItemName ?? "",
                    ItemIcon = item.ItemIcon ?? item.ItemShapeIcon ?? "",
                    ItemEquipmentPart = item.ItemEquipmentPart ?? "",
                    ItemEquipmentSlot = item.ItemEquipmentSlot ?? "",
                    ItemDescription = item.ItemDescription ?? "",
                    Starforce = ParseInt(item.Starforce),
                    PotentialOptionGrade = item.PotentialOptionGrade ?? "",
                    PotentialOption1 = item.PotentialOption1 ?? "",
                    PotentialOption2 = item.PotentialOption2 ?? "",
                    PotentialOption3 = item.PotentialOption3 ?? "",
                    AdditionalPotentialOptionGrade = item.AdditionalPotentialOptionGrade ?? "",
                    AdditionalPotentialOption1 = item.AdditionalPotentialOption1 ?? "",
                    AdditionalPotentialOption2 = item.AdditionalPotentialOption2 ?? "",
                    AdditionalPotentialOption3 = item.AdditionalPotentialOption3 ?? "",
                    SoulName = item.SoulName ?? "",
                    SoulOption = item.SoulOption ?? "",
                    ScrollUpgradeCount = ParseInt(item.ScrollUpgrade),
                    ScrollUpgradeableCount = ParseInt(item.ScrollUpgradeableCount),
                    ScrollResilienceCount = ParseInt(item.ScrollResilienceCount),
                    CuttableCount = ParseInt(item.CuttableCount),
                    GoldenHammerFlag = ParseBoolFlag(item.GoldenHammerFlag),
                    StarforceScrollable = !string.IsNullOrWhiteSpace(item.StarforceScrollFlag) && !item.StarforceScrollFlag.Contains("미사용"),
                    TotalOption = totalOpt,
                    BaseOption = baseOpt,
                    AddOption = addOpt,
                    EtcOption = etcOpt ?? new GearStatOption(),
                    StarforceOption = starOpt ?? new GearStatOption(),
                    ExceptionalOption = excOpt,
                    ExceptionalUpgrade = (excOpt?.EquipmentLevelDecrease ?? 0) > 0,
                    EquipmentLevelDecrease = ParseIntFlexible(item.ItemAddOption?.EquipmentLevelDecrease)
                        + ParseIntFlexible(item.ItemTotalOption?.EquipmentLevelDecrease)
                        + ParseIntFlexible(item.ItemBaseOption?.EquipmentLevelDecrease),
                    SpecialRingLevel = ParseIntFlexible(item.SpecialRingLevel),
                    RequiredLevel = ParseIntFlexible(item.ItemBaseOption?.BaseEquipmentLevel)
                };
            }
            catch
            {
                return null;
            }
        }

        private static GearStatOption ToStatOption(ItemOptionInfo? opt)
        {
            if (opt == null) return new GearStatOption();

            return new GearStatOption
            {
                Str = ParseInt(opt.Str),
                Dex = ParseInt(opt.Dex),
                Int = ParseInt(opt.Int),
                Luk = ParseInt(opt.Luk),
                MaxHp = ParseInt(opt.MaxHp),
                MaxMp = ParseInt(opt.MaxMp),
                AttackPower = ParseInt(opt.AttackPower),
                MagicPower = ParseInt(opt.MagicPower),
                Armor = ParseInt(opt.Armor),
                BossDamage = ParseInt(opt.BossDamage),
                IgnoreMonsterArmor = ParseInt(opt.IgnoreMonsterArmor),
                AllStat = ParseInt(opt.AllStat),
                Damage = ParseInt(opt.Damage),
                EquipmentLevelDecrease = ParseIntFlexible(opt.EquipmentLevelDecrease),
                MaxHpRate = ParseInt(opt.MaxHpRate),
                MaxMpRate = ParseInt(opt.MaxMpRate),
                Speed = ParseInt(opt.Speed),
                Jump = ParseInt(opt.Jump),
                BaseEquipmentLevel = ParseIntFlexible(opt.BaseEquipmentLevel),
            };
        }

        private static int ParseInt(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            var cleaned = s.Replace(",", "").Trim();
            return int.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;
        }

        private static int ParseIntFlexible(System.Text.Json.JsonElement? element)
        {
            if (element == null) return 0;
            var el = element.Value;
            switch (el.ValueKind)
            {
                case System.Text.Json.JsonValueKind.Number:
                    if (el.TryGetInt32(out var i)) return i;
                    if (el.TryGetDouble(out var d)) return (int)d;
                    break;
                case System.Text.Json.JsonValueKind.String:
                    return ParseInt(el.GetString());
            }
            return 0;
        }

        private static bool ParseBoolFlag(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            var val = s.Trim().ToLowerInvariant();
            if (val == "true" || val == "1" || val == "yes" || val == "y") return true;
            if (val.Contains("미사용") || val.Contains("미적용")) return false;
            return !val.Contains("false") && !val.Contains("0");
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

