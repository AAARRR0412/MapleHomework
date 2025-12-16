using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using MapleHomework.Services;

namespace MapleHomework.Models
{
    /// <summary>
    /// 캐릭터 그룹 정보
    /// </summary>
    public class CharacterGroup
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Color { get; set; } = "#5AC8FA"; // 기본 색상
        public int Order { get; set; } = 0;
    }

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

        // 그룹화 관련
        private string? _groupId;
        public string? GroupId
        {
            get => _groupId;
            set { _groupId = value; OnPropertyChanged(); }
        }

        private int _sortOrder = 0;
        public int SortOrder
        {
            get => _sortOrder;
            set { _sortOrder = value; OnPropertyChanged(); }
        }

        // 유니온 정보
        public int UnionLevel { get; set; }
        public string? UnionGrade { get; set; }

        private long _combatPower = 0;
        public long CombatPower
        {
            get => _combatPower;
            set { _combatPower = value; OnPropertyChanged(); }
        }

        // API 관련
        public string? Ocid { get; set; }
        public DateTime LastUpdatedTime { get; set; } = DateTime.MinValue;

        // 태스크 데이터
        public List<HomeworkTask> DailyTasks { get; set; } = new();
        public List<HomeworkTask> WeeklyTasks { get; set; } = new();
        public List<HomeworkTask> BossTasks { get; set; } = new();
        public List<HomeworkTask> MonthlyTasks { get; set; } = new();

        // 카테고리별 즐겨찾기 설정
        private bool _isDailyFavorite = false;
        public bool IsDailyFavorite
        {
            get => _isDailyFavorite;
            set { _isDailyFavorite = value; OnPropertyChanged(); }
        }

        private bool _isWeeklyFavorite = false;
        public bool IsWeeklyFavorite
        {
            get => _isWeeklyFavorite;
            set { _isWeeklyFavorite = value; OnPropertyChanged(); }
        }

        private bool _isBossFavorite = false;
        public bool IsBossFavorite
        {
            get => _isBossFavorite;
            set { _isBossFavorite = value; OnPropertyChanged(); }
        }

        private bool _isMonthlyFavorite = false;
        public bool IsMonthlyFavorite
        {
            get => _isMonthlyFavorite;
            set { _isMonthlyFavorite = value; OnPropertyChanged(); }
        }

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

        // 주간 보스 수익 계산
        public long WeeklyBossReward
        {
            get
            {
                long total = 0;
                foreach (var boss in BossTasks.Where(b => b.IsActive && b.IsChecked))
                {
                    total += BossRewardData.GetReward(boss.Name, boss.Difficulty, boss.PartySize, false);
                }
                return total;
            }
        }

        // 주간 보스 예상 수익 (전체)
        public long WeeklyBossExpectedReward
        {
            get
            {
                long total = 0;
                foreach (var boss in BossTasks.Where(b => b.IsActive))
                {
                    total += BossRewardData.GetReward(boss.Name, boss.Difficulty, boss.PartySize, false);
                }
                return total;
            }
        }

        // 포맷된 수익 문자열
        public string WeeklyBossRewardText => BossRewardData.FormatMeso(WeeklyBossReward);
        public string WeeklyBossExpectedRewardText => BossRewardData.FormatMeso(WeeklyBossExpectedReward);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public IEnumerable<HomeworkTask> HomeworkList => DailyTasks.Concat(WeeklyTasks).Concat(BossTasks).Concat(MonthlyTasks);

        public void NotifyProgressChanged()
        {
            OnPropertyChanged(nameof(DailyProgress));
            OnPropertyChanged(nameof(WeeklyProgress));
            OnPropertyChanged(nameof(BossProgress));
            OnPropertyChanged(nameof(BossCheckedCount));
            OnPropertyChanged(nameof(WeeklyBossReward));
            OnPropertyChanged(nameof(WeeklyBossExpectedReward));
            OnPropertyChanged(nameof(WeeklyBossRewardText));
            OnPropertyChanged(nameof(WeeklyBossExpectedRewardText));
            OnPropertyChanged(nameof(UnionLevel));
            OnPropertyChanged(nameof(UnionGrade));
        }
    }

    /// <summary>
    /// 전체 앱 데이터 (여러 캐릭터 저장)
    /// </summary>
    public class AppData
    {
        public List<CharacterProfile> Characters { get; set; } = new();
        public List<CharacterGroup> Groups { get; set; } = new();
        public string? SelectedCharacterId { get; set; }
        public string ApiKey { get; set; } = "";
        public bool IsDarkTheme { get; set; } = false;
        public bool AutoStartEnabled { get; set; } = false;
    }
}
