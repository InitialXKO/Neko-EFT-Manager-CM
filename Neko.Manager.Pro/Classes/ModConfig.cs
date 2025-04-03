using System.Collections.Generic;
using System.Text.Json;
using System.IO;

namespace Neko.EFT.Manager.X.Classes
{
    public class ModConfig
    {
        public string FilePath { get; set; }
        public string CustomName { get; set; }

        public static string ConfigFilePath => Path.Combine(App.ManagerConfig.InstallPath, "modConfig.json");

        public static Dictionary<string, string> Load()
        {
            if (File.Exists(ConfigFilePath))
            {
                string json = File.ReadAllText(ConfigFilePath);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
            return new Dictionary<string, string>();
        }

        public static void Save(Dictionary<string, string> modConfigs)
        {
            string json = JsonSerializer.Serialize(modConfigs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }
    }

}
