using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using MapleHomework.Models;
using MapleHomework.Services;
using MapleHomework.ViewModels;
using Forms = System.Windows.Forms;

namespace MapleHomework
{
    public partial class MainWindow : Window
    {
        public static MainWindow? Instance { get; private set; }
        
        public MainViewModel ViewModel { get; set; }
        private Forms.NotifyIcon? _notifyIcon;
        private NotificationService? _notificationService;
        private OverlayWindow? _overlayWindow;
        private SidebarWindow? _sidebarWindow;

        /// <summary>
        /// NotificationService 인스턴스 (외부 접근용)
        /// </summary>
        public NotificationService? NotificationServiceInstance => _notificationService;

        public MainWindow()
        {
            Instance = this;
            
            InitializeComponent();
            ViewModel = new MainViewModel();
            this.DataContext = ViewModel;
            
            // 사이드바 상태 변경 구독
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            
            // 메인 창 위치/크기 변경 시 사이드바 위치 동기화
            this.LocationChanged += MainWindow_LocationChanged;
            this.SizeChanged += MainWindow_SizeChanged;
            this.StateChanged += MainWindow_StateChanged;
            
            InitializeNotifyIcon();
            InitializeServices();
            LoadSavedData();
            RestoreWindowPosition(); // 저장된 위치 복원
            
            // 창 크기 변경 시 열 수 업데이트 및 사이드바 초기화
            this.Loaded += (s, e) => 
            {
                UpdateTaskColumnCount();
                InitializeSidebarWindow();
            };
        }

        private void InitializeSidebarWindow()
        {
            _sidebarWindow = new SidebarWindow(ViewModel, OnSidebarCharacterSelected);
            _sidebarWindow.Owner = this;
        }

