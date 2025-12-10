using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using MapleHomework.Models;
using MapleHomework.ViewModels;
using Forms = System.Windows.Forms;

namespace MapleHomework
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; set; }
        private Forms.NotifyIcon? _notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            this.DataContext = ViewModel;
            
            InitializeNotifyIcon();
            LoadSavedData();
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new Forms.NotifyIcon();
            try
            {
                // 앱 아이콘 가져오기 (없으면 시스템 기본 아이콘)
                _notifyIcon.Icon = SystemIcons.Application;
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
            contextMenu.Items.Add("종료", null, (s, e) => ExitApplication());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void ExitApplication()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            System.Windows.Application.Current.Shutdown();
        }

        private async void LoadSavedData()
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
            _notifyIcon?.Dispose();
            base.OnClosed(e);
        }
    }
}
