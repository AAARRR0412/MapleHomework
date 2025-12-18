using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using MapleHomework.Services;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace MapleHomework.Models
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



    public class HexaSkillDailyGroup
    {
        public DateTime Date { get; set; }
        public List<HexaSkillRecord> Items { get; set; } = new();
    }
}
