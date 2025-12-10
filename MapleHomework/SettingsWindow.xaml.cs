using System.Windows;
using System.Windows.Input;
using MapleHomework.Models;
using MapleHomework.Services;
using MapleHomework.ViewModels;

using MessageBox = System.Windows.MessageBox;

namespace MapleHomework
{
    public partial class SettingsWindow : Window
    {
        private MainViewModel _viewModel;
        private bool _isInitializing = true;

        public SettingsWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;

            // 현재 선택된 캐릭터의 닉네임 표시
            var appData = CharacterRepository.Load();
            TxtApiKey.Text = appData.ApiKey;
            
            // 현재 선택된 캐릭터의 닉네임
            if (_viewModel.SelectedCharacter != null)
            {
                TxtNickname.Text = _viewModel.SelectedCharacter.Nickname;
            }
            
            // 자동 시작 상태 로드
            AutoStartToggle.IsChecked = ConfigManager.IsAutoStartEnabled();
            
            _isInitializing = false;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AutoStartToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            bool isEnabled = AutoStartToggle.IsChecked == true;
            ConfigManager.SetAutoStart(isEnabled);

            // AppData에도 저장
            var appData = CharacterRepository.Load();
            appData.AutoStartEnabled = isEnabled;
            CharacterRepository.Save(appData);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = TxtApiKey.Text.Trim();
            string nickname = TxtNickname.Text.Trim();

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(nickname))
            {
                MessageBox.Show("API 키와 닉네임을 모두 입력해주세요.", "오류");
                return;
            }

            // AppData 업데이트
            var appData = CharacterRepository.Load();
            appData.ApiKey = apiKey;
            appData.AutoStartEnabled = AutoStartToggle.IsChecked == true;

            // 현재 선택된 캐릭터의 닉네임 업데이트
            if (_viewModel.SelectedCharacter != null)
            {
                _viewModel.SelectedCharacter.Nickname = nickname;
            }

            CharacterRepository.Save(appData);

            // API에서 캐릭터 정보 로드
            await _viewModel.LoadCharacterDataFromApi(apiKey, nickname);
            
            MessageBox.Show("성공적으로 저장되었습니다!", "알림");
            this.Close();
        }
    }
}
