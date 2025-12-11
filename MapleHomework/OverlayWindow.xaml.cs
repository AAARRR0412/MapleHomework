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
    /// <summary>
    /// 오버레이에 표시할 숙제 항목
    /// </summary>
    public class OverlayTaskItem
    {
        public string CharacterName { get; set; } = "";
        public string TaskName { get; set; } = "";
        public TaskCategory Category { get; set; }
        public string CategoryColor => Category switch
        {
            TaskCategory.Daily => "#5AC8FA",
            TaskCategory.Weekly => "#FF9500",
            TaskCategory.Boss => "#FF3B30",
            TaskCategory.Monthly => "#AF52DE",
            _ => "#888888"
        };
    }

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

        public ObservableCollection<OverlayTaskItem> Tasks { get; set; } = new();
        
        public bool HasTasks => Tasks.Any();
        public bool AllDone => !Tasks.Any();
        public int TaskCount => Tasks.Count;

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

            foreach (var character in appData.Characters)
            {
                var allTasks = new List<HomeworkTask>();
                
                // 즐겨찾기 카테고리 기준으로 필터링
                if (!settings.ShowOnlyFavorites || character.IsDailyFavorite)
                    allTasks.AddRange(character.DailyTasks.Where(t => t.IsActive && !t.IsChecked));
                
                if (!settings.ShowOnlyFavorites || character.IsWeeklyFavorite)
                    allTasks.AddRange(character.WeeklyTasks.Where(t => t.IsActive && !t.IsChecked));
                
                if (!settings.ShowOnlyFavorites || character.IsBossFavorite)
                    allTasks.AddRange(character.BossTasks.Where(t => t.IsActive && !t.IsChecked));
                
                if (!settings.ShowOnlyFavorites || character.IsMonthlyFavorite)
                    allTasks.AddRange(character.MonthlyTasks.Where(t => t.IsActive && !t.IsChecked));

                var pendingTasks = allTasks.Take(5); // 캐릭터당 최대 5개

                foreach (var task in pendingTasks)
                {
                    Tasks.Add(new OverlayTaskItem
                    {
                        CharacterName = character.Nickname,
                        TaskName = task.DisplayName,
                        Category = task.Category
                    });
                }
            }

            // 전체 최대 10개로 제한
            while (Tasks.Count > 10)
            {
                Tasks.RemoveAt(Tasks.Count - 1);
            }

            OnPropertyChanged(nameof(HasTasks));
            OnPropertyChanged(nameof(AllDone));
            OnPropertyChanged(nameof(TaskCount));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
