using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class ErrorForm : Form
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    // 控件声明
    private TextBox txtErrorMessage;
    private TextBox txtStackTrace;
    private TextBox txtErrorType;
    private TextBox txtSolution;
    private Button btnCopyToClipboard;
    private TextBox txtSystemInfo;
    private Button btnExit;

    public ErrorForm(string errorType, string errorMessage, string stackTrace)
    {
        // 窗体基本设置
        this.Text = "程序遇到严重错误";
        this.Width = 850;
        this.Height = 700;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.TopMost = true;
        this.Icon = SystemIcons.Error;
        this.Font = new Font("微软雅黑", 9);

        // 窗口置顶逻辑
        this.Load += (sender, e) => SetForegroundWindow(this.Handle);
        //this.FormClosing += (sender, e) => Environment.Exit(0);

        // 生成系统信息
        var systemInfo = GetSystemInfo();

        // 分析错误信息
        var errorAnalysis = new ErrorAnalysis(
            errorType,
            ErrorAnalyzer.GetSolution(errorType) // 保持原有解决方案逻辑
        );


        // 初始化界面组件
        InitializeComponents(systemInfo, errorMessage, stackTrace, errorAnalysis);

        // 记录错误日志
        LogError(systemInfo, errorMessage, stackTrace, errorAnalysis);
    }

    private void InitializeComponents(string systemInfo, string errorMessage, string stackTrace, ErrorAnalysis analysis)
    {
        // 系统信息面板
        var panelSystemInfo = new Panel { Dock = DockStyle.Top, Height = 180 };
        txtSystemInfo = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            Text = systemInfo,
            BackColor = Color.WhiteSmoke,
            ScrollBars = ScrollBars.Vertical
        };

        // 错误类型面板
        var panelErrorType = new Panel { Dock = DockStyle.Top, Height = 60 };
        var lblErrorType = new Label { Text = "错误类型：", Dock = DockStyle.Top, Height = 20 };
        txtErrorType = new TextBox
        {
            Text = analysis.ErrorType,
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.MistyRose,
            ForeColor = Color.DarkRed
        };
        // 解决方案面板
        var panelSolution = new Panel { Dock = DockStyle.Top, Height = 100 };
        var lblSolution = new Label { Text = "解决方案：", Dock = DockStyle.Top, Height = 20 };
        txtSolution = new TextBox
        {
            Text = analysis.Solution,
            Multiline = true,
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.Honeydew,
            ForeColor = Color.DarkGreen,
            ScrollBars = ScrollBars.Vertical
        };
        // 错误信息面板
        var lblError = new Label { Text = "错误信息：", Dock = DockStyle.Top, Height = 20 };
        txtErrorMessage = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Top,
            Height = 80,
            Text = errorMessage,
            ScrollBars = ScrollBars.Vertical
        };

        

        // 堆栈跟踪面板
        var lblStackTrace = new Label { Text = "堆栈跟踪：", Dock = DockStyle.Top, Height = 20 };
        txtStackTrace = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            Text = stackTrace,
            Font = new Font("Consolas", 8.5f),
            ScrollBars = ScrollBars.Both
        };

        // 按钮面板
        var flowPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 45,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(5)
        };

        btnExit = new Button
        {
            Text = "继续",
            Size = new Size(90, 30),
            BackColor = Color.LightCoral
        };
        btnExit.Click += (sender, e) => this.Close();

        btnCopyToClipboard = new Button
        {
            Text = "复制错误",
            Size = new Size(90, 30),
            BackColor = Color.LightSkyBlue
        };
        btnCopyToClipboard.Click += CopyToClipboardHandler;

        // 构建界面
        panelSystemInfo.Controls.Add(txtSystemInfo);
        panelErrorType.Controls.Add(txtErrorType);
        panelErrorType.Controls.Add(lblErrorType);
        panelSolution.Controls.Add(txtSolution);
        panelSolution.Controls.Add(lblSolution);
        flowPanel.Controls.Add(btnExit);
        flowPanel.Controls.Add(btnCopyToClipboard);

        this.Controls.Add(txtStackTrace);
        this.Controls.Add(lblStackTrace);
        this.Controls.Add(panelSolution);
        this.Controls.Add(txtErrorMessage);
        this.Controls.Add(lblError);
        this.Controls.Add(panelErrorType);
        this.Controls.Add(panelSystemInfo);
        this.Controls.Add(flowPanel);
    }

    private string GetSystemInfo()
    {
        var process = Process.GetCurrentProcess();
        var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));

        return $@"[系统信息]
时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}
操作系统: {Environment.OSVersion} (64bit: {Environment.Is64BitOperatingSystem})
运行时: {Environment.Version} (CLR: {Environment.Version.Major}.{Environment.Version.Minor})
架构: {(Environment.Is64BitProcess ? "x64" : "x86")} 
用户: {Environment.UserDomainName}\{Environment.UserName}
区域: {CultureInfo.CurrentCulture.Name}

