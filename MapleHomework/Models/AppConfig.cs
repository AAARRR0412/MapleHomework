using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace MapleHomework.Models
{
    // 1. 저장할 데이터의 형태 정의
    public class AppSettings
    {
        public string ApiKey { get; set; } = "";
        public string CharacterName { get; set; } = "";
        public bool IsDarkTheme { get; set; } = true; // 기본값: 다크 모드
        public bool AutoStartEnabled { get; set; } = false; // 기본값: 자동 시작 비활성화
    }

    // 2. 파일 저장/불러오기 담당
    public static class ConfigManager
    {
        private static string FilePath = "config.json"; // 실행 파일 옆에 생김
        private const string AppName = "MapleHomework";

        public static void Save(AppSettings settings)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(FilePath, jsonString);
        }

        public static AppSettings Load()
        {
            if (!File.Exists(FilePath))
                return new AppSettings(); // 파일 없으면 빈 설정 반환

            try
            {
                string jsonString = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<AppSettings>(jsonString) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings(); // 깨졌으면 초기화
            }
        }

        // 자동 시작 설정 (레지스트리)
        public static void SetAutoStart(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key == null) return;

                if (enabled)
                {
                    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppName, $"\"{exePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
            catch
            {
                // 레지스트리 접근 실패 시 무시
            }
        }

        // 자동 시작 상태 확인
        public static bool IsAutoStartEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                return key?.GetValue(AppName) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}