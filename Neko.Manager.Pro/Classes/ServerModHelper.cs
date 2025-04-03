using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using System.Text.Json.Serialization;

namespace Neko.EFT.Manager.X.Classes
{
    public class ServerModsHelper
    {
        private readonly string modsDirectoryPath;
        private readonly string modsBackupDirectoryPath;
        private readonly ServerCompatibilityInfo compatibilityInfo;

        public ServerModsHelper(string installPath, string akiServerPath)
        {
            modsDirectoryPath = Path.Combine(installPath, "user", "mods");
            modsBackupDirectoryPath = Path.Combine(installPath, "user", "modbackup-off");
            compatibilityInfo = new ServerCompatibilityInfo(akiServerPath);
        }

        public async Task<ObservableCollection<ServerModInfo>> LoadMods()
        {
            var mods = new ObservableCollection<ServerModInfo>();

            var modOrder = await InitializeOrderJson();
            var allModDirectories = new List<string>();

            if (Directory.Exists(modsDirectoryPath))
            {
                allModDirectories.AddRange(Directory.GetDirectories(modsDirectoryPath));
            }

            if (Directory.Exists(modsBackupDirectoryPath))
            {
                allModDirectories.AddRange(Directory.GetDirectories(modsBackupDirectoryPath));
            }

            foreach (var modDirectory in allModDirectories.OrderBy(d => modOrder.IndexOf(Path.GetFileName(d))))
            {
                var configFilePath = Path.Combine(modDirectory, "package.json");

                if (File.Exists(configFilePath))
                {
                    var json = await File.ReadAllTextAsync(configFilePath);
                    var modInfo = JsonSerializer.Deserialize<ServerModInfo>(json);
                    if (modInfo != null)
                    {
                        modInfo.DirectoryPath = modDirectory;
                        modInfo.DisplayName = Path.GetFileName(modDirectory);
                        modInfo.CompatibilityStatus = compatibilityInfo.CheckCompatibility(modInfo);
                        mods.Add(modInfo);
                    }
                }
            }

            await CheckAndUpdateModStatusAndOrder(mods, modOrder);

            return mods;
        }

        public async Task<List<string>> InitializeOrderJson()
        {
            var orderFilePath = Path.Combine(modsDirectoryPath, "order.json");
            var modOrder = new List<string>();

            if (File.Exists(orderFilePath))
            {
                var orderFileContent = await File.ReadAllTextAsync(orderFilePath);
                if (!string.IsNullOrWhiteSpace(orderFileContent))
                {
                    var orderJson = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(orderFileContent, new JsonSerializerOptions
                    {
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        PropertyNameCaseInsensitive = true
                    });
                    modOrder = orderJson?["order"] ?? new List<string>();
                }
            }

            if (modOrder.Count == 0)
            {
                var modDirectories = Directory.GetDirectories(modsDirectoryPath).Select(Path.GetFileName).ToList();

                foreach (var modDirectory in modDirectories)
                {
                    if (!modOrder.Contains(modDirectory))
                    {
                        modOrder.Add(modDirectory);
                    }
                }

                await SaveOrderJson(modOrder);
            }

            modOrder = modOrder.Distinct().ToList();
            await SaveOrderJson(modOrder);

            return modOrder;
        }

        public async Task SaveOrderJson(List<string> modOrder)
        {
            var orderFilePath = Path.Combine(modsDirectoryPath, "order.json");
            var updatedOrderJson = new Dictionary<string, List<string>> { { "order", modOrder } };
            var updatedOrderFileContent = JsonSerializer.Serialize(updatedOrderJson, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            await File.WriteAllTextAsync(orderFilePath, updatedOrderFileContent);
        }

        public async Task SaveMod(ServerModInfo modInfo)
        {
            var configFilePath = Path.Combine(modInfo.DirectoryPath, "package.json");

            if (File.Exists(configFilePath))
            {
                var json = await File.ReadAllTextAsync(configFilePath, Encoding.UTF8);
                var existingModInfo = JsonSerializer.Deserialize<ServerModInfo>(json, new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    PropertyNameCaseInsensitive = true
                });

                if (existingModInfo != null)
                {
                    existingModInfo.IsEnabled = modInfo.IsEnabled;

                    var updatedJson = JsonSerializer.Serialize(existingModInfo, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });

                    using (var writer = new StreamWriter(configFilePath, false, new UTF8Encoding(false)))
                    {
                        await writer.WriteAsync(updatedJson);
                    }
                }
            }
        }

