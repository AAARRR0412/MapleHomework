using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using MapleHomework.Services;
using MapleHomework.ViewModels;
using MapleHomework.Models;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;

namespace MapleHomework
{
    /// <summary>
    /// 요일별 통계 항목
    /// </summary>
    public class DayOfWeekStatItem
    {
        public string DayName { get; set; } = "";
        public double Percentage { get; set; }
        public double BarHeight => Math.Max(5, Percentage * 1.2); // 최소 5px, 최대 120px
        public Brush BarColor => Percentage >= 80 ? new SolidColorBrush(Color.FromRgb(76, 217, 100)) :
                                  Percentage >= 50 ? new SolidColorBrush(Color.FromRgb(255, 149, 0)) :
                                  new SolidColorBrush(Color.FromRgb(255, 59, 48));
    }

    public class GuideLineItem
    {
        public string Label { get; set; } = "";
        public double Rate { get; set; } // 0~1
    }

    public class HexaSkillSummary
    {
        public string SkillName { get; set; } = "";
        public string SkillIcon { get; set; } = "";
        public int StartLevel { get; set; }
        public int CurrentLevel { get; set; }
        public int Gain => Math.Max(0, CurrentLevel - StartLevel);
        public double BarRate { get; set; } // 0~1, UI용
        public string LevelText => $"Lv.{StartLevel} → Lv.{CurrentLevel}";
        public string GainText => Gain > 0 ? $"+{Gain}" : "+0";
    }

    /// <summary>
    /// 일일 기록 항목
    /// </summary>
    public class DailyRecordItem
    {
        public string DateText { get; set; } = "";
        public string CharacterName { get; set; } = "";
        public string CompletionText { get; set; } = "";
        public double CompletionRate { get; set; }
    }

    public partial class ReportWindow : Wpf.Ui.Controls.FluentWindow, INotifyPropertyChanged
    {
        private readonly MainViewModel _viewModel;
        private readonly MapleApiService _apiService;
        private bool _isInitializing;
        private Popup? _itemTooltipPopup;

        public ReportWindow(MainViewModel mainViewModel)
        {
            _isInitializing = true; // 초기화 시작 플래그 설정
            InitializeComponent();
            DataContext = this;
            _viewModel = mainViewModel;
            _apiService = new MapleApiService();
            _itemTooltipPopup = this.FindName("ItemTooltipPopup") as Popup;

            // 테마 변경 이벤트 구독
            _viewModel.ThemeChanged += OnThemeChanged;

            // 테마 적용 (라이트/다크)
            ApplyThemeResources();

            InitializeSelectors();

            // 팝업 위치 갱신: 창 이동/크기 변경 시 따라가도록
            this.LocationChanged += (_, __) => UpdatePopupPosition();
            this.SizeChanged += (_, __) => UpdatePopupPosition();

            _isInitializing = false; // 초기화 완료 플래그 해제
            LoadDashboard(); // 초기 리포트 로드
        }

        private void OnThemeChanged()
        {
            ApplyThemeResources();
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.ThemeChanged -= OnThemeChanged;
            base.OnClosed(e);
        }

        #region Properties

        private double _averageCompletionRate;
        public double AverageCompletionRate
        {
            get => _averageCompletionRate;
            set { _averageCompletionRate = value; OnPropertyChanged(); }
        }

        private int _activeDays;
        public int ActiveDays
        {
            get => _activeDays;
            set { _activeDays = value; OnPropertyChanged(); }
        }

        private string _totalRewardText = "0";
        public string TotalRewardText
        {
            get => _totalRewardText;
            set { _totalRewardText = value; OnPropertyChanged(); }
        }

        private string _mostProductiveDayText = "-";
        public string MostProductiveDayText
        {
            get => _mostProductiveDayText;
            set { _mostProductiveDayText = value; OnPropertyChanged(); }
        }

        public ObservableCollection<DayOfWeekStatItem> DayOfWeekStats { get; set; } = new();
        public ObservableCollection<DailyRecordItem> DailyRecordItems { get; set; } = new();
        public ObservableCollection<HexaSkillSummary> HexaSkillSummaries { get; set; } = new();

        #endregion

