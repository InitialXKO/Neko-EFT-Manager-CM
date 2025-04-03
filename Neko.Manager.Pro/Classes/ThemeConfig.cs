using Newtonsoft.Json;
using System;
using System.IO;

namespace Neko.EFT.Manager.X.Classes
{
    public class ThemeConfig
    {
        private const string ThemeConfigFilePath = "ThemeConfig.json";

        public string Theme { get; set; } = "Acrylic"; // 默认主题

        public static ThemeConfig Load()
        {
            try
            {
                if (File.Exists(ThemeConfigFilePath))
                {
                    string json = File.ReadAllText(ThemeConfigFilePath);
                    return JsonConvert.DeserializeObject<ThemeConfig>(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading theme config file: " + ex.Message);
            }

            return new ThemeConfig();
        }

        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this);
                File.WriteAllText(ThemeConfigFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving theme config file: " + ex.Message);
            }
        }
    }
}
