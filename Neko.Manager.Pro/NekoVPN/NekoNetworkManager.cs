using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Neko.EFT.Manager.X.Classes;
using System.Diagnostics;
using System.Net.NetworkInformation;
using Windows.ApplicationModel.Chat;
using ChatMessage = Neko.EFT.Manager.X.Classes.ChatMessage;

namespace Neko.EFT.Manager.X.NekoVPN
{
    // 客户端网络管理器
    public class NekoNetworkManager
    {
        private readonly IPEndPoint _serverEndpoint;
        private readonly string _clientIdentifier;  // 设备唯一标识符
        public static string _allocatedIp;  // 服务器分配的 IP 地址
        private string _targetClientIp;  // 目标成员 IP 地址

        /// <summary>
        /// 通过传入服务器地址（格式："IP:Port"）构造 NekoNetworkManager 实例
        /// </summary>
        /// <param name="serverAddress">服务器地址，例如 "110.42.41.105:12345"</param>
        public NekoNetworkManager(string serverAddress)
        {
            if (string.IsNullOrWhiteSpace(serverAddress))
            {
                throw new ArgumentException("服务器地址不能为空", nameof(serverAddress));
            }
            _serverEndpoint = ParseEndpoint(serverAddress);
            _clientIdentifier = ClientIdentifier.GetUniqueClientId();  // 设备唯一标识
        }