        private void InitializeSelectors()
        {
            // 캐릭터 필터: 현재 ViewModel의 캐릭터 목록 사용
            CharacterFilterCombo.Items.Clear();
            CharacterFilterCombo.Items.Add(new ComboBoxItem { Content = "전체 캐릭터", Tag = null });

            foreach (var character in _viewModel.Characters)
            {
                CharacterFilterCombo.Items.Add(new ComboBoxItem { Content = character.Nickname, Tag = character.Id });
            }

            // 현재 선택된 캐릭터로 초기 선택 설정
            if (_viewModel.SelectedCharacter != null)
            {
                var target = CharacterFilterCombo.Items
                    .OfType<ComboBoxItem>()
                    .FirstOrDefault(i => (i.Tag as string) == _viewModel.SelectedCharacter.Id);
                if (target != null)
                {
                    CharacterFilterCombo.SelectedItem = target;
                }
                else if (CharacterFilterCombo.Items.Count > 1)
                {
                    CharacterFilterCombo.SelectedIndex = 1; // 첫 번째 캐릭터
                }
            }
            else if (CharacterFilterCombo.Items.Count > 1)
            {
                CharacterFilterCombo.SelectedIndex = 1;
            }
            else
            {
                CharacterFilterCombo.SelectedIndex = 0; // 전체 캐릭터
            }
        }

        private void LoadDashboard()
        {
            if (_isInitializing || GrowthReportSection == null || NoDataPanel == null)
                return;

            LoadGrowthReport();

            GrowthReportSection.Visibility = Visibility.Visible;
        }

        private void LoadGrowthReport()
        {
            string? characterId = GetSelectedCharacterId();
            if (string.IsNullOrEmpty(characterId))
            {
                characterId = _viewModel.SelectedCharacter?.Id;
            }
            if (string.IsNullOrEmpty(characterId))
            {
                // 선택된 캐릭터가 없으면 UI 초기화
                ClearGrowthReportData();
                return;
            }
            
            int days = 30; // 기본값
            if (HistoryDaysCombo.SelectedItem is ComboBoxItem selectedDaysItem)
            {
                if (selectedDaysItem.Tag != null && int.TryParse(selectedDaysItem.Tag.ToString(), out int parsedDays))
                {
                    days = parsedDays;
                }
            }

            // 경험치
            var expGrowth = StatisticsService.GetExperienceGrowth(characterId, days);
            ExpGrowthList.ItemsSource = expGrowth;
            
            // 주간 평균 경험치를 한국어 포맷으로 표시
            var weeklyExpGain = StatisticsService.GetWeeklyAverageExpGain(characterId);
            WeeklyExpGrowthText.Text = weeklyExpGain > 0 ? Data.ExpTable.FormatExpKorean(weeklyExpGain) : "-";
            
            // 레벨업 예상 날짜
            EstimatedLevelUpText.Text = StatisticsService.GetEstimatedLevelUpDateFormatted(characterId);
            
            // 전투력
            var combatPowerGrowth = StatisticsService.GetCombatPowerGrowth(characterId, days);
            CombatPowerList.ItemsSource = combatPowerGrowth;
            NoCombatPowerDataText.Visibility = combatPowerGrowth.Any() ? Visibility.Collapsed : Visibility.Visible;

            // 6차 스킬
            var hexaHistory = StatisticsService.GetHexaSkillHistory(characterId, 50);
            // 마스터리 코어 등 동시 강화되는 스킬은 날짜/레벨 기준으로 묶어서 표시
            var groupedHexa = hexaHistory
                .GroupBy(h => new { h.Date.Date, h.OldLevel, h.NewLevel })
                .Select(g => new HexaSkillRecord
                {
                    Date = g.First().Date,
                    CharacterId = g.First().CharacterId,
                    CharacterName = g.First().CharacterName,
                    OldLevel = g.Key.OldLevel,
                    NewLevel = g.Key.NewLevel,
                    SkillName = string.Join(" / ", g.Select(x => x.SkillName).Distinct()),
                    SkillIcon = g.FirstOrDefault(x => !string.IsNullOrEmpty(x.SkillIcon))?.SkillIcon ?? g.First().SkillIcon
                })
                .OrderByDescending(x => x.Date)
                .ToList();

            HexaSkillList.ItemsSource = groupedHexa;
            NoHexaSkillDataText.Visibility = groupedHexa.Any() ? Visibility.Collapsed : Visibility.Visible;

            // 6차 스킬 요약(시각화용)
            HexaSkillSummaries.Clear();
            var summary = hexaHistory
                .Where(h => !string.IsNullOrEmpty(h.SkillName))
                .GroupBy(h => h.SkillName)
                .Select(g =>
                {
                    int start = g.Min(x => x.OldLevel > 0 ? x.OldLevel : x.NewLevel);
                    int current = g.Max(x => x.NewLevel);
                    string icon = g.LastOrDefault(x => !string.IsNullOrEmpty(x.SkillIcon))?.SkillIcon ?? "";
                    return new HexaSkillSummary
                    {
                        SkillName = g.Key,
                        SkillIcon = icon,
                        StartLevel = start,
                        CurrentLevel = current
                    };
                })
                .OrderByDescending(s => s.Gain)
                .ThenByDescending(s => s.CurrentLevel)
                .ToList();

            double maxGain = summary.Any() ? summary.Max(s => s.Gain) : 0;
            foreach (var s in summary)
            {
                s.BarRate = maxGain > 0 ? Math.Max(0.1, (double)s.Gain / maxGain) : 0.1;
                HexaSkillSummaries.Add(s);
            }

            // 장비 변경
            var itemHistory = StatisticsService.GetItemChangeHistory(characterId, 50);
            ItemChangeList.ItemsSource = itemHistory;
            NoItemChangeDataText.Visibility = itemHistory.Any() ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ClearGrowthReportData()
        {
            ExpGrowthList.ItemsSource = null;
            CombatPowerList.ItemsSource = null;
            HexaSkillList.ItemsSource = null;
            ItemChangeList.ItemsSource = null;
            
            WeeklyExpGrowthText.Text = "-";
            EstimatedLevelUpText.Text = "-";

            NoCombatPowerDataText.Visibility = Visibility.Visible;
            NoHexaSkillDataText.Visibility = Visibility.Visible;
            NoItemChangeDataText.Visibility = Visibility.Visible;
        }

        private string? GetSelectedCharacterId()
        {
            if (CharacterFilterCombo.SelectedIndex > 0 && CharacterFilterCombo.SelectedItem is ComboBoxItem charItem)
            {
                return charItem.Tag as string;
            }
            return null;
        }

        private string GetKoreanDayName(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => "월",
                DayOfWeek.Tuesday => "화",
                DayOfWeek.Wednesday => "수",
                DayOfWeek.Thursday => "목",
                DayOfWeek.Friday => "금",
                DayOfWeek.Saturday => "토",
                DayOfWeek.Sunday => "일",
                _ => ""
            };
        }

