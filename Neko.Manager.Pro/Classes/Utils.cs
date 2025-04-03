using CG.Web.MegaApiClient;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using CommunityToolkit.WinUI;
using Microsoft.UI.Text;
using Neko.EFT.Manager.X.Windows;

namespace Neko.EFT.Manager.X.Classes
{
    public class Utils
    {
        public static HttpClient utilsHttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(15),
            DefaultRequestHeaders = {
            { "X-GitHub-Api-Version", "2022-11-28" },
            { "User-Agent", "request" }
        }
        };
        
        /// <summary>
        /// Checks the installed EFT version
        /// </summary>
        /// <param name="path">The path to check.</param>
        public static void CheckEFTVersion(string path)
        {
            path += @"\EscapeFromTarkov.exe";
            if (File.Exists(path))
            {
                string fileVersion = FileVersionInfo.GetVersionInfo(path).ProductVersion;
                fileVersion = Regex.Match(fileVersion, @"[0]{1,}\.[0-9]{1,2}\.[0-9]{1,2}\.[0-9]{1,2}\-[0-9]{1,5}").Value.Replace("-", ".");
                App.ManagerConfig.TarkovVersion = fileVersion;

                Loggy.LogToFile("EFT Version is now: " + fileVersion);
            }
            else
            {
                Loggy.LogToFile("CheckEFTVersion: File did not exist at " + path);
            }
        }

        /// <summary>
        /// Checks the installed Neko version
        /// </summary>
        /// <param name="path">The path to check.</param>
        public static void CheckSITVersion(string path)
        {
            path += @"\BepInEx\plugins\StayInTarkov.dll";
            if (File.Exists(path))
            {
                string fileVersion = FileVersionInfo.GetVersionInfo(path).ProductVersion;
                fileVersion = Regex.Match(fileVersion, @"[1]{1,}\.[0-9]{1,2}\.[0-9]{1,5}\.[0-9]{1,5}").Value.ToString();
                App.ManagerConfig.SitVersion = fileVersion;
                Loggy.LogToFile("Neko Version is now: " + fileVersion);
            }
            else
            {
                Loggy.LogToFile("CheckSITVersion: File did not exist at " + path);
            }
        }

        /// <summary>
        /// Clones a directory
        /// </summary>
        /// <param name="root">Root path to clone</param>
        /// <param name="dest">Destination path to clone to</param>
        /// <returns></returns>
        public static async Task CloneDirectory(string root, string dest)
        {
            foreach (var directory in Directory.GetDirectories(root))
            {
                var newDirectory = Path.Combine(dest, Path.GetFileName(directory));
                Directory.CreateDirectory(newDirectory);
                CloneDirectory(directory, newDirectory);
            }

            foreach (var file in Directory.GetFiles(root))
            {
                File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
            }
        }

        /// <summary>
        /// Cleans up the EFT directory
        /// </summary>
        /// <returns></returns>
        public static async Task CleanUpEFTDirectory()
        {
            Loggy.LogToFile("Cleaning up EFT directory...");

            try
            {
                string battlEyeDir = App.ManagerConfig.InstallPath + @"\BattlEye";
                if (Directory.Exists(battlEyeDir))
                {
                    Directory.Delete(battlEyeDir, true);
                }
                string battlEyeExe = App.ManagerConfig.InstallPath + @"\EscapeFromTarkov_BE.exe";
                if (File.Exists(battlEyeExe))
                {
                    File.Delete(battlEyeExe);
                }
                string cacheDir = App.ManagerConfig.InstallPath + @"\cache";
                if (Directory.Exists(cacheDir))
                {
                    Directory.Delete(cacheDir, true);
                }
                string consistencyPath = App.ManagerConfig.InstallPath + @"\ConsistencyInfo";
                if (File.Exists(consistencyPath))
                {
                    File.Delete(consistencyPath);
                }
                string uninstallPath = App.ManagerConfig.InstallPath + @"\Uninstall.exe";
                if (File.Exists(uninstallPath))
                {
                    File.Delete(uninstallPath);
                }
                string logsDirPath = App.ManagerConfig.InstallPath + @"\Logs";
                if (Directory.Exists(logsDirPath))
                {
                    Directory.Delete(logsDirPath);
                }
            }
            catch (Exception ex)
            {
                Loggy.LogToFile("Cleanup: " + ex.Message);
            }

            Loggy.LogToFile("Cleanup done.");
        }

