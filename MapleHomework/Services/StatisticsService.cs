using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MapleHomework.Data;

namespace MapleHomework.Services
{
    /// <summary>
    /// 일일 완료 기록
    /// </summary>
    public class DailyRecord
    {
        public DateTime Date { get; set; }
        public string CharacterId { get; set; } = "";
        public string CharacterName { get; set; } = "";
        public int DailyCompleted { get; set; }
        public int DailyTotal { get; set; }
        public int WeeklyCompleted { get; set; }
        public int WeeklyTotal { get; set; }
        public int BossCompleted { get; set; }
        public int BossTotal { get; set; }
        public int MonthlyCompleted { get; set; }
        public int MonthlyTotal { get; set; }
        public long BossRewardEarned { get; set; }
        
        public double DailyCompletionRate => DailyTotal > 0 ? (double)DailyCompleted / DailyTotal * 100 : 0;
        public double WeeklyCompletionRate => WeeklyTotal > 0 ? (double)WeeklyCompleted / WeeklyTotal * 100 : 0;
        public double BossCompletionRate => BossTotal > 0 ? (double)BossCompleted / BossTotal * 100 : 0;
        public double OverallCompletionRate
        {
            get
            {
                int total = DailyTotal + WeeklyTotal + BossTotal + MonthlyTotal;
                int completed = DailyCompleted + WeeklyCompleted + BossCompleted + MonthlyCompleted;
                return total > 0 ? (double)completed / total * 100 : 0;
            }
        }
    }

    /// <summary>
    /// 캐릭터 성장 기록 (경험치, 전투력, 유니온 포함)
    /// </summary>
    public class CharacterGrowthRecord
    {
        public DateTime Date { get; set; }
        public string CharacterId { get; set; } = "";
        public string CharacterName { get; set; } = "";
        public int Level { get; set; }
        public long Experience { get; set; }
        public double ExperienceRate { get; set; } // 현재 경험치 비율 (0~100)
        public long TotalExp { get; set; } // 누적 경험치 (ExpTable 기반 계산)
        public long CombatPower { get; set; }
        public long HighestCombatPower { get; set; } // 최고 전투력 기록
        public int UnionLevel { get; set; }
        public long UnionPower { get; set; }
    }

    /// <summary>
    /// 6차 스킬 강화 기록
    /// </summary>
    public class HexaSkillRecord
    {
        public DateTime Date { get; set; }
        public string CharacterId { get; set; } = "";
        public string CharacterName { get; set; } = "";
        public string SkillName { get; set; } = "";
        public string SkillIcon { get; set; } = ""; // 스킬 아이콘
        public int OldLevel { get; set; }
        public int NewLevel { get; set; }
    }

    /// <summary>
    /// 장비 변경 기록
    /// </summary>
    public class ItemChangeRecord
    {
        public DateTime Date { get; set; }
        public string CharacterId { get; set; } = "";
        public string CharacterName { get; set; } = "";
        public string ItemSlot { get; set; } = ""; // 무기, 모자, 상의 등
        public string OldItemName { get; set; } = "";
        public string NewItemName { get; set; } = "";
        public string ChangeType { get; set; } = ""; // 장착, 강화, 스타포스 등
        public string ItemInfoJson { get; set; } = ""; // 툴팁용 상세 정보 JSON
    }

    /// <summary>
    /// 주간 보스 수익 기록
    /// </summary>
    public class WeeklyBossRewardRecord
    {
        public DateTime WeekStartDate { get; set; } // 해당 주의 목요일
        public string CharacterId { get; set; } = "";
        public string CharacterName { get; set; } = "";
        public long TotalReward { get; set; }
        public Dictionary<string, long> BossRewards { get; set; } = new(); // 보스별 수익
    }

