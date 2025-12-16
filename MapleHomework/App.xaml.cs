using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MapleHomework.Services;
using MapleHomework.ViewModels;
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

            // 전역 예외 처리 핸들러 등록
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // 서비스 초기화 및 마이그레이션
            var migrationService = new MigrationService();
            migrationService.MigrateIfNeeded();

            var appData = CharacterRepository.Load();

            // 앱 실행 시 테마 초기화 (ViewModel 생성 전에 리소스가 설정되어야 함)
            ThemeService.ApplyTheme(appData.IsDarkTheme);

            var apiService = new MapleApiService();
            var windowService = new WindowService();

            var mainViewModel = new MainViewModel(appData, apiService, windowService);

            // MainWindow 표시
            var mainWindow = new MainWindow(mainViewModel);
            mainWindow.Show();

            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash(e.Exception, "DispatcherUnhandledException");
            // e.Handled = true; // 필요시 주석 해제하여 크래시 방지 가능 (하지만 상태가 불안정할 수 있음)
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogCrash(e.Exception, "UnobservedTaskException");
            e.SetObserved(); // 프로세스 종료 방지
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogCrash(ex, "CurrentDomain_UnhandledException");
            }
        }

        private void LogCrash(Exception ex, string source)
        {
            try
            {
                string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string logDir = System.IO.Path.Combine(docsPath, "MapleScheduler", "crash-logs");

                if (!System.IO.Directory.Exists(logDir))
                {
                    System.IO.Directory.CreateDirectory(logDir);
                }

                string fileName = $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string content = $"[{DateTime.Now}] Source: {source}\nMessage: {ex.Message}\nStackTrace:\n{ex.StackTrace}\n\nInnerException:\n{ex.InnerException}";

                System.IO.File.WriteAllText(System.IO.Path.Combine(logDir, fileName), content);
            }
            catch
            {
                // 로깅 실패는 어쩔 수 없음
            }
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
                        // 배치 API 사용 (Workers 호출 최소화)
                        result = await apiService.CollectGrowthHistoryBatchAsync(ocid, characterId, characterName, specificDates, progress);
                    }
                    else
                    {
                        // 날짜 목록 생성 후 배치 수집
                        var dateList = Enumerable.Range(1, days)
                            .Select(i => DateTime.Now.AddDays(-i))
                            .ToList();
                        result = await apiService.CollectGrowthHistoryBatchAsync(ocid, characterId, characterName, dateList, progress);
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