using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MapleHomework.Models;

namespace MapleHomework.Services
{
    /// <summary>
    /// 원본 API 데이터 처리를 위한 서비스
    /// raw_api/ 폴더에서 원본 데이터를 읽어 실시간으로 가공
    /// </summary>
    public static class RawDataProcessor
    {
        private static readonly string RawPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MapleHomework", "api-raw");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        #region 데이터 존재 여부 확인

        /// <summary>
        /// 특정 날짜에 데이터가 존재하는지 확인
        /// </summary>
        public static bool HasDataForDate(DateTime date)
        {
            string dateStr = date.ToString("yyyy-MM-dd");
            string basicFile = Path.Combine(RawPath, $"{dateStr}-basic.json");
            return File.Exists(basicFile);
        }

        /// <summary>
        /// 수집된 날짜 목록 반환 (캘린더용)
        /// </summary>
        public static HashSet<DateTime> GetCollectedDates()
        {
            return GetDatesWithData();
        }
        
        /// <summary>
        /// 데이터가 존재하는 날짜 목록 반환
        /// </summary>
        public static HashSet<DateTime> GetDatesWithData()
        {
            var dates = new HashSet<DateTime>();
            
            if (!Directory.Exists(RawPath))
                return dates;

            foreach (var file in Directory.GetFiles(RawPath, "*-basic.json"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                // fileName: "2024-12-01-basic"
                var dateStr = fileName.Replace("-basic", "");
                if (DateTime.TryParse(dateStr, out var date))
                {
                    dates.Add(date.Date);
                }
            }

            return dates;
        }

        /// <summary>
        /// 특정 기간 내 데이터 없는 날짜 목록 반환
        /// </summary>
        public static List<DateTime> GetMissingDates(DateTime startDate, DateTime endDate)
        {
            var existingDates = GetDatesWithData();
            var missingDates = new List<DateTime>();

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                // 미래 날짜 제외
                if (date > DateTime.Today)
                    continue;
                    
                if (!existingDates.Contains(date))
                {
                    missingDates.Add(date);
                }
            }

            return missingDates.OrderBy(d => d).ToList();
        }

        /// <summary>
        /// 데이터 현황 요약 (캘린더 표시용)
        /// </summary>
        public static DataCollectionSummary GetDataSummary()
        {
            var existingDates = GetDatesWithData();
            
            return new DataCollectionSummary
            {
                TotalDays = existingDates.Count,
                OldestDate = existingDates.Any() ? existingDates.Min() : null,
                NewestDate = existingDates.Any() ? existingDates.Max() : null,
                ExistingDates = existingDates
            };
        }

        #endregion

        #region 원본 데이터 로드

        /// <summary>
        /// 특정 날짜의 기본 정보 로드
        /// </summary>
        public static CharacterBasicResponse? LoadBasicInfo(DateTime date)
        {
            return LoadRawData<CharacterBasicResponse>(date, "basic");
        }

        /// <summary>
        /// 특정 날짜의 유니온 정보 로드
        /// </summary>
        public static UnionResponse? LoadUnionInfo(DateTime date)
        {
            return LoadRawData<UnionResponse>(date, "union");
        }

        /// <summary>
        /// 특정 날짜의 스탯 정보 로드
        /// </summary>
        public static CharacterStatResponse? LoadStatInfo(DateTime date)
        {
            return LoadRawData<CharacterStatResponse>(date, "stat");
        }

        /// <summary>
        /// 특정 날짜의 장비 정보 로드
        /// </summary>
        public static ItemEquipmentResponse? LoadItemInfo(DateTime date)
        {
            return LoadRawData<ItemEquipmentResponse>(date, "item");
        }

        /// <summary>
        /// 특정 날짜의 6차 스킬 정보 로드
        /// </summary>
        public static CharacterSkillResponse? LoadSkill6Info(DateTime date)
        {
            return LoadRawData<CharacterSkillResponse>(date, "skill6");
        }