    /// <summary>
    /// 통계 데이터 전체
    /// </summary>
    public class StatisticsData
    {
        public List<DailyRecord> DailyRecords { get; set; } = new();
        public List<CharacterGrowthRecord> GrowthRecords { get; set; } = new();
        public List<WeeklyBossRewardRecord> BossRewardRecords { get; set; } = new();
        public List<HexaSkillRecord> HexaSkillRecords { get; set; } = new();
        public List<ItemChangeRecord> ItemChangeRecords { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 통계 서비스
    /// </summary>
    public static class StatisticsService
    {
        private const string FilePath = "statistics_data.json";
        private static readonly JsonSerializerOptions JsonOptions = new() 
        { 
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static StatisticsData Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return new StatisticsData();

                string json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<StatisticsData>(json, JsonOptions) ?? new StatisticsData();
            }
            catch
            {
                return new StatisticsData();
            }
        }

        public static void Save(StatisticsData data)
        {
            try
            {
                data.LastUpdated = DateTime.Now;
                string json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(FilePath, json);
            }
            catch { }
        }

        public static void RecordDailyCompletion(string characterId, string characterName,
            int dailyCompleted, int dailyTotal,
            int weeklyCompleted, int weeklyTotal,
            int bossCompleted, int bossTotal,
            int monthlyCompleted, int monthlyTotal,
            long bossRewardEarned)
        {
            var data = Load();
            var today = DateTime.Today;

            var existingRecord = data.DailyRecords
                .FirstOrDefault(r => r.Date.Date == today && r.CharacterId == characterId);

            if (existingRecord != null)
            {
                existingRecord.DailyCompleted = dailyCompleted;
                existingRecord.DailyTotal = dailyTotal;
                existingRecord.WeeklyCompleted = weeklyCompleted;
                existingRecord.WeeklyTotal = weeklyTotal;
                existingRecord.BossCompleted = bossCompleted;
                existingRecord.BossTotal = bossTotal;
                existingRecord.MonthlyCompleted = monthlyCompleted;
                existingRecord.MonthlyTotal = monthlyTotal;
                existingRecord.BossRewardEarned = bossRewardEarned;
            }
            else
            {
                data.DailyRecords.Add(new DailyRecord
                {
                    Date = today,
                    CharacterId = characterId,
                    CharacterName = characterName,
                    DailyCompleted = dailyCompleted,
                    DailyTotal = dailyTotal,
                    WeeklyCompleted = weeklyCompleted,
                    WeeklyTotal = weeklyTotal,
                    BossCompleted = bossCompleted,
                    BossTotal = bossTotal,
                    MonthlyCompleted = monthlyCompleted,
                    MonthlyTotal = monthlyTotal,
                    BossRewardEarned = bossRewardEarned
                });
            }

            data.DailyRecords = data.DailyRecords
                .Where(r => r.Date >= DateTime.Today.AddDays(-90))
                .ToList();

            Save(data);
        }

        public static void RecordCharacterGrowth(string characterId, string characterName, 
            int level, long experience = 0, double experienceRate = 0,
            long combatPower = 0, int unionLevel = 0, long unionPower = 0)
        {
            RecordCharacterGrowthForDate(DateTime.Today, characterId, characterName, 
                level, experience, experienceRate, combatPower, unionLevel, unionPower);
        }

        public static void RecordCharacterGrowthForDate(DateTime date, string characterId, string characterName, 
            int level, long experience = 0, double experienceRate = 0,
            long combatPower = 0, int unionLevel = 0, long unionPower = 0)
        {
            var data = Load();

            // 해당 캐릭터의 역대 최고 전투력 조회
            var historicalMaxCombatPower = data.GrowthRecords
                .Where(r => r.CharacterId == characterId)
                .Select(r => Math.Max(r.CombatPower, r.HighestCombatPower))
                .DefaultIfEmpty(0)
                .Max();

            // 누적 경험치 계산 (ExpTable 기반)
            var totalExp = ExpTable.CalculateTotalExp(level, experienceRate);

            var existingRecord = data.GrowthRecords
                .FirstOrDefault(r => r.Date.Date == date.Date && r.CharacterId == characterId);

            if (existingRecord != null)
            {
                existingRecord.Level = level;
                existingRecord.Experience = experience;
                existingRecord.ExperienceRate = experienceRate;
                existingRecord.TotalExp = totalExp;
                
                // 전투력: 역대 최고 기록을 갱신할 때만 기록
                if (combatPower > historicalMaxCombatPower)
                {
                    existingRecord.CombatPower = combatPower;
                    existingRecord.HighestCombatPower = combatPower;
                }
                else
                {
                    // 최고 기록 갱신이 아니면 현재 값 유지 (기존 기록보다 높으면 업데이트)
                    if (combatPower > existingRecord.CombatPower)
                        existingRecord.CombatPower = combatPower;
                    existingRecord.HighestCombatPower = Math.Max(existingRecord.HighestCombatPower, historicalMaxCombatPower);
                }
                
                existingRecord.UnionLevel = unionLevel;
                existingRecord.UnionPower = unionPower;
            }
            else
            {
                // 새 기록: 역대 최고 기록 갱신 여부 확인
                var isNewHighest = combatPower > historicalMaxCombatPower;
                
                data.GrowthRecords.Add(new CharacterGrowthRecord
                {
                    Date = date.Date,
                    CharacterId = characterId,
                    CharacterName = characterName,
                    Level = level,
                    Experience = experience,
                    ExperienceRate = experienceRate,
                    TotalExp = totalExp,
                    CombatPower = isNewHighest ? combatPower : 0, // 최고 기록 갱신 시에만 기록
                    HighestCombatPower = Math.Max(combatPower, historicalMaxCombatPower),
                    UnionLevel = unionLevel,
                    UnionPower = unionPower
                });
            }

            data.GrowthRecords = data.GrowthRecords
                .Where(r => r.Date >= DateTime.Today.AddDays(-365))
                .ToList();

            Save(data);
        }

        public static void RecordHexaSkillChange(string characterId, string characterName,
            string skillName, int oldLevel, int newLevel, string skillIcon = "", DateTime? date = null)
        {
            if (oldLevel == newLevel) return;

            var recordDate = date ?? DateTime.Now;
            var data = Load();
            
            if (data.HexaSkillRecords.Any(r => r.Date.Date == recordDate.Date && 
                                               r.CharacterId == characterId && 
                                               r.SkillName == skillName && 
                                               r.NewLevel == newLevel))
            {
                return;
            }

            data.HexaSkillRecords.Add(new HexaSkillRecord
            {
                Date = recordDate,
                CharacterId = characterId,
                CharacterName = characterName,
                SkillName = skillName,
                SkillIcon = skillIcon,
                OldLevel = oldLevel,
                NewLevel = newLevel
            });

            data.HexaSkillRecords = data.HexaSkillRecords
                .Where(r => r.Date >= DateTime.Today.AddDays(-180))
                .ToList();

            Save(data);
        }

        public static void RecordItemChange(string characterId, string characterName,
            string itemSlot, string oldItemName, string newItemName, string changeType, string itemInfoJson = "", DateTime? date = null)
        {
            if (oldItemName == newItemName) return;

            var recordDate = date ?? DateTime.Now;
            var data = Load();

            if (data.ItemChangeRecords.Any(r => r.Date.Date == recordDate.Date && 
                                                r.CharacterId == characterId && 
                                                r.ItemSlot == itemSlot && 
                                                r.NewItemName == newItemName))
            {
                return;
            }

            data.ItemChangeRecords.Add(new ItemChangeRecord
            {
                Date = recordDate,
                CharacterId = characterId,
                CharacterName = characterName,
                ItemSlot = itemSlot,
                OldItemName = oldItemName,
                NewItemName = newItemName,
                ChangeType = changeType,
                ItemInfoJson = itemInfoJson
            });

            data.ItemChangeRecords = data.ItemChangeRecords
                .Where(r => r.Date >= DateTime.Today.AddDays(-90))
                .ToList();

            Save(data);
        }

        public static void RecordWeeklyBossReward(string characterId, string characterName,
            long totalReward, Dictionary<string, long> bossRewards)
        {
            var data = Load();
            
            var today = DateTime.Today;
            int daysUntilThursday = ((int)DayOfWeek.Thursday - (int)today.DayOfWeek + 7) % 7;
            if (daysUntilThursday == 0 && today.TimeOfDay < TimeSpan.Zero)
                daysUntilThursday = 7;
            var weekStart = today.AddDays(-((7 - daysUntilThursday) % 7));

            var existingRecord = data.BossRewardRecords
                .FirstOrDefault(r => r.WeekStartDate.Date == weekStart.Date && r.CharacterId == characterId);

            if (existingRecord != null)
            {
                existingRecord.TotalReward = totalReward;
                existingRecord.BossRewards = bossRewards;
            }
            else
            {
                data.BossRewardRecords.Add(new WeeklyBossRewardRecord
                {
                    WeekStartDate = weekStart,
                    CharacterId = characterId,
                    CharacterName = characterName,
                    TotalReward = totalReward,
                    BossRewards = bossRewards
                });
            }

            data.BossRewardRecords = data.BossRewardRecords
                .Where(r => r.WeekStartDate >= DateTime.Today.AddDays(-365))
                .ToList();

            Save(data);
        }

        public static List<DailyRecord> GetDailyRecords(string? characterId, DateTime startDate, DateTime endDate)
        {
            var data = Load();
            var query = data.DailyRecords.Where(r => r.Date >= startDate && r.Date <= endDate);
            
            if (!string.IsNullOrEmpty(characterId))
                query = query.Where(r => r.CharacterId == characterId);

            return query.OrderBy(r => r.Date).ToList();
        }

        public static DateTime? CalculateEstimatedLevelUpDate(string characterId)
        {
            var data = Load();
            var records = data.GrowthRecords
                .Where(r => r.CharacterId == characterId)
                .OrderByDescending(r => r.Date)
                .Take(7)
                .ToList();

            if (records.Count < 2) return null;

            var latestRecord = records.First();
            var oldestRecord = records.Last();

            // 실제 경험치 계산 (ExpTable 사용)
            long latestTotalExp = latestRecord.TotalExp > 0 
                ? latestRecord.TotalExp 
                : ExpTable.CalculateTotalExp(latestRecord.Level, latestRecord.ExperienceRate);
            long oldestTotalExp = oldestRecord.TotalExp > 0 
                ? oldestRecord.TotalExp 
                : ExpTable.CalculateTotalExp(oldestRecord.Level, oldestRecord.ExperienceRate);

            long expGain = latestTotalExp - oldestTotalExp;
            
            int daysDiff = (latestRecord.Date - oldestRecord.Date).Days;
            if (daysDiff <= 0) return null;

            double dailyExpGain = (double)expGain / daysDiff;
            if (dailyExpGain <= 0) return null;

            // 현재 레벨에서 남은 경험치 계산
            if (!ExpTable.RequiredExp.TryGetValue(latestRecord.Level, out var requiredExp))
                return null;

            long remainingExp = (long)(requiredExp * (100 - latestRecord.ExperienceRate) / 100);
            int daysToLevelUp = (int)Math.Ceiling(remainingExp / dailyExpGain);

            return DateTime.Today.AddDays(daysToLevelUp);
        }

        /// <summary>
        /// 주간 평균 경험치 획득량 계산
        /// </summary>
        public static long GetWeeklyAverageExpGain(string characterId)
        {
            var data = Load();
            var records = data.GrowthRecords
                .Where(r => r.CharacterId == characterId && r.Date >= DateTime.Today.AddDays(-7))
                .OrderBy(r => r.Date)
                .ToList();

            if (records.Count < 2) return 0;

            var first = records.First();
            var last = records.Last();

            long firstTotalExp = first.TotalExp > 0 
                ? first.TotalExp 
                : ExpTable.CalculateTotalExp(first.Level, first.ExperienceRate);
            long lastTotalExp = last.TotalExp > 0 
                ? last.TotalExp 
                : ExpTable.CalculateTotalExp(last.Level, last.ExperienceRate);

            return lastTotalExp - firstTotalExp;
        }

        /// <summary>
        /// 레벨업 예상 날짜 (한국어 포맷)
        /// </summary>
        public static string GetEstimatedLevelUpDateFormatted(string characterId)
        {
            var estimatedDate = CalculateEstimatedLevelUpDate(characterId);
            if (!estimatedDate.HasValue) return "계산 불가";

            var daysRemaining = (estimatedDate.Value - DateTime.Today).Days;
            if (daysRemaining <= 0) return "오늘";
            if (daysRemaining == 1) return "내일";
            if (daysRemaining < 7) return $"{daysRemaining}일 후";
            if (daysRemaining < 30) return $"약 {daysRemaining / 7}주 후";
            if (daysRemaining < 365) return $"약 {daysRemaining / 30}개월 후";
            return $"약 {daysRemaining / 365}년 후";
        }

        public static List<ExpGrowthInfo> GetExperienceGrowth(string characterId, int days = 7)
        {
            var data = Load();
            var records = data.GrowthRecords
                .Where(r => r.CharacterId == characterId && r.Date >= DateTime.Today.AddDays(-days))
                .OrderBy(r => r.Date)
                .ToList();

            var result = new List<ExpGrowthInfo>();
            for (int i = 1; i < records.Count; i++)
            {
                var prev = records[i - 1];
                var curr = records[i];

                // % 단위 성장률
                double growth = curr.ExperienceRate - prev.ExperienceRate;
                if (curr.Level > prev.Level)
                {
                    growth += 100 * (curr.Level - prev.Level);
                }

                // 실제 경험치 계산 (ExpTable 사용)
                long prevTotalExp = curr.TotalExp > 0 ? prev.TotalExp : ExpTable.CalculateTotalExp(prev.Level, prev.ExperienceRate);
                long currTotalExp = curr.TotalExp > 0 ? curr.TotalExp : ExpTable.CalculateTotalExp(curr.Level, curr.ExperienceRate);
                long dailyExpGain = currTotalExp - prevTotalExp;

                // 현재 레벨의 필요 경험치
                ExpTable.RequiredExp.TryGetValue(curr.Level, out long requiredExp);

                result.Add(new ExpGrowthInfo
                {
                    Date = curr.Date,
                    Level = curr.Level,
                    ExperienceRate = curr.ExperienceRate,
                    DailyGrowth = growth,
                    TotalExp = currTotalExp,
                    DailyExpGain = dailyExpGain,
                    RequiredExp = requiredExp,
                    LeveledUp = curr.Level > prev.Level,
                    PreviousLevel = prev.Level
                });
            }

            // GraphRate: 실제 경험치 % (0~1) 기반으로 바로 반영
                    foreach (var item in result)
                    {
                // 최소 높이 보정(0.05) + 상한 1.0 유지
                item.GraphRate = Math.Max(0.05, Math.Min(1.0, item.ExperienceRate / 100.0));
            }

            return result;
        }

        public static List<UnionGrowthInfo> GetUnionGrowth(string characterId, int days = 30)
        {
            var data = Load();
            var records = data.GrowthRecords
                .Where(r => r.CharacterId == characterId && r.Date >= DateTime.Today.AddDays(-days) && r.UnionLevel > 0)
                .OrderBy(r => r.Date)
                .ToList();

            var result = new List<UnionGrowthInfo>();
            for (int i = 1; i < records.Count; i++)
            {
                var prev = records[i - 1];
                var curr = records[i];

                if (curr.UnionLevel != prev.UnionLevel)
                {
                    result.Add(new UnionGrowthInfo
                    {
                        Date = curr.Date,
                        OldLevel = prev.UnionLevel,
                        NewLevel = curr.UnionLevel,
                        LevelChange = curr.UnionLevel - prev.UnionLevel
                    });
                }
            }

            // GraphRate: 실제 유니온 레벨 절대값 기준 정규화
            if (result.Any())
            {
                int minLevel = result.Min(x => x.NewLevel);
                int maxLevel = result.Max(x => x.NewLevel);
                int span = Math.Max(1, maxLevel - minLevel);

                    foreach (var item in result)
                    {
                    item.GraphRate = Math.Max(0.05, (double)(item.NewLevel - minLevel) / span);
                }
            }

            return result;
        }

        public static List<CombatPowerGrowthInfo> GetCombatPowerGrowth(string characterId, int days = 30)
        {
            var data = Load();
            var records = data.GrowthRecords
                .Where(r => r.CharacterId == characterId && r.Date >= DateTime.Today.AddDays(-days) && r.CombatPower > 0)
                .OrderBy(r => r.Date)
                .ToList();

            var result = new List<CombatPowerGrowthInfo>();
            for (int i = 1; i < records.Count; i++)
            {
                var prev = records[i - 1];
                var curr = records[i];

                if (curr.CombatPower != prev.CombatPower)
                {
                    result.Add(new CombatPowerGrowthInfo
                    {
                        Date = curr.Date,
                        OldPower = prev.CombatPower,
                        NewPower = curr.CombatPower,
                        Change = curr.CombatPower - prev.CombatPower
                    });
                }
            }

            // GraphRate: 전투력 절대값 기준 정규화
            if (result.Any())
            {
                long minPower = result.Min(x => x.NewPower);
                long maxPower = result.Max(x => x.NewPower);
                long span = Math.Max(1, maxPower - minPower);

                    foreach (var item in result)
                    {
                    item.GraphRate = Math.Max(0.05, (double)(item.NewPower - minPower) / span);
                }
            }

            return result;
        }

        public static List<HexaSkillRecord> GetHexaSkillHistory(string? characterId = null, int limit = 20)
        {
            var data = Load();
            var query = data.HexaSkillRecords.AsEnumerable();

            if (!string.IsNullOrEmpty(characterId))
                query = query.Where(r => r.CharacterId == characterId);

            return query.OrderByDescending(r => r.Date).Take(limit).ToList();
        }

        public static List<ItemChangeRecord> GetItemChangeHistory(string? characterId = null, int limit = 20)
        {
            var data = Load();
            var query = data.ItemChangeRecords.AsEnumerable();

            if (!string.IsNullOrEmpty(characterId))
                query = query.Where(r => r.CharacterId == characterId);

            return query.OrderByDescending(r => r.Date).Take(limit).ToList();
        }

        public static WeeklyReportData GetWeeklyReport(DateTime weekStartDate)
        {
            var data = Load();
            var weekEndDate = weekStartDate.AddDays(7);

            var records = data.DailyRecords
                .Where(r => r.Date >= weekStartDate && r.Date < weekEndDate)
                .ToList();

            var bossRecords = data.BossRewardRecords
                .Where(r => r.WeekStartDate.Date == weekStartDate.Date)
                .ToList();

            return new WeeklyReportData
            {
                WeekStartDate = weekStartDate,
                WeekEndDate = weekEndDate,
                DailyRecords = records,
                TotalBossReward = bossRecords.Sum(r => r.TotalReward),
                AverageDailyCompletionRate = records.Any() ? records.Average(r => r.DailyCompletionRate) : 0,
                AverageWeeklyCompletionRate = records.Any() ? records.Average(r => r.WeeklyCompletionRate) : 0,
                AverageBossCompletionRate = records.Any() ? records.Average(r => r.BossCompletionRate) : 0
            };
        }

        public static MonthlyReportData GetMonthlyReport(int year, int month)
        {
            var data = Load();
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var records = data.DailyRecords
                .Where(r => r.Date >= monthStart && r.Date < monthEnd)
                .ToList();

            var bossRecords = data.BossRewardRecords
                .Where(r => r.WeekStartDate >= monthStart && r.WeekStartDate < monthEnd)
                .ToList();

            var completionByDayOfWeek = records
                .GroupBy(r => r.Date.DayOfWeek)
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(r => r.OverallCompletionRate)
                );

            return new MonthlyReportData
            {
                Year = year,
                Month = month,
                DailyRecords = records,
                TotalBossReward = bossRecords.Sum(r => r.TotalReward),
                AverageCompletionRate = records.Any() ? records.Average(r => r.OverallCompletionRate) : 0,
                CompletionByDayOfWeek = completionByDayOfWeek,
                MostProductiveDay = completionByDayOfWeek.Any() 
                    ? completionByDayOfWeek.OrderByDescending(x => x.Value).First().Key 
                    : DayOfWeek.Monday,
                TotalActiveDays = records.Select(r => r.Date.Date).Distinct().Count()
            };
        }

