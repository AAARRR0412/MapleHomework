using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MapleHomework.Models
{
    public class OcidResponse
    {
        [JsonPropertyName("ocid")]
        public string? Ocid { get; set; }
    }

    public class CharacterBasicResponse
    {
        [JsonPropertyName("character_name")]
        public string? CharacterName { get; set; }

        [JsonPropertyName("world_name")]
        public string? WorldName { get; set; }

        [JsonPropertyName("character_class")]
        public string? CharacterClass { get; set; }

        [JsonPropertyName("character_level")]
        public int CharacterLevel { get; set; }

        [JsonPropertyName("character_image")]
        public string? CharacterImage { get; set; }

        [JsonPropertyName("character_exp")]
        public long CharacterExp { get; set; }

        [JsonPropertyName("character_exp_rate")]
        public string? CharacterExpRate { get; set; }

        [JsonPropertyName("character_guild_name")]
        public string? CharacterGuildName { get; set; }
    }

    // 심볼 장비 응답
    public class SymbolEquipmentResponse
    {
        [JsonPropertyName("character_class")]
        public string? CharacterClass { get; set; }

        [JsonPropertyName("symbol")]
        public List<SymbolInfo>? Symbol { get; set; }
    }

    public class SymbolInfo
    {
        [JsonPropertyName("symbol_name")]
        public string? SymbolName { get; set; }

        [JsonPropertyName("symbol_level")]
        public int SymbolLevel { get; set; }

        [JsonPropertyName("symbol_force")]
        public string? SymbolForce { get; set; } // 아케인포스 or 어센틱포스

        [JsonPropertyName("symbol_str")]
        public string? SymbolStr { get; set; }

        [JsonPropertyName("symbol_dex")]
        public string? SymbolDex { get; set; }

        [JsonPropertyName("symbol_int")]
        public string? SymbolInt { get; set; }

        [JsonPropertyName("symbol_luk")]
        public string? SymbolLuk { get; set; }

        [JsonPropertyName("symbol_hp")]
        public string? SymbolHp { get; set; }
    }

    // 유니온 챔피언 응답
    public class UnionChampionResponse
    {
        [JsonPropertyName("union_champion")]
        public List<UnionChampionInfo>? UnionChampion { get; set; }
    }

    public class UnionChampionInfo
    {
        [JsonPropertyName("champion_name")]
        public string? ChampionName { get; set; }

        [JsonPropertyName("champion_grade")]
        public string? ChampionGrade { get; set; }

        [JsonPropertyName("champion_class")]
        public string? ChampionClass { get; set; }
    }

    // 유니온 정보 응답
    public class UnionResponse
    {
        [JsonPropertyName("union_level")]
        public int UnionLevel { get; set; }

        [JsonPropertyName("union_grade")]
        public string? UnionGrade { get; set; }

        [JsonPropertyName("union_artifact_level")]
        public int UnionArtifactLevel { get; set; }

        [JsonPropertyName("union_artifact_exp")]
        public long UnionArtifactExp { get; set; }

        [JsonPropertyName("union_artifact_point")]
        public int UnionArtifactPoint { get; set; }
    }

    // 유니온 공격대원 정보 응답 (신규)
    public class UnionRaiderResponse
    {
        [JsonPropertyName("union_raider_stat")]
        public List<string>? UnionRaiderStat { get; set; }

        [JsonPropertyName("union_raider_preset1")]
        public List<UnionBlockInfo>? UnionRaiderPreset1 { get; set; }
        
        [JsonPropertyName("union_raider_preset2")]
        public List<UnionBlockInfo>? UnionRaiderPreset2 { get; set; }

        [JsonPropertyName("union_raider_preset3")]
        public List<UnionBlockInfo>? UnionRaiderPreset3 { get; set; }

        [JsonPropertyName("union_raider_preset4")]
        public List<UnionBlockInfo>? UnionRaiderPreset4 { get; set; }

        [JsonPropertyName("union_raider_preset5")]
        public List<UnionBlockInfo>? UnionRaiderPreset5 { get; set; }
    }

    public class UnionBlockInfo 
    {
        [JsonPropertyName("block_type")]
        public string? BlockType { get; set; }

        [JsonPropertyName("block_class")]
        public string? BlockClass { get; set; }

        [JsonPropertyName("block_level")]
        public string? BlockLevel { get; set; }
    }


    // 캐릭터 스탯 응답
    public class CharacterStatResponse
    {
        [JsonPropertyName("character_class")]
        public string? CharacterClass { get; set; }

        [JsonPropertyName("final_stat")]
        public List<StatInfo>? FinalStat { get; set; }
    }

    public class StatInfo
    {
        [JsonPropertyName("stat_name")]
        public string? StatName { get; set; }

        [JsonPropertyName("stat_value")]
        public string? StatValue { get; set; }
    }

    // 헥사 스킬 응답 (기존)
    public class HexaStatResponse
    {
        [JsonPropertyName("character_class")]
        public string? CharacterClass { get; set; }

        [JsonPropertyName("character_hexa_core_equipment")]
        public List<HexaCoreInfo>? HexaCoreEquipment { get; set; }
    }

    public class HexaCoreInfo
    {
        [JsonPropertyName("hexa_core_name")]
        public string? HexaCoreName { get; set; }

        [JsonPropertyName("hexa_core_level")]
        public int HexaCoreLevel { get; set; }

        [JsonPropertyName("hexa_core_type")]
        public string? HexaCoreType { get; set; }

        [JsonPropertyName("linked_skill")]
        public List<LinkedSkillInfo>? LinkedSkill { get; set; }
    }

    public class LinkedSkillInfo
    {
        [JsonPropertyName("hexa_skill_id")]
        public string? HexaSkillId { get; set; }
    }

    // 캐릭터 스킬 응답 (신규 - 6차 스킬 아이콘 확보용)
    public class CharacterSkillResponse
    {
        [JsonPropertyName("character_skill_grade")]
        public string? CharacterSkillGrade { get; set; }

        [JsonPropertyName("character_skill")]
        public List<CharacterSkillInfo>? CharacterSkill { get; set; }
    }

    public class CharacterSkillInfo
    {
        [JsonPropertyName("skill_name")]
        public string? SkillName { get; set; }

        [JsonPropertyName("skill_description")]
        public string? SkillDescription { get; set; }

        [JsonPropertyName("skill_level")]
        public int SkillLevel { get; set; }

        [JsonPropertyName("skill_effect")]
        public string? SkillEffect { get; set; }

        [JsonPropertyName("skill_icon")]
        public string? SkillIcon { get; set; }
    }

    // 장비 아이템 응답
    public class ItemEquipmentResponse
    {
        [JsonPropertyName("character_class")]
        public string? CharacterClass { get; set; }

        [JsonPropertyName("character_gender")]
        public string? CharacterGender { get; set; }

        [JsonPropertyName("item_equipment")]
        public List<ItemEquipmentInfo>? ItemEquipment { get; set; }
    }

    public class ItemEquipmentInfo
    {
        [JsonPropertyName("item_equipment_part")]
        public string? ItemEquipmentPart { get; set; }

        [JsonPropertyName("item_equipment_slot")]
        public string? ItemEquipmentSlot { get; set; }

        [JsonPropertyName("item_name")]
        public string? ItemName { get; set; }

        [JsonPropertyName("item_icon")]
        public string? ItemIcon { get; set; }

        [JsonPropertyName("item_description")]
        public string? ItemDescription { get; set; }

        [JsonPropertyName("item_shape_name")]
        public string? ItemShapeName { get; set; }

        [JsonPropertyName("item_shape_icon")]
        public string? ItemShapeIcon { get; set; }

        [JsonPropertyName("item_total_option")]
        public ItemOptionInfo? ItemTotalOption { get; set; }

        [JsonPropertyName("item_base_option")]
        public ItemOptionInfo? ItemBaseOption { get; set; }

        [JsonPropertyName("item_add_option")]
        public ItemOptionInfo? ItemAddOption { get; set; }

        [JsonPropertyName("item_etc_option")]
        public ItemOptionInfo? ItemEtcOption { get; set; }

        [JsonPropertyName("item_starforce_option")]
        public ItemOptionInfo? ItemStarforceOption { get; set; }

        [JsonPropertyName("item_exceptional_option")]
        public ItemOptionInfo? ItemExceptionalOption { get; set; }

        [JsonPropertyName("potential_option_grade")]
        public string? PotentialOptionGrade { get; set; }

        [JsonPropertyName("additional_potential_option_grade")]
        public string? AdditionalPotentialOptionGrade { get; set; }

        [JsonPropertyName("potential_option_1")]
        public string? PotentialOption1 { get; set; }

        [JsonPropertyName("potential_option_2")]
        public string? PotentialOption2 { get; set; }

        [JsonPropertyName("potential_option_3")]
        public string? PotentialOption3 { get; set; }

        [JsonPropertyName("additional_potential_option_1")]
        public string? AdditionalPotentialOption1 { get; set; }

        [JsonPropertyName("additional_potential_option_2")]
        public string? AdditionalPotentialOption2 { get; set; }

        [JsonPropertyName("additional_potential_option_3")]
        public string? AdditionalPotentialOption3 { get; set; }

        [JsonPropertyName("scroll_upgrade")]
        public string? ScrollUpgrade { get; set; }

        [JsonPropertyName("scroll_upgradeable_count")]
        public string? ScrollUpgradeableCount { get; set; }

        [JsonPropertyName("scroll_resilience_count")]
        public string? ScrollResilienceCount { get; set; }

        [JsonPropertyName("cuttable_count")]
        public string? CuttableCount { get; set; }

        [JsonPropertyName("golden_hammer_flag")]
        public string? GoldenHammerFlag { get; set; }

        [JsonPropertyName("starforce")]
        public string? Starforce { get; set; }

        [JsonPropertyName("starforce_scroll_flag")]
        public string? StarforceScrollFlag { get; set; }

        [JsonPropertyName("special_ring_level")]
        public string? SpecialRingLevel { get; set; }

        [JsonPropertyName("soul_name")]
        public string? SoulName { get; set; }

        [JsonPropertyName("soul_option")]
        public string? SoulOption { get; set; }
    }

    public class ItemOptionInfo
    {
        [JsonPropertyName("str")]
        public string? Str { get; set; }

        [JsonPropertyName("dex")]
        public string? Dex { get; set; }

        [JsonPropertyName("int")]
        public string? Int { get; set; }

        [JsonPropertyName("luk")]
        public string? Luk { get; set; }

        [JsonPropertyName("max_hp")]
        public string? MaxHp { get; set; }

        [JsonPropertyName("max_mp")]
        public string? MaxMp { get; set; }

        [JsonPropertyName("attack_power")]
        public string? AttackPower { get; set; }

        [JsonPropertyName("magic_power")]
        public string? MagicPower { get; set; }

        [JsonPropertyName("armor")]
        public string? Armor { get; set; }

        [JsonPropertyName("speed")]
        public string? Speed { get; set; }

        [JsonPropertyName("jump")]
        public string? Jump { get; set; }

        [JsonPropertyName("boss_damage")]
        public string? BossDamage { get; set; }

        [JsonPropertyName("ignore_monster_armor")]
        public string? IgnoreMonsterArmor { get; set; }

        [JsonPropertyName("all_stat")]
        public string? AllStat { get; set; }

        [JsonPropertyName("damage")]
        public string? Damage { get; set; }

        [JsonPropertyName("equipment_level_decrease")]
        public System.Text.Json.JsonElement? EquipmentLevelDecrease { get; set; }

        [JsonPropertyName("max_hp_rate")]
        public string? MaxHpRate { get; set; }

        [JsonPropertyName("max_mp_rate")]
        public string? MaxMpRate { get; set; }

        // 추가 필드: 일부 응답에 존재
        [JsonPropertyName("base_equipment_level")]
        public System.Text.Json.JsonElement? BaseEquipmentLevel { get; set; }
    }
}
