using System;

namespace MapleHomework.Models
{
    public class CharacterCacheData
    {
        public DateTime Timestamp { get; set; }
        public CharacterBasicResponse? BasicInfo { get; set; }
        public CharacterStatResponse? StatInfo { get; set; }
        public ItemEquipmentResponse? ItemEquipment { get; set; }
        public CharacterSkillResponse? CharacterSkill { get; set; }
        public HexaMatrixStatResponse? HexaMatrixStat { get; set; }
        public HexaStatResponse? HexaMatrix { get; set; }
        public SymbolEquipmentResponse? SymbolEquipment { get; set; }
        public UnionResponse? UnionRaider { get; set; }
        // public GuildBasicResponse? GuildBasic { get; set; }

        public bool IsExpired(int hours = 6)
        {
            return (DateTime.Now - Timestamp).TotalHours >= hours;
        }
    }
}
