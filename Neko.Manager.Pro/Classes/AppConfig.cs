using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using static Neko.EFT.Manager.X.Classes.ServerConfigManager;

namespace Neko.EFT.Manager.X.Classes
{
    public class AppConfig
    {
        // 将配置文件存储到当前目录的 UserData/Config 文件夹中
        private static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string ConfigDirectory = Path.Combine(BaseDirectory, "UserData", "Config");
        private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "Serverconfig.json");

        public List<ServerSourceConfig> ServerSources { get; set; } // 用于存储服务器源的配置
        public string SelectedServer { get; set; }
        public string CurrentServerSourceUrl { get; set; } // 用于存储当前选定的服务器源的 URL

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    return JsonConvert.DeserializeObject<AppConfig>(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading config file: " + ex.Message);
            }

            return new AppConfig();
        }

        public void Save()
        {
            try
            {
                // 确保目录存在
                if (!Directory.Exists(ConfigDirectory))
                {
                    Directory.CreateDirectory(ConfigDirectory);
                }

                string json = JsonConvert.SerializeObject(this);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving config file: " + ex.Message);
            }
        }
    }
}
