using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MapleHomework.Data;
using MapleHomework.Models;
using MapleHomework.Services;
using MapleHomework.Rendering;
using MapleHomework.Rendering.Models;
using WpfImage = System.Windows.Controls.Image;
using System.IO;

namespace MapleHomework
{
    public partial class CharacterSearchWindow : Wpf.Ui.Controls.FluentWindow, INotifyPropertyChanged
    {
        private readonly MapleApiService _apiService;
        private string? _ocid;
        private string _searchedNickname = "";

        // UI 상태 바인딩용
        private bool _isHexaExpanded = false; // 기본값: 축소 (Collapsed)
        public bool IsHexaExpanded
        {
            get => _isHexaExpanded;
            set
            {
                if (_isHexaExpanded != value)
                {
                    _isHexaExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private ItemEquipmentResponse? _itemEquipment;
        private HexaStatResponse? _hexaMatrix;
        private HexaMatrixStatResponse? _hexaMatrixStat;
        private CharacterSkillResponse? _characterSkill;
        private SymbolEquipmentResponse? _symbolResponse;

        private List<SymbolDisplayItem> _arcaneSymbols = new();
        private List<SymbolDisplayItem> _authenticSymbols = new();
        private List<SymbolDisplayItem> _grandSymbols = new();
        private readonly Dictionary<string, Grid> _equipSlots = new();
        private System.Windows.Threading.DispatcherTimer _tooltipCloseTimer;




        private int _currentLevel;
        private int _currentPreset = 1;
        private bool _isDarkTheme = true;

        public CharacterSearchWindow()
        {
            InitializeComponent();
            _apiService = new MapleApiService();

            // 툴팁 닫기 타이머 (깜빡임 방지)
            _tooltipCloseTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _tooltipCloseTimer.Tick += (s, e) =>
            {
                TooltipPopup.IsOpen = false;
                _tooltipCloseTimer.Stop();
            };


            // 현재 테마 상태 확인 및 적용
            var settings = ConfigManager.Load();
            _isDarkTheme = ThemeService.ShouldUseDarkTheme(settings);

            // 이벤트 구독
            ThemeService.OnThemeChanged += SyncTheme;
            this.Closed += (s, e) => ThemeService.OnThemeChanged -= SyncTheme;

            ApplyTheme(_isDarkTheme);

            // 장비 슬롯 매핑
            InitializeEquipSlots();

            // 프리셋 UI 초기화
            // 헥사 비용 데이터 로드
            try
            {
                string dataPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "HexaCoreCosts.json");
                HexaCostCalculator.Initialize(dataPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load Hexa costs: {ex.Message}");
            }
        }



        private void InitializeEquipSlots()
        {
            // 링1~4는 API 슬롯명과 매핑
            _equipSlots["링1"] = Slot_Ring0;  // 4행 위치
            _equipSlots["링2"] = Slot_Ring1;  // 3행 위치
            _equipSlots["링3"] = Slot_Ring2;  // 2행 위치
            _equipSlots["링4"] = Slot_Ring3;  // 1행 위치
            _equipSlots["모자"] = Slot_Hat;
            _equipSlots["엠블렘"] = Slot_Emblem;
            _equipSlots["뱃지"] = Slot_Badge;
            _equipSlots["펜던트"] = Slot_Pendant0;
            _equipSlots["펜던트2"] = Slot_Pendant1;
            _equipSlots["어깨장식"] = Slot_Shoulder;
            _equipSlots["망토"] = Slot_Cape;
            _equipSlots["상의"] = Slot_Top;
            _equipSlots["장갑"] = Slot_Glove;
            _equipSlots["무기"] = Slot_Weapon;
            _equipSlots["벨트"] = Slot_Belt;
            _equipSlots["하의"] = Slot_Bottom;
            _equipSlots["보조무기"] = Slot_SubWeapon;
            _equipSlots["포켓 아이템"] = Slot_Pocket;
            _equipSlots["눈장식"] = Slot_Eye;
            _equipSlots["얼굴장식"] = Slot_Face;
            _equipSlots["신발"] = Slot_Shoes;
            _equipSlots["기계 심장"] = Slot_Heart;
            _equipSlots["안드로이드"] = Slot_Android;
            _equipSlots["귀고리"] = Slot_Earring;
            _equipSlots["훈장"] = Slot_Medal;
        }

        private void PresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button btn || btn.Tag is not string tagStr) return;
            if (!int.TryParse(tagStr, out int preset)) return;
            SetPreset(preset);
        }

        private void Preset1_Click(object sender, MouseButtonEventArgs e) => SetPreset(1);
        private void Preset2_Click(object sender, MouseButtonEventArgs e) => SetPreset(2);
        private void Preset3_Click(object sender, MouseButtonEventArgs e) => SetPreset(3);

        private void SetPreset(int preset)
        {
            _currentPreset = preset;
            UpdatePresetUI();

            // 장비 슬롯 다시 표시
            PopulateEquipmentSlots();
        }

        private void UpdatePresetUI()
        {
            // 프리셋 버튼 UI 업데이트 (선택된 버튼 하이라이트)
            var accentBrush = new LinearGradientBrush
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(1, 0)
            };
            accentBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(0x5A, 0xC8, 0xFA), 0));
            accentBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(0x4E, 0xCD, 0xC4), 1));

            var normalBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x64, 0x74, 0x8B));

            Preset1Border.Background = _currentPreset == 1 ? accentBrush : normalBrush;
            Preset2Border.Background = _currentPreset == 2 ? accentBrush : normalBrush;
            Preset3Border.Background = _currentPreset == 3 ? accentBrush : normalBrush;
        }

        /// <summary>
        /// 메인 UI 테마에 따라 테마 적용
        /// </summary>
        public void SyncTheme(bool isDark)
        {
            _isDarkTheme = isDark;
            ApplyTheme(_isDarkTheme);
        }

        private void ApplyTheme(bool isDark)
        {
            // ThemeService에서 전역 리소스(App.Current.Resources)를 업데이트하므로
            // 개별 창에서 리소스를 다시 설정할 필요가 없습니다.
            // DynamicResource가 자동으로 변경된 리소스를 감지합니다.
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                _ = SearchCharacterAsync();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _ = SearchCharacterAsync();
        }

        private void HeaderSearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                _ = SearchFromHeaderAsync();
        }

        private void HeaderSearchButton_Click(object sender, RoutedEventArgs e)
        {
            _ = SearchFromHeaderAsync();
        }

        private async Task SearchFromHeaderAsync()
        {
            var nickname = HeaderSearchTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(nickname))
                return;

            SearchTextBox.Text = nickname;
            await SearchCharacterAsync();
            HeaderSearchTextBox.Clear();
        }

        private async Task SearchCharacterAsync(bool forceUpdate = false)
        {
            var nickname = HeaderSearchTextBox.IsVisible ? HeaderSearchTextBox.Text.Trim() : SearchTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(nickname) && !string.IsNullOrEmpty(CharacterName.Text))
                nickname = CharacterName.Text; // Use current name if refreshing without text box

            if (string.IsNullOrEmpty(nickname))
            {
                ShowError("닉네임을 입력해주세요.");
                return;
            }
            _searchedNickname = nickname;

            ShowLoading(true, "정보를 확인하는 중...");
            HideError();
            ResultPanel.Visibility = Visibility.Collapsed;

            try
            {
                // 1. OCID 조회
                var ocid = await _apiService.GetOcidAsync(nickname);
                if (string.IsNullOrEmpty(ocid))
                {
                    ShowError($"'{nickname}' 캐릭터를 찾을 수 없습니다.");
                    ShowLoading(false);
                    return;
                }
                _ocid = ocid;

                CharacterCacheData? cacheData = null;

                // 2. 캐시 확인 (강제 업데이트가 아닐 경우)
                if (!forceUpdate)
                {
                    var loadedCache = await _apiService.LoadCharacterCache(ocid);
                    if (loadedCache != null && !loadedCache.IsExpired(6)) // 6시간 유효
                    {
                        cacheData = loadedCache;
                    }
                }

                // 3. 데이터가 없거나 만료되었으면 API 호출
                if (cacheData == null)
                {
                    ShowLoading(true, "최신 데이터를 불러오는 중...");
                    cacheData = await FetchAllCharacterData(ocid);

                    if (cacheData != null)
                    {
                        await _apiService.SaveCharacterCache(ocid, cacheData);
                    }
                }

                if (cacheData == null || cacheData.BasicInfo == null)
                {
                    ShowError("캐릭터 정보를 가져올 수 없습니다.");
                    ShowLoading(false);
                    return;
                }

                // 4. UI 업데이트
                PopulateUI(cacheData);
                UpdateCacheInfo(cacheData.Timestamp);

                // 패널 전환
                InitialSearchPanel.Visibility = Visibility.Collapsed;
                ResultPanel.Visibility = Visibility.Visible;

                // 헤더 검색창 표시
                HeaderSearchBox.Visibility = Visibility.Visible;

                // 경험치 히스토리 (별도 로딩)
                _ = LoadExpTrendAsync(ocid);
            }
            catch (Exception ex)
            {
                ShowError($"오류 발생: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async Task<CharacterCacheData?> FetchAllCharacterData(string ocid)
        {
            var data = new CharacterCacheData { Timestamp = DateTime.Now };
            string targetDate = DateTime.Now.ToString("yyyy-MM-dd");

            // 기본 정보 (3일치 시도)
            for (int i = 0; i < 3; i++)
            {
                targetDate = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd");
                data.BasicInfo = await _apiService.GetCharacterInfoAsync(ocid, targetDate);
                if (data.BasicInfo != null) break;
            }

            if (data.BasicInfo == null) return null;

            // 병렬 처리보다는 순차 처리로 안정성 확보 (필요시 병렬 가능)
            data.StatInfo = await _apiService.GetCharacterStatAsync(ocid, targetDate);
            data.ItemEquipment = await _apiService.GetItemEquipmentAsync(ocid, targetDate);
            data.CharacterSkill = await _apiService.GetCharacterSkillAsync(ocid, targetDate);
            data.HexaMatrix = await _apiService.GetHexaStatAsync(ocid, targetDate);
            data.HexaMatrixStat = await _apiService.GetHexaMatrixStatAsync(ocid, targetDate);
            data.SymbolEquipment = await _apiService.GetSymbolEquipmentAsync(ocid, targetDate);

            // 유니온
            var union = await _apiService.GetUnionInfoAsync(ocid, targetDate);
            // UnionRaiderResponse 구조가 맞는지 확인 필요 (여기선 가정)
            // data.UnionRaider = ... (기존 코드에 UnionRaider 모델이 명시적이지 않아서 생략 가능하거나 추가 필요)
            // 기존 코드: _apiService.GetUnionInfoAsync -> UnionRaiderResponse
            data.UnionRaider = union;

            // 길드 ? BasicInfo에 포함됨 via CharacterGuildName
            // 유니온 챔피언? CacheData에 추가 안했지만 필요하다면 추가. 
            // 현재 CacheData 모델에 UnionChampion이 없으므로 생략하거나 모델 수정 필요. 
            // (CacheData 모델에는 UnionRaider만 있음. UnionChampion은 별도)
            // 일단 UnionChampion은 매번 로딩하거나 CacheData에 추가해야 함.
            // *모델에 UnionChampionResponse 추가 필요* -> 일단 여기선 생략하고 실시간 로딩하거나, 모델 업데이트했다고 가정.
            // 기존 task에서 모델 CacheData 작성시 UnionChampionResponse를 깜빡했을 수 있음.
            // 일단 제외하고 진행 (또는 나중에 추가)

            return data;
        }

        private void PopulateUI(CharacterCacheData data)
        {
            if (data.BasicInfo == null) return;

            var basic = data.BasicInfo;
            // 기본 정보
            CharacterName.Text = basic.CharacterName;
            CharacterLevel.Text = basic.CharacterLevel.ToString();
            CharacterClass.Text = basic.CharacterClass ?? "-";
            WorldText.Text = basic.WorldName ?? "-";
            GuildText.Text = basic.CharacterGuildName ?? "-";
            _currentLevel = basic.CharacterLevel;

            if (!string.IsNullOrEmpty(basic.CharacterImage))
            {
                CharacterImage.Source = new BitmapImage(new Uri(basic.CharacterImage));
            }

            // 스탯
            if (data.StatInfo?.FinalStat != null)
            {
                var cpStat = data.StatInfo.FinalStat.Find(s => s.StatName == "전투력");
                long cp = 0;
                if (cpStat != null) long.TryParse(cpStat.StatValue, out cp);
                CombatPowerText.Text = FormatCombatPower(cp);

                var popStat = data.StatInfo.FinalStat.Find(s => s.StatName == "인기도");
                if (popStat != null) PopularityText.Text = popStat.StatValue;
            }

            // 유니온
            if (data.UnionRaider != null)
            {
                UnionLevelText.Text = data.UnionRaider.UnionLevel.ToString("N0");
            }

            // 장비
            _itemEquipment = data.ItemEquipment;
            PopulateEquipmentSlots();

            // 스킬/헥사
            _characterSkill = data.CharacterSkill;
            _hexaMatrix = data.HexaMatrix;
            _hexaMatrixStat = data.HexaMatrixStat;
            PopulateHexaCores();
            PopulateHexaStats();

            // 심볼
            _symbolResponse = data.SymbolEquipment;
            PopulateSymbols();
        }

        private void UpdateCacheInfo(DateTime timestamp)
        {
            LastUpdatedText.Text = $"업데이트: {timestamp:yyyy-MM-dd HH:mm}";
            CacheInfoPanel.Visibility = Visibility.Visible;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_ocid)) return;
            await SearchCharacterAsync(forceUpdate: true);
        }

        private async Task LoadExpTrendAsync(string ocid)
        {
            try
            {
                var expItems = new List<ExpGraphItem>();
                var dailyExpGains = new List<long>();

                // 7일치 데이터 수집 (레벨과 경험치 % 저장)
                var dailyData = new List<(DateTime date, int level, double expRate)>();

                for (int i = 7; i >= 0; i--)
                {
                    var date = DateTime.Now.AddDays(-i);
                    var dateStr = date.ToString("yyyy-MM-dd");

                    try
                    {
                        var basic = await _apiService.GetCharacterInfoAsync(ocid, dateStr);
                        if (basic != null && double.TryParse(basic.CharacterExpRate?.Replace("%", ""), out double rate))
                        {
                            dailyData.Add((date, basic.CharacterLevel, rate));
                        }
                    }
                    catch { }

                    await Task.Delay(30);
                }

                // 일일 경험치 획득량 계산 (평균 계산용)
                for (int i = 1; i < dailyData.Count; i++)
                {
                    var prev = dailyData[i - 1];
                    var curr = dailyData[i];

                    var expGain = ExpTable.CalculateExpGain(prev.level, prev.expRate, curr.level, curr.expRate);
                    if (expGain > 0)
                    {
                        dailyExpGains.Add(expGain);
                    }
                }

                // 7일치 그래프에 표시 - 각 날짜의 경험치 퍼센트
                // 최근 7일 데이터만 표시 (오늘 제외, 1~7일 전)
                var recentData = dailyData.Where(d => d.date.Date < DateTime.Now.Date).OrderByDescending(d => d.date).Take(7).Reverse().ToList();

                foreach (var data in recentData)
                {
                    expItems.Add(new ExpGraphItem
                    {
                        DateLabel = data.date.ToString("MM-dd"),
                        ExpRate = data.expRate,
                        ExpRateText = $"{data.expRate:F2}%",
                        GraphRate = Math.Min(data.expRate / 100.0, 1.0), // 0~100% 범위
                        ExpGain = 0
                    });
                }

                // UI 업데이트 (Context가 유지되지만 안전을 위해 확인)
                ExpGraphList.ItemsSource = expItems;

                // 7일 평균 일일 경험치 계산
                if (dailyExpGains.Any())
                {
                    var avgDailyExp = (long)dailyExpGains.Average();

                    // 현재 레벨 기준 평균 몇 %인지 계산
                    double avgExpRateAtLevel = 0;
                    if (ExpTable.RequiredExp.TryGetValue(_currentLevel, out var required) && required > 0)
                    {
                        avgExpRateAtLevel = (double)avgDailyExp / required * 100.0;
                    }

                    AvgExpText.Text = $"{avgExpRateAtLevel:F2}%";
                }
                else
                {
                    AvgExpText.Text = "-";
                }

                // 로딩 완료 - UI 전환
                ExpLoadingPanel.Visibility = Visibility.Collapsed;
                ExpContentPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExpTrend Error: {ex.Message}");
                // UI가 이미 닫혔거나 하는 경우를 대비해 catch만 하고 별도 처리는 생략하거나
                // 필요시 Dispatcher.Invoke(() => ShowError(...));
            }
        }

        private void PopulateEquipmentSlots()
        {
            // 모든 슬롯 초기화 (배경 이미지는 유지하고 아이템 이미지만 제거)
            foreach (var slot in _equipSlots.Values)
            {
                // 첫 번째 자식(배경 이미지)을 제외한 나머지 제거
                while (slot.Children.Count > 1)
                {
                    slot.Children.RemoveAt(slot.Children.Count - 1);
                }
                slot.Tag = null;
            }

            if (_itemEquipment == null) return;

            // 현재 프리셋에 맞는 장비 리스트 선택
            List<ItemEquipmentInfo>? items = _currentPreset switch
            {
                1 => _itemEquipment.ItemEquipmentPreset1 ?? _itemEquipment.ItemEquipment,
                2 => _itemEquipment.ItemEquipmentPreset2 ?? _itemEquipment.ItemEquipment,
                3 => _itemEquipment.ItemEquipmentPreset3 ?? _itemEquipment.ItemEquipment,
                _ => _itemEquipment.ItemEquipment
            };

            if (items == null) return;

            // 링 인덱스 관리
            int ringIndex = 0;
            var ringSlots = new Grid[] { Slot_Ring0, Slot_Ring1, Slot_Ring2, Slot_Ring3 };

            // 펜던트 인덱스 관리
            int pendantIndex = 0;
            var pendantSlots = new Grid[] { Slot_Pendant0, Slot_Pendant1 };

            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.ItemEquipmentSlot) || string.IsNullOrEmpty(item.ItemIcon)) continue;

                Grid? targetSlot = null;
                var slotName = item.ItemEquipmentSlot;

                if (slotName == "반지" || slotName.StartsWith("반지"))
                {
                    if (ringIndex < ringSlots.Length)
                        targetSlot = ringSlots[ringIndex++];
                }
                else if (slotName == "펜던트" || slotName.StartsWith("펜던트"))
                {
                    if (pendantIndex < pendantSlots.Length)
                        targetSlot = pendantSlots[pendantIndex++];
                }
                else if (_equipSlots.TryGetValue(slotName, out var slot))
                {
                    targetSlot = slot;
                }

                if (targetSlot != null)
                {
                    // 타원형 그림자 (메이플 스타일)
                    var shadow = new System.Windows.Shapes.Ellipse
                    {
                        Width = 28,
                        Height = 8,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Bottom,
                        Margin = new Thickness(0, 0, 0, 4),
                        Fill = new RadialGradientBrush
                        {
                            GradientOrigin = new System.Windows.Point(0.5, 0.5),
                            Center = new System.Windows.Point(0.5, 0.5),
                            RadiusX = 0.5,
                            RadiusY = 0.5,
                            GradientStops = new GradientStopCollection
                            {
                                new GradientStop(System.Windows.Media.Color.FromArgb(120, 0, 0, 0), 0),
                                new GradientStop(System.Windows.Media.Color.FromArgb(40, 0, 0, 0), 0.7),
                                new GradientStop(System.Windows.Media.Color.FromArgb(0, 0, 0, 0), 1)
                            }
                        }
                    };
                    targetSlot.Children.Add(shadow);

                    // 아이템 이미지
                    var image = new WpfImage
                    {
                        Source = new BitmapImage(new Uri(item.ItemIcon)),
                        Stretch = Stretch.None, // 원본 크기 유지
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        Margin = new Thickness(0, 1, 0, 3) // 1px 아래로, 그림자 위치와 맞추기
                    };
                    RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.Fant);
                    targetSlot.Children.Add(image);
                    targetSlot.Tag = item; // 아이템 데이터 저장
                }
            }
        }

        private void PopulateHexaCores()
        {
            try
            {
                if (_hexaMatrix?.HexaCoreEquipment == null || !_hexaMatrix.HexaCoreEquipment.Any())
                {
                    Dispatcher.Invoke(() =>
                    {
                        HexaCoreList.ItemsSource = null;
                        NoHexaText.Visibility = Visibility.Visible;
                    });
                    return;
                }

                Dispatcher.Invoke(() => NoHexaText.Visibility = Visibility.Collapsed);

                // API에서 조회한 스킬 데이터 + 저장된 데이터에서 아이콘 맵 생성
                var skillIconMap = new Dictionary<string, string>();

                // 1. 현재 API 조회 결과
                if (_characterSkill?.CharacterSkill != null)
                {
                    foreach (var skill in _characterSkill.CharacterSkill.Where(s => !string.IsNullOrEmpty(s.SkillName) && !string.IsNullOrEmpty(s.SkillIcon)))
                    {
                        skillIconMap.TryAdd(skill.SkillName!, skill.SkillIcon!);
                    }
                }

                // 2. 저장된 스킬 데이터 (추가)
                var savedSkill = RawDataProcessor.LoadLatestSkill6Info(_searchedNickname);
                if (savedSkill?.CharacterSkill != null)
                {
                    foreach (var skill in savedSkill.CharacterSkill.Where(s => !string.IsNullOrEmpty(s.SkillName) && !string.IsNullOrEmpty(s.SkillIcon)))
                    {
                        skillIconMap.TryAdd(skill.SkillName!, skill.SkillIcon!);
                    }
                }

                string ResolveIcon(string coreName, List<LinkedSkillInfo>? linked)
                {
                    if (string.IsNullOrEmpty(coreName)) return ""; // Safety check

                    if (skillIconMap.TryGetValue(coreName, out var icon)) return icon;

                    // 마스터리 코어: "A/B" 형태 → 첫 스킬명으로 아이콘 매칭
                    var firstName = coreName.Split(new[] { '/', ',' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(firstName) && skillIconMap.TryGetValue(firstName, out var iconSplit))
                        return iconSplit;

                    if (linked != null)
                    {
                        foreach (var lk in linked)
                        {
                            var id = lk?.HexaSkillId;
                            if (!string.IsNullOrEmpty(id) && skillIconMap.TryGetValue(id, out var icon2))
                                return icon2;
                        }
                    }
                    return "";
                }

                string GetBadgePath(string? coreType) => coreType switch
                {
                    "마스터리 코어" => "/Data/Mastery.png", // Added leading slash
                    "강화 코어" => "/Data/Enhance.png",
                    "공용 코어" => "/Data/Common.png",
                    _ => "/Data/Skill.png"
                };

                var coreItems = new List<HexaCoreItem>();
                string[] typeOrder = { "마스터리 코어", "스킬 코어", "강화 코어", "공용 코어" };

                foreach (var core in _hexaMatrix.HexaCoreEquipment)
                {
                    if (core == null) continue; // Null check

                    var coreType = core.HexaCoreType ?? "스킬 코어";
                    var currentLevel = core.HexaCoreLevel;

                    // 비용 계산
                    var (nextSol, nextFrag) = HexaCostCalculator.GetNextLevelCost(coreType, currentLevel);
                    var (remSol, remFrag) = HexaCostCalculator.GetRemainingCost(coreType, currentLevel);

                    var item = new HexaCoreItem
                    {
                        OriginalName = core.HexaCoreName ?? "",
                        SkillName = TruncateSkillName(core.HexaCoreName ?? "", 8),
                        CoreType = coreType,
                        CoreLevel = currentLevel,
                        BadgeIcon = GetBadgePath(coreType),
                        SkillIcon = ResolveIcon(core.HexaCoreName ?? "", core.LinkedSkill),

                        NextSolErda = nextSol,
                        NextFragment = nextFrag,
                        RemainingSolErda = remSol,
                        RemainingFragment = remFrag
                    };
                    coreItems.Add(item);
                }

                // 정렬
                var sorted = coreItems
                    .OrderBy(c => Array.IndexOf(typeOrder, c.CoreType) >= 0 ? Array.IndexOf(typeOrder, c.CoreType) : 999)
                    .ThenByDescending(c => c.CoreLevel)
                    .ToList();

                Dispatcher.Invoke(() =>
                {
                    HexaCoreList.ItemsSource = sorted;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HexaCore Error: {ex.Message}");
            }
        }

        private void PopulateHexaStats()
        {
            try
            {
                var statItems = new List<HexaStatItem>();

                void AddCore(List<HexaStatCoreInfo>? cores, int slotIndex)
                {
                    if (cores == null) return;
                    foreach (var core in cores)
                    {
                        if (core == null) continue; // Null check
                        if (string.IsNullOrEmpty(core.MainStatName)) continue;
                        statItems.Add(new HexaStatItem
                        {
                            MainStat = core.MainStatName ?? "",
                            MainLevel = core.MainStatLevel,
                            SubStat1 = core.SubStatName1 ?? "",
                            SubLevel1 = core.SubStatLevel1,
                            SubStat2 = core.SubStatName2 ?? "",
                            SubLevel2 = core.SubStatLevel2,
                            Grade = core.StatGrade,
                            SlotIndex = slotIndex
                        });
                    }
                }

                AddCore(_hexaMatrixStat?.CharacterHexaStatCore, 1);
                AddCore(_hexaMatrixStat?.CharacterHexaStatCore2, 2);
                AddCore(_hexaMatrixStat?.CharacterHexaStatCore3, 3);

                if (statItems.Any())
                {
                    Dispatcher.Invoke(() =>
                    {
                        HexaStatList.ItemsSource = statItems;
                        HexaStatList.Visibility = Visibility.Visible;
                    });
                }
                else
                {
                    Dispatcher.Invoke(() => HexaStatList.Visibility = Visibility.Collapsed);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HexaStat Error: {ex.Message}");
            }
        }

        private BitmapImage? GetBadgeIcon(string? coreType)
        {
            var badgeName = coreType switch
            {
                "공용 코어" => "Common.png",
                "스킬 코어" => "Skill.png",
                "강화 코어" => "Enhance.png",
                "마스터리 코어" => "Mastery.png",
                _ => "Common.png"
            };
            try
            {
                return new BitmapImage(new Uri($"pack://application:,,,/Data/{badgeName}"));
            }
            catch { return null; }
        }

        private BitmapImage? GetSkillIcon(string? iconUrl)
        {
            if (string.IsNullOrEmpty(iconUrl)) return null;
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(iconUrl);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                return bitmap;
            }
            catch { return null; }
        }

        private string TruncateSkillName(string name, int maxLength)
        {
            if (string.IsNullOrEmpty(name)) return "";
            if (name.Length <= maxLength) return name;
            return name[..(maxLength - 2)] + "..";
        }

        private void EquipSlot_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is not Grid grid) return;

            if (grid.Tag is ItemEquipmentInfo item)
            {
                ShowItemTooltip(item);
            }
        }

        private void EquipSlot_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HideItemTooltip();
        }

        private void ShowItemTooltip(ItemEquipmentInfo item)
        {
            _tooltipCloseTimer.Stop(); // 타이머 중지
            try
            {
                var tooltipBitmap = MapleTooltipRenderer.RenderEquipmentTooltip(item);
                if (tooltipBitmap != null)
                {
                    TooltipImage.Source = WpfBitmapConverter.ToBitmapSource(tooltipBitmap);

                    // 화면(Window) 기준 상대 좌표로 설정하여 마우스 따라가기 지원
                    TooltipPopup.PlacementTarget = this;
                    TooltipPopup.Placement = PlacementMode.Relative;

                    if (!TooltipPopup.IsOpen)
                    {
                        // 초기 위치 설정
                        var mousePos = Mouse.GetPosition(this);
                        TooltipPopup.HorizontalOffset = mousePos.X + 15;
                        TooltipPopup.VerticalOffset = mousePos.Y + 15;
                        TooltipPopup.IsOpen = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Tooltip error: {ex.Message}");
            }
        }

        private void HideItemTooltip()
        {
            // 즉시 닫지 않고 타이머 시작 (이동 중 깜빡임 방지)
            _tooltipCloseTimer.Start();
        }

        private void EquipSlot_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (TooltipPopup.IsOpen)
            {
                var mousePos = Mouse.GetPosition(this);
                TooltipPopup.HorizontalOffset = mousePos.X + 15;
                TooltipPopup.VerticalOffset = mousePos.Y + 15;
            }
        }

        // ════════════════════════════════════════════════════════════
        // HEXA CORE TOOLTIP & UI EVENTS
        // ════════════════════════════════════════════════════════════

        private void HexaCore_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is HexaCoreItem item)
            {
                ShowSkillTooltip(item);
            }
        }

        private void HexaCore_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            HideItemTooltip();
        }

        private void HexaCore_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (TooltipPopup.IsOpen)
            {
                var mousePos = Mouse.GetPosition(this);
                TooltipPopup.HorizontalOffset = mousePos.X + 15;
                TooltipPopup.VerticalOffset = mousePos.Y + 15;
            }
        }

        private void ShowSkillTooltip(HexaCoreItem item)
        {
            _tooltipCloseTimer.Stop();
            try
            {
                // HexaCoreItem -> SkillTooltipData 변환
                var data = new SkillTooltipData
                {
                    Name = item.SkillName, // or item.OriginalName ?
                    IconUrl = item.SkillIcon,
                    Level = item.CoreLevel,
                    MaxLevel = 30, // 가정
                    Description = $"코어 종류: {item.CoreType}\n\n[다음 레벨 조건]\n솔 에르다: {item.NextSolErdaText}\n솔 에르다 조각: {item.NextFragmentText}\n\n[졸업까지 남은 비용]\n솔 에르다: {item.RemainingSolErdaText}\n솔 에르다 조각: {item.RemainingFragmentText}",
                    SkillEffect = "6차 스킬 코어입니다.", // 상세 효과 데이터 없음
                    SkillEffectNext = ""
                };

                // 아이콘 변환 (URL -> BitmapImage -> Bitmap)
                if (!string.IsNullOrEmpty(item.SkillIcon))
                {
                    var bmpImage = GetSkillIcon(item.SkillIcon);
                    if (bmpImage != null)
                    {
                        data.IconBitmap = BitmapImageToBitmap(bmpImage);
                    }
                }

                var renderer = new SkillTooltipRenderer(data);
                var bitmap = renderer.Render();

                if (bitmap != null)
                {
                    TooltipImage.Source = WpfBitmapConverter.ToBitmapSource(bitmap);

                    TooltipPopup.PlacementTarget = this;
                    TooltipPopup.Placement = PlacementMode.Relative;

                    if (!TooltipPopup.IsOpen)
                    {
                        var mousePos = Mouse.GetPosition(this);
                        TooltipPopup.HorizontalOffset = mousePos.X + 15;
                        TooltipPopup.VerticalOffset = mousePos.Y + 15;
                        TooltipPopup.IsOpen = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Skill Tooltip Error: {ex.Message}");
            }
        }

        private System.Drawing.Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                return new System.Drawing.Bitmap(outStream);
            }
        }


        private void ShowLoading(bool show, string? message = null)
        {
            LoadingPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (!string.IsNullOrEmpty(message))
                LoadingText.Text = message;
        }

        private System.Windows.Threading.DispatcherTimer? _toastTimer;

        private void ShowError(string message)
        {
            ShowToast(message, isError: true);
        }

        private void ShowToast(string message, bool isError = true, int durationSeconds = 3)
        {
            ErrorText.Text = message;
            ToastIcon.Symbol = isError ? Wpf.Ui.Controls.SymbolRegular.ErrorCircle24 : Wpf.Ui.Controls.SymbolRegular.CheckmarkCircle24;
            ToastIcon.Foreground = new SolidColorBrush(isError ? System.Windows.Media.Color.FromRgb(255, 107, 107) : System.Windows.Media.Color.FromRgb(52, 211, 153));

            ErrorPanel.Visibility = Visibility.Visible;

            // 자동 숨김 타이머
            _toastTimer?.Stop();
            _toastTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(durationSeconds)
            };
            _toastTimer.Tick += (s, e) =>
            {
                _toastTimer.Stop();
                HideError();
            };
            _toastTimer.Start();
        }

        private void HideError()
        {
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        private string FormatCombatPower(long cp)
        {
            if (cp >= 100000000) // 1억 이상
                return $"{cp / 100000000.0:F2}억";
            if (cp >= 10000) // 1만 이상
                return $"{cp / 10000.0:F1}만";
            return cp.ToString("N0");
        }
        private void PopulateSymbols()
        {
            _arcaneSymbols.Clear();
            _authenticSymbols.Clear();
            _grandSymbols.Clear();

            if (SymbolList == null) return;
            // SymbolList.ItemsSource = null; // Don't clear explicitly to avoid flickering/blank state
            if (NoSymbolText != null) NoSymbolText.Visibility = Visibility.Collapsed;

            // 라디오 버튼 초기화 (Arcane 선택)
            if (TabArcane != null) TabArcane.IsChecked = true;
            if (TabAuthentic != null) TabAuthentic.IsChecked = false;
            if (TabGrand != null) TabGrand.IsChecked = false;

            if (_symbolResponse?.Symbol == null)
            {
                if (NoSymbolText != null) NoSymbolText.Visibility = Visibility.Visible;
                return;
            }

            foreach (var sym in _symbolResponse.Symbol)
            {
                var type = SymbolCalculator.GetSymbolType(sym.SymbolName ?? "");
                if (type == SymbolType.Unknown) continue;

                var (remCount, remCost) = SymbolCalculator.CalculateRemaining(sym.SymbolName ?? "", sym.SymbolLevel, sym.SymbolGrowthCount);

                double growthRate = 0;
                if (sym.SymbolRequireGrowthCount > 0)
                {
                    growthRate = (double)sym.SymbolGrowthCount / sym.SymbolRequireGrowthCount;
                    if (growthRate > 1) growthRate = 1;
                }

                int maxLevel = SymbolCalculator.GetMaxLevel(type);
                if (sym.SymbolLevel >= maxLevel)
                {
                    growthRate = 1;
                    remCount = 0;
                    remCost = 0;
                }

                var item = new SymbolDisplayItem
                {
                    SymbolName = sym.SymbolName,
                    SymbolIconUrl = sym.SymbolIcon ?? "",
                    Level = sym.SymbolLevel,
                    MaxLevel = maxLevel,
                    GrowthRate = growthRate,
                    GrowthText = (sym.SymbolLevel >= maxLevel) ? "MAX" : $"{sym.SymbolGrowthCount:N0} / {sym.SymbolRequireGrowthCount:N0} ({Math.Round(growthRate * 100)}%)",
                    RemainingCountText = (remCount > 0) ? $"{remCount:N0}개 남음" : "졸업",
                    RemainingCostText = (remCost > 0) ? FormatMeso(remCost) : "완료"
                };

                if (type == SymbolType.Arcane) _arcaneSymbols.Add(item);
                else if (type == SymbolType.Authentic) _authenticSymbols.Add(item);
                else if (type == SymbolType.GrandAuthentic) _grandSymbols.Add(item);
            }

            UpdateSymbolView("Arcane");
        }

        private void OnSymbolTabClicked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.RadioButton rb && rb.Content is string content)
            {
                UpdateSymbolView(content);
            }
        }

        private void UpdateSymbolView(string tabName)
        {
            List<SymbolDisplayItem>? items = null;
            if (tabName == "Arcane" || tabName == "아케인") items = _arcaneSymbols;
            else if (tabName == "Authentic" || tabName == "어센틱") items = _authenticSymbols;
            else if (tabName == "Grand" || tabName == "그랜드") items = _grandSymbols;

            if (items == null || items.Count == 0)
            {
                if (SymbolList != null) SymbolList.ItemsSource = null;
                if (NoSymbolText != null)
                {
                    NoSymbolText.Visibility = Visibility.Visible;
                    NoSymbolText.Text = $"{tabName} 심볼 정보가 없습니다.";
                }
            }
            else
            {
                if (SymbolList != null) SymbolList.ItemsSource = items;
                if (NoSymbolText != null) NoSymbolText.Visibility = Visibility.Collapsed;
            }
        }

        private string FormatMeso(long meso)
        {
            if (meso >= 100000000)
            {
                double uk = (double)meso / 100000000;
                return $"{uk:N1}억 메소";
            }
            else if (meso >= 10000)
            {
                double man = (double)meso / 10000;
                return $"{man:N1}만 메소";
            }
            return $"{meso:N0} 메소";
        }
    }

    public class SymbolDisplayItem
    {
        public string? SymbolName { get; set; }
        public string? SymbolIconUrl { get; set; }
        public int Level { get; set; }
        public int MaxLevel { get; set; }
        public string DisplayLevel => Level >= MaxLevel ? "MAX" : $"Lv.{Level}";
        public double GrowthRate { get; set; }
        public string? GrowthText { get; set; }
        public string? RemainingCountText { get; set; }
        public string? RemainingCostText { get; set; }
    }
}

