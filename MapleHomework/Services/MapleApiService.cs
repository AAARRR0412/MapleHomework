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
        private static readonly string RawPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MapleHomework", "api-raw");

        public MapleApiService()
        {
            _httpClient = new HttpClient();
            EnsureLogDirectory();
            EnsureRawDirectory();
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

        private static void EnsureRawDirectory()
        {
            try
            {
                if (!Directory.Exists(RawPath))
                    Directory.CreateDirectory(RawPath);
            }
            catch { }
        }

        private static void SaveRaw(string dateStr, string category, object data)
        {
            try
            {
                EnsureRawDirectory();
                string file = Path.Combine(RawPath, $"{dateStr}-{category}.json");
                File.WriteAllText(file, JsonSerializer.Serialize(data));
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
            // 재시도 로직: 429/400에서 최대 3회 (0.4s, 0.6s) 백오프, 기본 딜레이 0.2s
            const int maxRetry = 3;
            for (int attempt = 1; attempt <= maxRetry; attempt++)
            {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-nxopen-api-key", apiKey);

                    var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                {
                        var result = await response.Content.ReadFromJsonAsync<T>();
                        await Task.Delay(200); // 요청 간 0.2초 지연
                        return result;
                    }

                    string body = await response.Content.ReadAsStringAsync();
                    var status = (int)response.StatusCode;
                    bool shouldRetry = status == 429 || status == 400;

                    Log($"[HTTP {context}] status={status} ocid={ocid} date={date} body={body} attempt={attempt}");

                    if (!shouldRetry || attempt == maxRetry)
                    {
                        await Task.Delay(200);
                        return default;
                    }

                    // 백오프 (0.4s, 0.6s, 0.8s)
                    int delayMs = attempt == 1 ? 400 : attempt == 2 ? 600 : 800;
                    await Task.Delay(delayMs);
                    continue;
            }
            catch (Exception ex)
            {
                    Log($"[HTTP {context}] EX ocid={ocid} date={date} msg={ex.Message} attempt={attempt}");
                    if (attempt == maxRetry)
                    {
                        await Task.Delay(200);
                        return default;
                    }
                    int delayMs = attempt == 1 ? 400 : attempt == 2 ? 600 : 800;
                    await Task.Delay(delayMs);
                }
            }

            await Task.Delay(200);
            return default;
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

            // 대상 날짜 목록 (과거 -> 현재)
            var dateList = Enumerable.Range(1, daysBack)
                .Select(i => DateTime.Now.AddDays(-i))
                .OrderBy(d => d)
                .ToList();
            double totalSteps = Math.Max(1, dateList.Count * 3.0);
            double step = 0;
            void ReportStep()
            {
                if (progress == null) return;
                int pct = (int)Math.Min(100, Math.Round(step / totalSteps * 100));
                progress.Report(Math.Max(0, pct));
            }
            ReportStep();

            // 1) 경험치/전투력/유니온 (basic/union/stat) 전체 수집
            foreach (var targetDate in dateList)
            {
                string dateStr = targetDate.ToString("yyyy-MM-dd");
                try
                {
                    var charInfo = await GetCharacterInfoAsync(apiKey, ocid, dateStr);
                    var unionInfo = await GetUnionInfoAsync(apiKey, ocid, dateStr);
                    var statInfo = await GetCharacterStatAsync(apiKey, ocid, dateStr);
                    SaveRaw(dateStr, "basic", charInfo ?? new { empty = true });
                    SaveRaw(dateStr, "union", unionInfo ?? new { empty = true });
                    SaveRaw(dateStr, "stat", statInfo ?? new { empty = true });

                    if (charInfo == null)
                    {
                        Log($"[{dateStr}] FAIL charInfo null");
                        step++; ReportStep();
                        continue;
                    }

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

                    StatisticsService.RecordCharacterGrowthForDate(
                        targetDate, characterId, characterName,
                        charInfo.CharacterLevel, 0, expRate,
                        combatPower, unionLevel, unionPower
                    );
                    recordsAdded++;
                    Log($"[{dateStr}] OK basic/union/stat lv={charInfo.CharacterLevel} exp={expRate}% cp={combatPower} union={unionLevel}");
                }
                catch (Exception ex)
                {
                    Log($"[{dateStr}] ERR basic/union/stat {ex.Message}");
                }
                step++; ReportStep();
            }

            // 2) 6차 스킬 전체 수집 (날짜 순차)
            Dictionary<string, int>? prevSkills = null;
            Dictionary<string, string>? prevSkillIcons = null;
            foreach (var targetDate in dateList)
            {
                string dateStr = targetDate.ToString("yyyy-MM-dd");
                try
                {
                    var skillInfo = await GetCharacterSkillAsync(apiKey, ocid, dateStr, "6");
                    SaveRaw(dateStr, "skill6", skillInfo ?? new { empty = true });
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
                }
                catch (Exception ex)
                {
                    Log($"[{dateStr}] ERR skill {ex.Message}");
                }
                step++; ReportStep();
            }

            // 3) 장비 변경 전체 수집 (날짜 순차)
            Dictionary<string, ItemEquipmentInfo>? prevItems = null;
            HashSet<string> prevPresetNames = new();
            foreach (var targetDate in dateList)
            {
                string dateStr = targetDate.ToString("yyyy-MM-dd");
                try
                {
                    var itemInfo = await GetItemEquipmentAsync(apiKey, ocid, dateStr);
                    SaveRaw(dateStr, "item", itemInfo ?? new { empty = true });
                    if (itemInfo?.ItemEquipment != null)
                    {
                        // 슬롯 중복 방지: 같은 슬롯이 여러 번 올 경우 첫 번째만 사용
                        var currentItems = itemInfo.ItemEquipment
                            .Where(item => !string.IsNullOrEmpty(item.ItemEquipmentSlot) && !string.IsNullOrEmpty(item.ItemName))
                            .GroupBy(item => item.ItemEquipmentSlot!)
                            .ToDictionary(g => g.Key, g => g.First());

                        // 프리셋에 포함된 아이템 이름 집합
                        var currentPresetNames = CollectPresetNames(itemInfo);

                        if (prevItems != null)
                        {
                            foreach (var itemPair in currentItems)
                            {
                                var newItem = itemPair.Value;
                                string slot = itemPair.Key;

                                if (prevItems.TryGetValue(slot, out var oldItem))
                                {
                                    if (oldItem.ItemName != newItem.ItemName)
                                    {
                                        // 프리셋 전환만으로 인한 교체면 스킵
                                        if (currentPresetNames.Contains(newItem.ItemName ?? "") || prevPresetNames.Contains(oldItem.ItemName ?? ""))
                                            continue;
                                        // 정령의 펜던트는 스킵
                                        if (IsSpiritPendant(newItem.ItemName) || IsSpiritPendant(oldItem.ItemName))
                                            continue;

                                        string json = JsonSerializer.Serialize(newItem);
                                        string summary = BuildChangeSummary(oldItem, newItem, isReplace:true);
                                        RecordItemChangeForDate(targetDate, characterId, characterName, slot, oldItem.ItemName!, newItem.ItemName!, "교체", json, summary);
                                        Log($"[{dateStr}] ITEM slot={slot} replace {oldItem.ItemName} -> {newItem.ItemName}");
                                    }
                                    else if (IsItemOptionChanged(oldItem, newItem))
                                    {
                                        if (IsSpiritPendant(newItem.ItemName)) continue;
                                        string json = JsonSerializer.Serialize(newItem);
                                        string summary = BuildChangeSummary(oldItem, newItem);
                                        RecordItemChangeForDate(targetDate, characterId, characterName, slot, oldItem.ItemName!, newItem.ItemName!, "옵션 변경", json, summary);
                                        Log($"[{dateStr}] ITEM slot={slot} option-change {newItem.ItemName}");
                                    }
                                }
                                else
                                {
                                    string json = JsonSerializer.Serialize(newItem);
                                    // 프리셋에 이미 존재하는 아이템이면 신규 장착으로 보지 않음
                                    if (currentPresetNames.Contains(newItem.ItemName ?? "") || prevPresetNames.Contains(newItem.ItemName ?? ""))
                                        continue;
                                    if (IsSpiritPendant(newItem.ItemName)) continue;
                                    string summary = BuildChangeSummary(null, newItem, isNew:true);
                                    RecordItemChangeForDate(targetDate, characterId, characterName, slot, "없음", newItem.ItemName!, "장착", json, summary);
                                    Log($"[{dateStr}] ITEM slot={slot} equip {newItem.ItemName}");
                                }
                            }
                        }
                        prevItems = currentItems;
                        prevPresetNames = currentPresetNames;
                    }
                    else
                    {
                        Log($"[{dateStr}] WARN itemInfo null");
                    }
                }
                catch (Exception ex)
                {
                    Log($"[{dateStr}] ITEM error {ex.Message}");
                }
                step++; ReportStep();
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
            if (!AreOptionsEqual(oldItem.ItemBaseOption, newItem.ItemBaseOption)) return true;
            if (!AreOptionsEqual(oldItem.ItemAddOption, newItem.ItemAddOption)) return true;
            if (!AreOptionsEqual(oldItem.ItemEtcOption, newItem.ItemEtcOption)) return true;
            if (!AreOptionsEqual(oldItem.ItemStarforceOption, newItem.ItemStarforceOption)) return true;

            return false;
        }

        private string BuildChangeSummary(ItemEquipmentInfo? oldItem, ItemEquipmentInfo newItem, bool isNew = false, bool isReplace = false)
        {
            var parts = new List<string>();

            // 신규 장착/교체
            if (isNew)
            {
                parts.Add("신규 장착");
            }
            else if (isReplace && oldItem != null && !string.IsNullOrEmpty(oldItem.ItemName) && !string.IsNullOrEmpty(newItem.ItemName) && oldItem.ItemName != newItem.ItemName)
            {
                parts.Add($"{oldItem.ItemName} → {newItem.ItemName}");
            }

            // 스타포스
            int oldStar = ParseIntSafe(oldItem?.Starforce);
            int newStar = ParseIntSafe(newItem.Starforce);
            if (oldStar != newStar)
            {
                parts.Add($"스타포스 {oldStar}성 → {newStar}성");
            }

            // 잠재능력
            if (oldItem == null || oldItem.PotentialOptionGrade != newItem.PotentialOptionGrade ||
                oldItem.PotentialOption1 != newItem.PotentialOption1 ||
                oldItem.PotentialOption2 != newItem.PotentialOption2 ||
                oldItem.PotentialOption3 != newItem.PotentialOption3)
            {
                if (!string.IsNullOrEmpty(newItem.PotentialOptionGrade))
                    parts.Add($"잠재 {newItem.PotentialOptionGrade}");
                else if (oldItem != null && !string.IsNullOrEmpty(oldItem.PotentialOptionGrade))
                    parts.Add("잠재 변경");
            }

            // 에디셔널 잠재
            if (oldItem == null || oldItem.AdditionalPotentialOptionGrade != newItem.AdditionalPotentialOptionGrade ||
                oldItem.AdditionalPotentialOption1 != newItem.AdditionalPotentialOption1 ||
                oldItem.AdditionalPotentialOption2 != newItem.AdditionalPotentialOption2 ||
                oldItem.AdditionalPotentialOption3 != newItem.AdditionalPotentialOption3)
            {
                if (!string.IsNullOrEmpty(newItem.AdditionalPotentialOptionGrade))
                    parts.Add($"에디셔널 {newItem.AdditionalPotentialOptionGrade}");
                else if (oldItem != null && !string.IsNullOrEmpty(oldItem.AdditionalPotentialOptionGrade))
                    parts.Add("에디셔널 변경");
            }

            // 추가옵션
            if (oldItem == null || !AreOptionsEqual(oldItem.ItemAddOption, newItem.ItemAddOption))
            {
                parts.Add("추가옵션 변경");
            }

            // 스타포스 옵션
            if (oldItem == null || !AreOptionsEqual(oldItem.ItemStarforceOption, newItem.ItemStarforceOption))
            {
                if (!parts.Contains($"스타포스 {oldStar}성 → {newStar}성")) // avoid duplicate
                    parts.Add("스타포스 옵션 변경");
            }

            // 주문서 옵션 (etc)
            if (oldItem == null || !AreOptionsEqual(oldItem.ItemEtcOption, newItem.ItemEtcOption))
            {
                parts.Add("주문서 옵션 변경");
            }

            // 소울
            if (oldItem == null || oldItem.SoulOption != newItem.SoulOption)
            {
                if (!string.IsNullOrEmpty(newItem.SoulOption))
                    parts.Add("소울 변경");
            }

            // fallback
            if (parts.Count == 0)
                parts.Add("옵션 변경");

            return string.Join(" / ", parts);
        }

        private static int ParseIntSafe(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            var cleaned = s.Replace(",", "").Trim();
            return int.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v)
                ? v : 0;
        }

        private static HashSet<string> CollectPresetNames(ItemEquipmentResponse info)
        {
            var set = new HashSet<string>();
            void AddRange(List<ItemEquipmentInfo>? list)
            {
                if (list == null) return;
                foreach (var it in list)
                {
                    if (!string.IsNullOrEmpty(it.ItemName))
                        set.Add(it.ItemName);
                }
            }
            AddRange(info.ItemEquipmentPreset1);
            AddRange(info.ItemEquipmentPreset2);
            AddRange(info.ItemEquipmentPreset3);
            return set;
        }

        private static bool IsSpiritPendant(string? name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.Contains("정령의 펜던트");
        }

        private bool AreOptionsEqual(ItemOptionInfo? opt1, ItemOptionInfo? opt2)
        {
            if (opt1 == null && opt2 == null) return true;
            if (opt1 == null || opt2 == null) return false;

            return opt1.Str == opt2.Str &&
                   opt1.Dex == opt2.Dex &&
                   opt1.Int == opt2.Int &&
                   opt1.Luk == opt2.Luk &&
                   opt1.MaxHp == opt2.MaxHp &&
                   opt1.MaxMp == opt2.MaxMp &&
                   opt1.AttackPower == opt2.AttackPower &&
                   opt1.MagicPower == opt2.MagicPower &&
                   opt1.Armor == opt2.Armor &&
                   opt1.Speed == opt2.Speed &&
                   opt1.Jump == opt2.Jump &&
                   opt1.BossDamage == opt2.BossDamage &&
                   opt1.IgnoreMonsterArmor == opt2.IgnoreMonsterArmor &&
                   opt1.AllStat == opt2.AllStat &&
                   opt1.Damage == opt2.Damage &&
                   opt1.EquipmentLevelDecrease.ToString() == opt2.EquipmentLevelDecrease.ToString() &&
                   opt1.MaxHpRate == opt2.MaxHpRate &&
                   opt1.MaxMpRate == opt2.MaxMpRate &&
                   opt1.BaseEquipmentLevel.ToString() == opt2.BaseEquipmentLevel.ToString();
        }

        private void RecordHexaSkillChangeForDate(DateTime date, string characterId, string characterName, string skillName, int oldLevel, int newLevel, string icon)
        {
            StatisticsService.RecordHexaSkillChange(characterId, characterName, skillName, oldLevel, newLevel, icon, date);
        }

        private void RecordItemChangeForDate(DateTime date, string characterId, string characterName, string slot, string oldItem, string newItem, string type, string json, string summary)
        {
            StatisticsService.RecordItemChange(characterId, characterName, slot, oldItem, newItem, type, json, date, summary);
        }
    }

    public class GrowthHistoryResult
    {
        public bool Success { get; set; }
        public int RecordsAdded { get; set; }
        public string Message { get; set; } = "";
    }
}
