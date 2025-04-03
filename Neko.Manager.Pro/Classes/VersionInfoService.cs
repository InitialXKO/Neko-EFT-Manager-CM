using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class VersionInfoService
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task<List<VersionInfo>> GetVersionsAsync(string url)
    {
        var response = await client.GetStringAsync(url);
        return JsonSerializer.Deserialize<List<VersionInfo>>(response);
    }
}
