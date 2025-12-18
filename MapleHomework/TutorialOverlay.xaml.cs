using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Wpf.Ui.Controls;

namespace MapleHomework
{
    public partial class TutorialOverlay : Window
    {
        private int _currentStep = 1;
        private const int TotalSteps = 6;

        private readonly Grid[] _stepPanels;
        private readonly Ellipse[] _dots;

        public TutorialOverlay()
        {
            InitializeComponent();

            _stepPanels = new Grid[] { Step1Panel, Step2Panel, Step3Panel, Step4Panel, Step5Panel, Step6Panel };
            _dots = new Ellipse[] { Dot1, Dot2, Dot3, Dot4, Dot5, Dot6 };

            UpdateUI();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Owner != null)
            {
                // 메인 윈도우의 중앙에 위치 계산
                double ownerCenterX = Owner.Left + (Owner.Width / 2);
                double ownerCenterY = Owner.Top + (Owner.Height / 2);

                this.Left = ownerCenterX - (this.Width / 2);
                this.Top = ownerCenterY - (this.Height / 2);
            }
            else
            {
                // Owner가 없으면 화면 중앙
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                var screenHeight = SystemParameters.PrimaryScreenHeight;
                this.Left = (screenWidth - this.Width) / 2;
                this.Top = (screenHeight - this.Height) / 2;
            }
        }

        private void UpdateUI()
        {
            // 모든 패널 숨기기
            foreach (var panel in _stepPanels)
            {
                panel.Visibility = Visibility.Collapsed;
            }

            // 현재 스텝 패널만 표시
            if (_currentStep >= 1 && _currentStep <= TotalSteps)
            {
                _stepPanels[_currentStep - 1].Visibility = Visibility.Visible;
            }

            // 인디케이터 업데이트
            for (int i = 0; i < _dots.Length; i++)
            {
                _dots[i].Fill = (i == _currentStep - 1)
                    ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x5A, 0xC8, 0xFA))
                    : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3D, 0x4A, 0x5C));
            }

            // 버튼 텍스트 업데이트
            if (_currentStep == TotalSteps)
            {
                NextButtonText.Text = "시작하기";
                NextButtonIcon.Symbol = Wpf.Ui.Controls.SymbolRegular.Checkmark24;
            }
            else
            {
                NextButtonText.Text = "다음";
                NextButtonIcon.Symbol = Wpf.Ui.Controls.SymbolRegular.ChevronRight24;
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep < TotalSteps)
            {
                _currentStep++;
                UpdateUI();
            }
            else
            {
                // 튜토리얼 완료
                CompleteTutorial();
            }
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            CompleteTutorial();
        }

        private void CompleteTutorial()
        {
            // 설정에 튜토리얼 완료 저장
            var settings = Models.ConfigManager.Load();
            settings.HasSeenTutorial = true;
            Models.ConfigManager.Save(settings);

            DialogResult = true;
            Close();
        }
    }
}