        /// <summary>
        /// 특정 날짜의 헥사 스탯 정보 로드
        /// </summary>
        public static HexaStatResponse? LoadHexaStatInfo(DateTime date)
        {
            return LoadRawData<HexaStatResponse>(date, "hexamatrix");
        }

        /// <summary>
        /// 가장 최근 날짜의 헥사 스탯 정보 로드
        /// </summary>
        public static HexaStatResponse? LoadLatestHexaStatInfo()
        {
            var dates = GetDatesWithData();
            if (!dates.Any())
                return null;

            var latestDate = dates.Max();
            return LoadHexaStatInfo(latestDate);
        }

        /// <summary>
        /// 가장 최근 날짜의 6차 스킬 정보 로드
        /// </summary>
        public static CharacterSkillResponse? LoadLatestSkill6Info()
        {
            var dates = GetDatesWithData();
            if (!dates.Any())
                return null;

            var latestDate = dates.Max();
            return LoadSkill6Info(latestDate);
        }

        private static T? LoadRawData<T>(DateTime date, string category) where T : class
        {
            try
            {
                string dateStr = date.ToString("yyyy-MM-dd");
                string file = Path.Combine(RawPath, $"{dateStr}-{category}.json");
                
                if (!File.Exists(file))
                    return null;

                string json = File.ReadAllText(file);
                
                // empty 체크
                if (json.Contains("\"empty\":true"))
                    return null;

                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region 장비 옵션 상세 비교

        /// <summary>
        /// 두 장비 아이템의 상세 옵션 변경 내역 생성
        /// </summary>
        public static List<ItemOptionChange> CompareItemOptions(ItemEquipmentInfo? oldItem, ItemEquipmentInfo newItem)
        {
            var changes = new List<ItemOptionChange>();

            // 신규 아이템
            if (oldItem == null)
            {
                changes.Add(new ItemOptionChange
                {
                    ChangeType = ItemOptionChangeType.NewItem,
                    Category = "신규",
                    Description = "신규 장착",
                    NewValue = newItem.ItemName ?? ""
                });

                // 신규 아이템의 주요 옵션도 표시
                if (!string.IsNullOrEmpty(newItem.Starforce) && newItem.Starforce != "0")
                {
                    changes.Add(new ItemOptionChange
                    {
                        ChangeType = ItemOptionChangeType.Starforce,
                        Category = "스타포스",
                        NewValue = $"{newItem.Starforce}성"
                    });
                }

                if (!string.IsNullOrEmpty(newItem.PotentialOptionGrade))
                {
                    var potLines = GetPotentialLines(newItem);
                changes.Add(new ItemOptionChange
                {
                    ChangeType = ItemOptionChangeType.Potential,
                    Category = "잠재능력 등급업",
                    NewValue = newItem.PotentialOptionGrade,
                    Details = potLines
                });
                }

                if (!string.IsNullOrEmpty(newItem.AdditionalPotentialOptionGrade))
                {
                    var addLines = GetAdditionalPotentialLines(newItem);
                changes.Add(new ItemOptionChange
                {
                    ChangeType = ItemOptionChangeType.AdditionalPotential,
                    Category = "에디셔널 등급업",
                    NewValue = newItem.AdditionalPotentialOptionGrade,
                    Details = addLines
                });
                }

                return changes;
            }

            // 스타포스 변경
            int oldStar = ParseInt(oldItem.Starforce);
            int newStar = ParseInt(newItem.Starforce);
            if (oldStar != newStar)
            {
                changes.Add(new ItemOptionChange
                {
                    ChangeType = ItemOptionChangeType.Starforce,
                    Category = "스타포스",
                    OldValue = $"{oldStar}성",
                    NewValue = $"{newStar}성",
                    Description = $"{oldStar}성 → {newStar}성"
                });
            }

            // 잠재능력 등급 변경
            if (oldItem.PotentialOptionGrade != newItem.PotentialOptionGrade)
            {
                changes.Add(new ItemOptionChange
                {
                    ChangeType = ItemOptionChangeType.Potential,
                    Category = "잠재능력 등급업",
                    OldValue = oldItem.PotentialOptionGrade ?? "없음",
                    NewValue = newItem.PotentialOptionGrade ?? "없음",
                    Description = $"{oldItem.PotentialOptionGrade ?? "없음"} → {newItem.PotentialOptionGrade ?? "없음"}"
                });
            }

            // 잠재능력 옵션 변경
            var oldPotLines = GetPotentialLines(oldItem);
            var newPotLines = GetPotentialLines(newItem);
            if (!oldPotLines.SequenceEqual(newPotLines) && oldItem.PotentialOptionGrade == newItem.PotentialOptionGrade)
            {
                var detailChanges = CompareOptionLines(oldPotLines, newPotLines);
                if (detailChanges.Any())
                {
                    changes.Add(new ItemOptionChange
                    {
                        ChangeType = ItemOptionChangeType.PotentialOption,
                        Category = "잠재능력 옵션",
                        Details = detailChanges
                    });
                }
            }

            // 에디셔널 등급 변경
            if (oldItem.AdditionalPotentialOptionGrade != newItem.AdditionalPotentialOptionGrade)
            {
                changes.Add(new ItemOptionChange
                {
                    ChangeType = ItemOptionChangeType.AdditionalPotential,
                    Category = "에디셔널 등급업",
                    OldValue = oldItem.AdditionalPotentialOptionGrade ?? "없음",
                    NewValue = newItem.AdditionalPotentialOptionGrade ?? "없음",
                    Description = $"{oldItem.AdditionalPotentialOptionGrade ?? "없음"} → {newItem.AdditionalPotentialOptionGrade ?? "없음"}"
                });
            }

            // 에디셔널 옵션 변경
            var oldAddLines = GetAdditionalPotentialLines(oldItem);
            var newAddLines = GetAdditionalPotentialLines(newItem);
            if (!oldAddLines.SequenceEqual(newAddLines) && oldItem.AdditionalPotentialOptionGrade == newItem.AdditionalPotentialOptionGrade)
            {
                var detailChanges = CompareOptionLines(oldAddLines, newAddLines);
                if (detailChanges.Any())
                {
                    changes.Add(new ItemOptionChange
                    {
                        ChangeType = ItemOptionChangeType.AdditionalPotentialOption,
                        Category = "에디셔널 옵션",
                        Details = detailChanges
                    });
                }
            }

            // 추가옵션 변경
            var addOptionChanges = CompareAddOptions(oldItem.ItemAddOption, newItem.ItemAddOption);
            if (addOptionChanges.Any())
            {
                changes.Add(new ItemOptionChange
                {
                    ChangeType = ItemOptionChangeType.AddOption,
                    Category = "추가 옵션",
                    Details = addOptionChanges
                });
            }

            // 주문서 업그레이드 횟수 변경
            int oldScroll = ParseInt(oldItem.ScrollUpgrade);
            int newScroll = ParseInt(newItem.ScrollUpgrade);
            if (oldScroll != newScroll)
            {
                changes.Add(new ItemOptionChange
                {
                    ChangeType = ItemOptionChangeType.Scroll,
                    Category = "주문서",
                    OldValue = $"{oldScroll}회",
                    NewValue = $"{newScroll}회",
                    Description = $"업그레이드 {oldScroll}회 → {newScroll}회"
                });
            }

            // 주문서 옵션(etc) 변경
            var scrollOptionChanges = CompareEtcOptions(oldItem.ItemEtcOption, newItem.ItemEtcOption);
            if (scrollOptionChanges.Any())
            {
                changes.Add(new ItemOptionChange
                {
                    ChangeType = ItemOptionChangeType.ScrollOption,
                    Category = "주문서 옵션",
                    Details = scrollOptionChanges
                });
            }

            // 소울 변경
            if (oldItem.SoulName != newItem.SoulName || oldItem.SoulOption != newItem.SoulOption)
            {
                if (!string.IsNullOrEmpty(newItem.SoulName))
                {
                    changes.Add(new ItemOptionChange
                    {
                        ChangeType = ItemOptionChangeType.Soul,
                        Category = "소울",
                        OldValue = oldItem.SoulName ?? "없음",
                        NewValue = newItem.SoulName ?? "",
                        Description = $"{oldItem.SoulName ?? "없음"} → {newItem.SoulName}"
                    });
                }
            }

            return changes;
        }

        private static List<string> GetPotentialLines(ItemEquipmentInfo item)
        {
            var lines = new List<string>();
            if (!string.IsNullOrEmpty(item.PotentialOption1)) lines.Add(item.PotentialOption1);
            if (!string.IsNullOrEmpty(item.PotentialOption2)) lines.Add(item.PotentialOption2);
            if (!string.IsNullOrEmpty(item.PotentialOption3)) lines.Add(item.PotentialOption3);
            return lines;
        }

        private static List<string> GetAdditionalPotentialLines(ItemEquipmentInfo item)
        {
            var lines = new List<string>();
            if (!string.IsNullOrEmpty(item.AdditionalPotentialOption1)) lines.Add(item.AdditionalPotentialOption1);
            if (!string.IsNullOrEmpty(item.AdditionalPotentialOption2)) lines.Add(item.AdditionalPotentialOption2);
            if (!string.IsNullOrEmpty(item.AdditionalPotentialOption3)) lines.Add(item.AdditionalPotentialOption3);
            return lines;
        }

        private static List<string> CompareOptionLines(List<string> oldLines, List<string> newLines)
        {
            var changes = new List<string>();
            int maxLen = Math.Max(oldLines.Count, newLines.Count);
            
            for (int i = 0; i < maxLen; i++)
            {
                string oldVal = i < oldLines.Count ? oldLines[i] : "";
                string newVal = i < newLines.Count ? newLines[i] : "";
                
                if (oldVal != newVal)
                {
                    if (string.IsNullOrEmpty(oldVal))
                        changes.Add($"+ {newVal}");
                    else if (string.IsNullOrEmpty(newVal))
                        changes.Add($"- {oldVal}");
                    else
                        changes.Add($"{oldVal} → {newVal}");
                }
            }
            
            return changes;
        }

        private static List<string> CompareAddOptions(ItemOptionInfo? oldOpt, ItemOptionInfo? newOpt)
        {
            var changes = new List<string>();
            
            CompareStatValue(changes, "STR", oldOpt?.Str, newOpt?.Str);
            CompareStatValue(changes, "DEX", oldOpt?.Dex, newOpt?.Dex);
            CompareStatValue(changes, "INT", oldOpt?.Int, newOpt?.Int);
            CompareStatValue(changes, "LUK", oldOpt?.Luk, newOpt?.Luk);
            CompareStatValue(changes, "MaxHP", oldOpt?.MaxHp, newOpt?.MaxHp);
            CompareStatValue(changes, "MaxMP", oldOpt?.MaxMp, newOpt?.MaxMp);
            CompareStatValue(changes, "공격력", oldOpt?.AttackPower, newOpt?.AttackPower);
            CompareStatValue(changes, "마력", oldOpt?.MagicPower, newOpt?.MagicPower);
            CompareStatValue(changes, "올스탯%", oldOpt?.AllStat, newOpt?.AllStat);
            CompareStatValue(changes, "보공%", oldOpt?.BossDamage, newOpt?.BossDamage);
            CompareStatValue(changes, "데미지%", oldOpt?.Damage, newOpt?.Damage);
            
            return changes;
        }

        private static List<string> CompareEtcOptions(ItemOptionInfo? oldOpt, ItemOptionInfo? newOpt)
        {
            var changes = new List<string>();
            
            CompareStatValue(changes, "STR", oldOpt?.Str, newOpt?.Str);
            CompareStatValue(changes, "DEX", oldOpt?.Dex, newOpt?.Dex);
            CompareStatValue(changes, "INT", oldOpt?.Int, newOpt?.Int);
            CompareStatValue(changes, "LUK", oldOpt?.Luk, newOpt?.Luk);
            CompareStatValue(changes, "공격력", oldOpt?.AttackPower, newOpt?.AttackPower);
            CompareStatValue(changes, "마력", oldOpt?.MagicPower, newOpt?.MagicPower);
            
            return changes;
        }

        private static void CompareStatValue(List<string> changes, string statName, string? oldVal, string? newVal)
        {
            int oldNum = ParseInt(oldVal);
            int newNum = ParseInt(newVal);
            
            if (oldNum != newNum && (oldNum > 0 || newNum > 0))
            {
                changes.Add($"{statName} {oldNum} → {newNum}");
            }
        }

        private static int ParseInt(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            var cleaned = s.Replace(",", "").Replace("%", "").Trim();
            return int.TryParse(cleaned, out var v) ? v : 0;
        }

        #endregion

        #region 원본 데이터 기반 장비 변경 처리

        /// <summary>
        /// 원본 데이터에서 장비 변경 내역을 생성하여 기록
        /// </summary>
        public static void ProcessItemChangesFromRaw(string characterId, string characterName, DateTime startDate, DateTime endDate)
        {
            var dateList = new List<DateTime>();
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                if (date > DateTime.Today) continue;
                if (HasDataForDate(date))
                {
                    dateList.Add(date);
                }
            }

            if (dateList.Count < 1) return; // 최소 1일 이상의 데이터가 있어야 함

            dateList = dateList.OrderBy(d => d).ToList();

            Dictionary<string, ItemEquipmentInfo>? prevItems = null;
            HashSet<string> prevPresetNames = new();

            // 첫 날짜 이전의 데이터가 있는지 확인
            if (dateList.Any())
            {
                var firstDate = dateList.First();
                var prevDate = firstDate.AddDays(-1);
                var prevItemInfo = LoadItemInfo(prevDate);
                if (prevItemInfo?.ItemEquipment != null)
                {
                    prevItems = prevItemInfo.ItemEquipment
                        .Where(item => !string.IsNullOrEmpty(item.ItemEquipmentSlot) && !string.IsNullOrEmpty(item.ItemName))
                        .GroupBy(item => item.ItemEquipmentSlot!)
                        .ToDictionary(g => g.Key, g => g.First());
                    prevPresetNames = CollectPresetNames(prevItemInfo);
                }
            }

            bool isFirstValidData = true;
            
            foreach (var targetDate in dateList)
            {
                var itemInfo = LoadItemInfo(targetDate);
                if (itemInfo?.ItemEquipment == null) continue;

                // 슬롯 중복 방지
                var currentItems = itemInfo.ItemEquipment
                    .Where(item => !string.IsNullOrEmpty(item.ItemEquipmentSlot) && !string.IsNullOrEmpty(item.ItemName))
                    .GroupBy(item => item.ItemEquipmentSlot!)
                    .ToDictionary(g => g.Key, g => g.First());

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

                                string json = System.Text.Json.JsonSerializer.Serialize(newItem);
                                string summary = BuildChangeSummary(oldItem, newItem, isReplace: true);
                                var optionChanges = CompareItemOptions(oldItem, newItem);
                                string optionChangesJson = System.Text.Json.JsonSerializer.Serialize(optionChanges);
                                string itemIcon = newItem.ItemIcon ?? "";

                                StatisticsService.RecordItemChange(characterId, characterName, slot, oldItem.ItemName!, newItem.ItemName!, "교체", json, targetDate, summary, optionChangesJson, itemIcon);
                            }
                            else if (IsItemOptionChanged(oldItem, newItem))
                            {
                                if (IsSpiritPendant(newItem.ItemName)) continue;
                                string json = System.Text.Json.JsonSerializer.Serialize(newItem);
                                string summary = BuildChangeSummary(oldItem, newItem);
                                var optionChanges = CompareItemOptions(oldItem, newItem);
                                string optionChangesJson = System.Text.Json.JsonSerializer.Serialize(optionChanges);
                                string itemIcon = newItem.ItemIcon ?? "";

                                StatisticsService.RecordItemChange(characterId, characterName, slot, oldItem.ItemName!, newItem.ItemName!, "옵션 변경", json, targetDate, summary, optionChangesJson, itemIcon);
                            }
                        }
                        else
                        {
                            // 프리셋에 이미 존재하는 아이템이면 신규 장착으로 보지 않음
                            if (currentPresetNames.Contains(newItem.ItemName ?? "") || prevPresetNames.Contains(newItem.ItemName ?? ""))
                                continue;
                            if (IsSpiritPendant(newItem.ItemName)) continue;

                            string json = System.Text.Json.JsonSerializer.Serialize(newItem);
                            string summary = BuildChangeSummary(null, newItem, isNew: true);
                            var optionChanges = CompareItemOptions(null, newItem);
                            string optionChangesJson = System.Text.Json.JsonSerializer.Serialize(optionChanges);
                            string itemIcon = newItem.ItemIcon ?? "";

                            StatisticsService.RecordItemChange(characterId, characterName, slot, "없음", newItem.ItemName!, "장착", json, targetDate, summary, optionChangesJson, itemIcon);
                        }
                    }
                }
                else if (isFirstValidData)
                {
                    // 첫 번째 유효한 데이터이고 이전 데이터가 없으면, 현재 장비를 "초기 상태"로 기록
                    // 주요 장비 슬롯만 기록 (무기, 보조무기, 엠블렘, 모자, 상의, 하의, 신발, 장갑, 망토, 어깨장식, 얼굴장식, 눈장식, 귀고리, 반지, 펜던트, 벨트, 뱃지, 훈장)
                    var mainSlots = new HashSet<string> { "무기", "보조무기", "엠블렘", "모자", "상의", "하의", "신발", "장갑", "망토", "어깨장식", "얼굴장식", "눈장식", "귀고리", "반지1", "반지2", "반지3", "반지4", "펜던트", "펜던트2", "벨트", "뱃지", "훈장", "포켓 아이템" };
                    
                    foreach (var itemPair in currentItems)
                    {
                        var newItem = itemPair.Value;
                        string slot = itemPair.Key;
                        
                        // 주요 슬롯이 아니면 스킵
                        if (!mainSlots.Contains(slot)) continue;
                        // 정령의 펜던트 스킵
                        if (IsSpiritPendant(newItem.ItemName)) continue;
                        // 프리셋 아이템 스킵
                        if (currentPresetNames.Contains(newItem.ItemName ?? "")) continue;

                        string json = System.Text.Json.JsonSerializer.Serialize(newItem);
                        string summary = BuildChangeSummary(null, newItem, isNew: true);
                        var optionChanges = CompareItemOptions(null, newItem);
                        string optionChangesJson = System.Text.Json.JsonSerializer.Serialize(optionChanges);
                        string itemIcon = newItem.ItemIcon ?? "";

                        StatisticsService.RecordItemChange(characterId, characterName, slot, "없음", newItem.ItemName!, "장착", json, targetDate, summary, optionChangesJson, itemIcon);
                    }
                }

