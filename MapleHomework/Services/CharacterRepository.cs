using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MapleHomework.Models;

namespace MapleHomework.Services
{
    /// <summary>
    /// 캐릭터 데이터 저장/불러오기
    /// </summary>
    public static class CharacterRepository
    {
        private const string FilePath = "characters_data.json";
        private const int MaxCharacters = 10;

        /// <summary>
        /// 전체 앱 데이터 저장
        /// </summary>
        public static void Save(AppData data)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(FilePath, json);
            }
            catch
            {
                // 저장 실패 시 조용히 넘어감
            }
        }

        /// <summary>
        /// 전체 앱 데이터 불러오기
        /// </summary>
        public static AppData Load()
        {
            if (!File.Exists(FilePath))
                return new AppData();

            try
            {
                string json = File.ReadAllText(FilePath);
                var appData = JsonSerializer.Deserialize<AppData>(json) ?? new AppData();
                
                // 각 캐릭터의 보스 난이도를 GameData에서 동기화
                SyncBossDifficulties(appData);
                
                return appData;
            }
            catch
            {
                return new AppData();
            }
        }

        /// <summary>
        /// 보스 태스크의 AvailableDifficulties를 GameData에서 동기화
        /// </summary>
        private static void SyncBossDifficulties(AppData appData)
        {
            foreach (var character in appData.Characters)
            {
                // 주간 보스 동기화
                foreach (var task in character.BossTasks)
                {
                    var bossInfo = GameData.GetBossInfo(task.Name, isMonthly: false);
                    if (bossInfo != null)
                    {
                        task.AvailableDifficulties = new List<BossDifficulty>(bossInfo.AvailableDifficulties);
                        
                        // 현재 설정된 난이도가 사용 가능한 난이도에 없으면 기본값으로 설정
                        if (!task.AvailableDifficulties.Contains(task.Difficulty))
                        {
                            task.Difficulty = bossInfo.DefaultDifficulty;
                        }
                    }
                }

                // 월간 보스 동기화
                foreach (var task in character.MonthlyTasks)
                {
                    var bossInfo = GameData.GetBossInfo(task.Name, isMonthly: true);
                    if (bossInfo != null)
                    {
                        task.AvailableDifficulties = new List<BossDifficulty>(bossInfo.AvailableDifficulties);
                        
                        // 현재 설정된 난이도가 사용 가능한 난이도에 없으면 기본값으로 설정
                        if (!task.AvailableDifficulties.Contains(task.Difficulty))
                        {
                            task.Difficulty = bossInfo.DefaultDifficulty;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 기존 config.json에서 마이그레이션
        /// </summary>
        public static AppData MigrateFromOldConfig()
        {
            var appData = Load();
            
            // 기존 config.json 확인
            if (File.Exists("config.json"))
            {
                try
                {
                    var oldConfig = ConfigManager.Load();
                    if (!string.IsNullOrEmpty(oldConfig.ApiKey))
                    {
                        appData.ApiKey = oldConfig.ApiKey;
                        appData.IsDarkTheme = oldConfig.IsDarkTheme;
                        appData.AutoStartEnabled = oldConfig.AutoStartEnabled;

                        // 기존 캐릭터가 없으면 추가
                        if (appData.Characters.Count == 0 && !string.IsNullOrEmpty(oldConfig.CharacterName))
                        {
                            var character = new CharacterProfile
                            {
                                Nickname = oldConfig.CharacterName
                            };
                            appData.Characters.Add(character);
                            appData.SelectedCharacterId = character.Id;
                        }
                    }
                }
                catch
                {
                    // 마이그레이션 실패 시 무시
                }
            }

            // 기존 homework_data.json에서 태스크 데이터 마이그레이션
            if (appData.Characters.Count > 0 && File.Exists("homework_data.json"))
            {
                try
                {
                    var oldTaskData = TaskRepository.Load();
                    if (oldTaskData != null)
                    {
                        var firstChar = appData.Characters[0];
                        // 기존 데이터가 있으면 첫 번째 캐릭터에 적용
                        // (이미 태스크가 있으면 덮어쓰지 않음)
                    }
                }
                catch
                {
                    // 마이그레이션 실패 시 무시
                }
            }

            return appData;
        }

        /// <summary>
        /// 새 캐릭터 추가 가능 여부
        /// </summary>
        public static bool CanAddCharacter(AppData data)
        {
            return data.Characters.Count < MaxCharacters;
        }

        /// <summary>
        /// 새 캐릭터 추가
        /// </summary>
        public static CharacterProfile? AddCharacter(AppData data, string nickname)
        {
            if (!CanAddCharacter(data)) return null;

            var character = new CharacterProfile
            {
                Nickname = nickname
            };

            // 기본 태스크 복사
            foreach (var task in GameData.Dailies)
            {
                character.DailyTasks.Add(new HomeworkTask
                {
                    Name = task.Name,
                    Category = task.Category,
                    RequiredLevel = task.RequiredLevel,
                    IsActive = task.IsActive
                });
            }

            foreach (var task in GameData.Weeklies)
            {
                character.WeeklyTasks.Add(new HomeworkTask
                {
                    Name = task.Name,
                    Category = task.Category,
                    IsActive = task.IsActive
                });
            }

            // 주간 보스 - GameData.WeeklyBosses에서 정보 가져오기
            foreach (var bossInfo in GameData.WeeklyBosses)
            {
                character.BossTasks.Add(new HomeworkTask
                {
                    Name = bossInfo.Name,
                    Category = bossInfo.Category,
                    Difficulty = bossInfo.DefaultDifficulty,
                    AvailableDifficulties = new List<BossDifficulty>(bossInfo.AvailableDifficulties),
                    IsActive = bossInfo.IsActiveByDefault
                });
            }

            // 월간 보스 - GameData.MonthlyBosses에서 정보 가져오기
            foreach (var bossInfo in GameData.MonthlyBosses)
            {
                character.MonthlyTasks.Add(new HomeworkTask
                {
                    Name = bossInfo.Name,
                    Category = bossInfo.Category,
                    Difficulty = bossInfo.DefaultDifficulty,
                    AvailableDifficulties = new List<BossDifficulty>(bossInfo.AvailableDifficulties),
                    IsActive = bossInfo.IsActiveByDefault
                });
            }

            data.Characters.Add(character);
            return character;
        }

        /// <summary>
        /// 캐릭터 삭제
        /// </summary>
        public static bool RemoveCharacter(AppData data, string characterId)
        {
            var character = data.Characters.Find(c => c.Id == characterId);
            if (character == null) return false;

            data.Characters.Remove(character);

            // 선택된 캐릭터가 삭제되면 첫 번째 캐릭터 선택
            if (data.SelectedCharacterId == characterId)
            {
                data.SelectedCharacterId = data.Characters.Count > 0 ? data.Characters[0].Id : null;
            }

            return true;
        }
    }
}
