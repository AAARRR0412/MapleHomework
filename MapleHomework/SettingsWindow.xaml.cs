using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MapleHomework.Models;
using MapleHomework.Services;
using MapleHomework.ViewModels;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace MapleHomework
{
    public partial class SettingsWindow : Window
    {
        private MainViewModel _viewModel;
        private bool _isInitializing = true;

        public SettingsWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;

            // 콤보박스 초기화
            InitializeComboBoxes();

            // 현재 선택된 캐릭터의 닉네임 표시
            var appData = CharacterRepository.Load();
            TxtApiKey.Text = appData.ApiKey ?? "";
            
            // 현재 선택된 캐릭터의 닉네임
            if (_viewModel.SelectedCharacter != null)
            {
                TxtNickname.Text = _viewModel.SelectedCharacter.Nickname ?? "";
            }
            
            // 자동 시작 상태 로드
            AutoStartToggle.IsChecked = ConfigManager.IsAutoStartEnabled();

            // 설정 로드
            var settings = ConfigManager.Load();

            // 테마 모드 로드
            switch (settings.ThemeMode)
            {
                case ThemeMode.Light:
                    ThemeModeCombo.SelectedIndex = 0;
                    break;
                case ThemeMode.Dark:
                    ThemeModeCombo.SelectedIndex = 1;
                    break;
                case ThemeMode.System:
                    ThemeModeCombo.SelectedIndex = 2;
                    break;
            }

            // 알림 설정 로드
            NotificationToggle.IsChecked = settings.IsNotificationEnabled;
            
            // 일일 알림 (n시간 전)
            NotifyDailyCheck.IsChecked = settings.NotifyDailyTasks;
            int dailyIndex = settings.DailyNotifyHoursBefore - 1;
            if (dailyIndex >= 0 && dailyIndex < 12)
                DailyHoursCombo.SelectedIndex = dailyIndex;
            
            // 주간/보스/월간 알림 (n일 전)
            NotifyWeeklyCheck.IsChecked = settings.NotifyWeeklyTasks;
            int weeklyIndex = settings.WeeklyNotifyDaysBefore - 1;
            if (weeklyIndex >= 0 && weeklyIndex < 6)
                WeeklyDaysCombo.SelectedIndex = weeklyIndex;
            
            NotifyBossCheck.IsChecked = settings.NotifyBossTasks;
            int bossIndex = settings.BossNotifyDaysBefore - 1;
            if (bossIndex >= 0 && bossIndex < 6)
                BossDaysCombo.SelectedIndex = bossIndex;
            
            NotifyMonthlyCheck.IsChecked = settings.NotifyMonthlyTasks;
            int monthlyIndex = settings.MonthlyNotifyDaysBefore - 1;
            if (monthlyIndex >= 0 && monthlyIndex < 10)
                MonthlyDaysCombo.SelectedIndex = monthlyIndex;

            // 시작 시 팝업 설정
            StartupPopupToggle.IsChecked = settings.ShowStartupPopup;

            // 오버레이 설정 로드
            OverlayToggle.IsChecked = settings.IsOverlayEnabled;
            ShowOnlyFavoritesToggle.IsChecked = settings.ShowOnlyFavorites;
            OverlayProcessNameBox.Text = settings.OverlayProcessName ?? "MapleStory";
            
            // 투명도 슬라이더 초기화 (0.0~1.0 → 0~100)
            OverlayOpacitySlider.Value = settings.OverlayOpacity * 100;
            OpacityValueText.Text = $"{(int)(settings.OverlayOpacity * 100)}%";

            // 알림 패널 활성화 상태
            UpdateNotificationDetailPanel();
            
            _isInitializing = false;
        }

        private void InitializeComboBoxes()
        {
            // 테마 콤보박스 아이템 추가 (XAML 대신 코드에서)
            ThemeModeCombo.Items.Add(new ComboBoxItem { Content = "☀️ 라이트 모드", Tag = "Light" });
            ThemeModeCombo.Items.Add(new ComboBoxItem { Content = "🌙 다크 모드", Tag = "Dark" });
            ThemeModeCombo.Items.Add(new ComboBoxItem { Content = "🖥️ 시스템 설정 따라가기", Tag = "System" });

            // 일일: 1~12시간 전
            DailyHoursCombo.ItemsSource = Enumerable.Range(1, 12).ToList();
            
            // 주간/보스: 1~6일 전
            WeeklyDaysCombo.ItemsSource = Enumerable.Range(1, 6).ToList();
            BossDaysCombo.ItemsSource = Enumerable.Range(1, 6).ToList();
            
            // 월간: 1~10일 전
            MonthlyDaysCombo.ItemsSource = Enumerable.Range(1, 10).ToList();
        }

        private void UpdateNotificationDetailPanel()
        {
            bool isEnabled = NotificationToggle.IsChecked == true;
            NotificationDetailPanel.Opacity = isEnabled ? 1.0 : 0.5;
            NotificationDetailPanel.IsEnabled = isEnabled;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ThemeModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            SaveSettings();

            // 테마 즉시 적용
            var settings = ConfigManager.Load();
            bool isDark = ThemeService.ShouldUseDarkTheme(settings);
            ThemeService.ApplyTheme(isDark, settings.CurrentCustomTheme);
        }

        private void AutoStartToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            bool isEnabled = AutoStartToggle.IsChecked == true;
            ConfigManager.SetAutoStart(isEnabled);

            // AppData에도 저장
            var appData = CharacterRepository.Load();
            appData.AutoStartEnabled = isEnabled;
            CharacterRepository.Save(appData);
        }

        private void NotificationToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            UpdateNotificationDetailPanel();
            SaveSettings();
        }

        private void StartupPopupToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            SaveSettings();
        }

        private void OverlayToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            SaveSettings();
        }

        private void ShowOnlyFavoritesToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            SaveSettings();
        }

        private void OverlayOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            
            int percent = (int)OverlayOpacitySlider.Value;
            OpacityValueText.Text = $"{percent}%";
            SaveSettings();
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = BackupService.GetBackupFileFilter(),
                DefaultExt = BackupService.GetBackupExtension(),
                FileName = $"MapleHomework_Backup_{System.DateTime.Now:yyyyMMdd}"
            };

            if (dialog.ShowDialog() == true)
            {
                var result = BackupService.CreateBackup(dialog.FileName, "수동 백업");
                if (result.IsSuccess)
                {
                    MessageBox.Show($"백업이 생성되었습니다.\n{result.Value}", "백업 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"백업 생성 실패: {result.ErrorMessage}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = BackupService.GetBackupFileFilter()
            };

            if (dialog.ShowDialog() == true)
            {
                // 메타데이터 확인
                var metaResult = BackupService.ReadBackupMetadata(dialog.FileName);
                if (metaResult.IsFailure)
                {
                    MessageBox.Show($"백업 파일을 읽을 수 없습니다: {metaResult.ErrorMessage}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var meta = metaResult.Value!;
                var confirmResult = MessageBox.Show(
                    $"다음 백업을 복원하시겠습니까?\n\n" +
                    $"생성일: {meta.CreatedAt:yyyy-MM-dd HH:mm}\n" +
                    $"캐릭터 수: {meta.CharacterCount}개\n" +
                    $"설명: {meta.Description ?? "없음"}\n\n" +
                    $"⚠️ 현재 데이터가 덮어쓰기됩니다!",
                    "백업 복원 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    var result = BackupService.RestoreFromBackup(dialog.FileName);
                    if (result.IsSuccess)
                    {
                        MessageBox.Show("백업이 복원되었습니다.\n프로그램을 재시작해주세요.", "복원 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                        System.Windows.Application.Current.Shutdown();
                    }
                    else
                    {
                        MessageBox.Show($"복원 실패: {result.ErrorMessage}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void SaveSettings()
        {
            var settings = ConfigManager.Load();

            // 테마 설정
            if (ThemeModeCombo.SelectedItem is ComboBoxItem themeItem)
            {
                settings.ThemeMode = themeItem.Tag?.ToString() switch
                {
                    "Light" => ThemeMode.Light,
                    "Dark" => ThemeMode.Dark,
                    "System" => ThemeMode.System,
                    _ => ThemeMode.System
                };
            }
            
            // 레거시 필드 동기화
            settings.IsDarkTheme = ThemeService.ShouldUseDarkTheme(settings);

            // 알림 설정
            settings.IsNotificationEnabled = NotificationToggle.IsChecked == true;
            
            // 일일 알림 (n시간 전)
            settings.NotifyDailyTasks = NotifyDailyCheck.IsChecked == true;
            if (DailyHoursCombo.SelectedItem is int dailyHours)
                settings.DailyNotifyHoursBefore = dailyHours;
            
            // 주간 알림 (n일 전)
            settings.NotifyWeeklyTasks = NotifyWeeklyCheck.IsChecked == true;
            if (WeeklyDaysCombo.SelectedItem is int weeklyDays)
                settings.WeeklyNotifyDaysBefore = weeklyDays;
            
            // 보스 알림 (n일 전)
            settings.NotifyBossTasks = NotifyBossCheck.IsChecked == true;
            if (BossDaysCombo.SelectedItem is int bossDays)
                settings.BossNotifyDaysBefore = bossDays;
            
            // 월간 알림 (n일 전)
            settings.NotifyMonthlyTasks = NotifyMonthlyCheck.IsChecked == true;
            if (MonthlyDaysCombo.SelectedItem is int monthlyDays)
                settings.MonthlyNotifyDaysBefore = monthlyDays;

            // 시작 시 팝업
            settings.ShowStartupPopup = StartupPopupToggle.IsChecked == true;

            // 오버레이 설정
            settings.IsOverlayEnabled = OverlayToggle.IsChecked == true;
            settings.ShowOnlyFavorites = ShowOnlyFavoritesToggle.IsChecked == true;
            
            // 프로세스 이름 저장
            string processName = OverlayProcessNameBox.Text.Trim();
            if (!string.IsNullOrEmpty(processName))
            {
                settings.OverlayProcessName = processName;
            }

            // 투명도 저장 (0~100 → 0.0~1.0)
            settings.OverlayOpacity = OverlayOpacitySlider.Value / 100.0;

            ConfigManager.Save(settings);
            
            // 테마 변경 알림 (다른 윈도우들 업데이트)
            _viewModel.NotifyThemeChanged();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = TxtApiKey.Text.Trim();
            string nickname = TxtNickname.Text.Trim();

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(nickname))
            {
                MessageBox.Show("API 키와 닉네임을 모두 입력해주세요.", "오류");
                return;
            }

            // 현재 선택된 캐릭터의 닉네임 업데이트
            if (_viewModel.SelectedCharacter != null)
            {
                _viewModel.SelectedCharacter.Nickname = nickname;
            }

            // ViewModel 및 AppData 동기화 (in-memory 데이터가 오래된 값을 덮어쓰지 않도록)
            _viewModel.UpdateApiKeyAndAutoStart(apiKey, AutoStartToggle.IsChecked == true);

            // config.json에도 동기화 (API 키 / 대표 캐릭터)
            var settings = ConfigManager.Load();
            settings.ApiKey = apiKey;
            settings.CharacterName = nickname;
            ConfigManager.Save(settings);

            // 설정도 저장
            SaveSettings();

            // API에서 캐릭터 정보 로드
            await _viewModel.LoadCharacterDataFromApi(apiKey, nickname);
            
            MessageBox.Show("성공적으로 저장되었습니다!", "알림");
            this.Close();
        }
    }
}
