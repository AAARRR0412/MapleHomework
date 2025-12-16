using MapleHomework.Commands;
using MapleHomework.Models;
using MapleHomework.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MapleHomework.Data;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using System.Windows.Data;
using System.Windows;

namespace MapleHomework.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IMapleApiService _apiService;
        private readonly IWindowService _windowService;
        private DispatcherTimer _timer;
        private AppData _appData;

        /// <summary>
        /// AppData 인스턴스 (외부 접근용)
        /// </summary>
        public AppData AppData => _appData;

        // 테마 변경 이벤트 (다른 윈도우들에게 알림)
        public event Action? ThemeChanged;

        // 데이터 변경 이벤트 (캐릭터 추가/삭제, 즐겨찾기 변경 등)
        public event Action? DataChanged;

        /// <summary>
        /// 테마 변경 이벤트 발생
        /// </summary>
        public void NotifyThemeChanged()
        {
            ThemeChanged?.Invoke();
        }

        /// <summary>
        /// 데이터 변경 이벤트 발생
        /// </summary>
        public void NotifyDataChanged()
        {
            RefreshAllViews();
            DataChanged?.Invoke();
        }

        // 캐릭터 리스트
        public ObservableCollection<CharacterProfile> Characters { get; set; } = new();

        private CharacterProfile? _selectedCharacter;
        public CharacterProfile? SelectedCharacter
        {
            get => _selectedCharacter;
            set
            {
                if (_selectedCharacter != value)
                {
                    _selectedCharacter = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasSelectedCharacter));
                    OnPropertyChanged(nameof(CanAddCharacter));
                    if (value != null)
                    {
                        _appData.SelectedCharacterId = value.Id;
                        CharacterRepository.Save(_appData);
                        LoadCharacterTasks(value);
                        _ = LoadCharacterDataFromApi(value.Nickname);
                    }
                }
            }
        }

        public bool HasSelectedCharacter => SelectedCharacter != null;
        public bool CanAddCharacter => Characters.Count < 10;

        // 캐릭터 선택 패널 표시 여부
        private bool _isCharacterSelectorOpen = false;
        public bool IsCharacterSelectorOpen
        {
            get => _isCharacterSelectorOpen;
            set { _isCharacterSelectorOpen = value; OnPropertyChanged(); }
        }

        // 사이드바 표시 여부
        private bool _isSidebarOpen = false;
        public bool IsSidebarOpen
        {
            get => _isSidebarOpen;
            set { _isSidebarOpen = value; OnPropertyChanged(); }
        }

        public ICommand ToggleSidebarCommand => new RelayCommand(_ => IsSidebarOpen = !IsSidebarOpen);

        // 캐릭터 추가 UI 상태
        private bool _isAddingCharacter = false;
        public bool IsAddingCharacter
        {
            get => _isAddingCharacter;
            set { _isAddingCharacter = value; OnPropertyChanged(); }
        }

        private string _newCharacterName = "";
        public string NewCharacterName
        {
            get => _newCharacterName;
            set { _newCharacterName = value; OnPropertyChanged(); }
        }

        // 원본 데이터
        public ObservableCollection<HomeworkTask> DailyList { get; set; } = new();
        public ObservableCollection<HomeworkTask> WeeklyList { get; set; } = new();
        public ObservableCollection<HomeworkTask> BossList { get; set; } = new();
        public ObservableCollection<HomeworkTask> MonthlyList { get; set; } = new();

        // 필터링된 뷰
        public ICollectionView DailyView { get; private set; } = null!;
        public ICollectionView WeeklyView { get; private set; } = null!;
        public ICollectionView BossView { get; private set; } = null!;
        public ICollectionView MonthlyView { get; private set; } = null!;

        // 완료된 항목 뷰
        public ObservableCollection<HomeworkTask> CompletedList { get; set; } = new();

        public bool HasCompletedItems => CompletedList.Count > 0;

        // 편집 모드
        private bool _isEditMode = false;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                OnPropertyChanged();

                // 편집 모드 전환 시 섹션 뷰 필터를 즉시 갱신 (필터가 IsEditMode를 참조함)
                try
                {
                    ActiveSectionsView?.Refresh();
                    HiddenSectionsView?.Refresh();
                }
                catch { }

                RefreshAllViews();
            }
        }

        // 반응형 열 개수 (창 크기에 따라 변경)
        private int _taskColumnCount = 3;
        public int TaskColumnCount
        {
            get => _taskColumnCount;
            set { _taskColumnCount = value; OnPropertyChanged(); }
        }

        // 진행률 & 카운트
        private double _dailyProgressTarget;
        private double _weeklyProgressTarget;
        private double _bossProgressTarget;
        private string _bossCountText = "0/12";

        private double _dailyProgress;
        private double _weeklyProgress;
        private double _bossProgress;

        public double DailyProgress { get => _dailyProgress; set { _dailyProgress = value; OnPropertyChanged(); OnPropertyChanged(nameof(DailyProgressOffset)); } }
        public double WeeklyProgress { get => _weeklyProgress; set { _weeklyProgress = value; OnPropertyChanged(); OnPropertyChanged(nameof(WeeklyProgressOffset)); } }
        public double BossProgress { get => _bossProgress; set { _bossProgress = value; OnPropertyChanged(); OnPropertyChanged(nameof(BossProgressOffset)); } }
        public string BossCountText { get => _bossCountText; set { _bossCountText = value; OnPropertyChanged(); } }

        private const double CircleCircumference = Math.PI * 48;

        public double DailyProgressOffset => 0;
        public double WeeklyProgressOffset => 0;
        public double BossProgressOffset => 0;

        private DispatcherTimer? _animationTimer;
        private const double AnimationSpeed = 1.4;

        private void StartProgressAnimation()
        {
            if (_animationTimer == null)
            {
                _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
                _animationTimer.Tick += AnimateProgress;
                _animationTimer.Start();
            }
        }

        private void AnimateProgress(object? sender, EventArgs e)
        {
            bool needsUpdate = false;
            double step = AnimationSpeed;

            if (Math.Abs(_dailyProgress - _dailyProgressTarget) > 0.1)
            {
                _dailyProgress += (_dailyProgressTarget > _dailyProgress) ? step : -step;
                if (Math.Abs(_dailyProgress - _dailyProgressTarget) <= step) _dailyProgress = _dailyProgressTarget;
                OnPropertyChanged(nameof(DailyProgress));
                needsUpdate = true;
            }

            if (Math.Abs(_weeklyProgress - _weeklyProgressTarget) > 0.1)
            {
                _weeklyProgress += (_weeklyProgressTarget > _weeklyProgress) ? step : -step;
                if (Math.Abs(_weeklyProgress - _weeklyProgressTarget) <= step) _weeklyProgress = _weeklyProgressTarget;
                OnPropertyChanged(nameof(WeeklyProgress));
                needsUpdate = true;
            }

            if (Math.Abs(_bossProgress - _bossProgressTarget) > 0.1)
            {
                _bossProgress += (_bossProgressTarget > _bossProgress) ? step : -step;
                if (Math.Abs(_bossProgress - _bossProgressTarget) <= step) _bossProgress = _bossProgressTarget;
                OnPropertyChanged(nameof(BossProgress));
                needsUpdate = true;
            }

            if (!needsUpdate && _animationTimer != null)
            {
                _animationTimer.Stop();
                _animationTimer = null;
            }
        }

        private void SetProgressWithAnimation(double dailyTarget, double weeklyTarget, double bossTarget)
        {
            _dailyProgressTarget = dailyTarget;
            _weeklyProgressTarget = weeklyTarget;
            _bossProgressTarget = bossTarget;
            StartProgressAnimation();
        }

        public record PartyOption(int Count, string Display);



        public ObservableCollection<TaskSectionViewModel> Sections { get; set; } = new();
        public ICollectionView ActiveSectionsView { get; private set; } = null!;
        public ICollectionView HiddenSectionsView { get; private set; } = null!; // 완료되어 아래로 이동한 섹션들

        // 파티원 수 옵션 (솔플 ~ 6인)
        public IEnumerable<PartyOption> PartySizeOptions => new[]
        {
            new PartyOption(1, "솔플"),
            new PartyOption(2, "2인"),
            new PartyOption(3, "3인"),
            new PartyOption(4, "4인"),
            new PartyOption(5, "5인"),
            new PartyOption(6, "6인")
        };

        // 캐릭터 정보 (UI 바인딩용)
        private string _characterName = "캐릭터 추가";
        private string _characterImage = string.Empty;
        private string _worldName = "-";
        private string _characterClass = "-";
        private string _characterLevel = "-";
        public string CharacterName { get => _characterName; set { _characterName = value; OnPropertyChanged(); } }
        public string CharacterImage { get => _characterImage; set { _characterImage = value; OnPropertyChanged(); } }
        public string WorldName { get => _worldName; set { _worldName = value; OnPropertyChanged(); } }
        public string CharacterClass { get => _characterClass; set { _characterClass = value; OnPropertyChanged(); } }
        public string CharacterLevel { get => _characterLevel; set { _characterLevel = value; OnPropertyChanged(); } }

        // 카테고리별 즐겨찾기 상태 (UI 바인딩용)
        public bool IsDailyFavorite => SelectedCharacter?.IsDailyFavorite ?? false;
        public bool IsWeeklyFavorite => SelectedCharacter?.IsWeeklyFavorite ?? false;
        public bool IsBossFavorite => SelectedCharacter?.IsBossFavorite ?? false;
        public bool IsMonthlyFavorite => SelectedCharacter?.IsMonthlyFavorite ?? false;

        // 테마
        public bool IsDarkTheme { get; set; } = false;
        public string ThemeIcon { get; set; } = "WeatherSunny24";

        private Brush _background = Brushes.Transparent;
        private Brush _surface = Brushes.Transparent;
        private Brush _surfaceContainer = Brushes.Transparent;
        private Brush _onSurface = Brushes.Transparent;
        private Brush _onSurfaceVariant = Brushes.Transparent;
        private Brush _primary = Brushes.Transparent;
        private Brush _outline = Brushes.Transparent;
        private Brush _completedSurface = Brushes.Transparent;

        public Brush Background { get => _background; set { _background = value; OnPropertyChanged(); } }
        public Brush Surface { get => _surface; set { _surface = value; OnPropertyChanged(); } }
        public Brush SurfaceContainer { get => _surfaceContainer; set { _surfaceContainer = value; OnPropertyChanged(); } }
        public Brush OnSurface { get => _onSurface; set { _onSurface = value; OnPropertyChanged(); } }
        public Brush OnSurfaceVariant { get => _onSurfaceVariant; set { _onSurfaceVariant = value; OnPropertyChanged(); } }
        public Brush Primary { get => _primary; set { _primary = value; OnPropertyChanged(); } }
        public Brush Outline { get => _outline; set { _outline = value; OnPropertyChanged(); } }
        public Brush CompletedSurface { get => _completedSurface; set { _completedSurface = value; OnPropertyChanged(); } }

        // Commands
        public ICommand ToggleThemeCommand { get; }
        public ICommand ToggleTaskCommand { get; }
        public ICommand ToggleEditModeCommand { get; }
        public ICommand SelectCharacterCommand { get; }
        public ICommand AddCharacterCommand { get; } // Deprecated
        public ICommand StartAddCharacterCommand { get; }
        public ICommand ConfirmAddCharacterCommand { get; }
        public ICommand CancelAddCharacterCommand { get; }
        public ICommand RemoveCharacterCommand { get; }
        public ICommand ToggleCharacterSelectorCommand { get; }
        public ICommand OpenDashboardCommand { get; }
        public ICommand OpenBossRewardCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand SaveEditCommand { get; }
        public ICommand ToggleFavoriteCommand { get; }
        public ICommand ToggleDailyFavoriteCommand { get; }
        public ICommand ToggleWeeklyFavoriteCommand { get; }
        public ICommand ToggleBossFavoriteCommand { get; }
        public ICommand ToggleMonthlyFavoriteCommand { get; }
        public ICommand OpenReportCommand { get; }

        public MainViewModel(AppData appData, IMapleApiService apiService, IWindowService windowService)
        {
            _appData = appData;
            _apiService = apiService;
            _windowService = windowService;

            RemoveDuplicateCharacters();

            IsDarkTheme = _appData.IsDarkTheme;
            UpdateThemeColors();

            // 캐릭터 로드
            foreach (var character in _appData.Characters)
            {
                Characters.Add(character);
            }

            // Commands 초기화
            ToggleThemeCommand = new RelayCommand(_ =>
            {
                // 하단바 토글 → 설정/메모리/AppData 동기화 후 전체 창에 알림
                ApplyThemeAndPersist(!IsDarkTheme);
            });

            // 편집 취소 (변경사항 버리고 편집모드 종료)
            CancelEditCommand = new RelayCommand(_ =>
            {
                // 저장하지 않고 기존 데이터 다시 로드
                if (SelectedCharacter != null)
                {
                    LoadCharacterTasks(SelectedCharacter);
                }
                IsEditMode = false;
            });

            // 편집 저장 (변경사항 저장하고 편집모드 종료)
            SaveEditCommand = new RelayCommand(_ =>
            {
                SaveData();
                IsEditMode = false;
                RefreshAllViews();
            });

            ToggleTaskCommand = new RelayCommand(async param =>
            {
                if (!IsEditMode && param is HomeworkTask task)
                {
                    task.IsChecked = !task.IsChecked;

                    if (task.IsChecked)
                    {
                        task.LastCheckedTime = DateTime.Now;
                        await Task.Delay(550);
                    }

                    RefreshAllViews();
                }
            });

            ToggleEditModeCommand = new RelayCommand(_ => IsEditMode = !IsEditMode);

            SelectCharacterCommand = new RelayCommand(param =>
            {
                if (param is CharacterProfile character)
                {
                    SelectedCharacter = character;
                    IsCharacterSelectorOpen = false;
                    IsAddingCharacter = false;
                    NewCharacterName = "";
                }
            });

            AddCharacterCommand = new RelayCommand(_ => { }); // Deprecated

            StartAddCharacterCommand = new RelayCommand(_ =>
            {
                if (CanAddCharacter)
                {
                    NewCharacterName = "";
                    IsAddingCharacter = true;
                }
            });

            ConfirmAddCharacterCommand = new RelayCommand(_ =>
            {
                if (!string.IsNullOrWhiteSpace(NewCharacterName))
                {
                    var newChar = CharacterRepository.AddCharacter(_appData, NewCharacterName);
                    if (newChar != null)
                    {
                        Characters.Add(newChar);
                        SelectedCharacter = newChar;
                        CharacterRepository.Save(_appData);
                        OnPropertyChanged(nameof(CanAddCharacter));
                        NotifyDataChanged(); // 다른 창에 알림
                    }
                    IsAddingCharacter = false;
                    NewCharacterName = "";
                }
            });

            CancelAddCharacterCommand = new RelayCommand(_ =>
            {
                IsAddingCharacter = false;
                NewCharacterName = "";
            });

            RemoveCharacterCommand = new RelayCommand(param =>
            {
                if (param is CharacterProfile character)
                {
                    CharacterRepository.RemoveCharacter(_appData, character.Id);
                    Characters.Remove(character);
                    if (Characters.Count == 0 || SelectedCharacter == character)
                    {
                        SelectedCharacter = Characters.FirstOrDefault();
                    }
                    CharacterRepository.Save(_appData);
                    OnPropertyChanged(nameof(CanAddCharacter));
                    NotifyDataChanged();
                }
            });

            ToggleCharacterSelectorCommand = new RelayCommand(_ => IsCharacterSelectorOpen = !IsCharacterSelectorOpen);

            OpenDashboardCommand = new RelayCommand(_ =>
            {
                _windowService.OpenDashboard(_appData, this);
            });

            OpenBossRewardCommand = new RelayCommand(_ =>
            {
                _windowService.OpenBossReward(_appData, this);
            });

            ToggleFavoriteCommand = new RelayCommand(param =>
            {
                if (param is HomeworkTask task)
                {
                    task.IsFavorite = !task.IsFavorite;
                    SaveData();
                }
            });

            // 카테고리별 즐겨찾기 토글 커맨드
            ToggleDailyFavoriteCommand = new RelayCommand(_ =>
            {
                if (SelectedCharacter != null)
                {
                    SelectedCharacter.IsDailyFavorite = !SelectedCharacter.IsDailyFavorite;
                    SaveData();
                    OnPropertyChanged(nameof(IsDailyFavorite));
                    NotifyDataChanged();
                }
            });

            ToggleWeeklyFavoriteCommand = new RelayCommand(_ =>
            {
                if (SelectedCharacter != null)
                {
                    SelectedCharacter.IsWeeklyFavorite = !SelectedCharacter.IsWeeklyFavorite;
                    SaveData();
                    OnPropertyChanged(nameof(IsWeeklyFavorite));
                    NotifyDataChanged();
                }
            });

            ToggleBossFavoriteCommand = new RelayCommand(_ =>
            {
                if (SelectedCharacter != null)
                {
                    SelectedCharacter.IsBossFavorite = !SelectedCharacter.IsBossFavorite;
                    SaveData();
                    OnPropertyChanged(nameof(IsBossFavorite));
                    NotifyDataChanged();
                }
            });

            ToggleMonthlyFavoriteCommand = new RelayCommand(_ =>
            {
                if (SelectedCharacter != null)
                {
                    SelectedCharacter.IsMonthlyFavorite = !SelectedCharacter.IsMonthlyFavorite;
                    SaveData();
                    OnPropertyChanged(nameof(IsMonthlyFavorite));
                    NotifyDataChanged();
                }
            });

            // 리포트 창 열기
            OpenReportCommand = new RelayCommand(_ =>
            {
                _windowService.OpenReport(this);
            });

            UpdateThemeColors();
            InitializeViews();

            // 선택된 캐릭터 로드
            if (_appData.SelectedCharacterId != null)
            {
                SelectedCharacter = Characters.FirstOrDefault(c => c.Id == _appData.SelectedCharacterId);
            }
            if (SelectedCharacter == null && Characters.Any())
            {
                SelectedCharacter = Characters.First();
            }

            if (SelectedCharacter != null)
            {
                LoadCharacterTasks(SelectedCharacter);
            }

            CheckAndResetTasks();
            UpdateProgress();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _timer.Tick += (s, e) => CheckAndResetTasks();
            _timer.Start();
        }

        private void RemoveDuplicateCharacters()
        {
            var uniqueCharacters = new List<CharacterProfile>();
            var seenNicknames = new HashSet<string>();

            foreach (var charData in _appData.Characters)
            {
                if (string.IsNullOrEmpty(charData.Nickname) || charData.Nickname == "새 캐릭터")
                {
                    uniqueCharacters.Add(charData);
                    continue;
                }

                if (!seenNicknames.Contains(charData.Nickname))
                {
                    uniqueCharacters.Add(charData);
                    seenNicknames.Add(charData.Nickname);
                }
            }
            _appData.Characters = uniqueCharacters;
        }

        private void InitializeViews()
        {
            DailyView = System.Windows.Data.CollectionViewSource.GetDefaultView(DailyList);
            DailyView.Filter = item => FilterTask(item, TaskCategory.Daily);
            DailyView.SortDescriptions.Add(new SortDescription(nameof(HomeworkTask.Order), ListSortDirection.Ascending));

            WeeklyView = System.Windows.Data.CollectionViewSource.GetDefaultView(WeeklyList);
            WeeklyView.Filter = item => FilterTask(item, TaskCategory.Weekly);
            WeeklyView.SortDescriptions.Add(new SortDescription(nameof(HomeworkTask.Order), ListSortDirection.Ascending));

            BossView = System.Windows.Data.CollectionViewSource.GetDefaultView(BossList);
            BossView.Filter = item => FilterTask(item, TaskCategory.Boss);
            BossView.SortDescriptions.Add(new SortDescription(nameof(HomeworkTask.Order), ListSortDirection.Ascending));

            MonthlyView = System.Windows.Data.CollectionViewSource.GetDefaultView(MonthlyList);
            MonthlyView.Filter = item => FilterTask(item, TaskCategory.Monthly);
            MonthlyView.SortDescriptions.Add(new SortDescription(nameof(HomeworkTask.Order), ListSortDirection.Ascending));

            // Sections 초기화
            Sections.Clear();
            Sections.Add(new TaskSectionViewModel
            {
                Title = "일일 컨텐츠",
                Category = TaskCategory.Daily,
                ItemsView = DailyView,
                ToggleFavoriteCommand = ToggleDailyFavoriteCommand,
                HeaderBackground = System.Windows.Application.Current.TryFindResource("MapleCyanGradient") as Brush ?? Brushes.Cyan
            });
            Sections.Add(new TaskSectionViewModel
            {
                Title = "주간 컨텐츠",
                Category = TaskCategory.Weekly,
                ItemsView = WeeklyView,
                ToggleFavoriteCommand = ToggleWeeklyFavoriteCommand,
                HeaderBackground = System.Windows.Application.Current.TryFindResource("MapleOrangeGradient") as Brush ?? Brushes.Orange
            });
            Sections.Add(new TaskSectionViewModel
            {
                Title = "주간 보스",
                Category = TaskCategory.Boss,
                ItemsView = BossView,
                ToggleFavoriteCommand = ToggleBossFavoriteCommand,
                HeaderBackground = System.Windows.Application.Current.TryFindResource("MapleRedGradient") as Brush ?? Brushes.Red
            });
            Sections.Add(new TaskSectionViewModel
            {
                Title = "월간 컨텐츠",
                Category = TaskCategory.Monthly,
                ItemsView = MonthlyView,
                ToggleFavoriteCommand = ToggleMonthlyFavoriteCommand,
                HeaderBackground = System.Windows.Application.Current.TryFindResource("MaplePurpleGradient") as Brush ?? Brushes.Purple
            });

            var activeCvs = new CollectionViewSource { Source = Sections };
            ActiveSectionsView = activeCvs.View;
            // 편집 모드에서는 모든 섹션 표시, 일반 모드에서는 완료되지 않은 섹션만 표시
            ActiveSectionsView.Filter = o => IsEditMode || !((TaskSectionViewModel)o).IsAllCompleted;

            var hiddenCvs = new CollectionViewSource { Source = Sections };
            HiddenSectionsView = hiddenCvs.View;
            // 편집 모드에서는 숨김 섹션 없음, 일반 모드에서는 완료된 섹션만 표시
            HiddenSectionsView.Filter = o => !IsEditMode && ((TaskSectionViewModel)o).IsAllCompleted;
        }



        private bool FilterTask(object item, TaskCategory category)
        {
            if (item is HomeworkTask task)
            {
                if (!IsEditMode && task.IsHidden) return false;
                if (task.IsDeleted) return false;
                if (!IsEditMode && task.IsChecked) return false;
                if (!IsEditMode && !task.IsActive) return false; // Inactive tasks 숨김

                return task.Category == category;
            }
            return false;
        }

        // 데이터 갱신 (다른 창에서 호출 가능)
        public void RefreshAllViews()
        {
            DailyView?.Refresh();
            WeeklyView?.Refresh();
            BossView?.Refresh();
            MonthlyView?.Refresh();

            UpdateProgress();
        }

        public void RefreshExternalWindows()
        {
            // 이제 WindowService가 윈도우 인스턴스를 관리하므로 
            // MainViewModel에서 직접 갱신할 필요가 없거나,
            // WindowService에 RefreshAll() 같은 메서드를 추가하여 호출해야 함.
            // 현재 구조에서는 MainViewModel이 데이터 소스(_appData)를 공유하므로
            // 다른 윈도우들이 이벤트(DataChanged, ThemeChanged)를 구독하여 스스로 갱신하도록 하는 것이 좋음.
        }

        private void LoadCharacterTasks(CharacterProfile character)
        {
            DailyList.Clear();
            WeeklyList.Clear();
            BossList.Clear();
            MonthlyList.Clear();
            CompletedList.Clear();

            // 기본 태스크가 없으면 생성
            TaskRepository.EnsureDefaultTasks(character);

            foreach (var task in character.HomeworkList)
            {
                task.PropertyChanged -= OnTaskChanged; // 중복 구독 방지
                task.PropertyChanged += OnTaskChanged;

                // GameData 순서대로 정렬하기 위해 Order 값 갱신
                int orderIndex = -1;
                switch (task.Category)
                {
                    case TaskCategory.Daily:
                        orderIndex = GameData.Dailies.FindIndex(d => d.Name == task.Name);
                        break;
                    case TaskCategory.Weekly:
                        orderIndex = GameData.Weeklies.FindIndex(w => w.Name == task.Name);
                        break;
                    case TaskCategory.Boss:
                        orderIndex = GameData.WeeklyBosses.FindIndex(b => b.Name == task.Name);
                        break;
                    case TaskCategory.Monthly:
                        orderIndex = GameData.MonthlyBosses.FindIndex(m => m.Name == task.Name);
                        break;
                }
                if (orderIndex != -1) task.Order = orderIndex;

                switch (task.Category)
                {
                    case TaskCategory.Daily: DailyList.Add(task); break;
                    case TaskCategory.Weekly: WeeklyList.Add(task); break;
                    case TaskCategory.Boss: BossList.Add(task); break;
                    case TaskCategory.Monthly: MonthlyList.Add(task); break;
                }

                if (task.IsChecked && !IsEditMode && !task.IsDeleted)
                {
                    CompletedList.Add(task);
                }
            }

            CharacterName = character.Nickname;
            CharacterImage = character.ImageUrl ?? "";
            CharacterLevel = character.Level > 0 ? $"{character.Level}" : "-";
            CharacterClass = !string.IsNullOrEmpty(character.CharacterClass) ? character.CharacterClass : "-";
            WorldName = !string.IsNullOrEmpty(character.WorldName) ? character.WorldName : "-";

            // API 레벨 정보만 별도 저장해둔 필드가 없어서 UI 표시용으로만 사용
            int.TryParse(CharacterLevel, out _characterLevelInt);

            OnPropertyChanged(nameof(IsDailyFavorite));
            OnPropertyChanged(nameof(IsWeeklyFavorite));
            OnPropertyChanged(nameof(IsBossFavorite));
            OnPropertyChanged(nameof(IsMonthlyFavorite));
            OnPropertyChanged(nameof(HasCompletedItems));

            RefreshAllViews();
        }

        private void OnTaskChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HomeworkTask.IsChecked))
            {
                if (sender is HomeworkTask task)
                {
                    if (task.IsChecked)
                    {
                        if (!CompletedList.Contains(task)) CompletedList.Add(task);
                    }
                    else
                    {
                        if (CompletedList.Contains(task)) CompletedList.Remove(task);
                    }
                    CharacterRepository.Save(_appData);
                    OnPropertyChanged(nameof(HasCompletedItems));

                    // UI 스레드에서 지연 호출하여 컬렉션 열거 중 수정 문제 방지
                    System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new Action(() =>
                        {
                            try
                            {
                                RefreshAllViews();
                                NotifyDataChanged();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"RefreshAllViews error: {ex.Message}");
                            }
                        }));
                }
            }
            else if (e.PropertyName == nameof(HomeworkTask.IsActive))
            {
                if (sender is HomeworkTask task && task.Category == TaskCategory.Boss)
                {
                    if (task.IsActive)
                    {
                        // 활성화 된 경우 개수 체크
                        var activeBossCount = BossList.Count(t => t.IsActive);
                        if (activeBossCount > 12)
                        {
                            // 12개 초과 시 복구
                            task.IsActive = false; // 다시 끔 (이때 재귀 호출되지만 아래 조건에서 걸러짐)
                            System.Windows.MessageBox.Show("주간 보스는 최대 12개까지만 설정할 수 있습니다.\n(메이플스토리 결정석 판매 제한)",
                                "설정 제한", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                            return;
                        }
                    }
                    // 변경 사항은 편집 모드 종료 시 저장됨 (SaveEditCommand)
                    // 빠른 카운트 업데이트만 수행
                    var bossTotal = BossList.Count(t => !t.IsDeleted && !t.IsHidden && t.IsActive);
                    var bossDone = BossList.Count(t => !t.IsDeleted && !t.IsHidden && t.IsActive && t.IsChecked);
                    BossCountText = $"{bossDone}/{bossTotal}";
                }
            }
        }

        private void SaveData()
        {
            CharacterRepository.Save(_appData);
            UpdateProgress();
            NotifyDataChanged();
        }

        private void UpdateProgress()
        {
            // 각 카테고리별 전체 항목 수 (숨김/삭제 제외)
            var dailyTotal = DailyList.Count(t => !t.IsDeleted && !t.IsHidden && t.IsActive); // IsActive 체크 추가
            var weeklyTotal = WeeklyList.Count(t => !t.IsDeleted && !t.IsHidden && t.IsActive);
            var bossTotal = BossList.Count(t => !t.IsDeleted && !t.IsHidden && t.IsActive);
            // var monthlyTotal = MonthlyList.Count(t => !t.IsDeleted && !t.IsHidden && t.IsActive);

            // 각 카테고리별 완료 항목 수
            var dailyDone = DailyList.Count(t => !t.IsDeleted && !t.IsHidden && t.IsActive && t.IsChecked);
            var weeklyDone = WeeklyList.Count(t => !t.IsDeleted && !t.IsHidden && t.IsActive && t.IsChecked);
            var bossDone = BossList.Count(t => !t.IsDeleted && !t.IsHidden && t.IsActive && t.IsChecked);
            // var monthlyDone = MonthlyList.Count(t => !t.IsDeleted && !t.IsHidden && t.IsActive && t.IsChecked);

            // 진행률 계산 (0~100)
            double dProg = dailyTotal > 0 ? (double)dailyDone / dailyTotal * 100 : 0;
            double wProg = weeklyTotal > 0 ? (double)weeklyDone / weeklyTotal * 100 : 0;
            double bProg = bossTotal > 0 ? (double)bossDone / bossTotal * 100 : 0;

            SetProgressWithAnimation(dProg, wProg, bProg);

            BossCountText = $"{bossDone}/{bossTotal}";

            // Sections 정보 갱신 및 재정렬
            if (Sections.Count >= 4)
            {
                Sections[0].ProgressText = $"{dProg:0}%";
                Sections[0].ProgressValue = dProg;
                Sections[0].IsFavorite = IsDailyFavorite;
                Sections[0].IsAllCompleted = dailyTotal > 0 && dailyDone >= dailyTotal;

                Sections[1].ProgressText = $"{wProg:0}%";
                Sections[1].ProgressValue = wProg;
                Sections[1].IsFavorite = IsWeeklyFavorite;
                Sections[1].IsAllCompleted = weeklyTotal > 0 && weeklyDone >= weeklyTotal;

                Sections[2].SecondaryText = BossCountText; // 보스 카운트
                Sections[2].IsFavorite = IsBossFavorite;
                Sections[2].ProgressText = $"{bProg:0}%";
                Sections[2].ProgressValue = bProg;
                Sections[2].IsAllCompleted = bossTotal > 0 && bossDone >= bossTotal;

                Sections[3].IsFavorite = IsMonthlyFavorite;
                Sections[3].ProgressValue = 0; // 월간 진행률 표시 안함
                // 월간 진행률은 없지만 텍스트는 필요하다면
                Sections[3].IsAllCompleted = false; // 월간은 일단 고정? 아니면 로직 추가
                // var monthlyTotal ... (주석되어 있음)
                // 월간 로직이 주석처리 되어 있어서 IsAllCompleted = false로 두거나, 
                // MonthlyList 체크
                var mTotal = MonthlyList.Count(t => !t.IsDeleted && !t.IsHidden);
                var mDone = MonthlyList.Count(t => !t.IsDeleted && !t.IsHidden && t.IsChecked);
                Sections[3].IsAllCompleted = mTotal > 0 && mDone >= mTotal;

                // CollectionView Refresh를 지연 호출하여 UI 바인딩 충돌 방지
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() =>
                    {
                        try
                        {
                            ActiveSectionsView?.Refresh();
                            HiddenSectionsView?.Refresh();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"SectionsView Refresh error: {ex.Message}");
                        }
                    }));
            }
        }

        private void CheckAndResetTasks()
        {
            if (SelectedCharacter == null) return;

            bool changed = false;
            var now = DateTime.Now;

            foreach (var character in Characters)
            {
                foreach (var task in character.HomeworkList)
                {
                    if (task.CheckReset(now))
                    {
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                CharacterRepository.Save(_appData);
                // 현재 선택된 캐릭터라면 UI 갱신
                if (SelectedCharacter != null)
                {
                    LoadCharacterTasks(SelectedCharacter);
                }
            }
        }

        // --- API & UI Logic extracted from old ViewModel ---

        private int _characterLevelInt = 0;

        public async Task LoadCharacterDataFromApi(string nickname)
        {
            if (string.IsNullOrEmpty(nickname)) return;

            // [Caching] 오늘 이미 업데이트되었다면 API 호출 생략 (캐릭터가 선택된 상태일 때만 체크)
            if (SelectedCharacter != null &&
                SelectedCharacter.Nickname == nickname &&
                SelectedCharacter.LastUpdatedTime.Date == DateTime.Today)
            {
                return;
            }

            var ocid = await _apiService.GetOcidAsync(nickname);
            if (string.IsNullOrEmpty(ocid)) return;

            var basicInfo = await _apiService.GetCharacterInfoAsync(ocid);
            if (basicInfo == null)
            {
                // 어제 데이터도 없으면 그제 데이터 시도 (새벽 0~1시 갱신 딜레이 고려)
                var date = DateTime.Now.AddDays(-2).ToString("yyyy-MM-dd");
                basicInfo = await _apiService.GetCharacterInfoAsync(ocid, date);
            }

            if (basicInfo != null)
            {
                CharacterImage = basicInfo.CharacterImage ?? "";
                CharacterLevel = $"{basicInfo.CharacterLevel}";
                CharacterClass = basicInfo.CharacterClass ?? "-";
                WorldName = basicInfo.WorldName ?? "-";

                _characterLevelInt = basicInfo.CharacterLevel;

                if (SelectedCharacter != null)
                {
                    SelectedCharacter.ImageUrl = CharacterImage;
                    SelectedCharacter.Level = basicInfo.CharacterLevel;
                    SelectedCharacter.CharacterClass = CharacterClass;
                    SelectedCharacter.WorldName = WorldName;
                    SelectedCharacter.LastUpdatedTime = DateTime.Now; // [Caching] 업데이트 시간 갱신
                    CharacterRepository.Save(_appData);
                }
            }

            // 아케인/어센틱 심볼 퀘스트 자동 활성화 로직
            var itemEquip = await _apiService.GetItemEquipmentAsync(ocid);
            var symbolEquip = await _apiService.GetSymbolEquipmentAsync(ocid);

            if (SelectedCharacter != null && (itemEquip != null || symbolEquip != null))
            {
                UpdateSymbolQuests(character: SelectedCharacter, itemEquip?.Title, symbolEquip?.Symbol);
            }
        }

        /*
        private void UpdateSymbolQuests(CharacterProfile character, ItemEquipmentTitle? title, List<SymbolEquipmentInfo>? symbols)
        {
            // 1. 200레벨 이하면 심볼 퀘스트 모두 숨김/비활성 (간단 처리)
            // (정확히는 퀘스트별 레벨 제한이 있지만, 여기서는 보유 여부로 판단)

            // 심볼 맵: 심볼 이름 -> HomeworkTask 이름
            // 소멸의 여로, 츄츄 아일랜드, 레헬른, 아르카나, 모라스, 에스페라
            // 세르니움, 아르크스, 오디움, 도원경, 아르테리아, 카르시온

            // 이미 사용자가 수동으로 켰을 수도 있으므로, "보유 중인데 꺼져있으면 켠다" 로직만 수행
            // 혹은 "미보유인데 켜져있으면 끈다"? -> 사용자가 수동 관리할 수 있게 두는 게 나을 수도 있음.
            if (symbolData?.Symbol != null)
            {
                // 심볼 계산 로직
                // 아케인심볼: 교환불가 성장형
                // 어센틱심볼: 교환불가 성장형

                var symbols = symbolData.Symbol;
                var maxArcane = symbols.Where(s => s.SymbolName != null && s.SymbolName.Contains("아케인심볼"))
                                       .All(s => s.SymbolLevel >= 20);

                var maxAuthentic = symbols.Where(s => s.SymbolName != null && s.SymbolName.Contains("어센틱심볼"))
                                         .All(s => s.SymbolLevel >= 11); // 만랩 기준은 바뀔 수 있음

                IsSymbolDone = maxArcane && maxAuthentic;
            }
        }
        */

        private void UpdateSymbolQuests(CharacterProfile character, ItemEquipmentTitle? title, List<SymbolInfo>? symbols)
        {
            if (symbols == null) return;

            bool changed = false;

            var symbolNames = symbols.Select(s => s.SymbolName).Where(n => !string.IsNullOrEmpty(n)).ToList();

            foreach (var symName in symbolNames)
            {
                string? questName = SymbolToQuestMap(symName!);
                if (questName == null) continue;

                var task = character.HomeworkList.FirstOrDefault(t => t.Name == questName && t.Category == TaskCategory.Daily);
                if (task != null)
                {
                    var s = symbols.First(x => x.SymbolName == symName);
                    // 아케인: 만렙 20, 어센틱: 만렙 11

                    bool isMaxLevel = IsSymbolMaxLevel(symName!, s.SymbolLevel);

                    if (task.IsHidden)
                    {
                        task.IsHidden = false; // 보이게 설정
                        changed = true;
                    }

                    // 만렙이면? -> 사용자가 원하면 끌 수 있게 둠 (자동으로 끄진 않음, 팬케이크 등 교환 가능)
                }
            }

            if (changed)
            {
                CharacterRepository.Save(_appData);
                LoadCharacterTasks(character);
            }
        }

        private bool IsSymbolMaxLevel(string name, int level)
        {
            if (name.Contains("아케인심볼")) return level >= ArcaneMaxLevel;
            if (name.Contains("어센틱심볼")) return level >= AuthenticMaxLevel;
            return false;
        }

        private const int ArcaneMaxLevel = 20;
        private const int AuthenticMaxLevel = 11;

        private string? SymbolToQuestMap(string symbolName)
        {
            if (symbolName.Contains("소멸의 여로")) return "소멸의 여로";
            if (symbolName.Contains("츄츄 아일랜드")) return "츄츄 아일랜드";
            if (symbolName.Contains("레헬른")) return "레헬른";
            if (symbolName.Contains("아르카나")) return "아르카나";
            if (symbolName.Contains("모라스")) return "모라스";
            if (symbolName.Contains("에스페라")) return "에스페라";
            if (symbolName.Contains("세르니움")) return "세르니움"; // 전/후 포함
            if (symbolName.Contains("아르크스")) return "호텔 아르크스";
            if (symbolName.Contains("오디움")) return "오디움";
            if (symbolName.Contains("도원경")) return "도원경";
            if (symbolName.Contains("아르테리아")) return "아르테리아";
            if (symbolName.Contains("카르시온")) return "카르시온";
            return null;
        }

        // --- Theme Application ---

        private void UpdateThemeColors()
        {
            var res = System.Windows.Application.Current.Resources;

            // ThemeService에서 설정한 DynamicResource를 가져와서 ViewModel 속성에 할당
            // 주의: ViewModel의 속성 타입은 Brush이므로, Resource에서 Brush를 가져와야 함.

            // 헬퍼 함수: 리소스에서 Brush 가져오기 (없으면 투명)
            Brush GetBrush(string key) => res[key] as Brush ?? Brushes.Transparent;

            if (IsDarkTheme)
            {
                Background = GetBrush("ThemeBackgroundColor"); // #1E1E2E
                Surface = GetBrush("ThemeBackgroundColor");    // 메인 배경과 동일하게
                SurfaceContainer = GetBrush("ThemeSurfaceColor"); // #2D2D3D (카드 배경)
                OnSurface = GetBrush("ThemeTextColor");        // #FFFFFF
                OnSurfaceVariant = GetBrush("ThemeSecondaryTextColor"); // #888888

                Primary = GetBrush("ThemePrimaryColor");       // #5AC8FA
                Outline = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 255, 255)); // 옅은 흰색 테두리
                CompletedSurface = new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 255, 255, 255));
            }
            else
            {
                Background = GetBrush("ThemeBackgroundColor"); // #F2F2F7
                Surface = GetBrush("ThemeBackgroundColor");
                SurfaceContainer = GetBrush("ThemeSurfaceColor"); // #FFFFFF
                OnSurface = GetBrush("ThemeTextColor");        // #000000
                OnSurfaceVariant = GetBrush("ThemeSecondaryTextColor"); // #646464

                Primary = GetBrush("ThemePrimaryColor");       // #007AFF
                Outline = GetBrush("BorderColor");             // #E2E8F0
                CompletedSurface = new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 0, 0, 0));
            }

            ThemeIcon = IsDarkTheme ? "WeatherMoon24" : "WeatherSunny24";
        }

        public void SaveAllData()
        {
            CharacterRepository.Save(_appData);
        }



        public void UpdateNicknameAndAutoStart(string nickname, bool autoStart)
        {
            if (SelectedCharacter != null)
            {
                SelectedCharacter.Nickname = nickname;
            }
            _appData.AutoStartEnabled = autoStart;
            SaveAllData();
        }

        public void AddNewCharacterWithNickname(string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname)) return;

            var newChar = CharacterRepository.AddCharacter(_appData, nickname);
            if (newChar != null)
            {
                Characters.Add(newChar);
                SelectedCharacter = newChar;
                CharacterRepository.Save(_appData);
                OnPropertyChanged(nameof(CanAddCharacter));
                NotifyDataChanged();
            }
        }



        public void ApplyThemeAndPersist(bool isDark)
        {
            IsDarkTheme = isDark;
            _appData.IsDarkTheme = isDark;
            CharacterRepository.Save(_appData);

            ThemeService.ApplyTheme(isDark); // 전역 리소스 변경
            UpdateThemeColors(); // 바인딩 속성 갱신

            NotifyThemeChanged(); // 다른 창에 알림
        }

        // OnPropertyChanged Implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
