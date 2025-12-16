using System.Collections.Generic;
using System.Linq;
using MapleHomework.Data;

namespace MapleHomework.Services
{
    public static class HexaCostCalculator
    {
        // JSON 로드 필요 없음 (정적 데이터 사용)
        public static void Initialize(string dataPath)
        {
            // Do nothing
        }

        public static (int solErda, int fragment) GetNextLevelCost(string type, int currentLevel)
        {
            var list = GetCostList(type);
            // currentLevel + 1 레벨의 비용을 찾음 (예: 1레벨 -> 2레벨 갈 때 비용은 Level 2 데이터에 있음)
            // 참고: HexaCoreCosts.json 구조 상 "level 2" 항목이 1->2 가는 비용임.

            // 만약 currentLevel이 0이면(미보유->1) Level 1 비용 반환
            int targetLevel = currentLevel + 1;
            var costData = list?.FirstOrDefault(x => x.Level == targetLevel);

            if (costData != null)
            {
                return (costData.SolErda, costData.Fragment);
            }

            return (0, 0); // Max level or unknown
        }

        public static (int solErda, int fragment) GetRemainingCost(string type, int currentLevel)
        {
            var list = GetCostList(type);
            if (list == null) return (0, 0);

            // 현재 레벨 다음 레벨부터 30레벨까지의 비용 합계
            // 예: 현재 1레벨 -> 2,3,4...30 레벨 비용 합산
            var remaining = list.Where(x => x.Level > currentLevel && x.Level <= 30);

            return (remaining.Sum(x => x.SolErda), remaining.Sum(x => x.Fragment));
        }

        private static List<HexaCoreConstants.HexaLevelCost>? GetCostList(string type)
        {
            return type?.ToLower() switch
            {
                "skill" or "origin" or "스킬 코어" => HexaCoreConstants.SkillCore,
                "mastery" or "마스터리 코어" => HexaCoreConstants.MasteryCore,
                "reinforcement" or "boost" or "강화 코어" => HexaCoreConstants.ReinforcementCore,
                "common" or "공용 코어" => HexaCoreConstants.CommonCore,
                _ => HexaCoreConstants.SkillCore // Default
            };
        }
    }
}
