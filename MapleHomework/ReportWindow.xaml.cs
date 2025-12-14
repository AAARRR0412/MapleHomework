using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WpfButton = System.Windows.Controls.Button;
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

    /// <summary>
    /// 누락 날짜 선택 항목
    /// </summary>
    public class MissingDateItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        
        public DateTime Date { get; set; }
        public string DateText => Date.ToString("MM/dd (ddd)");
        
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public partial class ReportWindow : Wpf.Ui.Controls.FluentWindow, INotifyPropertyChanged
    {
        private readonly MainViewModel _viewModel;
        private readonly MapleApiService _apiService;
        private bool _isInitializing;
        private Popup? _itemTooltipPopup;
        private DateTime? _collectStartTime;
        private string _expRangeMode = "day";
        private int _chartDays = 30; // 차트 조회 기간 (기본 30일, 0은 전체)
        private List<MissingDateItem> _missingDates = new(); // 누락 날짜 목록
        
        // 캘린더 관련 필드
        private DateTime _calendarDisplayMonth = DateTime.Today;
        private HashSet<DateTime> _collectedDates = new(); // 데이터 수집된 날짜
        private HashSet<DateTime> _selectedDates = new(); // 사용자가 선택한 날짜
        private DateTime? _firstClickDate = null; // 오셀로 스타일: 첫 번째 클릭 날짜
        private DateTime? _lastCollectedDate = null; // 마지막 수집된 날짜 (연속 수집용)
        
        // 장비 변경 내역 필터 상태
        private List<ItemChangeRecord> _allItemChangeRecords = new(); // 전체 장비 변경 내역 (필터링 전)
        private bool _showPresetItems = false; // 프리셋 아이템 표시 여부
        private bool _showSpiritPendant = false; // 정령의 펜던트 표시 여부
        private HashSet<string> _enabledChangeTypes = new HashSet<string> { "장착", "교체", "옵션 변경" }; // 활성화된 변경 타입
        private int _itemDateRangeDays = 0; // 날짜 범위 (0 = 전체)

        private class HexaCoreItem
        {
            public string SkillName { get; set; } = "";
            public string SkillIcon { get; set; } = "";
            public int OldLevel { get; set; }
            public int NewLevel { get; set; }
            public int CoreLevel { get; set; }
            public string BadgeIcon { get; set; } = "";
            public string CoreType { get; set; } = "";
            public string OriginalName { get; set; } = "";
        }

        private class HexaCoreGroup
        {
            public string TypeLabel { get; set; } = "";
            public string TypeIcon { get; set; } = "";
            public List<HexaCoreItem> Items { get; set; } = new();
        }

        private class HexaSkillDailyGroup
        {
            public DateTime Date { get; set; }
            public List<HexaSkillRecord> Items { get; set; } = new();
        }

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
            
            // Loaded 이벤트에서도 테마 적용 (XAML 파싱 완료 후 DynamicResource 경고 방지)
            this.Loaded += (s, e) => ApplyThemeResources();

            InitializeSelectors();

            UpdateRangeButtonsVisual();

            // 팝업 위치 갱신: 창 이동/크기 변경 시 따라가도록
            this.LocationChanged += (_, __) => UpdatePopupPosition();
            this.SizeChanged += (_, __) => UpdatePopupPosition();

            _isInitializing = false; // 초기화 완료 플래그 해제
            _ = RefreshIfStaleAndLoadAsync(); // 자동 최신화 후 로드
        }

        private void OnThemeChanged()
        {
            ApplyThemeResources();
        }

        private void SetExpRange(string mode)
        {
            _expRangeMode = mode;
            UpdateRangeButtonsVisual();
            LoadGrowthReport();
        }

        private void UpdateRangeButtonsVisual()
        {
            void Mark(WpfButton? btn, bool active)
            {
                if (btn == null) return;
                btn.FontWeight = active ? FontWeights.Bold : FontWeights.Normal;
                btn.Opacity = active ? 1.0 : 0.7;
            }

            Mark(ExpDayBtn, _expRangeMode == "day");
            Mark(ExpWeekBtn, _expRangeMode == "week");
            Mark(ExpMonthBtn, _expRangeMode == "month");
        }

        private void ExpRangeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is WpfButton btn && btn.Tag is string mode)
            {
                SetExpRange(mode);
                
                // 1일 선택 시에만 기간 드롭다운 표시
                if (ChartDaysCombo != null)
                {
                    ChartDaysCombo.Visibility = mode == "day" ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private void ChartDaysCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            
            if (ChartDaysCombo?.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
            {
                if (selectedItem.Tag != null && int.TryParse(selectedItem.Tag.ToString(), out int days))
                {
                    _chartDays = days;
                    LoadGrowthReport();
                }
            }
        }

        private void UpdateDataSummary()
        {
            // 현재 선택된 캐릭터 ID 가져오기
            string? characterId = GetSelectedCharacterId() ?? _viewModel.SelectedCharacter?.Id;
            
            if (string.IsNullOrEmpty(characterId))
            {
                // 캐릭터가 선택되지 않은 경우 빈 데이터 표시
                if (DataSummaryText != null)
                {
                    DataSummaryText.Text = "0일";
                }
                if (MissingDaysText != null)
                {
                    MissingDaysText.Text = "-";
                }
                _collectedDates = new HashSet<DateTime>();
                _lastCollectedDate = null;
                RenderCalendar();
                return;
            }
            
            // 캐릭터별 수집된 날짜 목록 가져오기 (검증된 데이터만)
            _collectedDates = GetVerifiedCollectedDates(characterId);
            
            // 마지막 수집 날짜 설정 (연속 수집 검증용)
            _lastCollectedDate = _collectedDates.Count > 0 ? _collectedDates.Max() : (DateTime?)null;
            
            if (DataSummaryText != null)
            {
                DataSummaryText.Text = $"{_collectedDates.Count}일";
            }
            
            // 최근 90일 기준 누락 날짜 수
            var endDate = DateTime.Today.AddDays(-1);
            var startDate = endDate.AddDays(-89);
            var missingDates = StatisticsService.GetMissingDatesForCharacter(characterId, startDate, endDate);
            
            if (MissingDaysText != null)
            {
                MissingDaysText.Text = $"{missingDates.Count}일";
            }
            
            // 캘린더 업데이트
            RenderCalendar();
        }
        
        /// <summary>
        /// 검증된 수집 날짜 목록 반환 (부분 데이터 제거)
        /// </summary>
        private HashSet<DateTime> GetVerifiedCollectedDates(string characterId)
        {
            var allDates = StatisticsService.GetCollectedDatesForCharacter(characterId);
            var verifiedDates = new HashSet<DateTime>();
            
            foreach (var date in allDates)
            {
                // 필수 데이터가 있는지 확인 (basic, stat 등)
                if (StatisticsService.HasCompleteDataForDate(characterId, date))
                {
                    verifiedDates.Add(date);
                }
                else
                {
                    // 부분 데이터는 삭제 (오염된 데이터 정리)
                    StatisticsService.RemoveIncompleteDataForDate(characterId, date);
                }
            }
            
            return verifiedDates;
        }

        #region 캘린더 관련 메서드
        
        private void RenderCalendar()
        {
            if (CalendarGrid == null || CalendarMonthText == null) return;
            
            CalendarGrid.Children.Clear();
            CalendarMonthText.Text = _calendarDisplayMonth.ToString("yyyy년 M월");
            
            var firstDayOfMonth = new DateTime(_calendarDisplayMonth.Year, _calendarDisplayMonth.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            var startOffset = (int)firstDayOfMonth.DayOfWeek; // 일요일 = 0
            
            // 이전 달 빈 셀
            for (int i = 0; i < startOffset; i++)
            {
                CalendarGrid.Children.Add(new Border());
            }
            
            // 현재 달 날짜들
            for (int day = 1; day <= lastDayOfMonth.Day; day++)
            {
                var date = new DateTime(_calendarDisplayMonth.Year, _calendarDisplayMonth.Month, day);
                var btn = CreateCalendarDayButton(date);
                CalendarGrid.Children.Add(btn);
            }
            
            // 다음 달 빈 셀 (42개 = 6주 * 7일)
            while (CalendarGrid.Children.Count < 42)
            {
                CalendarGrid.Children.Add(new Border());
            }
            
            UpdateSelectedDatesCount();
        }
        
        private Border CreateCalendarDayButton(DateTime date)
        {
            var border = new Border
            {
                Margin = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = date
            };
            
            var textBlock = new TextBlock
            {
                Text = date.Day.ToString(),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                FontSize = 10,
                Padding = new Thickness(0, 4, 0, 4)
            };
            
            bool isCollected = _collectedDates.Contains(date);
            bool isSelected = _selectedDates.Contains(date);
            bool isFuture = date >= DateTime.Today;
            bool isTooOld = date < DateTime.Today.AddDays(-365); // 1년 이상 오래된 날짜
            
            if (isFuture || isTooOld)
            {
                // 미래 또는 너무 오래된 날짜 - 비활성화
                border.Background = new SolidColorBrush(Color.FromArgb(30, 100, 100, 100));
                textBlock.Foreground = new SolidColorBrush(Color.FromArgb(100, 100, 100, 100));
                border.IsEnabled = false;
            }
            else if (isCollected)
            {
                // 데이터 있음 - 파란색 (선택 불가 - API 중복 호출 방지)
                border.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)); // #3B82F6
                textBlock.Foreground = System.Windows.Media.Brushes.White;
                border.Cursor = System.Windows.Input.Cursors.Arrow; // 일반 커서
                border.ToolTip = "이미 수집된 날짜입니다";
                // 이벤트 연결하지 않음 - 선택 불가
            }
            else if (isSelected)
            {
                // 선택됨 - 초록색
                border.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // #10B981
                textBlock.Foreground = System.Windows.Media.Brushes.White;
                textBlock.FontWeight = FontWeights.Bold;
                AttachCalendarDayEvents(border);
            }
            else
            {
                // 데이터 없음 (누락) - 빨간색
                border.Background = new SolidColorBrush(Color.FromArgb(60, 244, 63, 94)); // #F43F5E 30%
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(244, 63, 94));
                textBlock.FontWeight = FontWeights.SemiBold;
                AttachCalendarDayEvents(border);
            }
            
            // 오늘 표시
            if (date.Date == DateTime.Today.AddDays(-1)) // 어제 (수집 가능한 가장 최근 날짜)
            {
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(90, 200, 250)); // AccentCyan
                border.BorderThickness = new Thickness(2);
            }
            
            border.Child = textBlock;
            return border;
        }
        
        private void AttachCalendarDayEvents(Border border)
        {
            border.MouseLeftButtonDown += CalendarDay_MouseDown;
        }
        
        private void CalendarDay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is DateTime date)
            {
                // 이미 수집된 날짜는 선택 불가
                if (_collectedDates.Contains(date)) return;
                
                // 오셀로 스타일 선택
                if (_firstClickDate == null)
                {
                    // 첫 번째 클릭: 시작점 설정
                    _firstClickDate = date;
                    _selectedDates.Clear();
                    _selectedDates.Add(date);
                }
                else
                {
                    // 두 번째 클릭: 범위 선택
                    var startDate = _firstClickDate.Value < date ? _firstClickDate.Value : date;
                    var endDate = _firstClickDate.Value > date ? _firstClickDate.Value : date;
                    
                    // 선택 범위 계산 (수집되지 않은 날짜만)
                    var newSelection = new HashSet<DateTime>();
                    for (var d = startDate; d <= endDate; d = d.AddDays(1))
                    {
                        // 어제까지만 선택 가능
                        if (d >= DateTime.Today) continue;
                        // 이미 수집된 날짜 제외
                        if (_collectedDates.Contains(d)) continue;
                        
                        newSelection.Add(d);
                    }
                    
                    // 연속성 검증: 선택 범위 + 기존 수집 날짜가 연속되어야 함
                    if (_collectedDates.Any() && newSelection.Any())
                    {
                        var allDates = _collectedDates.Union(newSelection).OrderBy(d => d).ToList();
                        bool isContinuous = true;
                        for (int i = 1; i < allDates.Count; i++)
                        {
                            if ((allDates[i] - allDates[i - 1]).Days > 1)
                            {
                                isContinuous = false;
                                break;
                            }
                        }
                        
                        if (!isContinuous)
                        {
                            System.Windows.MessageBox.Show(
                                "선택한 날짜 범위가 기존 수집된 날짜와 연속되지 않습니다.\n빈 날짜 없이 연속된 범위만 선택할 수 있습니다.", 
                                "알림", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Information);
                            _firstClickDate = null;
                            return;
                        }
                    }
                    
                    _selectedDates.Clear();
                    foreach (var d in newSelection)
                    {
                        _selectedDates.Add(d);
                    }
                    
                    _firstClickDate = null; // 리셋
                }
                
                RenderCalendar();
                UpdateSelectedDatesCount();
            }
        }
        
        private void CalendarPrevMonth_Click(object sender, RoutedEventArgs e)
        {
            _calendarDisplayMonth = _calendarDisplayMonth.AddMonths(-1);
            RenderCalendar();
        }
        
        private void CalendarNextMonth_Click(object sender, RoutedEventArgs e)
        {
            _calendarDisplayMonth = _calendarDisplayMonth.AddMonths(1);
            RenderCalendar();
        }
        
        private void UpdateSelectedDatesCount()
        {
            if (_selectedDates.Count == 0)
            {
                if (SelectedStartDateText != null) SelectedStartDateText.Text = "-";
                if (SelectedEndDateText != null) SelectedEndDateText.Text = "-";
            }
            else if (_selectedDates.Count == 1)
            {
                var date = _selectedDates.First();
                if (SelectedStartDateText != null) SelectedStartDateText.Text = $"{date:yyyy-MM-dd}";
                if (SelectedEndDateText != null) SelectedEndDateText.Text = $"{date:yyyy-MM-dd}";
            }
            else
            {
                var minDate = _selectedDates.Min();
                var maxDate = _selectedDates.Max();
                if (SelectedStartDateText != null) SelectedStartDateText.Text = $"{minDate:yyyy-MM-dd}";
                if (SelectedEndDateText != null) SelectedEndDateText.Text = $"{maxDate:yyyy-MM-dd}";
            }
        }
        
        #endregion

        private void SelectAllMissing_Click(object sender, RoutedEventArgs e)
        {
            // 현재 보고 있는 달의 누락된 날짜만 선택
            var firstDayOfMonth = new DateTime(_calendarDisplayMonth.Year, _calendarDisplayMonth.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            
            // 어제까지만 (오늘과 미래는 제외)
            var yesterday = DateTime.Today.AddDays(-1);
            if (lastDayOfMonth > yesterday) lastDayOfMonth = yesterday;
            
            // 1년 이상 오래된 날짜도 제외
            var oneYearAgo = DateTime.Today.AddDays(-365);
            if (firstDayOfMonth < oneYearAgo) firstDayOfMonth = oneYearAgo;
            
            // 해당 달의 누락된 날짜만 선택 (이미 수집된 날짜 제외)
            for (var date = firstDayOfMonth; date <= lastDayOfMonth; date = date.AddDays(1))
            {
                if (!_collectedDates.Contains(date))
                {
                    _selectedDates.Add(date);
                }
            }
            RenderCalendar();
        }

        private void DeselectAllMissing_Click(object sender, RoutedEventArgs e)
        {
            _selectedDates.Clear();
            RenderCalendar();
        }

        private async Task RefreshIfStaleAndLoadAsync()
        {
            // 데이터 수집 현황 업데이트
            UpdateDataSummary();
            
            var last = StatisticsService.GetLastUpdated();
            
            // statistics_data.json 파일이 없을 때만 자동 새로고침 실행
            // (last가 DateTime.MinValue이면 파일이 없거나 비어있음)
            bool needRefresh = last == DateTime.MinValue;

            if (!needRefresh)
            {
                LoadDashboard();
                return;
            }

            string? characterId = GetSelectedCharacterId() ?? _viewModel.SelectedCharacter?.Id;
            var character = _viewModel.Characters.FirstOrDefault(c => c.Id == characterId);
            if (character == null || string.IsNullOrEmpty(character.Ocid))
            {
                LoadDashboard();
                return;
            }

            try
            {
                CollectProgressBar.Visibility = Visibility.Visible;
                CollectProgressBar.IsIndeterminate = true;
                CollectStatusText.Text = "자동 갱신 중...";
                await _apiService.CollectGrowthHistoryAsync(character.Ocid, character.Id, character.Nickname, 1, null);
            }
            catch
            {
                // 무시하고 진행
            }
            finally
            {
                CollectProgressBar.Visibility = Visibility.Collapsed;
                CollectStatusText.Text = string.Empty;
                LoadDashboard();
                UpdateDataSummary();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.ThemeChanged -= OnThemeChanged;
            
            // 백그라운드 수집 이벤트 구독 해제 (수집은 계속 진행됨)
            App.CollectProgressChanged -= OnCollectProgressChanged;
            App.CollectCompleted -= OnCollectCompleted;
            
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
                else if (CharacterFilterCombo.Items.Count > 0)
                {
                    CharacterFilterCombo.SelectedIndex = 0; // 첫 번째 캐릭터
                }
            }
            else if (CharacterFilterCombo.Items.Count > 0)
            {
                CharacterFilterCombo.SelectedIndex = 0;
            }
        }

        private void LoadDashboard()
        {
            if (_isInitializing || GrowthReportSection == null || NoDataPanel == null)
                return;

            // 리소스 먼저 적용 (DynamicResource 경고 방지)
            ApplyThemeResources();

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
            
            // 차트 조회 기간 (0이면 전체 데이터)
            int chartDays = _chartDays > 0 ? _chartDays : 365; // 전체 = 최대 365일

            // 경험치
            var expGrowth = StatisticsService.GetExperienceGrowth(characterId, chartDays, _expRangeMode);
            ExpGrowthList.ItemsSource = expGrowth;
            
            // 최근 30일 일간 평균 경험치 (경험치 + %)
            var dailyAvg = StatisticsService.GetMonthlyDailyAverageExpGain(characterId, 30);
            if (dailyAvg.ExpPerDay > 0 || Math.Abs(dailyAvg.PercentPerDay) > 0.001)
            {
                var expText = Data.ExpTable.FormatExpKorean(dailyAvg.ExpPerDay);
                var percText = $"{dailyAvg.PercentPerDay:F2}%";
                WeeklyExpGrowthText.Text = $"{expText} ({percText})";
            }
            else
            {
                WeeklyExpGrowthText.Text = "-";
            }
            
            // 레벨업 예상 날짜
            EstimatedLevelUpText.Text = StatisticsService.GetEstimatedLevelUpDateFormatted(characterId);
            
            // 전투력 (항상 전체 기간 조회 - 최고 기록 갱신 시점을 모두 표시)
            var combatPowerGrowth = StatisticsService.GetCombatPowerGrowth(characterId, 365, "day", onlyNewHigh: true);
            CombatPowerList.ItemsSource = combatPowerGrowth;
            NoCombatPowerDataText.Visibility = combatPowerGrowth.Any() ? Visibility.Collapsed : Visibility.Visible;
            
            // 전투력 최고 기록
            if (combatPowerGrowth.Any())
            {
                var maxPower = combatPowerGrowth.Max(c => c.NewPower);
                MaxCombatPowerText.Text = maxPower.ToString("N0");
            }
            else
            {
                MaxCombatPowerText.Text = "-";
            }

            // 6차 스킬 - 상세 내역 (히스토리)
            var hexaHistory = StatisticsService.GetHexaSkillHistory(characterId, 50);
            var hexaByDate = hexaHistory
                .GroupBy(h => h.Date.Date)
                .Select(g => new HexaSkillDailyGroup
                {
                    Date = g.Key,
                    Items = g.OrderByDescending(x => x.NewLevel).ToList()
                })
                .OrderByDescending(g => g.Date)
                .ToList();

            HexaSkillList.ItemsSource = hexaByDate;
            NoHexaSkillDataText.Visibility = hexaHistory.Any() ? Visibility.Collapsed : Visibility.Visible;

            // 6차 스킬 - 현황 (현재 코어 레벨)
            HexaCoreGroups.ItemsSource = null;
            _ = LoadHexaCoreCurrentAsync(characterId, hexaHistory);

            // 장비 변경
            var itemHistory = StatisticsService.GetItemChangeHistory(characterId, 50);
            _allItemChangeRecords = itemHistory; // 전체 데이터 저장
            ApplyItemFilters(); // 필터 적용
        }

        private void ClearGrowthReportData()
        {
            ExpGrowthList.ItemsSource = null;
            CombatPowerList.ItemsSource = null;
            HexaCoreGroups.ItemsSource = null;
            HexaSkillList.ItemsSource = null;
            ItemChangeList.ItemsSource = null;
            
            WeeklyExpGrowthText.Text = "-";
            EstimatedLevelUpText.Text = "-";

            NoCombatPowerDataText.Visibility = Visibility.Visible;
            NoHexaSkillDataText.Visibility = Visibility.Visible;
            NoItemChangeDataText.Visibility = Visibility.Visible;
        }

        private Task LoadHexaCoreCurrentAsync(string characterId, List<HexaSkillRecord> hexaHistory)
        {
            var character = _viewModel.Characters.FirstOrDefault(c => c.Id == characterId);
            if (character == null)
                return Task.CompletedTask;

            // 히스토리에서 아이콘 매핑 (스킬명 기준)
            var iconMap = hexaHistory
                .Where(h => !string.IsNullOrEmpty(h.SkillName) && !string.IsNullOrEmpty(h.SkillIcon))
                .GroupBy(h => h.SkillName!)
                .ToDictionary(g => g.Key, g => g.First().SkillIcon!);

            try
            {
                // 저장된 원본 데이터에서 가장 최근 데이터 로드
                var skillResp = RawDataProcessor.LoadLatestSkill6Info();
                var skillIconMap = (skillResp?.CharacterSkill ?? new List<CharacterSkillInfo>())
                    .Where(s => !string.IsNullOrEmpty(s.SkillName) && !string.IsNullOrEmpty(s.SkillIcon))
                    .GroupBy(s => s.SkillName!)
                    .ToDictionary(g => g.Key, g => g.First().SkillIcon!);

                var hexaStat = RawDataProcessor.LoadLatestHexaStatInfo();
                if (hexaStat?.HexaCoreEquipment == null) return Task.CompletedTask;

                string masteryBadge = "Data/Mastery.png";
                string skillBadge = "Data/Skill.png";
                string enhanceBadge = "Data/Enhance.png";
                string commonBadge = "Data/Common.png";

                string GetBadgeFromType(string? type) => type switch
                {
                    "마스터리 코어" => masteryBadge,
                    "강화 코어" => enhanceBadge,
                    "공용 코어" => commonBadge,
                    _ => skillBadge
                };

                string ResolveIcon(string coreName, List<LinkedSkillInfo>? linked)
                {
                    if (iconMap.TryGetValue(coreName, out var icon)) return icon;
                    if (skillIconMap.TryGetValue(coreName, out var iconSkill)) return iconSkill;

                    // 마스터리 코어: "A/B" 형태 → 첫 스킬명으로 아이콘 매칭
                    var firstName = coreName.Split(new[] { '/', ',' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(firstName))
                    {
                        if (iconMap.TryGetValue(firstName, out var iconSplit)) return iconSplit;
                        if (skillIconMap.TryGetValue(firstName, out var iconSkill2)) return iconSkill2;
                    }

                    if (linked != null)
                    {
                        foreach (var lk in linked)
                        {
                            var id = lk?.HexaSkillId;
                            if (string.IsNullOrEmpty(id)) continue;
                            if (iconMap.TryGetValue(id, out var icon2)) return icon2;
                            if (skillIconMap.TryGetValue(id, out var icon3)) return icon3;
                        }
                    }
                    return ""; // 없음
                }

                var coreItems = hexaStat.HexaCoreEquipment.Select(core => new HexaCoreItem
                {
                    SkillName = TrimCoreName(core.HexaCoreName ?? ""),
                    OriginalName = core.HexaCoreName ?? "",
                    SkillIcon = ResolveIcon(core.HexaCoreName ?? "", core.LinkedSkill),
                    OldLevel = core.HexaCoreLevel,
                    NewLevel = core.HexaCoreLevel,
                    CoreLevel = core.HexaCoreLevel,
                    BadgeIcon = GetBadgeFromType(core.HexaCoreType),
                    CoreType = string.IsNullOrEmpty(core.HexaCoreType) ? "스킬 코어" : core.HexaCoreType!
                }).ToList();

                string[] typeOrder = { "마스터리 코어", "스킬 코어", "강화 코어", "공용 코어" };
                var groups = coreItems
                    .GroupBy(c => c.CoreType)
                    .Select(g => new HexaCoreGroup
                    {
                        TypeLabel = g.Key,
                        TypeIcon = GetBadgeFromType(g.Key),
                        Items = g.OrderByDescending(x => x.CoreLevel).ToList()
                    })
                    .OrderBy(g =>
                    {
                        int idx = Array.IndexOf(typeOrder, g.TypeLabel);
                        return idx >= 0 ? idx : int.MaxValue;
                    })
                    .ThenBy(g => g.TypeLabel)
                    .ToList();

                HexaCoreGroups.ItemsSource = groups;
                
                // HEXA 스탯 로드
                LoadHexaStats();
            }
            catch
            {
                // 무시
            }
            
            return Task.CompletedTask;
        }
        
        private void LoadHexaStats()
        {
            try
            {
                // HEXA 스탯 코어 정보 가져오기 (hexamatrix-stat API 데이터)
                var hexaStatCore = RawDataProcessor.LoadLatestHexaMatrixStatInfo();
                
                var statCoreItems = new List<HexaStatCoreItem>();
                
                if (hexaStatCore != null)
                {
                    // character_hexa_stat_core
                    if (hexaStatCore.CharacterHexaStatCore?.Any() == true)
                    {
                        var core = hexaStatCore.CharacterHexaStatCore.First();
                        statCoreItems.Add(new HexaStatCoreItem
                        {
                            CoreLabel = "스탯 코어 1",
                            MainStatName = core.MainStatName ?? "-",
                            SubStatName1 = core.SubStatName1 ?? "-",
                            SubStatName2 = core.SubStatName2 ?? "-",
                            MainStatLevel = core.MainStatLevel,
                            SubStatLevel1 = core.SubStatLevel1,
                            SubStatLevel2 = core.SubStatLevel2,
                            StatGrade = core.StatGrade
                        });
                    }
                    
                    // character_hexa_stat_core_2
                    if (hexaStatCore.CharacterHexaStatCore2?.Any() == true)
                    {
                        var core = hexaStatCore.CharacterHexaStatCore2.First();
                        statCoreItems.Add(new HexaStatCoreItem
                        {
                            CoreLabel = "스탯 코어 2",
                            MainStatName = core.MainStatName ?? "-",
                            SubStatName1 = core.SubStatName1 ?? "-",
                            SubStatName2 = core.SubStatName2 ?? "-",
                            MainStatLevel = core.MainStatLevel,
                            SubStatLevel1 = core.SubStatLevel1,
                            SubStatLevel2 = core.SubStatLevel2,
                            StatGrade = core.StatGrade
                        });
                    }
                    
                    // character_hexa_stat_core_3
                    if (hexaStatCore.CharacterHexaStatCore3?.Any() == true)
                    {
                        var core = hexaStatCore.CharacterHexaStatCore3.First();
                        statCoreItems.Add(new HexaStatCoreItem
                        {
                            CoreLabel = "스탯 코어 3",
                            MainStatName = core.MainStatName ?? "-",
                            SubStatName1 = core.SubStatName1 ?? "-",
                            SubStatName2 = core.SubStatName2 ?? "-",
                            MainStatLevel = core.MainStatLevel,
                            SubStatLevel1 = core.SubStatLevel1,
                            SubStatLevel2 = core.SubStatLevel2,
                            StatGrade = core.StatGrade
                        });
                    }
                }
                
                if (HexaStatCoreList != null)
                {
                    HexaStatCoreList.ItemsSource = statCoreItems;
                }
                
                if (NoHexaStatText != null)
                {
                    NoHexaStatText.Visibility = statCoreItems.Any() ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            catch
            {
                if (NoHexaStatText != null) NoHexaStatText.Visibility = Visibility.Visible;
            }
        }
        
        private class HexaStatItem
        {
            public string StatName { get; set; } = "";
            public string StatValue { get; set; } = "";
        }
        
        private class HexaStatCoreItem
        {
            public string CoreLabel { get; set; } = "";
            public string MainStatName { get; set; } = "";
            public string SubStatName1 { get; set; } = "";
            public string SubStatName2 { get; set; } = "";
            public int MainStatLevel { get; set; }
            public int SubStatLevel1 { get; set; }
            public int SubStatLevel2 { get; set; }
            public int StatGrade { get; set; }
        }

        private static string CleanCoreName(string name)
        {
            return string.IsNullOrEmpty(name) ? name : name.Replace("/", ",\n");
        }

        private static string TrimCoreName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var idx = name.IndexOf('/');
            return idx >= 0 ? name.Substring(0, idx).Trim() : name;
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
            UpdateDataSummary(); // 캐릭터별 데이터 수집 현황 업데이트
            LoadDashboard();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // 리소스 먼저 적용 (DynamicResource 경고 방지)
            ApplyThemeResources();
            
            // 원본 데이터에서 장비 변경 내역 재처리
            string? characterId = GetSelectedCharacterId();
            if (string.IsNullOrEmpty(characterId))
            {
                characterId = _viewModel.SelectedCharacter?.Id;
            }
            
            if (!string.IsNullOrEmpty(characterId))
            {
                var summary = RawDataProcessor.GetDataSummary();
                if (summary.OldestDate.HasValue && summary.NewestDate.HasValue)
                {
                    var characterName = _viewModel.SelectedCharacter?.Nickname ?? "";
                    
                    // 1. 기존 분석 데이터 초기화 (장비, 스킬 내역 삭제)
                    StatisticsService.ClearAnalysisData(characterId);
                    
                    // 2. 전투력 데이터 갱신 (raw stat 파일에서 다시 로드)
                    RawDataProcessor.RefreshCombatPowerFromRaw(characterId, characterName);
                    
                    // 3. 전체 기간에 대해 장비 변경 재분석 수행
                    RawDataProcessor.ProcessItemChangesFromRaw(
                        characterId, 
                        characterName, 
                        summary.OldestDate.Value, 
                        summary.NewestDate.Value
                    );
                }
            }
            
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
            
            // 캘린더 및 UI 업데이트
            _collectedDates.Clear();
            _selectedDates.Clear();
            _lastCollectedDate = null;
            UpdateDataSummary();
            LoadDashboard();
            
            MessageBox.Show("성장 리포트 캐시가 삭제되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        #region Quick Navigation
        
        private void NavExpPower_Click(object sender, RoutedEventArgs e)
        {
            ScrollToElement("ExpPowerSection");
        }
        
        private void NavEquipment_Click(object sender, RoutedEventArgs e)
        {
            ScrollToElement("EquipmentSection");
        }
        
        private void NavHexaCore_Click(object sender, RoutedEventArgs e)
        {
            ScrollToElement("HexaCoreSection");
        }
        
        private void NavHexaSkill_Click(object sender, RoutedEventArgs e)
        {
            ScrollToElement("HexaSkillSection");
        }
        
        private void ScrollToElement(string elementName)
        {
            var element = this.FindName(elementName) as FrameworkElement;
            if (element != null && MainScrollViewer != null)
            {
                var transform = element.TransformToAncestor(MainScrollViewer);
                var point = transform.Transform(new System.Windows.Point(0, 0));
                MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset + point.Y - 20);
            }
        }
        
        #endregion

        private void CollectHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            // 이미 수집 중인지 확인
            if (App.IsCollecting)
            {
                MessageBox.Show("이미 데이터 수집이 진행 중입니다.\n완료 시 윈도우 알림으로 안내됩니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string? characterId = (CharacterFilterCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string;

            if (string.IsNullOrEmpty(characterId))
            {
                MessageBox.Show("캐릭터가 선택되어 있는지 확인해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var character = _viewModel.Characters.FirstOrDefault(c => c.Id == characterId);
            if (character == null || string.IsNullOrEmpty(character.Ocid))
            {
                MessageBox.Show("선택된 캐릭터의 OCID 정보가 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 캘린더에서 선택된 날짜 수집
            List<DateTime>? specificDates = _selectedDates.OrderBy(d => d).ToList();
            int days = specificDates.Count;

            if (!specificDates.Any())
            {
                MessageBox.Show("캘린더에서 수집할 날짜를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // UI 업데이트
            CollectHistoryButton.IsEnabled = false;
            CollectHistoryButton.Content = "수집 중...";
            CollectProgressBar.Visibility = Visibility.Visible;
            CollectProgressBar.IsIndeterminate = true;
            CollectProgressBar.Value = 0;
            _collectStartTime = DateTime.Now;
            CollectStatusText.Text = "0% (예상 완료 시간 계산 중...)";

            // 이벤트 구독
            App.CollectProgressChanged += OnCollectProgressChanged;
            App.CollectCompleted += OnCollectCompleted;

            // 알림 서비스 가져오기
            var notificationService = MainWindow.Instance?.NotificationServiceInstance;

            // 백그라운드 수집 시작
            App.StartBackgroundCollect(
                character.Ocid,
                characterId,
                character.Nickname,
                days,
                specificDates,
                notificationService
            );
        }

        private void OnCollectProgressChanged(int progress)
        {
            // UI 스레드에서 실행
            Dispatcher.Invoke(() =>
            {
                if (CollectProgressBar == null) return;
                
                CollectProgressBar.IsIndeterminate = false;
                CollectProgressBar.Value = progress;

                if (_collectStartTime.HasValue && progress > 0 && progress <= 100)
                {
                    var elapsed = DateTime.Now - _collectStartTime.Value;
                    var remaining = TimeSpan.FromSeconds(elapsed.TotalSeconds * (100 - progress) / progress);
                    var etaText = $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
                    CollectStatusText.Text = $"{progress}% (예상 남은 시간: {etaText})";
                }
                else
                {
                    CollectStatusText.Text = $"{progress}%";
                }
            });
        }

        private void OnCollectCompleted(bool success, string message)
        {
            // 이벤트 구독 해제
            App.CollectProgressChanged -= OnCollectProgressChanged;
            App.CollectCompleted -= OnCollectCompleted;

            // UI 스레드에서 실행
            Dispatcher.Invoke(async () =>
            {
                // UI 복구
                if (CollectHistoryButton != null)
                {
                    CollectHistoryButton.IsEnabled = true;
                    CollectHistoryButton.Content = "선택한 날짜 수집";
                }
                if (CollectProgressBar != null)
                {
                    CollectProgressBar.Visibility = Visibility.Collapsed;
                    CollectProgressBar.IsIndeterminate = false;
                }
                if (CollectStatusText != null)
                {
                    CollectStatusText.Text = "";
                }
                _collectStartTime = null;

                // 데이터 갱신
                if (success)
                {
                    // 완료 애니메이션 표시
                    ShowCollectCompleteAnimation(true);
                    
                    // 전체 대시보드 새로고침 (장비 변경 내역 포함)
                    LoadDashboard();
                    UpdateDataSummary();
                    
                    // 선택된 날짜 초기화
                    _selectedDates.Clear();
                    RenderCalendar();
                    
                    // 애니메이션 자동 숨기기
                    await Task.Delay(2500);
                    HideCollectCompleteAnimation();
                }
                else
                {
                    // 실패 애니메이션
                    ShowCollectCompleteAnimation(false);
                    await Task.Delay(2500);
                    HideCollectCompleteAnimation();
                }
            });
        }
        
        private void ShowCollectCompleteAnimation(bool success)
        {
            if (CollectCompleteOverlay == null || CollectCompleteText == null) return;
            
            // 토스트 아이콘 배경 및 텍스트 설정
            var iconBorder = CollectCompleteOverlay.FindName("ToastIconBorder") as Border 
                             ?? this.FindName("ToastIconBorder") as Border;
            var toastIcon = CollectCompleteOverlay.FindName("ToastIcon") as TextBlock
                            ?? this.FindName("ToastIcon") as TextBlock;
            
            if (success)
            {
                CollectCompleteText.Text = "수집 완료!";
                if (toastIcon != null) toastIcon.Text = "✓";
                if (iconBorder != null)
                {
                    iconBorder.Background = new LinearGradientBrush(
                        Color.FromRgb(52, 211, 153), // #34D399
                        Color.FromRgb(16, 185, 129), // #10B981
                        45);
                }
                
                // 성공 시 추가 효과들 시작
                StartSheenEffect();
                StartGlowEffect();
                StartParticleEffect();
                StartRippleEffect();        // 원형 파동 효과
                StartBurstSparkleEffect();  // 폭발 스파클 효과
            }
            else
            {
                CollectCompleteText.Text = "수집 실패";
                if (toastIcon != null) toastIcon.Text = "✕";
                if (iconBorder != null)
                {
                    iconBorder.Background = new LinearGradientBrush(
                        Color.FromRgb(251, 113, 133), // #FB7185
                        Color.FromRgb(244, 63, 94),   // #F43F5E
                        45);
                }
            }
            
            CollectCompleteOverlay.Visibility = Visibility.Visible;
            
            // iOS 스타일 스프링 애니메이션 (Scale + Opacity)
            var scaleTransform = CollectCompleteOverlay.RenderTransform as System.Windows.Media.ScaleTransform;
            if (scaleTransform == null)
            {
                scaleTransform = new System.Windows.Media.ScaleTransform(0.8, 0.8);
                CollectCompleteOverlay.RenderTransform = scaleTransform;
            }
            
            // 스케일 애니메이션 (0.8 → 1.0, 바운스 효과)
            var scaleAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = new System.Windows.Media.Animation.BackEase 
                { 
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut,
                    Amplitude = 0.3
                }
            };
            
            // 페이드 인 애니메이션
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            
            scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleAnim);
            scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleAnim);
            CollectCompleteOverlay.BeginAnimation(OpacityProperty, fadeIn);
        }
        
        #region 수집 완료 애니메이션 효과
        
        private System.Windows.Threading.DispatcherTimer? _particleTimer;
        private Random _particleRandom = new Random();
        
        /// <summary>
        /// 쉬머링(Sheen) 효과 - 하얀색 그라데이션이 대각선으로 지나감 (2번 반복)
        /// </summary>
        private void StartSheenEffect()
        {
            if (SheenRect == null || SheenTransform == null) return;
            
            SheenRect.Opacity = 1;
            
            var sheenAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = -300,
                To = 400,
                Duration = TimeSpan.FromMilliseconds(600),
                RepeatBehavior = new System.Windows.Media.Animation.RepeatBehavior(2),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
            };
            sheenAnim.Completed += (s, e) => SheenRect.Opacity = 0;
            
            SheenTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, sheenAnim);
        }
        
        /// <summary>
        /// 아우터 글로우(Pulsing) 효과 - 테두리가 강하게 빛남
        /// </summary>
        private void StartGlowEffect()
        {
            if (ApiCollectCard == null) return;
            
            var glowEffect = ApiCollectCard.Effect as System.Windows.Media.Effects.DropShadowEffect;
            if (glowEffect == null) return;
            
            // BlurRadius 펄스 애니메이션 (더 강하게)
            var blurAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 5,
                To = 45,
                Duration = TimeSpan.FromMilliseconds(350),
                AutoReverse = true,
                RepeatBehavior = new System.Windows.Media.Animation.RepeatBehavior(4)
            };
            
            // Opacity 펄스 애니메이션 (더 밝게)
            var opacityAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.3,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(350),
                AutoReverse = true,
                RepeatBehavior = new System.Windows.Media.Animation.RepeatBehavior(4)
            };
            
            glowEffect.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty, blurAnim);
            glowEffect.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.OpacityProperty, opacityAnim);
        }
        
        /// <summary>
        /// 반짝이(Sparkle) 파티클 효과 시작 - 더 자주, 더 오래
        /// </summary>
        private void StartParticleEffect()
        {
            if (ParticleCanvas == null) return;
            
            _particleTimer = new System.Windows.Threading.DispatcherTimer();
            _particleTimer.Interval = TimeSpan.FromMilliseconds(70); // 150 → 100ms (더 자주)
            _particleTimer.Tick += (s, e) => SpawnParticle();
            _particleTimer.Start();
            
            // 2.5초 후 파티클 생성 중지
            Task.Delay(2500).ContinueWith(_ => 
            {
                Dispatcher.Invoke(() => _particleTimer?.Stop());
            });
        }
        
        /// <summary>
        /// 원형 파동(Ripple) 효과 - 버튼에서 팡 하고 원형으로 퍼져나감
        /// </summary>
        private void StartRippleEffect()
        {
            if (ParticleCanvas == null || ApiCollectCard == null) return;
            
            double centerX = ApiCollectCard.ActualWidth / 2;
            double centerY = ApiCollectCard.ActualHeight / 2;
            
            // 3개의 파동을 시간차로 생성
            for (int wave = 0; wave < 3; wave++)
            {
                int delay = wave * 150; // 150ms 간격
                
                Task.Delay(delay).ContinueWith(_ => 
                {
                    Dispatcher.Invoke(() => SpawnRipple(centerX, centerY));
                });
            }
        }
        
        private void SpawnRipple(double centerX, double centerY)
        {
            if (ParticleCanvas == null) return;
            
            // 원형 파동 (Ellipse)
            var ripple = new System.Windows.Shapes.Ellipse
            {
                Width = 20,
                Height = 20,
                StrokeThickness = 3,
                Stroke = new System.Windows.Media.LinearGradientBrush(
                    Color.FromRgb(52, 211, 153),   // #34D399
                    Color.FromRgb(16, 185, 129),   // #10B981
                    45),
                Fill = System.Windows.Media.Brushes.Transparent,
                RenderTransformOrigin = new System.Windows.Point(0.5, 0.5)
            };
            
            // 중앙에 위치
            System.Windows.Controls.Canvas.SetLeft(ripple, centerX - 10);
            System.Windows.Controls.Canvas.SetTop(ripple, centerY - 10);
            
            ParticleCanvas.Children.Add(ripple);
            
            // 스케일 + 투명도 애니메이션
            var scaleTransform = new System.Windows.Media.ScaleTransform(1, 1);
            ripple.RenderTransform = scaleTransform;
            
            // 탄력있는 확대 (0 → 최대 크기)
            var scaleAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1,
                To = 15, // 크게 확대
                Duration = TimeSpan.FromMilliseconds(700),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase 
                { 
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut 
                }
            };
            
            // 투명도 페이드아웃
            var opacityAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.9,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(700),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase 
                { 
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn 
                }
            };
            opacityAnim.Completed += (s, e) => ParticleCanvas.Children.Remove(ripple);
            
            scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleAnim);
            scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleAnim);
            ripple.BeginAnimation(OpacityProperty, opacityAnim);
        }
        
        /// <summary>
        /// 폭발 스파클 효과 - 중심에서 바깥으로 별들이 퍼져나감
        /// </summary>
        private void StartBurstSparkleEffect()
        {
            if (ParticleCanvas == null || ApiCollectCard == null) return;
            
            double centerX = ApiCollectCard.ActualWidth / 2;
            double centerY = ApiCollectCard.ActualHeight / 2;
            
            // 12개의 스파클을 원형으로 배치
            int sparkleCount = 12;
            for (int i = 0; i < sparkleCount; i++)
            {
                double angle = (360.0 / sparkleCount) * i * Math.PI / 180;
                SpawnBurstSparkle(centerX, centerY, angle);
            }
        }
        
        private void SpawnBurstSparkle(double centerX, double centerY, double angle)
        {
            if (ParticleCanvas == null) return;
            
            // 별 모양 파티클
            var star = new System.Windows.Shapes.Path
            {
                Data = System.Windows.Media.Geometry.Parse("M 8,0 L 10,6 L 16,8 L 10,10 L 8,16 L 6,10 L 0,8 L 6,6 Z"),
                Fill = new System.Windows.Media.SolidColorBrush(Color.FromRgb(255, 223, 107)), // 밝은 골드
                Width = 14,
                Height = 14,
                Opacity = 1,
                RenderTransformOrigin = new System.Windows.Point(0.5, 0.5)
            };
            
            // 중앙에서 시작
            System.Windows.Controls.Canvas.SetLeft(star, centerX - 7);
            System.Windows.Controls.Canvas.SetTop(star, centerY - 7);
            
            ParticleCanvas.Children.Add(star);
            
            // 이동 거리 계산
            double distance = Math.Max(ApiCollectCard.ActualWidth, ApiCollectCard.ActualHeight) * 0.6;
            double targetX = Math.Cos(angle) * distance;
            double targetY = Math.Sin(angle) * distance;
            
            // Transform 그룹
            var transformGroup = new System.Windows.Media.TransformGroup();
            var translateTransform = new System.Windows.Media.TranslateTransform(0, 0);
            var scaleTransform = new System.Windows.Media.ScaleTransform(0.3, 0.3);
            var rotateTransform = new System.Windows.Media.RotateTransform(_particleRandom.Next(0, 360));
            transformGroup.Children.Add(scaleTransform);
            transformGroup.Children.Add(rotateTransform);
            transformGroup.Children.Add(translateTransform);
            star.RenderTransform = transformGroup;
            
            // 이동 애니메이션 (탄력있게)
            var moveXAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = targetX,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase 
                { 
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut 
                }
            };
            
            var moveYAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = targetY,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase 
                { 
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut 
                }
            };
            
            // 크기 확대 후 축소
            var scaleAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.3,
                To = 1.5,
                Duration = TimeSpan.FromMilliseconds(300),
                AutoReverse = true
            };
            
            // 회전
            var rotateAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = rotateTransform.Angle,
                To = rotateTransform.Angle + 180,
                Duration = TimeSpan.FromMilliseconds(600)
            };
            
            // 투명도
            var opacityAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1,
                To = 0,
                BeginTime = TimeSpan.FromMilliseconds(300),
                Duration = TimeSpan.FromMilliseconds(400)
            };
            opacityAnim.Completed += (s, e) => ParticleCanvas.Children.Remove(star);
            
            translateTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, moveXAnim);
            translateTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, moveYAnim);
            scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleAnim);
            scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleAnim);
            rotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, rotateAnim);
            star.BeginAnimation(OpacityProperty, opacityAnim);
        }
        
        /// <summary>
        /// 개별 파티클 생성 - 더 크고 눈에 띄는 골드 색상
        /// </summary>
        private void SpawnParticle()
        {
            if (ParticleCanvas == null || ApiCollectCard == null) return;
            
            // 십자가 모양 별 (더 크게, 진한 앤틱 골드 색상)
            var starPath = new System.Windows.Shapes.Path
            {
                Data = System.Windows.Media.Geometry.Parse("M 8,0 L 10,6 L 16,8 L 10,10 L 8,16 L 6,10 L 0,8 L 6,6 Z"),
                Fill = new System.Windows.Media.SolidColorBrush(Color.FromRgb(218, 165, 32)), // Goldenrod (더 진한 골드)
                Width = 16,
                Height = 16,
                Opacity = 0,
                RenderTransformOrigin = new System.Windows.Point(0.5, 0.5)
            };
            
            var transformGroup = new System.Windows.Media.TransformGroup();
            var scaleTransform = new System.Windows.Media.ScaleTransform(0.5, 0.5);
            var rotateTransform = new System.Windows.Media.RotateTransform(_particleRandom.Next(0, 360));
            transformGroup.Children.Add(scaleTransform);
            transformGroup.Children.Add(rotateTransform);
            starPath.RenderTransform = transformGroup;
            
            // 랜덤 위치
            double x = _particleRandom.Next(10, (int)Math.Max(50, ApiCollectCard.ActualWidth - 20));
            double y = _particleRandom.Next(10, (int)Math.Max(50, ApiCollectCard.ActualHeight - 20));
            System.Windows.Controls.Canvas.SetLeft(starPath, x);
            System.Windows.Controls.Canvas.SetTop(starPath, y);
            
            ParticleCanvas.Children.Add(starPath);
            
            // 애니메이션 스토리보드
            var story = new System.Windows.Media.Animation.Storyboard();
            
            // 투명도 (나타났다 사라짐) - 더 오래 유지
            var opacityAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                AutoReverse = true
            };
            System.Windows.Media.Animation.Storyboard.SetTarget(opacityAnim, starPath);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));
            
            // 크기 X - 더 크게 확대
            var scaleXAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.3, To = 1.5,
                Duration = TimeSpan.FromMilliseconds(1000)
            };
            System.Windows.Media.Animation.Storyboard.SetTarget(scaleXAnim, starPath);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleXAnim, 
                new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
            
            // 크기 Y - 더 크게 확대
            var scaleYAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.3, To = 1.5,
                Duration = TimeSpan.FromMilliseconds(1000)
            };
            System.Windows.Media.Animation.Storyboard.SetTarget(scaleYAnim, starPath);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleYAnim, 
                new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
            
            // 회전 - 더 많이 회전
            var rotateAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = rotateTransform.Angle,
                To = rotateTransform.Angle + 120,
                Duration = TimeSpan.FromMilliseconds(1000)
            };
            System.Windows.Media.Animation.Storyboard.SetTarget(rotateAnim, starPath);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(rotateAnim, 
                new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(RotateTransform.Angle)"));
            
            story.Children.Add(opacityAnim);
            story.Children.Add(scaleXAnim);
            story.Children.Add(scaleYAnim);
            story.Children.Add(rotateAnim);
            
            // 애니메이션 완료 시 제거 (메모리 누수 방지)
            story.Completed += (s, e) => ParticleCanvas.Children.Remove(starPath);
            
            story.Begin();
        }
        
        #endregion
        
        private void HideCollectCompleteAnimation()
        {
            if (CollectCompleteOverlay == null) return;
            
            var scaleTransform = CollectCompleteOverlay.RenderTransform as System.Windows.Media.ScaleTransform;
            
            // 스케일 축소 애니메이션 (1.0 → 0.8)
            var scaleAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1.0,
                To = 0.8,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase 
                { 
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn
                }
            };
            
            // 페이드 아웃 애니메이션
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            fadeOut.Completed += (s, e) => CollectCompleteOverlay.Visibility = Visibility.Collapsed;
            
            if (scaleTransform != null)
            {
                scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleAnim);
                scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleAnim);
            }
            CollectCompleteOverlay.BeginAnimation(OpacityProperty, fadeOut);
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

            // 항상 새 브러시를 생성하여 DynamicResource 바인딩이 확실히 업데이트되도록 함
            // Window.Resources와 Application.Resources 모두 업데이트 (DataTemplate 내부에서도 접근 가능하도록)
            void SetBrush(string key, Color color)
            {
                var brush = new SolidColorBrush(color);
                Resources[key] = brush;
                // Application 수준 리소스도 업데이트하여 DataTemplate 내부에서도 접근 가능하게 함
                if (System.Windows.Application.Current?.Resources != null)
                {
                    System.Windows.Application.Current.Resources[key] = brush;
                }
            }

            if (isDark)
            {
                // 다크 모드 - 메이플 스케줄러 스타일 다크 테마
                SetBrush("WindowBackground", Color.FromRgb(20, 24, 36));      // #141824
                SetBrush("CardBackground", Color.FromRgb(32, 38, 54));        // #202636
                SetBrush("CardBackgroundHover", Color.FromRgb(44, 52, 70));   // #2C3446
                SetBrush("CardBackgroundAlt", Color.FromRgb(28, 34, 48));     // #1C2230
                SetBrush("TextPrimary", Color.FromRgb(245, 247, 250));        // #F5F7FA
                SetBrush("TextSecondary", Color.FromRgb(160, 170, 190));      // #A0AABE
                SetBrush("TextMuted", Color.FromRgb(110, 120, 140));          // #6E788C
                SetBrush("TextInverse", Color.FromRgb(20, 24, 36));           // #141824
                SetBrush("DividerColor", Color.FromRgb(50, 58, 76));          // #323A4C
                SetBrush("BorderColor", Color.FromRgb(50, 58, 76));           // #323A4C
                SetBrush("ChartTextPrimary", Color.FromRgb(235, 240, 250));   // #EBF0FA
                SetBrush("ChartTextSecondary", Color.FromRgb(160, 170, 190)); // #A0AABE
                SetBrush("ApiCardBackground", Color.FromRgb(38, 44, 60));     // #262C3C
                SetBrush("ApiCardBorder", Color.FromRgb(60, 68, 88));         // #3C4458
                // 아이템 카드
                SetBrush("ItemCardBackground", Color.FromRgb(36, 42, 58));    // #242A3A
                SetBrush("ItemCardBorder", Color.FromRgb(52, 60, 80));        // #343C50
                SetBrush("ItemCardHover", Color.FromRgb(48, 56, 74));         // #30384A
                SetBrush("BadgeBackground", Color.FromRgb(50, 45, 75));       // #322D4B
                SetBrush("BadgeText", Color.FromRgb(167, 139, 250));          // #A78BFA
                SetBrush("DetailViewBackground", Color.FromRgb(22, 26, 40));  // #161A28
                // 액센트 색상
                SetBrush("AccentCyan", Color.FromRgb(90, 200, 250));          // #5AC8FA
                SetBrush("AccentOrange", Color.FromRgb(255, 159, 67));        // #FF9F43
                SetBrush("AccentGreen", Color.FromRgb(52, 211, 153));         // #34D399
                SetBrush("AccentPurple", Color.FromRgb(167, 139, 250));       // #A78BFA
                SetBrush("AccentRed", Color.FromRgb(251, 113, 133));          // #FB7185
                SetBrush("AccentBlue", Color.FromRgb(96, 165, 250));          // #60A5FA
            }
            else
            {
                // 라이트 모드
                SetBrush("WindowBackground", Color.FromRgb(245, 247, 251));   // #F5F7FB
                SetBrush("CardBackground", Colors.White);                      // #FFFFFF
                SetBrush("CardBackgroundHover", Color.FromRgb(240, 244, 250)); // #F0F4FA
                SetBrush("CardBackgroundAlt", Color.FromRgb(248, 250, 252));   // #F8FAFC
                SetBrush("TextPrimary", Color.FromRgb(15, 23, 42));            // #0F172A
                SetBrush("TextSecondary", Color.FromRgb(71, 85, 105));         // #475569
                SetBrush("TextMuted", Color.FromRgb(100, 116, 139));           // #64748B
                SetBrush("TextInverse", Colors.White);                          // #FFFFFF
                SetBrush("DividerColor", Color.FromRgb(226, 232, 240));        // #E2E8F0
                SetBrush("BorderColor", Color.FromRgb(226, 232, 240));         // #E2E8F0
                SetBrush("ChartTextPrimary", Color.FromRgb(15, 23, 42));       // #0F172A
                SetBrush("ChartTextSecondary", Color.FromRgb(71, 85, 105));    // #475569
                SetBrush("ApiCardBackground", Color.FromRgb(255, 246, 236));   // #FFF6EC
                SetBrush("ApiCardBorder", Color.FromRgb(255, 227, 194));       // #FFE3C2
                // 아이템 카드
                SetBrush("ItemCardBackground", Color.FromRgb(250, 251, 252));  // #FAFBFC
                SetBrush("ItemCardBorder", Color.FromRgb(229, 233, 240));      // #E5E9F0
                SetBrush("ItemCardHover", Color.FromRgb(240, 244, 248));       // #F0F4F8
                SetBrush("BadgeBackground", Color.FromRgb(238, 242, 255));     // #EEF2FF
                SetBrush("BadgeText", Color.FromRgb(99, 102, 241));            // #6366F1
                SetBrush("DetailViewBackground", Color.FromRgb(26, 29, 46));   // #1A1D2E (항상 다크)
                // 액센트 색상
                SetBrush("AccentCyan", Color.FromRgb(90, 200, 250));           // #5AC8FA
                SetBrush("AccentOrange", Color.FromRgb(255, 159, 67));         // #FF9F43
                SetBrush("AccentGreen", Color.FromRgb(16, 185, 129));          // #10B981
                SetBrush("AccentPurple", Color.FromRgb(167, 139, 250));        // #A78BFA
                SetBrush("AccentRed", Color.FromRgb(244, 63, 94));             // #F43F5E
                SetBrush("AccentBlue", Color.FromRgb(59, 130, 246));           // #3B82F6
            }

            // MainGrid 배경 업데이트
            MainGrid.Background = (Brush)Resources["WindowBackground"];
        }

        #endregion

        #region Scroll Handling
        private void InnerScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 우선 해당 ScrollViewer가 스크롤 가능하면 자신을 스크롤,
            // 스크롤할 높이가 없으면 부모(MainScrollViewer)에 위임
            if (sender is ScrollViewer sv)
            {
                if (sv.ScrollableHeight > 0)
                {
                    sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
                    e.Handled = true;
                    return;
                }
            }

            if (MainScrollViewer != null)
            {
                MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 장비 상세 보기 영역 스크롤 - 항상 부모로 전달 (자체 스크롤 없음)
        /// </summary>
        private void DetailView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (MainScrollViewer != null)
            {
                MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }
        #endregion

        #region 장비 변경 내역 필터링

        private void ItemFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (ItemFilterPanel == null) return;
            
            ItemFilterPanel.Visibility = ItemFilterPanel.Visibility == Visibility.Visible 
                ? Visibility.Collapsed 
                : Visibility.Visible;
        }

        private void ItemFilter_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            ApplyItemFilters();
        }

        private void ItemDateRangeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || ItemDateRangeCombo?.SelectedItem is not System.Windows.Controls.ComboBoxItem selectedItem)
                return;

            if (selectedItem.Tag != null && int.TryParse(selectedItem.Tag.ToString(), out int days))
            {
                _itemDateRangeDays = days;
                ApplyItemFilters();
            }
        }

        private void ItemFilterReset_Click(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            _isInitializing = true;
            
            // 필터 초기화
            if (FilterPresetItems != null) FilterPresetItems.IsChecked = false;
            if (FilterSpiritPendant != null) FilterSpiritPendant.IsChecked = false;
            if (FilterMedalTitle != null) FilterMedalTitle.IsChecked = true; // 훈장/칭호 기본 표시
            if (FilterTypeEquip != null) FilterTypeEquip.IsChecked = true;
            if (FilterTypeReplace != null) FilterTypeReplace.IsChecked = true;
            if (FilterTypeOption != null) FilterTypeOption.IsChecked = true;
            if (ItemDateRangeCombo != null) ItemDateRangeCombo.SelectedIndex = 0;

            _isInitializing = false;

            _showPresetItems = false;
            _showSpiritPendant = false;
            _enabledChangeTypes = new HashSet<string> { "장착", "교체", "옵션 변경" };
            _itemDateRangeDays = 0;

            ApplyItemFilters();
        }

        private void ApplyItemFilters()
        {
            if (ItemChangeList == null || NoItemChangeDataText == null) return;

            // 필터 상태 업데이트
            if (FilterPresetItems != null)
                _showPresetItems = FilterPresetItems.IsChecked == true;
            
            if (FilterSpiritPendant != null)
                _showSpiritPendant = FilterSpiritPendant.IsChecked == true;
            
            bool showMedalTitle = FilterMedalTitle?.IsChecked == true;

            // 변경 타입 필터 업데이트
            _enabledChangeTypes.Clear();
            if (FilterTypeEquip?.IsChecked == true) _enabledChangeTypes.Add("장착");
            if (FilterTypeReplace?.IsChecked == true) _enabledChangeTypes.Add("교체");
            if (FilterTypeOption?.IsChecked == true) _enabledChangeTypes.Add("옵션 변경");

            // 필터링 적용
            var filtered = _allItemChangeRecords.AsEnumerable();

            // 날짜 범위 필터
            if (_itemDateRangeDays > 0)
            {
                var cutoffDate = DateTime.Today.AddDays(-_itemDateRangeDays);
                filtered = filtered.Where(r => r.Date >= cutoffDate);
            }

            // 변경 타입 필터
            if (_enabledChangeTypes.Count > 0)
            {
                filtered = filtered.Where(r => _enabledChangeTypes.Contains(r.ChangeType));
            }

            // 정령의 펜던트 필터
            if (!_showSpiritPendant)
            {
                filtered = filtered.Where(r => 
                    !r.OldItemName.Contains("정령의 펜던트") && 
                    !r.NewItemName.Contains("정령의 펜던트"));
            }
            
            // 훈장/칭호 필터
            if (!showMedalTitle)
            {
                filtered = filtered.Where(r => 
                    !IsMedalOrTitle(r.ItemSlot));
            }
            
            // 프리셋 아이템 필터 (아이템 이름에 "프리셋" 포함 여부)
            // 참고: 프리셋 아이템은 데이터 수집 시 이미 필터링되어 저장되지 않으므로
            // 이 필터는 현재 저장된 데이터에서는 효과가 제한적일 수 있음

            var filteredList = filtered.OrderByDescending(r => r.Date).ToList();
            ItemChangeList.ItemsSource = filteredList;
            NoItemChangeDataText.Visibility = filteredList.Any() ? Visibility.Collapsed : Visibility.Visible;
        }
        
        /// <summary>
        /// 훈장 또는 칭호 슬롯인지 확인
        /// </summary>
        private static bool IsMedalOrTitle(string? slot)
        {
            if (string.IsNullOrEmpty(slot)) return false;
            return slot.Contains("훈장") || slot.Contains("칭호");
        }

        #endregion
    }
}