        /// <summary>
        /// 创建房间，同时传入房间密码及最大客户端数量
        /// </summary>
        ///
        /// <summary>
        /// 更新房主完整登录地址
        /// </summary>
        /// <param name="roomId">房间ID</param>
        /// <param name="hostLoginUrl">完整的服务器登录地址，例如 "http://10.26.0.5:8000"</param>
        /// <param name="cancellationToken">取消标记</param>
        /// <returns>返回操作结果</returns>
        public async Task<OperationResult> UpdateLoginAddressAsync(string roomId, string hostLoginUrl, CancellationToken cancellationToken)
        {
            // 注意：此处要求房主客户端必须已经获得由服务端分配的 IP 地址（存储在 _allocatedIp 中）
            if (string.IsNullOrEmpty(_allocatedIp))
            {
                throw new InvalidOperationException("更新登录地址失败：未获取到服务器分配的 IP 地址。");
            }

            return await ExecuteWithRetriesAsync(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync(_serverEndpoint.Address, _serverEndpoint.Port).ConfigureAwait(false);
                client.SendTimeout = 5000;
                client.ReceiveTimeout = 5000;

                // 构造更新请求，此处传入房间ID、房主分配的IP以及完整的登录地址
                var request = new NetworkRequest
                {
                    Action = "UPDATE_LOGIN_ADDRESS",
                    RoomId = roomId,
                    ClientIp = _allocatedIp,   // 此处必须与服务端房主验证时使用的 IP 一致
                    HostLoginUrl = hostLoginUrl
                };

                return await SendRequest<OperationResult>(client, request, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }


        public async Task<OperationResult> SendMessageAsync(string roomId, string messageContent, CancellationToken cancellationToken)
        {
            return await ExecuteWithRetriesAsync(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync(_serverEndpoint.Address, _serverEndpoint.Port).ConfigureAwait(false);
                client.SendTimeout = 5000;
                client.ReceiveTimeout = 5000;

                // 构造发送消息请求
                var request = new NetworkRequest
                {
                    Action = "SEND_MESSAGE",
                    RoomId = roomId,
                    ClientIp = _clientIdentifier, // 或其他唯一标识
                    MessageContent = messageContent
                };

                return await SendRequest<OperationResult>(client, request, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        public async Task<GetMessagesResult> GetMessagesAsync(string roomId, CancellationToken cancellationToken)
        {
            return await ExecuteWithRetriesAsync(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync(_serverEndpoint.Address, _serverEndpoint.Port).ConfigureAwait(false);
                client.SendTimeout = 5000;
                client.ReceiveTimeout = 5000;

                var request = new NetworkRequest
                {
                    Action = "GET_MESSAGES",
                    RoomId = roomId
                };

                return await SendRequest<GetMessagesResult>(client, request, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }


        public class GetMessagesResult : OperationResult
        {
            public List<ChatMessage> Messages { get; set; }
        }


        public async Task<Room> CreateRoomAsync(string password, int maxClients, CancellationToken cancellationToken)
        {
            return await ExecuteWithRetriesAsync(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync(_serverEndpoint.Address, _serverEndpoint.Port).ConfigureAwait(false);
                // 设置发送与接收超时
                client.SendTimeout = 5000;
                client.ReceiveTimeout = 5000;

                var request = new NetworkRequest
                {
                    Action = "CREATE_ROOM",
                    Password = password,
                    MaxClients = maxClients,
                    ClientIp = _clientIdentifier
                };

                return await SendRequest<Room>(client, request, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 加入房间，传入房间ID和密码
        /// </summary>
        public async Task<JoinResult> JoinRoomAsync(string roomId, string password, CancellationToken cancellationToken)
        {
            return await ExecuteWithRetriesAsync(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync(_serverEndpoint.Address, _serverEndpoint.Port).ConfigureAwait(false);
                client.SendTimeout = 5000;
                client.ReceiveTimeout = 5000;

                var request = new NetworkRequest
                {
                    Action = "JOIN_ROOM",
                    RoomId = roomId,
                    Password = password,
                    ClientIp = _clientIdentifier
                };

                var result = await SendRequest<JoinResult>(client, request, cancellationToken).ConfigureAwait(false);

                if (result == null)
                {
                    Debug.WriteLine("[客户端] ❌ 服务器返回了 `null`，无法加入房间");
                    return new JoinResult { Success = false, Error = "服务器返回了无效响应" };
                }

                if (!result.Success)
                {
                    Debug.WriteLine($"[客户端] ❌ 加入失败: {result.Error}");
                    return result;
                }

                if (string.IsNullOrEmpty(result.IpAddress))
                {
                    Debug.WriteLine("[客户端] ❌ 服务器未分配 IP，无法加入");
                    return new JoinResult { Success = false, Error = "服务器未返回有效的 IP" };
                }

                _allocatedIp = result.IpAddress;


                _allocatedIp = result.IpAddress; // 存储服务器分配的 IP

                // 检查分配的 IP 是否已在本机配置
                if (IpHelper.IsIpAlreadyInUse(_allocatedIp))
                {
                    Debug.WriteLine($"检测到 IP {_allocatedIp} 已存在，尝试删除配置...");
                    string[] interfaces = IpHelper.GetInterfacesUsingIp(_allocatedIp);
                    if (interfaces.Length == 0)
                    {
                        Debug.WriteLine("未找到使用该 IP 的网络接口。");
                    }
                    else
                    {
                        foreach (string iface in interfaces)
                        {
                            Debug.WriteLine($"正在删除接口 \"{iface}\" 上的 IP {_allocatedIp} ...");
                            IpHelper.RemoveIp(_allocatedIp, iface);
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"IP {_allocatedIp} 未被使用，可以直接配置。");
                }

                // 更新配置文件
                var newDeviceId = ClientIdentifier.GetUniqueClientId();
                var newIp = _allocatedIp;
                var newToken = roomId;
                var newName = GetDeviceName();

                var configPath = Path.Combine(Directory.GetCurrentDirectory(), "configs", "EFT.yaml");
                try
                {
                    VntConfig config = YamlConfigManager.LoadConfig(configPath);
                    Debug.WriteLine($"成功加载配置: {configPath}");
                    Debug.WriteLine($"Server Address: {config.server_address}");
                    Debug.WriteLine($"Token: {config.token}");
                    Debug.WriteLine($"Device ID: {config.device_id}");
                    Debug.WriteLine($"IP: {config.ip}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ERROR] 加载 YAML 配置失败: {ex.Message}");
                }

                YamlConfigManager.UpdateIp(configPath, newIp);
                Debug.WriteLine($"✅ 组网IP 已更新为：{newIp}!");
                YamlConfigManager.UpdateToken(configPath, newToken);
                Debug.WriteLine($"✅ 组网Token 已更新为：{newToken}!");
                YamlConfigManager.UpdateDeviceId(configPath, newDeviceId);
                Debug.WriteLine($"✅ 设备 ID 已更新为：{newDeviceId}!");
                YamlConfigManager.UpdateName(configPath, newName);
                Debug.WriteLine($"✅ 设备 ID 已更新为：{newName}!");


                return result;
            }).ConfigureAwait(false);
        }


        public string GetDeviceName()
        {
            return Environment.MachineName;
        }
        /// <summary>
        /// 客户端退出房间
        /// </summary>
        public async Task<OperationResult> ExitRoomAsync(string roomId, string allocatedIp, CancellationToken cancellationToken)

        {

            Debug.WriteLine($"[INFO] 退出房间：{roomId}，IP：{_allocatedIp}");
            if (_allocatedIp == null)
            {
                throw new InvalidOperationException("无法退出房间：未获取到分配的 IP 地址");
            }

            return await ExecuteWithRetriesAsync(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync(_serverEndpoint.Address, _serverEndpoint.Port).ConfigureAwait(false);
                client.SendTimeout = 5000;
                client.ReceiveTimeout = 5000;

                var request = new NetworkRequest
                {
                    Action = "EXIT_ROOM",
                    RoomId = roomId,
                    ClientIp = _allocatedIp
                };

                return await SendRequest<OperationResult>(client, request, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 房主销毁房间（需要房主密码验证）
        /// </summary>
        public async Task<OperationResult> DestroyRoomAsync(string roomId, string password, CancellationToken cancellationToken)
        {
            return await ExecuteWithRetriesAsync(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync(_serverEndpoint.Address, _serverEndpoint.Port).ConfigureAwait(false);
                client.SendTimeout = 5000;
                client.ReceiveTimeout = 5000;

                var request = new NetworkRequest
                {
                    Action = "DESTROY_ROOM",
                    RoomId = roomId,
                    Password = password
                };

                return await SendRequest<OperationResult>(client, request, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 获取房间状态
        /// </summary>
        public async Task<RoomStatusResult> GetRoomStatusAsync(string roomId, CancellationToken cancellationToken)
        {
            return await ExecuteWithRetriesAsync(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync(_serverEndpoint.Address, _serverEndpoint.Port).ConfigureAwait(false);
                client.SendTimeout = 5000;
                client.ReceiveTimeout = 5000;

                var request = new NetworkRequest
                {
                    Action = "GET_ROOM_STATUS",
                    RoomId = roomId
                };

                return await SendRequest<RoomStatusResult>(client, request, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 房主踢出指定客户端（需要房主密码验证，TargetIp指定目标客户端）
        /// </summary>
        public async Task<OperationResult> KickClientAsync(string roomId, string password, string targetClientIp, CancellationToken cancellationToken)
        {
            return await ExecuteWithRetriesAsync(async () =>
            {
                using var client = new TcpClient();
                await client.ConnectAsync(_serverEndpoint.Address, _serverEndpoint.Port).ConfigureAwait(false);
                client.SendTimeout = 5000;
                client.ReceiveTimeout = 5000;

                var request = new NetworkRequest
                {
                    Action = "KICK_CLIENT",
                    RoomId = roomId,
                    Password = password,
                    TargetIp = targetClientIp
                };

                return await SendRequest<OperationResult>(client, request, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 发送请求并读取响应（按行读取），支持超时和取消
        /// </summary>
        private static async Task<T?> SendRequest<T>(TcpClient client, object data, CancellationToken cancellationToken)
        {
            if (client == null || !client.Connected)
            {
                Debug.WriteLine("[错误] 连接已断开，无法发送请求。");
                return Activator.CreateInstance<T>(); // 返回默认实例，而不是 null
            }

            try
            {
                var stream = client.GetStream();
                using var writer = new StreamWriter(stream) { AutoFlush = true };
                using var reader = new StreamReader(stream);

                var json = JsonConvert.SerializeObject(data);
                await writer.WriteLineAsync(json).ConfigureAwait(false);
                Debug.WriteLine($"[发送数据] {json}");

                var timeout = TimeSpan.FromSeconds(10);
                var readTask = reader.ReadLineAsync();
                var timeoutTask = Task.Delay(timeout, cancellationToken);
                var completedTask = await Task.WhenAny(readTask, timeoutTask).ConfigureAwait(false);

                if (completedTask == timeoutTask)
                {
                    Debug.WriteLine("[超时] 服务器响应超时");
                    return Activator.CreateInstance<T>(); // 避免 null
                }

                var response = await readTask.ConfigureAwait(false);
                if (string.IsNullOrEmpty(response))
                {
                    Debug.WriteLine("[警告] 服务器返回空响应");
                    return Activator.CreateInstance<T>();
                }

                Debug.WriteLine($"[收到响应] {response}");

                try
                {
                    var result = JsonConvert.DeserializeObject<T>(response);
                    return result ?? Activator.CreateInstance<T>(); // 确保不会返回 null
                }
                catch (JsonException jsonEx)
                {
                    Debug.WriteLine($"[JSON 解析错误] {jsonEx.Message}");
                    return Activator.CreateInstance<T>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[请求失败] {ex.Message}");
                return Activator.CreateInstance<T>();
            }
        }



        /// <summary>
        /// 简单的重试机制：遇到网络相关异常时进行重试
        /// </summary>
        private static async Task<T?> ExecuteWithRetriesAsync<T>(Func<Task<T>> action, int maxRetries = 3, int delayMilliseconds = 1000)
        {
            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    return await action().ConfigureAwait(false);
                }
                catch (IOException ex)
                {
                    attempt++;
                    Debug.WriteLine($"[网络错误] {ex.Message}，重试 {attempt}/{maxRetries}");
                }
                catch (SocketException ex)
                {
                    attempt++;
                    Debug.WriteLine($"[Socket 错误] {ex.Message}，重试 {attempt}/{maxRetries}");
                }
                catch (TimeoutException ex)
                {
                    attempt++;
                    Debug.WriteLine($"[超时] {ex.Message}，重试 {attempt}/{maxRetries}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[未知错误] {ex.Message}，不再重试");
                    break; // 遇到非网络错误，不再重试
                }

                if (attempt < maxRetries)
                {
                    await Task.Delay(delayMilliseconds).ConfigureAwait(false);
                }
            }

            Debug.WriteLine($"[请求失败] 已达到最大重试次数({maxRetries})，返回默认值。");
            return default;
        }


        /// <summary>
        /// 将 "IP:Port" 格式的字符串解析为 IPEndPoint
        /// </summary>
        private static IPEndPoint? ParseEndpoint(string endpoint)
        {
            try
            {
                var parts = endpoint.Split(':');
                if (parts.Length != 2)
                {
                    Debug.WriteLine($"[错误] 服务器地址格式错误: {endpoint}");
                    return null;
                }

                if (!IPAddress.TryParse(parts[0], out var ipAddress))
                {
                    Debug.WriteLine($"[错误] IP 地址格式错误: {parts[0]}");
                    return null;
                }

                if (!int.TryParse(parts[1], out int port) || port < 1 || port > 65535)
                {
                    Debug.WriteLine($"[错误] 端口号格式错误: {parts[1]}");
                    return null;
                }

                return new IPEndPoint(ipAddress, port);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[异常] 解析服务器地址失败: {ex.Message}");
                return null;
            }
        }

    }

    // 数据结构：房间状态返回结果
    public class RoomStatusResult
    {
        public bool Exists { get; set; }
        public List<ClientInfo> Clients { get; set; }

        public string HostLoginUrl { get; set; }
    }

    // 数据结构：网络请求，扩展了 ClientIp 和 TargetIp 字段
    public class NetworkRequest
    {
        public string Action { get; set; }
        public string RoomId { get; set; }
        public string Password { get; set; }
        public int MaxClients { get; set; }
        public string ClientIp { get; set; }
        public string TargetIp { get; set; }


        public string MessageContent { get; set; }
        public string HostLoginUrl { get; set; }
    }

    // 数据结构：房间信息（需与服务端保持一致）
    public class Room
    {
        public string Id { get; set; }
        public string Password { get; set; }
        public string Subnet { get; set; }
        public string Gateway { get; set; }
        public string SubnetMask { get; set; }
        public int MaxClients { get; set; }
        public Queue<string> AvailableIps { get; set; }
        public List<ClientInfo> Clients { get; set; }

        public string HostLoginUrl { get; set; }
    }

    // 数据结构：客户端信息
    public class ClientInfo
    {
        public string IpAddress { get; set; }
        public string DeviceId { get; set; }
    }

    // 数据结构：加入房间的结果
    public class JoinResult
    {
        public bool Success { get; set; }
        public string IpAddress { get; set; }
        public string SubnetMask { get; set; }
        public string Gateway { get; set; }
        public string Error { get; set; }
    }

    // 数据结构：通用操作结果（退出房间、销毁房间、踢出客户端等操作）
    public class OperationResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
    }
}

public static class IpHelper
{
    /// <summary>
    /// 检查指定的 IP 地址是否在本机上配置
    /// </summary>
    public static bool IsIpAlreadyInUse(string ipAddress)
    {
        if (!IPAddress.TryParse(ipAddress, out IPAddress targetIp))
        {
            throw new ArgumentException("无效的 IP 地址格式", nameof(ipAddress));
        }

        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface ni in interfaces)
        {
            IPInterfaceProperties ipProps = ni.GetIPProperties();
            foreach (UnicastIPAddressInformation ipInfo in ipProps.UnicastAddresses)
            {
                if (ipInfo.Address.Equals(targetIp))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 获取所有配置了指定 IP 的网络接口名称
    /// </summary>
    public static string[] GetInterfacesUsingIp(string ipAddress)
    {
        List<string> interfaceNames = new List<string>();

        if (!IPAddress.TryParse(ipAddress, out IPAddress targetIp))
        {
            throw new ArgumentException("无效的 IP 地址格式", nameof(ipAddress));
        }

        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface ni in interfaces)
        {
            IPInterfaceProperties ipProps = ni.GetIPProperties();
            foreach (UnicastIPAddressInformation ipInfo in ipProps.UnicastAddresses)
            {
                if (ipInfo.Address.Equals(targetIp))
                {
                    interfaceNames.Add(ni.Name);
                }
            }
        }
        return interfaceNames.ToArray();
    }

    /// <summary>
    /// 通过 netsh 命令删除指定接口上的 IP 配置
    /// </summary>
    public static void RemoveIp(string ipAddress, string interfaceName)
    {
        string arguments = $"interface ip delete address \"{interfaceName}\" addr={ipAddress}";

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "netsh",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        try
        {
            using (Process process = Process.Start(psi))
            {
                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd();
                string errorOutput = process.StandardError.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(output))
                    Console.WriteLine($"[netsh 输出] {output}");
                if (!string.IsNullOrWhiteSpace(errorOutput))
                    Console.WriteLine($"[netsh 错误] {errorOutput}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"删除 IP 时发生错误：{ex.Message}");
        }
    }
}