        public static List<CharacterGrowthRecord> GetGrowthHistory(string characterId, int days = 30)
        {
            var data = Load();
            var cutoffDate = DateTime.Today.AddDays(-days);

            return data.GrowthRecords
                .Where(r => r.CharacterId == characterId && r.Date >= cutoffDate)
                .OrderBy(r => r.Date)
                .ToList();
        }

        /// <summary>
        /// 성장 리포트 관련 캐시(경험치/유니온/전투력/헥사/장비 기록) 초기화
        /// </summary>
        public static void ClearGrowthData()
        {
            var data = Load();
            data.GrowthRecords.Clear();
            data.HexaSkillRecords.Clear();
            data.ItemChangeRecords.Clear();
            Save(data);
        }
    }

    public class ExpGrowthInfo
    {
        public DateTime Date { get; set; }
        public int Level { get; set; }
        public double ExperienceRate { get; set; }
        public double DailyGrowth { get; set; } // % 단위
        public double GraphRate { get; set; }
        
        // 실제 경험치 값 (ExpTable 기반)
        public long TotalExp { get; set; } // 누적 경험치
        public long DailyExpGain { get; set; } // 일일 경험치 획득량
        public long RequiredExp { get; set; } // 현재 레벨업 필요 경험치
        
        // 한국어 포맷팅된 문자열
        public string DailyExpGainFormatted => ExpTable.FormatExpKorean(DailyExpGain);
        public string TotalExpFormatted => ExpTable.FormatExpKorean(TotalExp);
        public string RequiredExpFormatted => ExpTable.FormatExpKorean(RequiredExp);
        
