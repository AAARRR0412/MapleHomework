using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MapleHomework.Models;
using MapleHomework.ViewModels;
using Wpf.Ui.Controls;

namespace MapleHomework
{
    public partial class DashboardWindow : FluentWindow
    {
        private MainViewModel _viewModel;

        public ObservableCollection<CharacterProfile> Characters { get; set; }

        public DashboardWindow(AppData appData, MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            Characters = new ObservableCollection<CharacterProfile>(appData.Characters);
            this.DataContext = this;
        }

        public int CharacterCount => Characters.Count;

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
    }
}
