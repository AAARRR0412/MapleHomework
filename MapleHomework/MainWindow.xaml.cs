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
        public MainViewModel ViewModel { get; set; }
        private Forms.NotifyIcon? _notifyIcon;
        private NotificationService? _notificationService;
        private OverlayWindow? _overlayWindow;

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            this.DataContext = ViewModel;
            
            InitializeNotifyIcon();
            InitializeServices();
            LoadSavedData();
            RestoreWindowPosition(); // 저장된 위치 복원
            
            // 창 크기 변경 시 열 수 업데이트
            this.SizeChanged += MainWindow_SizeChanged;
            this.Loaded += (s, e) => UpdateTaskColumnCount();
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
        }

        private void UpdateTaskColumnCount()
        {
            // 콘텐츠 영역 너비 기준으로 열 수 결정 (대략 창 너비 - 여백)
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
                // 트레이 아이콘을 icon.ico로 설정
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
            catch
            {
                // 아이콘 로드 실패 시 무시
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
            // 필요하다면 API 키로 추가 데이터 로드
            if (!string.IsNullOrEmpty(settings.ApiKey))
            {
                // 여기서 굳이 다시 로드할 필요는 없음 (MainViewModel에서 함)
            }
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
            _notifyIcon?.Dispose();
            base.OnClosed(e);
        }
    }
}