        // 레벨업 여부
        public bool LeveledUp { get; set; }
        public int PreviousLevel { get; set; }
    }

    public class UnionGrowthInfo
    {
        public DateTime Date { get; set; }
        public int OldLevel { get; set; }
        public int NewLevel { get; set; }
        public int LevelChange { get; set; }
        public double GraphRate { get; set; }
    }

    public class CombatPowerGrowthInfo
    {
        public DateTime Date { get; set; }
        public long OldPower { get; set; }
        public long NewPower { get; set; }
        public long Change { get; set; }
        public double GraphRate { get; set; }
    }

    public class WeeklyReportData
    {
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public List<DailyRecord> DailyRecords { get; set; } = new();
        public long TotalBossReward { get; set; }
        public double AverageDailyCompletionRate { get; set; }
        public double AverageWeeklyCompletionRate { get; set; }
        public double AverageBossCompletionRate { get; set; }
    }

    public class MonthlyReportData
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public List<DailyRecord> DailyRecords { get; set; } = new();
        public long TotalBossReward { get; set; }
        public double AverageCompletionRate { get; set; }
        public Dictionary<DayOfWeek, double> CompletionByDayOfWeek { get; set; } = new();
        public DayOfWeek MostProductiveDay { get; set; }
        public int TotalActiveDays { get; set; }
    }
}
