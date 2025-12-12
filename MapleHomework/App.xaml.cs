using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MapleHomework.Services;
using Application = System.Windows.Application;

namespace MapleHomework
{
    public partial class App : Application
    {
        private static Mutex? _mutex;
        private const string MutexName = "MapleHomework_SingleInstance_Mutex";
        
        // 백그라운드 API 수집 관리
        private static Task? _backgroundCollectTask;
        private static CancellationTokenSource? _collectCancellation;
        public static bool IsCollecting => _backgroundCollectTask != null && !_backgroundCollectTask.IsCompleted;
        
        // 수집 진행률 이벤트
        public static event Action<int>? CollectProgressChanged;
        public static event Action<bool, string>? CollectCompleted;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 다중 인스턴스 방지
            _mutex = new Mutex(true, MutexName, out bool createdNew);

            if (!createdNew)
            {
                // 이미 실행 중인 인스턴스가 있음
                BringExistingInstanceToFront();
                Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        private void BringExistingInstanceToFront()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var processes = Process.GetProcessesByName(currentProcess.ProcessName)
                    .Where(p => p.Id != currentProcess.Id)
                    .ToList();

                foreach (var process in processes)
                {
                    var handle = process.MainWindowHandle;
                    if (handle != IntPtr.Zero)
                    {
                        // 최소화되어 있으면 복원
                        if (IsIconic(handle))
                        {
                            ShowWindow(handle, SW_RESTORE);
                        }
                        // 포그라운드로 가져오기
                        SetForegroundWindow(handle);
                        break;
                    }
                }
            }
            catch
            {
                // 실패 시 무시
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 백그라운드 수집 취소
            _collectCancellation?.Cancel();
            
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }

        /// <summary>
        /// 백그라운드에서 API 데이터 수집 시작
        /// </summary>
        public static void StartBackgroundCollect(
            string apiKey,
            string ocid,
            string characterId,
            string characterName,
            int days,
            List<DateTime>? specificDates = null,
            NotificationService? notificationService = null)
        {
            if (IsCollecting)
            {
                return; // 이미 수집 중
            }

            _collectCancellation = new CancellationTokenSource();
            var token = _collectCancellation.Token;

            _backgroundCollectTask = Task.Run(async () =>
            {
                var apiService = new MapleApiService();
                
                try
                {
                    // 시작 알림 (UI 스레드에서 실행)
                    Current?.Dispatcher.Invoke(() =>
                    {
                        notificationService?.ShowApiCollectStart(characterName);
                    });

                    var progress = new Progress<int>(p =>
                    {
                        CollectProgressChanged?.Invoke(p);
                    });

                    GrowthHistoryResult result;
                    if (specificDates != null && specificDates.Any())
                    {
                        result = await apiService.CollectGrowthHistoryForDatesAsync(apiKey, ocid, characterId, characterName, specificDates, progress);
                    }
                    else
                    {
                        result = await apiService.CollectGrowthHistoryAsync(apiKey, ocid, characterId, characterName, days, progress);
                    }

                    // 완료 알림 (UI 스레드에서 실행)
                    Current?.Dispatcher.Invoke(() =>
                    {
                        notificationService?.ShowApiCollectComplete(characterName, result.Success, result.Message);
                        CollectCompleted?.Invoke(result.Success, result.Message);
                    });
                }
                catch (OperationCanceledException)
                {
                    // 취소됨
                }
                catch (Exception ex)
                {
                    Current?.Dispatcher.Invoke(() =>
                    {
                        notificationService?.ShowApiCollectComplete(characterName, false, $"오류: {ex.Message}");
                        CollectCompleted?.Invoke(false, $"오류: {ex.Message}");
                    });
                }
            }, token);
        }

        /// <summary>
        /// 백그라운드 수집 취소
        /// </summary>
        public static void CancelBackgroundCollect()
        {
            _collectCancellation?.Cancel();
        }
    }
}