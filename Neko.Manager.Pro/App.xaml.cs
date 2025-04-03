using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Neko.EFT.Manager.X.Classes;
using System;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.Foundation;
using Neko.EFT.Manager.X.Windows;
using CommunityToolkit.WinUI.Notifications;
using System.Threading.Tasks;
using Neko.EFT.Manager.X.Pages;
using System.Diagnostics;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.IO;
using UnhandledExceptionEventArgs = System.UnhandledExceptionEventArgs;
using System.Threading;
using Microsoft.Extensions.Logging;
using LogLevel = NLog.LogLevel;
using ILogger = NLog.ILogger;
using Polly;
using System.Collections.Generic;
using System.Linq;

namespace Neko.EFT.Manager.X
{
    public partial class App : Application
    {
        private static readonly Dictionary<string, string> ErrorTypeMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "NullReferenceException", "空引用异常" },
        { "FileNotFoundException", "文件未找到错误" },
        { "UnauthorizedAccessException", "权限不足错误" },
        { "SqlException", "数据库操作错误" },
        { "TimeoutException", "操作超时错误" },
        { "OutOfMemoryException", "内存不足错误" },
        { "ArgumentException", "参数无效错误" }
    };
        private static string FormatErrorType(string rawErrorType)
        {
            // 检查类型名是否包含关键字（如 "NullReference"）
            var matchedKey = ErrorTypeMappings.Keys
                .FirstOrDefault(key => rawErrorType.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0);

            if (matchedKey != null)
            {
                return $"{rawErrorType} [{ErrorTypeMappings[matchedKey]}]";
            }
            return $"{rawErrorType} [未识别错误类型]";
        }
        public static MainWindow MainWindow { get; private set; }

        public static MainWindow MainWindowInstance { get; private set; }

        // 假设你在 App.xaml.cs 中定义了 VntManagementWindowInstance
        public static VntManagementWindow VntManagementWindowInstance { get; set; }

        // 确保在启动时正确初始化


        private static ManagerConfig _managerConfig = new ManagerConfig();
        public static ManagerConfig ManagerConfig
        {
            get => _managerConfig;
            set
            {
                _managerConfig = value;
                _managerConfig.OnPropertyChanged(nameof(ManagerConfig));
            }
        }

        public GameAOPage GameAOPageS { get; }

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private SplashScreenWindow splashScreenWindow;

        [Obsolete]
        public App()
        {
            // 初始化日志系统（优先执行）
            InitializeLogging();
            ManagerConfig.Load(); // 应用启动时加载配置
            this.InitializeComponent();
            logger.Info("Application initialized");

            // 注册全局异常处理（确保单次注册）
            RegisterExceptionHandlers();

            Loggy.SetupLogFile();
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            Debug.WriteLine("App constructor completed");
        }

        private void InitializeLogging()
        {
            var config = new LoggingConfiguration();

            // 添加异步文件目标（提升性能）
            var fileTarget = new FileTarget("logfile")
            {
                FileName = "${basedir}/logs/app.log",
                Layout = "${longdate} ${level} ${message} ${exception:format=ToString}",
                ArchiveEvery = FileArchivePeriod.Day,
                MaxArchiveFiles = 7
            };
            config.AddTarget(fileTarget);

            // 控制台目标（仅调试时启用）
            if (Debugger.IsAttached)
            {
                var consoleTarget = new ConsoleTarget("logconsole")
                {
                    Layout = "${longdate} ${level} ${message} ${exception}"
                };
                config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget);
            }

            // 应用配置
            LogManager.Configuration = config;
        }

        private void RegisterExceptionHandlers()
        {
            // 统一异常处理委托
            Action<Exception> handleException = ex =>
            {
                try
                {
                    var exceptionType = ex.GetType().FullName;
                    LogError(ex);
                    ShowErrorDialog(exceptionType, ex.Message, ex.StackTrace);
                }
                catch (Exception handlerEx)
                {
                    EmergencyLog($"Error handler failed: {handlerEx}");
                }
            };

            // UI线程异常（WinUI3）
            this.UnhandledException += (sender, e) =>
            {
                e.Handled = true;
                handleException(e.Exception);
            };

            // 非托管线程异常
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    handleException(ex);
                }
            };

            // 任务异常
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                e.SetObserved();
                handleException(e.Exception);
            };
        }
        /// <summary>
        /// 增强型错误日志记录（包含完整异常类型链）
        /// </summary>
        private void LogError(Exception ex)
        {
            try
            {
                // 记录主异常类型和消息
                var exceptionType = ex.GetType().FullName;
                logger.Error($"[{exceptionType}] {ex.Message}", ex);

                // 记录完整异常链
                Exception? current = ex;
                int depth = 0;

                while (current != null && depth < 10) // 防止无限循环
                {
                    var stackTrace = current.StackTrace?.Replace(Environment.NewLine, " | ") ?? "No StackTrace";
                    logger.Debug($"Exception Chain #{depth}: {current.GetType().FullName}\n" +
                                $"Message: {current.Message}\n" +
                                $"Source: {current.Source}\n" +
                                $"StackTrace: {stackTrace}");

                    current = current.InnerException;
                    depth++;
                }
            }
            catch (Exception logEx)
            {
                EmergencyLog($"日志记录失败: {logEx.Message}\n" +
                            $"原始错误类型: {ex.GetType().Name}\n" +
                            $"原始错误消息: {ex.Message}");
            }
        }


        /// <summary>
        /// 增强型错误日志记录
        /// </summary>


        /// <summary>
        /// 应急日志（当正常日志系统失效时使用）
        /// </summary>
        private static void EmergencyLog(string message)
        {
            try
            {
                File.AppendAllText("emergency.log",
                    $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
            }
            catch { /* 最终保底处理 */ }
        }

        /// <summary>
        /// 显示用户友好的错误提示
        /// </summary>
        private static void ShowErrorDialog(string errorType, string errorMessage, string stackTrace)
        {
            // 格式化错误类型
            string formattedErrorType = FormatErrorType(errorType);

            Thread thread = new(() =>
            {
                var form = new ErrorForm(formattedErrorType, errorMessage, stackTrace);
                System.Windows.Forms.Application.Run(form);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }



        private void OnProcessExit(object? sender, EventArgs e)
        {
            try
            {
                LogManager.Shutdown(); // 确保日志队列清空
            }
            catch (Exception ex)
            {
                EmergencyLog($"Shutdown failed: {ex.Message}");
            }
        }
        private bool IsFirstRun()
        {
            // 如果配置文件不存在或者关键配置为空，则认为是首次运行
            return !File.Exists(ManagerConfig.ConfigFilePath) || string.IsNullOrEmpty(ManagerConfig.InstallPath);
        }
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {


            //ManagerConfig.Load();

            Debug.WriteLine("Application launched.");

            var appArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            var mainInstance = AppInstance.FindOrRegisterForKey("main");

            if (!mainInstance.IsCurrent)
            {
                Debug.WriteLine("Redirecting to current instance...");
                await mainInstance.RedirectActivationToAsync(appArgs);
                System.Diagnostics.Process.GetCurrentProcess().Kill();
                return;
            }

            AppInstance.GetCurrent().Activated += App_Activated;

            // 显示启动画面
            Debug.WriteLine("Displaying splash screen...");
            var splashScreen = new SplashScreenWindow();
            splashScreen.Activate();
            splashScreenWindow = splashScreen;

            // 等待片刻以确保启动画面正确显示
            await Task.Delay(500);

            try
            {
                // 加载配置文件
                UpdateSplashScreenStatus("正在加载配置文件...");
                //ManagerConfig.Load();
                UpdateSplashScreenStatus("配置文件加载完成。");

                // 检查用户协议
                if (!ManagerConfig.IUserAgreementAccepted)
                {
                    UpdateSplashScreenStatus("显示用户协议...");
                    Debug.WriteLine("Displaying user agreement...");
                    var userAgreementWindow = new UserAgreementWindow();
                    userAgreementWindow.Activate();

                    bool isAgreementAccepted = await userAgreementWindow.WaitForUserAgreementAsync();

                    if (!isAgreementAccepted)
                    {
                        Debug.WriteLine("User declined agreement. Exiting application...");
                        UpdateSplashScreenStatus("用户未接受协议，退出应用程序。");
                        Application.Current.Exit();
                        return;
                    }

                    ManagerConfig.IUserAgreementAccepted = true;
                    ManagerConfig.Save();
                    UpdateSplashScreenStatus("用户协议已接受。");
                }

                // 初始化后台服务
                UpdateSplashScreenStatus("正在初始化后台服务...");
                await InitializeBackendServicesAsync();
                UpdateSplashScreenStatus("后台服务初始化完成。");

                // 检查是否需要显示配置引导
                if (!ManagerConfig.IConfigGuide)
                {
                    UpdateSplashScreenStatus("首次启动，准备配置引导...");
                    Debug.WriteLine("首次运行，展示配置引导窗口...");

                    var configGuideWindow = new ConfigGuideWindow();
                    c_window = configGuideWindow;
                    c_window.Activate();

                    bool isConfigured = await c_window.WaitForConfigurationAsync();

                    if (!isConfigured)
                    {
                        Debug.WriteLine("用户取消了配置引导，退出应用...");
                        UpdateSplashScreenStatus("用户取消配置引导，退出应用程序。");
                        Application.Current.Exit();
                        return;
                    }

                    ManagerConfig.IConfigGuide = true;
                    ManagerConfig.Save();
                    UpdateSplashScreenStatus("配置引导完成");
                }

                // 加载资源文件
                UpdateSplashScreenStatus("正在加载资源文件...");
                await LoadResourceFilesAsync();
                UpdateSplashScreenStatus("资源文件加载完成");

                await Task.Delay(1000);
                // 初始化主窗口
                UpdateSplashScreenStatus("正在初始化主窗口...");
                MainWindow = new MainWindow();
                m_window = MainWindow;
                await Task.Delay(1000);
                UpdateSplashScreenStatus("主窗口初始化完成，正在启动");

                await Task.Delay(2000);
                // 关闭启动画面
                Debug.WriteLine("Closing splash screen.");
                splashScreen.Close();

                // 激活主窗口
                Debug.WriteLine("Activating main window.");
                m_window.Activate();
                m_window.Closed += OnWindowClosed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Initialization failed: {ex.Message}");
                UpdateSplashScreenStatus($"初始化失败：{ex.Message}");
                await Task.Delay(3000); // 短暂停留以显示错误信息
                Application.Current.Exit();
            }
        }

        private async Task InitializeBackendServicesAsync()
        {
            // 模拟后台服务初始化
            await Task.Delay(2000);
            Debug.WriteLine("Backend services initialized.");
        }

        private async Task LoadResourceFilesAsync()
        {
            // 模拟资源文件加载
            await Task.Delay(2000);
            Debug.WriteLine("Resource files loaded.");
        }


        private void UpdateSplashScreenStatus(string message)
        {

            Debug.WriteLine(message);
            splashScreenWindow?.UpdateStatus(message);
        }


        private void App_Activated(object? sender, AppActivationArguments e)
        {
            Debug.WriteLine("Application activated.");
            m_window.DispatcherQueue.TryEnqueue(() =>
            {
                Debug.WriteLine("Activating main window from App_Activated.");
                m_window?.Activate();
            });
        }

        private void OnWindowClosed(object sender, WindowEventArgs e)
        {
            Debug.WriteLine("Main window closed.");
            MainWindow = null;
            Application.Current.Exit(); // 确保应用程序完全退出
        }

        private void ShowSplashScreen()
        {
            Debug.WriteLine("Showing splash screen page...");
            splashScreenWindow = new SplashScreenWindow();
            var splashScreenPage = new SplashScreenPage();
            splashScreenWindow.Content = splashScreenPage;
            splashScreenWindow.Activate();
        }

        internal static Window m_window;
        internal static ConfigGuideWindow c_window;
    }
}
