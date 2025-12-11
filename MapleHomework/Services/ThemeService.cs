using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using MapleHomework.Models;
using Microsoft.Win32;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;

namespace MapleHomework.Services
{
    /// <summary>
    /// 테마 관리 서비스
    /// </summary>
    public static class ThemeService
    {
        private static bool _isWatchingSystemTheme = false;

        /// <summary>
        /// 시스템 테마가 다크 모드인지 확인
        /// </summary>
        public static bool IsSystemDarkTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                
                if (key != null)
                {
                    var value = key.GetValue("AppsUseLightTheme");
                    if (value is int intValue)
                    {
                        return intValue == 0; // 0 = Dark, 1 = Light
                    }
                }
            }
            catch
            {
                // 레지스트리 접근 실패
            }

            return false; // 기본값: 라이트
        }

        /// <summary>
        /// 현재 설정에 따라 실제 다크 모드 여부 반환
        /// </summary>
        public static bool ShouldUseDarkTheme(AppSettings settings)
        {
            return settings.ThemeMode switch
            {
                ThemeMode.Dark => true,
                ThemeMode.Light => false,
                ThemeMode.System => IsSystemDarkTheme(),
                _ => false
            };
        }

        /// <summary>
        /// 테마 적용
        /// </summary>
        public static void ApplyTheme(bool isDark, CustomTheme? customTheme = null)
        {
            var app = Application.Current;
            if (app == null) return;

            // 기본 테마 색상
            Color primaryColor, accentColor, backgroundColor, surfaceColor, textColor, secondaryTextColor;

            if (customTheme != null)
            {
                // 커스텀 테마 적용
                primaryColor = ParseColor(customTheme.PrimaryColor);
                accentColor = ParseColor(customTheme.AccentColor);
                backgroundColor = ParseColor(customTheme.BackgroundColor);
                surfaceColor = ParseColor(customTheme.SurfaceColor);
                textColor = ParseColor(customTheme.TextColor);
                secondaryTextColor = ParseColor(customTheme.SecondaryTextColor);
            }
            else if (isDark)
            {
                // 기본 다크 테마
                primaryColor = Color.FromRgb(90, 200, 250);      // #5AC8FA
                accentColor = Color.FromRgb(255, 149, 0);        // #FF9500
                backgroundColor = Color.FromRgb(30, 30, 46);     // #1E1E2E
                surfaceColor = Color.FromRgb(45, 45, 61);        // #2D2D3D
                textColor = Color.FromRgb(255, 255, 255);        // #FFFFFF
                secondaryTextColor = Color.FromRgb(136, 136, 136); // #888888
            }
            else
            {
                // 기본 라이트 테마
                primaryColor = Color.FromRgb(0, 122, 255);       // #007AFF
                accentColor = Color.FromRgb(255, 149, 0);        // #FF9500
                backgroundColor = Color.FromRgb(242, 242, 247);  // #F2F2F7
                surfaceColor = Color.FromRgb(255, 255, 255);     // #FFFFFF
                textColor = Color.FromRgb(0, 0, 0);              // #000000
                secondaryTextColor = Color.FromRgb(100, 100, 100); // #646464
            }

            // 리소스 딕셔너리에 색상 적용
            app.Resources["ThemePrimaryColor"] = new SolidColorBrush(primaryColor);
            app.Resources["ThemeAccentColor"] = new SolidColorBrush(accentColor);
            app.Resources["ThemeBackgroundColor"] = new SolidColorBrush(backgroundColor);
            app.Resources["ThemeSurfaceColor"] = new SolidColorBrush(surfaceColor);
            app.Resources["ThemeTextColor"] = new SolidColorBrush(textColor);
            app.Resources["ThemeSecondaryTextColor"] = new SolidColorBrush(secondaryTextColor);

            // Boolean 플래그도 설정
            app.Resources["IsDarkTheme"] = isDark;
        }

        /// <summary>
        /// 시스템 테마 변경 감지 시작
        /// </summary>
        public static void StartWatchingSystemTheme(Action onThemeChanged)
        {
            if (_isWatchingSystemTheme) return;
            _isWatchingSystemTheme = true;

            // 시스템 이벤트 감지 (Windows 10+)
            SystemEvents.UserPreferenceChanged += (sender, e) =>
            {
                if (e.Category == UserPreferenceCategory.General)
                {
                    var settings = ConfigManager.Load();
                    if (settings.ThemeMode == ThemeMode.System)
                    {
                        onThemeChanged?.Invoke();
                    }
                }
            };
        }

        /// <summary>
        /// 색상 문자열 파싱
        /// </summary>
        private static Color ParseColor(string hex)
        {
            try
            {
                if (hex.StartsWith("#"))
                    hex = hex.Substring(1);

                if (hex.Length == 6)
                {
                    return Color.FromRgb(
                        Convert.ToByte(hex.Substring(0, 2), 16),
                        Convert.ToByte(hex.Substring(2, 2), 16),
                        Convert.ToByte(hex.Substring(4, 2), 16)
                    );
                }
                else if (hex.Length == 8)
                {
                    return Color.FromArgb(
                        Convert.ToByte(hex.Substring(0, 2), 16),
                        Convert.ToByte(hex.Substring(2, 2), 16),
                        Convert.ToByte(hex.Substring(4, 2), 16),
                        Convert.ToByte(hex.Substring(6, 2), 16)
                    );
                }
            }
            catch { }

            return Colors.White;
        }

        /// <summary>
        /// 기본 테마 프리셋
        /// </summary>
        public static CustomTheme[] GetDefaultThemes()
        {
            return new[]
            {
                new CustomTheme
                {
                    Name = "메이플 오리지널",
                    PrimaryColor = "#FF9500",
                    AccentColor = "#5AC8FA",
                    BackgroundColor = "#1E1E2E",
                    SurfaceColor = "#2D2D3D",
                    TextColor = "#FFFFFF",
                    SecondaryTextColor = "#888888"
                },
                new CustomTheme
                {
                    Name = "하늘빛",
                    PrimaryColor = "#5AC8FA",
                    AccentColor = "#34C759",
                    BackgroundColor = "#0A1929",
                    SurfaceColor = "#132F4C",
                    TextColor = "#FFFFFF",
                    SecondaryTextColor = "#B2BAC2"
                },
                new CustomTheme
                {
                    Name = "로즈",
                    PrimaryColor = "#FF2D55",
                    AccentColor = "#FF9500",
                    BackgroundColor = "#1C1C1E",
                    SurfaceColor = "#2C2C2E",
                    TextColor = "#FFFFFF",
                    SecondaryTextColor = "#8E8E93"
                },
                new CustomTheme
                {
                    Name = "포레스트",
                    PrimaryColor = "#34C759",
                    AccentColor = "#30D158",
                    BackgroundColor = "#161B22",
                    SurfaceColor = "#21262D",
                    TextColor = "#C9D1D9",
                    SecondaryTextColor = "#8B949E"
                },
                new CustomTheme
                {
                    Name = "퍼플",
                    PrimaryColor = "#AF52DE",
                    AccentColor = "#BF5AF2",
                    BackgroundColor = "#1A1A2E",
                    SurfaceColor = "#16213E",
                    TextColor = "#EAEAEA",
                    SecondaryTextColor = "#9D9D9D"
                }
            };
        }
    }
}
