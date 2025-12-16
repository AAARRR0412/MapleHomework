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

        // 정렬 순서
        public int Order { get; set; } = 0;

        // 숨김 처리 여부 (삭제는 아니지만 목록에서 안보이게)
        public bool IsHidden { get; set; } = false;

        // 삭제 여부 (실제 삭제는 아니지만 휴지통 개념)
        public bool IsDeleted { get; set; } = false;

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

        public bool CheckReset(DateTime now)
        {
            // 리셋 시간이 지났는지 확인
            // 메이플스토리 리셋: 매일 자정 (00:00)
            // 주간/보스 리셋: 목요일 자정 (00:00)
            // 월간 리셋: 매월 1일 자정 (00:00)

            if (!IsChecked) return false;

            DateTime resetTime = DateTime.MinValue;
            DateTime last = LastCheckedTime;

            // 로직 간소화: "마지막 체크 시간"의 다음 리셋 타임을 구하고, 현재 시간이 그보다 지났으면 리셋
            switch (Category)
            {
                case TaskCategory.Daily:
                    resetTime = last.Date.AddDays(1); // 다음날 0시
                    break;
                case TaskCategory.Weekly:
                case TaskCategory.Boss:
                    // 다음 목요일 0시 찾기
                    int daysUntilThursday = ((int)DayOfWeek.Thursday - (int)last.DayOfWeek + 7) % 7;
                    if (daysUntilThursday == 0) daysUntilThursday = 7;
                    resetTime = last.Date.AddDays(daysUntilThursday);
                    break;
                case TaskCategory.Monthly:
                    resetTime = new DateTime(last.Year, last.Month, 1).AddMonths(1); // 다음달 1일
                    break;
            }

            if (now >= resetTime)
            {
                IsChecked = false;
                return true;
            }
            return false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}