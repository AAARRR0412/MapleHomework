using System.Collections.ObjectModel;
using System.ComponentModel;
using MapleHomework.Models;

namespace MapleHomework.Models
{
    /// <summary>
    /// 오늘의 할 일 항목
    /// </summary>
    public class TodoItem
    {
        public string CharacterName { get; set; } = "";
        public string TaskName { get; set; } = "";
        public TaskCategory Category { get; set; }
        public string CategoryText => Category switch
        {
            TaskCategory.Daily => "일일",
            TaskCategory.Weekly => "주간",
            TaskCategory.Boss => "보스",
            TaskCategory.Monthly => "월간",
            _ => ""
        };
        public string CategoryColor => Category switch
        {
            TaskCategory.Daily => "#3B82F6",   // Blue
            TaskCategory.Weekly => "#F97316",  // Orange
            TaskCategory.Boss => "#E11D48",    // Rose (Soft Red)
            TaskCategory.Monthly => "#8B5CF6", // Violet
            _ => "#64748B"
        };
    }

    /// <summary>
    /// 캐릭터별 그룹화된 숙제
    /// </summary>
    public class CharacterTaskGroup : INotifyPropertyChanged
    {
        public string Nickname { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public int Level { get; set; }
        public ObservableCollection<TodoItem> Tasks { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
