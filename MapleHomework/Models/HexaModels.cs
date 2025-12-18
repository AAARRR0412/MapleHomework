using System;

namespace MapleHomework.Models
{
    // 헥사코어 아이템 클래스 (ReportWindow/CharacterSearchWindow 공용)
    public class HexaCoreItem
    {
        public string SkillName { get; set; } = "";
        public string OriginalName { get; set; } = "";
        public string CoreType { get; set; } = "";
        public int CoreLevel { get; set; }

        // UI Display Properties (Unified names)
        public string SkillIcon { get; set; } = "";
        public string BadgeIcon { get; set; } = "";

        // 강화 비용 정보
        public int NextSolErda { get; set; }
        public int NextFragment { get; set; }
        public int RemainingSolErda { get; set; }
        public int RemainingFragment { get; set; }

        public int OldLevel { get; set; } // For ReportWindow compatibility
        public int NewLevel { get; set; } // For ReportWindow compatibility

        public bool IsMaxLevel => CoreLevel >= 30;

        // UI Display Properties
        public string NextCostText => IsMaxLevel ? "MAX" : $"{NextSolErda} / {NextFragment}";
        public string RemainingCostText => IsMaxLevel ? "-" : $"{RemainingSolErda} / {RemainingFragment}";

        public string NextSolErdaText => IsMaxLevel ? "-" : $"{NextSolErda}개";
        public string NextFragmentText => IsMaxLevel ? "-" : $"{NextFragment}개";
        public string RemainingSolErdaText => IsMaxLevel ? "-" : $"{RemainingSolErda}개";
        public string RemainingFragmentText => IsMaxLevel ? "-" : $"{RemainingFragment}개";

        public double ProgressValue => CoreLevel >= 30 ? 100 : (CoreLevel / 30.0 * 100);
        public double ProgressFactor
        {
            get
            {
                if (CoreLevel >= 30) return 1.0;
                if (CoreLevel <= 0) return 0.0;
                double result = CoreLevel / 30.0;
                return double.IsNaN(result) || double.IsInfinity(result) ? 0.0 : result;
            }
        }
        public string ProgressText => $"{ProgressValue:F1}%";
    }

    // 헥사스텟 아이템
    public class HexaStatItem
    {
        public string MainStat { get; set; } = "";
        public int MainLevel { get; set; }
        public string SubStat1 { get; set; } = "";
        public int SubLevel1 { get; set; }
        public string SubStat2 { get; set; } = "";
        public int SubLevel2 { get; set; }
        public int Grade { get; set; }
        public int SlotIndex { get; set; }
    }

    public class HexaCoreGroup
    {
        public string TypeLabel { get; set; } = "";
        public string TypeIcon { get; set; } = "";
        public System.Collections.Generic.List<HexaCoreItem> Items { get; set; } = new();
    }
}
