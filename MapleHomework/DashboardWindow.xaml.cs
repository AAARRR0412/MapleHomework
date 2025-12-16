using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MapleHomework.Models;
using MapleHomework.Services;
using MapleHomework.ViewModels;
using MapleHomework.Commands;
using Wpf.Ui.Controls;

namespace MapleHomework
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
        public int PendingCount => Tasks.Count;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public partial class DashboardWindow : FluentWindow, INotifyPropertyChanged
    {
        private MainViewModel _viewModel;
        private AppData _appData;

        public ObservableCollection<CharacterProfile> Characters { get; set; }
        public ObservableCollection<TodoItem> TodayTasks { get; set; } = new();
        public ObservableCollection<CharacterTaskGroup> GroupedTasks { get; set; } = new();

        private bool _showOnlyFavorites = true;
        public bool ShowOnlyFavorites
        {
            get => _showOnlyFavorites;
            set
            {
                _showOnlyFavorites = value;
                OnPropertyChanged();
                RefreshTodayTasks();
            }
        }

        // 반응형 열 개수
        private int _taskColumnCount = 3;
        public int TaskColumnCount
        {
            get => _taskColumnCount;
            set { _taskColumnCount = value; OnPropertyChanged(); }
        }

        // 전체 미완료 숙제 수 (즐겨찾기 상관없이)
        private int _totalUncompletedCount = 0;
        // 즐겨찾기된 숙제 수 (완료/미완료 상관없이)
        private int _totalFavoriteCount = 0;

        public DashboardWindow(AppData appData, MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _appData = appData;
            Characters = new ObservableCollection<CharacterProfile>(appData.Characters);
            this.DataContext = this;

            // 테마 적용
            ApplyThemeResources();
            _viewModel.ThemeChanged += OnThemeChanged;
            _viewModel.DataChanged += OnDataChanged;

            RefreshTodayTasks();

            // 창 크기 변경 시 열 수 업데이트
            this.SizeChanged += DashboardWindow_SizeChanged;
            this.Loaded += (s, e) => UpdateTaskColumnCount();
        }

        private void OnThemeChanged()
        {
            Dispatcher.Invoke(() => ApplyThemeResources());
        }

        private void OnDataChanged()
        {
            Dispatcher.Invoke(() =>
            {
                // 캐릭터 목록 새로고침
                Characters.Clear();
                foreach (var c in _appData.Characters)
                {
                    Characters.Add(c);
                }
                RefreshTodayTasks();
                OnPropertyChanged(nameof(CharacterCount));
            });
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _viewModel.ThemeChanged -= OnThemeChanged;
            _viewModel.DataChanged -= OnDataChanged;
            base.OnClosed(e);
        }

        /// <summary>
        /// 테마 리소스 적용 (다크/라이트 모드)
        /// </summary>
        public void ApplyThemeResources()
        {
            // 배경색은 XAML DynamicResource에서 처리하므로 수동 설정 제거
            // 필요한 경우 여기서 추가적인 코드 레벨 테마 로직 처리
        }

        private void DashboardWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTaskColumnCount();
        }

        private void UpdateTaskColumnCount()
        {
            // 오늘의 할일 영역 너비 기준 (대략 창 너비의 60% - 여백)
            double contentWidth = this.ActualWidth * 0.6 - 60;

            // 각 박스당 최소 180px 필요
            const double minItemWidth = 180;

            if (contentWidth >= minItemWidth * 3)
                TaskColumnCount = 3;
            else if (contentWidth >= minItemWidth * 2)
                TaskColumnCount = 2;
            else
                TaskColumnCount = 1;
        }

        public int CharacterCount => Characters.Count;
        public int TotalPendingCount => TodayTasks.Count;
        public bool HasPendingTasks => TodayTasks.Any();

        /// <summary>
        /// 빈 상태 메시지
        /// </summary>
        public string EmptyMessage
        {
            get
            {
                if (ShowOnlyFavorites)
                {
                    if (_totalFavoriteCount == 0)
                    {
                        return "⭐ 즐겨찾기된 숙제가 없습니다";
                    }
                    else
                    {
                        return "🎉 모든 즐겨찾기 숙제를 완료했습니다!";
                    }
                }
                else
                {
                    if (_totalUncompletedCount == 0)
                    {
                        return "🎉 모든 숙제를 완료했습니다!";
                    }
                    return "";
                }
            }
        }

        public string EmptySubMessage
        {
            get
            {
                if (ShowOnlyFavorites && _totalFavoriteCount == 0)
                {
                    return "메인 화면에서 일일/주간/보스/월간 헤더의\n별표(★)를 눌러 즐겨찾기를 추가해보세요";
                }
                return "";
            }
        }

        /// <summary>
        /// 외부에서 호출 가능한 데이터 갱신 메서드
        /// </summary>
        public void RefreshData()
        {
            // Characters 데이터 갱신
            Characters.Clear();
            foreach (var c in _appData.Characters)
            {
                Characters.Add(c);
            }

            RefreshTodayTasks();
            OnPropertyChanged(nameof(CharacterCount));
            OnPropertyChanged(nameof(TotalPendingCount));
            OnPropertyChanged(nameof(HasPendingTasks));
        }

        /// <summary>
        /// 오늘의 할 일 목록 새로고침
        /// </summary>
        private void RefreshTodayTasks()
        {
            TodayTasks.Clear();
            GroupedTasks.Clear();

            _totalUncompletedCount = 0;
            _totalFavoriteCount = 0;

            foreach (var character in Characters)
            {
                // 카테고리별 즐겨찾기 여부에 따라 태스크 필터링
                var pendingTasksWithCategory = new List<(HomeworkTask task, bool isFavorite)>();

                foreach (var task in character.DailyTasks.Where(t => t.IsActive))
                {
                    if (!task.IsChecked) _totalUncompletedCount++;
                    if (character.IsDailyFavorite) _totalFavoriteCount++;
                    if (!task.IsChecked)
                        pendingTasksWithCategory.Add((task, character.IsDailyFavorite));
                }

                foreach (var task in character.WeeklyTasks.Where(t => t.IsActive))
                {
                    if (!task.IsChecked) _totalUncompletedCount++;
                    if (character.IsWeeklyFavorite) _totalFavoriteCount++;
                    if (!task.IsChecked)
                        pendingTasksWithCategory.Add((task, character.IsWeeklyFavorite));
                }

                foreach (var task in character.BossTasks.Where(t => t.IsActive))
                {
                    if (!task.IsChecked) _totalUncompletedCount++;
                    if (character.IsBossFavorite) _totalFavoriteCount++;
                    if (!task.IsChecked)
                        pendingTasksWithCategory.Add((task, character.IsBossFavorite));
                }

                foreach (var task in character.MonthlyTasks.Where(t => t.IsActive))
                {
                    if (!task.IsChecked) _totalUncompletedCount++;
                    if (character.IsMonthlyFavorite) _totalFavoriteCount++;
                    if (!task.IsChecked)
                        pendingTasksWithCategory.Add((task, character.IsMonthlyFavorite));
                }

                // 필터링 (즐겨찾기만 표시 옵션)
                var filteredTasks = ShowOnlyFavorites
                    ? pendingTasksWithCategory.Where(x => x.isFavorite).Select(x => x.task).ToList()
                    : pendingTasksWithCategory.Select(x => x.task).ToList();

                if (filteredTasks.Any())
                {
                    var group = new CharacterTaskGroup
                    {
                        Nickname = character.Nickname,
                        ImageUrl = character.ImageUrl,
                        Level = character.Level
                    };

                    foreach (var task in filteredTasks)
                    {
                        var todoItem = new TodoItem
                        {
                            CharacterName = character.Nickname,
                            TaskName = task.Name,
                            Category = task.Category
                        };

                        TodayTasks.Add(todoItem);
                        group.Tasks.Add(todoItem);
                    }

                    GroupedTasks.Add(group);
                }
            }

            OnPropertyChanged(nameof(TotalPendingCount));
            OnPropertyChanged(nameof(HasPendingTasks));
            OnPropertyChanged(nameof(EmptyMessage));
            OnPropertyChanged(nameof(EmptySubMessage));
        }

        public ICommand SelectAndCloseCommand => new RelayCommand(param =>
        {
            if (param is CharacterProfile character)
            {
                _viewModel.SelectedCharacter = character;
                this.Close();
            }
        });

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
