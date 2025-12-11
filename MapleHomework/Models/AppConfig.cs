using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using System.Text.Json.Serialization; // JsonIgnore 추가

namespace MapleHomework.Models
{
    /// <summary>
    /// 테마 모드
    /// </summary>
    public enum ThemeMode
    {
        Light,      // 라이트 모드
        Dark,       // 다크 모드
        System      // 시스템 설정 따라가기
    }

    /// <summary>
    /// 커스텀 테마 설정
    /// </summary>
    public class CustomTheme
    {
        public string Name { get; set; } = "";
        public string PrimaryColor { get; set; } = "#5AC8FA";
        public string AccentColor { get; set; } = "#FF9500";
        public string BackgroundColor { get; set; } = "#1E1E2E";
        public string SurfaceColor { get; set; } = "#2D2D3D";
        public string TextColor { get; set; } = "#FFFFFF";
        public string SecondaryTextColor { get; set; } = "#888888";
    }

    /// <summary>
    /// 모바일 알림 설정
    /// </summary>
    public class MobileNotificationSettings
    {
        public bool Enabled { get; set; } = false;
        public string Provider { get; set; } = ""; // "pushover", "telegram", "discord"
        public string ApiToken { get; set; } = "";
        public string UserId { get; set; } = ""; // Pushover User Key, Telegram Chat ID, Discord Webhook URL
    }

    // 1. 저장할 데이터의 형태 정의
    public class AppSettings
    {
        public string ApiKey { get; set; } = "";
        public string CharacterName { get; set; } = "";
        
        [JsonIgnore] // ThemeMode로 대체되었으므로 직렬화에서 제외
        public bool IsDarkTheme { get; set; } = false;
        
        public ThemeMode ThemeMode { get; set; } = ThemeMode.System; // 기본값을 System으로 변경
        public bool AutoStartEnabled { get; set; } = false; // 기본값: 자동 시작 비활성화

        // 알림 설정
        public bool IsNotificationEnabled { get; set; } = false;
        
        // 일일 알림: n시간 전 (자정 기준)
        public bool NotifyDailyTasks { get; set; } = true;
        public int DailyNotifyHoursBefore { get; set; } = 4; // 기본값: 자정 4시간 전 (오후 8시)
        
        // 주간 알림: n일 전 (목요일 기준)
        public bool NotifyWeeklyTasks { get; set; } = true;
        public int WeeklyNotifyDaysBefore { get; set; } = 1; // 기본값: 1일 전 (수요일)
        
        // 보스 알림: n일 전 (목요일 기준)
        public bool NotifyBossTasks { get; set; } = true;
        public int BossNotifyDaysBefore { get; set; } = 1; // 기본값: 1일 전 (수요일)
        
        // 월간 알림: n일 전 (월초 기준)
        public bool NotifyMonthlyTasks { get; set; } = true;
        public int MonthlyNotifyDaysBefore { get; set; } = 3; // 기본값: 3일 전

        // 시작 시 팝업 표시
        public bool ShowStartupPopup { get; set; } = true;

        // 오버레이 설정
        public bool IsOverlayEnabled { get; set; } = false;
        public bool ShowOnlyFavorites { get; set; } = true; // 즐겨찾기만 표시
        public string OverlayProcessName { get; set; } = "MapleStory"; // 감지할 프로세스 이름
        public double OverlayOpacity { get; set; } = 0.8; // 오버레이 투명도 (0.0 ~ 1.0)

        // 커스텀 테마 설정
        public CustomTheme? CurrentCustomTheme { get; set; }
        public List<CustomTheme> SavedThemes { get; set; } = new();

        // 모바일 알림 설정
        public MobileNotificationSettings MobileNotification { get; set; } = new();

        // 위젯 모드 설정
        public bool WidgetModeEnabled { get; set; } = false;
        public double WidgetOpacity { get; set; } = 0.9;
        public int WidgetPositionX { get; set; } = 100;
        public int WidgetPositionY { get; set; } = 100;

        // 메인 윈도우 위치 저장
        public double MainWindowLeft { get; set; } = double.NaN;
        public double MainWindowTop { get; set; } = double.NaN;
        public double MainWindowWidth { get; set; } = 460;
        public double MainWindowHeight { get; set; } = 750;
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
                return new AppSettings();

            try
            {
                string jsonString = File.ReadAllText(FilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(jsonString) ?? new AppSettings();

                // 레거시 IsDarkTheme 값 마이그레이션
                if (settings.ThemeMode == ThemeMode.System && settings.IsDarkTheme)
                {
                    settings.ThemeMode = ThemeMode.Dark;
                }
                
                return settings;
            }
            catch
            {
                return new AppSettings();
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