using ComponentAce.Compression.Libs.zlib;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Neko.EFT.Manager.X.Classes
{
    public class TarkovRequesting
    {
        public string Session;
        public string RemoteEndPoint;
        public bool isUnity;

        private static HttpClient httpClientTR;

        public TarkovRequesting(string session, string remoteEndPoint, bool isUnity = true)
        {
            Session = session;
            RemoteEndPoint = remoteEndPoint;
            httpClientTR = new()
            {
                BaseAddress = new Uri(RemoteEndPoint),
            };

            httpClientTR.DefaultRequestHeaders.Add("Cookie", $"PHPSESSID={SessionManager.SessionId}");
            httpClientTR.DefaultRequestHeaders.Add("SessionId", SessionManager.SessionId);
            httpClientTR.DefaultRequestHeaders.Add("Accept-Encoding", "deflate");
            httpClientTR.Timeout = new TimeSpan(0, 0, 1);
        }

        private async Task<(Stream, HttpResponseMessage)> SendAsync(string url, string method = "GET", string data = null, bool compress = true, CancellationToken cancellationToken = default)
        {
            // disable SSL encryption
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            var fullUri = url;
            if (!Uri.IsWellFormedUriString(fullUri, UriKind.Absolute))
                fullUri = RemoteEndPoint + fullUri;

            if (!fullUri.StartsWith("https://") && !fullUri.StartsWith("http://"))
                fullUri = fullUri.Insert(0, "https://");

            // Create HttpClient instance
            HttpClient httpClient = new HttpClient();

            // Add session headers if available
            if (!string.IsNullOrEmpty(SessionManager.SessionId))
            {
                httpClient.DefaultRequestHeaders.Add("Cookie", $"PHPSESSID={SessionManager.SessionId}");
                httpClient.DefaultRequestHeaders.Add("SessionId", SessionManager.SessionId);
            }

            // Add Accept-Encoding header
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "deflate, gzip");

            // Create HttpRequestMessage
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(method), fullUri);

            // Add request body if applicable
            if (method != "GET" && !string.IsNullOrEmpty(data))
            {
                byte[] bytes = compress ? SimpleZlib.CompressToBytes(data, zlibConst.Z_BEST_SPEED) : Encoding.UTF8.GetBytes(data);
                request.Content = new ByteArrayContent(bytes);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                if (compress)
                {
                    request.Content.Headers.Add("content-encoding", "deflate");
                }
            }

            // Send request and get response
            //HttpResponseMessage httpResponse = await httpClient.SendAsync(request);

            //// Get response stream
            //Stream responseStream = await httpResponse.Content.ReadAsStreamAsync();

            //return (responseStream, httpResponse);
            HttpResponseMessage httpResponse = await httpClient.SendAsync(request);

            // Get response stream
            Stream responseStream = await httpResponse.Content.ReadAsStreamAsync();

            return (responseStream, httpResponse);
        }

        private async Task<(Stream, HttpResponseMessage)> SendHttpAsync(string url, string method = "GET", string data = null, bool compress = true)
        {
            // Create HttpClient instance (if not already created)
            if (httpClientTR == null)
            {
                httpClientTR = new HttpClient();
                httpClientTR.BaseAddress = new Uri(RemoteEndPoint);
                httpClientTR.DefaultRequestHeaders.Add("Cookie", $"PHPSESSID={SessionManager.SessionId}");
                httpClientTR.DefaultRequestHeaders.Add("SessionId", SessionManager.SessionId);
                httpClientTR.DefaultRequestHeaders.Add("Accept-Encoding", "deflate");
                httpClientTR.Timeout = TimeSpan.FromSeconds(5); // Adjust timeout as needed
            }

            // Compose full URL if not absolute
            var fullUri = url;
            if (!Uri.IsWellFormedUriString(fullUri, UriKind.Absolute))
                fullUri = RemoteEndPoint + fullUri;

            if (!fullUri.StartsWith("http://") && !fullUri.StartsWith("https://"))
                fullUri = "http://" + fullUri;

            // Create HttpRequestMessage
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(method), fullUri);

            // Add request body if applicable
            if (method != "GET" && !string.IsNullOrEmpty(data))
            {
                byte[] bytes = compress ? SimpleZlib.CompressToBytes(data, zlibConst.Z_BEST_COMPRESSION) : Encoding.UTF8.GetBytes(data);
                request.Content = new ByteArrayContent(bytes);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                if (compress)
                {
                    request.Content.Headers.Add("content-encoding", "deflate");
                }
            }

            // Send request and get response
            HttpResponseMessage httpResponse = await httpClientTR.SendAsync(request);

            // Get response stream
            Stream responseStream = await httpResponse.Content.ReadAsStreamAsync();

            return (responseStream, httpResponse);
        }

        public async Task PutJsonAsync(string url, string data, bool compress = true)
        {
            using (Stream stream = (await SendAsync(url, "PUT", data, compress)).Item1) { }
        }

        public async Task<string> GetJsonAsync(string url, CancellationToken cancellationToken = default, bool compress = true)
        {
            // SendAsync 方法现在接受 CancellationToken 参数
            using var stream = (await SendAsync(url, "GET", null, compress, cancellationToken)).Item1;
            using var reader = new StreamReader(stream);
            var uncompressedData = await reader.ReadToEndAsync();
            var decodedData = SimpleZlib.Decompress(uncompressedData, null);
            return decodedData;
        }

        public async Task<string> PostJsonAsync(string url, string data, bool compress = true)
        {
            var postItems = await SendAsync(url, "POST", data, compress);
            var stream = postItems.Item1;
            using (stream)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    if (stream == null)
                        return "";
                    await stream.CopyToAsync(ms);

                    if (compress)
                        return SimpleZlib.Decompress(ms.ToArray(), null);
                    else
                        return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
        }




    }
}
