using System;
using System.Drawing;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MapleHomework.Controls;
using MapleHomework.Models;
using MapleHomework.Services;
using MapleHomework.ViewModels;
using Forms = System.Windows.Forms;

namespace MapleHomework
{
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        public static MainWindow? Instance { get; private set; }

        public MainViewModel ViewModel { get; set; }
        private Forms.NotifyIcon? _notifyIcon;
        private NotificationService? _notificationService;
        private OverlayWindow? _overlayWindow;


        /// <summary>
        /// NotificationService 인스턴스 (외부 접근용)
        /// </summary>
        public NotificationService? NotificationServiceInstance => _notificationService;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();

            Instance = this;

            ViewModel = viewModel;
            this.DataContext = ViewModel;

            // 메인 창 위치/크기 변경 시 사이드바 위치 동기화 (제거: 오버레이이므로 불필요)
            this.SizeChanged += MainWindow_SizeChanged;

            InitializeNotifyIcon();
            InitializeServices();
            LoadSavedData();
            RestoreWindowPosition(); // 저장된 위치 복원

            // 창 크기 변경 시 열 수 업데이트
            this.Loaded += (s, e) =>
            {
                UpdateTaskColumnCount();
                ShowTutorialIfFirstRun();
            };
        }

        private void ShowTutorialIfFirstRun()
        {
            try
            {
                var settings = Models.ConfigManager.Load();
                if (!settings.HasSeenTutorial)
                {
                    var tutorial = new TutorialOverlay();
                    tutorial.Owner = this;
                    tutorial.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                // 튜토리얼 오류 시 건너뛰기
                System.Diagnostics.Debug.WriteLine($"Tutorial error: {ex.Message}");
                var settings = Models.ConfigManager.Load();
                settings.HasSeenTutorial = true;
                Models.ConfigManager.Save(settings);
            }
        }



        // 사이드바 오버레이 배경 클릭 시 닫기
        private void SidebarOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 배경 부분만 클릭했을 때 닫기 (내부 컨텐츠 클릭 제외)
            if (e.OriginalSource == sender || (e.OriginalSource is System.Windows.Controls.Border b && b.Name == ""))
            {
                ViewModel.IsSidebarOpen = false;
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
        }

        private void UpdateTaskColumnCount()
        {
            // 콘텐츠 영역 너비 (좌우 패딩 제외)
            double contentWidth = this.ActualWidth - 80;

            // 사용자 요청: 3열 기준 670px로 조정
            // 3열: 670px 이상
            // 2열: 430px 이상

            if (contentWidth >= 670)
            {
                ViewModel.TaskColumnCount = 3;
            }
            else if (contentWidth >= 430)
            {
                ViewModel.TaskColumnCount = 2;
            }
            else
            {
                ViewModel.TaskColumnCount = 1;
            }
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
            contextMenu.Items.Add("시작 팝업 테스트", null, (s, e) => ShowStartupPopupTest());
            contextMenu.Items.Add("튜토리얼 보기", null, (s, e) => ShowTutorialManually());
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

        /// <summary>
        /// 시작 팝업 테스트 (트레이 메뉴)
        /// </summary>
        private void ShowStartupPopupTest()
        {
            var popup = new StartupPopupWindow();
            popup.Show();
        }

        /// <summary>
        /// 튜토리얼 수동 표시 (트레이 메뉴)
        /// </summary>
        private void ShowTutorialManually()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    ShowWindow(); // 윈도우 보이기 및 활성화

                    var tutorial = new TutorialOverlay();
                    tutorial.Owner = this;
                    tutorial.ShowDialog();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Tutorial error: {ex.Message}");
                    System.Windows.MessageBox.Show($"튜토리얼 실행 중 오류가 발생했습니다:\n{ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
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
            // _sidebarWindow?.Close(); // 통합으로 인해 제거
            _notifyIcon?.Dispose();
            base.OnClosed(e);
        }

        #region 하단바 메뉴 이벤트

        private void BottomMenuButton_Click(object sender, RoutedEventArgs e)
        {
            MenuOverlay.Visibility = MenuOverlay.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void CloseMenuPopup(object sender, RoutedEventArgs e)
        {
            MenuOverlay.Visibility = Visibility.Collapsed;
        }

        private void OpenDashboard_Click(object sender, RoutedEventArgs e)
        {
            MenuOverlay.Visibility = Visibility.Collapsed;
            var appData = new AppData { Characters = ViewModel.Characters.ToList() };
            var dashboardWindow = new DashboardWindow(appData, ViewModel);
            // Owner 제거 - 다른 창 뒤로 갈 수 있도록
            dashboardWindow.Show();
        }

        private void OpenStatisticsReport_Click(object sender, RoutedEventArgs e)
        {
            MenuOverlay.Visibility = Visibility.Collapsed;
            var reportWindow = new ReportWindow(ViewModel);
            // Owner 제거 - 다른 창 뒤로 갈 수 있도록
            reportWindow.Show();
        }

        private void OpenBossCalculator_Click(object sender, RoutedEventArgs e)
        {
            MenuOverlay.Visibility = Visibility.Collapsed;
            var appData = new AppData { Characters = ViewModel.Characters.ToList() };
            var bossWindow = new BossRewardWindow(appData, ViewModel);
            // Owner 제거 - 다른 창 뒤로 갈 수 있도록
            bossWindow.Show();
        }

        private void OpenCharacterSearch_Click(object sender, RoutedEventArgs e)
        {
            MenuOverlay.Visibility = Visibility.Collapsed;
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
            MenuOverlay.Visibility = Visibility.Collapsed;
            var settingsWindow = new SettingsWindow(ViewModel);
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        #endregion

        #region Character Drag & Drop

        private CharacterProfile? _draggedCharacter;
        private System.Windows.Point _dragStartPoint;
        private DragAdorner? _dragAdorner;
        private AdornerLayer? _adornerLayer;
        private FrameworkElement? _draggedElement;
        private CharacterProfile? _lastHoveredCharacter;

        private void CharacterCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            if (sender is FrameworkElement element && element.DataContext is CharacterProfile character)
            {
                _draggedCharacter = character;
                _draggedElement = element;
            }
        }

        private void CharacterCard_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _draggedCharacter == null || _draggedElement == null) return;

            var currentPos = e.GetPosition(null);
            var diff = _dragStartPoint - currentPos;

            // 드래그 시작 임계값 (5픽셀)
            if (Math.Abs(diff.X) > 5 || Math.Abs(diff.Y) > 5)
            {
                // Adorner 생성
                CreateDragAdorner(_draggedElement);

                var data = new System.Windows.DataObject("CharacterProfile", _draggedCharacter);

                // 드래그 중 위치 업데이트를 위한 이벤트 연결
                _draggedElement.QueryContinueDrag += OnQueryContinueDrag;

                DragDrop.DoDragDrop(_draggedElement, data, System.Windows.DragDropEffects.Move);

                // 드래그 종료 후 정리
                _draggedElement.QueryContinueDrag -= OnQueryContinueDrag;
                RemoveDragAdorner();
                ResetCardTransforms();

                _draggedCharacter = null;
                _draggedElement = null;
                _lastHoveredCharacter = null;
            }
        }

        private void CreateDragAdorner(FrameworkElement element)
        {
            if (_adornerLayer != null) return;

            _adornerLayer = AdornerLayer.GetAdornerLayer(this);
            if (_adornerLayer == null) return;

            // VisualBrush로 카드 복제
            var visualBrush = new VisualBrush(element)
            {
                Opacity = 0.9,
                Stretch = Stretch.None
            };

            _dragAdorner = new DragAdorner(this, element.ActualWidth, element.ActualHeight, visualBrush);

            // 마우스 오프셋 설정
            var mousePos = System.Windows.Input.Mouse.GetPosition(element);
            _dragAdorner.SetOffsets(mousePos.X, mousePos.Y);

            _adornerLayer.Add(_dragAdorner);

            // 초기 위치 설정
            var screenPos = System.Windows.Input.Mouse.GetPosition(this);
            _dragAdorner.UpdatePosition(screenPos.X, screenPos.Y);
        }

        private void RemoveDragAdorner()
        {
            if (_adornerLayer != null && _dragAdorner != null)
            {
                _adornerLayer.Remove(_dragAdorner);
            }
            _adornerLayer = null;
            _dragAdorner = null;
        }

        private void OnQueryContinueDrag(object sender, System.Windows.QueryContinueDragEventArgs e)
        {
            // 드래그 중 Adorner 위치 업데이트
            if (_dragAdorner != null)
            {
                var pos = System.Windows.Input.Mouse.GetPosition(this);
                _dragAdorner.UpdatePosition(pos.X, pos.Y);
            }
        }

        private void CharacterList_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("CharacterProfile"))
            {
                e.Effects = System.Windows.DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.Effects = System.Windows.DragDropEffects.Move;

            // 현재 마우스 위치의 캐릭터 찾기
            var targetCharacter = FindCharacterAtPosition(e);

            // 타겟 변경 시 카드 이동 애니메이션
            if (targetCharacter != null && targetCharacter != _lastHoveredCharacter && targetCharacter != _draggedCharacter)
            {
                AnimateCardShift(targetCharacter);
                _lastHoveredCharacter = targetCharacter;
            }

            // Adorner 위치 업데이트
            if (_dragAdorner != null)
            {
                var pos = e.GetPosition(this);
                _dragAdorner.UpdatePosition(pos.X, pos.Y);
            }

            e.Handled = true;
        }

        private CharacterProfile? FindCharacterAtPosition(System.Windows.DragEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement element)
            {
                var current = element;
                while (current != null && !(current.DataContext is CharacterProfile))
                {
                    current = LogicalTreeHelper.GetParent(current) as FrameworkElement
                           ?? VisualTreeHelper.GetParent(current) as FrameworkElement;
                }
                return current?.DataContext as CharacterProfile;
            }
            return null;
        }

        private void AnimateCardShift(CharacterProfile hoveredCharacter)
        {
            // 간단한 시각적 피드백: 호버된 카드의 테두리 하이라이트
            // (복잡한 TranslateTransform 애니메이션은 ItemsControl 구조상 어려움)
            // 실제 재정렬은 Drop에서 처리
        }

        private void CharacterCard_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 드래그가 시작되지 않았으면 클릭으로 선택
            if (_draggedCharacter != null && _dragAdorner == null)
            {
                // Adorner가 생성되지 않았다면 드래그가 시작되지 않은 것 = 클릭
                ViewModel.SelectCharacterCommand.Execute(_draggedCharacter);
            }
            _draggedCharacter = null;
            _draggedElement = null;
        }

        private void ResetCardTransforms()
        {
            // 모든 카드의 Transform 초기화
            _lastHoveredCharacter = null;
        }

        private void CharacterList_Drop(object sender, System.Windows.DragEventArgs e)
        {
            RemoveDragAdorner();
            ResetCardTransforms();

            if (!e.Data.GetDataPresent("CharacterProfile")) return;

            var sourceCharacter = e.Data.GetData("CharacterProfile") as CharacterProfile;
            if (sourceCharacter == null) return;

            var targetCharacter = FindCharacterAtPosition(e);

            if (targetCharacter != null && sourceCharacter != targetCharacter)
            {
                ViewModel.MoveCharacter(sourceCharacter, targetCharacter);
            }
        }

        private void CharacterDragDropList_ItemsReordered(object sender, RoutedEventArgs e)
        {
            if (e is Controls.ItemsReorderedEventArgs args)
            {
                ViewModel.MoveCharacterByIndex(args.OldIndex, args.NewIndex);
            }
        }

        #endregion
    }
}
