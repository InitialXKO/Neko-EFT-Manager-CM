using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FileSelector;

namespace FileSelector;

public class ManagerConfig : PropertyChangedBase
{
    private static readonly string BaseDirectory = AppContext.BaseDirectory;
    private static readonly string ConfigDirectory = Path.Combine(BaseDirectory, "UserData", "Config");
    private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "ManagerConfig.json");



    private string _installPath;
    public string InstallPath
    {
        get => _installPath;
        set => SetField(ref _installPath, value);
    }

    private string _akiServerPath;
    public string AkiServerPath
    {
        get => _akiServerPath;
        set => SetField(ref _akiServerPath, value);
    }




    private Dictionary<string, string> _installedMods = new();
    public Dictionary<string, string> InstalledMods
    {
        get => _installedMods;
        set => SetField(ref _installedMods, value);
    }



    public static void Load()
    {
        try
        {
            ManagerConfig config = new();

            // 确保目录存在
            if (!Directory.Exists(ConfigDirectory))
            {
                Directory.CreateDirectory(ConfigDirectory);
            }

            if (File.Exists(ConfigFilePath))
                config = JsonSerializer.Deserialize<ManagerConfig>(File.ReadAllText(ConfigFilePath));

            App.ManagerConfig = config;
        }
        catch (Exception ex)
        {
            
        }
    }

    public static void Save()
    {
        string configDir = Path.Combine(AppContext.BaseDirectory, "UserData", "Config");
        string configFilePath = Path.Combine(configDir, "ManagerConfig.json");

        try
        {
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            ManagerConfig existingConfig;

            // 读取现有配置
            if (File.Exists(configFilePath))
            {
                string json = File.ReadAllText(configFilePath);
                existingConfig = JsonSerializer.Deserialize<ManagerConfig>(json) ?? new ManagerConfig();
            }
            else
            {
                existingConfig = new ManagerConfig();
            }

            // 更新 InstallPath 和 AkiServerPath
            if (!string.IsNullOrEmpty(App.ManagerConfig.InstallPath))
            {
                existingConfig.InstallPath = App.ManagerConfig.InstallPath;
            }

            if (!string.IsNullOrEmpty(App.ManagerConfig.AkiServerPath))
            {
                existingConfig.AkiServerPath = App.ManagerConfig.AkiServerPath;
            }

            // 保存更新后的配置
            string updatedJson = JsonSerializer.Serialize(existingConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configFilePath, updatedJson);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error saving config file: " + ex.Message);
        }
    }






}

public class PropertyChangedBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value,
    [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
