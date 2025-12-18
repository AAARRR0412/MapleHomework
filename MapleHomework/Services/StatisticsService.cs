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
        public string ChangeSummary { get; set; } = ""; // 옵션 차이 요약 (예: 스타포스 15성 -> 20성)
        public string OptionChangesJson { get; set; } = ""; // 상세 옵션 변경 내역 JSON
        public string ItemIcon { get; set; } = ""; // 아이템 아이콘 URL

        // UI 바인딩용: JSON에서 파싱된 옵션 변경 목록
        public List<ItemOptionChange> OptionChanges
        {
            get
            {
                if (string.IsNullOrEmpty(OptionChangesJson))
                    return new List<ItemOptionChange>();
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<ItemOptionChange>>(OptionChangesJson)
                           ?? new List<ItemOptionChange>();
                }
                catch
                {
                    return new List<ItemOptionChange>();
                }
            }
        }

        // 신규 아이템 여부
        public bool IsNewItem => ChangeType == "장착" || OptionChanges.Any(c => c.IsNewItem);

        // UI 렌더링용 (저장되지 않아도 됨, 런타임에 채움)
        [System.Text.Json.Serialization.JsonIgnore]
        public string JobClass { get; set; } = "";
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

        // OLD Path: Documents/MapleScheduler
        private static readonly string RootDataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MapleScheduler");

        // NEW Path: Documents/MapleScheduler/statistics
        private static readonly string DataFolder = System.IO.Path.Combine(RootDataFolder, "statistics");

        private static readonly string LegacyFilePath = System.IO.Path.Combine(RootDataFolder, "statistics_data.json");

        private static string GetCharacterFolder(string characterName)
        {
            // 윈도우 파일명으로 사용할 수 없는 문자 제거
            var safeName = string.Join("_", characterName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(DataFolder, safeName);
        }

        private static string GetFilePath(string characterName)
        {
            var folder = GetCharacterFolder(characterName);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return Path.Combine(folder, "statistics.json");
        }

        static StatisticsService()
        {
            if (!System.IO.Directory.Exists(RootDataFolder))
                System.IO.Directory.CreateDirectory(RootDataFolder);

            if (!System.IO.Directory.Exists(DataFolder))
                System.IO.Directory.CreateDirectory(DataFolder);

            // 폴더 구조 마이그레이션 (Root -> statistics)
            try
            {
                var directories = Directory.GetDirectories(RootDataFolder);
                foreach (var dir in directories)
                {
                    var dirName = Path.GetFileName(dir);
                    if (dirName.Equals("statistics", StringComparison.OrdinalIgnoreCase)) continue;
                    if (dirName.Equals("config", StringComparison.OrdinalIgnoreCase)) continue; // 설정 폴더 제외 (혹시 있다면)

                    // 캐릭터 폴더로 추정되면 이동
                    var destDir = Path.Combine(DataFolder, dirName);
                    if (!Directory.Exists(destDir))
                    {
                        Directory.Move(dir, destDir);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Folder Migration Error: {ex}");
            }
        }
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// 마이그레이션: 기존 statistics_data.json이 있다면 캐릭터별로 분리
        /// </summary>
        public static void MigrateLegacyData()
        {
            try
            {
                if (!File.Exists(LegacyFilePath)) return;

                string json = File.ReadAllText(LegacyFilePath);
                var data = JsonSerializer.Deserialize<StatisticsData>(json, JsonOptions);
                if (data == null) return;

                // 2. 캐릭터별로 데이터 분리 및 저장
                SaveAll(data);

                // 3. 기존 파일 백업 (.bak)
                string backupPath = LegacyFilePath + ".bak";
                if (File.Exists(backupPath)) File.Delete(backupPath);
                File.Move(LegacyFilePath, backupPath);
            }
            catch (Exception ex)
            {
                // 로깅 필요 시 추가
                System.Diagnostics.Debug.WriteLine($"Migration failed: {ex.Message}");
            }
        }

        public static DateTime GetLastUpdated(string characterName)
        {
            try
            {
                MigrateLegacyData();
                var file = GetFilePath(characterName);
                if (!File.Exists(file)) return DateTime.MinValue;
                // 파일 수정 시간 or 파일 내부 시간? 파일 내부가 정확함
                var data = Load(characterName);
                return data?.LastUpdated ?? DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        // 전역 마지막 업데이트 (가장 최근 수정된 파일 기준)
        public static DateTime GetLastUpdated()
        {
            MigrateLegacyData();
            if (!Directory.Exists(DataFolder)) return DateTime.MinValue;

            var statsFiles = Directory.GetFiles(DataFolder, "statistics.json", SearchOption.AllDirectories);
            if (statsFiles.Length == 0) return DateTime.MinValue;

            DateTime maxDate = DateTime.MinValue;
            foreach (var file in statsFiles)
            {
                if (File.GetLastWriteTime(file) > maxDate)
                {
                    maxDate = File.GetLastWriteTime(file);
                }
            }
            return maxDate;
        }

        /// <summary>
        /// 특정 캐릭터의 데이터 로드 (파일 분리 적용)
        /// </summary>
        public static StatisticsData Load(string characterName)
        {
            MigrateLegacyData();
            try
            {
                string file = GetFilePath(characterName);
                if (!File.Exists(file)) return new StatisticsData();

                string json = File.ReadAllText(file);
                var data = JsonSerializer.Deserialize<StatisticsData>(json, JsonOptions);
                return data ?? new StatisticsData();
            }
            catch
            {
                return new StatisticsData();
            }
        }

        /// <summary>
        /// 백업용: 모든 캐릭터 데이터 통합 로드
        /// </summary>
        public static StatisticsData LoadAll()
        {
            MigrateLegacyData();
            var result = new StatisticsData();

            if (!Directory.Exists(DataFolder)) return result;

            var userFolders = Directory.GetDirectories(DataFolder);
            foreach (var folder in userFolders)
            {
                var file = Path.Combine(folder, "statistics.json");
                if (File.Exists(file))
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var data = JsonSerializer.Deserialize<StatisticsData>(json, JsonOptions);
                        if (data != null)
                        {
                            result.DailyRecords.AddRange(data.DailyRecords);
                            result.GrowthRecords.AddRange(data.GrowthRecords);
                            result.BossRewardRecords.AddRange(data.BossRewardRecords);
                            result.HexaSkillRecords.AddRange(data.HexaSkillRecords);
                            result.ItemChangeRecords.AddRange(data.ItemChangeRecords);
                            if (data.LastUpdated > result.LastUpdated)
                                result.LastUpdated = data.LastUpdated;
                        }
                    }
                    catch { }
                }
            }
            return result;
        }

        /// <summary>
        /// 특정 캐릭터 데이터 저장
        /// </summary>
        public static void Save(string characterName, StatisticsData data)
        {
            try
            {
                data.LastUpdated = DateTime.Now;
                string file = GetFilePath(characterName);
                string json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(file, json);
            }
            catch { }
        }

        /// <summary>
        /// 백업 복원 및 마이그레이션용: 통합 데이터를 캐릭터별로 분리 저장
        /// </summary>
        public static void SaveAll(StatisticsData data)
        {
            // 모든 기록을 CharacterName 기준으로 그룹화
            // CharacterName이 없는 경우(ID만 있는 경우)는... 별도 처리 필요하지만
            // 현재 구조상 Name이 무조건 있어야 함. 없다면 "Unknown" 폴더?

            var names = new HashSet<string>();
            names.UnionWith(data.DailyRecords.Select(r => r.CharacterName));
            names.UnionWith(data.GrowthRecords.Select(r => r.CharacterName));
            names.UnionWith(data.BossRewardRecords.Select(r => r.CharacterName));
            names.UnionWith(data.HexaSkillRecords.Select(r => r.CharacterName));
            names.UnionWith(data.ItemChangeRecords.Select(r => r.CharacterName));
            names.Remove(""); // 빈 이름 제거

            foreach (var name in names)
            {
                var subset = new StatisticsData
                {
                    LastUpdated = data.LastUpdated,
                    DailyRecords = data.DailyRecords.Where(r => r.CharacterName == name).ToList(),
                    GrowthRecords = data.GrowthRecords.Where(r => r.CharacterName == name).ToList(),
                    BossRewardRecords = data.BossRewardRecords.Where(r => r.CharacterName == name).ToList(),
                    HexaSkillRecords = data.HexaSkillRecords.Where(r => r.CharacterName == name).ToList(),
                    ItemChangeRecords = data.ItemChangeRecords.Where(r => r.CharacterName == name).ToList(),
                };
                Save(name, subset);
            }
        }

        #region Helper Methods (Updated signatures)

        /// <summary>
        /// 특정 캐릭터의 데이터가 존재하는지 확인
        /// </summary>
        /// <summary>
        /// 특정 캐릭터의 데이터가 존재하는지 확인
        /// </summary>
        public static bool HasDataForCharacter(string characterId, string characterName)
        {
            var data = Load(characterName);
            return data.GrowthRecords.Any(r => r.CharacterId == characterId);
        }

        /// <summary>
        /// 특정 캐릭터의 수집된 데이터 일수 반환 (캐릭터 이름 필요하면 오버로딩)
        /// </summary>
        /// <summary>
        /// 특정 캐릭터의 수집된 데이터 일수 반환
        /// </summary>
        public static int GetCollectedDaysCount(string characterId, string characterName)
        {
            var data = Load(characterName);
            return data.GrowthRecords
                .Where(r => r.CharacterId == characterId)
                .Select(r => r.Date.Date)
                .Distinct()
                .Count();
        }

        /// <summary>
        /// 특정 캐릭터의 수집된 날짜 목록 반환
        /// </summary>
        /// <summary>
        /// 특정 캐릭터의 수집된 날짜 목록 반환
        /// </summary>
        public static HashSet<DateTime> GetCollectedDatesForCharacter(string characterId, string characterName)
        {
            var data = Load(characterName);
            return data.GrowthRecords
                .Where(r => r.CharacterId == characterId)
                .Select(r => r.Date.Date)
                .ToHashSet();
        }

        /// <summary>
        /// 특정 캐릭터의 누락 날짜 목록 반환 (최근 N일 기준)
        /// </summary>
        /// <summary>
        /// 특정 캐릭터의 누락 날짜 목록 반환 (최근 N일 기준)
        /// </summary>
        public static List<DateTime> GetMissingDatesForCharacter(string characterId, string characterName, DateTime startDate, DateTime endDate)
        {
            var collectedDates = GetCollectedDatesForCharacter(characterId, characterName);
            var missingDates = new List<DateTime>();

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                if (date > DateTime.Today.AddDays(-1)) continue; // 오늘과 미래는 제외
                if (!collectedDates.Contains(date))
                {
                    missingDates.Add(date);
                }
            }

            return missingDates.OrderBy(d => d).ToList();
        }

        /// <summary>
        /// 특정 날짜에 완전한 데이터가 있는지 확인 (경험치 데이터 기준)
        /// </summary>
        public static bool HasCompleteDataForDate(string characterId, string characterName, DateTime date)
        {
            try
            {
                var data = Load(characterName);
                // 해당 날짜에 GrowthRecord가 있는지 확인
                var hasGrowthRecord = data.GrowthRecords
                    .Any(r => r.CharacterId == characterId && r.Date.Date == date.Date);

                return hasGrowthRecord;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 특정 날짜의 불완전한 데이터 제거
        /// </summary>
        public static void RemoveIncompleteDataForDate(string characterId, string characterName, DateTime date)
        {
            try
            {
                var data = Load(characterName);

                // 해당 날짜의 모든 관련 레코드 제거
                data.GrowthRecords.RemoveAll(r => r.CharacterId == characterId && r.Date.Date == date.Date);
                data.HexaSkillRecords.RemoveAll(r => r.CharacterId == characterId && r.Date.Date == date.Date);
                data.ItemChangeRecords.RemoveAll(r => r.CharacterId == characterId && r.Date.Date == date.Date);

                Save(characterName, data);
            }
            catch { }
        }

        /// <summary>
        /// 특정 캐릭터의 분석 데이터(장비 변경, 스킬 변경)를 초기화합니다.
        /// 새로고침(재분석) 시 사용됩니다.
        /// </summary>
        public static void ClearAnalysisData(string characterId, string characterName)
        {
            try
            {
                var data = Load(characterName);

                // 해당 캐릭터의 장비 변경 내역 삭제
                int removedItems = data.ItemChangeRecords.RemoveAll(r => r.CharacterId == characterId);

                // 해당 캐릭터의 6차 스킬 변경 내역 삭제
                int removedSkills = data.HexaSkillRecords.RemoveAll(r => r.CharacterId == characterId);

                // 성장 기록(GrowthRecords)은 유지합니다 (원본 API 데이터가 없을 수도 있으므로)

                Save(characterName, data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClearAnalysisData error: {ex.Message}");
            }
        }

        /// <summary>
        /// 특정 캐릭터의 모든 데이터를 삭제합니다. (캐시 삭제)
        /// </summary>
        public static void ClearCharacterData(string characterId, string characterName)
        {
            try
            {
                // 캐릭터별 파일 삭제가 가장 깔끔함
                var filePath = GetFilePath(characterName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // 폴더도 비어있으면 삭제
                var folder = GetCharacterFolder(characterName);
                if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
                {
                    Directory.Delete(folder);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClearCharacterData error: {ex.Message}");
            }
        }



        public static void RecordDailyCompletion(string characterId, string characterName,
            int dailyCompleted, int dailyTotal,
            int weeklyCompleted, int weeklyTotal,
            int bossCompleted, int bossTotal,
            int monthlyCompleted, int monthlyTotal,
            long bossRewardEarned)
        {
            var data = Load(characterName);
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

            Save(characterName, data);
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
            var data = Load(characterName);

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

                // 전투력: 항상 최신 값으로 업데이트 (그래프 표시를 위해)
                if (combatPower > 0)
                {
                    existingRecord.CombatPower = combatPower;
                    existingRecord.HighestCombatPower = Math.Max(combatPower, Math.Max(existingRecord.HighestCombatPower, historicalMaxCombatPower));
                }

                existingRecord.UnionLevel = unionLevel;
                existingRecord.UnionPower = unionPower;
            }
            else
            {
                // 새 기록: 전투력은 항상 저장 (그래프 표시를 위해)
                data.GrowthRecords.Add(new CharacterGrowthRecord
                {
                    Date = date.Date,
                    CharacterId = characterId,
                    CharacterName = characterName,
                    Level = level,
                    Experience = experience,
                    ExperienceRate = experienceRate,
                    TotalExp = totalExp,
                    CombatPower = combatPower, // 전투력 항상 저장
                    HighestCombatPower = Math.Max(combatPower, historicalMaxCombatPower),
                    UnionLevel = unionLevel,
                    UnionPower = unionPower
                });
            }

            data.GrowthRecords = data.GrowthRecords
                .Where(r => r.Date >= DateTime.Today.AddDays(-365))
                .ToList();

            Save(characterName, data);
        }

        public static void RecordHexaSkillChange(string characterId, string characterName,
            string skillName, int oldLevel, int newLevel, string skillIcon = "", DateTime? date = null)
        {
            if (oldLevel == newLevel) return;

            var recordDate = date ?? DateTime.Now;
            var data = Load(characterName);

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

            Save(characterName, data);
        }

        public static void RecordItemChange(string characterId, string characterName,
            string itemSlot, string oldItemName, string newItemName, string changeType,
            string itemInfoJson = "", DateTime? date = null, string changeSummary = "",
            string optionChangesJson = "", string itemIcon = "")
        {
            // 이름이 같더라도 옵션 변경은 기록해야 한다.
            if (oldItemName == newItemName && changeType != "옵션 변경") return;

            var recordDate = date ?? DateTime.Now;
            var data = Load(characterName);

            // 중복 체크 완화: 같은 날짜에 같은 슬롯에서 같은 아이템이 같은 변경 타입이어도
            // 옵션 변경 내용이 다르면 기록 (옵션 변경 내용으로 구분)
            if (data.ItemChangeRecords.Any(r => r.Date.Date == recordDate.Date &&
                                                r.CharacterId == characterId &&
                                                r.ItemSlot == itemSlot &&
                                                r.NewItemName == newItemName &&
                                                r.ChangeType == changeType &&
                                                r.OptionChangesJson == optionChangesJson))
            {
                // 완전히 동일한 변경 내역이면 스킵
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
                ItemInfoJson = itemInfoJson,
                ChangeSummary = changeSummary,
                OptionChangesJson = optionChangesJson,
                ItemIcon = itemIcon
            });

            data.ItemChangeRecords = data.ItemChangeRecords
                .Where(r => r.Date >= DateTime.Today.AddDays(-180))
                .ToList();

            Save(characterName, data);
        }

        public static void RecordWeeklyBossReward(string characterId, string characterName,
            long totalReward, Dictionary<string, long> bossRewards)
        {
            var data = Load(characterName);

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

            Save(characterName, data);
        }

        public static List<DailyRecord> GetDailyRecords(string? characterId, string characterName, DateTime startDate, DateTime endDate)
        {
            var data = Load(characterName);
            var query = data.DailyRecords.Where(r => r.Date >= startDate && r.Date <= endDate);

            if (!string.IsNullOrEmpty(characterId))
                query = query.Where(r => r.CharacterId == characterId);

            return query.OrderBy(r => r.Date).ToList();
        }

        public static DateTime? CalculateEstimatedLevelUpDate(string characterId, string characterName)
        {
            var data = Load(characterName);
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
        /// 최근 30일 기준 일간 평균 경험치 상승 (경험치, %)
        /// </summary>
        public static (long ExpPerDay, double PercentPerDay) GetMonthlyDailyAverageExpGain(string characterId, string characterName, int days = 30)
        {
            var data = Load(characterName);
            var records = data.GrowthRecords
                .Where(r => r.CharacterId == characterId && r.Date >= DateTime.Today.AddDays(-days))
                .OrderBy(r => r.Date)
                .ToList();

            if (records.Count < 2) return (0, 0);

            var first = records.First();
            var last = records.Last();

            long firstTotalExp = first.TotalExp > 0
                ? first.TotalExp
                : ExpTable.CalculateTotalExp(first.Level, first.ExperienceRate);
            long lastTotalExp = last.TotalExp > 0
                ? last.TotalExp
                : ExpTable.CalculateTotalExp(last.Level, last.ExperienceRate);

            int daysDiff = (last.Date - first.Date).Days;
            if (daysDiff <= 0) return (0, 0);

            long expGain = lastTotalExp - firstTotalExp;
            long expPerDay = expGain > 0 ? expGain / daysDiff : 0;

            // 퍼센트 상승: 레벨 증가를 100% 단위로 환산하여 차이 계산
            double percentGain = (last.Level - first.Level) * 100.0 + (last.ExperienceRate - first.ExperienceRate);
            double percentPerDay = percentGain / daysDiff;

            return (expPerDay, percentPerDay);
        }

        /// <summary>
        /// 레벨업 예상 날짜 (한국어 포맷)
        /// </summary>
        public static string GetEstimatedLevelUpDateFormatted(string characterId, string characterName)
        {
            var estimatedDate = CalculateEstimatedLevelUpDate(characterId, characterName);
            if (!estimatedDate.HasValue) return "계산 불가";

            var daysRemaining = (estimatedDate.Value - DateTime.Today).Days;
            if (daysRemaining <= 0) return "오늘";
            if (daysRemaining == 1) return "내일";
            if (daysRemaining < 7) return $"{daysRemaining}일 후";
            if (daysRemaining < 30) return $"약 {daysRemaining / 7}주 후";
            if (daysRemaining < 365) return $"약 {daysRemaining / 30}개월 후";
            return $"약 {daysRemaining / 365}년 후";
        }

        public static List<ExpGrowthInfo> GetExperienceGrowth(string characterId, string characterName, int days = 7, string mode = "day")
        {
            var data = Load(characterName);
            var records = data.GrowthRecords
                .Where(r => r.CharacterId == characterId && r.Date >= DateTime.Today.AddDays(-days))
                .OrderBy(r => r.Date)
                .ToList();

            // 버킷 단위(일/주/월)로 집계: 각 버킷의 마지막 기록을 사용해 구간 차이를 계산
            var buckets = records
                .GroupBy(r => GetBucketKey(r.Date, mode))
                .Select(g => g.OrderBy(x => x.Date).Last())
                .OrderBy(r => r.Date)
                .ToList();

            var result = new List<ExpGrowthInfo>();
            for (int i = 1; i < buckets.Count; i++)
            {
                var prev = buckets[i - 1];
                var curr = buckets[i];

                // 실제 경험치 계산 (ExpTable 사용)
                long prevTotalExp = curr.TotalExp > 0 ? prev.TotalExp : ExpTable.CalculateTotalExp(prev.Level, prev.ExperienceRate);
                long currTotalExp = curr.TotalExp > 0 ? curr.TotalExp : ExpTable.CalculateTotalExp(curr.Level, curr.ExperienceRate);
                long dailyExpGain = currTotalExp - prevTotalExp;

                // 현재 레벨의 필요 경험치
                ExpTable.RequiredExp.TryGetValue(curr.Level, out long requiredExp);

                // % 단위 성장률 (레벨업 포함)
                double growth = curr.ExperienceRate - prev.ExperienceRate;
                if (curr.Level > prev.Level)
                {
                    growth += 100 * (curr.Level - prev.Level);
                }

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

            // GraphRate: 경험치 %를 0.0~1.0 범위로 직접 매핑 (절대적 기준)
            // 사용자 요청: 상대적 기준이 아닌 절대적 기준으로 변경
            if (result.Any())
            {
                foreach (var item in result)
                {
                    // 경험치 0~100%를 0.05~1.0 범위로 매핑 (최소 높이 보장)
                    item.GraphRate = 0.05 + 0.95 * (item.ExperienceRate / 100.0);
                }
            }

            return result;
        }

        public static List<CombatPowerGrowthInfo> GetCombatPowerGrowth(string characterId, string characterName, int days = 30, string mode = "day", bool onlyNewHigh = false)
        {
            var data = Load(characterName);
            var records = data.GrowthRecords
                .Where(r => r.CharacterId == characterId && r.Date >= DateTime.Today.AddDays(-days) && r.CombatPower > 0)
                .OrderBy(r => r.Date)
                .ToList();

            // 버킷 단위 집계 (일/주/월)
            var buckets = records
                .GroupBy(r => GetBucketKey(r.Date, mode))
                .Select(g => g.OrderBy(x => x.Date).Last())
                .OrderBy(r => r.Date)
                .ToList();

            var result = new List<CombatPowerGrowthInfo>();
            long runningMax = 0;

            // onlyNewHigh 모드: 가장 오래된 날짜부터 최신 날짜까지 순서대로 순회하며
            // 전투력이 최고 기록을 갱신할 때마다 기록
            if (onlyNewHigh)
            {
                for (int i = 0; i < buckets.Count; i++)
                {
                    var curr = buckets[i];
                    long newPower = curr.CombatPower;

                    if (newPower > runningMax)
                    {
                        result.Add(new CombatPowerGrowthInfo
                        {
                            Date = curr.Date,
                            OldPower = runningMax,
                            NewPower = newPower,
                            Change = newPower - runningMax
                        });
                        runningMax = newPower;
                    }
                }
            }
            else
            {
                // 기존 방식: 모든 변화 기록
                for (int i = 1; i < buckets.Count; i++)
                {
                    var prev = buckets[i - 1];
                    var curr = buckets[i];

                    long newPower = curr.CombatPower;
                    long oldPower = prev.CombatPower;
                    result.Add(new CombatPowerGrowthInfo
                    {
                        Date = curr.Date,
                        OldPower = oldPower,
                        NewPower = newPower,
                        Change = newPower - oldPower
                    });
                }
            }

            // GraphRate: 최소~최대 범위를 0.05~1.0 사이로 매핑 (빈 공간 최소화)
            if (result.Any())
            {
                long maxPower = result.Max(x => x.NewPower);
                long minPower = result.Min(x => x.NewPower);
                long range = maxPower - minPower;

                foreach (var item in result)
                {
                    if (range > 0)
                        item.GraphRate = 0.05 + 0.95 * ((double)(item.NewPower - minPower) / range);
                    else
                        item.GraphRate = 1.0; // 모든 값이 같으면 최대치
                }
            }

            return result;
        }

        private static DateTime GetBucketKey(DateTime date, string mode)
        {
            mode = (mode ?? "day").ToLowerInvariant();
            return mode switch
            {
                "week" => StartOfWeek(date),
                "month" => new DateTime(date.Year, date.Month, 1),
                _ => date.Date
            };
        }

        private static DateTime StartOfWeek(DateTime dt)
        {
            int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
            return dt.Date.AddDays(-diff);
        }

        public static List<HexaSkillRecord> GetHexaSkillHistory(string? characterId, string characterName, int limit = 20)
        {
            var data = Load(characterName);
            var query = data.HexaSkillRecords.AsEnumerable();

            if (!string.IsNullOrEmpty(characterId))
                query = query.Where(r => r.CharacterId == characterId);

            return query.OrderByDescending(r => r.Date).Take(limit).ToList();
        }

        public static List<ItemChangeRecord> GetItemChangeHistory(string? characterId, string characterName, int limit = 20)
        {
            var data = Load(characterName);
            var query = data.ItemChangeRecords.AsEnumerable();

            if (!string.IsNullOrEmpty(characterId))
                query = query.Where(r => r.CharacterId == characterId);

            return query.OrderByDescending(r => r.Date).Take(limit).ToList();
        }

        public static WeeklyReportData GetWeeklyReport(string characterName, DateTime weekStartDate)
        {
            var data = Load(characterName);
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

        public static MonthlyReportData GetMonthlyReport(string characterName, int year, int month)
        {
            var data = Load(characterName);
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

        public static List<CharacterGrowthRecord> GetGrowthHistory(string characterId, string characterName, int days = 30)
        {
            var data = Load(characterName);
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
            try
            {
                // 백업용: 모든 통계 파일 삭제
                if (Directory.Exists(DataFolder))
                {
                    var files = Directory.GetFiles(DataFolder, "statistics.json", SearchOption.AllDirectories);
                    foreach (var f in files)
                        File.Delete(f);
                }

                // api-raw 폴더 삭제
                var apiRawPath = System.IO.Path.Combine(DataFolder, "api-raw");
                if (System.IO.Directory.Exists(apiRawPath))
                {
                    System.IO.Directory.Delete(apiRawPath, true);
                }

                // Legacy file
                if (System.IO.File.Exists(LegacyFilePath))
                {
                    System.IO.File.Delete(LegacyFilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClearGrowthData error: {ex.Message}");
            }
        }
        #endregion
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

        // 한국어 단위 포맷팅
        public string NewPowerFormatted => ExpTable.FormatExpKorean(NewPower);
        public string ChangeFormatted => Change >= 0 ? $"+{ExpTable.FormatExpKorean(Change)}" : ExpTable.FormatExpKorean(Change);
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