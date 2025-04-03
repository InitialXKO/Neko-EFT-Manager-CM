using ComponentAce.Compression.Libs.zlib;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Neko.EFT.Manager.X.Classes
{
    public static class NekoNetTools
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<string> SendGetRequest(string serverAddress)
        {
            try
            {
                string getUrl = "/launcher/server/connect"; // 替换为你的 GET 请求路径
                

                var responseItems = await SendAsync(serverAddress, getUrl, "GET");
                var responseStream = responseItems.Item1;
                var httpResponse = responseItems.Item2;

                string responseBody = await GetResponseBody(responseStream, httpResponse);
               

                return responseBody; // 返回延迟的字符串表示
            }
            catch (Exception)
            {
                
                return string.Empty; // 返回一个空字符串表示出现异常
            }
        }
        public static async Task<string> GetServerVersion(string serverAddress)
        {
            try
            {
                string getUrl = "/launcher/server/version"; // 替换为你的 GET 请求路径


                var responseItems = await SendAsync(serverAddress, getUrl, "GET");
                var responseStream = responseItems.Item1;
                var httpResponse = responseItems.Item2;

                string responseBody = await GetResponseBody(responseStream, httpResponse);


                return responseBody; // 返回延迟的字符串表示
            }
            catch (Exception)
            {

                return string.Empty; // 返回一个空字符串表示出现异常
            }
        }
        public static async Task<string> GetEFTVersion(string serverAddress)
        {
            try
            {
                string getUrl = "/launcher/profile/compatibleTarkovVersion"; // 替换为你的 GET 请求路径


                var responseItems = await SendAsync(serverAddress, getUrl, "GET");
                var responseStream = responseItems.Item1;
                var httpResponse = responseItems.Item2;

                string responseBody = await GetResponseBody(responseStream, httpResponse);


                return responseBody; // 返回延迟的字符串表示
            }
            catch (Exception)
            {

                return string.Empty; // 返回一个空字符串表示出现异常
            }
        }

        public static async Task<string> GetPlayerPresenceInfo(string serverAddress)
        {
            try
            {
                string getUrl = "/fika/presence/get"; // 请求玩家状态信息的路径

                var responseItems = await SendAsync(serverAddress, getUrl, "GET");
                var responseStream = responseItems.Item1;
                var httpResponse = responseItems.Item2;

                string responseBody = await GetResponseBody(responseStream, httpResponse);

                return responseBody; // 返回获取到的玩家状态信息
            }
            catch (Exception)
            {
                return string.Empty; // 返回一个空字符串表示出现异常
            }
        }


        public static async Task<string>GetProfile(string serverAddress)
        {
            try
            {
                string getUrl = "/launcher/profiles"; // 替换为你的 GET 请求路径


                var responseItems = await SendAsync(serverAddress, getUrl, "GET");
                var responseStream = responseItems.Item1;
                var httpResponse = responseItems.Item2;

                string responseBody = await GetResponseBody(responseStream, httpResponse);


                return responseBody; // 返回延迟的字符串表示
            }
            catch (Exception)
            {

                return string.Empty; // 返回一个空字符串表示出现异常
            }
        }


        public static async Task<long> GetPing(string serverAddress)
        {
            try
            {
                string getUrl = "/launcher/ping"; // 替换为你的 GET 请求路径
                var stopwatch = Stopwatch.StartNew();

                var responseItems = await SendAsync(serverAddress, getUrl, "GET");
                var responseStream = responseItems.Item1;
                var httpResponse = responseItems.Item2;

                stopwatch.Stop();
                long latency = stopwatch.ElapsedMilliseconds;

                Console.WriteLine("GET 请求响应头：");
                foreach (var header in httpResponse.Headers)
                {
                    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }

                string responseBody = await GetResponseBody(responseStream, httpResponse);
                Console.WriteLine("GET 请求响应体：");
                Console.WriteLine(responseBody);
                Console.WriteLine($"延迟：{latency}ms");

                // 检查响应体是否包含 "pong!"
                if (responseBody.Contains("pong!"))
                {
                    return latency; // 返回延迟值
                }
                else
                {
                    Console.WriteLine("响应体中未找到 'pong!'，检测延迟失败。");
                    return -1; // 返回 -1 表示检测失败
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送 GET 请求时发生错误：{ex.Message}");
                return -1; // 返回 -1 表示发生错误
            }
        }

        private static async Task<(Stream, HttpResponseMessage)> SendAsync(string serverAddress, string getUrl, string method = "GET", string data = null, bool compress = true, CancellationToken cancellationToken = default)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            var fullUri = getUrl;
            if (!Uri.IsWellFormedUriString(fullUri, UriKind.Absolute))
                fullUri = serverAddress + fullUri;

            if (!fullUri.StartsWith("https://") && !fullUri.StartsWith("http://"))
                fullUri = "https://" + fullUri;

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Cookie", $"PHPSESSID={SessionManager.SessionId}");
            httpClient.DefaultRequestHeaders.Add("SessionId", SessionManager.SessionId);
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "deflate, gzip");

            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(method), fullUri);

            if (method != "GET" && !string.IsNullOrEmpty(data))
            {
                byte[] bytes = compress ? CompressToBytes(data, CompressionLevel.Optimal) : System.Text.Encoding.UTF8.GetBytes(data);
                request.Content = new ByteArrayContent(bytes);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                if (compress)
                {
                    request.Content.Headers.Add("content-encoding", "deflate");
                }
            }

            HttpResponseMessage httpResponse = await httpClient.SendAsync(request);

            Stream responseStream = await httpResponse.Content.ReadAsStreamAsync();

            return (responseStream, httpResponse);
        }

        private static async Task<string> GetResponseBody(Stream responseStream, HttpResponseMessage httpResponse)
        {
            byte[] responseBytes = await StreamToByteArrayAsync(responseStream);
            string contentEncoding = httpResponse.Content.Headers.ContentEncoding.ToString();

            if (contentEncoding == "deflate")
            {
                return SimpleZlib.Decompress(responseBytes, null);
            }
            else
            {
                return SimpleZlib.Decompress(responseBytes, null);
            }
        }

        private static async Task<byte[]> StreamToByteArrayAsync(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }

        private static byte[] CompressToBytes(string data, CompressionLevel compressionLevel)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(memoryStream, compressionLevel))
                using (var writer = new StreamWriter(deflateStream))
                {
                    writer.Write(data);
                }
                return memoryStream.ToArray();
            }
        }
    }
}
