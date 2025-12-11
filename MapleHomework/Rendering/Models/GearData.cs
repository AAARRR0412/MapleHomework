using System;
using System.Collections.Generic;
using MapleHomework.Rendering.Core;

namespace MapleHomework.Rendering.Models
{
    /// <summary>
    /// 장비 아이템 데이터 모델 (Nexon API 응답 기반)
    /// </summary>
    public class GearData
    {
        public string ItemEquipmentPart { get; set; } = "";
        public string ItemEquipmentSlot { get; set; } = "";
        public string ItemName { get; set; } = "";
        public string ItemIcon { get; set; } = "";
        public string ItemDescription { get; set; } = "";
        public string ItemShapeName { get; set; } = "";
        public string ItemShapeIcon { get; set; } = "";

        public int Starforce { get; set; }
        public bool StarforceScrollable { get; set; }
        public int ScrollUpgradeableCount { get; set; }
        public int ScrollUpgradeCount { get; set; }
        public int ScrollResilienceCount { get; set; }
        public bool GoldenHammerFlag { get; set; }
        public int CuttableCount { get; set; }

        public string PotentialOptionGrade { get; set; } = "";
        public string PotentialOption1 { get; set; } = "";
        public string PotentialOption2 { get; set; } = "";
        public string PotentialOption3 { get; set; } = "";
        public string AdditionalPotentialOptionGrade { get; set; } = "";
        public string AdditionalPotentialOption1 { get; set; } = "";
        public string AdditionalPotentialOption2 { get; set; } = "";
        public string AdditionalPotentialOption3 { get; set; } = "";

        public bool ExceptionalUpgrade { get; set; }
        public GearStatOption? ExceptionalOption { get; set; }

        public GearStatOption? BaseOption { get; set; }
        public GearStatOption? AddOption { get; set; }
        public GearStatOption? EtcOption { get; set; }
        public GearStatOption? StarforceOption { get; set; }
        public GearStatOption? TotalOption { get; set; }

        public string SoulName { get; set; } = "";
        public string SoulOption { get; set; } = "";

        public int EquipmentLevelDecrease { get; set; }
        public int SpecialRingLevel { get; set; }
        public int RequiredLevel { get; set; } = 200;
        public string JobClass { get; set; } = "";
        public int KarmaType { get; set; }

        public PotentialGrade GetPotentialGrade()
        {
            return ParseGrade(PotentialOptionGrade);
        }

        public PotentialGrade GetAdditionalPotentialGrade()
        {
            return ParseGrade(AdditionalPotentialOptionGrade);
        }

        private static PotentialGrade ParseGrade(string grade)
        {
            if (string.IsNullOrEmpty(grade)) return PotentialGrade.None;
            return grade.ToLower() switch
            {
                "레어" or "rare" => PotentialGrade.Rare,
                "에픽" or "epic" => PotentialGrade.Epic,
                "유니크" or "unique" => PotentialGrade.Unique,
                "레전드리" or "legendary" => PotentialGrade.Legendary,
                _ => PotentialGrade.None
            };
        }

        public int GetBonusStatTotal()
        {
            if (AddOption == null) return 0;
            return AddOption.Str + AddOption.Dex + AddOption.Int + AddOption.Luk +
                   AddOption.MaxHp / 10 + AddOption.MaxMp / 10 +
                   AddOption.AttackPower + AddOption.MagicPower +
                   AddOption.AllStat * 10;
        }

        public bool HasScrollUpgrade() => ScrollUpgradeCount > 0;

        public List<string> GetPotentialOptions()
        {
            var options = new List<string>();
            if (!string.IsNullOrEmpty(PotentialOption1)) options.Add(PotentialOption1);
            if (!string.IsNullOrEmpty(PotentialOption2)) options.Add(PotentialOption2);
            if (!string.IsNullOrEmpty(PotentialOption3)) options.Add(PotentialOption3);
            return options;
        }

        public List<string> GetAdditionalPotentialOptions()
        {
            var options = new List<string>();
            if (!string.IsNullOrEmpty(AdditionalPotentialOption1)) options.Add(AdditionalPotentialOption1);
            if (!string.IsNullOrEmpty(AdditionalPotentialOption2)) options.Add(AdditionalPotentialOption2);
            if (!string.IsNullOrEmpty(AdditionalPotentialOption3)) options.Add(AdditionalPotentialOption3);
            return options;
        }

        public bool IsUntradeable() => KarmaType > 0;
    }

    public class GearStatOption
    {
        public int Str { get; set; }
        public int Dex { get; set; }
        public int Int { get; set; }
        public int Luk { get; set; }
        public int MaxHp { get; set; }
        public int MaxMp { get; set; }
        public int AttackPower { get; set; }
        public int MagicPower { get; set; }
        public int Armor { get; set; }
        public int Speed { get; set; }
        public int Jump { get; set; }
        public int BossDamage { get; set; }
        public int IgnoreMonsterArmor { get; set; }
        public int AllStat { get; set; }
        public int Damage { get; set; }
        public int EquipmentLevelDecrease { get; set; }
        public int MaxHpRate { get; set; }
        public int MaxMpRate { get; set; }
        public int BaseEquipmentLevel { get; set; }
    }
}

