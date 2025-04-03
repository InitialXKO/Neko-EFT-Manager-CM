using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Neko.EFT.Manager.X.Classes
{
    public static class ServerConfigManager
    {
        // 将路径指向程序目录下的 UserData/Config 文件夹
        private static readonly string BaseDirectory = AppContext.BaseDirectory;
        private static readonly string ConfigDirectory = Path.Combine(BaseDirectory, "UserData", "Config");
        public static readonly string serverSourceConfigPath = Path.Combine(ConfigDirectory, "server_source_configX.json");
        public static readonly string defaultLocalConfigPath = Path.Combine(ConfigDirectory, "local_server_config.json");

        public static List<ServerSourceConfig> LoadServerSources()
        {
            if (!Directory.Exists(ConfigDirectory))
            {
                Directory.CreateDirectory(ConfigDirectory);
            }

            if (!File.Exists(serverSourceConfigPath))
            {
                // 默认服务器源配置
                var defaultSources = new List<ServerSourceConfig>
                {
                    new ServerSourceConfig { Name = "本地源", Url = defaultLocalConfigPath },
                    new ServerSourceConfig { Name = "捐赠者服务器源", Url = "https://gitee.com/Neko17-Offical/neko-server-list-repo/raw/master/ServerList/serverlist.json" }
                };

                // 保存默认服务器源配置到文件
                SaveServerSources(defaultSources);

                // 创建默认本地服务器配置文件
                if (!File.Exists(defaultLocalConfigPath))
                {
                    var defaultConfig = new List<ServerConfig>
                    {
                        new ServerConfig { name = "本地服务器", serverAddress = "http://127.0.0.1:6969", newPort = "6970" }
                    };
                    SaveServerConfigs(defaultConfig);
                }

                return defaultSources;
            }

            var json = File.ReadAllText(serverSourceConfigPath);
            return JsonConvert.DeserializeObject<List<ServerSourceConfig>>(json);
        }

        public static void SaveServerSources(List<ServerSourceConfig> sources)
        {
            var json = JsonConvert.SerializeObject(sources, Formatting.Indented);
            File.WriteAllText(serverSourceConfigPath, json);
        }

        public static async Task<List<ServerConfig>> LoadServerConfigs(ServerSourceConfig source)
        {
            string url = ConvertToRawUrl(source.Url);
            if (url == defaultLocalConfigPath)
            {
                if (!File.Exists(defaultLocalConfigPath))
                {
                    var defaultConfig = new List<ServerConfig>
                    {
                        new ServerConfig { name = "本地服务器", serverAddress = "http://127.0.0.1:6969", newPort = "6970" }
                    };
                    SaveServerConfigs(defaultConfig);
                    return defaultConfig;
                }

                var json = File.ReadAllText(defaultLocalConfigPath);
                return JsonConvert.DeserializeObject<List<ServerConfig>>(json);
            }
            else
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<ServerConfig>>(responseBody);
                }
            }
        }

        public static string ConvertToRawUrl(string url)
        {
            if (url.Contains("github.com"))
            {
                return url.Replace("github.com", "raw.githubusercontent.com").Replace("/blob", "");
            }
            return url;
        }

        public static void SaveServerConfigs(List<ServerConfig> config)
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(defaultLocalConfigPath, json);
        }

        public class GiteeFileResponse
        {
            public string content { get; set; }
        }

        public class GitHubFileResponse
        {
            public string content { get; set; }
        }
    }
}
