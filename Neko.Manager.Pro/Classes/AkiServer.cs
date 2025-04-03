using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Neko.EFT.Manager.X.Classes
{
    public static class AkiServer
    {
        #region events
        public static event OutputDataReceivedEventHandler? OutputDataReceived;
        public delegate void OutputDataReceivedEventHandler(object sender, DataReceivedEventArgs e);

        public static event StateChangedEventHandler? RunningStateChanged;
        public delegate void StateChangedEventHandler(RunningState runningState);

        public static StreamWriter Writer;
        #endregion

        #region fields

        public enum RunningState
        {
            NOT_RUNNING,
            RUNNING,
            STOPPED_UNEXPECTEDLY
        }

        public static Process? Process;

        private static string exeName = ""; // 默认值为空字符串
        public static string ExeName
        {
            get => exeName;
        }

        public static string FilePath
        {
            get => App.ManagerConfig.AkiServerPath != null ? Path.Combine(App.ManagerConfig.AkiServerPath, ExeName) : "";
        }

        public static string Directory
        {
            get => App.ManagerConfig.AkiServerPath != null ? App.ManagerConfig.AkiServerPath : "";
        }

        private static RunningState _state = RunningState.NOT_RUNNING;
        public static RunningState State
        {
            get => _state;
        }

        private static bool stopRequest = false;

        #endregion

        public static void Start()
        {
            if (_state == RunningState.RUNNING)
                return;

            // 自动确定可执行文件名
            DetermineExeName();

            Process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = FilePath,
                    WorkingDirectory = Directory,
                    UseShellExecute = false,
                    StandardOutputEncoding = Encoding.UTF8,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,  // 启用标准输入流
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            Process.OutputDataReceived += new DataReceivedEventHandler((sender, e) => OutputDataReceivedEvent(sender, e));
            Process.Exited += new EventHandler((sender, e) => ExitedEvent(sender, e));

            Process.Start();
            Process.BeginOutputReadLine();

            Writer = Process.StandardInput; // 保留标准输入流的引用

            _state = RunningState.RUNNING;

            RunningStateChanged?.Invoke(_state);
        }


        public static void Stop()
        {
            if (_state == RunningState.NOT_RUNNING || Process == null || Process.HasExited)
                return;

            stopRequest = true;

            // this allows to gracefully close a console app.
            Win32.CloseConsoleProgram(Process);
        }

        public static bool IsUnhandledInstanceRunning()
        {
            Process[] akiServerProcesses = Process.GetProcessesByName(ExeName.Replace(".exe", ""));

            if (akiServerProcesses.Length > 0)
            {
                if (Process == null || Process.HasExited)
                    return true;

                foreach (Process akiServerProcess in akiServerProcesses)
                {
                    if (Process.Id != akiServerProcess.Id)
                        return true;
                }
            }

            return false;
        }

        public static void DetermineExeName()
        {
            if (App.ManagerConfig.AkiServerPath == null)
                throw new InvalidOperationException("服务端路径未设置");

            var possibleExecutables = new[] { "Aki.Server.exe", "SPT.Server.exe", "Server.exe" };
            foreach (var exe in possibleExecutables)
            {
                var exePath = Path.Combine(App.ManagerConfig.AkiServerPath, exe);
                if (File.Exists(exePath))
                {
                    exeName = exe;
                    return;
                }
            }

            // 如果没有找到匹配的可执行文件，抛出异常
            throw new FileNotFoundException("未找到服务器可执行文件");
        }


        private static void OutputDataReceivedEvent(object sender, DataReceivedEventArgs e)
        {
            OutputDataReceived?.Invoke(sender, e);
        }

        private static void ExitedEvent(object? sender, EventArgs e)
        {
            if (_state == RunningState.RUNNING && !stopRequest)
            {
                _state = RunningState.STOPPED_UNEXPECTEDLY;
            }
            else
            {
                _state = RunningState.NOT_RUNNING;
            }

            stopRequest = false;
            RunningStateChanged?.Invoke(_state);
            Writer.Close(); // 在进程退出时关闭 StreamWriter
        }
    }
}
