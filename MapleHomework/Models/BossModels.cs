using System.Windows;
using System.Windows.Media;
using MapleHomework.Services;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace MapleHomework.Models
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
}
