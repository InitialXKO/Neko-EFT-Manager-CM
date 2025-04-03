using System.Text.Json.Serialization;

public class VersionInfo
{
    [JsonPropertyName("ServerVersion")]
    public string ServerVersion { get; set; }

    [JsonPropertyName("ClientVersion")]
    public string ClientVersion { get; set; }

    [JsonPropertyName("ServerDownloadLink")]
    public string ServerDownloadLink { get; set; }

    [JsonPropertyName("ClientDownloadLink")]
    public string ClientDownloadLink { get; set; }
}
