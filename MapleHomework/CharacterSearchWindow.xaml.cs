using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MapleHomework.Data;
using MapleHomework.Models;
using MapleHomework.Services;
using MapleHomework.CharaSimResource;
using WpfImage = System.Windows.Controls.Image;

namespace MapleHomework
{
    public partial class CharacterSearchWindow : Window
    {
        private readonly MapleApiService _apiService;
        private ItemEquipmentResponse? _itemEquipment;
        private HexaStatResponse? _hexaMatrix;
        private HexaMatrixStatResponse? _hexaMatrixStat;
        private CharacterSkillResponse? _characterSkill;
        private readonly Dictionary<string, Grid> _equipSlots = new();

        // 헥사코어 아이템 클래스 (ReportWindow와 동일)
        private class HexaCoreItem
        {
            public string SkillName { get; set; } = "";
            public string OriginalName { get; set; } = "";
            public string CoreType { get; set; } = "";
            public int CoreLevel { get; set; }
            public string SkillIconUrl { get; set; } = "";
            public string BadgeIconPath { get; set; } = "";
        }

        // 경험치 그래프 아이템
        private class ExpGraphItem
        {
            public string DateLabel { get; set; } = "";
            public double ExpRate { get; set; }
            public string ExpRateText { get; set; } = "";
            public double GraphRate { get; set; }
            public long ExpGain { get; set; }
        }

        // 헥사스텟 아이템
        private class HexaStatItem
        {
            public string MainStat { get; set; } = "";
            public int MainLevel { get; set; }
            public string SubStat1 { get; set; } = "";
            public int SubLevel1 { get; set; }
            public string SubStat2 { get; set; } = "";
            public int SubLevel2 { get; set; }
            public int Grade { get; set; }
            public int SlotIndex { get; set; }
        }

        private int _currentLevel;
        private int _currentPreset = 1;
        private bool _isDarkTheme = true;

