using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MapleHomework.Models
{
    public enum TaskCategory { Daily, Weekly, Boss, Monthly } // Monthly 추가됨

    // 난이도 목록 (순서대로 정렬)
    public enum BossDifficulty
    {
        Easy, Normal, Hard, Chaos, Extreme
    }

    public class HomeworkTask : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public TaskCategory Category { get; set; }

        // 해당 퀘스트를 수행하기 위한 최소 레벨 (0이면 제한 없음)
        public int RequiredLevel { get; set; } = 0;

        // 사용자가 이 숙제를 할지 말지 선택 (설정값)
        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        // 중요/즐겨찾기 숙제 (대시보드 및 오버레이 노출 여부)
        private bool _isFavorite = false;
        public bool IsFavorite
        {
            get => _isFavorite;
            set { _isFavorite = value; OnPropertyChanged(); }
        }

        // 보스가 선택할 수 있는 난이도 목록
        public List<BossDifficulty> AvailableDifficulties { get; set; } = new() { BossDifficulty.Normal };

        // 보스 난이도
        private BossDifficulty _difficulty = BossDifficulty.Normal;
        public BossDifficulty Difficulty
        {
            get => _difficulty;
            set { _difficulty = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        // 보스 파티원 수 (1~6인)
        private int _partySize = 1;
        public int PartySize
        {
            get => _partySize;
            set 
            { 
                _partySize = Math.Max(1, Math.Min(6, value)); 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        // 표시용 이름 (보스인 경우 파티원 수 포함)
        public string DisplayName
        {
            get
            {
                if (Category == TaskCategory.Boss || Category == TaskCategory.Monthly)
                {
                    return $"{Name} ({PartySize}인)";
                }
                return Name;
            }
        }

        // 완료 여부
        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    LastCheckedTime = DateTime.Now;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime LastCheckedTime { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}