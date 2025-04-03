using Neko.EFT.Manager.X.Classes;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

public static class ConfigHelper
{
    private static string ConfigPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "appconfig.json");

    public static async Task<AppConfig> LoadConfigAsync()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = await File.ReadAllTextAsync(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json);
            }
            else
            {
                // File does not exist, create a default config
                var defaultConfig = new AppConfig();
                await SaveConfigAsync(defaultConfig);  // Save the default config
                return defaultConfig;
            }
        }
        catch (Exception ex)
        {
            // Handle or log the exception as needed
            Console.WriteLine($"Error loading config: {ex.Message}");
            return new AppConfig();
        }
    }

    public static async Task SaveConfigAsync(AppConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config);
            await File.WriteAllTextAsync(ConfigPath, json);
        }
        catch (Exception ex)
        {
            // Handle or log the exception as needed
            Console.WriteLine($"Error saving config: {ex.Message}");
        }
    }
}
