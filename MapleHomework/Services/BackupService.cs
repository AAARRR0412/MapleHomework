using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Win32;
using MapleHomework.Models;

namespace MapleHomework.Services
{
    public static class BackupService
    {
        private const string BackupExtension = ".mhback";
        private const string BackupFilter = "MapleHomework Backup (*.mhback)|*.mhback";

        public static string GetBackupFileFilter() => BackupFilter;
        public static string GetBackupExtension() => BackupExtension;

        public class BackupMetadata
        {
            public DateTime CreatedAt { get; set; }
            public string Version { get; set; } = "1.0";
            public string Description { get; set; } = "";
            public int CharacterCount { get; set; }
        }

        public class BackupContainer
        {
            public BackupMetadata Metadata { get; set; } = new();
            public AppData Data { get; set; } = new();
            public AppSettings Settings { get; set; } = new();
            public StatisticsData Statistics { get; set; } = new();
        }

        /// <summary>
        /// 백업 생성
        /// </summary>
        public static Result<string> CreateBackup(string filePath, string description = "")
        {
            try
            {
                var appData = CharacterRepository.Load();
                var settings = ConfigManager.Load();
                var statistics = StatisticsService.Load();

                var container = new BackupContainer
                {
                    Metadata = new BackupMetadata
                    {
                        CreatedAt = DateTime.Now,
                        Description = description,
                        CharacterCount = appData.Characters.Count
                    },
                    Data = appData,
                    Settings = settings,
                    Statistics = statistics
                };

                string json = JsonSerializer.Serialize(container, new JsonSerializerOptions { WriteIndented = false });

                // GZip 압축
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                using (GZipStream gzip = new GZipStream(fs, CompressionLevel.Optimal))
                using (StreamWriter writer = new StreamWriter(gzip))
                {
                    writer.Write(json);
                }

                return Result<string>.Success($"백업 파일이 생성되었습니다: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(ex);
            }
        }

        /// <summary>
        /// 백업 파일 메타데이터 읽기
        /// </summary>
        public static Result<BackupMetadata> ReadBackupMetadata(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                using (GZipStream gzip = new GZipStream(fs, CompressionMode.Decompress))
                using (StreamReader reader = new StreamReader(gzip))
                {
                    string json = reader.ReadToEnd();
                    var container = JsonSerializer.Deserialize<BackupContainer>(json);
                    
                    if (container == null || container.Metadata == null)
                        return Result<BackupMetadata>.Failure("유효하지 않은 백업 파일입니다.");

                    return Result<BackupMetadata>.Success(container.Metadata);
                }
            }
            catch (Exception ex)
            {
                return Result<BackupMetadata>.Failure(ex);
            }
        }

        /// <summary>
        /// 백업 복원
        /// </summary>
        public static Result RestoreFromBackup(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                using (GZipStream gzip = new GZipStream(fs, CompressionMode.Decompress))
                using (StreamReader reader = new StreamReader(gzip))
                {
                    string json = reader.ReadToEnd();
                    var container = JsonSerializer.Deserialize<BackupContainer>(json);

                    if (container == null)
                        return Result.Failure("백업 데이터를 읽을 수 없습니다.");

                    // 데이터 복원
                    CharacterRepository.Save(container.Data);
                    ConfigManager.Save(container.Settings);
                    StatisticsService.Save(container.Statistics);

                    return Result.Success();
                }
            }
            catch (Exception ex)
            {
                return Result.Failure(ex);
            }
        }
    }
}
