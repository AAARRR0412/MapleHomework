using MapleHomework.Models;
using MapleHomework.ViewModels;

namespace MapleHomework.Services
{
    public interface IWindowService
    {
        void OpenDashboard(AppData appData, MainViewModel mainViewModel);
        void OpenBossReward(AppData appData, MainViewModel mainViewModel);
        void OpenReport(MainViewModel mainViewModel);
        void CloseAll();
    }
}
