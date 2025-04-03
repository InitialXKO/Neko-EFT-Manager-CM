using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Neko.EFT.Manager.X.Classes
{
    public class ManagerConfig : PropertyChangedBase
    {
        public static readonly string BaseDirectory = AppContext.BaseDirectory;
        public static readonly string ConfigDirectory = Path.Combine(BaseDirectory, "UserData", "Config");
        public static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "ManagerConfig.json");

        private string _lastServer = "http://127.0.0.1:6969";


        private int _refreshInterval = 30; // 默认值
        public int RefreshInterval
        {
            get => _refreshInterval;
            set
            {
                if (SetField(ref _refreshInterval, value))
                {
                    ManagerConfig.SaveAccountInfo(); // 实时保存配置
                }
            }
        }


        public string LastServer
        {
            get => _lastServer;
            set => SetField(ref _lastServer, value);
        }

        private string _username;
        public string Username
        {
            get => _username;
            set => SetField(ref _username, value);
        }

        private string _password;
        public string Password
        {
            get => _password;
            set => SetField(ref _password, value);
        }

        private string _installPath;
        public string InstallPath
        {
            get => _installPath;
            set
            {
                if (SetField(ref _installPath, value))
                {
                    OnPropertyChanged(nameof(InstallPath));
                    
                }
            }
        }


        private string _akiServerPath;
        public string AkiServerPath
        {
            get => _akiServerPath;
            set
            {
                if (SetField(ref _akiServerPath, value))
                {
                    OnPropertyChanged(nameof(AkiServerPath));
                    
                }
            }
        }

        private bool _rememberLogin = false;
        public bool RememberLogin
        {
            get => _rememberLogin;
            set => SetField(ref _rememberLogin, value);
        }

        private bool _closeAfterLaunch = false;
        public bool CloseAfterLaunch
        {
            get => _closeAfterLaunch;
            set
            {
                if (SetField(ref _closeAfterLaunch, value))
                {
                    ManagerConfig.Save(); // 在此处调用保存方法
                }
            }
        }

        private string _tarkovVersion;
        public string TarkovVersion
        {
            get => _tarkovVersion;
            set => SetField(ref _tarkovVersion, value);
        }

        private string _sitVersion;
        public string SitVersion
        {
            get => _sitVersion;
            set => SetField(ref _sitVersion, value);
        }

        private bool _lookForUpdates = true;
        public bool LookForUpdates
        {
            get => _lookForUpdates;
            set
            {
                if (SetField(ref _lookForUpdates, value))
                {
                    ManagerConfig.Save(); // 在此处调用保存方法
                }
            }
        }

        private bool _Neko_lookForSITUpdates = false;
        public bool Neko_lookForSITUpdates
        {
            get => _Neko_lookForSITUpdates;
            set => SetField(ref _Neko_lookForSITUpdates, value);
        }

        private bool _acceptedModsDisclaimer = false;
        public bool AcceptedModsDisclaimer
        {
            get => _acceptedModsDisclaimer;
            set => SetField(ref _acceptedModsDisclaimer, value);
        }

        private string _modCollectionVersion;
        public string ModCollectionVersion
        {
            get => _modCollectionVersion;
            set => SetField(ref _modCollectionVersion, value);
        }

        private Dictionary<string, string> _installedMods = new();
        public Dictionary<string, string> InstalledMods
        {
            get => _installedMods;
            set => SetField(ref _installedMods, value);
        }

        private string _ConsoleFontColorV = Colors.DarkSlateBlue.ToString();
        public string ConsoleFontColorV
        {
            get => _ConsoleFontColorV;
            set => SetField(ref _ConsoleFontColorV, value);
        }

        private string _consoleFontFamily = "Consolas";
        public string ConsoleFontFamily
        {
            get => _consoleFontFamily;
            set => SetField(ref _consoleFontFamily, value);
        }
        public static bool IsConfigurationGuideShown { get; set; }
        // 是否已经接受过用户协议
        private bool _IuserAgreementAccepted = false;
        public bool IUserAgreementAccepted
        {
            get => _IuserAgreementAccepted;
            set => SetField(ref _IuserAgreementAccepted, value);
        }

        private bool _IConfigGuide = false;
        public bool IConfigGuide
        {
            get => _IConfigGuide;
            set => SetField(ref _IConfigGuide, value);
        }

        private bool _proModeEnabled = false;
        public bool ProModeEnabled
        {
            get => _proModeEnabled;
            set
            {
                if (SetField(ref _proModeEnabled, value))
                {
                    ManagerConfig.Save(); // 保存配置
                }
            }
        }

        private string _roomManagementServer;
        public string RoomManagementServer
        {
            get => _roomManagementServer;
            set
            {
                if (SetField(ref _roomManagementServer, value))
                {
                    if (!IsLoading)
                    {
                        ManagerConfig.Save();
                    }
                }
            }
        }

        private string _vntServer;
        public string VNTServer
        {
            get => _vntServer;
            set
            {
                if (SetField(ref _vntServer, value))
                {
                    if (!IsLoading)
                    {
                        ManagerConfig.Save();
                    }
                }
            }
        }

        private string _NRMSKey;
        public string NRMSKey
        {
            get => _NRMSKey;
            set
            {
                if (SetField(ref _NRMSKey, value))
                {
                    if (!IsLoading)
                    {
                        ManagerConfig.Save();
                    }
                }
            }
        }


        public static void Load()
        {
            try
            {
                if (!Directory.Exists(ConfigDirectory))
                {
                    Directory.CreateDirectory(ConfigDirectory);
                }

                IsLoading = true;
                if (File.Exists(ConfigFilePath))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    App.ManagerConfig = JsonSerializer.Deserialize<ManagerConfig>(File.ReadAllText(ConfigFilePath), options);
                }
                if (App.ManagerConfig == null)
                {
                    App.ManagerConfig = new ManagerConfig();
                }
            }
            catch (Exception ex)
            {
                Loggy.LogToFile("ManagerConfig.Load: " + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }


        public static bool IsLoading { get; set; } = false;


        public static void Save(bool SaveAccount = false)
        {
            // 获取程序目录下的 UserData/Config 路径
            string configDir = Path.Combine(AppContext.BaseDirectory, "UserData", "Config");
            string configFilePath = Path.Combine(configDir, "ManagerConfig.json");

            Debug.WriteLine(configFilePath);

            try
            {
                // 确保目录存在
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                // 检查 ManagerConfig 是否为 null
                if (App.ManagerConfig == null)
                {
                    Debug.WriteLine("ManagerConfig 未初始化，无法保存配置。");
                    return; // 或者抛出异常
                }

                if (SaveAccount == false)
                {
                    // 创建新配置对象，并删除用户名和密码
                    ManagerConfig newLauncherConfig = (ManagerConfig)App.ManagerConfig.MemberwiseClone();
                    newLauncherConfig.Username = null;
                    newLauncherConfig.Password = null;

                    File.WriteAllText(configFilePath, JsonSerializer.Serialize(newLauncherConfig, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    File.WriteAllText(configFilePath, JsonSerializer.Serialize(App.ManagerConfig, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error saving config file: " + ex.Message);
            }
        }

        public static void SaveAccountInfo()
        {
            var configDir = Path.Combine(AppContext.BaseDirectory, "UserData", "Config");
            var configFilePath = Path.Combine(configDir, "ManagerConfig.json");

            Debug.WriteLine(configFilePath);

            try
            {
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                // 直接保存包含用户名和密码的配置
                File.WriteAllText(configFilePath, JsonSerializer.Serialize(App.ManagerConfig, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error saving account info: " + ex.Message);
            }
        }
    }

    public class PropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = null) // 改成 public
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

}
