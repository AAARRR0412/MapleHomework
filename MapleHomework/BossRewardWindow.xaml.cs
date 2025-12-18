using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MapleHomework.Models;
using MapleHomework.Services;
using Wpf.Ui.Controls;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace MapleHomework
{


    public partial class BossRewardWindow : FluentWindow
    {
        private readonly AppData _appData;
        private readonly MapleHomework.ViewModels.MainViewModel _viewModel;
        public MapleHomework.ViewModels.MainViewModel ViewModel => _viewModel;
        private CharacterProfile? _selectedCharacter;

        public BossRewardWindow(AppData appData, MapleHomework.ViewModels.MainViewModel viewModel)
        {
            InitializeComponent();
            _appData = appData;
            _viewModel = viewModel;
            DataContext = this; // 바인딩을 위해 추가

            ApplyThemeResources();
            ThemeService.OnThemeChanged += OnThemeChanged_Service;
            _viewModel.DataChanged += OnDataChanged;

            CmbCharacter.ItemsSource = appData.Characters;
            if (appData.Characters.Any())
            {
                CmbCharacter.SelectedIndex = 0;
            }

            TxtTotalCharacters.Text = $"총 {appData.Characters.Count}개 캐릭터";
            TxtTotalCharacters.Text = $"총 {appData.Characters.Count}개 캐릭터";

            // [New] 서버 변경 감지
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            CalculateTotalRewards();
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MapleHomework.ViewModels.MainViewModel.SelectedServer))
            {
                // UI 스레드에서 실행
                Dispatcher.Invoke(() =>
                {
                    UpdateCharacterFilter();
                    CalculateTotalRewards();
                });
            }
        }

        private void UpdateCharacterFilter()
        {
            var selectedServer = _viewModel.SelectedServer;
            var filteredList = (selectedServer == "전체")
                ? _appData.Characters.ToList()
                : _appData.Characters.Where(c => c.WorldName == selectedServer).ToList();

            CmbCharacter.ItemsSource = filteredList;

            if (filteredList.Any())
            {
                if (_selectedCharacter == null || !filteredList.Contains(_selectedCharacter))
                {
                    CmbCharacter.SelectedIndex = 0;
                }
                else
                {
                    CmbCharacter.SelectedItem = _selectedCharacter;
                }
            }
            else
            {
                _selectedCharacter = null;
            }
            RefreshBossRewardList();
        }

        private void OnThemeChanged_Service(bool isDark)
        {
            Dispatcher.Invoke(() => ApplyThemeResources());
        }

        private void OnDataChanged()
        {
            Dispatcher.Invoke(() =>
            {
                // 캐릭터 목록 새로고침
                UpdateCharacterFilter();
                TxtTotalCharacters.Text = $"총 {_appData.Characters.Count}개 캐릭터";
                RefreshData();
            });
        }

        protected override void OnClosed(System.EventArgs e)
        {
            ThemeService.OnThemeChanged -= OnThemeChanged_Service;
            _viewModel.DataChanged -= OnDataChanged;
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            base.OnClosed(e);
        }

        /// <summary>
        /// 테마 리소스 적용 (다크/라이트 모드)
        /// 새 디자인은 DynamicResource를 사용하므로 App.xaml의 리소스만 업데이트하면 자동 적용됨
        /// </summary>
        public void ApplyThemeResources()
        {
            // MainGrid가 DynamicResource를 사용하도록 강제
            if (MainGrid != null)
            {
                MainGrid.SetResourceReference(System.Windows.Controls.Panel.BackgroundProperty, "WindowBackground");
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CmbCharacter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedCharacter = CmbCharacter.SelectedItem as CharacterProfile;
            RefreshBossRewardList();
        }

        /// <summary>
        /// 외부에서 호출 가능한 데이터 갱신 메서드
        /// </summary>
        public void RefreshData()
        {
            RefreshBossRewardList();
            CalculateTotalRewards();
        }

        private void RefreshBossRewardList()
        {
            if (_selectedCharacter == null) return;

            var items = new List<BossRewardItem>();
            int checkedCount = 0;
            long earnedWeeklyReward = 0;      // 획득한 수익
            long expectedWeeklyReward = 0;    // 예상 수익 (전체)
            long earnedMonthlyReward = 0;     // 획득한 월간 수익
            long expectedMonthlyReward = 0;   // 예상 월간 수익

            // 주간 보스
            foreach (var boss in _selectedCharacter.BossTasks.Where(b => b.IsActive))
            {
                var item = new BossRewardItem
                {
                    BossName = boss.Name,
                    DisplayName = boss.DisplayName,
                    Difficulty = boss.Difficulty,
                    DifficultyText = boss.Difficulty.ToString(),
                    PartySize = boss.PartySize,
                    IsChecked = boss.IsChecked,
                    IsMonthly = false,
                    TotalReward = BossRewardData.GetReward(boss.Name, boss.Difficulty, 1, false)
                };

                items.Add(item);

                // 예상 수익은 항상 더함
                expectedWeeklyReward += item.PersonalReward;

                if (boss.IsChecked)
                {
                    checkedCount++;
                    earnedWeeklyReward += item.PersonalReward;
                }
            }

            // 보상 높은 순으로 정렬
            items = items.OrderByDescending(i => i.TotalReward).ToList();

            // 월간 보스 수익 계산
            foreach (var boss in _selectedCharacter.MonthlyTasks.Where(b => b.IsActive))
            {
                var reward = BossRewardData.GetReward(boss.Name, boss.Difficulty, boss.PartySize, true);
                expectedMonthlyReward += reward;

                if (boss.IsChecked)
                {
                    earnedMonthlyReward += reward;
                }
            }

            BossRewardList.ItemsSource = items;
            TxtCompletedBossCount.Text = $"{checkedCount}/{_selectedCharacter.BossTasks.Count(b => b.IsActive)}";
            TxtWeeklyBossReward.Text = BossRewardData.FormatMeso(earnedWeeklyReward);
            TxtWeeklyBossRewardShort.Text = BossRewardData.FormatMeso(expectedWeeklyReward);
            TxtMonthlyBossReward.Text = BossRewardData.FormatMeso(earnedMonthlyReward);
            TxtMonthlyBossExpected.Text = BossRewardData.FormatMeso(expectedMonthlyReward);
        }

        private void CalculateTotalRewards()
        {
            long totalWeeklyEarned = 0;    // 획득한 주간 수익
            long totalWeeklyExpected = 0;  // 예상 주간 수익

            // [New] 현재 선택된 서버 필터링 적용
            var selectedServer = _viewModel.SelectedServer;
            var targetCharacters = (selectedServer == "전체")
                ? _appData.Characters.ToList()
                : _appData.Characters.Where(c => c.WorldName == selectedServer).ToList();

            // 라벨 업데이트
            TxtTotalLabel.Text = (selectedServer == "전체")
                ? "전체 캐릭터 합산"
                : $"{selectedServer} 서버 합산";

            foreach (var character in targetCharacters)
            {
                // 주간 보스 - 예상 수익 (전체)
                foreach (var boss in character.BossTasks.Where(b => b.IsActive))
                {
                    var reward = BossRewardData.GetReward(boss.Name, boss.Difficulty, boss.PartySize, false);
                    totalWeeklyExpected += reward;

                    if (boss.IsChecked)
                    {
                        totalWeeklyEarned += reward;
                    }
                }
            }

            TxtTotalWeeklyReward.Text = $"{BossRewardData.FormatMeso(totalWeeklyEarned)}";
            TxtTotalWeeklyExpected.Text = $"{BossRewardData.FormatMeso(totalWeeklyExpected)}";
        }
    }
}
