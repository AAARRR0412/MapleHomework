using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MapleHomework.Models;

namespace MapleHomework.Services
{
    public class BatchCollectResponse
    {
        public bool Success { get; set; }
        public int Count { get; set; }
        public Dictionary<string, Dictionary<string, JsonElement>>? Data { get; set; }
    }

    public class UnionChampionResponse
    {
        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("union_champion")]
        public List<UnionChampion>? Champions { get; set; }

        [JsonPropertyName("champion_badge_total_info")]
        public List<ChampionBadgeInfo>? TotalInfo { get; set; }
    }

    public class UnionChampion
    {
        [JsonPropertyName("champion_name")]
        public string? ChampionName { get; set; }

        [JsonPropertyName("champion_slot")]
        public int ChampionSlot { get; set; }

        [JsonPropertyName("champion_grade")]
        public string? ChampionGrade { get; set; }

        [JsonPropertyName("champion_class")]
        public string? ChampionClass { get; set; }

        [JsonPropertyName("champion_badge_info")]
        public List<ChampionBadgeInfo>? BadgeInfo { get; set; }
    }

    public class ChampionBadgeInfo
    {
        [JsonPropertyName("stat")]
        public string? Stat { get; set; }
    }

    public class MapleApiService : IMapleApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://maple-api-proxy.hoon7604.workers.dev/maplestory/v1";
        private const string ProxyBaseUrl = "https://maple-api-proxy.hoon7604.workers.dev";
        private const int BatchSizeLimit = 15;

        private static readonly string RawPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "MapleScheduler", "api-raw");

        public MapleApiService()
        {
            _httpClient = new HttpClient();
            EnsureDirectory(RawPath);
        }

        private static void EnsureDirectory(string path)
        {
            try { if (!Directory.Exists(path)) Directory.CreateDirectory(path); } catch { }
        }

        private static void SaveRaw(string characterName, string dateStr, string category, object data)
        {
            try
            {
                // 캐릭터별 폴더 생성: api-raw/{Name}
                string charPath = Path.Combine(RawPath, characterName);
                EnsureDirectory(charPath);

                string file = Path.Combine(charPath, $"{dateStr}-{category}.json");
                File.WriteAllText(file, JsonSerializer.Serialize(data));
            }
            catch { }
        }

        public async Task<string?> GetOcidAsync(string characterName)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/id?character_name={characterName}");
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<OcidResponse>();
                return result?.Ocid;
            }
            catch { return null; }
        }

        private async Task<T?> GetJsonAsync<T>(string url)
        {
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                        return await response.Content.ReadFromJsonAsync<T>();
                    if ((int)response.StatusCode == 429)
                    {
                        await Task.Delay(500 * attempt);
                        continue;
                    }
                    break;
                }
                catch
                {
                    if (attempt == 3) return default;
                    await Task.Delay(500);
                }
            }
            return default;
        }

        private string? GetEffectiveQueryDate(string? date)
        {
            // 1. null이면 어제 날짜로 설정 (기존 로직 유지)
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            // 2. 만약 설정된 날짜가 '오늘'이라면 null로 반환하여 API 호출 시 파라미터 제외
            if (date == DateTime.Now.ToString("yyyy-MM-dd"))
            {
                return null;
            }

            return date;
        }

        // --- 단건 조회 메서드 ---
        public Task<CharacterBasicResponse?> GetCharacterInfoAsync(string ocid, string? date = null)
        {
            string? queryDate = GetEffectiveQueryDate(date);
            string param = queryDate != null ? $"&date={queryDate}" : "";
            return GetJsonAsync<CharacterBasicResponse>($"{BaseUrl}/character/basic?ocid={ocid}{param}");
        }
        public Task<SymbolEquipmentResponse?> GetSymbolEquipmentAsync(string ocid, string? date = null)
        {
            string? queryDate = GetEffectiveQueryDate(date);
            string param = queryDate != null ? $"&date={queryDate}" : "";
            return GetJsonAsync<SymbolEquipmentResponse>($"{BaseUrl}/character/symbol-equipment?ocid={ocid}{param}");
        }

        public Task<UnionChampionResponse?> GetUnionChampionAsync(string ocid, string? date = null)
        {
            string? queryDate = GetEffectiveQueryDate(date);
            string param = queryDate != null ? $"&date={queryDate}" : "";
            return GetJsonAsync<UnionChampionResponse>($"{BaseUrl}/user/union-champion?ocid={ocid}{param}");
        }
        public Task<UnionResponse?> GetUnionInfoAsync(string ocid, string? date = null)
        {
            string? queryDate = GetEffectiveQueryDate(date);
            string param = queryDate != null ? $"&date={queryDate}" : "";
            return GetJsonAsync<UnionResponse>($"{BaseUrl}/user/union?ocid={ocid}{param}");
        }
        public Task<CharacterStatResponse?> GetCharacterStatAsync(string ocid, string? date = null)
        {
            string? queryDate = GetEffectiveQueryDate(date);
            string param = queryDate != null ? $"&date={queryDate}" : "";
            return GetJsonAsync<CharacterStatResponse>($"{BaseUrl}/character/stat?ocid={ocid}{param}");
        }
        public Task<HexaStatResponse?> GetHexaStatAsync(string ocid, string? date = null)
        {
            string? queryDate = GetEffectiveQueryDate(date);
            string param = queryDate != null ? $"&date={queryDate}" : "";
            return GetJsonAsync<HexaStatResponse>($"{BaseUrl}/character/hexamatrix?ocid={ocid}{param}");
        }
        public Task<HexaMatrixStatResponse?> GetHexaMatrixStatAsync(string ocid, string? date = null)
        {
            string? queryDate = GetEffectiveQueryDate(date);
            string param = queryDate != null ? $"&date={queryDate}" : "";
            return GetJsonAsync<HexaMatrixStatResponse>($"{BaseUrl}/character/hexamatrix-stat?ocid={ocid}{param}");
        }
        public Task<CharacterSkillResponse?> GetCharacterSkillAsync(string ocid, string? date = null, string grade = "6")
        {
            string? queryDate = GetEffectiveQueryDate(date);
            string param = queryDate != null ? $"&date={queryDate}" : "";
            return GetJsonAsync<CharacterSkillResponse>($"{BaseUrl}/character/skill?ocid={ocid}{param}&character_skill_grade={grade}");
        }
        public Task<ItemEquipmentResponse?> GetItemEquipmentAsync(string ocid, string? date = null)
        {
            string? queryDate = GetEffectiveQueryDate(date);
            string param = queryDate != null ? $"&date={queryDate}" : "";
            return GetJsonAsync<ItemEquipmentResponse>($"{BaseUrl}/character/item-equipment?ocid={ocid}{param}");
        }
        public Task<UnionRaiderResponse?> GetUnionRaiderInfoAsync(string ocid, string? date = null)
        {
            string? queryDate = GetEffectiveQueryDate(date);
            string param = queryDate != null ? $"&date={queryDate}" : "";
            return GetJsonAsync<UnionRaiderResponse>($"{BaseUrl}/user/union-raider?ocid={ocid}{param}");
        }
        // [신규] 시드링 교체 슬롯 조회
        public Task<RingExchangeResponse?> GetRingExchangeAsync(string ocid, string? date = null)
        {
            string? queryDate = GetEffectiveQueryDate(date);
            string param = queryDate != null ? $"&date={queryDate}" : "";
            return GetJsonAsync<RingExchangeResponse>($"{BaseUrl}/character/ring-exchange-skill-equipment?ocid={ocid}{param}");
        }

        #region 배치 API 수집 및 처리

        public async Task<BatchCollectResponse?> BatchCollectAsync(string ocid, List<string> dates, List<string>? types = null)
        {
            types ??= new List<string> { "basic", "union", "stat" };
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"{ProxyBaseUrl}/batch-collect",
                    new { ocid = ocid, dates = dates, types = types }
                );
                return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<BatchCollectResponse>() : null;
            }
            catch { return null; }
        }

        private T? ExtractFromBatchResponse<T>(BatchCollectResponse response, string date, string type)
        {
            try
            {
                if (response.Data != null && response.Data.TryGetValue(date, out var dateData) &&
                    dateData.TryGetValue(type, out var element) &&
                    !(element.TryGetProperty("error", out var err) && err.GetBoolean()))
                {
                    return JsonSerializer.Deserialize<T>(element.GetRawText());
                }
                return default;
            }
            catch { return default; }
        }

        public async Task<GrowthHistoryResult> CollectGrowthHistoryBatchAsync(
            string ocid, string characterId, string characterName,
            List<DateTime> targetDates, IProgress<int>? progress = null)
        {
            var result = new GrowthHistoryResult();
            var recordsAdded = 0;
            var dateList = targetDates.OrderBy(d => d).ToList(); // Restored for post-processing usage
            var today = DateTime.Today;
            var hasToday = targetDates.Any(d => d.Date == today);

            // 배치는 오늘을 제외한 과거 날짜만 처리
            var batchDates = targetDates.Where(d => d.Date != today).OrderBy(d => d).ToList();
            var batchDateStrings = batchDates.Select(d => d.ToString("yyyy-MM-dd")).ToList();
            var batches = SplitIntoBatches(batchDateStrings, BatchSizeLimit);

            // 전체 단계: 배치 단계 + (오늘 데이터가 있다면 1단계 추가)
            int totalSteps = batches.Count * 3 + (hasToday ? 1 : 0) + 1;
            int currentStep = 0;

            void ReportProgress() => progress?.Report((int)((double)currentStep / totalSteps * 100));

            // 1. 기본 정보 (배치)
            foreach (var batch in batches)
            {
                var resp = await BatchCollectAsync(ocid, batch, new List<string> { "basic", "union", "stat" });
                if (resp?.Data != null)
                {
                    foreach (var dateStr in batch)
                    {
                        var basic = ExtractFromBatchResponse<CharacterBasicResponse>(resp, dateStr, "basic");
                        var union = ExtractFromBatchResponse<UnionResponse>(resp, dateStr, "union");
                        var stat = ExtractFromBatchResponse<CharacterStatResponse>(resp, dateStr, "stat");

                        SaveRaw(characterName, dateStr, "basic", (object?)basic ?? new { empty = true });
                        SaveRaw(characterName, dateStr, "union", (object?)union ?? new { empty = true });
                        SaveRaw(characterName, dateStr, "stat", (object?)stat ?? new { empty = true });

                        if (basic != null)
                        {
                            double expRate = 0;
                            if (basic.CharacterExpRate != null) double.TryParse(basic.CharacterExpRate.Replace("%", ""), out expRate);
                            long cp = 0;
                            var cpStat = stat?.FinalStat?.Find(s => s.StatName == "전투력");
                            if (cpStat != null) long.TryParse(cpStat.StatValue, out cp);

                            StatisticsService.RecordCharacterGrowthForDate(
                                DateTime.Parse(dateStr), characterId, characterName,
                                basic.CharacterLevel, 0, expRate, cp,
                                union?.UnionLevel ?? 0, union?.UnionArtifactLevel ?? 0
                            );
                            recordsAdded++;
                        }
                    }
                }
                currentStep++;
                ReportProgress();
                await Task.Delay(100);
            }

            // 2. 6차 스킬 (배치)
            foreach (var batch in batches)
            {
                var resp = await BatchCollectAsync(ocid, batch, new List<string> { "skill6", "hexamatrix", "hexastat" });
                if (resp?.Data != null)
                {
                    foreach (var dateStr in batch)
                    {
                        var skill = ExtractFromBatchResponse<CharacterSkillResponse>(resp, dateStr, "skill6");
                        var hexa = ExtractFromBatchResponse<HexaStatResponse>(resp, dateStr, "hexamatrix");
                        var hexaStat = ExtractFromBatchResponse<HexaMatrixStatResponse>(resp, dateStr, "hexastat");

                        SaveRaw(characterName, dateStr, "skill6", (object?)skill ?? new { empty = true });
                        SaveRaw(characterName, dateStr, "hexamatrix", (object?)hexa ?? new { empty = true });
                        SaveRaw(characterName, dateStr, "hexastat", (object?)hexaStat ?? new { empty = true });
                        RecordHexaSkillChanges(DateTime.Parse(dateStr), characterId, characterName, skill);
                    }
                }
                currentStep++;
                ReportProgress();
                await Task.Delay(100);
            }

            // 3. 장비 정보 (배치)
            foreach (var batch in batches)
            {
                var taskBatch = BatchCollectAsync(ocid, batch, new List<string> { "item" });
                var ringTasks = batch.Select(d => GetRingExchangeAsync(ocid, d)).ToList();

                var resp = await taskBatch;
                var ringResults = await Task.WhenAll(ringTasks);

                for (int i = 0; i < batch.Count; i++)
                {
                    string dateStr = batch[i];

                    if (resp?.Data != null)
                    {
                        var item = ExtractFromBatchResponse<ItemEquipmentResponse>(resp, dateStr, "item");
                        SaveRaw(characterName, dateStr, "item", (object?)item ?? new { empty = true });
                    }

                    var ring = ringResults[i];
                    SaveRaw(characterName, dateStr, "ring", (object?)ring ?? new { empty = true });
                }

                currentStep++;
                ReportProgress();
                await Task.Delay(100);
            }

            // 4. [오늘 데이터] 개별 수집 (Parameter Omit 로직 적용)
            if (hasToday)
            {
                string todayStr = today.ToString("yyyy-MM-dd");
                string? dateParam = todayStr; // 명시적으로 오늘 날짜를 전달해야 헬퍼 메서드에서 null(오늘)로 변환됨

                try
                {
                    // 기본 + 유니온 + 스탯
                    var basic = await GetCharacterInfoAsync(ocid, dateParam);
                    var union = await GetUnionInfoAsync(ocid, dateParam);
                    var stat = await GetCharacterStatAsync(ocid, dateParam);

                    SaveRaw(characterName, todayStr, "basic", (object?)basic ?? new { empty = true });
                    SaveRaw(characterName, todayStr, "union", (object?)union ?? new { empty = true });
                    SaveRaw(characterName, todayStr, "stat", (object?)stat ?? new { empty = true });

                    if (basic != null)
                    {
                        double expRate = 0;
                        if (basic.CharacterExpRate != null) double.TryParse(basic.CharacterExpRate.Replace("%", ""), out expRate);
                        long cp = 0;
                        var cpStat = stat?.FinalStat?.Find(s => s.StatName == "전투력");
                        if (cpStat != null) long.TryParse(cpStat.StatValue, out cp);

                        StatisticsService.RecordCharacterGrowthForDate(
                            today, characterId, characterName,
                            basic.CharacterLevel, 0, expRate, cp,
                            union?.UnionLevel ?? 0, union?.UnionArtifactLevel ?? 0
                        );
                        recordsAdded++;
                    }

                    // 6차 + 헥사
                    var skill6 = await GetCharacterSkillAsync(ocid, dateParam, "6");
                    var hexa = await GetHexaStatAsync(ocid, dateParam);
                    var hexaStat = await GetHexaMatrixStatAsync(ocid, dateParam);

                    SaveRaw(characterName, todayStr, "skill6", (object?)skill6 ?? new { empty = true });
                    SaveRaw(characterName, todayStr, "hexamatrix", (object?)hexa ?? new { empty = true });
                    SaveRaw(characterName, todayStr, "hexastat", (object?)hexaStat ?? new { empty = true });
                    RecordHexaSkillChanges(today, characterId, characterName, skill6);

                    // 장비 + 시드링
                    var item = await GetItemEquipmentAsync(ocid, dateParam);
                    var ring = await GetRingExchangeAsync(ocid, dateParam);

                    SaveRaw(characterName, todayStr, "item", (object?)item ?? new { empty = true });
                    SaveRaw(characterName, todayStr, "ring", (object?)ring ?? new { empty = true });
                }
                catch
                {
                    // 개별 수집 실패 시 무시 (recordsAdded 증가 안함)
                }

                currentStep++;
                ReportProgress();
            }

            // 4. 후처리: 장비 변경 내역 분석
            if (dateList.Any())
            {
                RawDataProcessor.ProcessItemChangesFromRaw(
                    characterId, characterName, dateList.First(), dateList.Last());
            }

            currentStep++;
            ReportProgress();

            result.Success = true;
            result.RecordsAdded = recordsAdded;
            result.Message = $"데이터 수집 및 분석 완료: {recordsAdded}일치";
            return result;
        }

        public async Task<GrowthHistoryResult> CollectGrowthHistoryAsync(
            string ocid, string characterId, string characterName, int daysBack = 30, IProgress<int>? progress = null)
        {
            var dates = Enumerable.Range(1, daysBack).Select(i => DateTime.Now.AddDays(-i)).ToList();
            return await CollectGrowthHistoryBatchAsync(ocid, characterId, characterName, dates, progress);
        }

        public async Task<GrowthHistoryResult> CollectGrowthHistoryForDatesAsync(
            string ocid, string characterId, string characterName, List<DateTime> targetDates, IProgress<int>? progress = null)
        {
            return await CollectGrowthHistoryBatchAsync(ocid, characterId, characterName, targetDates, progress);
        }

        private void RecordHexaSkillChanges(DateTime date, string charId, string charName, CharacterSkillResponse? skill)
        {
            if (skill?.CharacterSkill == null) return;
            var prevSkill = RawDataProcessor.LoadSkill6Info(charName, date.AddDays(-1));

            if (prevSkill?.CharacterSkill != null)
            {
                var prevMap = prevSkill.CharacterSkill.Where(s => !string.IsNullOrEmpty(s.SkillName))
                    .ToDictionary(s => s.SkillName!, s => s.SkillLevel);

                foreach (var curr in skill.CharacterSkill)
                {
                    if (string.IsNullOrEmpty(curr.SkillName)) continue;
                    if (prevMap.TryGetValue(curr.SkillName, out int prevLevel))
                    {
                        if (curr.SkillLevel > prevLevel)
                            StatisticsService.RecordHexaSkillChange(charId, charName, curr.SkillName, prevLevel, curr.SkillLevel, curr.SkillIcon ?? "", date);
                    }
                    else if (curr.SkillLevel > 0)
                    {
                        StatisticsService.RecordHexaSkillChange(charId, charName, curr.SkillName, 0, curr.SkillLevel, curr.SkillIcon ?? "", date);
                    }
                }
            }
        }

        private static List<List<T>> SplitIntoBatches<T>(List<T> source, int batchSize)
        {
            var batches = new List<List<T>>();
            for (int i = 0; i < source.Count; i += batchSize)
                batches.Add(source.Skip(i).Take(batchSize).ToList());
            return batches;
        }

        #endregion
        // Cache Logic
        private static readonly string _cachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MapleHomework", "Cache");

        public string CachePath => _cachePath;

        private static void EnsureCacheDirectory()
        {
            try { if (!Directory.Exists(_cachePath)) Directory.CreateDirectory(_cachePath); } catch { }
        }

        public async Task<CharacterCacheData?> LoadCharacterCache(string ocid)
        {
            EnsureCacheDirectory();
            string filePath = Path.Combine(_cachePath, $"{ocid}.json");
            if (!File.Exists(filePath)) return null;
            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<CharacterCacheData>(json);
            }
            catch { return null; }
        }

        public async Task SaveCharacterCache(string ocid, CharacterCacheData data)
        {
            EnsureCacheDirectory();
            string filePath = Path.Combine(CachePath, $"{ocid}.json");
            try
            {
                string json = JsonSerializer.Serialize(data);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch { }
        }
    }

    public class GrowthHistoryResult
    {
        public bool Success { get; set; }
        public int RecordsAdded { get; set; }
        public string Message { get; set; } = "";
    }
}