                prevItems = currentItems;
                prevPresetNames = currentPresetNames;
                isFirstValidData = false;
            }
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

        private static bool IsItemOptionChanged(ItemEquipmentInfo oldItem, ItemEquipmentInfo newItem)
        {
            if (oldItem.Starforce != newItem.Starforce) return true;
            if (oldItem.PotentialOptionGrade != newItem.PotentialOptionGrade) return true;
            if (oldItem.AdditionalPotentialOptionGrade != newItem.AdditionalPotentialOptionGrade) return true;
            if (oldItem.PotentialOption1 != newItem.PotentialOption1) return true;
            if (oldItem.PotentialOption2 != newItem.PotentialOption2) return true;
            if (oldItem.PotentialOption3 != newItem.PotentialOption3) return true;
            if (oldItem.AdditionalPotentialOption1 != newItem.AdditionalPotentialOption1) return true;
            if (oldItem.AdditionalPotentialOption2 != newItem.AdditionalPotentialOption2) return true;
            if (oldItem.AdditionalPotentialOption3 != newItem.AdditionalPotentialOption3) return true;
            if (!AreOptionsEqual(oldItem.ItemTotalOption, newItem.ItemTotalOption)) return true;
            if (!AreOptionsEqual(oldItem.ItemBaseOption, newItem.ItemBaseOption)) return true;
            if (!AreOptionsEqual(oldItem.ItemAddOption, newItem.ItemAddOption)) return true;
            if (!AreOptionsEqual(oldItem.ItemEtcOption, newItem.ItemEtcOption)) return true;
            if (!AreOptionsEqual(oldItem.ItemStarforceOption, newItem.ItemStarforceOption)) return true;
            if (oldItem.SoulName != newItem.SoulName || oldItem.SoulOption != newItem.SoulOption) return true;
            return false;
        }

        private static bool AreOptionsEqual(ItemOptionInfo? opt1, ItemOptionInfo? opt2)
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
                   opt1.Jump == opt2.Jump;
        }

        private static string BuildChangeSummary(ItemEquipmentInfo? oldItem, ItemEquipmentInfo newItem, bool isNew = false, bool isReplace = false)
        {
            var parts = new List<string>();

            if (isNew)
            {
                parts.Add("신규 장착");
            }
            else if (isReplace && oldItem != null && !string.IsNullOrEmpty(oldItem.ItemName) && !string.IsNullOrEmpty(newItem.ItemName) && oldItem.ItemName != newItem.ItemName)
            {
                parts.Add($"{oldItem.ItemName} → {newItem.ItemName}");
            }

            int oldStar = ParseInt(oldItem?.Starforce);
            int newStar = ParseInt(newItem.Starforce);
            if (oldStar != newStar)
            {
                parts.Add($"스타포스 {oldStar}성 → {newStar}성");
            }

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

            if (oldItem == null || !AreOptionsEqual(oldItem.ItemAddOption, newItem.ItemAddOption))
            {
                parts.Add("추가옵션 변경");
            }

            if (oldItem == null || !AreOptionsEqual(oldItem.ItemEtcOption, newItem.ItemEtcOption))
            {
                parts.Add("주문서 옵션 변경");
            }

            if (oldItem == null || oldItem.SoulOption != newItem.SoulOption)
            {
                if (!string.IsNullOrEmpty(newItem.SoulOption))
                    parts.Add("소울 변경");
            }

            if (parts.Count == 0)
                parts.Add("옵션 변경");

            return string.Join(" / ", parts);
        }

        #endregion
    }

    #region 데이터 모델

    /// <summary>
    /// 데이터 수집 현황 요약
    /// </summary>
    public class DataCollectionSummary
    {
        public int TotalDays { get; set; }
        public DateTime? OldestDate { get; set; }
        public DateTime? NewestDate { get; set; }
        public HashSet<DateTime> ExistingDates { get; set; } = new();
    }

    /// <summary>
    /// 장비 옵션 변경 타입
    /// </summary>
    public enum ItemOptionChangeType
    {
        NewItem,            // 신규 장착
        Replace,            // 아이템 교체
        Starforce,          // 스타포스
        Potential,          // 잠재능력 등급
        PotentialOption,    // 잠재능력 옵션
        AdditionalPotential,     // 에디셔널 등급
        AdditionalPotentialOption, // 에디셔널 옵션
        AddOption,          // 추가옵션
        Scroll,             // 주문서 업그레이드 횟수
        ScrollOption,       // 주문서 옵션
        Soul                // 소울
    }

    /// <summary>
    /// 장비 옵션 변경 상세 정보
    /// </summary>
    public class ItemOptionChange
    {
        public ItemOptionChangeType ChangeType { get; set; }
        public string Category { get; set; } = "";       // 표시용 카테고리명
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
        public string Description { get; set; } = "";    // 요약 설명 (예: "15성 → 20성")
        public List<string> Details { get; set; } = new(); // 상세 변경 내역

        // UI 바인딩용 프로퍼티
        public string DisplayText => !string.IsNullOrEmpty(Description) 
            ? Description 
            : Details.Any() 
                ? string.Join("\n", Details) 
                : $"{OldValue} → {NewValue}";

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

    #endregion
}
