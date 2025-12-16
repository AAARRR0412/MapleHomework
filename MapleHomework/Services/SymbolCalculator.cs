using System;
using System.Collections.Generic;

namespace MapleHomework.Services
{
    /// <summary>
    /// 심볼 종류
    /// </summary>
    public enum SymbolType
    {
        Unknown,
        Arcane,       // 아케인 심볼
        Authentic,    // 어센틱 심볼
        GrandAuthentic // 그랜드 심볼
    }

    /// <summary>
    /// 심볼 관련 계산기
    /// </summary>
    public static class SymbolCalculator
    {
        // 아케인 심볼 이름 목록
        private static readonly HashSet<string> ArcaneSymbolNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "소멸의 여로", "츄츄 아일랜드", "레헬른", "아르카나", "모라스", "에스페라"
        };

        // 어센틱 심볼 이름 목록
        private static readonly HashSet<string> AuthenticSymbolNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "세르니움", "아르크스", "오디움", "도원경", "아르테리아", "카르시온"
        };

        // 그랜드 심볼 이름 목록
        private static readonly HashSet<string> GrandSymbolNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "탈라하트"
        };

        // 아케인 심볼 레벨별 성장 요구량
        private static readonly int[] ArcaneGrowthRequirements =
        {
            0, 12, 15, 20, 27, 36, 47, 60, 75, 92,  // 1~10레벨
            111, 132, 155, 180, 207, 236, 267, 300, 335, 372  // 11~20레벨
        };

        // 어센틱 심볼 레벨별 성장 요구량
        private static readonly int[] AuthenticGrowthRequirements =
        {
            0, 29, 76, 141, 224, 325, 444, 581, 736, 909, 1100  // 1~11레벨
        };

        // 그랜드 심볼 레벨별 성장 요구량 (예상치)
        private static readonly int[] GrandGrowthRequirements =
        {
            0, 50, 130, 240, 380, 550, 750, 980, 1240, 1530, 1850  // 1~11레벨
        };

        // 아케인 레벨업 비용 (메소) - 레벨 1->2부터
        private static readonly long[] ArcaneLevelUpCosts =
        {
            11060000, 12900000, 14880000, 17000000, 19270000, 21680000,
            24230000, 26920000, 29760000, 32740000, 35870000, 39140000,
            42560000, 46120000, 49830000, 53690000, 57690000, 61840000, 66130000
        };

        // 어센틱 레벨업 비용 (메소) - 레벨 1->2부터
        private static readonly long[] AuthenticLevelUpCosts =
        {
            56710000, 75500000, 97800000, 123600000, 152900000,
            185700000, 222000000, 261800000, 305100000, 351900000
        };

        /// <summary>
        /// 심볼 이름으로 심볼 종류를 반환
        /// </summary>
        public static SymbolType GetSymbolType(string symbolName)
        {
            if (string.IsNullOrEmpty(symbolName)) return SymbolType.Unknown;

            // 심볼 이름에서 "아케인심볼 :" 또는 "어센틱심볼 :" 접두사 제거
            var cleanName = symbolName
                .Replace("아케인심볼 :", "")
                .Replace("어센틱심볼 :", "")
                .Replace("그랜드심볼 :", "")
                .Trim();

            if (ArcaneSymbolNames.Contains(cleanName)) return SymbolType.Arcane;
            if (AuthenticSymbolNames.Contains(cleanName)) return SymbolType.Authentic;
            if (GrandSymbolNames.Contains(cleanName)) return SymbolType.GrandAuthentic;

            // 접두사로 판단
            if (symbolName.Contains("아케인")) return SymbolType.Arcane;
            if (symbolName.Contains("어센틱")) return SymbolType.Authentic;
            if (symbolName.Contains("그랜드")) return SymbolType.GrandAuthentic;

            return SymbolType.Unknown;
        }

        /// <summary>
        /// 해당 심볼 타입의 최대 레벨 반환
        /// </summary>
        public static int GetMaxLevel(SymbolType type)
        {
            return type switch
            {
                SymbolType.Arcane => 20,
                SymbolType.Authentic => 11,
                SymbolType.GrandAuthentic => 11,
                _ => 0
            };
        }

        /// <summary>
        /// 남은 성장 횟수와 비용 계산
        /// </summary>
        /// <returns>(남은 성장 횟수, 남은 비용 메소)</returns>
        public static (int remainingCount, long remainingCost) CalculateRemaining(string symbolName, int currentLevel, long currentGrowth)
        {
            var type = GetSymbolType(symbolName);
            int maxLevel = GetMaxLevel(type);

            if (currentLevel >= maxLevel)
            {
                return (0, 0);
            }

            int[] growthReqs;
            long[] levelUpCosts;

            switch (type)
            {
                case SymbolType.Arcane:
                    growthReqs = ArcaneGrowthRequirements;
                    levelUpCosts = ArcaneLevelUpCosts;
                    break;
                case SymbolType.Authentic:
                    growthReqs = AuthenticGrowthRequirements;
                    levelUpCosts = AuthenticLevelUpCosts;
                    break;
                case SymbolType.GrandAuthentic:
                    growthReqs = GrandGrowthRequirements;
                    levelUpCosts = AuthenticLevelUpCosts; // 그랜드도 어센틱과 동일 비용 가정
                    break;
                default:
                    return (0, 0);
            }

            // 남은 성장 횟수 계산
            int remainingCount = 0;
            long remainingCost = 0;

            // 현재 레벨에서 남은 성장
            if (currentLevel > 0 && currentLevel < growthReqs.Length)
            {
                remainingCount += (int)Math.Max(0, growthReqs[currentLevel] - currentGrowth);
            }

            // 다음 레벨들의 성장 요구량 합산
            for (int lvl = currentLevel + 1; lvl < maxLevel && lvl < growthReqs.Length; lvl++)
            {
                remainingCount += growthReqs[lvl];
            }

            // 남은 레벨업 비용 계산
            for (int lvl = currentLevel; lvl < maxLevel && (lvl - 1) < levelUpCosts.Length; lvl++)
            {
                if (lvl > 0 && (lvl - 1) < levelUpCosts.Length)
                {
                    remainingCost += levelUpCosts[lvl - 1];
                }
            }

            return (remainingCount, remainingCost);
        }
    }
}
