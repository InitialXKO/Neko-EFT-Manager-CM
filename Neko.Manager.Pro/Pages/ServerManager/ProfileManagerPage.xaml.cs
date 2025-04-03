using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;

namespace Neko.EFT.Manager.X.Pages.ServerManager
{
    public sealed partial class ProfileManagerPage : Page
    {
        private readonly ObservableCollection<Profile> _profiles = new();
        private readonly ObservableCollection<Backup> _backups = new();
        private bool _isLoading;

        public ProfileManagerPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
            Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            ProfileListView.ItemsSource = _profiles;
            BackupListView.ItemsSource = _backups;
            await LoadProfilesAsync();
        }

        private async Task LoadProfilesAsync()
        {
            if (_isLoading) return;

            try
            {
                ToggleLoadingState(true);
                _profiles.Clear();

                var profilesPath = Path.Combine(App.ManagerConfig.AkiServerPath, "user", "profiles");
                if (!Directory.Exists(profilesPath)) return;

                var profiles = await Task.Run(() =>
                {
                    var result = new List<Profile>();
                    foreach (var file in Directory.EnumerateFiles(profilesPath, "*.json"))
                    {
                        try
                        {
                            using var stream = File.OpenRead(file);
                            using var doc = JsonDocument.Parse(stream);
                            var root = doc.RootElement;

                            var profile = new Profile
                            {
                                FilePath = file,
                                LastModified = File.GetLastWriteTime(file).ToString("G")
                            };

                            // 解析JSON结构
                            if (root.TryGetProperty("info", out var info))
                            {
                                profile.Id = info.TryGetProperty("id", out var id) ? id.GetString() : "";
                                profile.Username = info.TryGetProperty("username", out var username) ? username.GetString() : "Unknown";
                            }

                            if (root.TryGetProperty("characters", out var characters))
                            {
                                // PMC解析
                                if (characters.TryGetProperty("pmc", out var pmc) &&
                                    pmc.TryGetProperty("Info", out var pmcInfo))
                                {
                                    profile.PmcNickname = pmcInfo.TryGetProperty("Nickname", out var nickname)
                                        ? nickname.GetString()
                                        : "Unnamed PMC";
                                }

                                // Scav解析
                                if (characters.TryGetProperty("scav", out var scav) &&
                                    scav.TryGetProperty("Info", out var scavInfo))
                                {
                                    profile.ScavNickname = scavInfo.TryGetProperty("Nickname", out var scavNickname)
                                        ? scavNickname.GetString()
                                        : "Unnamed Scav";
                                }
                            }
                            result.Add(profile);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error loading {file}: {ex.Message}");
                        }
                    }
                    return result;
                });

                await DispatcherQueue.EnqueueAsync(() =>
                {
                    foreach (var profile in profiles)
                    {
                        _profiles.Add(profile);
                    }
                });
            }
            finally
            {
                ToggleLoadingState(false);
                SetButtonStates();
            }
        }

