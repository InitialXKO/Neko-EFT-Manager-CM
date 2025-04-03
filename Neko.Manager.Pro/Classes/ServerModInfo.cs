using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Neko.EFT.Manager.X.Classes
{
    public class ServerModInfo : INotifyPropertyChanged
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonIgnore]
        public string DisplayName { get; set; } // 新增的显示名称属性，不需要在JSON中显示

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("akiVersion")]
        public string AkiVersion { get; set; }

        [JsonPropertyName("sptVersion")]
        public string SPTVersion { get; set; }

        [JsonPropertyName("main")]
        public string Main { get; set; }

        [JsonPropertyName("scripts")]
        public Dictionary<string, string> Scripts { get; set; }

        [JsonPropertyName("devDependencies")]
        public Dictionary<string, string> DevDependencies { get; set; }

        [JsonPropertyName("author")]
        public string Author { get; set; }

        [JsonPropertyName("license")]
        public string License { get; set; }

        [JsonIgnore]
        private bool isEnabled;
        [JsonIgnore]
        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [JsonIgnore]
        public string DirectoryPath { get; set; }

        [JsonIgnore]
        private CompatibilityStatus compatibilityStatus;
        [JsonIgnore]
        public CompatibilityStatus CompatibilityStatus
        {
            get { return compatibilityStatus; }
            set
            {
                compatibilityStatus = value;
                OnPropertyChanged(nameof(CompatibilityStatus));
            }
        }

        [JsonIgnore]
        public string CompatibilityStatusChinese => CompatibilityStatus.ToChinese();
    }
}
