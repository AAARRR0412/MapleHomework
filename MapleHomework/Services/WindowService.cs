using System;
using System.Windows;
using MapleHomework.Models;
using MapleHomework.ViewModels;

namespace MapleHomework.Services
{
    public class WindowService : IWindowService
    {
        private DashboardWindow? _dashboardWindow;
        private BossRewardWindow? _bossRewardWindow;
        private ReportWindow? _reportWindow;

        public void OpenDashboard(AppData appData, MainViewModel mainViewModel)
        {
            if (_dashboardWindow != null && _dashboardWindow.IsLoaded)
            {
                if (_dashboardWindow.WindowState == WindowState.Minimized)
                {
                    _dashboardWindow.WindowState = WindowState.Normal;
                }
                _dashboardWindow.Activate();
                _dashboardWindow.Focus();
            }
            else
            {
                _dashboardWindow = new DashboardWindow(appData, mainViewModel);
                _dashboardWindow.Closed += (s, e) => _dashboardWindow = null;
                _dashboardWindow.Show();
            }
        }

        public void OpenBossReward(AppData appData, MainViewModel mainViewModel)
        {
            if (_bossRewardWindow != null && _bossRewardWindow.IsLoaded)
            {
                if (_bossRewardWindow.WindowState == WindowState.Minimized)
                {
                    _bossRewardWindow.WindowState = WindowState.Normal;
                }
                _bossRewardWindow.Activate();
                _bossRewardWindow.Focus();
            }
            else
            {
                _bossRewardWindow = new BossRewardWindow(appData, mainViewModel);
                _bossRewardWindow.Closed += (s, e) => _bossRewardWindow = null;
                _bossRewardWindow.Show();
            }
        }

        public void OpenReport(MainViewModel mainViewModel)
        {
            if (_reportWindow != null && _reportWindow.IsLoaded)
            {
                if (_reportWindow.WindowState == WindowState.Minimized)
                    _reportWindow.WindowState = WindowState.Normal;
                _reportWindow.Activate();
                _reportWindow.Topmost = true;
                _reportWindow.Topmost = false;
                _reportWindow.Focus();
            }
            else
            {
                _reportWindow = new ReportWindow(mainViewModel)
                {
                    Owner = System.Windows.Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                _reportWindow.Closed += (s, e) => _reportWindow = null;
                _reportWindow.Show();
                _reportWindow.Activate();
            }
        }

        public void CloseAll()
        {
            _dashboardWindow?.Close();
            _bossRewardWindow?.Close();
            _reportWindow?.Close();
        }
    }
}