        private async void BackupProfile_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileListView.SelectedItem is not Profile selectedProfile) return;

            try
            {
                ToggleLoadingState(true);

                var backupRoot = Path.Combine(App.ManagerConfig.AkiServerPath, "user", "profiles-backup");
                var profileBackupDir = Path.Combine(backupRoot, $"{selectedProfile.Id}backup");

                Directory.CreateDirectory(profileBackupDir);

                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var backupName = $"{selectedProfile.Username}-{timestamp}.zip";
                var backupPath = Path.Combine(profileBackupDir, backupName);

                await Task.Run(() =>
                {
                    using var zip = ZipFile.Open(backupPath, ZipArchiveMode.Create);
                    zip.CreateEntryFromFile(selectedProfile.FilePath, Path.GetFileName(selectedProfile.FilePath));
                });

                LoadBackupsForSelectedProfile();
                ShowToast("备份成功", $"{backupName} 已创建");
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"备份失败: {ex.Message}");
            }
            finally
            {
                ToggleLoadingState(false);
            }
        }

        private async void RestoreProfile_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileListView.SelectedItem is not Profile selectedProfile ||
                BackupListView.SelectedItem is not Backup selectedBackup) return;

            var confirm = await ShowConfirmationDialog(
                "确认还原",
                $"确定要还原备份 {selectedBackup.FileName} 到当前存档吗？");

            if (!confirm) return;

            try
            {
                ToggleLoadingState(true);

                var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                await Task.Run(() =>
                {
                    using var zip = ZipFile.OpenRead(selectedBackup.FilePath);
                    zip.ExtractToDirectory(tempDir);
                });

                var extractedFile = Path.Combine(tempDir, Path.GetFileName(selectedProfile.FilePath));
                if (File.Exists(extractedFile))
                {
                    await Task.Run(() => File.Copy(extractedFile, selectedProfile.FilePath, true));
                    await LoadProfilesAsync();
                    ShowToast("还原成功", $"{selectedBackup.FileName} 已还原");
                }

                Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"还原失败: {ex.Message}");
            }
            finally
            {
                ToggleLoadingState(false);
            }
        }

        private async void DeleteBackup_Click(object sender, RoutedEventArgs e)
        {
            if (BackupListView.SelectedItem is not Backup selectedBackup) return;

            var confirm = await ShowConfirmationDialog(
                "确认删除",
                $"确定要永久删除备份 {selectedBackup.FileName} 吗？");

            if (!confirm) return;

            try
            {
                await Task.Run(() => File.Delete(selectedBackup.FilePath));
                _backups.Remove(selectedBackup);
                ShowToast("删除成功", $"{selectedBackup.FileName} 已删除");
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"删除失败: {ex.Message}");
            }
        }

        private async void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileListView.SelectedItem is not Profile selectedProfile) return;

            // 确认对话框
            var confirmDialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = "确认删除存档",
                Content = $"确定要永久删除存档【{selectedProfile.Username}】吗？\n" +
                         $"PMC: {selectedProfile.PmcNickname}\n" +
                         $"Scav: {selectedProfile.ScavNickname}",
                PrimaryButtonText = "永久删除",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await confirmDialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            try
            {
                ToggleLoadingState(true);

                // 异步删除文件
                await Task.Run(() =>
                {
                    if (File.Exists(selectedProfile.FilePath))
                    {
                        File.Delete(selectedProfile.FilePath);
                    }
                });

                // 更新UI
                await DispatcherQueue.EnqueueAsync(() =>
                {
                    _profiles.Remove(selectedProfile);
                    _backups.Clear();
                    SetButtonStates();
                });

                // 显示操作结果
                ShowToast("删除成功", $"已删除存档: {selectedProfile.Username}");
            }
            catch (Exception ex)
            {
                await DispatcherQueue.EnqueueAsync(() =>
                {
                    new ContentDialog
                    {
                        XamlRoot = Content.XamlRoot,
                        Title = "删除失败",
                        Content = $"无法删除存档:\n{ex.Message}",
                        CloseButtonText = "确定"
                    }.ShowAsync();
                });
            }
            finally
            {
                ToggleLoadingState(false);
            }
        }

        private async void LoadBackupsForSelectedProfile()
        {
            if (ProfileListView.SelectedItem is not Profile selectedProfile) return;

            try
            {
                BackupLoadingRing.IsActive = true;
                _backups.Clear();

                var backups = await Task.Run(() =>
                {
                    var result = new List<Backup>();
                    var backupDir = Path.Combine(
                        App.ManagerConfig.AkiServerPath,
                        "user",
                        "profiles-backup",
                        $"{selectedProfile.Id}backup");

                    if (!Directory.Exists(backupDir)) return result;

                    foreach (var file in Directory.EnumerateFiles(backupDir, "*.zip"))
                    {
                        result.Add(new Backup
                        {
                            FileName = Path.GetFileName(file),
                            CreatedDate = File.GetCreationTime(file).ToString("G"),
                            FilePath = file
                        });
                    }
                    return result;
                });

                await DispatcherQueue.EnqueueAsync(() =>
                {
                    foreach (var backup in backups)
                    {
                        _backups.Add(backup);
                    }
                });
            }
            finally
            {
                BackupLoadingRing.IsActive = false;
                SetButtonStates();
            }
        }

        private void SetButtonStates()
        {
            DeleteProfileButton.IsEnabled = ProfileListView.SelectedItem != null;
            BackupProfileButton.IsEnabled = ProfileListView.SelectedItem != null;
            RestoreProfileButton.IsEnabled = ProfileListView.SelectedItem != null &&
                                            BackupListView.SelectedItem != null;
            DeleteBackupButton.IsEnabled = BackupListView.SelectedItem != null;
        }

        private void ToggleLoadingState(bool isLoading)
        {
            _isLoading = isLoading;
            LoadingRing.IsActive = isLoading;
            LoadingText.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            MainGrid.Opacity = isLoading ? 0.5 : 1;
            MainGrid.IsHitTestVisible = !isLoading;
        }

        private async Task<bool> ShowConfirmationDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = title,
                Content = message,
                PrimaryButtonText = "确认",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        private async Task ShowErrorDialog(string message)
        {
            await DispatcherQueue.EnqueueAsync(() =>
            {
                new ContentDialog
                {
                    XamlRoot = Content.XamlRoot,
                    Title = "错误",
                    Content = message,
                    CloseButtonText = "确定"
                }.ShowAsync();
            });
        }

        private void ShowToast(string title, string message)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ToastNotificationHelper.ShowNotification(
                    title,
                    message,
                    "确定",
                    _ => { },
                    "通知",
                    "错误");
            });
        }

        private void OnProfileSelectionChanged(object sender, SelectionChangedEventArgs e)
            => LoadBackupsForSelectedProfile();

        private void OnBackupSelectionChanged(object sender, SelectionChangedEventArgs e)
            => SetButtonStates();
    }

    public class Profile
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string PmcNickname { get; set; }
        public string ScavNickname { get; set; }
        public string LastModified { get; set; }
        public string FilePath { get; set; }
    }

    public class Backup
    {
        public string FileName { get; set; }
        public string CreatedDate { get; set; }
        public string FilePath { get; set; }
    }
}
