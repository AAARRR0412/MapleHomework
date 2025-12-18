using MapleHomework.Commands;
using MapleHomework.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace MapleHomework.ViewModels
{
    public class TaskSectionViewModel : INotifyPropertyChanged
    {
        public string Title { get; set; } = "";
        public Brush HeaderBackground { get; set; } = Brushes.Gray;
        public TaskCategory Category { get; set; }
        public ICollectionView ItemsView { get; set; } = null!;
        public ICommand ToggleFavoriteCommand { get; set; } = null!;

        private bool _isFavorite;
        public bool IsFavorite { get => _isFavorite; set { _isFavorite = value; OnPropertyChanged(); } }

        private string _progressText = "";
        public string ProgressText { get => _progressText; set { _progressText = value; OnPropertyChanged(); } }

        private double _progressValue;
        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressOffset));
            }
        }

        public double ProgressOffset => 150.8 * (1 - (ProgressValue / 100.0));

        private bool _isAllCompleted;
        public bool IsAllCompleted { get => _isAllCompleted; set { _isAllCompleted = value; OnPropertyChanged(); } }

        // 편집 모드 여부 (섹션 가시성 제어용) -> 섹션 자체가 아니라 외부에서 ItemsControl 바인딩으로 처리하지만,
        // 필요하다면 추가. 여기서는 없어도 됨.

        // 보스 섹션 전용
        private string _secondaryText = "";
        public string SecondaryText { get => _secondaryText; set { _secondaryText = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowSecondaryText)); } }
        public bool ShowSecondaryText => !string.IsNullOrEmpty(SecondaryText);

        // 카테고리 섹션 접기/펼치기
        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        // 카테고리 순서 (드래그앤드롭)
        private int _order;
        public int Order
        {
            get => _order;
            set { _order = value; OnPropertyChanged(); }
        }

        // 접기/펼치기 토글 커맨드
        private ICommand? _toggleExpandCommand;

        // 편집 모드 체크용 Func (MainViewModel에서 설정)
        public Func<bool>? IsEditModeFunc { get; set; }

        public ICommand ToggleExpandCommand => _toggleExpandCommand ??= new RelayCommand(_ =>
        {
            // 편집 모드일 때는 접기/펼치기 비활성화
            if (IsEditModeFunc?.Invoke() == true) return;
            IsExpanded = !IsExpanded;
        });

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
