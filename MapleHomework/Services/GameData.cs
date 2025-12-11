using System.Collections.Generic;
using MapleHomework.Models;

namespace MapleHomework.Services
{
    /// <summary>
    /// 보스 정보 (난이도, 수익 포함)
    /// </summary>
    public class BossInfo
    {
        public string Name { get; set; } = "";
        public TaskCategory Category { get; set; }
        public BossDifficulty DefaultDifficulty { get; set; }
        public List<BossDifficulty> AvailableDifficulties { get; set; } = new();
        public bool IsActiveByDefault { get; set; } = false;
        
        // 난이도별 수익 (1인 기준)
        public Dictionary<BossDifficulty, long> Rewards { get; set; } = new();
    }

    public static class GameData
    {
        // 일일 퀘스트 목록 (RequiredLevel = 해당 지역 최소 레벨)
        public static List<HomeworkTask> Dailies = new()
        {
            new() { Name = "몬스터파크", Category = TaskCategory.Daily, RequiredLevel = 0 },
            new() { Name = "소멸의 여로", Category = TaskCategory.Daily, RequiredLevel = 200 },
            new() { Name = "츄츄 아일랜드", Category = TaskCategory.Daily, RequiredLevel = 210 },
            new() { Name = "레헬른", Category = TaskCategory.Daily, RequiredLevel = 220 },
            new() { Name = "아르카나", Category = TaskCategory.Daily, RequiredLevel = 225 },
            new() { Name = "모라스", Category = TaskCategory.Daily, RequiredLevel = 230 },
            new() { Name = "에스페라", Category = TaskCategory.Daily, RequiredLevel = 235 },
            new() { Name = "문브릿지", Category = TaskCategory.Daily, RequiredLevel = 245 },
            new() { Name = "고통의 미궁", Category = TaskCategory.Daily, RequiredLevel = 250 },
            new() { Name = "리멘", Category = TaskCategory.Daily, RequiredLevel = 255 },
            new() { Name = "세르니움", Category = TaskCategory.Daily, RequiredLevel = 260 },
            new() { Name = "호텔 아르크스", Category = TaskCategory.Daily, RequiredLevel = 265 },
            new() { Name = "오디움", Category = TaskCategory.Daily, RequiredLevel = 270 },
            new() { Name = "도원경", Category = TaskCategory.Daily, RequiredLevel = 275 },
            new() { Name = "아르테리아", Category = TaskCategory.Daily, RequiredLevel = 280 },
            new() { Name = "카르시온", Category = TaskCategory.Daily, RequiredLevel = 285 },
            new() { Name = "탈라하트", Category = TaskCategory.Daily, RequiredLevel = 290 },
            new() { Name = "데일리 기프트", Category = TaskCategory.Daily, RequiredLevel = 0 }
        };

        // 주간 퀘스트 목록
        public static List<HomeworkTask> Weeklies = new()
        {
            new() { Name = "몬스터파크 익스트림", Category = TaskCategory.Weekly },
            new() { Name = "아즈모스 협곡", Category = TaskCategory.Weekly },
            new() { Name = "에픽 던전 : 하이마운틴", Category = TaskCategory.Weekly },
            new() { Name = "에픽 던전 : 앵글러 컴퍼니", Category = TaskCategory.Weekly },
            new() { Name = "에픽 던전 : 악몽 선경", Category = TaskCategory.Weekly },
            new() { Name = "무릉도장", Category = TaskCategory.Weekly },
            new() { Name = "길드 지하수로", Category = TaskCategory.Weekly },
            new() { Name = "길드 플래그 레이스", Category = TaskCategory.Weekly },
            new() { Name = "아케인 주간 퀘스트", Category = TaskCategory.Weekly },
            new() { Name = "크리티아스", Category = TaskCategory.Weekly },
            new() { Name = "타락한 세계수", Category = TaskCategory.Weekly },
            new() { Name = "헤이븐", Category = TaskCategory.Weekly },
            new() { Name = "유니온 주간 드래곤", Category = TaskCategory.Weekly }
        };