        private void OnSidebarCharacterSelected(CharacterProfile character)
        {
            ViewModel.SelectedCharacter = character;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.IsSidebarOpen))
            {
                ToggleSidebarWindow();
            }
        }

        private void ToggleSidebarWindow()
        {
            if (_sidebarWindow == null) return;
            
            if (ViewModel.IsSidebarOpen)
            {
                UpdateSidebarPosition();
                _sidebarWindow.ShowWithAnimation();
            }
            else
            {
                _sidebarWindow.HideWithAnimation();
            }
        }

        private void UpdateSidebarPosition()
        {
            if (_sidebarWindow == null) return;
            
            _sidebarWindow.UpdatePosition(this.Left, this.Top, this.ActualHeight);
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            if (ViewModel.IsSidebarOpen)
            {
                UpdateSidebarPosition();
            }
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (_sidebarWindow == null) return;
            
            if (this.WindowState == WindowState.Minimized)
            {
                _sidebarWindow.Hide();
            }
            else if (this.WindowState == WindowState.Normal && ViewModel.IsSidebarOpen)
            {
                UpdateSidebarPosition();
                _sidebarWindow.Show();
            }
        }

        private void RestoreWindowPosition()
        {
            var settings = ConfigManager.Load();
            
            // 저장된 위치가 유효한지 확인
            if (!double.IsNaN(settings.MainWindowLeft) && !double.IsNaN(settings.MainWindowTop))
            {
                // 화면 범위 내에 있는지 확인
                var screenWidth = SystemParameters.VirtualScreenWidth;
                var screenHeight = SystemParameters.VirtualScreenHeight;
                var screenLeft = SystemParameters.VirtualScreenLeft;
                var screenTop = SystemParameters.VirtualScreenTop;

                if (settings.MainWindowLeft >= screenLeft && 
                    settings.MainWindowLeft < screenLeft + screenWidth - 100 &&
                    settings.MainWindowTop >= screenTop && 
                    settings.MainWindowTop < screenTop + screenHeight - 100)
                {
                    this.Left = settings.MainWindowLeft;
                    this.Top = settings.MainWindowTop;
                    this.WindowStartupLocation = WindowStartupLocation.Manual;
                }
            }
            
            // 저장된 크기 복원
            if (settings.MainWindowWidth > 0 && settings.MainWindowHeight > 0)
            {
                this.Width = settings.MainWindowWidth;
                this.Height = settings.MainWindowHeight;
            }
        }

        private void SaveWindowPosition()
        {
            var settings = ConfigManager.Load();
            
            // 최소화 상태가 아닐 때만 저장
            if (this.WindowState == WindowState.Normal)
            {
                settings.MainWindowLeft = this.Left;
                settings.MainWindowTop = this.Top;
                settings.MainWindowWidth = this.Width;
                settings.MainWindowHeight = this.Height;
                ConfigManager.Save(settings);
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTaskColumnCount();
            
            // 사이드바 위치도 업데이트
            if (ViewModel.IsSidebarOpen)
            {
                UpdateSidebarPosition();
            }
        }

        private void UpdateTaskColumnCount()
        {
            // 콘텐츠 영역 너비 기준으로 열 수 결정
            double contentWidth = this.ActualWidth - 80; // 좌우 패딩 고려
            
            // 각 박스당 최소 200px 필요
            const double minItemWidth = 200;
            
            if (contentWidth >= minItemWidth * 3)
                ViewModel.TaskColumnCount = 3;
            else if (contentWidth >= minItemWidth * 2)
                ViewModel.TaskColumnCount = 2;
            else
                ViewModel.TaskColumnCount = 1;
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new Forms.NotifyIcon();
            try
            {
                // 트레이 아이콘을 WPF 리소스에서 로드 (단일 exe 배포 지원)
                var iconUri = new Uri("pack://application:,,,/icon.ico", UriKind.Absolute);
                var streamInfo = System.Windows.Application.GetResourceStream(iconUri);
                if (streamInfo != null)
                {
                    _notifyIcon.Icon = new System.Drawing.Icon(streamInfo.Stream);
                }
                else
                {
                    // 파일 시스템에서 시도 (개발 환경 등)
                var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                }
                else
                {
                    _notifyIcon.Icon = SystemIcons.Application;
                    }
                }
            }
            catch
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }
            
            _notifyIcon.Text = "Maple Scheduler";
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();

            // 컨텍스트 메뉴
            var contextMenu = new Forms.ContextMenuStrip();
            contextMenu.Items.Add("열기", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("-"); // 구분선
            contextMenu.Items.Add("알림 테스트", null, (s, e) => _notificationService?.TestNotification());
            contextMenu.Items.Add("-"); // 구분선
            contextMenu.Items.Add("종료", null, (s, e) => ExitApplication());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void InitializeServices()
        {
            // 알림 서비스 초기화
            if (_notifyIcon != null)
            {
                _notificationService = new NotificationService(_notifyIcon);
                _notificationService.Start();
            }

            // 오버레이 윈도우 초기화
            _overlayWindow = new OverlayWindow();
            _overlayWindow.StartOverlay();
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void ExitApplication()
        {
            // 모든 데이터 저장
            ViewModel.SaveAllData();

            // 서비스 정리
            _notificationService?.Stop();
            _overlayWindow?.StopOverlay();
            _overlayWindow?.Close();

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            System.Windows.Application.Current.Shutdown();
        }

        private void LoadSavedData()
        {
            var settings = ConfigManager.Load();
            // 자동 저장된 선택된 캐릭터 로드 (MainViewModel 생성자에서 처리됨)
            // API 키는 서버에서 처리됨
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // 트레이로 숨기기 전에 데이터와 위치 저장
            SaveWindowPosition();
            ViewModel.SaveAllData();
            
            this.Hide();
            _notifyIcon?.ShowBalloonTip(1000, "Maple Scheduler", "프로그램이 트레이로 최소화되었습니다.", Forms.ToolTipIcon.Info);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(ViewModel);
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }
        
        protected override void OnClosed(EventArgs e)
        {
            // 위치와 모든 데이터 저장
            SaveWindowPosition();
            ViewModel.SaveAllData();

            _notificationService?.Stop();
            _overlayWindow?.StopOverlay();
            _overlayWindow?.Close();
            _sidebarWindow?.Close();
            _notifyIcon?.Dispose();
            base.OnClosed(e);
        }

        #region 하단바 메뉴 이벤트
        
        private void BottomMenuButton_Click(object sender, RoutedEventArgs e)
        {
            BottomMenuPopup.IsOpen = !BottomMenuPopup.IsOpen;
        }

        private void CloseMenuPopup(object sender, RoutedEventArgs e)
        {
            BottomMenuPopup.IsOpen = false;
        }

        private void OpenDashboard_Click(object sender, RoutedEventArgs e)
        {
            BottomMenuPopup.IsOpen = false;
            var appData = new AppData { Characters = ViewModel.Characters.ToList() };
            var dashboardWindow = new DashboardWindow(appData, ViewModel);
            // Owner 제거 - 다른 창 뒤로 갈 수 있도록
            dashboardWindow.Show();
        }

        private void OpenStatisticsReport_Click(object sender, RoutedEventArgs e)
        {
            BottomMenuPopup.IsOpen = false;
            var reportWindow = new ReportWindow(ViewModel);
            // Owner 제거 - 다른 창 뒤로 갈 수 있도록
            reportWindow.Show();
        }

        private void OpenBossCalculator_Click(object sender, RoutedEventArgs e)
        {
            BottomMenuPopup.IsOpen = false;
            var appData = new AppData { Characters = ViewModel.Characters.ToList() };
            var bossWindow = new BossRewardWindow(appData, ViewModel);
            // Owner 제거 - 다른 창 뒤로 갈 수 있도록
            bossWindow.Show();
        }

        private void OpenCharacterSearch_Click(object sender, RoutedEventArgs e)
        {
            BottomMenuPopup.IsOpen = false;
            var searchWindow = new CharacterSearchWindow();
            // Owner 제거 - 다른 창 뒤로 갈 수 있도록
            
            // 메인 UI 테마 동기화
            searchWindow.SyncTheme(ViewModel.IsDarkTheme);
            
            // 테마 변경 이벤트 구독
            void OnThemeChanged() => searchWindow.SyncTheme(ViewModel.IsDarkTheme);
            ViewModel.ThemeChanged += OnThemeChanged;
            searchWindow.Closed += (s, args) => ViewModel.ThemeChanged -= OnThemeChanged;
            
            searchWindow.Show();
        }
        
        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            BottomMenuPopup.IsOpen = false;
            var settingsWindow = new SettingsWindow(ViewModel);
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            // 다크/라이트 모드 전환 (현재는 다크 모드만 지원)
            // TODO: 라이트 모드 구현
            System.Windows.MessageBox.Show("라이트 모드는 추후 지원 예정입니다.", "알림", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        #endregion
    }
}
