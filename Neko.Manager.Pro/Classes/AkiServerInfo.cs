using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Neko.EFT.Manager.X.Classes;

public class AkiServerInfo
{
    private string[] editions;
    private Dictionary<string, string> descriptions ;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    [JsonPropertyName("editions")]
    public string[] Editions { get => editions; init => editions = value; }
    [JsonPropertyName("profileDescriptions")]
    public Dictionary<string, string> Descriptions { get => descriptions; init => descriptions = value; }
}
