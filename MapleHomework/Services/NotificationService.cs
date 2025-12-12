using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using MapleHomework.Models;
using Forms = System.Windows.Forms;

namespace MapleHomework.Services
{
    /// <summary>
    /// 숙제 알림 서비스
    /// </summary>
    public class NotificationService
    {
        private readonly DispatcherTimer _timer;
        private readonly Forms.NotifyIcon _notifyIcon;
        
        // 카테고리별 마지막 알림 시간 추적
        private DateTime _lastDailyNotification = DateTime.MinValue;
        private DateTime _lastWeeklyNotification = DateTime.MinValue;
        private DateTime _lastBossNotification = DateTime.MinValue;

        public NotificationService(Forms.NotifyIcon notifyIcon)
        {
            _notifyIcon = notifyIcon;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1) // 1분마다 체크
            };
            _timer.Tick += CheckAndNotify;
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private void CheckAndNotify(object? sender, EventArgs e)
        {
            var settings = ConfigManager.Load();

            // 알림이 비활성화되어 있으면 스킵
            if (!settings.IsNotificationEnabled) return;

            var now = DateTime.Now;
            var pendingTasks = new List<(string CharacterName, string TaskName, TaskCategory Category)>();
            var appData = CharacterRepository.Load();

            // 일일 알림 체크 (자정 n시간 전)
            if (settings.NotifyDailyTasks && ShouldNotifyDaily(now, settings))
            {
                pendingTasks.AddRange(GetDailyPendingTasks(appData));
                if (pendingTasks.Any(t => t.Category == TaskCategory.Daily))
                {
                    _lastDailyNotification = now;
                }
            }

            // 주간 알림 체크 (목요일 n일 전)
            if (settings.NotifyWeeklyTasks && ShouldNotifyWeekly(now, settings))
            {
                pendingTasks.AddRange(GetWeeklyPendingTasks(appData));
                if (pendingTasks.Any(t => t.Category == TaskCategory.Weekly))
                {
                    _lastWeeklyNotification = now;
                }
            }

            // 보스 알림 체크 (목요일 n일 전)
            if (settings.NotifyBossTasks && ShouldNotifyBoss(now, settings))
            {
                pendingTasks.AddRange(GetBossPendingTasks(appData));
                if (pendingTasks.Any(t => t.Category == TaskCategory.Boss))
                {
                    _lastBossNotification = now;
                }
            }

            if (pendingTasks.Any())
            {
                ShowNotification(pendingTasks);
            }
        }

        /// <summary>
        /// 일일 알림 조건: 자정 n시간 전이고, 오늘 아직 알림 안 보냄
        /// </summary>
        private bool ShouldNotifyDaily(DateTime now, AppSettings settings)
        {
            // 이미 오늘 알림을 보냈으면 스킵
            if (_lastDailyNotification.Date == now.Date) return false;

            // 자정 n시간 전 = 24 - n시
            int notifyHour = 24 - settings.DailyNotifyHoursBefore;
            return now.Hour >= notifyHour;
        }

        /// <summary>
        /// 주간 알림 조건: 목요일 n일 전이고, 해당 주간에 아직 알림 안 보냄
        /// </summary>
        private bool ShouldNotifyWeekly(DateTime now, AppSettings settings)
        {
            // 이번 주 목요일 계산
            var thisThursday = GetNextThursday(now);
            var notifyDate = thisThursday.AddDays(-settings.WeeklyNotifyDaysBefore);

            // 알림 날짜가 지났고, 이번 주간 사이클에서 아직 알림 안 보냄
            if (now.Date >= notifyDate.Date)
            {
                var lastThursday = thisThursday.AddDays(-7);
                return _lastWeeklyNotification < lastThursday;
            }
            return false;
        }

