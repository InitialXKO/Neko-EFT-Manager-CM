using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Neko.EFT.Manager.X.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class ServerConfigM
{
    public static string RemoteEndPoint { get; set; } = "http:/127.0.0.1:6969"; // 默认值
}

class NekoBackendTools
{
    public static async void DisplayResponse(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            Console.WriteLine("响应为空或为null。");
            return;
        }

        try
        {
            var jsonResponse = JArray.Parse(response);

            foreach (var item in jsonResponse)
            {
                var serverId = item["ServerId"]?.ToString();
                var hostProfileId = item["HostProfileId"]?.ToString();
                var hostName = item["HostName"]?.ToString();
                var location = item["Location"]?.ToString();
                var playerCount = item["PlayerCount"]?.ToString();
                var status = item["Status"]?.ToString();
            }
        }
        catch (Exception ex)
        {
            await Utils.ShowInfoBar("错误", $"发生异常：{ex}.", InfoBarSeverity.Error);
        }
    }

    public static async Task<string> PostJsonAsync(string url, string data, string serverAddress, int timeout = 5000, int retryAttempts = 3, bool debug = false)
    {
        int attempt = 0;

        while (attempt < retryAttempts)
        {
            try
            {
                if (!url.StartsWith("/"))
                {
                    url = "/" + url;
                }

                var bytes = await asyncRequestFromPath(url, serverAddress, "POST", data, timeout, debug);
                if (bytes != null)
                {
                    return Encoding.UTF8.GetString(bytes);
                }
                else
                {
                    throw new Exception("响应字节为空。");
                }
            }
            catch (Exception ex)
            {
                await Utils.ShowInfoBar("错误", $"发生异常：{ex.Message}.", InfoBarSeverity.Error);
            }

            await Task.Delay(1000); // 1秒延迟
            attempt++;
        }
        throw new Exception($"无法与 Aki 服务器 {url} 通信以发布 JSON 数据：{data}");
    }



    public static async Task<byte[]?> asyncRequestFromPath(string path, string serverAddress, string method = "GET", string? data = null, int timeout = 9999, bool debug = false)
    {
        if (!Uri.IsWellFormedUriString(path, UriKind.Absolute))
        {
            path = serverAddress + path;
        }

        return await asyncRequest(new Uri(path), serverAddress, method, data, timeout, debug);
    }

    public static async Task<byte[]?> asyncRequest(Uri uri, string serverAddress, string method = "GET", string? data = null, int timeout = 9999, bool debug = false)
    {

        var compress = true;
        using (HttpClientHandler handler = new HttpClientHandler())
        {
            using (HttpClient httpClient = new HttpClient(handler))
            {
                handler.UseCookies = true;
                handler.CookieContainer = new CookieContainer();
                httpClient.Timeout = TimeSpan.FromMilliseconds(timeout);
                Uri baseAddress = new Uri(serverAddress);

                foreach (var item in GetHeaders(SessionManager.SessionId))
                {
                    if (item.Key == "Cookie")
                    {
                        string[] pairs = item.Value.Split(';');
                        var keyValuePairs = pairs
                            .Select(p => p.Split(new[] { '=' }, 2))
                            .Where(kvp => kvp.Length == 2)
                            .ToDictionary(kvp => kvp[0], kvp => kvp[1]);
                        foreach (var kvp in keyValuePairs)
                        {
                            handler.CookieContainer.Add(baseAddress, new Cookie(kvp.Key, kvp.Value));
                        }
                    }
                    else
                    {
                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(item.Key, item.Value);
                    }
                }


                if (!debug && method == "POST")
                {
                    httpClient.DefaultRequestHeaders.AcceptEncoding.TryParseAdd("deflate");
                }

                HttpContent? byteContent = null;
                if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(data))
                {
                    if (debug)
                    {
                        compress = false;
                        httpClient.DefaultRequestHeaders.Add("debug", "1");
                    }
                    var inputDataBytes = Encoding.UTF8.GetBytes(data);
                    var bytes = compress ? NekoZlib.Compress(inputDataBytes, ZlibCompression.Normal) : inputDataBytes;
                    byteContent = new ByteArrayContent(bytes);
                    byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    if (compress)
                    {
                        byteContent.Headers.ContentEncoding.Add("deflate");
                    }
                }

                HttpResponseMessage response;
                if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                {
                    response = await httpClient.PostAsync(uri, byteContent);
                }
                else
                {
                    response = await httpClient.GetAsync(uri);
                }

                if (response.IsSuccessStatusCode)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();

                    if (Zlib.IsCompressed(bytes))
                    {
                        bytes = NekoZlib.Decompress(bytes);
                    }

                    return bytes;
                }
                else
                {
                    await Utils.ShowInfoBar("错误", $"无法向服务器发送 API 请求。状态码：{response.StatusCode}", InfoBarSeverity.Error);
                    return null;
                }
            }
        }
    }


    public static Dictionary<string, string> GetHeaders(string sessionId)
    {
        return new Dictionary<string, string>
    {
        { "Cookie", $"PHPSESSID={sessionId}" },
        { "SessionId", sessionId }
    };
    }


}