        /// <summary>
        /// Downloads a file and shows a progress bar if enabled
        /// </summary>
        /// <param name="fileName">The name of the file to be downloaded.</param>
        /// <param name="filePath">The path (not including the filename) to download to.</param>
        /// <param name="fileUrl">The URL to download from.</param>
        /// <param name="showProgress">If a progress bar should show the status.</param>
        /// <returns></returns>
        public async static Task<bool> DownloadFile(string fileName, string filePath, string fileUrl, bool showProgress = false)
        {
            var window = App.m_window as MainWindow;
            DispatcherQueue mainQueue = window.DispatcherQueue;

            if (fileUrl.Contains("mega.nz"))
            {
                try
                {
                    Loggy.LogToFile("Attempting to use Mega API.");

                    MegaApiClient megaApiClient = new MegaApiClient();
                    await megaApiClient.LoginAnonymousAsync();

                    // Todo: Add proper error handling below
                    if (!megaApiClient.IsLoggedIn)
                        return false;

                    Loggy.LogToFile($"Starting download of '{fileName}' from '{fileUrl}'");

                    if (showProgress == true)
                        mainQueue.TryEnqueue(() =>
                        {
                            window.actionPanel.Visibility = Visibility.Visible;
                            window.actionProgressRing.Visibility = Visibility.Visible;
                            window.actionTextBlock.Text = $"下载中 '{fileName}'";
                        });

                    Progress<double> progress = new Progress<double>((prog) => { mainQueue.TryEnqueue(() => { window.actionProgressBar.Value = (int)Math.Floor(prog); }); });

                    Uri fileLink = new(fileUrl);
                    INode fileNode = await megaApiClient.GetNodeFromLinkAsync(fileLink);

                    await megaApiClient.DownloadFileAsync(fileNode, App.ManagerConfig.InstallPath + $@"\{fileName}", progress);

                    if (showProgress == true)
                        mainQueue.TryEnqueue(() =>
                        {
                            window.actionPanel.Visibility = Visibility.Collapsed;
                            window.actionProgressRing.Visibility = Visibility.Collapsed;
                            window.actionTextBlock.Text = "";
                        });

                    return true;
                }
                catch
                {
                    return false;
                }
            }

            try
            {
                Loggy.LogToFile($"Starting download of '{fileName}' from '{fileUrl}'");
                if (showProgress == true)
                    mainQueue.TryEnqueue(() =>
                    {
                        window.actionPanel.Visibility = Visibility.Visible;
                        window.actionProgressRing.Visibility = Visibility.Visible;
                        window.actionTextBlock.Text = $"下载中 '{fileName}'";
                    });

                filePath = filePath + $@"\{fileName}";

                if (File.Exists(filePath))
                    File.Delete(filePath);

                var progress = new Progress<float>((prog) => { mainQueue.TryEnqueue(() => { window.actionProgressBar.Value = (int)Math.Floor(prog); }); });
                using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    await HttpClientProgressExtensions.DownloadDataAsync(utilsHttpClient, fileUrl, file, progress);

                if (showProgress == true)
                    mainQueue.TryEnqueue(() =>
                    {
                        window.actionPanel.Visibility = Visibility.Collapsed;
                        window.actionProgressRing.Visibility = Visibility.Collapsed;
                        window.actionTextBlock.Text = "";
                    });

                return true;
            }
            catch (Exception ex)
            {
                Loggy.LogToFile("DownloadFile: " + ex.Message);
                return false;
            }
        }