        /// <summary>
        /// 보스 알림 조건: 목요일 n일 전이고, 해당 주간에 아직 알림 안 보냄
        /// </summary>
        private bool ShouldNotifyBoss(DateTime now, AppSettings settings)
        {
            var thisThursday = GetNextThursday(now);
            var notifyDate = thisThursday.AddDays(-settings.BossNotifyDaysBefore);

            if (now.Date >= notifyDate.Date)
            {
                var lastThursday = thisThursday.AddDays(-7);
                return _lastBossNotification < lastThursday;
            }
            return false;
        }

        private DateTime GetNextThursday(DateTime from)
        {
            var date = from.Date;
            while (date.DayOfWeek != DayOfWeek.Thursday)
            {
                date = date.AddDays(1);
            }
            return date;
        }

        private List<(string CharacterName, string TaskName, TaskCategory Category)> GetDailyPendingTasks(AppData appData)
        {
            var result = new List<(string, string, TaskCategory)>();
            foreach (var character in appData.Characters)
            {
                foreach (var task in character.DailyTasks.Where(t => t.IsActive && !t.IsChecked && t.IsFavorite))
                {
                    result.Add((character.Nickname, task.Name, TaskCategory.Daily));
                }
            }
            return result;
        }

        private List<(string CharacterName, string TaskName, TaskCategory Category)> GetWeeklyPendingTasks(AppData appData)
        {
            var result = new List<(string, string, TaskCategory)>();
            foreach (var character in appData.Characters)
            {
                foreach (var task in character.WeeklyTasks.Where(t => t.IsActive && !t.IsChecked && t.IsFavorite))
                {
                    result.Add((character.Nickname, task.Name, TaskCategory.Weekly));
                }
            }
            return result;
        }

        private List<(string CharacterName, string TaskName, TaskCategory Category)> GetBossPendingTasks(AppData appData)
        {
            var result = new List<(string, string, TaskCategory)>();
            foreach (var character in appData.Characters)
            {
                foreach (var task in character.BossTasks.Where(t => t.IsActive && !t.IsChecked && t.IsFavorite))
                {
                    result.Add((character.Nickname, task.Name, TaskCategory.Boss));
                }
            }
            return result;
        }

        private void ShowNotification(List<(string CharacterName, string TaskName, TaskCategory Category)> pendingTasks)
        {
            if (pendingTasks.Count == 0) return;

            // 카테고리별 그룹화
            var dailyCount = pendingTasks.Count(t => t.Category == TaskCategory.Daily);
            var weeklyCount = pendingTasks.Count(t => t.Category == TaskCategory.Weekly);
            var bossCount = pendingTasks.Count(t => t.Category == TaskCategory.Boss);

            string title = "📋 메이플 숙제 알림";
            string message = "";

            if (dailyCount > 0) message += $"🌅 일일: {dailyCount}개\n";
            if (weeklyCount > 0) message += $"📅 주간: {weeklyCount}개\n";
            if (bossCount > 0) message += $"👹 보스: {bossCount}개\n";

            message += $"\n총 {pendingTasks.Count}개의 숙제가 남아있습니다!";

            _notifyIcon.ShowBalloonTip(5000, title, message, Forms.ToolTipIcon.Warning);
        }

        /// <summary>
        /// 수동으로 알림 테스트
        /// </summary>
        public void TestNotification()
        {
            var appData = CharacterRepository.Load();
            var pendingTasks = new List<(string CharacterName, string TaskName, TaskCategory Category)>();

            pendingTasks.AddRange(GetDailyPendingTasks(appData));
            pendingTasks.AddRange(GetWeeklyPendingTasks(appData));
            pendingTasks.AddRange(GetBossPendingTasks(appData));

            if (pendingTasks.Any())
            {
                ShowNotification(pendingTasks);
            }
            else
            {
                _notifyIcon.ShowBalloonTip(3000, "📋 메이플 숙제 알림", "완료하지 않은 즐겨찾기 숙제가 없습니다!", Forms.ToolTipIcon.Info);
            }
        }
    }
}

