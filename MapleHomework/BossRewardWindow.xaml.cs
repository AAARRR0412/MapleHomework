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
    /// <summary>
    /// 보스 수익 항목 (UI 바인딩용)
    /// </summary>
    public class BossRewardItem
    {
        public string BossName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public BossDifficulty Difficulty { get; set; }
        public string DifficultyText { get; set; } = "";
        public int PartySize { get; set; } = 1;
        public bool IsChecked { get; set; }
        public bool IsMonthly { get; set; }
        
        public long TotalReward { get; set; }
        public long PersonalReward => TotalReward / PartySize;
        
        // UI 바인딩용 속성들
        public string TotalRewardText => PartySize > 1 ? $"(총 {BossRewardData.FormatMeso(TotalReward)})" : "";
        public string PersonalRewardText => BossRewardData.FormatMeso(PersonalReward);
        public string PartyInfoText => PartySize > 1 ? $"{PartySize}인 파티" : "솔로";
        
        // 파티원 2인 이상일 때만 총 보상 표시
        public Visibility ShowTotalReward => PartySize > 1 ? Visibility.Visible : Visibility.Collapsed;
        
        // 배경색 - 어두운 테마에 맞게
        public Brush Background => IsChecked 
            ? new SolidColorBrush(Color.FromArgb(40, 76, 217, 100))   // 녹색 (완료)
            : new SolidColorBrush(Color.FromRgb(85, 95, 110));        // 기본 어두운 색
        
        public Brush BorderColor => IsChecked 
            ? new SolidColorBrush(Color.FromArgb(80, 76, 217, 100))   // 녹색 테두리
            : new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)); // 투명한 테두리
        
        public Brush CheckBackground => IsChecked 
            ? new SolidColorBrush(Color.FromRgb(76, 217, 100))        // 녹색
            : new SolidColorBrush(Color.FromRgb(100, 110, 125));      // 회색
        
        public Visibility CheckVisibility => IsChecked ? Visibility.Visible : Visibility.Collapsed;
        
        public Brush DifficultyBackground => Difficulty switch
        {
            BossDifficulty.Easy => new SolidColorBrush(Color.FromRgb(90, 200, 250)),    // 파란색
            BossDifficulty.Normal => new SolidColorBrush(Color.FromRgb(52, 199, 89)),   // 녹색
            BossDifficulty.Hard => new SolidColorBrush(Color.FromRgb(255, 149, 0)),     // 주황색
            BossDifficulty.Chaos => new SolidColorBrush(Color.FromRgb(255, 59, 48)),    // 빨간색
            BossDifficulty.Extreme => new SolidColorBrush(Color.FromRgb(175, 82, 222)), // 보라색
            _ => new SolidColorBrush(Color.FromRgb(128, 128, 128))
        };
        
        // 텍스트 색상 - 어두운 배경에 밝은 글씨
        public Brush TextForeground => IsChecked 
            ? new SolidColorBrush(Color.FromArgb(180, 200, 210, 220))  // 흐릿한 밝은 색
            : new SolidColorBrush(Color.FromRgb(255, 255, 255));       // 흰색
        
        public Brush RewardForeground => IsChecked 
            ? new SolidColorBrush(Color.FromRgb(95, 224, 134))         // 밝은 녹색 (완료)
            : new SolidColorBrush(Color.FromRgb(255, 215, 0));         // 금색
    }
    
    public partial class BossRewardWindow : FluentWindow
    {
        private readonly AppData _appData;
        private CharacterProfile? _selectedCharacter;
        
        public BossRewardWindow(AppData appData)
        {
            InitializeComponent();
            _appData = appData;
            
            ApplyThemeResources();
            
            CmbCharacter.ItemsSource = appData.Characters;
            if (appData.Characters.Any())
            {
                CmbCharacter.SelectedIndex = 0;
            }
            
            TxtTotalCharacters.Text = $"총 {appData.Characters.Count}개 캐릭터";
            CalculateTotalRewards();
        }
        
        /// <summary>
        /// 테마 리소스 적용 (다크/라이트 모드)
        /// </summary>
        public void ApplyThemeResources()
        {
            var settings = ConfigManager.Load();
            bool isDark = ThemeService.ShouldUseDarkTheme(settings);
            
            if (isDark)
            {
                // 다크 모드
                MainGrid.Background = new SolidColorBrush(Color.FromRgb(61, 74, 92));
                TitleBarBorder.Background = new SolidColorBrush(Color.FromRgb(61, 74, 92));
                LeftPanelBorder.Background = new SolidColorBrush(Color.FromRgb(74, 90, 110));
                CharacterSelectBorder.Background = new SolidColorBrush(Color.FromRgb(74, 90, 110));
                WeeklyRewardBorder.Background = new SolidColorBrush(Color.FromRgb(74, 90, 110));
                MonthlyRewardBorder.Background = new SolidColorBrush(Color.FromRgb(74, 90, 110));
                CharacterSelectLabel.Foreground = new SolidColorBrush(Color.FromRgb(176, 190, 197));
            }
            else
            {
                // 라이트 모드
                MainGrid.Background = new SolidColorBrush(Color.FromRgb(245, 247, 250));
                TitleBarBorder.Background = new SolidColorBrush(Color.FromRgb(90, 103, 120));
                LeftPanelBorder.Background = new SolidColorBrush(Colors.White);
                CharacterSelectBorder.Background = new SolidColorBrush(Colors.White);
                WeeklyRewardBorder.Background = new SolidColorBrush(Colors.White);
                MonthlyRewardBorder.Background = new SolidColorBrush(Colors.White);
                CharacterSelectLabel.Foreground = new SolidColorBrush(Color.FromRgb(80, 90, 100));
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
            
            foreach (var character in _appData.Characters)
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
            
            TxtTotalWeeklyReward.Text = $"{BossRewardData.FormatMeso(totalWeeklyEarned)} 메소";
            TxtTotalWeeklyExpected.Text = $"{BossRewardData.FormatMeso(totalWeeklyExpected)} 메소";
        }
    }
}