        public void ShowNotification(string title, string content)
        {
            // 创建 Toast 通知
            ToastNotification toast = new ToastNotification(CreateToastXml(title, content));

            // 显示 Toast 通知
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private XmlDocument CreateToastXml(string title, string content)
        {
            // 创建 XML 文档
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);

            // 获取文本节点
            XmlNodeList textNodes = toastXml.GetElementsByTagName("text");

            // 设置标题和内容
            textNodes.Item(0).AppendChild(toastXml.CreateTextNode(title));
            textNodes.Item(1).AppendChild(toastXml.CreateTextNode(content));

            return toastXml;
        }
        /// <summary>
        /// Downloads the patcher
        /// </summary>
        /// <param name="sitVersionTarget"></param>
        /// <returns></returns>
        public async static Task DownloadAndRunPatcher(string sitVersionTarget = "")
        {
            MainWindow window = App.m_window as MainWindow;
            DispatcherQueue mainQueue = window.DispatcherQueue;

            Loggy.LogToFile("Downloading Patcher");
            // todo: proper error message
            if (App.ManagerConfig.TarkovVersion == null)
            {
                Loggy.LogToFile("DownloadPatcher: TarkovVersion is 'null'");
                return;
            }

            string releasesString = await utilsHttpClient.GetStringAsync(@"https://dev.sp-tarkov.com/api/v1/repos/SPT-AKI/Downgrade-Patches/releases");
            List<GiteaRelease> giteaReleases = JsonSerializer.Deserialize<List<GiteaRelease>>(releasesString);

            // todo: proper error message
            if (giteaReleases == null)
            {
                Loggy.LogToFile("DownloadPatcher: giteaReleases is 'null'");
            }

            List<GiteaRelease> patcherList = new List<GiteaRelease>();
            string tarkovBuild = App.ManagerConfig.TarkovVersion.Split(".").Last();
            string sitBuild = sitVersionTarget.Split(".").Last();
            string tarkovVersionToDowngrade = tarkovBuild != sitBuild ? tarkovBuild : "";

            // find the patcher automatically based on the target Neko version
            if (string.IsNullOrEmpty(tarkovVersionToDowngrade))
            {
                Loggy.LogToFile("DownloadPatcher: tarkovVersionToDowngrade is 'null'");
                return;
            }
            else
            {
                foreach (var release in giteaReleases)
                {
                    if (tarkovVersionToDowngrade == sitBuild)
                    {
                        tarkovVersionToDowngrade = "";
                        break;
                    }

                    var releaseName = release.name;

                    var patcherFrom = releaseName.Split(" to ")[0];
                    var patcherTo = releaseName.Split(" to ")[1];

                    if (patcherFrom == tarkovVersionToDowngrade)
                    {
                        patcherList.Add(release);
                        tarkovVersionToDowngrade = patcherTo;
                    }
                }

                if (!string.IsNullOrEmpty(tarkovVersionToDowngrade))
                {
                    mainQueue.TryEnqueue(async () =>
                    {
                        ContentDialog contentDialog = new()
                        {
                            XamlRoot = window.Content.XamlRoot,
                            Title = "下载错误",
                            Content = "确保所配置的《逃离塔尔科夫》路径与所选的 Neko 版本兼容，或者使用不同的 Neko 版本。.",
                            CloseButtonText = "确定"
                        };

                        await contentDialog.ShowAsync();
                    });

                    return;
                }
            }

            if (patcherList.Count == 0)
                return;

            if (patcherList.Count == 1 && patcherList[0].name.Split(" to ")[0] != App.ManagerConfig.TarkovVersion.Split(".").Last())
            {
                bool warningResult = false;

                mainQueue.TryEnqueue(async () =>
                {
                    ContentDialog contentDialog = new()
                    {
                        XamlRoot = window.Content.XamlRoot,
                        Title = "警告",
                        Content = $"你确定要继续吗？{App.ManagerConfig.TarkovVersion.Split(".").Last()}\nRequired： {patcherList[0].name.Split("到")[0]}",
                        IsPrimaryButtonEnabled = true,
                        PrimaryButtonText = "确定",
                        CloseButtonText = "取消"
                    };

                    ContentDialogResult contentDialogResult = await contentDialog.ShowAsync();

                    if (contentDialogResult == ContentDialogResult.Primary)
                        warningResult = true;
                });

                if (warningResult == false)
                    return;
            }

            bool success = true;
            string result = "";

            int currentPatcher = 0;
            int currentPatcherCount = patcherList.Count;

            foreach (var patcher in patcherList)
            {
                string mirrorsUrl = patcher.assets.Find(q => q.name == "mirrors.json").browser_download_url;
                string mirrorsString = await utilsHttpClient.GetStringAsync(mirrorsUrl);
                List<Mirrors> mirrors = JsonSerializer.Deserialize<List<Mirrors>>(mirrorsString);
                string link = null;

                foreach (Mirrors mirror in mirrors)
                {
                    if (mirror.Link.Contains("gofile.io"))
                    {
                        link = mirror.Link;
                        break;
                    }

                    if (mirror.Link.Contains("mega.nz"))
                    {
                        link = mirror.Link;
                        break;
                    }

                    if (mirror.Link.Contains("dev.sp-tarkov"))
                    {
                        link = mirror.Link;
                        break;
                    }
                }

                if (link == null)
                {
                    Loggy.LogToFile("DownloadPatcher: link is 'null'");
                    return;
                }

                success = await DownloadFile("Patcher.zip", App.ManagerConfig.InstallPath, link, true);

                if (success == false)
                {
                    //todo: proper error message
                    break;
                }

                ExtractArchive(App.ManagerConfig.InstallPath + @"\Patcher.zip", App.ManagerConfig.InstallPath);

                mainQueue.TryEnqueue(() =>
                {
                    window.actionPanel.Visibility = Visibility.Visible;
                    window.actionProgressRing.Visibility = Visibility.Visible;
                    window.actionProgressBar.Visibility = Visibility.Collapsed;
                    window.actionTextBlock.Text = "将补丁程序文件复制到根目录";
                });

                var patcherDir = Directory.GetDirectories(App.ManagerConfig.InstallPath, "Patcher*").First();

                await CloneDirectory(patcherDir, App.ManagerConfig.InstallPath);
                Directory.Delete(patcherDir, true);

                mainQueue.TryEnqueue(() =>
                {
                    window.actionTextBlock.Text = "运行补丁程序";
                });

                result = await RunPatcher();

                if (result != "Patcher was successful." || result == null)
                {
                    break;
                }
            }

            if (result != "Patcher was successful." || result == null)
            {
                mainQueue.TryEnqueue(async () =>
                {
                    window.actionPanel.Visibility = Visibility.Collapsed;
                    window.actionProgressRing.Visibility = Visibility.Collapsed;
                    window.actionProgressBar.Visibility = Visibility.Visible;
                    window.actionTextBlock.Text = "";

                    ContentDialog contentDialog = new()
                    {
                        XamlRoot = window.Content.XamlRoot,
                        Title = "修补错误",
                        Content = $"补丁程序运行失败：/n{result}/n/n确保您的文件夹是干净的!",
                        CloseButtonText = "确定"
                    };

                    await contentDialog.ShowAsync();
                });
                return;
            }
            else
            {
                mainQueue.TryEnqueue(async () =>
                {
                    window.actionPanel.Visibility = Visibility.Collapsed;
                    window.actionProgressRing.Visibility = Visibility.Collapsed;
                    window.actionProgressBar.Visibility = Visibility.Visible;
                    window.actionTextBlock.Text = "";

                    ContentDialog contentDialog = new()
                    {
                        XamlRoot = window.Content.XamlRoot,
                        Title = "修补完成",
                        Content = result,
                        CloseButtonText = "确定"
                    };

                    await contentDialog.ShowAsync();
                });
                return;
            }
        }