[硬件信息]
CPU 核心: {Environment.ProcessorCount}
系统内存: {GetTotalPhysicalMemory()}
磁盘空间: {drive.TotalSize / 1024 / 1024 / 1024} GB (可用: {drive.AvailableFreeSpace / 1024 / 1024 / 1024} GB)
屏幕分辨率: {GetScreenResolution()}

[进程信息]
进程内存: {process.WorkingSet64 / 1024 / 1024} MB
线程数量: {process.Threads.Count}
启动时间: {DateTime.Now - process.StartTime:hh\:mm\:ss}
句柄数量: {process.HandleCount}

[运行时状态]
GC 内存: {GC.GetTotalMemory(false) / 1024 / 1024} MB
调试模式: {Debugger.IsAttached}
交互模式: {Environment.UserInteractive}
程序版本: {Assembly.GetExecutingAssembly().GetName().Version}
启动参数: {string.Join(" ", Environment.GetCommandLineArgs())}";
    }

    private string GetScreenResolution()
    {
        var screen = Screen.PrimaryScreen;
        return $"{screen.Bounds.Width}x{screen.Bounds.Height} (DPI: {screen.BitsPerPixel})";
    }

    private string GetTotalPhysicalMemory()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                var totalBytes = Convert.ToUInt64(obj["TotalPhysicalMemory"]);
                return $"{totalBytes / 1024 / 1024 / 1024} GB";
            }
        }
        catch { }
        return "N/A";
    }

    private void LogError(string systemInfo, string errorMessage, string stackTrace, ErrorAnalysis analysis)
    {
        try
        {
            var logContent = $"{systemInfo}\n\n" +
                             $"[错误类型]\n{analysis.ErrorType}\n\n" +
                             $"[错误信息]\n{errorMessage}\n\n" +
                             $"[解决方案]\n{analysis.Solution}\n\n" +
                             $"[堆栈跟踪]\n{stackTrace}";

            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MyApp",
                "ErrorLogs",
                $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            File.WriteAllText(logPath, logContent);
        }
        catch { /* 日志记录失败时静默处理 */}
    }

    private void CopyToClipboardHandler(object sender, EventArgs e)
    {
        try
        {
            var content = $"【错误类型】\n{txtErrorType.Text}\n\n" +
                          $"【环境信息】\n{txtSystemInfo.Text}\n\n" +
                          $"【错误信息】\n{txtErrorMessage.Text}\n\n" +
                          $"【解决方案】\n{txtSolution.Text}\n\n" +
                          $"【堆栈跟踪】\n{txtStackTrace.Text}";

            Clipboard.SetText(content);
            MessageBox.Show("完整错误信息已复制到剪贴板", "复制成功",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"复制失败: {ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // 错误分析系统
    private static class ErrorAnalyzer
    {
        private static readonly List<ErrorPattern> Patterns = new List<ErrorPattern>
        {
            new ErrorPattern("FileNotFoundException", "文件未找到错误",
                "1. 检查文件路径是否正确\n2. 确认文件是否存在\n3. 验证文件访问权限"),

            new ErrorPattern("UnauthorizedAccessException", "权限不足错误",
                "1. 以管理员身份运行程序\n2. 检查文件/目录权限\n3. 关闭杀毒软件试试"),

            new ErrorPattern("SqlException", "数据库操作错误",
                "1. 检查数据库连接字符串\n2. 验证数据库服务状态\n3. 检查SQL语句语法"),

            new ErrorPattern("NullReferenceException", "空引用异常",
                "1. 检查对象是否初始化\n2. 使用null条件运算符(?.)\n3. 添加空值检查逻辑"),

            new ErrorPattern("TimeoutException", "操作超时错误",
                "1. 检查网络连接\n2. 增加超时时间设置\n3. 优化资源密集型操作"),

            new ErrorPattern("OutOfMemoryException", "内存不足错误",
                "1. 关闭不必要的程序\n2. 优化内存使用\n3. 增加物理内存"),

            new ErrorPattern("ArgumentException", "参数无效错误",
                "1. 检查输入参数格式\n2. 验证参数取值范围\n3. 查看API文档")
        };

        public static string GetSolution(string errorType)
        {
            return Patterns.FirstOrDefault(p =>
                p.Type.Equals(errorType, StringComparison.OrdinalIgnoreCase)
            )?.Solution ?? DefaultSolution;
        }

        private const string DefaultSolution =
            "1. 尝试重启应用程序\n2. 检查日志文件获取详细信息\n3. 联系技术支持团队";
        public static ErrorAnalysis AnalyzeError(string errorMessage)
        {
            foreach (var pattern in Patterns)
            {
                if (errorMessage.IndexOf(pattern.Pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return new ErrorAnalysis(pattern.Type, pattern.Solution);
                }
            }

            return new ErrorAnalysis(
                "未识别错误类型",
                "1. 尝试重启应用程序\n2. 检查日志文件获取详细信息\n3. 联系技术支持团队");
        }

        private record ErrorPattern(string Pattern, string Type, string Solution);
    }

    private record ErrorAnalysis(string ErrorType, string Solution);
}
