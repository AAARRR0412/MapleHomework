using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using MapleHomework.Models;
using MapleHomework.Services;

namespace MapleHomework
{


    public partial class OverlayWindow : Window, INotifyPropertyChanged
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hMonitor, int dpiType, out uint dpiX, out uint dpiY);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const uint MONITOR_DEFAULTTONEAREST = 2;
        private const int MDT_EFFECTIVE_DPI = 0;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion

        private readonly DispatcherTimer _updateTimer;
        private IntPtr _mapleWindowHandle = IntPtr.Zero;
        private bool _isMapleRunning = false;

        public ObservableCollection<TodoItem> Tasks { get; set; } = new();

        public bool HasTasks => Tasks.Any();
        public bool AllDone => !Tasks.Any();
        public int TaskCount => Tasks.Count;

        // 현재 선택된 캐릭터 정보
        private string _characterName = "";
        public string CharacterName
        {
            get => _characterName;
            set { _characterName = value; OnPropertyChanged(); }
        }

        private int _characterLevel = 0;
        public int CharacterLevel
        {
            get => _characterLevel;
            set { _characterLevel = value; OnPropertyChanged(); }
        }

        private string? _characterImage;
        public string? CharacterImage
        {
            get => _characterImage;
            set { _characterImage = value; OnPropertyChanged(); }
        }

        // 진행률
        private double _dailyProgress = 0;
        public double DailyProgress
        {
            get => _dailyProgress;
            set { _dailyProgress = value; OnPropertyChanged(); OnPropertyChanged(nameof(DailyProgressWidth)); }
        }

        private double _weeklyProgress = 0;
        public double WeeklyProgress
        {
            get => _weeklyProgress;
            set { _weeklyProgress = value; OnPropertyChanged(); OnPropertyChanged(nameof(WeeklyProgressWidth)); }
        }

        private double _bossProgress = 0;
        public double BossProgress
        {
            get => _bossProgress;
            set { _bossProgress = value; OnPropertyChanged(); OnPropertyChanged(nameof(BossProgressWidth)); }
        }

        // 완료/총개수 표시용
        private int _dailyCompleted = 0;
        public int DailyCompleted
        {
            get => _dailyCompleted;
            set { _dailyCompleted = value; OnPropertyChanged(); }
        }

        private int _dailyTotal = 0;
        public int DailyTotal
        {
            get => _dailyTotal;
            set { _dailyTotal = value; OnPropertyChanged(); }
        }

        private int _weeklyCompleted = 0;
        public int WeeklyCompleted
        {
            get => _weeklyCompleted;
            set { _weeklyCompleted = value; OnPropertyChanged(); }
        }

        private int _weeklyTotal = 0;
        public int WeeklyTotal
        {
            get => _weeklyTotal;
            set { _weeklyTotal = value; OnPropertyChanged(); }
        }

        private int _bossCompleted = 0;
        public int BossCompleted
        {
            get => _bossCompleted;
            set { _bossCompleted = value; OnPropertyChanged(); }
        }

        private int _bossTotal = 0;
        public int BossTotal
        {
            get => _bossTotal;
            set { _bossTotal = value; OnPropertyChanged(); }
        }

        // 게이지 바 너비 (최대 150px 기준)
        private const double MaxGaugeWidth = 150;
        public double DailyProgressWidth => (DailyProgress / 100) * MaxGaugeWidth;
        public double WeeklyProgressWidth => (WeeklyProgress / 100) * MaxGaugeWidth;
        public double BossProgressWidth => (BossProgress / 100) * MaxGaugeWidth;

        private double _overlayOpacity = 0.8;
        public double OverlayOpacity
        {
            get => _overlayOpacity;
            set
            {
                _overlayOpacity = value;
                OnPropertyChanged();
            }
        }

        public OverlayWindow()
        {
            InitializeComponent();
            DataContext = this;

            // 클릭 투과 설정
            Loaded += (s, e) => MakeClickThrough();

            // 업데이트 타이머 (500ms마다 - 더 빠른 반응)
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _updateTimer.Tick += UpdateOverlay;
        }

        /// <summary>
        /// 오버레이 시작
        /// </summary>
        public void StartOverlay()
        {
            RefreshTasks();
            LoadSettings();
            _updateTimer.Start();
        }

        /// <summary>
        /// 오버레이 중지
        /// </summary>
        public void StopOverlay()
        {
            _updateTimer.Stop();
            this.Hide();
        }

        /// <summary>
        /// 설정 로드
        /// </summary>
        private void LoadSettings()
        {
            var settings = ConfigManager.Load();
            OverlayOpacity = settings.OverlayOpacity;
        }

        /// <summary>
        /// 마우스 클릭 투과 설정
        /// </summary>
        private void MakeClickThrough()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW);
        }

        /// <summary>
        /// DPI 스케일 팩터 가져오기
        /// </summary>
        private double GetDpiScale(IntPtr hwnd)
        {
            try
            {
                var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    int result = GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out uint dpiX, out uint _);
                    if (result == 0) // S_OK
                    {
                        return dpiX / 96.0;
                    }
                }
            }
            catch
            {
                // DPI 가져오기 실패 시 기본값
            }

            // 기본 DPI 스케일 (WPF 방식)
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
            {
                return source.CompositionTarget.TransformToDevice.M11;
            }

            return 1.0;
        }

        /// <summary>
        /// 오버레이 위치/표시 업데이트
        /// </summary>
        private void UpdateOverlay(object? sender, EventArgs e)
        {
            var settings = ConfigManager.Load();

            // 오버레이가 비활성화되어 있으면 숨김
            if (!settings.IsOverlayEnabled)
            {
                if (this.IsVisible) this.Hide();
                return;
            }

            // 투명도 업데이트
            OverlayOpacity = settings.OverlayOpacity;

            // 설정된 프로세스 찾기
            string processName = settings.OverlayProcessName;
            if (string.IsNullOrEmpty(processName)) processName = "MapleStory";

            var mapleProcess = Process.GetProcessesByName(processName).FirstOrDefault();

            if (mapleProcess == null)
            {
                _isMapleRunning = false;
                _mapleWindowHandle = IntPtr.Zero;
                if (this.IsVisible) this.Hide();
                return;
            }

            _mapleWindowHandle = mapleProcess.MainWindowHandle;

            if (_mapleWindowHandle == IntPtr.Zero)
            {
                if (this.IsVisible) this.Hide();
                return;
            }

            // MapleStory가 포커스 상태인지 확인
            var foregroundWindow = GetForegroundWindow();
            GetWindowThreadProcessId(foregroundWindow, out uint foregroundPid);

            bool isMapleActive = (foregroundPid == mapleProcess.Id);

            if (!isMapleActive)
            {
                if (this.IsVisible) this.Hide();
                return;
            }

            // MapleStory 창 위치 가져오기
            if (GetWindowRect(_mapleWindowHandle, out RECT rect))
            {
                // DPI 스케일링 적용
                double dpiScale = GetDpiScale(_mapleWindowHandle);

                // 물리 픽셀을 WPF 논리 픽셀로 변환
                double windowLeft = rect.Left / dpiScale;
                double windowTop = rect.Top / dpiScale;
                double windowRight = rect.Right / dpiScale;
                double windowWidth = windowRight - windowLeft;

                // 우측 상단에 배치 (여백 20픽셀)
                double overlayWidth = this.ActualWidth > 0 ? this.ActualWidth : 300;
                double margin = 20;
                double newLeft = windowRight - overlayWidth - margin;
                double newTop = windowTop + 50;

                // 창이 화면 밖으로 나가지 않도록 보정
                if (newLeft < windowLeft)
                {
                    newLeft = windowLeft + margin;
                }

                // 위치 업데이트
                this.Left = newLeft;
                this.Top = newTop;

                if (!this.IsVisible)
                {
                    RefreshTasks();
                    this.Show();
                }
            }

            // 새로 시작된 경우 태스크 새로고침
            if (!_isMapleRunning)
            {
                _isMapleRunning = true;
                RefreshTasks();
            }
        }

        /// <summary>
        /// 숙제 목록 새로고침
        /// </summary>
        public void RefreshTasks()
        {
            Tasks.Clear();

            var appData = CharacterRepository.Load();
            var settings = ConfigManager.Load();

            // 현재 선택된 캐릭터 찾기
            var selectedCharacter = appData.Characters
                .FirstOrDefault(c => c.Id == appData.SelectedCharacterId)
                ?? appData.Characters.FirstOrDefault();

            if (selectedCharacter != null)
            {
                // 캐릭터 정보 업데이트
                CharacterName = selectedCharacter.Nickname;
                CharacterLevel = selectedCharacter.Level;
                CharacterImage = selectedCharacter.ImageUrl;

                // 진행률 계산 및 완료/총개수 업데이트
                var (dailyProg, dailyComp, dailyTot) = CalculateProgressWithCounts(selectedCharacter.DailyTasks);
                DailyProgress = dailyProg;
                DailyCompleted = dailyComp;
                DailyTotal = dailyTot;

                var (weeklyProg, weeklyComp, weeklyTot) = CalculateProgressWithCounts(selectedCharacter.WeeklyTasks);
                WeeklyProgress = weeklyProg;
                WeeklyCompleted = weeklyComp;
                WeeklyTotal = weeklyTot;

                var (bossProg, bossComp, bossTot) = CalculateProgressWithCounts(selectedCharacter.BossTasks);
                BossProgress = bossProg;
                BossCompleted = bossComp;
                BossTotal = bossTot;

                // 미완료 숙제 수집
                var allTasks = new List<HomeworkTask>();

                if (!settings.ShowOnlyFavorites || selectedCharacter.IsDailyFavorite)
                    allTasks.AddRange(selectedCharacter.DailyTasks.Where(t => t.IsActive && !t.IsChecked));

                if (!settings.ShowOnlyFavorites || selectedCharacter.IsWeeklyFavorite)
                    allTasks.AddRange(selectedCharacter.WeeklyTasks.Where(t => t.IsActive && !t.IsChecked));

                if (!settings.ShowOnlyFavorites || selectedCharacter.IsBossFavorite)
                    allTasks.AddRange(selectedCharacter.BossTasks.Where(t => t.IsActive && !t.IsChecked));

                if (!settings.ShowOnlyFavorites || selectedCharacter.IsMonthlyFavorite)
                    allTasks.AddRange(selectedCharacter.MonthlyTasks.Where(t => t.IsActive && !t.IsChecked));

                foreach (var task in allTasks.Take(10))
                {
                    Tasks.Add(new TodoItem
                    {
                        CharacterName = selectedCharacter.Nickname,
                        TaskName = task.DisplayName,
                        Category = task.Category
                    });
                }
            }

            OnPropertyChanged(nameof(HasTasks));
            OnPropertyChanged(nameof(AllDone));
            OnPropertyChanged(nameof(TaskCount));
        }

        /// <summary>
        /// 진행률 및 완료/총개수 계산
        /// </summary>
        private (double progress, int completed, int total) CalculateProgressWithCounts(IEnumerable<HomeworkTask> tasks)
        {
            var activeTasks = tasks.Where(t => t.IsActive).ToList();
            int total = activeTasks.Count;
            if (total == 0) return (100, 0, 0);

            int completed = activeTasks.Count(t => t.IsChecked);
            double progress = (double)completed / total * 100;
            return (progress, completed, total);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
