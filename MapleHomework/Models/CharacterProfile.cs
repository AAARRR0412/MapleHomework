using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MapleHomework.Models
{
    /// <summary>
    /// 캐릭터 프로필 정보
    /// </summary>
    public class CharacterProfile : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        private string _nickname = "";
        public string Nickname
        {
            get => _nickname;
            set { _nickname = value; OnPropertyChanged(); }
        }

        private string _worldName = "";
        public string WorldName
        {
            get => _worldName;
            set { _worldName = value; OnPropertyChanged(); }
        }

        private string _characterClass = "";
        public string CharacterClass
        {
            get => _characterClass;
            set { _characterClass = value; OnPropertyChanged(); }
        }

        private int _level = 0;
        public int Level
        {
            get => _level;
            set { _level = value; OnPropertyChanged(); }
        }

        private string _imageUrl = "";
        public string ImageUrl
        {
            get => _imageUrl;
            set { _imageUrl = value; OnPropertyChanged(); }
        }

        // API 관련
        public string? Ocid { get; set; }
        public DateTime LastUpdatedTime { get; set; } = DateTime.MinValue;

        // 태스크 데이터
        public List<HomeworkTask> DailyTasks { get; set; } = new();
        public List<HomeworkTask> WeeklyTasks { get; set; } = new();
        public List<HomeworkTask> BossTasks { get; set; } = new();
        public List<HomeworkTask> MonthlyTasks { get; set; } = new();

        // 진행률 계산
        public double DailyProgress
        {
            get
            {
                var active = DailyTasks.FindAll(t => t.IsActive);
                if (active.Count == 0) return 0;
                return (double)active.FindAll(t => t.IsChecked).Count / active.Count * 100;
            }
        }

        public double WeeklyProgress
        {
            get
            {
                var active = WeeklyTasks.FindAll(t => t.IsActive);
                if (active.Count == 0) return 0;
                return (double)active.FindAll(t => t.IsChecked).Count / active.Count * 100;
            }
        }

        public double BossProgress
        {
            get
            {
                var active = BossTasks.FindAll(t => t.IsActive);
                if (active.Count == 0) return 0;
                return (double)active.FindAll(t => t.IsChecked).Count / active.Count * 100;
            }
        }

        public int BossCheckedCount => BossTasks.FindAll(t => t.IsActive && t.IsChecked).Count;
        public int BossActiveCount => BossTasks.FindAll(t => t.IsActive).Count;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void NotifyProgressChanged()
        {
            OnPropertyChanged(nameof(DailyProgress));
            OnPropertyChanged(nameof(WeeklyProgress));
            OnPropertyChanged(nameof(BossProgress));
            OnPropertyChanged(nameof(BossCheckedCount));
        }
    }

    /// <summary>
    /// 전체 앱 데이터 (여러 캐릭터 저장)
    /// </summary>
    public class AppData
    {
        public List<CharacterProfile> Characters { get; set; } = new();
        public string? SelectedCharacterId { get; set; }
        public string ApiKey { get; set; } = "";
        public bool IsDarkTheme { get; set; } = true;
        public bool AutoStartEnabled { get; set; } = false;
    }
}