        // 주간 보스 정보 (난이도 + 수익 통합)
        public static List<BossInfo> WeeklyBosses = new()
        {
            new() { Name = "자쿰", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Chaos, IsActiveByDefault = true,
                    AvailableDifficulties = new() { BossDifficulty.Chaos },
                    Rewards = new() { { BossDifficulty.Chaos, 8_080_000 } } },
            
            new() { Name = "매그너스", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Hard, IsActiveByDefault = true,
                    AvailableDifficulties = new() { BossDifficulty.Hard },
                    Rewards = new() { { BossDifficulty.Hard, 8_560_000 } } },
            
            new() { Name = "힐라", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Hard, IsActiveByDefault = true,
                    AvailableDifficulties = new() { BossDifficulty.Hard },
                    Rewards = new() { { BossDifficulty.Hard, 5_750_000 } } },
            
            new() { Name = "파풀라투스", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Chaos, IsActiveByDefault = true,
                    AvailableDifficulties = new() { BossDifficulty.Chaos },
                    Rewards = new() { { BossDifficulty.Chaos, 13_800_000 } } },
            
            new() { Name = "피에르", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Chaos, IsActiveByDefault = true,
                    AvailableDifficulties = new() { BossDifficulty.Chaos },
                    Rewards = new() { { BossDifficulty.Chaos, 8_170_000 } } },
            
            new() { Name = "반반", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Chaos, IsActiveByDefault = true,
                    AvailableDifficulties = new() { BossDifficulty.Chaos },
                    Rewards = new() { { BossDifficulty.Chaos, 8_150_000 } } },
            
            new() { Name = "블러디퀸", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Chaos, IsActiveByDefault = true,
                    AvailableDifficulties = new() { BossDifficulty.Chaos },
                    Rewards = new() { { BossDifficulty.Chaos, 8_140_000 } } },
            
            new() { Name = "벨룸", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Chaos, IsActiveByDefault = true,
                    AvailableDifficulties = new() { BossDifficulty.Chaos },
                    Rewards = new() { { BossDifficulty.Chaos, 9_280_000 } } },
            
            new() { Name = "핑크빈", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Chaos, IsActiveByDefault = true,
                    AvailableDifficulties = new() { BossDifficulty.Chaos },
                    Rewards = new() { { BossDifficulty.Chaos, 6_580_000 } } },
            
            new() { Name = "시그너스", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Normal, IsActiveByDefault = true,
                    AvailableDifficulties = new() { BossDifficulty.Easy, BossDifficulty.Normal },
                    Rewards = new() { { BossDifficulty.Easy, 4_550_000 }, { BossDifficulty.Normal, 7_500_000 } } },
            
            new() { Name = "스우", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Normal, IsActiveByDefault = true,
                    AvailableDifficulties = new() { BossDifficulty.Normal, BossDifficulty.Hard, BossDifficulty.Extreme },
                    Rewards = new() { { BossDifficulty.Normal, 17_600_000 }, { BossDifficulty.Hard, 54_200_000 }, { BossDifficulty.Extreme, 604_000_000 } } },
            
            new() { Name = "데미안", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Normal, IsActiveByDefault = true,
                    AvailableDifficulties = new() { BossDifficulty.Normal, BossDifficulty.Hard },
                    Rewards = new() { { BossDifficulty.Normal, 18_400_000 }, { BossDifficulty.Hard, 51_500_000 } } },
            
            new() { Name = "가디언 엔젤 슬라임", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Normal, IsActiveByDefault = false,
                    AvailableDifficulties = new() { BossDifficulty.Normal, BossDifficulty.Chaos },
                    Rewards = new() { { BossDifficulty.Normal, 26_800_000 }, { BossDifficulty.Chaos, 79_100_000 } } },
            
            new() { Name = "루시드", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Normal, IsActiveByDefault = false,
                    AvailableDifficulties = new() { BossDifficulty.Easy, BossDifficulty.Normal, BossDifficulty.Hard },
                    Rewards = new() { { BossDifficulty.Easy, 31_400_000 }, { BossDifficulty.Normal, 37_500_000 }, { BossDifficulty.Hard, 66_200_000 } } },
            
            new() { Name = "윌", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Normal, IsActiveByDefault = false,
                    AvailableDifficulties = new() { BossDifficulty.Easy, BossDifficulty.Normal, BossDifficulty.Hard },
                    Rewards = new() { { BossDifficulty.Easy, 34_000_000 }, { BossDifficulty.Normal, 43_300_000 }, { BossDifficulty.Hard, 81_200_000 } } },
            
            new() { Name = "더스크", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Normal, IsActiveByDefault = false,
                    AvailableDifficulties = new() { BossDifficulty.Normal, BossDifficulty.Chaos },
                    Rewards = new() { { BossDifficulty.Normal, 46_300_000 }, { BossDifficulty.Chaos, 73_500_000 } } },
            
            new() { Name = "진 힐라", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Normal, IsActiveByDefault = false,
                    AvailableDifficulties = new() { BossDifficulty.Normal, BossDifficulty.Hard },
                    Rewards = new() { { BossDifficulty.Normal, 74_900_000 }, { BossDifficulty.Hard, 112_000_000 } } },
            
            new() { Name = "듄켈", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Normal, IsActiveByDefault = false,
                    AvailableDifficulties = new() { BossDifficulty.Normal, BossDifficulty.Hard },
                    Rewards = new() { { BossDifficulty.Normal, 50_000_000 }, { BossDifficulty.Hard, 99_400_000 } } },
            
            new() { Name = "선택받은 세렌", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Normal, IsActiveByDefault = false,
                    AvailableDifficulties = new() { BossDifficulty.Normal, BossDifficulty.Hard, BossDifficulty.Extreme },
                    Rewards = new() { { BossDifficulty.Normal, 266_000_000 }, { BossDifficulty.Hard, 396_000_000 }, { BossDifficulty.Extreme, 3_150_000_000 } } },
            
            new() { Name = "감시자 칼로스", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Normal, IsActiveByDefault = false,
                    AvailableDifficulties = new() { BossDifficulty.Easy, BossDifficulty.Normal, BossDifficulty.Chaos, BossDifficulty.Extreme },
                    Rewards = new() { { BossDifficulty.Easy, 311_000_000 }, { BossDifficulty.Normal, 561_000_000 }, { BossDifficulty.Chaos, 1_340_000_000 }, { BossDifficulty.Extreme, 4_320_000_000 } } },
            
            new() { Name = "최초의 대적자", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Normal, IsActiveByDefault = false,
                    AvailableDifficulties = new() { BossDifficulty.Easy, BossDifficulty.Normal, BossDifficulty.Hard, BossDifficulty.Extreme },
                    Rewards = new() { { BossDifficulty.Easy, 324_000_000 }, { BossDifficulty.Normal, 589_000_000 }, { BossDifficulty.Hard, 1_510_000_000 }, { BossDifficulty.Extreme, 4_960_000_000 } } },
            
            new() { Name = "카링", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Normal, IsActiveByDefault = false,
                    AvailableDifficulties = new() { BossDifficulty.Easy, BossDifficulty.Normal, BossDifficulty.Hard, BossDifficulty.Extreme },
                    Rewards = new() { { BossDifficulty.Easy, 419_000_000 }, { BossDifficulty.Normal, 714_000_000 }, { BossDifficulty.Hard, 1_830_000_000 }, { BossDifficulty.Extreme, 5_670_000_000 } } },
            
            new() { Name = "림보", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Normal, IsActiveByDefault = false,
                    AvailableDifficulties = new() { BossDifficulty.Normal, BossDifficulty.Hard },
                    Rewards = new() { { BossDifficulty.Normal, 1_080_000_000 }, { BossDifficulty.Hard, 2_510_000_000 } } },
            
            new() { Name = "발드릭스", Category = TaskCategory.Boss, DefaultDifficulty = BossDifficulty.Normal, IsActiveByDefault = false,
                    AvailableDifficulties = new() { BossDifficulty.Normal, BossDifficulty.Hard },
                    Rewards = new() { { BossDifficulty.Normal, 1_440_000_000 }, { BossDifficulty.Hard, 3_240_000_000 } } }
        };

