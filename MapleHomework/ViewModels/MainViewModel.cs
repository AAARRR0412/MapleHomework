using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using MapleHomework.Models;
using MapleHomework.Services;

namespace MapleHomework.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        public RelayCommand(Action<object?> execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly MapleApiService _apiService;
        private DispatcherTimer _timer;
        private AppData _appData;

        // 대시보드 윈도우 인스턴스 (중복 열림 방지)
        private DashboardWindow? _dashboardWindow;
        // 보스 수익 계산기 윈도우 인스턴스
        private BossRewardWindow? _bossRewardWindow;

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

        // 편집 모드
        private bool _isEditMode = false;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                OnPropertyChanged();
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

        public IEnumerable<BossDifficulty> DifficultyOptions => Enum.GetValues(typeof(BossDifficulty)).Cast<BossDifficulty>();
        
        // 파티원 수 옵션 (1~6인)
        public IEnumerable<int> PartySizeOptions => new[] { 1, 2, 3, 4, 5, 6 };

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
        public ICommand AddCharacterCommand { get; }
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
        private ReportWindow? _reportWindow;

        public MainViewModel()
        {
            _apiService = new MapleApiService();
            _appData = CharacterRepository.MigrateFromOldConfig();
            
            RemoveDuplicateCharacters();

            IsDarkTheme = _appData.IsDarkTheme;

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
                if (param is CharacterProfile character && Characters.Count > 1)
                {
                    CharacterRepository.RemoveCharacter(_appData, character.Id);
                    Characters.Remove(character);
                    if (SelectedCharacter == character)
                    {
                        SelectedCharacter = Characters.FirstOrDefault();
                    }
                    CharacterRepository.Save(_appData);
                    OnPropertyChanged(nameof(CanAddCharacter));
                    NotifyDataChanged(); // 다른 창에 알림
                }
            });

            ToggleCharacterSelectorCommand = new RelayCommand(_ =>
            {
                IsCharacterSelectorOpen = !IsCharacterSelectorOpen;
                if (!IsCharacterSelectorOpen)
                {
                    IsAddingCharacter = false;
                    NewCharacterName = "";
                }
            });

            OpenDashboardCommand = new RelayCommand(_ =>
            {
                // 이미 열려 있는 창이 있는지 확인
                if (_dashboardWindow != null && _dashboardWindow.IsLoaded)
                {
                    // 최소화되어 있으면 복원
                    if (_dashboardWindow.WindowState == System.Windows.WindowState.Minimized)
                    {
                        _dashboardWindow.WindowState = System.Windows.WindowState.Normal;
                    }
                    // 창을 활성화하고 최상위로 가져오기
                    _dashboardWindow.Activate();
                    _dashboardWindow.Focus();
                }
                else
                {
                    // 새 창 생성
                    _dashboardWindow = new DashboardWindow(_appData, this);
                    _dashboardWindow.Closed += (s, e) => _dashboardWindow = null; // 닫히면 참조 해제
                    _dashboardWindow.Show();
                }
            });

            OpenBossRewardCommand = new RelayCommand(_ =>
            {
                // 이미 열려 있는 창이 있는지 확인
                if (_bossRewardWindow != null && _bossRewardWindow.IsLoaded)
                {
                    if (_bossRewardWindow.WindowState == System.Windows.WindowState.Minimized)
                    {
                        _bossRewardWindow.WindowState = System.Windows.WindowState.Normal;
                    }
                    _bossRewardWindow.Activate();
                    _bossRewardWindow.Focus();
                }
                else
                {
                    _bossRewardWindow = new BossRewardWindow(_appData, this);
                    _bossRewardWindow.Closed += (s, e) => _bossRewardWindow = null;
                    _bossRewardWindow.Show();
                }
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
                if (_reportWindow != null && _reportWindow.IsLoaded)
                {
                    if (_reportWindow.WindowState == System.Windows.WindowState.Minimized)
                        _reportWindow.WindowState = System.Windows.WindowState.Normal;
                    _reportWindow.Activate();
                    _reportWindow.Topmost = true;
                    _reportWindow.Topmost = false;
                    _reportWindow.Focus();
                }
                else
                {
                    _reportWindow = new ReportWindow(this)
                    {
                        Owner = System.Windows.Application.Current.MainWindow,
                        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
                    };
                    _reportWindow.Closed += (s, e) => _reportWindow = null;
                    _reportWindow.Show();
                    _reportWindow.Activate();
                }
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
                    seenNicknames.Add(charData.Nickname);
                    uniqueCharacters.Add(charData);
                }
            }

            if (uniqueCharacters.Count != _appData.Characters.Count)
            {
                _appData.Characters = uniqueCharacters;
                CharacterRepository.Save(_appData);
            }
        }

        private void InitializeViews()
        {
            DailyView = CollectionViewSource.GetDefaultView(DailyList);
            WeeklyView = CollectionViewSource.GetDefaultView(WeeklyList);
            BossView = CollectionViewSource.GetDefaultView(BossList);
            MonthlyView = CollectionViewSource.GetDefaultView(MonthlyList);

            DailyView.Filter = FilterTask;
            WeeklyView.Filter = FilterTask;
            BossView.Filter = FilterTask;
            MonthlyView.Filter = FilterTask;
        }

        private void LoadCharacterTasks(CharacterProfile character)
        {
            foreach (var t in DailyList) t.PropertyChanged -= OnTaskChanged;
            foreach (var t in WeeklyList) t.PropertyChanged -= OnTaskChanged;
            foreach (var t in BossList) t.PropertyChanged -= OnTaskChanged;
            foreach (var t in MonthlyList) t.PropertyChanged -= OnTaskChanged;

            DailyList.Clear();
            WeeklyList.Clear();
            BossList.Clear();
            MonthlyList.Clear();

            if (character.DailyTasks.Count == 0)
            {
                foreach (var task in GameData.Dailies)
                    character.DailyTasks.Add(new HomeworkTask { Name = task.Name, Category = task.Category, RequiredLevel = task.RequiredLevel, IsActive = task.IsActive });
            }
            if (character.WeeklyTasks.Count == 0)
            {
                foreach (var task in GameData.Weeklies)
                    character.WeeklyTasks.Add(new HomeworkTask { Name = task.Name, Category = task.Category, IsActive = task.IsActive });
            }
            if (character.BossTasks.Count == 0)
            {
                foreach (var task in GameData.Bosses)
                    character.BossTasks.Add(new HomeworkTask { Name = task.Name, Category = task.Category, Difficulty = task.Difficulty, AvailableDifficulties = new List<BossDifficulty>(task.AvailableDifficulties), IsActive = task.IsActive });
            }
            if (character.MonthlyTasks.Count == 0)
            {
                foreach (var task in GameData.Monthlies)
                    character.MonthlyTasks.Add(new HomeworkTask { Name = task.Name, Category = task.Category, IsActive = task.IsActive });
            }

            foreach (var task in character.DailyTasks) DailyList.Add(task);
            foreach (var task in character.WeeklyTasks) WeeklyList.Add(task);
            foreach (var task in character.BossTasks) BossList.Add(task);
            foreach (var task in character.MonthlyTasks) MonthlyList.Add(task);

            foreach (var t in DailyList) t.PropertyChanged += OnTaskChanged;
            foreach (var t in WeeklyList) t.PropertyChanged += OnTaskChanged;
            foreach (var t in BossList) t.PropertyChanged += OnTaskChanged;
            foreach (var t in MonthlyList) t.PropertyChanged += OnTaskChanged;

            CharacterName = character.Nickname;
            CharacterImage = character.ImageUrl;
            CharacterLevel = character.Level > 0 ? character.Level.ToString() : "-";
            CharacterClass = !string.IsNullOrEmpty(character.CharacterClass) ? character.CharacterClass : "-";
            WorldName = !string.IsNullOrEmpty(character.WorldName) ? character.WorldName : "-";

            _characterLevelInt = character.Level;

            // 카테고리별 즐겨찾기 상태 업데이트
            OnPropertyChanged(nameof(IsDailyFavorite));
            OnPropertyChanged(nameof(IsWeeklyFavorite));
            OnPropertyChanged(nameof(IsBossFavorite));
            OnPropertyChanged(nameof(IsMonthlyFavorite));

            RefreshAllViews();
            
            _ = CheckAndUpdateCharacterInfo(character);
        }

        private async Task CheckAndUpdateCharacterInfo(CharacterProfile character)
        {
            if (string.IsNullOrEmpty(character.Nickname)) return;
            if (character.Nickname == "새 캐릭터") return;

            if ((DateTime.Now - character.LastUpdatedTime).TotalMinutes < 10) return;

            await LoadCharacterDataFromApi(character.Nickname);
        }

        private bool FilterTask(object obj)
        {
            if (obj is HomeworkTask task)
            {
                if (IsEditMode) return true;
                return task.IsActive && !task.IsChecked;
            }
            return false;
        }

        private void UpdateCompletedList()
        {
            CompletedList.Clear();

            var completedItems = DailyList.Where(t => t.IsActive && t.IsChecked)
                .Concat(WeeklyList.Where(t => t.IsActive && t.IsChecked))
                .Concat(BossList.Where(t => t.IsActive && t.IsChecked))
                .Concat(MonthlyList.Where(t => t.IsActive && t.IsChecked))
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Name);

            foreach (var item in completedItems)
            {
                CompletedList.Add(item);
            }

            OnPropertyChanged(nameof(HasCompletedItems));
        }

        public bool HasCompletedItems => CompletedList.Any();

        private void RefreshAllViews()
        {
            DailyView.Refresh();
            WeeklyView.Refresh();
            BossView.Refresh();
            MonthlyView.Refresh();
            UpdateCompletedList();
            SaveData();
            UpdateProgress();
            
            // 선택된 캐릭터의 진행률/수익 갱신
            SelectedCharacter?.NotifyProgressChanged();
            
            // 열려있는 다른 창들 갱신
            RefreshExternalWindows();
        }

        /// <summary>
        /// 열려있는 외부 창들(Dashboard, BossReward) 갱신
        /// </summary>
        public void RefreshExternalWindows()
        {
            // 대시보드 창 갱신
            if (_dashboardWindow != null && _dashboardWindow.IsLoaded)
            {
                _dashboardWindow.RefreshData();
            }
            
            // 보스 수익 계산기 창 갱신
            if (_bossRewardWindow != null && _bossRewardWindow.IsLoaded)
            {
                _bossRewardWindow.RefreshData();
            }
        }

        public async Task LoadCharacterDataFromApi(string nickname)
        {
            if (string.IsNullOrEmpty(nickname)) return;
            if (SelectedCharacter == null) return;

            string? ocid = SelectedCharacter.Ocid;
            
            if (string.IsNullOrEmpty(ocid))
            {
                ocid = await _apiService.GetOcidAsync(nickname);
            }

            if (string.IsNullOrEmpty(ocid)) 
            { 
                if (SelectedCharacter.Nickname == nickname) CharacterName = "찾을 수 없음"; 
                return; 
            }

            try
            {
                // 1. 기본 정보 (이미지, 레벨 등)
            var basicInfo = await _apiService.GetCharacterInfoAsync(ocid);
            if (basicInfo != null)
            {
                SelectedCharacter.Nickname = basicInfo.CharacterName ?? "알 수 없음";
                SelectedCharacter.ImageUrl = basicInfo.CharacterImage ?? "";
                SelectedCharacter.Level = basicInfo.CharacterLevel;
                SelectedCharacter.CharacterClass = basicInfo.CharacterClass ?? "-";
                SelectedCharacter.WorldName = basicInfo.WorldName ?? "-";
                SelectedCharacter.Ocid = ocid;
                SelectedCharacter.LastUpdatedTime = DateTime.Now;

                if (SelectedCharacter.Nickname == nickname)
                {
                    CharacterName = SelectedCharacter.Nickname;
                    CharacterImage = SelectedCharacter.ImageUrl;
                    CharacterLevel = SelectedCharacter.Level.ToString();
                    CharacterClass = SelectedCharacter.CharacterClass;
                    WorldName = SelectedCharacter.WorldName;
                    
                    _characterLevelInt = SelectedCharacter.Level;
                    ApplyLevelRestrictions(basicInfo.CharacterLevel);
                    await LoadSymbolDataAndAutoDisable(ocid);
                }
                }

                // 2. 유니온 정보 (GetUnionInfoAsync 사용)
                var unionInfo = await _apiService.GetUnionInfoAsync(ocid);
                if (unionInfo != null)
                {
                    SelectedCharacter.UnionLevel = unionInfo.UnionLevel;
                    SelectedCharacter.UnionGrade = unionInfo.UnionGrade;
                }

                // 3. 전투력 정보
                var statInfo = await _apiService.GetCharacterStatAsync(ocid);
                if (statInfo?.FinalStat != null)
                {
                    // 전투력 정보는 현재 사용되지 않으므로 제거
                }

                // await SyncUnionChampions(apiKey, ocid); // 이 기능은 현재 API로 구현 불가하여 비활성화
                
                CharacterRepository.Save(_appData);
            }
            catch (Exception ex)
            {
                // 오류 발생 시 로그 출력 또는 사용자에게 알림
                Console.WriteLine($"Error loading character data: {ex.Message}");
            }
        }

        /*
        private async Task SyncUnionChampions(string apiKey, string ocid)
        {
            var unionData = await _apiService.GetUnionChampionAsync(apiKey, ocid);
            if (unionData?.UnionChampion == null) return;

            var existingNicknames = Characters.Select(c => c.Nickname).ToHashSet();
            bool isDataChanged = false;

            foreach (var champion in unionData.UnionChampion)
            {
                if (string.IsNullOrEmpty(champion.ChampionName)) continue;
                if (existingNicknames.Contains(champion.ChampionName)) continue;

                if (!CharacterRepository.CanAddCharacter(_appData)) break;

                var newChar = CharacterRepository.AddCharacter(_appData, champion.ChampionName);
                if (newChar != null)
                {
                    newChar.CharacterClass = champion.ChampionClass ?? "-";
                    Characters.Add(newChar);
                    isDataChanged = true;
                }
            }

            if (isDataChanged)
            {
                CharacterRepository.Save(_appData);
                OnPropertyChanged(nameof(CanAddCharacter));
            }

            foreach (var character in Characters)
            {
                if (character.Level > 0 && !string.IsNullOrEmpty(character.ImageUrl)) continue;
                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(character.Nickname)) continue;

                try
                {
                    string? champOcid = character.Ocid;
                    if (string.IsNullOrEmpty(champOcid))
                    {
                        champOcid = await _apiService.GetOcidAsync(apiKey, character.Nickname);
                        await Task.Delay(100);
                    }

                    if (!string.IsNullOrEmpty(champOcid))
                    {
                        character.Ocid = champOcid;
                        var champInfo = await _apiService.GetCharacterInfoAsync(apiKey, champOcid);
                        await Task.Delay(100);

                        if (champInfo != null)
                        {
                            character.Nickname = champInfo.CharacterName ?? character.Nickname;
                            character.ImageUrl = champInfo.CharacterImage ?? "";
                            character.Level = champInfo.CharacterLevel;
                            character.CharacterClass = champInfo.CharacterClass ?? "-";
                            character.WorldName = champInfo.WorldName ?? "-";
                            
                            if (character.DailyTasks.Count > 0)
                            {
                                foreach (var task in character.DailyTasks)
                                {
                                    if (task.RequiredLevel > 0 && task.RequiredLevel > character.Level)
                                    {
                                        task.IsActive = false;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            CharacterRepository.Save(_appData);
        }
        */

        public async Task LoadCharacterData(string nickname)
        {
            await LoadCharacterDataFromApi(nickname);
        }

        private int _characterLevelInt = 0;

        private void ApplyLevelRestrictions(int characterLevel)
        {
            _characterLevelInt = characterLevel;

            foreach (var task in DailyList)
            {
                if (task.RequiredLevel > 0 && task.RequiredLevel > characterLevel)
                {
                    task.IsActive = false;
                }
            }

            RefreshAllViews();
        }

        public bool CanActivateTask(HomeworkTask task)
        {
            if (task.RequiredLevel == 0) return true;
            return _characterLevelInt >= task.RequiredLevel;
        }

        private static readonly Dictionary<string, string> SymbolToQuestMap = new()
        {
            { "소멸의 여로", "소멸의 여로" },
            { "츄츄 아일랜드", "츄츄 아일랜드" },
            { "레헬른", "레헬른" },
            { "아르카나", "아르카나" },
            { "모라스", "모라스" },
            { "에스페라", "에스페라" },
            { "세르니움", "세르니움" },
            { "아르크스", "호텔 아르크스" },
            { "오디움", "오디움" },
            { "도원경", "도원경" },
            { "아르테리아", "아르테리아" },
            { "카르시온", "카르시온" },
            { "탈라하트", "탈라하트" }
        };

        private const int ArcaneMaxLevel = 20;
        private const int AuthenticMaxLevel = 11;

        private async Task LoadSymbolDataAndAutoDisable(string ocid)
        {
            var symbolData = await _apiService.GetSymbolEquipmentAsync(ocid);
            if (symbolData?.Symbol == null) return;

            foreach (var symbol in symbolData.Symbol)
            {
                if (string.IsNullOrEmpty(symbol.SymbolName)) continue;

                string cleanName = symbol.SymbolName
                    .Replace("아케인심볼 : ", "")
                    .Replace("어센틱심볼 : ", "")
                    .Trim();

                bool isArcane = symbol.SymbolName.Contains("아케인");
                int maxLevel = isArcane ? ArcaneMaxLevel : AuthenticMaxLevel;
                bool isMaxLevel = symbol.SymbolLevel >= maxLevel;

                if (isMaxLevel && SymbolToQuestMap.TryGetValue(cleanName, out string? questName))
                {
                    var quest = DailyList.FirstOrDefault(d => d.Name == questName);
                    if (quest != null && quest.IsActive)
                    {
                        quest.IsActive = false;
                    }
                }
            }

            RefreshAllViews();
        }

        private void CheckAndResetTasks()
        {
            DateTime now = DateTime.Now;
            DateTime todayMidnight = now.Date;

            foreach (var task in DailyList) if (task.IsChecked && task.LastCheckedTime < todayMidnight) task.IsChecked = false;

            DateTime thisThursday = GetMostRecentThursday(now);
            foreach (var task in WeeklyList.Concat(BossList)) if (task.IsChecked && task.LastCheckedTime < thisThursday) task.IsChecked = false;

            DateTime thisMonthFirst = new DateTime(now.Year, now.Month, 1);
            foreach (var task in MonthlyList) if (task.IsChecked && task.LastCheckedTime < thisMonthFirst) task.IsChecked = false;

            SaveData();
            UpdateProgress();
        }

        private DateTime GetMostRecentThursday(DateTime now)
        {
            DateTime date = now.Date;
            while (date.DayOfWeek != DayOfWeek.Thursday) date = date.AddDays(-1);
            return date;
        }

        private void SaveData()
        {
            if (SelectedCharacter == null) return;

            SelectedCharacter.DailyTasks = new List<HomeworkTask>(DailyList);
            SelectedCharacter.WeeklyTasks = new List<HomeworkTask>(WeeklyList);
            SelectedCharacter.BossTasks = new List<HomeworkTask>(BossList);
            SelectedCharacter.MonthlyTasks = new List<HomeworkTask>(MonthlyList);

            CharacterRepository.Save(_appData);
        }

        /// <summary>
        /// 자동 시작 설정을 ViewModel의 AppData와 함께 동기화
        /// (설정 창에서 저장 시 in-memory 데이터가 덮어써지는 문제 방지)
        /// </summary>
        public void UpdateAutoStart(bool autoStartEnabled)
        {
            _appData.AutoStartEnabled = autoStartEnabled;
            CharacterRepository.Save(_appData);
        }

        /// <summary>
        /// 닉네임과 자동 시작 설정을 함께 저장
        /// </summary>
        public void UpdateNicknameAndAutoStart(string nickname, bool autoStartEnabled)
        {
            _appData.AutoStartEnabled = autoStartEnabled;
            
            // 현재 선택된 캐릭터의 닉네임도 저장
            if (SelectedCharacter != null)
            {
                SelectedCharacter.Nickname = nickname;
            }
            
            CharacterRepository.Save(_appData);
        }

        /// <summary>
        /// 새 캐릭터를 닉네임으로 추가하고 선택
        /// </summary>
        public void AddNewCharacterWithNickname(string nickname)
        {
            var newChar = CharacterRepository.AddCharacter(_appData, nickname);
            if (newChar != null)
            {
                Characters.Add(newChar);
                SelectedCharacter = newChar;
                _appData.SelectedCharacterId = newChar.Id;
                CharacterRepository.Save(_appData);
            }
        }

        /// <summary>
        /// 프로그램 종료 시 모든 데이터 저장
        /// </summary>
        public void SaveAllData()
        {
            // 현재 캐릭터의 숙제 데이터 저장
            if (SelectedCharacter != null)
            {
                SelectedCharacter.DailyTasks = new List<HomeworkTask>(DailyList);
                SelectedCharacter.WeeklyTasks = new List<HomeworkTask>(WeeklyList);
                SelectedCharacter.BossTasks = new List<HomeworkTask>(BossList);
                SelectedCharacter.MonthlyTasks = new List<HomeworkTask>(MonthlyList);
                _appData.SelectedCharacterId = SelectedCharacter.Id;
            }

            // 테마 설정 저장
            _appData.IsDarkTheme = IsDarkTheme;

            // 캐릭터 데이터 저장
            CharacterRepository.Save(_appData);

            // config.json에도 테마 저장 (호환성)
            var settings = ConfigManager.Load();
            settings.IsDarkTheme = IsDarkTheme;
            ConfigManager.Save(settings);
        }

        private void OnTaskChanged(object? s, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HomeworkTask.IsChecked) ||
                e.PropertyName == nameof(HomeworkTask.IsActive) ||
                e.PropertyName == nameof(HomeworkTask.Difficulty))
            {
                if (e.PropertyName == nameof(HomeworkTask.IsActive) && s is HomeworkTask task)
                {
                    if (task.Category == TaskCategory.Daily && task.IsActive)
                    {
                        if (task.RequiredLevel > 0 && task.RequiredLevel > _characterLevelInt)
                        {
                            task.IsActive = false;
                            return;
                        }
                    }

                    if (task.Category == TaskCategory.Boss && task.IsActive)
                    {
                        int activeBossCount = BossList.Count(b => b.IsActive);
                        if (activeBossCount > 12)
                        {
                            task.IsActive = false;
                            return;
                        }
                    }
                }

                SaveData();
                UpdateProgress();
                if (e.PropertyName == nameof(HomeworkTask.Difficulty)) BossView.Refresh();
            }
        }

        /// <summary>
        /// 테마를 토글 버튼/설정창 모두에서 공용으로 적용하고 저장
        /// </summary>
        public void ApplyThemeAndPersist(bool isDark)
        {
            IsDarkTheme = isDark;
            _appData.IsDarkTheme = isDark;

            // in-memory → 저장소 동기화
            CharacterRepository.Save(_appData);

            // config.json에도 저장 (설정 창과 동기화)
            var settings = ConfigManager.Load();
            settings.IsDarkTheme = isDark;
            settings.ThemeMode = isDark ? ThemeMode.Dark : ThemeMode.Light;
            ConfigManager.Save(settings);

            // UI 리소스 갱신
            UpdateThemeColors();
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(ThemeIcon));

            // 열려 있는 모든 창 갱신
            NotifyThemeChanged();
        }

        private void UpdateProgress()
        {
            var activeDaily = DailyList.Where(t => t.IsActive).ToList();
            var dailyTarget = activeDaily.Any() ? (double)activeDaily.Count(t => t.IsChecked) / activeDaily.Count * 100 : 0;

            var activeWeekly = WeeklyList.Where(t => t.IsActive).ToList();
            var weeklyTarget = activeWeekly.Any() ? (double)activeWeekly.Count(t => t.IsChecked) / activeWeekly.Count * 100 : 0;

            var activeBoss = BossList.Where(t => t.IsActive).ToList();
            var bossTarget = activeBoss.Any() ? (double)activeBoss.Count(t => t.IsChecked) / activeBoss.Count * 100 : 0;

            SetProgressWithAnimation(dailyTarget, weeklyTarget, bossTarget);

            int bossCheckCount = BossList.Where(t => t.IsActive && t.IsChecked).Count();
            BossCountText = $"{bossCheckCount}/12";
        }

        private void UpdateThemeColors()
        {
            var resources = System.Windows.Application.Current.Resources;
            
            if (IsDarkTheme)
            {
                ThemeIcon = "WeatherMoon24";
                Background = new SolidColorBrush(Color.FromRgb(61, 74, 92));
                Surface = new SolidColorBrush(Color.FromRgb(74, 90, 110));
                SurfaceContainer = new SolidColorBrush(Color.FromRgb(85, 85, 95));
                OnSurface = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                OnSurfaceVariant = new SolidColorBrush(Color.FromRgb(200, 210, 220));
                Primary = new SolidColorBrush(Color.FromRgb(90, 200, 250));
                Outline = new SolidColorBrush(Color.FromRgb(100, 110, 125));
                CompletedSurface = new SolidColorBrush(Color.FromRgb(70, 85, 100));
                
                // Application Resources 업데이트 (다크 모드)
                resources["WindowBackground"] = new SolidColorBrush(Color.FromRgb(26, 29, 46));
                resources["CardBackground"] = new SolidColorBrush(Color.FromRgb(40, 45, 65));
                resources["CardBackgroundHover"] = new SolidColorBrush(Color.FromRgb(50, 55, 75));
                resources["CardBackgroundAlt"] = new SolidColorBrush(Color.FromRgb(35, 40, 58));
                resources["TextPrimary"] = new SolidColorBrush(Color.FromRgb(240, 240, 245));
                resources["TextSecondary"] = new SolidColorBrush(Color.FromRgb(180, 185, 195));
                resources["TextMuted"] = new SolidColorBrush(Color.FromRgb(140, 145, 155));
                resources["TextInverse"] = new SolidColorBrush(Color.FromRgb(30, 35, 45));
                resources["DividerColor"] = new SolidColorBrush(Color.FromRgb(60, 65, 85));
                resources["BorderColor"] = new SolidColorBrush(Color.FromRgb(70, 75, 95));
                resources["ApiCardBackground"] = new SolidColorBrush(Color.FromRgb(50, 45, 40));
                resources["ApiCardBorder"] = new SolidColorBrush(Color.FromRgb(90, 75, 55));
                resources["ItemCardBackground"] = new SolidColorBrush(Color.FromRgb(45, 50, 70));
                resources["ItemCardBorder"] = new SolidColorBrush(Color.FromRgb(65, 70, 90));
                resources["ItemCardHover"] = new SolidColorBrush(Color.FromRgb(55, 60, 80));
                resources["BadgeBackground"] = new SolidColorBrush(Color.FromRgb(45, 50, 75));
                resources["BadgeText"] = new SolidColorBrush(Color.FromRgb(165, 180, 255));
                resources["DetailViewBackground"] = new SolidColorBrush(Color.FromRgb(26, 29, 46));
            }
            else
            {
                ThemeIcon = "WeatherSunny24";
                Background = new SolidColorBrush(Color.FromRgb(61, 74, 92));
                Surface = new SolidColorBrush(Color.FromRgb(250, 250, 252));
                SurfaceContainer = new SolidColorBrush(Color.FromRgb(235, 235, 240));
                OnSurface = new SolidColorBrush(Color.FromRgb(50, 55, 65));
                OnSurfaceVariant = new SolidColorBrush(Color.FromRgb(100, 105, 115));
                Primary = new SolidColorBrush(Color.FromRgb(90, 200, 250));
                Outline = new SolidColorBrush(Color.FromRgb(210, 215, 220));
                CompletedSurface = new SolidColorBrush(Color.FromRgb(245, 248, 250));
                
                // Application Resources 업데이트 (라이트 모드)
                resources["WindowBackground"] = new SolidColorBrush(Color.FromRgb(245, 247, 251));
                resources["CardBackground"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                resources["CardBackgroundHover"] = new SolidColorBrush(Color.FromRgb(238, 242, 247));
                resources["CardBackgroundAlt"] = new SolidColorBrush(Color.FromRgb(248, 250, 252));
                resources["TextPrimary"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                resources["TextSecondary"] = new SolidColorBrush(Color.FromRgb(71, 85, 105));
                resources["TextMuted"] = new SolidColorBrush(Color.FromRgb(100, 116, 139));
                resources["TextInverse"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                resources["DividerColor"] = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                resources["BorderColor"] = new SolidColorBrush(Color.FromRgb(226, 232, 240));
                resources["ApiCardBackground"] = new SolidColorBrush(Color.FromRgb(255, 246, 236));
                resources["ApiCardBorder"] = new SolidColorBrush(Color.FromRgb(255, 227, 194));
                resources["ItemCardBackground"] = new SolidColorBrush(Color.FromRgb(250, 251, 252));
                resources["ItemCardBorder"] = new SolidColorBrush(Color.FromRgb(229, 233, 240));
                resources["ItemCardHover"] = new SolidColorBrush(Color.FromRgb(240, 244, 248));
                resources["BadgeBackground"] = new SolidColorBrush(Color.FromRgb(238, 242, 255));
                resources["BadgeText"] = new SolidColorBrush(Color.FromRgb(99, 102, 241));
                resources["DetailViewBackground"] = new SolidColorBrush(Color.FromRgb(26, 29, 46));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
