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
}
