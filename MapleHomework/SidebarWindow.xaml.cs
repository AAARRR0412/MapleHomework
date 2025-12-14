using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MapleHomework.Models;
using MapleHomework.ViewModels;

namespace MapleHomework
{
    public partial class SidebarWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly Action<CharacterProfile>? _onCharacterSelected;

        public SidebarWindow(MainViewModel viewModel, Action<CharacterProfile>? onCharacterSelected = null)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _onCharacterSelected = onCharacterSelected;
            
            CharacterList.ItemsSource = viewModel.Characters;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            HideWithAnimation();
        }

        private void CharacterItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is CharacterProfile character)
            {
                _onCharacterSelected?.Invoke(character);
            }
        }

        /// <summary>
        /// 애니메이션과 함께 표시
        /// </summary>
        public void ShowWithAnimation()
        {
            // 캐릭터 목록 새로고침
            CharacterList.ItemsSource = null;
            CharacterList.ItemsSource = _viewModel.Characters;
            
            this.Opacity = 0;
            this.Show();
            
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
            fadeIn.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            this.BeginAnimation(OpacityProperty, fadeIn);
        }

        /// <summary>
        /// 애니메이션과 함께 숨김
        /// </summary>
        public void HideWithAnimation()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            fadeOut.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn };
            fadeOut.Completed += (s, e) => this.Hide();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }

        /// <summary>
        /// 메인 창에 맞춰 위치 업데이트
        /// </summary>
        public void UpdatePosition(double mainLeft, double mainTop, double mainHeight)
        {
            this.Left = mainLeft - this.Width + 10; // 메인 창 왼쪽에 붙음 (10px 겹침)
            this.Top = mainTop;
            this.Height = mainHeight;
        }
    }
}

