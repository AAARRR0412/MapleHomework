using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using MapleHomework.Models;

namespace MapleHomework.Services
{
    public static class RawDataProcessor
    {
        private static string GetRawPath(string characterName)
        {
            // characterName이 null이거나 비어있으면 기본 경로(하위 호환) or 에러
            // 여기서는 안전하게 기본 경로 + 이름
            string basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "MapleScheduler", "api-raw");

            if (string.IsNullOrEmpty(characterName)) return basePath;
            return Path.Combine(basePath, characterName);
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        #region 로컬 전용 데이터 모델

        // 링 익스체인지 API 응답은 단일 객체 (배열 아님)
        private class LocalRingResponse
        {
            [JsonPropertyName("special_ring_exchange_name")]
            public string? SpecialRingExchangeName { get; set; }

            [JsonPropertyName("special_ring_exchange_level")]
            public int SpecialRingExchangeLevel { get; set; }

            [JsonPropertyName("special_ring_exchange_icon")]
            public string? SpecialRingExchangeIcon { get; set; }

            [JsonPropertyName("special_ring_exchange_description")]
            public string? SpecialRingExchangeDescription { get; set; }

            public bool HasRing => !string.IsNullOrEmpty(SpecialRingExchangeName) && SpecialRingExchangeLevel > 0;
        }

        #endregion

        #region 데이터 로드 및 헬퍼

        public static bool HasDataForDate(string characterName, DateTime date)
        {
            string dateStr = date.ToString("yyyy-MM-dd");
            return File.Exists(Path.Combine(GetRawPath(characterName), $"{dateStr}-basic.json"));
        }

        public static DataCollectionSummary GetDataSummary(string characterName)
        {
            var dates = new HashSet<DateTime>();
            string path = GetRawPath(characterName);
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.GetFiles(path, "*-basic.json"))
                {
                    var dateStr = Path.GetFileNameWithoutExtension(file).Replace("-basic", "");
                    if (DateTime.TryParse(dateStr, out var date)) dates.Add(date.Date);
                }
            }

