using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MapleHomework.Models;
using MapleHomework.Services;

namespace MapleHomework
{
    /// <summary>
    /// 캐릭터별 미완료 숙제 정보
    /// </summary>
    public class CharacterTaskInfo : INotifyPropertyChanged
    {
        public string Nickname { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public int Level { get; set; }
        public string CharacterClass { get; set; } = "";
        public ObservableCollection<PendingTaskInfo> PendingTasks { get; set; } = new();

        public bool HasPendingTasks => PendingTasks.Any();
        public int PendingCount => PendingTasks.Count;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// 미완료 숙제 정보
    /// </summary>
    public class PendingTaskInfo
    {
        public string TaskName { get; set; } = "";
        public TaskCategory Category { get; set; }
        
        public string CategoryIcon => Category switch
        {
            TaskCategory.Weekly => "📅",
            TaskCategory.Boss => "👹",
            TaskCategory.Monthly => "🗓️",
            _ => "📋"
        };
        
        public string CategoryColor => Category switch
        {
            TaskCategory.Weekly => "#CC9F5000", // 주황
            TaskCategory.Boss => "#CCFF3B30",   // 빨강
            TaskCategory.Monthly => "#CCAF52DE", // 보라
            _ => "#CC888888"
        };
    }

    public partial class StartupPopupWindow : Window, INotifyPropertyChanged
    {
        private static DateTime _lastShownDate = DateTime.MinValue;
        
        public ObservableCollection<CharacterTaskInfo> CharacterTasks { get; set; } = new();

        public StartupPopupWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadPendingTasks();
        }

        /// <summary>
        /// 팝업을 표시해야 하는지 확인
        /// </summary>
        public static bool ShouldShowPopup()
        {
            var settings = ConfigManager.Load();
            
            // 설정에서 비활성화된 경우
            if (!settings.ShowStartupPopup) return false;
            
            // 오늘 이미 표시한 경우
            if (_lastShownDate.Date == DateTime.Now.Date) return false;
            
            // 미완료 숙제가 있는지 확인
            var appData = CharacterRepository.Load();
            foreach (var character in appData.Characters)
            {
                var hasWeekly = character.WeeklyTasks.Any(t => t.IsActive && !t.IsChecked && t.IsFavorite);
                var hasBoss = character.BossTasks.Any(t => t.IsActive && !t.IsChecked && t.IsFavorite);
                var hasMonthly = character.MonthlyTasks.Any(t => t.IsActive && !t.IsChecked && t.IsFavorite);
                
                if (hasWeekly || hasBoss || hasMonthly) return true;
            }
            
            return false;
        }

        /// <summary>
        /// 미완료 숙제 로드
        /// </summary>
        private void LoadPendingTasks()
        {
            CharacterTasks.Clear();
            var appData = CharacterRepository.Load();

            foreach (var character in appData.Characters)
            {
                var pendingTasks = new ObservableCollection<PendingTaskInfo>();

                // 주간 퀘스트
                foreach (var task in character.WeeklyTasks.Where(t => t.IsActive && !t.IsChecked && t.IsFavorite))
                {
                    pendingTasks.Add(new PendingTaskInfo
                    {
                        TaskName = task.Name,
                        Category = TaskCategory.Weekly
                    });
                }

                // 주간 보스
                foreach (var task in character.BossTasks.Where(t => t.IsActive && !t.IsChecked && t.IsFavorite))
                {
                    pendingTasks.Add(new PendingTaskInfo
                    {
                        TaskName = task.Name,
                        Category = TaskCategory.Boss
                    });
                }

                // 월간 보스
                foreach (var task in character.MonthlyTasks.Where(t => t.IsActive && !t.IsChecked && t.IsFavorite))
                {
                    pendingTasks.Add(new PendingTaskInfo
                    {
                        TaskName = task.Name,
                        Category = TaskCategory.Monthly
                    });
                }

                if (pendingTasks.Any())
                {
                    CharacterTasks.Add(new CharacterTaskInfo
                    {
                        Nickname = character.Nickname,
                        ImageUrl = character.ImageUrl,
                        Level = character.Level,
                        CharacterClass = character.CharacterClass,
                        PendingTasks = pendingTasks
                    });
                }
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (DontShowAgainCheck.IsChecked == true)
            {
                _lastShownDate = DateTime.Now;
            }
            this.Close();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (DontShowAgainCheck.IsChecked == true)
            {
                _lastShownDate = DateTime.Now;
            }
            this.Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

