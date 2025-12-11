using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using MapleHomework.Models;

namespace MapleHomework.Services
{
    public class MapleApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://open.api.nexon.com/maplestory/v1";
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MapleHomework", "api-collect.log");

        public MapleApiService()
        {
            _httpClient = new HttpClient();
            EnsureLogDirectory();
        }

        private static void EnsureLogDirectory()
        {
            try
            {
                var dir = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch { }
        }

        private static void Log(string message)
        {
            try
            {
                EnsureLogDirectory();
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch { }
        }

        // 1. 닉네임으로 OCID 조회
        public async Task<string?> GetOcidAsync(string apiKey, string characterName)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/id?character_name={characterName}");
                request.Headers.Add("x-nxopen-api-key", apiKey);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<OcidResponse>();
                return result?.Ocid;
            }
            catch
            {
                return null;
            }
        }

        // 공통 GET + 로깅
        private async Task<T?> GetJsonAsync<T>(string url, string apiKey, string context, string? ocid = null, string? date = null)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-nxopen-api-key", apiKey);

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    string body = await response.Content.ReadAsStringAsync();
                    Log($"[HTTP {context}] status={(int)response.StatusCode} ocid={ocid} date={date} body={body}");
                    return default;
                }

                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (Exception ex)
            {
                Log($"[HTTP {context}] EX ocid={ocid} date={date} msg={ex.Message}");
                return default;
            }
        }

        // 2. 캐릭터 정보 조회
        public Task<CharacterBasicResponse?> GetCharacterInfoAsync(string apiKey, string ocid, string? date = null)
        {
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return GetJsonAsync<CharacterBasicResponse>($"{BaseUrl}/character/basic?ocid={ocid}&date={date}", apiKey, "basic", ocid, date);
        }

        // 3. 심볼 장비 정보 조회
        public Task<SymbolEquipmentResponse?> GetSymbolEquipmentAsync(string apiKey, string ocid, string? date = null)
        {
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return GetJsonAsync<SymbolEquipmentResponse>($"{BaseUrl}/character/symbol-equipment?ocid={ocid}&date={date}", apiKey, "symbol", ocid, date);
        }

        // 4. 유니온 정보 조회
        public Task<UnionResponse?> GetUnionInfoAsync(string apiKey, string ocid, string? date = null)
        {
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return GetJsonAsync<UnionResponse>($"{BaseUrl}/user/union?ocid={ocid}&date={date}", apiKey, "union", ocid, date);
        }

        // 4-1. 유니온 공격대 정보 조회 (신규)
        public Task<UnionRaiderResponse?> GetUnionRaiderInfoAsync(string apiKey, string ocid, string? date = null)
        {
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return GetJsonAsync<UnionRaiderResponse>($"{BaseUrl}/user/union-raider?ocid={ocid}&date={date}", apiKey, "union-raider", ocid, date);
        }

        // 5. 캐릭터 스탯 정보 조회
        public Task<CharacterStatResponse?> GetCharacterStatAsync(string apiKey, string ocid, string? date = null)
        {
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return GetJsonAsync<CharacterStatResponse>($"{BaseUrl}/character/stat?ocid={ocid}&date={date}", apiKey, "stat", ocid, date);
        }

        // 6. 헥사 스킬 정보 조회 (코어 정보) - 기존 메서드 유지
        public async Task<HexaStatResponse?> GetHexaStatAsync(string apiKey, string ocid, string? date = null)
        {
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/character/hexamatrix?ocid={ocid}&date={date}");
                request.Headers.Add("x-nxopen-api-key", apiKey);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<HexaStatResponse>();
            }
            catch
            {
                return null;
            }
        }

        // 7. 캐릭터 스킬 정보 조회 (6차 스킬 아이콘 확보용)
        public Task<CharacterSkillResponse?> GetCharacterSkillAsync(string apiKey, string ocid, string? date = null, string grade = "6")
        {
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return GetJsonAsync<CharacterSkillResponse>($"{BaseUrl}/character/skill?ocid={ocid}&date={date}&character_skill_grade={grade}", apiKey, "skill", ocid, date);
        }

        // 8. 장비 정보 조회
        public Task<ItemEquipmentResponse?> GetItemEquipmentAsync(string apiKey, string ocid, string? date = null)
        {
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return GetJsonAsync<ItemEquipmentResponse>($"{BaseUrl}/character/item-equipment?ocid={ocid}&date={date}", apiKey, "item", ocid, date);
        }

        /// <summary>
        /// 과거 데이터를 수집하여 통계에 저장 (최대 1년 전까지)
        /// </summary>
        public async Task<GrowthHistoryResult> CollectGrowthHistoryAsync(
            string apiKey, 
            string ocid, 
            string characterId, 
            string characterName,
            int daysBack = 30,
            IProgress<int>? progress = null)
        {
            var result = new GrowthHistoryResult();
            var recordsAdded = 0;

            daysBack = Math.Min(daysBack, 365);

            Log($"[START] CollectGrowthHistory days={daysBack} id={characterId} name={characterName}");

            // 이전 상태 저장용
            Dictionary<string, int>? prevSkills = null; // 스킬명 -> 레벨
            Dictionary<string, string>? prevSkillIcons = null; // 스킬명 -> 아이콘
            Dictionary<string, ItemEquipmentInfo>? prevItems = null; // 슬롯 -> 장비정보

            // 과거 -> 현재 순으로 순회
            for (int i = daysBack; i >= 1; i--)
            {
                try
                {
                    var targetDate = DateTime.Now.AddDays(-i);
                    string dateStr = targetDate.ToString("yyyy-MM-dd");

                    // 1. 캐릭터 기본 정보
                    var charInfo = await GetCharacterInfoAsync(apiKey, ocid, dateStr);
                    
                    // 2. 유니온 정보
                    var unionInfo = await GetUnionInfoAsync(apiKey, ocid, dateStr);
                    
                    // 3. 스탯 정보
                    var statInfo = await GetCharacterStatAsync(apiKey, ocid, dateStr);

                    // 4. 6차 스킬 정보 (skill 엔드포인트 사용)
                    var skillInfo = await GetCharacterSkillAsync(apiKey, ocid, dateStr, "6");

                    // 5. 장비 정보
                    var itemInfo = await GetItemEquipmentAsync(apiKey, ocid, dateStr);

                    if (charInfo != null)
                    {
                        // 경험치 및 전투력 파싱
                        double expRate = 0;
                        if (!string.IsNullOrEmpty(charInfo.CharacterExpRate))
                            double.TryParse(charInfo.CharacterExpRate.Replace("%", ""), out expRate);

                        long combatPower = 0;
                        if (statInfo?.FinalStat != null)
                        {
                            var cpStat = statInfo.FinalStat.Find(s => s.StatName == "전투력");
                            if (cpStat != null) long.TryParse(cpStat.StatValue, out combatPower);
                        }

                        int unionLevel = unionInfo?.UnionLevel ?? 0;
                        long unionPower = unionInfo?.UnionArtifactLevel ?? 0;

                        // 성장 기록
                        StatisticsService.RecordCharacterGrowthForDate(
                            targetDate, characterId, characterName,
                            charInfo.CharacterLevel, 0, expRate,
                            combatPower, unionLevel, unionPower
                        );
                        recordsAdded++;
                        Log($"[{dateStr}] OK basic/union/stat lv={charInfo.CharacterLevel} exp={expRate}% cp={combatPower} union={unionLevel}");

                        // 6차 스킬 비교 및 기록
                        if (skillInfo?.CharacterSkill != null)
                        {
                            var currentSkills = skillInfo.CharacterSkill
                                .Where(s => !string.IsNullOrEmpty(s.SkillName))
                                .ToDictionary(s => s.SkillName!, s => s.SkillLevel);
                            
                            var currentIcons = skillInfo.CharacterSkill
                                .Where(s => !string.IsNullOrEmpty(s.SkillName))
                                .ToDictionary(s => s.SkillName!, s => s.SkillIcon ?? "");

                            if (prevSkills != null)
                            {
                                foreach (var skill in currentSkills)
                                {
                                    if (prevSkills.TryGetValue(skill.Key, out int oldLevel))
                                    {
                                        if (skill.Value > oldLevel)
                                        {
                                            string icon = currentIcons.ContainsKey(skill.Key) ? currentIcons[skill.Key] : "";
                                            RecordHexaSkillChangeForDate(targetDate, characterId, characterName, skill.Key, oldLevel, skill.Value, icon);
                                        }
                                    }
                                    else
                                    {
                                        // 신규 스킬 (0 -> Level)
                                        string icon = currentIcons.ContainsKey(skill.Key) ? currentIcons[skill.Key] : "";
                                        RecordHexaSkillChangeForDate(targetDate, characterId, characterName, skill.Key, 0, skill.Value, icon);
                                    }
                                }
                            }
                            prevSkills = currentSkills;
                            prevSkillIcons = currentIcons;
                        }
                        else
                        {
                            Log($"[{dateStr}] WARN skillInfo null");
                        }

                        // 장비 변경 비교 및 기록
                        if (itemInfo?.ItemEquipment != null)
                        {
                            var currentItems = itemInfo.ItemEquipment
                                .Where(item => !string.IsNullOrEmpty(item.ItemEquipmentSlot) && !string.IsNullOrEmpty(item.ItemName))
                                .ToDictionary(item => item.ItemEquipmentSlot!, item => item);

                            if (prevItems != null)
                            {
                                foreach (var itemPair in currentItems)
                                {
                                    var newItem = itemPair.Value;
                                    string slot = itemPair.Key;

                                    if (prevItems.TryGetValue(slot, out var oldItem))
                                    {
                                        // 이름이 다르거나, 상세 옵션이 다른 경우
                                        if (oldItem.ItemName != newItem.ItemName)
                                        {
                                            string json = JsonSerializer.Serialize(newItem);
                                            RecordItemChangeForDate(targetDate, characterId, characterName, slot, oldItem.ItemName!, newItem.ItemName!, "교체", json);
                                        }
                                        else if (IsItemOptionChanged(oldItem, newItem))
                                        {
                                            string json = JsonSerializer.Serialize(newItem);
                                            RecordItemChangeForDate(targetDate, characterId, characterName, slot, oldItem.ItemName!, newItem.ItemName!, "옵션 변경", json);
                                        }
                                    }
                                    else
                                    {
                                        // 신규 장착
                                        string json = JsonSerializer.Serialize(newItem);
                                        RecordItemChangeForDate(targetDate, characterId, characterName, slot, "없음", newItem.ItemName!, "장착", json);
                                    }
                                }
                            }
                            prevItems = currentItems;
                        }
                        else
                        {
                            Log($"[{dateStr}] WARN itemInfo null");
                        }
                    }
                    else
                    {
                        Log($"[{dateStr}] FAIL charInfo null");
                    }

                    int processedCount = daysBack - i + 1;
                    progress?.Report((int)((double)processedCount / daysBack * 100));

                    // 레이트 리밋 완화
                    await Task.Delay(300);
                }
                catch (HttpRequestException httpEx)
                {
                    var code = httpEx.StatusCode.HasValue ? ((int)httpEx.StatusCode.Value).ToString() : "unknown";
                    Log($"[HTTP ERROR] status={code} message={httpEx.Message}");
                    await Task.Delay(500);
                    continue;
                }
                catch (Exception ex)
                {
                    Log($"[ERROR] {ex.Message}");
                    await Task.Delay(300);
                    continue;
                }
            }

            result.Success = recordsAdded > 0;
            result.RecordsAdded = recordsAdded;
            result.Message = recordsAdded > 0
                ? $"{recordsAdded}일치 데이터를 수집했습니다."
                : "수집된 데이터가 없습니다. 로그(api-collect.log)를 확인해주세요.";

            Log($"[END] recordsAdded={recordsAdded}");

            return result;
        }

        private bool IsItemOptionChanged(ItemEquipmentInfo oldItem, ItemEquipmentInfo newItem)
        {
            // 주요 스펙 비교
            if (oldItem.Starforce != newItem.Starforce) return true;
            if (oldItem.PotentialOptionGrade != newItem.PotentialOptionGrade) return true;
            if (oldItem.AdditionalPotentialOptionGrade != newItem.AdditionalPotentialOptionGrade) return true;
            
            // 잠재능력 상세 비교
            if (oldItem.PotentialOption1 != newItem.PotentialOption1) return true;
            if (oldItem.PotentialOption2 != newItem.PotentialOption2) return true;
            if (oldItem.PotentialOption3 != newItem.PotentialOption3) return true;
            
            // 에디셔널 상세 비교
            if (oldItem.AdditionalPotentialOption1 != newItem.AdditionalPotentialOption1) return true;
            if (oldItem.AdditionalPotentialOption2 != newItem.AdditionalPotentialOption2) return true;
            if (oldItem.AdditionalPotentialOption3 != newItem.AdditionalPotentialOption3) return true;

            // 추옵 비교 (ItemTotalOption - ItemBaseOption)으로 계산하거나 TotalOption 자체를 비교
            // 여기서는 TotalOption 객체 전체 비교 (참조 비교가 아닌 값 비교 필요)
            if (!AreOptionsEqual(oldItem.ItemTotalOption, newItem.ItemTotalOption)) return true;

            return false;
        }

        private bool AreOptionsEqual(ItemOptionInfo? opt1, ItemOptionInfo? opt2)
        {
            if (opt1 == null && opt2 == null) return true;
            if (opt1 == null || opt2 == null) return false;

            return opt1.Str == opt2.Str &&
                   opt1.Dex == opt2.Dex &&
                   opt1.Int == opt2.Int &&
                   opt1.Luk == opt2.Luk &&
                   opt1.AttackPower == opt2.AttackPower &&
                   opt1.MagicPower == opt2.MagicPower &&
                   opt1.AllStat == opt2.AllStat &&
                   opt1.BossDamage == opt2.BossDamage &&
                   opt1.IgnoreMonsterArmor == opt2.IgnoreMonsterArmor;
        }

        private void RecordHexaSkillChangeForDate(DateTime date, string characterId, string characterName, string skillName, int oldLevel, int newLevel, string icon)
        {
            StatisticsService.RecordHexaSkillChange(characterId, characterName, skillName, oldLevel, newLevel, icon, date);
        }

        private void RecordItemChangeForDate(DateTime date, string characterId, string characterName, string slot, string oldItem, string newItem, string type, string json)
        {
            StatisticsService.RecordItemChange(characterId, characterName, slot, oldItem, newItem, type, json, date);
        }
    }

    public class GrowthHistoryResult
    {
        public bool Success { get; set; }
        public int RecordsAdded { get; set; }
        public string Message { get; set; } = "";
    }
}