            return new DataCollectionSummary
            {
                TotalDays = dates.Count,
                OldestDate = dates.Count > 0 ? dates.Min() : null,
                NewestDate = dates.Count > 0 ? dates.Max() : null,
                ExistingDates = dates
            };
        }

        public static ItemEquipmentResponse? LoadItemInfo(string characterName, DateTime date) => LoadRawData<ItemEquipmentResponse>(characterName, date, "item");

        public static RingExchangeResponse? LoadRingInfo(string characterName, DateTime date) => LoadRawData<RingExchangeResponse>(characterName, date, "ring");

        private static LocalRingResponse? LoadLocalRingInfo(string characterName, DateTime date)
        {
            try
            {
                string file = Path.Combine(GetRawPath(characterName), $"{date:yyyy-MM-dd}-ring.json");
                if (!File.Exists(file)) return null;
                string json = File.ReadAllText(file);
                if (json.Contains("\"empty\":true")) return null;
                return JsonSerializer.Deserialize<LocalRingResponse>(json, JsonOptions);
            }
            catch { return null; }
        }

        public static CharacterSkillResponse? LoadSkill6Info(string characterName, DateTime date) => LoadRawData<CharacterSkillResponse>(characterName, date, "skill6");

        public static CharacterSkillResponse? LoadLatestSkill6Info(string characterName) => LoadLatestData<CharacterSkillResponse>(characterName, "skill6");

        public static HexaStatResponse? LoadLatestHexaStatInfo(string characterName) => LoadLatestData<HexaStatResponse>(characterName, "hexamatrix");

        public static HexaMatrixStatResponse? LoadLatestHexaMatrixStatInfo(string characterName) => LoadLatestData<HexaMatrixStatResponse>(characterName, "hexastat");

        private static T? LoadLatestData<T>(string characterName, string category) where T : class
        {
            var summary = GetDataSummary(characterName);
            if (!summary.ExistingDates.Any()) return null;

            // 최신 날짜부터 역순으로 탐색하여 데이터가 존재하는 파일 찾기
            foreach (var date in summary.ExistingDates.OrderByDescending(d => d))
            {
                var data = LoadRawData<T>(characterName, date, category);
                if (data != null) return data;
            }
            return null;
        }

        private static T? LoadRawData<T>(string characterName, DateTime date, string type) where T : class
        {
            try
            {
                string file = Path.Combine(GetRawPath(characterName), $"{date:yyyy-MM-dd}-{type}.json");
                if (!File.Exists(file)) return default;
                string json = File.ReadAllText(file);
                if (json.Contains("\"empty\":true")) return default;
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch { return default; }
        }

        /// <summary>
        /// raw stat 데이터에서 전투력 정보를 다시 로드하여 GrowthRecords 업데이트
        /// </summary>
        public static int RefreshCombatPowerFromRaw(string characterId, string characterName)
        {
            var summary = GetDataSummary(characterName);
            if (!summary.ExistingDates.Any()) return 0;

            int updated = 0;
            foreach (var date in summary.ExistingDates.OrderBy(d => d))
            {
                var stat = LoadRawData<CharacterStatResponse>(characterName, date, "stat");
                var basic = LoadRawData<CharacterBasicResponse>(characterName, date, "basic");

                if (stat?.FinalStat == null || basic == null) continue;

                var cpStat = stat.FinalStat.Find(s => s.StatName == "전투력");
                if (cpStat == null || string.IsNullOrEmpty(cpStat.StatValue)) continue;

                if (!long.TryParse(cpStat.StatValue, out long combatPower) || combatPower <= 0) continue;

                double expRate = 0;
                if (basic.CharacterExpRate != null)
                    double.TryParse(basic.CharacterExpRate.Replace("%", ""), out expRate);

                var union = LoadRawData<UnionResponse>(characterName, date, "union");

                StatisticsService.RecordCharacterGrowthForDate(
                    date, characterId, characterName,
                    basic.CharacterLevel, 0, expRate, combatPower,
                    union?.UnionLevel ?? 0, union?.UnionArtifactLevel ?? 0
                );
                updated++;
            }

            return updated;
        }

        #endregion

        #region 핵심 로직: 아이템 변경 감지 및 기록

        public static void ProcessItemChangesFromRaw(string characterId, string characterName, DateTime startDate, DateTime endDate)
        {
            var allExistingDates = GetDataSummary(characterName).ExistingDates.OrderBy(d => d).ToList();

            var targetDates = allExistingDates
                .Where(d => d >= startDate.Date && d <= endDate.Date && d <= DateTime.Today)
                .ToList();

            if (targetDates.Count == 0) return;

            var seenItemHashes = new HashSet<string>();
            var seenSeedRingHashes = new HashSet<string>(); // 시드링 전용 (슬롯 간 이동 추적)
            var pastDates = allExistingDates.Where(d => d < targetDates[0]).ToList();

            foreach (var pastDate in pastDates)
            {
                var raw = LoadItemInfo(characterName, pastDate);
                var ring = LoadLocalRingInfo(characterName, pastDate);
                if (raw == null) continue;

                var snapshot = new InventorySnapshot(raw, ring);
                foreach (var list in snapshot.ItemsBySlot.Values)
                {
                    foreach (var item in list) seenItemHashes.Add(GenerateItemHash(item));
                }
                // 시드링 해시도 수집 (장비+링익스체인지 통합)
                foreach (var hash in snapshot.AllSeedRingHashes)
                    seenSeedRingHashes.Add(hash);
            }

            InventorySnapshot? prevSnapshot = null;
            var prevDate = targetDates[0].AddDays(-1);
            if (HasDataForDate(characterName, prevDate))
            {
                var prevRaw = LoadItemInfo(characterName, prevDate);
                var prevRing = LoadLocalRingInfo(characterName, prevDate);
                if (prevRaw != null)
                {
                    prevSnapshot = new InventorySnapshot(prevRaw, prevRing);
                    foreach (var list in prevSnapshot.ItemsBySlot.Values)
                        foreach (var item in list) seenItemHashes.Add(GenerateItemHash(item));
                    foreach (var hash in prevSnapshot.AllSeedRingHashes)
                        seenSeedRingHashes.Add(hash);
                }
            }

            foreach (var date in targetDates)
            {
                var currentRaw = LoadItemInfo(characterName, date);
                var currentRing = LoadLocalRingInfo(characterName, date);

                if (currentRaw == null) continue;

                var currentSnapshot = new InventorySnapshot(currentRaw, currentRing);

                if (prevSnapshot != null)
                {
                    DetectAndRecordChanges(characterId, characterName, date, prevSnapshot, currentSnapshot,
                        currentRaw.CharacterClass, seenItemHashes, seenSeedRingHashes);
                }

                foreach (var list in currentSnapshot.ItemsBySlot.Values)
                    foreach (var item in list) seenItemHashes.Add(GenerateItemHash(item));
                foreach (var hash in currentSnapshot.AllSeedRingHashes)
                    seenSeedRingHashes.Add(hash);

                prevSnapshot = currentSnapshot;
            }
        }

        private static void DetectAndRecordChanges(string charId, string charName, DateTime date,
            InventorySnapshot prev, InventorySnapshot curr, string? charClass,
            HashSet<string> seenHashes, HashSet<string> seenSeedRingHashes)
        {
            var allSlots = prev.ItemsBySlot.Keys.Union(curr.ItemsBySlot.Keys).ToList();

            foreach (var slot in allSlots)
            {
                var prevItems = prev.ItemsBySlot.ContainsKey(slot) ? new List<ItemEquipmentInfo>(prev.ItemsBySlot[slot]) : new List<ItemEquipmentInfo>();
                var currItems = curr.ItemsBySlot.ContainsKey(slot) ? new List<ItemEquipmentInfo>(curr.ItemsBySlot[slot]) : new List<ItemEquipmentInfo>();

                for (int i = currItems.Count - 1; i >= 0; i--)
                {
                    var matchIndex = prevItems.FindIndex(p => IsExactMatch(p, currItems[i]));
                    if (matchIndex != -1)
                    {
                        currItems.RemoveAt(i);
                        prevItems.RemoveAt(matchIndex);
                    }
                }

                foreach (var newItem in currItems)
                {
                    // 시드링은 슬롯 간 이동 (장비 <-> 링 익스체인지) 시 신규로 감지되지 않도록 처리
                    if (IsSeedRing(newItem))
                    {
                        string seedHash = GenerateSeedRingHash(newItem);

                        // 이전 스냅샷의 전체 시드링 중에 같은 해시가 있으면 슬롯 간 이동으로 판단
                        if (prev.AllSeedRingHashes.Contains(seedHash))
                        {
                            continue; // 슬롯 간 이동은 변경으로 기록하지 않음
                        }

                        // 과거 데이터에서 이미 본 시드링이면 skip
                        if (seenSeedRingHashes.Contains(seedHash))
                        {
                            continue;
                        }

                        // 시드링 레벨 변경 감지 (같은 이름, 다른 레벨)
                        var sameName = prevItems.Find(p => IsSeedRing(p) && NormalizeName(p.ItemName) == NormalizeName(newItem.ItemName));
                        if (sameName != null)
                        {
                            var changes = CompareItemOptions(sameName, newItem);
                            if (changes.Any())
                            {
                                string summary = GetChangeSummary(changes);
                                string json = SerializeItem(newItem, charClass);
                                string changeJson = JsonSerializer.Serialize(changes);

                                StatisticsService.RecordItemChange(charId, charName, slot,
                                    sameName.ItemName!, newItem.ItemName!, "옵션 변경",
                                    json, date, summary, changeJson, newItem.ItemIcon ?? "");
                            }
                            prevItems.Remove(sameName);
                            continue;
                        }

                        // 진짜 신규 시드링
                        string type = "장착";
                        string oldName = "없음";
                        string summaryNew = "신규 장착";
                        var changesNew = CompareItemOptions(null, newItem);
                        string changeJsonNew = JsonSerializer.Serialize(changesNew);
                        string jsonNew = SerializeItem(newItem, charClass);

                        StatisticsService.RecordItemChange(charId, charName, slot,
                            oldName, newItem.ItemName!, type,
                            jsonNew, date, summaryNew, changeJsonNew, newItem.ItemIcon ?? "");
                        continue;
                    }

                    var bestMatch = FindBestMatch(newItem, prevItems);

                    if (bestMatch != null)
                    {
                        if (!IsSpiritPendant(newItem.ItemName))
                        {
                            var changes = CompareItemOptions(bestMatch, newItem);
                            if (changes.Any())
                            {
                                string summary = GetChangeSummary(changes);
                                string json = SerializeItem(newItem, charClass);
                                string changeJson = JsonSerializer.Serialize(changes);

                                StatisticsService.RecordItemChange(charId, charName, slot,
                                    bestMatch.ItemName!, newItem.ItemName!, "옵션 변경",
                                    json, date, summary, changeJson, newItem.ItemIcon ?? "");
                            }
                        }
                        prevItems.Remove(bestMatch);
                    }
                    else
                    {
                        if (!IsSpiritPendant(newItem.ItemName))
                        {
                            string type = "장착";
                            string oldName = "없음";

                            if (prevItems.Count > 0)
                            {
                                type = "교체";
                                var oldItem = prevItems[0];
                                oldName = oldItem.ItemName ?? "알 수 없음";
                            }
                            else
                            {
                                string currentHash = GenerateItemHash(newItem);
                                if (seenHashes.Contains(currentHash))
                                {
                                    continue;
                                }
                            }

                            string summary = type == "교체" ? $"{oldName} → {newItem.ItemName}" : "신규 장착";
                            var changes = CompareItemOptions(null, newItem);
                            string changeJson = JsonSerializer.Serialize(changes);
                            string json = SerializeItem(newItem, charClass);

                            StatisticsService.RecordItemChange(charId, charName, slot,
                                oldName, newItem.ItemName!, type,
                                json, date, summary, changeJson, newItem.ItemIcon ?? "");
                        }
                    }
                }
            }
        }

        #endregion


        #region 핵심 로직: 6차 스킬 변경 감지 및 기록

        public static void ProcessHexaSkillChangesFromRaw(string characterId, string characterName, DateTime startDate, DateTime endDate)
        {
            var allExistingDates = GetDataSummary(characterName).ExistingDates.OrderBy(d => d).ToList();
            var targetDates = allExistingDates
                .Where(d => d >= startDate.Date && d <= endDate.Date && d <= DateTime.Today)
                .ToList();

            if (targetDates.Count == 0) return;

            // 이전 날짜 데이터 로드 (시작일 하루 전)
            Dictionary<string, int> prevSkills = new Dictionary<string, int>();
            DateTime prevDate = targetDates[0].AddDays(-1);

            // 이전 기록이 존재하는 가장 가까운 날짜 찾기
            // (하루 전이 없으면 더 과거로 검색)
            var pastDates = allExistingDates.Where(d => d < targetDates[0]).OrderByDescending(d => d).ToList();
            if (pastDates.Any())
            {
                var pastData = LoadSkill6Info(characterName, pastDates.First());
                if (pastData?.CharacterSkill != null)
                {
                    foreach (var skill in pastData.CharacterSkill)
                    {
                        if (!string.IsNullOrEmpty(skill.SkillName))
                        {
                            prevSkills[skill.SkillName] = skill.SkillLevel;
                        }
                    }
                }
            }

            foreach (var date in targetDates)
            {
                var currentData = LoadSkill6Info(characterName, date);
                if (currentData?.CharacterSkill == null) continue;

                var currentSkills = new Dictionary<string, int>();

                foreach (var skill in currentData.CharacterSkill)
                {
                    if (string.IsNullOrEmpty(skill.SkillName)) continue;

                    string name = skill.SkillName;
                    int level = skill.SkillLevel;
                    string icon = skill.SkillIcon ?? "";

                    currentSkills[name] = level;

                    // 이전 기록과 비교
                    if (prevSkills.TryGetValue(name, out int oldLevel))
                    {
                        if (level > oldLevel)
                        {
                            // 레벨 상승 감지
                            StatisticsService.RecordHexaSkillChange(characterId, characterName, name, oldLevel, level, icon, date);
                        }
                    }
                    else
                    {
                        // 신규 스킬 습득 (1레벨 이상일 때만)
                        if (level > 0)
                        {
                            StatisticsService.RecordHexaSkillChange(characterId, characterName, name, 0, level, icon, date);
                        }
                    }
                }

                // 다음 날짜 비교를 위해 현재 상태를 이전 상태로 업데이트
                // (단, 현재 날짜에 데이터가 있었던 스킬들만 갱신하거나, 전체를 갱신)
                // 사라진 스킬은 없다고 가정 (스킬 초기화는 고려하지 않음 -> 레벨 0됨)
                foreach (var kvp in currentSkills)
                {
                    prevSkills[kvp.Key] = kvp.Value;
                }
            }
        }

        #endregion

        #region 매칭 및 비교 알고리즘

        private static bool IsSeedRing(ItemEquipmentInfo? item)
        {
            if (item == null) return false;
            if (GetSpecialRingLevel(item) > 0) return true;

            var name = NormalizeName(item.ItemName);
            if (string.IsNullOrEmpty(name)) return false;

            return name.Contains("리스트레인트") || name.Contains("웨폰퍼프") ||
                   name.Contains("리스크테이커") || name.Contains("크라이시스") ||
                   name.Contains("링 오브 썸") || name.Contains("오버패스") ||
                   name.Contains("얼티메이텀") || name.Contains("헬스컷") ||
                   name.Contains("리밋 브레이커") || name.Contains("마나컷") ||
                   name.Contains("듀라빌리티") || name.Contains("맥스") ||
                   name.Contains("크리디펜스") || name.Contains("크리쉬프트") ||
                   name.Contains("스탠스 쉬프트") || name.Contains("레벨퍼프") ||
                   name.Contains("타워인핸스") || name.Contains("컨티뉴어스");
        }

        private static string NormalizeName(string? name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            return Regex.Replace(name, @"\s*\d+레벨$", "").Trim();
        }

        private static string GenerateItemHash(ItemEquipmentInfo item)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(NormalizeName(item.ItemName));

            if (IsSeedRing(item))
            {
                sb.Append($"|SeedRing|Lv{GetSpecialRingLevel(item)}");
                return sb.ToString();
            }

            sb.Append($"|{item.Starforce}");
            sb.Append($"|{item.PotentialOptionGrade}");
            sb.Append($"|{item.AdditionalPotentialOptionGrade}");
            sb.Append($"|{item.PotentialOption1}|{item.PotentialOption2}|{item.PotentialOption3}");
            sb.Append($"|{item.AdditionalPotentialOption1}|{item.AdditionalPotentialOption2}|{item.AdditionalPotentialOption3}");
            AppendOptionHash(sb, item.ItemAddOption);
            AppendOptionHash(sb, item.ItemEtcOption);
            AppendOptionHash(sb, item.ItemStarforceOption);

            return sb.ToString();
        }

        private static void AppendOptionHash(System.Text.StringBuilder sb, ItemOptionInfo? opt)
        {
            sb.Append('|').Append(ParseInt(opt?.Str));
            sb.Append('|').Append(ParseInt(opt?.Dex));
            sb.Append('|').Append(ParseInt(opt?.Int));
            sb.Append('|').Append(ParseInt(opt?.Luk));
            sb.Append('|').Append(ParseInt(opt?.AttackPower));
            sb.Append('|').Append(ParseInt(opt?.MagicPower));
        }

        private static ItemEquipmentInfo? FindBestMatch(ItemEquipmentInfo target, List<ItemEquipmentInfo> candidates)
        {
            string targetName = NormalizeName(target.ItemName);
            var sameNameCandidates = candidates
                .Where(c => NormalizeName(c.ItemName) == targetName)
                .ToList();

            if (sameNameCandidates.Count == 0) return null;
            if (sameNameCandidates.Count == 1) return sameNameCandidates[0];

            return sameNameCandidates.OrderByDescending(c => CalculateSimilarityScore(target, c)).First();
        }

        private static int CalculateSimilarityScore(ItemEquipmentInfo a, ItemEquipmentInfo b)
        {
            if (IsSeedRing(a) && IsSeedRing(b))
            {
                return GetSpecialRingLevel(a) == GetSpecialRingLevel(b) ? 100 : 0;
            }

            int score = 0;
            if (IsOptionEqual(a.ItemAddOption, b.ItemAddOption)) score += 40;
            if (IsPotEqual(a, b)) score += 30;
            if (IsAddPotEqual(a, b)) score += 20;
            if (a.Starforce == b.Starforce) score += 10;
            if (a.PotentialOptionGrade == b.PotentialOptionGrade) score += 5;
            return score;
        }

        private static bool IsExactMatch(ItemEquipmentInfo a, ItemEquipmentInfo b)
        {
            if (NormalizeName(a.ItemName) != NormalizeName(b.ItemName)) return false;

            if (IsSeedRing(a) || IsSeedRing(b))
            {
                return GetSpecialRingLevel(a) == GetSpecialRingLevel(b);
            }

            if (a.Starforce != b.Starforce) return false;
            if (a.PotentialOptionGrade != b.PotentialOptionGrade) return false;
            if (a.AdditionalPotentialOptionGrade != b.AdditionalPotentialOptionGrade) return false;
            if (!IsOptionEqual(a.ItemBaseOption, b.ItemBaseOption)) return false;
            if (!IsOptionEqual(a.ItemAddOption, b.ItemAddOption)) return false;
            if (!IsOptionEqual(a.ItemEtcOption, b.ItemEtcOption)) return false;
            if (!IsOptionEqual(a.ItemStarforceOption, b.ItemStarforceOption)) return false;
            if (a.PotentialOption1 != b.PotentialOption1) return false;
            if (a.PotentialOption2 != b.PotentialOption2) return false;
            if (a.PotentialOption3 != b.PotentialOption3) return false;
            if (a.AdditionalPotentialOption1 != b.AdditionalPotentialOption1) return false;
            if (a.AdditionalPotentialOption2 != b.AdditionalPotentialOption2) return false;
            if (a.AdditionalPotentialOption3 != b.AdditionalPotentialOption3) return false;
            if (a.SoulName != b.SoulName) return false;
            if (a.SoulOption != b.SoulOption) return false;

            return true;
        }

        public static List<ItemOptionChange> CompareItemOptions(ItemEquipmentInfo? oldItem, ItemEquipmentInfo newItem)
        {
            var changes = new List<ItemOptionChange>();

            if (oldItem == null)
            {
                changes.Add(new ItemOptionChange { ChangeType = ItemOptionChangeType.NewItem, Description = "신규 장착" });

                // 시드링이 아닌 경우 모든 옵션 표시
                if (GetSpecialRingLevel(newItem) < 1)
                {
                    // 스타포스
                    if (newItem.Starforce != "0" && !string.IsNullOrEmpty(newItem.Starforce))
                        changes.Add(new ItemOptionChange
                        {
                            ChangeType = ItemOptionChangeType.Starforce,
                            Category = "스타포스",
                            NewValue = $"{newItem.Starforce}성"
                            // Description 없이 NewValue만 표시
                        });

                    // 잠재능력
                    if (!string.IsNullOrEmpty(newItem.PotentialOptionGrade) && newItem.PotentialOptionGrade != "없음")
                    {
                        var potLines = GetPotLines(newItem);
                        changes.Add(new ItemOptionChange
                        {
                            ChangeType = ItemOptionChangeType.Potential,
                            Category = "잠재 옵션",
                            NewValue = newItem.PotentialOptionGrade,
                            Details = potLines
                        });
                    }

                    // 에디셔널 잠재능력
                    if (!string.IsNullOrEmpty(newItem.AdditionalPotentialOptionGrade) && newItem.AdditionalPotentialOptionGrade != "없음")
                    {
                        var addPotLines = GetAddPotLines(newItem);
                        changes.Add(new ItemOptionChange
                        {
                            ChangeType = ItemOptionChangeType.AdditionalPotential,
                            Category = "에디 옵션",
                            NewValue = newItem.AdditionalPotentialOptionGrade,
                            Details = addPotLines
                        });
                    }

                    // 주문서 강화
                    int scrollUp = ParseInt(newItem.ScrollUpgrade);
                    if (scrollUp > 0)
                    {
                        changes.Add(new ItemOptionChange
                        {
                            ChangeType = ItemOptionChangeType.Scroll,
                            Category = "주문서",
                            NewValue = $"{scrollUp}회",
                            Description = $"주문서 강화 {scrollUp}회"
                        });
                    }

                    // 소울
                    if (!string.IsNullOrEmpty(newItem.SoulName))
                    {
                        changes.Add(new ItemOptionChange
                        {
                            ChangeType = ItemOptionChangeType.Soul,
                            Category = "소울",
                            NewValue = newItem.SoulName,
                            Description = newItem.SoulName
                        });
                    }
                }
                return changes;
            }

            bool isOldSeed = IsSeedRing(oldItem);
            bool isNewSeed = IsSeedRing(newItem);

            if (isOldSeed || isNewSeed)
            {
                int oldRingLv = GetSpecialRingLevel(oldItem);
                int newRingLv = GetSpecialRingLevel(newItem);

                if (oldRingLv > 0 && newRingLv > 0 && oldRingLv != newRingLv)
                {
                    changes.Add(new ItemOptionChange
                    {
                        ChangeType = ItemOptionChangeType.Option,
                        Category = "링 레벨",
                        OldValue = $"{oldRingLv}레벨",
                        NewValue = $"{newRingLv}레벨",
                        Description = $"스킬 레벨: {oldRingLv} → {newRingLv}"
                    });
                }
                return changes;
            }

            if (oldItem.Starforce != newItem.Starforce)
            {
                changes.Add(new ItemOptionChange
                {
                    ChangeType = ItemOptionChangeType.Starforce,
                    Category = "스타포스",
                    OldValue = $"{oldItem.Starforce}성",
                    NewValue = $"{newItem.Starforce}성",
                    Description = $"{oldItem.Starforce}성 → {newItem.Starforce}성"
                });
            }

            if (oldItem.PotentialOptionGrade != newItem.PotentialOptionGrade || !IsPotEqual(oldItem, newItem))
            {
                // 잠재 옵션: 변경 전 표시 없이 새 옵션 3줄 전체 표시
                var newPotLines = GetPotLines(newItem);
                if (newPotLines.Any() || oldItem.PotentialOptionGrade != newItem.PotentialOptionGrade)
                {
                    changes.Add(new ItemOptionChange
                    {
                        ChangeType = oldItem.PotentialOptionGrade != newItem.PotentialOptionGrade
                            ? ItemOptionChangeType.Potential
                            : ItemOptionChangeType.PotentialOption,
                        Category = "잠재 옵션",
                        NewValue = newItem.PotentialOptionGrade ?? "없음",
                        Details = newPotLines // 새 옵션 3줄 전체
                    });
                }
            }

            if (oldItem.AdditionalPotentialOptionGrade != newItem.AdditionalPotentialOptionGrade || !IsAddPotEqual(oldItem, newItem))
            {
                // 에디셔널 잠재 옵션: 변경 전 표시 없이 새 옵션 3줄 전체 표시
                var newAddPotLines = GetAddPotLines(newItem);
                if (newAddPotLines.Any() || oldItem.AdditionalPotentialOptionGrade != newItem.AdditionalPotentialOptionGrade)
                {
                    changes.Add(new ItemOptionChange
                    {
                        ChangeType = oldItem.AdditionalPotentialOptionGrade != newItem.AdditionalPotentialOptionGrade
                            ? ItemOptionChangeType.AdditionalPotential
                            : ItemOptionChangeType.AdditionalPotentialOption,
                        Category = "에디 옵션",
                        NewValue = newItem.AdditionalPotentialOptionGrade ?? "없음",
                        Details = newAddPotLines // 새 옵션 3줄 전체
                    });
                }
            }

            if (!IsOptionEqual(oldItem.ItemAddOption, newItem.ItemAddOption))
            {
                var diffs = GetOptionDiffs(oldItem.ItemAddOption, newItem.ItemAddOption);
                if (diffs.Any())
                {
                    changes.Add(new ItemOptionChange
                    {
                        ChangeType = ItemOptionChangeType.AddOption,
                        Category = "추가 옵션",
                        Details = diffs
                    });
                }
            }

            if (oldItem.ScrollUpgrade != newItem.ScrollUpgrade || !IsOptionEqual(oldItem.ItemEtcOption, newItem.ItemEtcOption))
            {
                var diffs = GetOptionDiffs(oldItem.ItemEtcOption, newItem.ItemEtcOption);
                string desc = "주문서 강화";
                if (oldItem.ScrollUpgrade != newItem.ScrollUpgrade)
                    desc += $" ({oldItem.ScrollUpgrade}회 → {newItem.ScrollUpgrade}회)";

                changes.Add(new ItemOptionChange
                {
                    ChangeType = ItemOptionChangeType.Scroll,
                    Category = "주문서",
                    Description = desc,
                    Details = diffs
                });
            }

            if (oldItem.SoulName != newItem.SoulName || oldItem.SoulOption != newItem.SoulOption)
            {
                changes.Add(new ItemOptionChange
                {
                    ChangeType = ItemOptionChangeType.Soul,
                    Category = "소울",
                    OldValue = oldItem.SoulName ?? "없음",
                    NewValue = newItem.SoulName ?? "없음",
                    Description = $"{oldItem.SoulName} → {newItem.SoulName}"
                });
            }

            return changes;
        }

        private static bool IsOptionEqual(ItemOptionInfo? a, ItemOptionInfo? b)
        {
            if (a == null && b == null) return true;
            if (a == null) return IsAllZero(b);
            if (b == null) return IsAllZero(a);

            return ParseInt(a.Str) == ParseInt(b.Str) &&
                   ParseInt(a.Dex) == ParseInt(b.Dex) &&
                   ParseInt(a.Int) == ParseInt(b.Int) &&
                   ParseInt(a.Luk) == ParseInt(b.Luk) &&
                   ParseInt(a.AttackPower) == ParseInt(b.AttackPower) &&
                   ParseInt(a.MagicPower) == ParseInt(b.MagicPower) &&
                   ParseInt(a.BossDamage) == ParseInt(b.BossDamage) &&
                   ParseInt(a.IgnoreMonsterArmor) == ParseInt(b.IgnoreMonsterArmor) &&
                   ParseInt(a.AllStat) == ParseInt(b.AllStat) &&
                   ParseInt(a.Damage) == ParseInt(b.Damage) &&
                   ParseInt(a.MaxHp) == ParseInt(b.MaxHp) &&
                   ParseInt(a.MaxMp) == ParseInt(b.MaxMp);
        }

        private static bool IsAllZero(ItemOptionInfo? opt)
        {
            if (opt == null) return true;
            return ParseInt(opt.Str) == 0 && ParseInt(opt.Dex) == 0 &&
                   ParseInt(opt.Int) == 0 && ParseInt(opt.Luk) == 0 &&
                   ParseInt(opt.AttackPower) == 0 && ParseInt(opt.MagicPower) == 0;
        }

        private static bool IsPotEqual(ItemEquipmentInfo a, ItemEquipmentInfo b)
            => a.PotentialOption1 == b.PotentialOption1 && a.PotentialOption2 == b.PotentialOption2 && a.PotentialOption3 == b.PotentialOption3;

        private static bool IsAddPotEqual(ItemEquipmentInfo a, ItemEquipmentInfo b)
            => a.AdditionalPotentialOption1 == b.AdditionalPotentialOption1 && a.AdditionalPotentialOption2 == b.AdditionalPotentialOption2 && a.AdditionalPotentialOption3 == b.AdditionalPotentialOption3;

        #endregion

        #region 유틸리티 메서드

        private class InventorySnapshot
        {
            public Dictionary<string, List<ItemEquipmentInfo>> ItemsBySlot { get; private set; } = new();

            // 시드링 해시 목록 (장비 슬롯 + 링 익스체인지 슬롯 통합)
            public HashSet<string> AllSeedRingHashes { get; private set; } = new();

            public InventorySnapshot(ItemEquipmentResponse raw, LocalRingResponse? ringRaw = null)
            {
                var allItems = new List<ItemEquipmentInfo>();
                if (raw.ItemEquipment != null) allItems.AddRange(raw.ItemEquipment);
                if (raw.ItemEquipmentPreset1 != null) allItems.AddRange(raw.ItemEquipmentPreset1);
                if (raw.ItemEquipmentPreset2 != null) allItems.AddRange(raw.ItemEquipmentPreset2);
                if (raw.ItemEquipmentPreset3 != null) allItems.AddRange(raw.ItemEquipmentPreset3);

                // 링 익스체인지 슬롯의 시드링 추가 (단일 객체)
                if (ringRaw != null && ringRaw.HasRing)
                {
                    var converted = new ItemEquipmentInfo
                    {
                        ItemName = ringRaw.SpecialRingExchangeName,
                        SpecialRingLevel = WrapIntToJsonElement(ringRaw.SpecialRingExchangeLevel),
                        ItemIcon = ringRaw.SpecialRingExchangeIcon,
                        ItemDescription = ringRaw.SpecialRingExchangeDescription,
                        ItemEquipmentSlot = "반지",
                        ItemAddOption = null,
                        ItemBaseOption = null,
                        ItemEtcOption = null,
                        ItemStarforceOption = null,
                        Starforce = "0",
                        ScrollUpgrade = "0"
                    };
                    allItems.Add(converted);
                }

                foreach (var item in allItems)
                {
                    string slot = item.ItemEquipmentSlot ?? "";
                    if (slot.Contains("반지") || slot.Contains("Ring")) slot = "반지";
                    if (slot.Contains("펜던트") || slot.Contains("Pendant")) slot = "펜던트";

                    if (string.IsNullOrEmpty(slot)) continue;

                    if (!ItemsBySlot.ContainsKey(slot)) ItemsBySlot[slot] = new List<ItemEquipmentInfo>();
                    if (!ItemsBySlot[slot].Any(existing => IsExactMatch(existing, item)))
                    {
                        ItemsBySlot[slot].Add(item);
                    }

                    // 시드링 해시 수집 (장비/링익스체인지 슬롯 간 이동 추적용)
                    if (IsSeedRing(item))
                    {
                        AllSeedRingHashes.Add(GenerateSeedRingHash(item));
                    }
                }
            }

            private System.Text.Json.JsonElement WrapIntToJsonElement(int value)
            {
                var json = $"{{\"val\":{value}}}";
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("val").Clone();
            }
        }

        // 시드링 전용 해시 (이름 + 레벨만으로 동일성 판단)
        private static string GenerateSeedRingHash(ItemEquipmentInfo item)
        {
            return $"{NormalizeName(item.ItemName)}|SeedRing|Lv{GetSpecialRingLevel(item)}";
        }

        private static int GetSpecialRingLevel(ItemEquipmentInfo item)
        {
            if (item.SpecialRingLevel.HasValue)
            {
                try
                {
                    var element = item.SpecialRingLevel.Value;
                    if (element.ValueKind == JsonValueKind.Number) return element.GetInt32();
                    if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out int v)) return v;
                }
                catch { }
            }
            if (!string.IsNullOrEmpty(item.ItemName))
            {
                var match = Regex.Match(item.ItemName, @"\s*(\d+)레벨$");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int level)) return level;
            }
            return 0;
        }

        private static string GetChangeSummary(List<ItemOptionChange> changes)
        {
            var summaries = changes.Select(c => c.Category).Distinct().ToList();
            if (summaries.Count > 2) return $"{summaries[0]} 외 {summaries.Count - 1}건";
            return string.Join(", ", summaries);
        }

        private static bool IsSpiritPendant(string? name) => name != null && name.Contains("정령의 펜던트");

        private static List<string> GetPotLines(ItemEquipmentInfo item)
        {
            var list = new List<string>();
            if (!string.IsNullOrEmpty(item.PotentialOption1)) list.Add(item.PotentialOption1);
            if (!string.IsNullOrEmpty(item.PotentialOption2)) list.Add(item.PotentialOption2);
            if (!string.IsNullOrEmpty(item.PotentialOption3)) list.Add(item.PotentialOption3);
            return list;
        }

        private static List<string> GetAddPotLines(ItemEquipmentInfo item)
        {
            var list = new List<string>();
            if (!string.IsNullOrEmpty(item.AdditionalPotentialOption1)) list.Add(item.AdditionalPotentialOption1);
            if (!string.IsNullOrEmpty(item.AdditionalPotentialOption2)) list.Add(item.AdditionalPotentialOption2);
            if (!string.IsNullOrEmpty(item.AdditionalPotentialOption3)) list.Add(item.AdditionalPotentialOption3);
            return list;
        }

        private static List<string> CompareStringLines(List<string> oldLines, List<string> newLines)
        {
            var res = new List<string>();
            int max = Math.Max(oldLines.Count, newLines.Count);
            for (int i = 0; i < max; i++)
            {
                string o = i < oldLines.Count ? oldLines[i] : "";
                string n = i < newLines.Count ? newLines[i] : "";
                if (o != n)
                {
                    if (string.IsNullOrEmpty(o)) res.Add($"+ {n}");
                    else if (string.IsNullOrEmpty(n)) res.Add($"- {o}");
                    else res.Add($"{o} → {n}");
                }
            }
            return res;
        }

        private static List<string> GetOptionDiffs(ItemOptionInfo? oldOpt, ItemOptionInfo? newOpt)
        {
            var diffs = new List<string>();
            if (oldOpt == null && newOpt == null) return diffs;

            CheckStat(diffs, "STR", oldOpt?.Str, newOpt?.Str);
            CheckStat(diffs, "DEX", oldOpt?.Dex, newOpt?.Dex);
            CheckStat(diffs, "INT", oldOpt?.Int, newOpt?.Int);
            CheckStat(diffs, "LUK", oldOpt?.Luk, newOpt?.Luk);
            CheckStat(diffs, "공격력", oldOpt?.AttackPower, newOpt?.AttackPower);
            CheckStat(diffs, "마력", oldOpt?.MagicPower, newOpt?.MagicPower);
            CheckStat(diffs, "올스탯%", oldOpt?.AllStat, newOpt?.AllStat);

            return diffs;
        }

        private static void CheckStat(List<string> diffs, string name, string? oldVal, string? newVal)
        {
            int o = ParseInt(oldVal);
            int n = ParseInt(newVal);
            if (o != n) diffs.Add($"{name} {o} → {n}");
        }

        private static int ParseInt(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            return int.TryParse(s.Replace(",", "").Replace("%", ""), out int v) ? v : 0;
        }

        private static string SerializeItem(ItemEquipmentInfo item, string? charClass)
        {
            var json = JsonSerializer.Serialize(item);
            if (!string.IsNullOrEmpty(charClass))
            {
                var options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All)
                };

                using var doc = JsonDocument.Parse(json);
                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = options.Encoder });
                writer.WriteStartObject();
                writer.WriteString("character_class", charClass);
                foreach (var prop in doc.RootElement.EnumerateObject()) prop.WriteTo(writer);
                writer.WriteEndObject();
                writer.Flush();
                return System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }
            return json;
        }

        #endregion
    }

    // --- Data Models (유지) ---
    public class DataCollectionSummary { public int TotalDays { get; set; } public DateTime? OldestDate { get; set; } public DateTime? NewestDate { get; set; } public HashSet<DateTime> ExistingDates { get; set; } = new(); }
    public enum ItemOptionChangeType { NewItem, Replace, Starforce, Potential, PotentialOption, AdditionalPotential, AdditionalPotentialOption, AddOption, Scroll, ScrollOption, Soul, Option }
    public class ItemOptionChange
    {
        public ItemOptionChangeType ChangeType { get; set; }
        public string Category { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> Details { get; set; } = new();

        // DisplayText: 신규 아이템일 때는 "→" 없이 현재 옵션만 표시
        public string DisplayText
        {
            get
            {
                if (!string.IsNullOrEmpty(Description))
                    return Description;
                if (Details.Any())
                    return string.Join("\n", Details);
                // OldValue가 없으면 NewValue만 표시 (→ 없이)
                if (string.IsNullOrEmpty(OldValue))
                    return NewValue;
                return $"{OldValue} → {NewValue}";
            }
        }

        public string CategoryIcon => ChangeType switch
        {
            ItemOptionChangeType.NewItem => "✨",
            ItemOptionChangeType.Replace => "🔄",
            ItemOptionChangeType.Starforce => "⭐",
            ItemOptionChangeType.Potential or ItemOptionChangeType.PotentialOption => "💎",
            ItemOptionChangeType.AdditionalPotential or ItemOptionChangeType.AdditionalPotentialOption => "💠",
            ItemOptionChangeType.AddOption => "➕",
            ItemOptionChangeType.Scroll or ItemOptionChangeType.ScrollOption => "📜",
            ItemOptionChangeType.Soul => "👻",
            _ => "•"
        };

        public bool IsNewItem => ChangeType == ItemOptionChangeType.NewItem;
        public bool HasDetails => Details.Any();
    }
}