        #region Event Handlers

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                // 더블클릭: 최대화/복원
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else if (e.ButtonState == MouseButtonState.Pressed)
            {
                // 드래그: 창 이동
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CharacterFilterCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            LoadDashboard();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDashboard();
            MessageBox.Show("데이터가 새로고침되었습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 전체 타이틀 영역 드래그 이동/더블클릭 최대화
        private void TitleBarBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                return;
            }

            try
            {
                DragMove();
            }
            catch
            {
                // ignore
            }
        }

        private void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("성장 리포트 캐시(경험치/유니온/전투력/헥사/장비 기록)를 모두 삭제할까요?\n복구할 수 없습니다.",
                "캐시 삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            StatisticsService.ClearGrowthData();
            LoadDashboard();
            MessageBox.Show("성장 리포트 캐시가 삭제되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void CollectHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var appData = CharacterRepository.Load();
            string? apiKey = appData.ApiKey;
            string? characterId = (CharacterFilterCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string;

            // config.json과 characters_data.json의 API 키가 다를 경우 최신 설정값으로 동기화
            var settings = ConfigManager.Load();
            if (!string.IsNullOrWhiteSpace(settings.ApiKey) && settings.ApiKey != apiKey)
            {
                apiKey = settings.ApiKey;
                appData.ApiKey = apiKey;
                CharacterRepository.Save(appData);
            }

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(characterId))
            {
                MessageBox.Show("API 키가 설정되어 있고, 캐릭터가 선택되어 있는지 확인해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int days = 30; // 기본값
            if (HistoryDaysCombo.SelectedItem is System.Windows.Controls.ComboBoxItem selectedDaysItem)
            {
                if (selectedDaysItem.Tag != null && int.TryParse(selectedDaysItem.Tag.ToString(), out int parsedDays))
                {
                    days = parsedDays;
                }
            }

            var character = _viewModel.Characters.FirstOrDefault(c => c.Id == characterId);
            if (character == null || string.IsNullOrEmpty(character.Ocid))
            {
                MessageBox.Show("선택된 캐릭터의 OCID 정보가 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CollectHistoryButton.IsEnabled = false;
            var oldButtonContent = CollectHistoryButton.Content;
            CollectHistoryButton.Content = "수집 중...";
            CollectProgressBar.Visibility = Visibility.Visible;
            CollectProgressBar.IsIndeterminate = true;
            CollectProgressBar.Value = 0;
            CollectStatusText.Text = "데이터 수집 중...";

            try
            {
                var progress = new Progress<int>(p => CollectProgressBar.Value = p);

                var result = await _apiService.CollectGrowthHistoryAsync(apiKey, character.Ocid, characterId, character.Nickname, days, progress);

                if (result.Success)
                {
                    MessageBox.Show(result.Message, "수집 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadGrowthReport(); // 데이터 갱신
                }
                else
                {
                    MessageBox.Show(result.Message, "수집 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 수집 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CollectHistoryButton.IsEnabled = true;
                CollectHistoryButton.Content = oldButtonContent;
                CollectProgressBar.Visibility = Visibility.Collapsed;
                CollectProgressBar.IsIndeterminate = false;
                CollectStatusText.Text = "";
            }
        }

        // 주의: 동일 시그니처 핸들러가 중복되지 않도록 하나만 유지
        private void ItemChangeBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_itemTooltipPopup == null) return;
            if (sender is not Border border) return;

            // 팝업을 창 외곽 오른쪽 상단에 고정, 사라지지 않음
            _itemTooltipPopup.IsOpen = false;
            _itemTooltipPopup.DataContext = border.DataContext;
            _itemTooltipPopup.Placement = PlacementMode.Absolute;
            _itemTooltipPopup.PlacementTarget = null; // 절대 좌표 사용
            _itemTooltipPopup.StaysOpen = true;

            UpdatePopupPosition();
            _itemTooltipPopup.IsOpen = true;
            e.Handled = true;
        }

        private void UpdatePopupPosition()
        {
            if (_itemTooltipPopup == null) return;

            // 창의 스크린 좌표 기준으로 우측 상단에 붙여 배치
            double offsetX = this.Left + this.ActualWidth + 8; // 창 오른쪽에서 약간 띄움
            double offsetY = this.Top + 16;                     // 상단에서 약간 띄움

            _itemTooltipPopup.HorizontalOffset = offsetX;
            _itemTooltipPopup.VerticalOffset = offsetY;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// 라이트/다크 테마 리소스 적용
        /// </summary>
        public void ApplyThemeResources()
        {
            var settings = ConfigManager.Load();
            bool isDark = ThemeService.ShouldUseDarkTheme(settings);

            void SetBrush(string key, Color color)
            {
                if (Resources[key] is SolidColorBrush existing)
                {
                    if (existing.IsFrozen)
                    {
                        Resources[key] = new SolidColorBrush(color);
                    }
                    else
                    {
                        existing.Color = color;
                    }
                }
                else
                {
                    Resources[key] = new SolidColorBrush(color);
                }
            }

            if (isDark)
            {
                // 다크 모드
                SetBrush("WindowBackground", Color.FromRgb(24, 28, 40));
                SetBrush("CardBackground", Color.FromRgb(38, 44, 58));
                SetBrush("CardBackgroundHover", Color.FromRgb(48, 56, 72));
                SetBrush("TextPrimary", Colors.White);
                SetBrush("TextSecondary", Color.FromRgb(169, 178, 195));
                SetBrush("TextMuted", Color.FromRgb(120, 128, 145));
                SetBrush("DividerColor", Color.FromRgb(55, 64, 78));
                SetBrush("ChartTextPrimary", Color.FromRgb(232, 237, 247));
                SetBrush("ChartTextSecondary", Color.FromRgb(174, 183, 200));
                SetBrush("ApiCardBackground", Color.FromRgb(42, 48, 62));
                SetBrush("ApiCardBorder", Color.FromRgb(64, 72, 90));
                MainGrid.Background = (Brush)Resources["WindowBackground"];
            }
            else
            {
                // 라이트 모드 (밝은 톤으로 전체 카드/배경 정리)
                SetBrush("WindowBackground", Color.FromRgb(245, 247, 251));
                SetBrush("CardBackground", Colors.White);
                SetBrush("CardBackgroundHover", Color.FromRgb(234, 238, 244));
                SetBrush("TextPrimary", Color.FromRgb(15, 23, 42));
                SetBrush("TextSecondary", Color.FromRgb(71, 85, 105));
                SetBrush("TextMuted", Color.FromRgb(100, 116, 139));
                SetBrush("DividerColor", Color.FromRgb(226, 232, 240));
                SetBrush("ChartTextPrimary", Color.FromRgb(15, 23, 42));
                SetBrush("ChartTextSecondary", Color.FromRgb(71, 85, 105));
                SetBrush("ApiCardBackground", Color.FromRgb(255, 246, 236));
                SetBrush("ApiCardBorder", Color.FromRgb(255, 227, 194));
                MainGrid.Background = (Brush)Resources["WindowBackground"];
            }
        }

        #endregion

        #region Scroll Handling
        private void InnerScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (MainScrollViewer == null) return;

            // 부모 스크롤뷰로 휠 이벤트 전달
            e.Handled = true;
            double offset = MainScrollViewer.VerticalOffset - e.Delta;
            MainScrollViewer.ScrollToVerticalOffset(offset);
        }
        #endregion
    }

}