        // 월간 보스 정보
        public static List<BossInfo> MonthlyBosses = new()
        {
            new() { Name = "검은 마법사", Category = TaskCategory.Monthly, DefaultDifficulty = BossDifficulty.Hard, IsActiveByDefault = true,
                    AvailableDifficulties = new() { BossDifficulty.Hard, BossDifficulty.Extreme },
                    Rewards = new() { { BossDifficulty.Hard, 700_000_000 }, { BossDifficulty.Extreme, 9_200_000_000 } } }
        };

        // 기존 코드 호환용 (Bosses, Monthlies)
        public static List<HomeworkTask> Bosses => CreateBossTasksFromInfo(WeeklyBosses);
        public static List<HomeworkTask> Monthlies => CreateBossTasksFromInfo(MonthlyBosses);

        private static List<HomeworkTask> CreateBossTasksFromInfo(List<BossInfo> bossInfos)
        {
            var tasks = new List<HomeworkTask>();
            foreach (var info in bossInfos)
            {
                tasks.Add(new HomeworkTask
                {
                    Name = info.Name,
                    Category = info.Category,
                    Difficulty = info.DefaultDifficulty,
                    AvailableDifficulties = new List<BossDifficulty>(info.AvailableDifficulties),
                    IsActive = info.IsActiveByDefault
                });
            }
            return tasks;
        }

        /// <summary>
        /// 보스 이름으로 BossInfo 찾기
        /// </summary>
        public static BossInfo? GetBossInfo(string bossName, bool isMonthly = false)
        {
            var list = isMonthly ? MonthlyBosses : WeeklyBosses;
            return list.Find(b => b.Name == bossName);
        }

        /// <summary>
        /// 보스 수익 가져오기 (파티원 수 적용)
        /// </summary>
        public static long GetBossReward(string bossName, BossDifficulty difficulty, int partySize, bool isMonthly = false)
        {
            var info = GetBossInfo(bossName, isMonthly);
            if (info == null) return 0;
            
            if (info.Rewards.TryGetValue(difficulty, out long reward))
            {
                return reward / partySize;
            }
            return 0;
        }

        /// <summary>
        /// 보스의 사용 가능한 난이도 가져오기
        /// </summary>
        public static List<BossDifficulty> GetAvailableDifficulties(string bossName, bool isMonthly = false)
        {
            var info = GetBossInfo(bossName, isMonthly);
            return info?.AvailableDifficulties ?? new List<BossDifficulty> { BossDifficulty.Normal };
        }
    }
}