        public CharacterSearchWindow()
        {
            InitializeComponent();
            _apiService = new MapleApiService();

            // 현재 테마 상태 확인 및 적용
            var settings = ConfigManager.Load();
            _isDarkTheme = ThemeService.ShouldUseDarkTheme(settings);
            ApplyTheme(_isDarkTheme);

            // 장비 슬롯 매핑
            InitializeEquipSlots();
            
            // 프리셋 UI 초기화
            UpdatePresetUI();
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
            // 뱃지 슬롯 삭제됨
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
            var accentBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x50, 0x55, 0xBB, 0xEE));
            var normalBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x25, 0xFF, 0xFF, 0xFF));
            var accentBorder = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x55, 0xBB, 0xEE));
            var normalBorder = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x35, 0xFF, 0xFF, 0xFF));
            var whiteText = System.Windows.Media.Brushes.White;
            var mutedText = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xAA, 0xFF, 0xFF, 0xFF));

            Preset1Border.Background = _currentPreset == 1 ? accentBrush : normalBrush;
            Preset1Border.BorderBrush = _currentPreset == 1 ? accentBorder : normalBorder;
            ((System.Windows.Controls.TextBlock)Preset1Border.Child).Foreground = 
                _currentPreset == 1 ? whiteText : mutedText;

            Preset2Border.Background = _currentPreset == 2 ? accentBrush : normalBrush;
            Preset2Border.BorderBrush = _currentPreset == 2 ? accentBorder : normalBorder;
            ((System.Windows.Controls.TextBlock)Preset2Border.Child).Foreground = 
                _currentPreset == 2 ? whiteText : mutedText;

            Preset3Border.Background = _currentPreset == 3 ? accentBrush : normalBrush;
            Preset3Border.BorderBrush = _currentPreset == 3 ? accentBorder : normalBorder;
            ((System.Windows.Controls.TextBlock)Preset3Border.Child).Foreground = 
                _currentPreset == 3 ? whiteText : mutedText;
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
            // 공통 색상 정의
            var labelDark = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF));
            var labelLight = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x64, 0x74, 0x8B));
            var textDark = System.Windows.Media.Brushes.White;
            var textLight = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1E, 0x29, 0x3B));
            var subTextLight = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x47, 0x55, 0x69));
            
            if (isDark)
            {
                // 다크 테마 배경
                var darkGradient = new LinearGradientBrush
                {
                    StartPoint = new System.Windows.Point(0, 0),
                    EndPoint = new System.Windows.Point(0, 1)
                };
                darkGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(0x4A, 0x55, 0x68), 0));
                darkGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(0x2D, 0x37, 0x48), 0.3));
                darkGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(0x1A, 0x20, 0x2C), 1));
                MainContainer.Background = darkGradient;
                
                // 다크 테마 타이틀바
                var titleBarGradient = new LinearGradientBrush
                {
                    StartPoint = new System.Windows.Point(0, 0),
                    EndPoint = new System.Windows.Point(0, 1)
                };
                titleBarGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(0x5D, 0x6C, 0x7A), 0));
                titleBarGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(0x4A, 0x55, 0x68), 1));
                TitleBar.Background = titleBarGradient;
                TitleText.Foreground = textDark;
                
                // 다크 테마 검색창
                SearchCard.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x25, 0xFF, 0xFF, 0xFF));
                SearchCard.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF));
                SearchTextBox.Foreground = textDark;
                SearchTextBox.CaretBrush = textDark;
                
                // 다크 테마 카드 색상
                var cardBg = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x12, 0xFF, 0xFF, 0xFF));
                var cardBorder = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF));
                
                ProfileCard.Background = cardBg;
                ProfileCard.BorderBrush = cardBorder;
                ExpCard.Background = cardBg;
                ExpCard.BorderBrush = cardBorder;
                EquipmentCard.Background = cardBg;
                EquipmentCard.BorderBrush = cardBorder;
                HexaCoreCard.Background = cardBg;
                HexaCoreCard.BorderBrush = cardBorder;
                
                // 다크 테마 텍스트 색상
                CharacterName.Foreground = textDark;
                CharacterClass.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xAA, 0xFF, 0xFF, 0xFF));
                LoadingText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xAA, 0xFF, 0xFF, 0xFF));
                
                // 다크 테마 라벨 색상
                LabelCombatPower.Foreground = labelDark;
                LabelUnion.Foreground = labelDark;
                LabelWorld.Foreground = labelDark;
                LabelGuild.Foreground = labelDark;
                LabelAvgExp.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xAA, 0xFF, 0xFF, 0xFF));
                AvgExpAmount.Foreground = labelDark;
                
                // 다크 테마 추가 요소
                PresetsLabel.Foreground = labelDark;
                ExpLoadingText2.Foreground = labelDark;
                NoHexaText.Foreground = labelDark;
                EquipmentHeaderText.Foreground = textDark;
            }
            else
            {
                // 라이트 테마 배경 - 세련된 그라데이션
                var lightGradient = new LinearGradientBrush
                {
                    StartPoint = new System.Windows.Point(0, 0),
                    EndPoint = new System.Windows.Point(0, 1)
                };
                lightGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(0xF8, 0xFA, 0xFC), 0));
                lightGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(0xF1, 0xF5, 0xF9), 0.5));
                lightGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(0xE2, 0xE8, 0xF0), 1));
                MainContainer.Background = lightGradient;
                
                // 라이트 테마 타이틀바
                var titleBarGradient = new LinearGradientBrush
                {
                    StartPoint = new System.Windows.Point(0, 0),
                    EndPoint = new System.Windows.Point(0, 1)
                };
                titleBarGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF), 0));
                titleBarGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(0xF8, 0xFA, 0xFC), 1));
                TitleBar.Background = titleBarGradient;
                TitleText.Foreground = textLight;
                
                // 라이트 테마 검색창
                SearchCard.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
                SearchCard.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE2, 0xE8, 0xF0));
                SearchTextBox.Foreground = textLight;
                SearchTextBox.CaretBrush = textLight;
                
                // 라이트 테마 카드 색상
                var cardBg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
                var cardBorder = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE2, 0xE8, 0xF0));
                
                ProfileCard.Background = cardBg;
                ProfileCard.BorderBrush = cardBorder;
                ExpCard.Background = cardBg;
                ExpCard.BorderBrush = cardBorder;
                EquipmentCard.Background = cardBg;
                EquipmentCard.BorderBrush = cardBorder;
                HexaCoreCard.Background = cardBg;
                HexaCoreCard.BorderBrush = cardBorder;
                
                // 라이트 테마 텍스트 색상 - 가독성 좋은 다크 컬러
                CharacterName.Foreground = textLight;
                CharacterClass.Foreground = subTextLight;
                LoadingText.Foreground = subTextLight;
                
                // 라이트 테마 라벨 색상
                LabelCombatPower.Foreground = labelLight;
                LabelUnion.Foreground = labelLight;
                LabelWorld.Foreground = labelLight;
                LabelGuild.Foreground = labelLight;
                LabelAvgExp.Foreground = subTextLight;
                AvgExpAmount.Foreground = labelLight;
                
                // 라이트 테마 추가 요소
                PresetsLabel.Foreground = labelLight;
                ExpLoadingText2.Foreground = labelLight;
                NoHexaText.Foreground = labelLight;
                EquipmentHeaderText.Foreground = textLight;
            }

            // 전역 테마 서비스도 업데이트
            ThemeService.ApplyTheme(isDark);
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

        private void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                _ = SearchCharacterAsync();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _ = SearchCharacterAsync();
        }

        private async Task SearchCharacterAsync()
        {
            var nickname = SearchTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(nickname))
            {
                ShowError("닉네임을 입력해주세요.");
                return;
            }

            ShowLoading(true, "캐릭터 정보를 불러오는 중...");
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

                // 2. 기본 정보 조회
                ShowLoading(true, "기본 정보 조회 중...");
                var basic = await _apiService.GetCharacterInfoAsync(ocid);
                if (basic == null)
                {
                    ShowError("캐릭터 기본 정보를 가져올 수 없습니다.");
                    ShowLoading(false);
                    return;
                }

                // 기본 정보 표시
                CharacterName.Text = basic.CharacterName ?? nickname;
                CharacterLevel.Text = basic.CharacterLevel.ToString();
                CharacterClass.Text = basic.CharacterClass ?? "-";
                WorldText.Text = basic.WorldName ?? "-";
                GuildText.Text = basic.CharacterGuildName ?? "-";
                _currentLevel = basic.CharacterLevel;

                // 캐릭터 이미지
                if (!string.IsNullOrEmpty(basic.CharacterImage))
                {
                    CharacterImage.Source = new BitmapImage(new Uri(basic.CharacterImage));
                }

                // 3. 스탯 조회 (전투력)
                ShowLoading(true, "전투력 조회 중...");
                var stat = await _apiService.GetCharacterStatAsync(ocid);
                if (stat?.FinalStat != null)
                {
                    var cpStat = stat.FinalStat.Find(s => s.StatName == "전투력");
                    if (cpStat != null && long.TryParse(cpStat.StatValue, out long cp))
                    {
                        CombatPowerText.Text = FormatCombatPower(cp);
                    }
                }

                // 4. 유니온 조회
                ShowLoading(true, "유니온 정보 조회 중...");
                var union = await _apiService.GetUnionInfoAsync(ocid);
                if (union != null)
                {
                    UnionLevelText.Text = union.UnionLevel.ToString("N0");
                }

                // 5. 장비 조회
                ShowLoading(true, "장비 정보 조회 중...");
                _itemEquipment = await _apiService.GetItemEquipmentAsync(ocid);
                PopulateEquipmentSlots();

                // 6. 스킬 정보 조회 (헥사 코어 아이콘용)
                ShowLoading(true, "스킬 정보 조회 중...");
                _characterSkill = await _apiService.GetCharacterSkillAsync(ocid);
                
                // 7. 헥사 코어 조회
                ShowLoading(true, "HEXA 코어 조회 중...");
                _hexaMatrix = await _apiService.GetHexaStatAsync(ocid);
                _hexaMatrixStat = await _apiService.GetHexaMatrixStatAsync(ocid);
                PopulateHexaCores();
                PopulateHexaStats();

                // 먼저 화면을 표시하고 경험치는 비동기로 로딩
                ShowLoading(false);
                ResultPanel.Visibility = Visibility.Visible;
                
                // 경험치 로딩 상태 표시
                ExpLoadingPanel.Visibility = Visibility.Visible;
                ExpContentPanel.Visibility = Visibility.Collapsed;
                
                // 7. 경험치 추이 (7일) - 비동기로 별도 로딩
                _ = LoadExpTrendAsync(ocid);
            }
            catch (Exception ex)
            {
                ShowError($"오류가 발생했습니다: {ex.Message}");
                ShowLoading(false);
            }
        }

        private async Task LoadExpTrendAsync(string ocid)
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
                AvgExpAmount.Text = $"({ExpTable.FormatExpKorean(avgDailyExp)})";
            }
            else
            {
                AvgExpText.Text = "-";
                AvgExpAmount.Text = "";
            }
            
            // 로딩 완료 - UI 전환
            ExpLoadingPanel.Visibility = Visibility.Collapsed;
            ExpContentPanel.Visibility = Visibility.Visible;
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
                    var image = new WpfImage
                    {
                        Source = new BitmapImage(new Uri(item.ItemIcon)),
                        Stretch = Stretch.None, // 원본 크기 유지
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center
                    };
                    RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
                    targetSlot.Children.Add(image);
                    targetSlot.Tag = item; // 아이템 데이터 저장
                }
            }
        }

        private void PopulateHexaCores()
        {
            if (_hexaMatrix?.HexaCoreEquipment == null || !_hexaMatrix.HexaCoreEquipment.Any())
            {
                HexaCoreList.ItemsSource = null;
                NoHexaText.Visibility = Visibility.Visible;
                return;
            }

            NoHexaText.Visibility = Visibility.Collapsed;

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
            var savedSkill = RawDataProcessor.LoadLatestSkill6Info();
            if (savedSkill?.CharacterSkill != null)
            {
                foreach (var skill in savedSkill.CharacterSkill.Where(s => !string.IsNullOrEmpty(s.SkillName) && !string.IsNullOrEmpty(s.SkillIcon)))
                {
                    skillIconMap.TryAdd(skill.SkillName!, skill.SkillIcon!);
                }
            }

            string ResolveIcon(string coreName, List<LinkedSkillInfo>? linked)
            {
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
                "마스터리 코어" => "Data/Mastery.png",
                "강화 코어" => "Data/Enhance.png",
                "공용 코어" => "Data/Common.png",
                _ => "Data/Skill.png"
            };

            var coreItems = new List<HexaCoreItem>();
            string[] typeOrder = { "마스터리 코어", "스킬 코어", "강화 코어", "공용 코어" };

            foreach (var core in _hexaMatrix.HexaCoreEquipment)
            {
                var item = new HexaCoreItem
                {
                    OriginalName = core.HexaCoreName ?? "",
                    SkillName = TruncateSkillName(core.HexaCoreName ?? "", 8),
                    CoreType = core.HexaCoreType ?? "",
                    CoreLevel = core.HexaCoreLevel,
                    BadgeIconPath = GetBadgePath(core.HexaCoreType),
                    SkillIconUrl = ResolveIcon(core.HexaCoreName ?? "", core.LinkedSkill)
                };
                coreItems.Add(item);
            }

            // 정렬
            var sorted = coreItems
                .OrderBy(c => Array.IndexOf(typeOrder, c.CoreType) >= 0 ? Array.IndexOf(typeOrder, c.CoreType) : 999)
                .ThenByDescending(c => c.CoreLevel)
                .ToList();

            HexaCoreList.ItemsSource = sorted;
        }

        private void PopulateHexaStats()
        {
            var statItems = new List<HexaStatItem>();

            void AddCore(List<HexaStatCoreInfo>? cores, int slotIndex)
            {
                if (cores == null) return;
                foreach (var core in cores)
                {
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
                HexaStatList.ItemsSource = statItems;
                HexaStatList.Visibility = Visibility.Visible;
            }
            else
            {
                HexaStatList.Visibility = Visibility.Collapsed;
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
            try
            {
                var tooltipBitmap = MapleTooltipRenderer.RenderEquipmentTooltip(item);
                if (tooltipBitmap != null)
                {
                    TooltipImage.Source = MapleHomework.Rendering.Core.WpfBitmapConverter.ToBitmapSource(tooltipBitmap);
                    
                    // Popup을 마우스 위치에 표시 (창 밖으로도 나감)
                    TooltipPopup.HorizontalOffset = 20;
                    TooltipPopup.VerticalOffset = 10;
                    TooltipPopup.IsOpen = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Tooltip error: {ex.Message}");
            }
        }

        private void HideItemTooltip()
        {
            TooltipPopup.IsOpen = false;
        }

        private void ShowLoading(bool show, string? message = null)
        {
            LoadingPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (!string.IsNullOrEmpty(message))
                LoadingText.Text = message;
        }

        private void ShowError(string message)
        {
            ErrorPanel.Visibility = Visibility.Visible;
            ErrorText.Text = message;
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
    }
}

