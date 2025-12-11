using System;
using System.Drawing;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media;
using MapleHomework.Rendering.Core;
using MapleHomework.Rendering.Models;
using MapleHomework.Rendering.Tooltips;

namespace MapleHomework.Rendering
{
    /// <summary>
    /// 메이플스토리 UI 렌더러 팩토리
    /// API 응답 데이터를 바로 렌더링할 수 있도록 지원
    /// </summary>
    public static class MapleRendererFactory
    {
        /// <summary>
        /// 장비 데이터로 툴팁 비트맵 생성
        /// </summary>
        public static Bitmap? RenderGearTooltip(GearData gear, bool use22Style = true)
        {
            var renderer = new GearTooltipRenderer
            {
                Gear = gear,
                Use22Style = use22Style
            };
            return renderer.Render();
        }

        /// <summary>
        /// 장비 데이터로 툴팁 이미지 소스 생성 (WPF용)
        /// </summary>
        public static ImageSource? RenderGearTooltipAsImageSource(GearData gear, bool use22Style = true)
        {
            using var bitmap = RenderGearTooltip(gear, use22Style);
            return WpfBitmapConverter.ToImageSource(bitmap);
        }

        /// <summary>
        /// 장비 데이터로 툴팁 비트맵 생성 (아이콘 포함, 비동기)
        /// </summary>
        public static async Task<Bitmap?> RenderGearTooltipAsync(GearData gear, bool use22Style = true)
        {
            var renderer = new GearTooltipRenderer
            {
                Gear = gear,
                Use22Style = use22Style
            };
            await renderer.LoadIconAsync();
            return renderer.Render();
        }

        /// <summary>
        /// 장비 데이터로 툴팁 이미지 소스 생성 (아이콘 포함, 비동기)
        /// </summary>
        public static async Task<ImageSource?> RenderGearTooltipAsImageSourceAsync(GearData gear, bool use22Style = true)
        {
            using var bitmap = await RenderGearTooltipAsync(gear, use22Style);
            return WpfBitmapConverter.ToImageSource(bitmap);
        }

        /// <summary>
        /// 스킬 데이터로 툴팁 비트맵 생성
        /// </summary>
        public static Bitmap? RenderSkillTooltip(SkillData skill, bool use22Style = true)
        {
            var renderer = new SkillTooltipRenderer
            {
                Skill = skill,
                Use22Style = use22Style
            };
            return renderer.Render();
        }

        /// <summary>
        /// 스킬 데이터로 툴팁 이미지 소스 생성 (WPF용)
        /// </summary>
        public static ImageSource? RenderSkillTooltipAsImageSource(SkillData skill, bool use22Style = true)
        {
            using var bitmap = RenderSkillTooltip(skill, use22Style);
            return WpfBitmapConverter.ToImageSource(bitmap);
        }

        /// <summary>
        /// 스킬 데이터로 툴팁 비트맵 생성 (아이콘 포함, 비동기)
        /// </summary>
        public static async Task<Bitmap?> RenderSkillTooltipAsync(SkillData skill, bool use22Style = true)
        {
            var renderer = new SkillTooltipRenderer
            {
                Skill = skill,
                Use22Style = use22Style
            };
            await renderer.LoadIconAsync();
            return renderer.Render();
        }

        /// <summary>
        /// JSON 문자열에서 장비 데이터 파싱
        /// </summary>
        public static GearData? ParseGearFromJson(string? json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                };
                return JsonSerializer.Deserialize<GearData>(json, options);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Nexon API ItemEquipmentInfo에서 GearData로 변환
        /// </summary>
        public static GearData? ConvertFromApiResponse(dynamic? apiItem)
        {
            if (apiItem == null)
                return null;

            try
            {
                var gear = new GearData
                {
                    ItemName = apiItem.item_name?.ToString() ?? "",
                    ItemIcon = apiItem.item_icon?.ToString() ?? "",
                    ItemEquipmentPart = apiItem.item_equipment_part?.ToString() ?? "",
                    ItemEquipmentSlot = apiItem.item_equipment_slot?.ToString() ?? "",
                    ItemDescription = apiItem.item_description?.ToString() ?? "",
                    Starforce = int.TryParse(apiItem.starforce?.ToString(), out int sf) ? sf : 0,
                    PotentialOptionGrade = apiItem.potential_option_grade?.ToString() ?? "",
                    PotentialOption1 = apiItem.potential_option_1?.ToString() ?? "",
                    PotentialOption2 = apiItem.potential_option_2?.ToString() ?? "",
                    PotentialOption3 = apiItem.potential_option_3?.ToString() ?? "",
                    AdditionalPotentialOptionGrade = apiItem.additional_potential_option_grade?.ToString() ?? "",
                    AdditionalPotentialOption1 = apiItem.additional_potential_option_1?.ToString() ?? "",
                    AdditionalPotentialOption2 = apiItem.additional_potential_option_2?.ToString() ?? "",
                    AdditionalPotentialOption3 = apiItem.additional_potential_option_3?.ToString() ?? "",
                    SoulName = apiItem.soul_name?.ToString() ?? "",
                    SoulOption = apiItem.soul_option?.ToString() ?? "",
                    ScrollUpgradeCount = int.TryParse(apiItem.scroll_upgrade?.ToString(), out int su) ? su : 0,
                    ScrollUpgradeableCount = int.TryParse(apiItem.scroll_upgradeable_count?.ToString(), out int suc) ? suc : 0,
                };

                // 스탯 옵션 변환
                gear.TotalOption = ConvertStatOption(apiItem.item_total_option);
                gear.BaseOption = ConvertStatOption(apiItem.item_base_option);
                gear.AddOption = ConvertStatOption(apiItem.item_add_option);
                gear.EtcOption = ConvertStatOption(apiItem.item_etc_option);
                gear.StarforceOption = ConvertStatOption(apiItem.item_starforce_option);

                return gear;
            }
            catch
            {
                return null;
            }
        }

        private static GearStatOption ConvertStatOption(dynamic opt)
        {
            if (opt == null)
                return new GearStatOption();

            return new GearStatOption
            {
                Str = int.TryParse(opt.str?.ToString(), out int str) ? str : 0,
                Dex = int.TryParse(opt.dex?.ToString(), out int dex) ? dex : 0,
                Int = int.TryParse(opt.@int?.ToString(), out int @int) ? @int : 0,
                Luk = int.TryParse(opt.luk?.ToString(), out int luk) ? luk : 0,
                MaxHp = int.TryParse(opt.max_hp?.ToString(), out int hp) ? hp : 0,
                MaxMp = int.TryParse(opt.max_mp?.ToString(), out int mp) ? mp : 0,
                AttackPower = int.TryParse(opt.attack_power?.ToString(), out int atk) ? atk : 0,
                MagicPower = int.TryParse(opt.magic_power?.ToString(), out int matk) ? matk : 0,
                Armor = int.TryParse(opt.armor?.ToString(), out int armor) ? armor : 0,
                BossDamage = int.TryParse(opt.boss_damage?.ToString(), out int bd) ? bd : 0,
                IgnoreMonsterArmor = int.TryParse(opt.ignore_monster_armor?.ToString(), out int ied) ? ied : 0,
                AllStat = int.TryParse(opt.all_stat?.ToString(), out int allstat) ? allstat : 0,
            };
        }
    }
}

