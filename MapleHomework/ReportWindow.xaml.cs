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
using MapleHomework.Rendering;

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
        private DateTime? _collectStartTime;
        private string _expRangeMode = "day";
        private int _chartDays = 30; // 차트 조회 기간 (기본 30일, 0은 전체)
        private List<MissingDateItem> _missingDates = new(); // 누락 날짜 목록
        private TaskCompletionSource<bool>? _collectionTcs; // 수집 완료 대기용
        private bool _isBusy = false; // 수집 중 UI 잠금용

        // 카운트다운 타이머 관련
        private System.Windows.Threading.DispatcherTimer? _countdownTimer;
        private int _remainingSeconds = 0;
        private int _lastProgress = 0;

        // 캘린더 관련 필드
        private DateTime _calendarDisplayMonth = DateTime.Today;
        private HashSet<DateTime> _collectedDates = new(); // 데이터 수집된 날짜
        private HashSet<DateTime> _selectedDates = new(); // 사용자가 선택한 날짜
        private DateTime? _firstClickDate = null; // 오셀로 스타일: 첫 번째 클릭 날짜
        private DateTime? _lastCollectedDate = null; // 마지막 수집된 날짜 (연속 수집용)

        // UI State
        private bool _isHexaExpanded = false;
        public bool IsHexaExpanded
        {
            get => _isHexaExpanded;
            set
            {
                if (_isHexaExpanded != value)
                {
                    _isHexaExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        // 장비 변경 내역 필터 상태
        private List<ItemChangeRecord> _allItemChangeRecords = new(); // 전체 장비 변경 내역 (필터링 전)
        private bool _showPresetItems = false; // 프리셋 아이템 표시 여부
        private bool _showSpiritPendant = false; // 정령의 펜던트 표시 여부
        private HashSet<string> _enabledChangeTypes = new HashSet<string> { "장착", "교체", "옵션 변경" }; // 활성화된 변경 타입
        private int _itemDateRangeDays = 0; // 날짜 범위 (0 = 전체)

        public class HexaCoreItem
        {
            public string SkillName { get; set; } = "";
            public string OriginalName { get; set; } = "";
            public string CoreType { get; set; } = "";
            public int CoreLevel { get; set; }
            public string SkillIcon { get; set; } = ""; // Note: CharacterSearchWindow used SkillIconUrl, here we use SkillIcon (from existing code usage)
            public string BadgeIcon { get; set; } = ""; // Note: CharacterSearchWindow used BadgeIconPath, here BadgeIcon

            // Data properties for UI (Added)
            public int NextSolErda { get; set; }
            public int NextFragment { get; set; }
            public int RemainingSolErda { get; set; }
            public int RemainingFragment { get; set; }

            // Compatibility properties (restored)
            public int OldLevel { get; set; }
            public int NewLevel { get; set; }

            public bool IsMaxLevel => CoreLevel >= 30;

            // UI Display Properties
            public string NextCostText => IsMaxLevel ? "MAX" : $"{NextSolErda} / {NextFragment}";
            public string RemainingCostText => IsMaxLevel ? "-" : $"{RemainingSolErda} / {RemainingFragment}";

            public string NextSolErdaText => IsMaxLevel ? "-" : $"{NextSolErda}개";
            public string NextFragmentText => IsMaxLevel ? "-" : $"{NextFragment}개";
            public string RemainingSolErdaText => IsMaxLevel ? "-" : $"{RemainingSolErda}개";
            public string RemainingFragmentText => IsMaxLevel ? "-" : $"{RemainingFragment}개";

            public double ProgressValue => CoreLevel >= 30 ? 100 : (CoreLevel / 30.0 * 100);
            public double ProgressFactor
            {
                get
                {
                    if (CoreLevel >= 30) return 1.0;
                    if (CoreLevel <= 0) return 0.0;
                    double result = CoreLevel / 30.0;
                    return double.IsNaN(result) || double.IsInfinity(result) ? 0.0 : result;
                }
            }
            public string ProgressText => $"{ProgressValue:F1}%";
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
            _apiService = new MapleApiService();

            // 테마 변경 이벤트 구독 (ThemeService 전역 이벤트 사용)
            ThemeService.OnThemeChanged += OnThemeChanged_Service;

            // 테마 적용 (라이트/다크)
            ApplyThemeResources();

            // Loaded 이벤트에서도 테마 적용 (XAML 파싱 완료 후 DynamicResource 경고 방지)
            this.Loaded += (s, e) => ApplyThemeResources();

            InitializeSelectors();

            UpdateRangeButtonsVisual();



            _isInitializing = false; // 초기화 완료 플래그 해제
            _ = RefreshIfStaleAndLoadAsync(); // 자동 최신화 후 로드
        }

        private void OnThemeChanged_Service(bool isDark)
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
            string? characterName = GetSelectedCharacterName() ?? _viewModel.SelectedCharacter?.Nickname;

            if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(characterName))
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
            _collectedDates = GetVerifiedCollectedDates(characterId, characterName);

            // 마지막 수집 날짜 설정 (연속 수집 검증용)
            _lastCollectedDate = _collectedDates.Count > 0 ? _collectedDates.Max() : (DateTime?)null;

            if (DataSummaryText != null)
            {
                DataSummaryText.Text = $"{_collectedDates.Count}일";
            }

            // 최근 90일 기준 누락 날짜 수
            var endDate = DateTime.Today.AddDays(-1);
            var startDate = endDate.AddDays(-89);
            var missingDates = StatisticsService.GetMissingDatesForCharacter(characterId, characterName, startDate, endDate);

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
        private HashSet<DateTime> GetVerifiedCollectedDates(string characterId, string characterName)
        {
            var allDates = StatisticsService.GetCollectedDatesForCharacter(characterId, characterName);
            var verifiedDates = new HashSet<DateTime>();

            foreach (var date in allDates)
            {
                // 필수 데이터가 있는지 확인 (basic, stat 등)
                if (StatisticsService.HasCompleteDataForDate(characterId, characterName, date))
                {
                    verifiedDates.Add(date);
                }
                else
                {
                    // 부분 데이터는 삭제 (오염된 데이터 정리)
                    StatisticsService.RemoveIncompleteDataForDate(characterId, characterName, date);
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
                Tag = date,
                Background = System.Windows.Media.Brushes.Transparent // Default
            };

            var textBlock = new TextBlock
            {
                Text = date.Day.ToString(),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                FontSize = 10,
                Padding = new Thickness(0, 4, 0, 4)
            };
            // 기본 텍스트 색상 (테마 연동)
            textBlock.SetResourceReference(TextBlock.ForegroundProperty, "CalendarText");

            bool isCollected = _collectedDates.Contains(date);
            bool isSelected = _selectedDates.Contains(date);
            bool isFuture = date > DateTime.Today;
            bool isTooOld = date < DateTime.Today.AddDays(-365); // 1년 이상 오래된 날짜

            if (isFuture || isTooOld)
            {
                // 미래 또는 너무 오래된 날짜 - 비활성화 (투명도 조절)
                border.Opacity = 0.3;
                border.IsEnabled = false;
                border.Background = System.Windows.Media.Brushes.Transparent;
            }
            else if (isCollected)
            {
                // 데이터 있음 - 파란색 (선택 불가)
                border.SetResourceReference(Border.BackgroundProperty, "CalendarCollectedBg");
                textBlock.Foreground = System.Windows.Media.Brushes.White; // 항상 흰색
                border.Cursor = System.Windows.Input.Cursors.Arrow;
                border.ToolTip = "이미 수집된 날짜입니다";
            }
            else if (isSelected)
            {
                // 선택됨 - 테마 강조색 (반투명)
                border.SetResourceReference(Border.BackgroundProperty, "CalendarSelectedBg");
                // 텍스트는 CalendarText 유지
                textBlock.FontWeight = FontWeights.Bold;
                AttachCalendarDayEvents(border);
            }
            else
            {
                // 데이터 없음 (누락) - 빨간색 (반투명 배경 + 진한 텍스트)
                border.SetResourceReference(Border.BackgroundProperty, "CalendarMissingBgDim");
                textBlock.SetResourceReference(TextBlock.ForegroundProperty, "CalendarMissingBg");
                textBlock.FontWeight = FontWeights.SemiBold;
                AttachCalendarDayEvents(border);
            }

            // 어제 (수집 기준일) 표시 - 테두리 강조
            if (date.Date == DateTime.Today)
            {
                border.SetResourceReference(Border.BorderBrushProperty, "ThemeAccentColor");
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
                        // 미래 날짜는 제외
                        if (d > DateTime.Today) continue;
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

            string? characterId = GetSelectedCharacterId() ?? _viewModel.SelectedCharacter?.Id;
            string? characterName = GetSelectedCharacterName() ?? _viewModel.SelectedCharacter?.Nickname;

            // 캐릭터 정보가 없으면 로드하고 종료
            if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(characterName))
            {
                LoadDashboard();
                return;
            }

            var last = StatisticsService.GetLastUpdated(characterName);

            // 1. 통계 파일이 없거나 비어있는 경우
            bool needRefresh = last == DateTime.MinValue;

            // 2. [Auto-Collect] 최근 7일 내 누락된 데이터가 있는 경우
            List<DateTime> missingRecentDates = new();

            if (!string.IsNullOrEmpty(characterId))
            {
                var endDate = DateTime.Today; // 오늘 포함
                var startDate = endDate.AddDays(-6);      // 7일 전까지
                var collected = StatisticsService.GetCollectedDatesForCharacter(characterId, characterName);

                for (var d = startDate; d <= endDate; d = d.AddDays(1))
                {
                    if (!collected.Contains(d))
                    {
                        missingRecentDates.Add(d);
                    }
                }

                if (missingRecentDates.Any())
                {
                    needRefresh = true;
                }
            }

            if (!needRefresh)
            {
                LoadDashboard();
                return;
            }

            var character = _viewModel.Characters.FirstOrDefault(c => c.Id == characterId);
            if (character == null || string.IsNullOrEmpty(character.Ocid))
            {
                LoadDashboard();
                return;
            }

            // 누락된 날짜만큼 반복 호출하거나 배치 수집
            // 여기서는 기존 로직(최근 1일분 or 전체) 대신, 누락된 날짜 리스트를 활용
            if (missingRecentDates.Any())
            {
                // UI 갱신 (선택된 날짜로 표시)
                _selectedDates.Clear();
                foreach (var d in missingRecentDates) _selectedDates.Add(d);
                RenderCalendar();

                // 수집 시작 (기존 버튼 클릭 로직 재활용을 위해 메서드 호출)
                await StartCollectionAsync(character, missingRecentDates);
                return;
            }

            // 여기까지 왔는데 needRefresh가 true라면? (이론상 7일 이내는 다 있는데 파일은 없거나 다른 이유)
            LoadDashboard();
            UpdateDataSummary();
        }

        protected override void OnClosed(EventArgs e)
        {
            ThemeService.OnThemeChanged -= OnThemeChanged_Service;

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

            CharacterFilterCombo.SelectionChanged -= CharacterFilterCombo_SelectionChanged;
            CharacterFilterCombo.SelectionChanged += CharacterFilterCombo_SelectionChanged;
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
            string? characterName = GetSelectedCharacterName();

            if (string.IsNullOrEmpty(characterId))
            {
                characterId = _viewModel.SelectedCharacter?.Id;
                characterName = _viewModel.SelectedCharacter?.Nickname;
            }
            if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(characterName))
            {
                // 선택된 캐릭터가 없으면 UI 초기화
                ClearGrowthReportData();
                return;
            }

            // 차트 조회 기간 (0이면 전체 데이터)
            int chartDays = _chartDays > 0 ? _chartDays : 365; // 전체 = 최대 365일

            // 경험치
            var expGrowth = StatisticsService.GetExperienceGrowth(characterId, characterName, chartDays, _expRangeMode);
            ExpGrowthList.ItemsSource = expGrowth;

            // 최근 30일 일간 평균 경험치 (경험치 + %)
            var dailyAvg = StatisticsService.GetMonthlyDailyAverageExpGain(characterId, characterName, 30);
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
            EstimatedLevelUpText.Text = StatisticsService.GetEstimatedLevelUpDateFormatted(characterId, characterName);

            // 전투력 (항상 전체 기간 조회 - 최고 기록 갱신 시점을 모두 표시)
            var combatPowerGrowth = StatisticsService.GetCombatPowerGrowth(characterId, characterName, 365, "day", onlyNewHigh: true);
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
            var hexaHistory = StatisticsService.GetHexaSkillHistory(characterId, characterName, 50);
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
            var itemHistory = StatisticsService.GetItemChangeHistory(characterId, characterName, 50);

            // 툴팁 렌더링을 위해 직업 정보 주입
            var character = _viewModel.Characters.FirstOrDefault(c => c.Id == characterId);
            if (character != null && !string.IsNullOrEmpty(character.CharacterClass))
            {
                foreach (var item in itemHistory)
                {
                    item.JobClass = character.CharacterClass;
                }
            }

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
                var characterName = character.Nickname;
                var skillResp = RawDataProcessor.LoadLatestSkill6Info(characterName);
                var skillIconMap = (skillResp?.CharacterSkill ?? new List<CharacterSkillInfo>())
                    .Where(s => !string.IsNullOrEmpty(s.SkillName) && !string.IsNullOrEmpty(s.SkillIcon))
                    .GroupBy(s => s.SkillName!)
                    .ToDictionary(g => g.Key, g => g.First().SkillIcon!);

                var hexaStat = RawDataProcessor.LoadLatestHexaStatInfo(characterName);
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

                var coreItems = hexaStat.HexaCoreEquipment.Select(core =>
                {
                    var coreType = string.IsNullOrEmpty(core.HexaCoreType) ? "스킬 코어" : core.HexaCoreType!;
                    var currentLevel = core.HexaCoreLevel;

                    // 비용 계산
                    var (nextSol, nextFrag) = Services.HexaCostCalculator.GetNextLevelCost(coreType, currentLevel);
                    var (remSol, remFrag) = Services.HexaCostCalculator.GetRemainingCost(coreType, currentLevel);

                    return new HexaCoreItem
                    {
                        SkillName = TrimCoreName(core.HexaCoreName ?? ""),
                        OriginalName = core.HexaCoreName ?? "",
                        SkillIcon = ResolveIcon(core.HexaCoreName ?? "", core.LinkedSkill),
                        OldLevel = core.HexaCoreLevel,
                        NewLevel = core.HexaCoreLevel,
                        CoreLevel = core.HexaCoreLevel,
                        BadgeIcon = GetBadgeFromType(core.HexaCoreType),
                        CoreType = coreType,
                        NextSolErda = nextSol,
                        NextFragment = nextFrag,
                        RemainingSolErda = remSol,
                        RemainingFragment = remFrag
                    };
                }).ToList();

                string[] typeOrder = { "마스터리 코어", "스킬 코어", "강화 코어", "공용 코어" };

                var flatList = coreItems
                    .OrderBy(x =>
                    {
                        int idx = Array.IndexOf(typeOrder, x.CoreType);
                        return idx >= 0 ? idx : int.MaxValue;
                    })
                    .ThenByDescending(x => x.CoreLevel)
                    .ThenBy(x => x.SkillName)
                    .ToList();

                if (HexaCoreGroups != null)
                {
                    HexaCoreGroups.ItemsSource = flatList;
                }

                // HEXA 스탯 로드
                LoadHexaStats(character.Nickname);
            }
            catch
            {
                // 무시
            }

            return Task.CompletedTask;
        }

        private void LoadHexaStats(string characterName)
        {
            try
            {
                // HEXA 스탯 코어 정보 가져오기 (hexamatrix-stat API 데이터)
                var hexaStatCore = RawDataProcessor.LoadLatestHexaMatrixStatInfo(characterName);

                var statCoreItems = new List<HexaStatCoreItem>();

                if (hexaStatCore != null)
                {
                    // character_hexa_stat_core
                    if (hexaStatCore.CharacterHexaStatCore?.Any() == true)
                    {
                        var core = hexaStatCore.CharacterHexaStatCore.First();
                        statCoreItems.Add(new HexaStatCoreItem
                        {
                            SlotIndex = 1,
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
                            SlotIndex = 2,
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
                            SlotIndex = 3,
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

            // Compatibility properties for Ported UI
            public int SlotIndex { get; set; }
            public string MainStat => MainStatName;
            public int MainLevel => MainStatLevel;
            public string SubStat1 => SubStatName1;
            public string SubStat2 => SubStatName2;
            public int SubLevel1 => SubStatLevel1;
            public int SubLevel2 => SubStatLevel2;
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

        private string? GetSelectedCharacterName()
        {
            if (CharacterFilterCombo.SelectedIndex > 0 && CharacterFilterCombo.SelectedItem is ComboBoxItem charItem)
            {
                // Content is set to Nickname in InitializeSelectors
                return charItem.Content as string;
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

        private async void CharacterFilterCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isInitializing || _isBusy) return;

            string? characterId = GetSelectedCharacterId();
            if (string.IsNullOrEmpty(characterId)) return;

            // UI 잠금
            _isBusy = true;
            CharacterFilterCombo.IsEnabled = false;

            try
            {
                // 데이터 자동 최신화 (RefreshIfStaleAndLoadAsync의 로직을 여기에 통합/호출)
                await RefreshIfStaleAndLoadAsync();
            }
            finally
            {
                _isBusy = false;
                CharacterFilterCombo.IsEnabled = true;
            }
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
                var characterName = _viewModel.SelectedCharacter?.Nickname ?? "";
                var summary = RawDataProcessor.GetDataSummary(characterName);
                if (summary.OldestDate.HasValue && summary.NewestDate.HasValue)
                {
                    // var characterName = ... (Removed redundant declaration)

                    // 1. 기존 분석 데이터 초기화 (장비, 스킬 내역 삭제)
                    StatisticsService.ClearAnalysisData(characterId, characterName);

                    // 2. 전투력 데이터 갱신 (raw stat 파일에서 다시 로드)
                    RawDataProcessor.RefreshCombatPowerFromRaw(characterId, characterName);

                    // 3. 전체 기간에 대해 장비 변경 재분석 수행
                    RawDataProcessor.ProcessItemChangesFromRaw(
                        characterId,
                        characterName,
                        summary.OldestDate.Value,
                        summary.NewestDate.Value
                    );

                    // 4. 전체 기간에 대해 6차 스킬 변경 재분석 수행 (누락된 로직 추가)
                    RawDataProcessor.ProcessHexaSkillChangesFromRaw(
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
            if (!specificDates.Any())
            {
                MessageBox.Show("캘린더에서 수집할 날짜를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 공통 수집 메서드 호출 (결과는 OnCollectCompleted에서 처리)
            _ = StartCollectionAsync(character, specificDates);
        }

        private Task StartCollectionAsync(CharacterProfile character, List<DateTime> dates)
        {
            if (App.IsCollecting) return Task.CompletedTask;
            if (string.IsNullOrEmpty(character.Ocid)) return Task.CompletedTask;

            // UI 업데이트
            if (CollectHistoryButton != null)
            {
                CollectHistoryButton.IsEnabled = false;
                CollectHistoryButton.Content = "수집 중...";
            }
            if (CollectProgressBar != null)
            {
                CollectProgressBar.Visibility = Visibility.Visible;
                CollectProgressBar.IsIndeterminate = true;
                CollectProgressBar.Value = 0;
            }
            _collectStartTime = DateTime.Now;
            if (CollectStatusText != null)
            {
                CollectStatusText.Text = "0% (예상 완료 시간 계산 중...)";
            }

            // 이벤트 구독
            App.CollectProgressChanged += OnCollectProgressChanged;
            App.CollectCompleted += OnCollectCompleted;

            _collectionTcs = new TaskCompletionSource<bool>();

            // 알림 서비스 가져오기
            var notificationService = MainWindow.Instance?.NotificationServiceInstance;

            // 백그라운드 수집 시작
            App.StartBackgroundCollect(
                character.Ocid!, // 위에서 체크함
                character.Id,
                character.Nickname,
                dates.Count,
                dates,
                notificationService
            );

            return _collectionTcs.Task;
        }

        private void OnCollectProgressChanged(int progress)
        {
            // UI 스레드에서 실행
            Dispatcher.Invoke(() =>
            {
                if (CollectProgressBar == null) return;

                CollectProgressBar.IsIndeterminate = false;
                CollectProgressBar.Value = progress;
                _lastProgress = progress;

                if (_collectStartTime.HasValue && progress > 0 && progress <= 100)
                {
                    var elapsed = DateTime.Now - _collectStartTime.Value;
                    var remainingSeconds = (int)(elapsed.TotalSeconds * (100 - progress) / progress);
                    _remainingSeconds = remainingSeconds;

                    // 타이머가 없으면 생성 및 시작
                    if (_countdownTimer == null)
                    {
                        _countdownTimer = new System.Windows.Threading.DispatcherTimer
                        {
                            Interval = TimeSpan.FromSeconds(1)
                        };
                        _countdownTimer.Tick += CountdownTimer_Tick;
                        _countdownTimer.Start();
                    }

                    UpdateCountdownText();
                }
                else
                {
                    CollectStatusText.Text = $"{progress}%";
                }
            });
        }

        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
                UpdateCountdownText();
            }
        }

        private void UpdateCountdownText()
        {
            if (CollectStatusText == null) return;

            var minutes = _remainingSeconds / 60;
            var seconds = _remainingSeconds % 60;
            var etaText = $"{minutes:D2}:{seconds:D2}";
            CollectStatusText.Text = $"{_lastProgress}% (예상 남은 시간: {etaText})";
        }

        private void StopCountdownTimer()
        {
            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer.Tick -= CountdownTimer_Tick;
                _countdownTimer = null;
            }
            _remainingSeconds = 0;
            _lastProgress = 0;
        }

        private void OnCollectCompleted(bool success, string message)
        {
            // 이벤트 구독 해제
            App.CollectProgressChanged -= OnCollectProgressChanged;
            App.CollectCompleted -= OnCollectCompleted;

            // 카운트다운 타이머 정지
            Dispatcher.Invoke(() => StopCountdownTimer());

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

                // TCS 완료 처리
                _collectionTcs?.TrySetResult(success);
                _collectionTcs = null;

                // 데이터 갱신
                bool isRealSuccess = success;
                string displayMessage = "수집 완료!";

                // 0건 수집 (특히 오늘 날짜 포함 시) 확인
                if (success && message.Contains("0일치"))
                {
                    if (_selectedDates.Contains(DateTime.Today) || _selectedDates.Any(d => d.Date == DateTime.Today))
                    {
                        // 오늘은 0건이라도 성공으로 처리 (이미 최신이거나 갱신됨)
                        displayMessage = "수집 완료! (최신 상태)";
                    }
                    else
                    {
                        // 다른 날짜인데 0건이면 이미 수집되었거나 데이터 없음
                        displayMessage = "수집된 데이터가 없습니다.";
                    }
                }
                else if (!success)
                {
                    displayMessage = "갱신 실패";
                    if (message.Contains("오류")) displayMessage = message; // 상세 오류 표시
                }

                if (isRealSuccess)
                {
                    // 완료 애니메이션 (성공)
                    ShowCollectCompleteAnimation(true, displayMessage);

                    // 전체 대시보드 새로고침
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
                    // 실패 애니메이션 (빨간색 토스트)
                    ShowCollectCompleteAnimation(false, displayMessage);
                    await Task.Delay(3500); // 실패 메시지는 조금 더 오래 표시
                    HideCollectCompleteAnimation();
                }
            });
        }

        private void ShowCollectCompleteAnimation(bool success, string? message = null)
        {
            if (CollectCompleteOverlay == null || CollectCompleteText == null) return;

            // 토스트 아이콘 배경 및 텍스트 설정
            var iconBorder = CollectCompleteOverlay.FindName("ToastIconBorder") as Border
                             ?? this.FindName("ToastIconBorder") as Border;
            var toastIcon = CollectCompleteOverlay.FindName("ToastIcon") as TextBlock
                            ?? this.FindName("ToastIcon") as TextBlock;

            if (iconBorder != null)
            {
                var gradient = new LinearGradientBrush { StartPoint = new System.Windows.Point(0, 0), EndPoint = new System.Windows.Point(1, 1) };

                if (success)
                {
                    // 녹색 (성공)
                    gradient.GradientStops.Add(new GradientStop(Color.FromRgb(52, 211, 153), 0)); // #34D399
                    gradient.GradientStops.Add(new GradientStop(Color.FromRgb(16, 185, 129), 1)); // #10B981
                    if (toastIcon != null) toastIcon.Text = "✓";
                }
                else
                {
                    // 빨간색 (실패)
                    gradient.GradientStops.Add(new GradientStop(Color.FromRgb(248, 113, 113), 0)); // #F87171
                    gradient.GradientStops.Add(new GradientStop(Color.FromRgb(220, 38, 38), 1));   // #DC2626
                    if (toastIcon != null) toastIcon.Text = "!";
                }
                iconBorder.Background = gradient;
            }

            // 메시지 설정
            CollectCompleteText.Text = message ?? (success ? "수집 완료!" : "갱신 실패");

            CollectCompleteOverlay.Visibility = Visibility.Visible;

            // 등장 애니메이션 (Scale + Fade)
            var sb = new System.Windows.Media.Animation.Storyboard();

            var scaleX = new System.Windows.Media.Animation.DoubleAnimation(0.8, 1.0, TimeSpan.FromMilliseconds(300)) { EasingFunction = new System.Windows.Media.Animation.BackEase { Amplitude = 0.5, EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut } };
            var scaleY = new System.Windows.Media.Animation.DoubleAnimation(0.8, 1.0, TimeSpan.FromMilliseconds(300)) { EasingFunction = new System.Windows.Media.Animation.BackEase { Amplitude = 0.5, EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut } };
            var fade = new System.Windows.Media.Animation.DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(200));

            System.Windows.Media.Animation.Storyboard.SetTarget(scaleX, ToastScaleTransform); System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleX, new PropertyPath("ScaleX"));
            System.Windows.Media.Animation.Storyboard.SetTarget(scaleY, ToastScaleTransform); System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleY, new PropertyPath("ScaleY"));
            System.Windows.Media.Animation.Storyboard.SetTarget(fade, CollectCompleteOverlay); System.Windows.Media.Animation.Storyboard.SetTargetProperty(fade, new PropertyPath("Opacity"));

            sb.Children.Add(scaleX);
            sb.Children.Add(scaleY);
            sb.Children.Add(fade);
            sb.Begin();

            // 파티클 효과 (성공 시에만)
            if (success)
            {
                StartParticleEffect();
                StartSheenEffect();
            }
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
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                AutoReverse = true
            };
            System.Windows.Media.Animation.Storyboard.SetTarget(opacityAnim, starPath);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));

            // 크기 X - 더 크게 확대
            var scaleXAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.3,
                To = 1.5,
                Duration = TimeSpan.FromMilliseconds(1000)
            };
            System.Windows.Media.Animation.Storyboard.SetTarget(scaleXAnim, starPath);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(scaleXAnim,
                new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));

            // 크기 Y - 더 크게 확대
            var scaleYAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0.3,
                To = 1.5,
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
        private void ItemSlot_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ItemChangeRecord record)
            {
                if (string.IsNullOrEmpty(record.ItemInfoJson)) return;
                ShowItemTooltip(record, element);
            }
        }

        private void ItemSlot_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HideItemTooltip();
        }

        private void ShowItemTooltip(ItemChangeRecord record, FrameworkElement target)
        {
            if (ItemTooltipPopup == null) return;

            // DataContext 설정을 통해 XAML의 Converter가 작동하여 이미지를 생성함
            ItemTooltipPopup.DataContext = record;

            // 팝업 위치 설정
            ItemTooltipPopup.PlacementTarget = target;
            ItemTooltipPopup.Placement = PlacementMode.MousePoint;
            ItemTooltipPopup.HorizontalOffset = 20;
            ItemTooltipPopup.VerticalOffset = 20;

            ItemTooltipPopup.IsOpen = true;
        }

        private void HideItemTooltip()
        {
            if (ItemTooltipPopup != null)
            {
                ItemTooltipPopup.IsOpen = false;
            }
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
        /// ThemeService에서 전역 리소스를 관리하므로 개별 창에서의 설정은 불필요합니다.
        /// </summary>
        public void ApplyThemeResources()
        {
            // ThemeService에서 설정을 하지만, MainGrid가 DynamicResource를 사용하도록 강제
            if (MainGrid != null)
            {
                MainGrid.SetResourceReference(System.Windows.Controls.Panel.BackgroundProperty, "WindowBackground");
            }
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