        /// <summary>
        /// Extracts a Zip archive using SharpCompress
        /// </summary>
        /// <param name="filePath">The file to extract</param>
        /// <param name="destination">The destination to extract to</param>
        /// <returns></returns>
        public static void ExtractArchive(string filePath, string destination)
        {
            var window = App.m_window as MainWindow;
            DispatcherQueue mainQueue = window.DispatcherQueue;

            try
            {
                using ZipArchive zip = ZipArchive.Open(filePath);
                var files = zip.Entries;

                var totalFiles = files.Where(file => !file.IsDirectory);
                int completed = 0;

                // Show Action Panel
                mainQueue.TryEnqueue(() =>
                {
                    window.actionPanel.Visibility = Visibility.Visible;
                    window.actionProgressRing.Visibility = Visibility.Visible;
                });

                var progress = new Progress<float>((prog) => { mainQueue.TryEnqueue(() => { window.actionProgressBar.Value = (int)Math.Floor(prog); }); });
                IProgress<float> progressBar = progress;

                foreach (var file in files)
                {
                    if (file.IsDirectory == false)
                    {
                        file.WriteToDirectory(destination, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });

                        completed++;
                        progressBar.Report(((float)completed / totalFiles.Count()) * 100);
                        mainQueue.TryEnqueue(() => { window.actionTextBlock.Text = $"释放文件 {file.Key.Split("/").Last()} ({completed}/{totalFiles.Count()})"; });
                    }
                }

                mainQueue.TryEnqueue(() =>
                {
                    window.actionPanel.Visibility = Visibility.Collapsed;
                    window.actionProgressRing.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                Loggy.LogToFile("ExtractFile: Error when opening Archive: " + ex.Message + "\n" + ex);
            }
        }

        /// <summary>
        /// Runs the downgrade patcher
        /// </summary>
        /// <returns>string with result</returns>
        private async static Task<string> RunPatcher()
        {
            Loggy.LogToFile("Starting Patcher");
            if (!File.Exists(App.ManagerConfig.InstallPath + @"\Patcher.exe"))
                return null;

            Process patcherProcess = new()
            {
                StartInfo = new()
                {
                    FileName = App.ManagerConfig.InstallPath + @"\Patcher.exe",
                    WorkingDirectory = App.ManagerConfig.InstallPath,
                    Arguments = "autoclose"
                },
                EnableRaisingEvents = true
            };
            patcherProcess.Start();
            await patcherProcess.WaitForExitAsync();

            string patcherResult = null;

            switch (patcherProcess.ExitCode)
            {
                case 0:
                    {
                        patcherResult = "Patcher was closed.";
                        break;
                    }
                case 10:
                    {
                        patcherResult = "Patcher was successful.";
                        if (File.Exists(App.ManagerConfig.InstallPath + @"\Patcher.exe"))
                            File.Delete(App.ManagerConfig.InstallPath + @"\Patcher.exe");

                        if (File.Exists(App.ManagerConfig.InstallPath + @"\Patcher.log"))
                            File.Delete(App.ManagerConfig.InstallPath + @"\Patcher.log");

                        if (Directory.Exists(App.ManagerConfig.InstallPath + @"\Aki_Patches"))
                            Directory.Delete(App.ManagerConfig.InstallPath + @"\Aki_Patches", true);

                        break;
                    }
                case 11:
                    {
                        patcherResult = "Could not find 'EscapeFromTarkov.exe'.";
                        break;
                    }
                case 12:
                    {
                        patcherResult = "'Aki_Patches' is missing.";
                        break;
                    }
                case 13:
                    {
                        patcherResult = "Install folder is missing a file.";
                        break;
                    }
                case 14:
                    {
                        patcherResult = "Install folder is missing a folder.";
                        break;
                    }
                case 15:
                    {
                        patcherResult = "Patcher failed.";
                        break;
                    }
                default:
                    {
                        patcherResult = "Unknown error.";
                        break;
                    }

            }
            Loggy.LogToFile("RunPatcher: " + patcherResult);
            return patcherResult;
        }

        /// <summary>
        /// Installs the selected Neko version
        /// </summary>
        /// <param name="selectedVersion">The <see cref="GithubRelease"/> to install</param>
        /// <returns></returns>
        public async static Task InstallSIT(GithubRelease selectedVersion)
        {
            var window = App.m_window as MainWindow;
            DispatcherQueue mainQueue = window.DispatcherQueue;

            if (string.IsNullOrEmpty(App.ManagerConfig.InstallPath))
            {
                await Utils.ShowInfoBar("错误", "未设置安装路径。在设置中配置.", InfoBarSeverity.Error);
                return;
            }

            try
            {
                if (selectedVersion == null)
                {
                    Loggy.LogToFile("InstallSIT: selectVersion is 'null'");
                    return;
                }

                if (File.Exists(App.ManagerConfig.InstallPath + @"\EscapeFromTarkov_BE.exe"))
                {
                    await CleanUpEFTDirectory();
                }

                if (File.Exists(App.ManagerConfig.InstallPath + @"\SITLauncher\CoreFiles\StayInTarkov-Release.zip"))
                    File.Delete(App.ManagerConfig.InstallPath + @"\SITLauncher\CoreFiles\StayInTarkov-Release.zip");


                if (App.ManagerConfig.TarkovVersion != selectedVersion.body)
                {
                    await Task.Run(() => DownloadAndRunPatcher(selectedVersion.body));
                    CheckEFTVersion(App.ManagerConfig.InstallPath);
                }

                if (!Directory.Exists(App.ManagerConfig.InstallPath + @"\SITLauncher\CoreFiles"))
                    Directory.CreateDirectory(App.ManagerConfig.InstallPath + @"\SITLauncher\CoreFiles");

                if (!Directory.Exists(App.ManagerConfig.InstallPath + @"\SITLauncher\Backup\CoreFiles"))
                    Directory.CreateDirectory(App.ManagerConfig.InstallPath + @"\SITLauncher\Backup\CoreFiles");

                if (!Directory.Exists(App.ManagerConfig.InstallPath + @"\BepInEx\plugins"))
                {
                    await DownloadFile("BepInEx5.zip", App.ManagerConfig.InstallPath + @"\SITLauncher", "https://github.com/BepInEx/BepInEx/releases/download/v5.4.22/BepInEx_x64_5.4.22.0.zip", true);
                    ExtractArchive(App.ManagerConfig.InstallPath + @"\SITLauncher\BepInEx5.zip", App.ManagerConfig.InstallPath);
                    Directory.CreateDirectory(App.ManagerConfig.InstallPath + @"\BepInEx\plugins");
                }

                //We don't use index as they might be different from version to version
                string releaseZipUrl = selectedVersion.assets.Find(q => q.name == "StayInTarkov-Release.zip").browser_download_url;

                await DownloadFile("StayInTarkov-Release.zip", App.ManagerConfig.InstallPath + @"\SITLauncher\CoreFiles", releaseZipUrl, true);

                ExtractArchive(App.ManagerConfig.InstallPath + @"\SITLauncher\CoreFiles\StayInTarkov-Release.zip", App.ManagerConfig.InstallPath + @"\SITLauncher\CoreFiles\");

                if (File.Exists(App.ManagerConfig.InstallPath + @"\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll"))
                    File.Copy(App.ManagerConfig.InstallPath + @"\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll", App.ManagerConfig.InstallPath + @"\SITLauncher\Backup\CoreFiles\Assembly-CSharp.dll", true);
                File.Copy(App.ManagerConfig.InstallPath + @"\SITLauncher\CoreFiles\StayInTarkov-Release\Assembly-CSharp.dll", App.ManagerConfig.InstallPath + @"\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll", true);

                File.Copy(App.ManagerConfig.InstallPath + @"\SITLauncher\CoreFiles\StayInTarkov-Release\StayInTarkov.dll", App.ManagerConfig.InstallPath + @"\BepInEx\plugins\StayInTarkov.dll", true);

                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Neko.EFT.Manager.X.Resources.Aki.Common.dll"))
                {
                    using (var file = new FileStream(App.ManagerConfig.InstallPath + @"\EscapeFromTarkov_Data\Managed\Aki.Common.dll", FileMode.Create, FileAccess.Write))
                    {
                        resource.CopyTo(file);
                    }
                }

                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Neko.EFT.Manager.X.Resources.Aki.Reflection.dll"))
                {
                    using (var file = new FileStream(App.ManagerConfig.InstallPath + @"\EscapeFromTarkov_Data\Managed\Aki.Reflection.dll", FileMode.Create, FileAccess.Write))
                    {
                        resource.CopyTo(file);
                    }
                }

                // Run on UI thread to prevent System.InvalidCastException, WinUI bug yikes
                mainQueue.TryEnqueue(() =>
                {
                    CheckSITVersion(App.ManagerConfig.InstallPath);
                });

                await ShowInfoBar("安装", "SIT安装完成.", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowInfoBarWithLogButton("安装错误", "安装过程中遇到错误.", InfoBarSeverity.Error, 10);

                Loggy.LogToFile("Install Neko: " + ex.Message + "\n" + ex);

                return;
            }
        }

        /// <summary>
        /// Opens the launcher log
        /// </summary>
        public static void OpenLauncherLog()
        {
            string filePath = AppContext.BaseDirectory + @"Launcher.log";

            if (File.Exists(filePath))
            {
                Process.Start("explorer.exe", filePath);
            }
        }

        /// <summary>
        /// Show a simple native toast notification and removes it after 5 seconds
        /// </summary>
        /// <param name="title">The title of the notification</param>
        /// <param name="content">The content of the notification</param>
        public static async void ShowSimpleNotification(string title, string content)
        {
            AppNotification simpleNotification = new AppNotificationBuilder()
                .AddText(title)
                .AddText(content)
                .BuildNotification();

            AppNotificationManager.Default.Show(simpleNotification);

            await Task.Delay(TimeSpan.FromSeconds(5));

            if (simpleNotification?.Id != null)
            {
                await AppNotificationManager.Default.RemoveByIdAsync(simpleNotification.Id);
            }
        }

        /// <summary>
        /// Shows the InfoBar of the main window
        /// </summary>
        /// <param name="title">Title of the message</param>
        /// <param name="message">The message to show</param>
        /// <param name="severity">The <see cref="InfoBarSeverity"/> to display</param>
        /// <param name="delay">The delay (in seconds) before removing the InfoBar</param>
        /// <returns></returns>
        private static InfoBar _currentInfoBar;

        public static async Task<InfoBar> ShowInfoBar(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational, int delay = 3)
        {
            MainWindow window = App.m_window as MainWindow;

            // 隐藏当前显示的 InfoBar
            if (_currentInfoBar != null)
            {
                HideInfoBar(_currentInfoBar, window);
            }

            // 创建新的 InfoBar
            InfoBar infoBar = null;

            if (window.DispatcherQueue.HasThreadAccess)
            {
                infoBar = new InfoBar
                {
                    Title = title,
                    Message = message,
                    Severity = severity,
                    IsOpen = true
                };

                window.InfoBarStackPanel.Children.Add(infoBar);
            }
            else
            {
                await window.DispatcherQueue.EnqueueAsync(async () =>
                {
                    infoBar = new InfoBar
                    {
                        Title = title,
                        Message = message,
                        Severity = severity,
                        IsOpen = true
                    };

                    window.InfoBarStackPanel.Children.Add(infoBar);
                });
            }

            // 更新 _currentInfoBar
            _currentInfoBar = infoBar;

            // 延迟一段时间后隐藏 InfoBar
            await Task.Delay(TimeSpan.FromSeconds(delay));
            HideInfoBar(infoBar, window);

            // 返回新的 InfoBar
            return infoBar;
        }

        public static void HideInfoBar(InfoBar infoBar, MainWindow window)
        {
            if (window.DispatcherQueue.HasThreadAccess)
            {
                window.DispatcherQueue.TryEnqueue(() =>
                {
                    window.InfoBarStackPanel.Children.Remove(infoBar);
                    _currentInfoBar = null;
                });
            }
            else
            {
                _ = window.DispatcherQueue.TryEnqueue(() =>
                {
                    window.InfoBarStackPanel.Children.Remove(infoBar);
                    _currentInfoBar = null;
                });
            }
        }


        //public static async Task<InfoBar> ShowInfoBarSM(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational, int delay = 3)
        //{
        //    SPTServerManager sptwindow = App.spt_window as SPTServerManager;

        //    // 隐藏当前显示的 InfoBar
        //    if (_currentInfoBar != null)
        //    {
        //        HideInfoBarSM(_currentInfoBar, sptwindow);
        //    }

        //    // 创建新的 InfoBar
        //    InfoBar infoBar = null;

        //    if (sptwindow.DispatcherQueue.HasThreadAccess)
        //    {
        //        infoBar = new InfoBar
        //        {
        //            Title = title,
        //            Message = message,
        //            Severity = severity,
        //            IsOpen = true
        //        };

        //        sptwindow.InfoBarStackPanelS.Children.Add(infoBar);
        //    }
        //    else
        //    {
        //        await sptwindow.DispatcherQueue.EnqueueAsync(async () =>
        //        {
        //            infoBar = new InfoBar
        //            {
        //                Title = title,
        //                Message = message,
        //                Severity = severity,
        //                IsOpen = true
        //            };

        //            sptwindow.InfoBarStackPanelS.Children.Add(infoBar);
        //        });
        //    }

        //    // 更新 _currentInfoBar
        //    _currentInfoBar = infoBar;

        //    // 延迟一段时间后隐藏 InfoBar
        //    await Task.Delay(TimeSpan.FromSeconds(delay));
        //    HideInfoBarSM(infoBar, sptwindow);

        //    // 返回新的 InfoBar
        //    return infoBar;
        //}

        //public static void HideInfoBarSM(InfoBar infoBar, SPTServerManager sptwindow)
        //{
        //    if (sptwindow.DispatcherQueue.HasThreadAccess)
        //    {
        //        sptwindow.DispatcherQueue.TryEnqueue(() =>
        //        {
        //            sptwindow.InfoBarStackPanelS.Children.Remove(infoBar);
        //            _currentInfoBar = null;
        //        });
        //    }
        //    else
        //    {
        //        _ = sptwindow.DispatcherQueue.TryEnqueue(() =>
        //        {
        //            sptwindow.InfoBarStackPanelS.Children.Remove(infoBar);
        //            _currentInfoBar = null;
        //        });
        //    }
        //}


        public static void ShowSystemNotification(string title, string message)
        {
            // 创建 Toast 通知模板
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);

            // 设置标题和消息
            XmlNodeList textElements = toastXml.GetElementsByTagName("text");
            textElements[0].AppendChild(toastXml.CreateTextNode(title));
            textElements[1].AppendChild(toastXml.CreateTextNode(message));

            // 创建 Toast 通知对象
            ToastNotification toast = new ToastNotification(toastXml);

            // 设置通知的优先级（可选）
            toast.Priority = ToastNotificationPriority.High;

            // 显示 Toast 通知
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        /// <summary>
        /// Shows the InfoBar of the main window with an Open Log button
        /// </summary>
        /// <param name="title">Title of the message</param>
        /// <param name="message">The message to show</param>
        /// <param name="severity">The <see cref="InfoBarSeverity"/> to display</param>
        /// <param name="delay">The delay (in seconds) before removing the InfoBar</param>
        /// <returns></returns>
        public static async void ShowInfoBarWithLogButton(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational, int delay = 5)
        {
            MainWindow window = App.m_window as MainWindow;

            if (window.DispatcherQueue.HasThreadAccess)
            {
                window.DispatcherQueue.TryEnqueue(async () =>
                {
                    Button infoBarButton = new() { Content = "Open Log" };
                    infoBarButton.Click += (e, s) =>
                    {
                        OpenLauncherLog();
                    };

                    InfoBar infoBar = new()
                    {
                        Title = title,
                        Message = message,
                        Severity = severity,
                        IsOpen = true,
                        ActionButton = infoBarButton
                    };

                    window.InfoBarStackPanel.Children.Add(infoBar);

                    await Task.Delay(TimeSpan.FromSeconds(delay));

                    window.InfoBarStackPanel.Children.Remove(infoBar);
                });
            }
        }

        public static async void SITUpdateInfo()
        {
            
            MainWindow window = App.m_window as MainWindow;
            window.SITUpdateInfoBar.Title = "更新:";
            window.SITUpdateInfoBar.Message = "SIT组件有更新可用.";
            window.SITUpdateInfoBar.Severity = InfoBarSeverity.Informational;

            window.SITUpdateInfoBar.IsOpen = true;

            await Task.Delay(TimeSpan.FromSeconds(30));

            window.SITUpdateInfoBar.IsOpen = false;


        }

        public enum UpdateResult
        {
            Success,
            NetworkError,
            OtherError
        }


        public static async Task<UpdateResult> SITUpdateVoid()
        {
            try
            {
                string latestVersion = GetGiteeReleaseVersion();

                if (latestVersion == "error")
                {
                    //await Utils.ShowInfoBar("发生异常", $"发生网络错误", InfoBarSeverity.Error);
                    return UpdateResult.NetworkError;
                }

                string LocationVersion = GetLocationVersion();
                MainWindow window = App.m_window as MainWindow;

                // 继续检查本地的版本是否与最新的版本匹配
                DispatcherQueue queue = DispatcherQueue.GetForCurrentThread();
                if (queue != null)
                {
                    queue.TryEnqueue(async () =>
                    {
                        SITUpdateInfo(); // 注意这里使用 await 等待异步操作完成
                    });
                }


                return UpdateResult.Success;
            }
            catch (Exception ex)
            {
                // 在这里显示异常信息给用户
                //await Utils.ShowInfoBar("发生异常", $"发生错误: {ex.Message}", InfoBarSeverity.Error);
                return UpdateResult.OtherError;
            }
        }






        public static string GetLocationVersion()
        {
            string installLocation = App.ManagerConfig.InstallPath; // "J:\\EFT\\GAMES\\Client.0.13.5.3.26535"
            string directory = installLocation; // "J:\\EFT\\GAMES\\Client.0.13.5.3.26535"
            string dllPath = Path.Combine(directory, "BepInEx\\plugins\\StayInTarkov.dll"); // "J:\\EFT\\GAMES\\Client.0.13.5.3.26535\\BepInEx\\plugins\\Neko.Core.dll"

            // 这里返回释放位置的 DLL 文件的版本号
            return FileVersionInfo.GetVersionInfo(dllPath).FileVersion;
        }

        public static string GetGiteeReleaseVersion()
        {
            try
            {
                string url = "https://gitee.com/api/v5/repos/Neko17-Offical/stayintarkov/releases/latest";
                string access_token = "b689715aaa2e3afe91ada582ee51237a";

                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    client.Headers.Add("Authorization", "token " + access_token);
                    string json = client.DownloadString(url);
                    Match match = Regex.Match(json, "\"tag_name\":\"(.*?)\"");

                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                    else
                    {
                        return "error";
                    }
                }
            }
            catch (Exception ex)
            {
                return "error";
            }
        }


        public enum SITUpdatedResult
        {
            Success,
            NetworkError,
            OtherError,
            WebError
        }


        public static async Task<SITUpdatedResult> UpdateDllFromGitee(string dllPath)
        {
            try
            {
                string url = "https://gitee.com/api/v5/repos/Neko17-Offical/stayintarkov/releases/latest"; // 你的仓库在 Gitee 上的 API URL
                string access_token = "b689715aaa2e3afe91ada582ee51237a"; // 你的 Gitee Access Token

                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    client.Headers.Add("Authorization", "token " + access_token);

                    string json = await client.DownloadStringTaskAsync(url);

                    // 使用正则表达式从 JSON 中提取下载 URL
                    Match match = Regex.Match(json, "\"browser_download_url\":\"(.*?)\"");
                    if (match.Success)
                    {
                        string downloadUrl = match.Groups[1].Value;
                        await client.DownloadFileTaskAsync(downloadUrl, dllPath);

                        return SITUpdatedResult.Success;
                    }
                    else
                    {
                        return SITUpdatedResult.OtherError;
                    }
                }
            }
            catch (Exception ex)
            {

                return SITUpdatedResult.NetworkError;
            }
            //catch (WebException ex)
            //{
            //    return SITUpdatedResult.WebError;
            //}
        }



        public static async void SITUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            string LatestVersion = GetGiteeReleaseVersion();
            string installLocation = App.ManagerConfig.InstallPath;
            string directory = installLocation;
            string dllPath = Path.Combine(directory, "BepInEx\\plugins\\StayInTarkov.dll");

            UpdateResult updateResult = (UpdateResult)await Utils.UpdateDllFromGitee(dllPath);
            MainWindow window = App.m_window as MainWindow;
            window.SITUpdateInfoBar.IsOpen = false;
            //await Task.Delay(TimeSpan.FromSeconds(2));

            // Check for network error by waiting for a certain duration
            if (updateResult == UpdateResult.Success)
            {
                await Utils.ShowInfoBar("SIT组件更新", $"SIT组件已更新至{LatestVersion}.", InfoBarSeverity.Success);
            }
            else if (updateResult == UpdateResult.NetworkError && await CheckNetworkErrorContinuously())
            {
                await Utils.ShowInfoBar("SIT组件更新", "网络错误，更新失败", InfoBarSeverity.Error);
            }
            else if (updateResult == UpdateResult.OtherError)
            {
                await Utils.ShowInfoBar("SIT组件更新", "其他错误，更新失败", InfoBarSeverity.Error);
            }
            //else if (updateResult == UpdateResult.WebError && await CheckNetworkErrorContinuously())
            //{
            //    await Utils.ShowInfoBar("SIT组件更新", "其他错误，更新失败", InfoBarSeverity.Error);
            //}

        }


        public static async Task<bool> CheckNetworkErrorContinuously()
        {
            // Implement logic to check network error continuously for a certain duration
            // Return true if network error persists for a specified duration, otherwise return false
            // Example: check network connectivity every few seconds for a specified duration
            int durationInSeconds = 30;
            int intervalInSeconds = 5;
            for (int i = 0; i < durationInSeconds / intervalInSeconds; i++)
            {
                if (!IsNetworkConnected())
                {
                    await Task.Delay(TimeSpan.FromSeconds(intervalInSeconds));
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsNetworkConnected()
        {
            // Implement logic to check network connectivity
            // Return true if network is connected, otherwise return false
            // Example: use System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()
            return System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
        }
    }
    public static class GlobalState
    {
        public static List<string> ConsoleOutput { get; set; } = new List<string>();
    }

}
