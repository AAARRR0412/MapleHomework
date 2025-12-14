using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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

    public class MapleApiService
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
            EnsureRawDirectory();
        }

        private static void EnsureRawDirectory()
        {
            try { if (!Directory.Exists(RawPath)) Directory.CreateDirectory(RawPath); } catch { }
        }

        private static void SaveRaw(string dateStr, string category, object data)
        {
            try
            {
                string file = Path.Combine(RawPath, $"{dateStr}-{category}.json");
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

        // --- 단건 조회 메서드 ---
        public Task<CharacterBasicResponse?> GetCharacterInfoAsync(string ocid, string? date = null)
        {
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return GetJsonAsync<CharacterBasicResponse>($"{BaseUrl}/character/basic?ocid={ocid}&date={date}");
        }
        public Task<SymbolEquipmentResponse?> GetSymbolEquipmentAsync(string ocid, string? date = null)
        {
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return GetJsonAsync<SymbolEquipmentResponse>($"{BaseUrl}/character/symbol-equipment?ocid={ocid}&date={date}");
        }
        public Task<UnionResponse?> GetUnionInfoAsync(string ocid, string? date = null)
        {
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return GetJsonAsync<UnionResponse>($"{BaseUrl}/user/union?ocid={ocid}&date={date}");
        }
        public Task<CharacterStatResponse?> GetCharacterStatAsync(string ocid, string? date = null)
        {
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return GetJsonAsync<CharacterStatResponse>($"{BaseUrl}/character/stat?ocid={ocid}&date={date}");
        }
        public Task<HexaStatResponse?> GetHexaStatAsync(string ocid, string? date = null)
        {
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return GetJsonAsync<HexaStatResponse>($"{BaseUrl}/character/hexamatrix?ocid={ocid}&date={date}");
        }
        public Task<HexaMatrixStatResponse?> GetHexaMatrixStatAsync(string ocid, string? date = null)
        {
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return GetJsonAsync<HexaMatrixStatResponse>($"{BaseUrl}/character/hexamatrix-stat?ocid={ocid}&date={date}");
        }
        public Task<CharacterSkillResponse?> GetCharacterSkillAsync(string ocid, string? date = null, string grade = "6")
        {
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return GetJsonAsync<CharacterSkillResponse>($"{BaseUrl}/character/skill?ocid={ocid}&date={date}&character_skill_grade={grade}");
        }
        public Task<ItemEquipmentResponse?> GetItemEquipmentAsync(string ocid, string? date = null)
        {
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return GetJsonAsync<ItemEquipmentResponse>($"{BaseUrl}/character/item-equipment?ocid={ocid}&date={date}");
        }
        public Task<UnionRaiderResponse?> GetUnionRaiderInfoAsync(string ocid, string? date = null)
        {
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return GetJsonAsync<UnionRaiderResponse>($"{BaseUrl}/user/union-raider?ocid={ocid}&date={date}");
        }
        // [신규] 시드링 교체 슬롯 조회
        public Task<RingExchangeResponse?> GetRingExchangeAsync(string ocid, string? date = null)
        {
            date ??= DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            return GetJsonAsync<RingExchangeResponse>($"{BaseUrl}/character/ring-exchange-skill-equipment?ocid={ocid}&date={date}");
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
            var dateList = targetDates.OrderBy(d => d).ToList();
            var dateStrings = dateList.Select(d => d.ToString("yyyy-MM-dd")).ToList();
            var batches = SplitIntoBatches(dateStrings, BatchSizeLimit);
            
            int totalSteps = batches.Count * 3 + 1; 
            int currentStep = 0;

            void ReportProgress() => progress?.Report((int)((double)currentStep / totalSteps * 100));

            // 1. 기본 정보
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

                        SaveRaw(dateStr, "basic", (object?)basic ?? new { empty = true });
                        SaveRaw(dateStr, "union", (object?)union ?? new { empty = true });
                        SaveRaw(dateStr, "stat", (object?)stat ?? new { empty = true });

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

            // 2. 6차 스킬
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

                        SaveRaw(dateStr, "skill6", (object?)skill ?? new { empty = true });
                        SaveRaw(dateStr, "hexamatrix", (object?)hexa ?? new { empty = true });
                        SaveRaw(dateStr, "hexastat", (object?)hexaStat ?? new { empty = true });
                        RecordHexaSkillChanges(DateTime.Parse(dateStr), characterId, characterName, skill);
                    }
                }
                currentStep++;
                ReportProgress();
                await Task.Delay(100);
            }

            // 3. 장비 정보 (시드링 포함)
            foreach (var batch in batches)
            {
                // Task 생성 시점에 비동기 작업이 시작됩니다 (병렬 실행)
                var taskBatch = BatchCollectAsync(ocid, batch, new List<string> { "item" });
                var ringTasks = batch.Select(d => GetRingExchangeAsync(ocid, d)).ToList();
                
                // 여기서 각각 await를 호출하면 결과가 나올 때까지 기다립니다.
                // taskBatch와 ringTasks는 이미 동시에 실행 중입니다.
                var resp = await taskBatch;
                var ringResults = await Task.WhenAll(ringTasks);

                for (int i = 0; i < batch.Count; i++)
                {
                    string dateStr = batch[i];
                    
                    if (resp?.Data != null)
                    {
                        var item = ExtractFromBatchResponse<ItemEquipmentResponse>(resp, dateStr, "item");
                        SaveRaw(dateStr, "item", (object?)item ?? new { empty = true });
                    }
                    
                    var ring = ringResults[i];
                    SaveRaw(dateStr, "ring", (object?)ring ?? new { empty = true });
                }
                
                currentStep++;
                ReportProgress();
                await Task.Delay(100);
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
            var prevSkill = RawDataProcessor.LoadSkill6Info(date.AddDays(-1));
            
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
    }

    public class GrowthHistoryResult
    {
        public bool Success { get; set; }
        public int RecordsAdded { get; set; }
        public string Message { get; set; } = "";
    }
}