        public async Task ManageModStatus(ServerModInfo servermodInfo, bool enable)
        {
            var modDirectoryPath = servermodInfo.DirectoryPath;
            var targetDirectoryPath = enable ? modsDirectoryPath : modsBackupDirectoryPath;

            if (Directory.Exists(modDirectoryPath))
            {
                var newModDirectoryPath = Path.Combine(targetDirectoryPath, Path.GetFileName(modDirectoryPath));

                if (!Directory.Exists(targetDirectoryPath))
                {
                    Directory.CreateDirectory(targetDirectoryPath);
                }

                if (!Directory.Exists(newModDirectoryPath))
                {
                    Directory.Move(modDirectoryPath, newModDirectoryPath);
                    servermodInfo.IsEnabled = enable;
                    servermodInfo.DirectoryPath = newModDirectoryPath;

                    await SaveMod(servermodInfo);

                    var modOrder = (await InitializeOrderJson()).ToList();
                    if (enable)
                    {
                        if (!modOrder.Contains(Path.GetFileName(newModDirectoryPath)))
                        {
                            modOrder.Add(Path.GetFileName(newModDirectoryPath));
                        }
                    }
                    else
                    {
                        modOrder.Remove(Path.GetFileName(newModDirectoryPath));
                    }
                    await SaveOrderJson(modOrder);
                }
            }
        }

        public async Task CheckAndUpdateModStatusAndOrder(ObservableCollection<ServerModInfo> mods, List<string> modOrder)
        {
            var modDirectories = mods.Select(m => m.DirectoryPath).ToList();

            var currentOrder = modDirectories.OrderBy(d => modOrder.IndexOf(Path.GetFileName(d))).ToList();
            if (!modDirectories.SequenceEqual(currentOrder))
            {
                var orderedMods = new ObservableCollection<ServerModInfo>();
                foreach (var modDirectory in currentOrder)
                {
                    var mod = mods.FirstOrDefault(m => m.DirectoryPath == modDirectory);
                    if (mod != null)
                    {
                        orderedMods.Add(mod);
                    }
                }
                mods.Clear();
                foreach (var mod in orderedMods)
                {
                    mods.Add(mod);
                }
            }

            foreach (var mod in mods)
            {
                var shouldBeEnabled = modOrder.Contains(Path.GetFileName(mod.DirectoryPath));
                if (mod.IsEnabled != shouldBeEnabled)
                {
                    mod.IsEnabled = shouldBeEnabled;
                    await SaveMod(mod);
                }
            }
        }

        public async Task InstallMod(StorageFile file)
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            Directory.CreateDirectory(tempDirectory);

            await Task.Run(() => System.IO.Compression.ZipFile.ExtractToDirectory(file.Path, tempDirectory));

            var packageJsonPath = Directory.EnumerateFiles(tempDirectory, "package.json", SearchOption.AllDirectories).FirstOrDefault();

            if (packageJsonPath == null)
            {
                Directory.Delete(tempDirectory, true);
                throw new Exception("无效的模组文件，缺少 package.json。");
            }

            var modDirectoryName = Path.GetFileName(Path.GetDirectoryName(packageJsonPath));
            var targetModDirectory = Path.Combine(modsDirectoryPath, modDirectoryName);

            if (Directory.Exists(targetModDirectory))
            {
                Directory.Delete(targetModDirectory, true);
            }

            Directory.Move(Path.GetDirectoryName(packageJsonPath), targetModDirectory);

            Directory.Delete(tempDirectory, true);

            var modOrder = (await InitializeOrderJson()).ToList();
            modOrder.Add(modDirectoryName);
            await SaveOrderJson(modOrder);
        }
    }
}