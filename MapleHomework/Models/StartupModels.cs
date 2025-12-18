using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using MapleHomework.Models;

namespace MapleHomework.Models
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
}
