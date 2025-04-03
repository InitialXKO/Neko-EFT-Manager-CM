using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Neko.EFT.Manager.X.NekoVPN;
using Microsoft.UI.Xaml.Media;
using Neko.EFT.Manager.X.Classes;
using Neko.EFT.Manager.X.Windows;
using System.ComponentModel;
using System.Management;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Neko.EFT.Manager.X;
using Neko.EFT.Manager.X.Controls;
using static Neko.EFT.Manager.X.Pages.VntConfigManagementPage;
using System.Text.RegularExpressions;
using Microsoft.UI.Dispatching;
using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using System.Security.Cryptography;
using System.Windows.Controls;
using Page = Microsoft.UI.Xaml.Controls.Page;
using PasswordBox = Microsoft.UI.Xaml.Controls.PasswordBox;
using TextBox = Microsoft.UI.Xaml.Controls.TextBox;
using ListViewItem = Microsoft.UI.Xaml.Controls.ListViewItem;
using Button = Microsoft.UI.Xaml.Controls.Button;
using StackPanel = Microsoft.UI.Xaml.Controls.StackPanel;
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;
using System.Text.Json;
using TextChangedEventArgs = Microsoft.UI.Xaml.Controls.TextChangedEventArgs;
using TextBlock = Microsoft.UI.Xaml.Controls.TextBlock;
using NLog.Fluent;


namespace Neko.EFT.Manager.Pro.NekoVPN
{
    public sealed partial class NetworkPage : Page
    {
        // 当前网络管理器实例（通过 UI 动态传入服务器地址）
        private NekoNetworkManager _networkManager;
        private CancellationTokenSource _chatPollingCts;
        // 模拟当前客户端的标识（实际项目中可使用硬件ID生成唯一标识）
        private readonly string _clientIp = ClientIdentifier.GetUniqueClientId();
        private string? _allocatedIp;
        private readonly string _clientIdentifier = ClientIdentifier.GetUniqueClientId();
        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        // 当前所在房间ID与房间密码（用于后续操作，如退出、解散、踢人等）
        private string _currentRoomId = string.Empty;
        private string _currentRoomPassword = string.Empty;
        public ObservableCollection<VntConfig> VntConfigs { get; set; } = new();
        // 标识当前客户端是否为房主
        private bool _isOwner = false;
        private DispatcherQueue _uiDispatcher;

        // 客户端列表数据源（绑定到 UI 中的 ClientListView）
        private ObservableCollection<ClientInfo> _clientList = new();

        private DispatcherTimer _statusTimer;
        public ObservableCollection<ChatMessage> ChatMessages { get; } = new ObservableCollection<ChatMessage>();

        public NetworkPage()
        {
            InitializeComponent();
            this.DataContext = this; // 让 XAML 绑定起作用
            this.Loaded += Page_Loaded;
            
            //SetBinding(Page.DataContextProperty, new Binding { Source = App.ManagerConfig });
            //_dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            // 假设 XAML 中已定义 ServerAddressBox、CreatPasswordBox、MaxClientsBox 等控件
            CreatedRoomPanel.Visibility = Visibility.Visible;
            JoinedRoomPanel.Visibility = Visibility.Visible;
            CreatedRoomStatusText.Text = "尚未创建房间";
            JoinedRoomStatusText.Text = "尚未加入房间";
            ClientListView.ItemsSource = _clientList;
            HostLoginUrlText.Text = "主机未就绪或未加入房间";





            string configPath = Path.Combine(Directory.GetCurrentDirectory(), "configs", "EFT.yaml");

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
            this.DataContext = this;


            if (string.IsNullOrWhiteSpace(ServerAddressBox.Password))
            {
                ServerAddressBox.Password = "127.0.0.1:12345";
            }
            // 初次实例化网络管理器
            _networkManager = new NekoNetworkManager(ServerAddressBox.Password.Trim());
        }

        private void ServerInfoBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            // 获取输入的加密字符串
            string encryptedBase64 = ServerInfoBox.Text;
            if (string.IsNullOrWhiteSpace(encryptedBase64))
            {
                ServerInfoBox.PlaceholderText = null;
                return;
            }

            try
            {


                App.ManagerConfig.NRMSKey = ServerInfoBox.Text; // 更新并保存
                // 将 Base64 字符串转换为字节数组
                byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);
                // 使用与服务端相同的密钥和 IV 解密
                string decryptedText = DecryptAES(encryptedBytes, "0123456789ABCDEF", "ABCDEF0123456789");
                // 假设解密后的格式为 "NRMS地址;VNT地址"
                string[] addresses = decryptedText.Split(';');
                if (addresses.Length == 2)
                {
                    //ServerInfoBox.ToolTip = $"NRMS: {addresses[0]}, VNT: {addresses[1]}";

                    App.ManagerConfig.RoomManagementServer = addresses[0];
                    ServerAddressBox.Password = addresses[0];
                    App.ManagerConfig.VNTServer = addresses[1];
                    VNTServerAddressBox.Password = addresses[1];
                }
                else
                {

                    ServerAddressBox.Password = "解密后的格式不正确";
                    VNTServerAddressBox.Password = "解密后的格式不正确";
                    
                }
            }
            catch (Exception ex)
            {
                ServerAddressBox.Password = "解密失败";
                VNTServerAddressBox.Password = "解密失败";
            }
        }

        /// <summary>
        /// 使用 AES 算法解密加密数据，返回明文字符串。
        /// </summary>
        /// <param name="encryptedBytes">加密后的字节数组</param>
        /// <param name="key">密钥（16 字节）</param>
        /// <param name="iv">初始向量（16 字节）</param>
        /// <returns>解密后的明文字符串</returns>
        private string DecryptAES(byte[] encryptedBytes, string key, string iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = Encoding.UTF8.GetBytes(iv);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _uiDispatcher = DispatcherQueue.GetForCurrentThread();
            if (App.ManagerConfig != null)

            {

                ServerInfoBox.Text = App.ManagerConfig.NRMSKey ?? "请输入服务器加密地址";
                // 如果 RoomManagementServer 为 null，则使用默认地址 "127.0.0.1:12345"
                ServerAddressBox.Password = App.ManagerConfig.RoomManagementServer ?? "127.0.0.1:12345";

                // 如果 VNTServer 为 null，则使用默认地址 "127.0.0.1:12346"
                VNTServerAddressBox.Password = App.ManagerConfig.VNTServer ?? "127.0.0.1:12346";
            }
        }



        private bool IsValidIpAndPort(string input)
        {
            string pattern = @"^((25[0-5]|2[0-4][0-9]|1\d{2}|[1-9]?\d)\.){3}(25[0-5]|2[0-4][0-9]|1\d{2}|[1-9]?\d):([1-9][0-9]{0,4})$";
            return Regex.IsMatch(input, pattern);
        }

        // 校验 Room Management 服务器地址
        private void ServerAddressBox_PaaswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            if (passwordBox != null && !string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                if (IsValidIpAndPort(passwordBox.Password))
                {
                    App.ManagerConfig.RoomManagementServer = passwordBox.Password; // 更新并保存
                    passwordBox.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Green);
                }
                else
                {
                    passwordBox.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Red);
                }
            }
        }

        private void VNTServerAddressBox_PaaswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            if (passwordBox != null && !string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                if (IsValidIpAndPort(passwordBox.Password))
                {
                    App.ManagerConfig.VNTServer = passwordBox.Password; // 更新并保存
                    passwordBox.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Green);
                }
                else
                {
                    passwordBox.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Red);
                }
            }
        }




        /// <summary>
        /// 每次操作前，根据 UI 中的服务器地址重新创建网络管理器实例
        /// </summary>
        private NekoNetworkManager GetNetworkManager()
        {
            if (ServerAddressBox == null)
            {
                throw new InvalidOperationException("ServerAddressBox 控件未初始化。");
            }

            string serverAddress = ServerAddressBox.Password?.Trim();
            if (string.IsNullOrWhiteSpace(serverAddress))
            {
                // 如果输入为空，则使用默认地址
                serverAddress = "110.42.41.105:12345";
            }
            // 重新创建实例
            _networkManager = new NekoNetworkManager(serverAddress);
            return _networkManager;
        }

        private void StartStatusPolling()
        {
            _statusTimer = new DispatcherTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(3);
            _statusTimer.Tick += async (s, e) =>
            {


                if (!string.IsNullOrEmpty(_currentRoomId))
                {
                    var manager = GetNetworkManager();  // 使用最新的服务器地址创建新的实例
                    var status = await manager.GetRoomStatusAsync(_currentRoomId, CancellationToken.None);
                    if (status == null)
                    {
                        Console.WriteLine("警告: status 为空，可能是服务器未返回数据！");
                        return;  // 直接返回，防止 NullReferenceException
                    }

                    if (!status.Exists)
                    {
                        ShowRoomDisbandedMessage();
                        _statusTimer.Stop();
                    }
                    else
                    {
                        UpdateClientList(status.Clients);
                        if (!string.IsNullOrEmpty(status.HostLoginUrl))
                        {
                            HostLoginUrlText.Text = $"房主服务端登录地址: {status.HostLoginUrl}";
                            HostLoginUrlText.Foreground = new SolidColorBrush(Colors.DarkCyan); // 设置文字为红色
                        }
                        else
                        {
                            HostLoginUrlText.Text = "房主未就绪";
                            HostLoginUrlText.Foreground = new SolidColorBrush(Colors.DarkRed); // 设置文字为红色

                        }
                    }
                }
            };
            _statusTimer.Start();
        }


        //private void StartChatPolling(string roomId)

        //{
        //    Debug.WriteLine("StartChatPolling 线程ID：" + System.Threading.Thread.CurrentThread.ManagedThreadId);  
        //    StopChatPolling(); // 若已有轮询任务，则先取消
        //    _chatPollingCts = new CancellationTokenSource();

        //    Debug.WriteLine("启动聊天轮询任务...");
        //    // 启动后台任务，不需要等待结果
        //    _ = PollChatMessagesAsync(roomId, _chatPollingCts.Token);
        //    Debug.WriteLine("聊天轮询任务已启动。");  
        //}

        //private void StopChatPolling()
        //{
        //    _chatPollingCts?.Cancel();
        //    _chatPollingCts = null;
        //}
        //private async Task PollChatMessagesAsync(string roomId, CancellationToken cancellationToken)
        //{

        //    Debug.WriteLine("PollChatMessagesAsync 线程ID：" + System.Threading.Thread.CurrentThread.ManagedThreadId);
        //    // 使用页面加载时捕获的 _uiDispatcher，而不是每次调用 GetForCurrentThread()
        //    if (_uiDispatcher == null)
        //    {
        //        Debug.WriteLine("UI调度器未初始化。");
        //        return;
        //    }

        //    while (!cancellationToken.IsCancellationRequested)
        //    {
        //        try
        //        {
        //            if (ChatListView == null)
        //            {
        //                Debug.WriteLine("ChatListView 已被销毁，停止 UI 更新");
        //                return;
        //            }

        //            // 调用网络管理器获取消息
        //            Debug.WriteLine("正在轮询消息...");
        //            var result = await _networkManager.GetMessagesAsync(roomId, cancellationToken);
        //            Debug.WriteLine("轮询消息完成。");

        //            if (result != null && result.Success && result.Messages != null)
        //            {
        //                await DispatcherQueue.EnqueueAsync(() =>
        //                {
        //                    try
        //                    {
        //                        if (ChatListView != null)
        //                        {
        //                            ChatMessages.Clear();
        //                            foreach (var msg in result.Messages)
        //                            {
        //                                ChatMessages.Add(msg);
        //                            }
        //                        }
        //                        Debug.WriteLine("UI 更新完成。");
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        Debug.WriteLine($"UI 更新失败: {ex.Message}");
        //                    }
        //                });
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine($"轮询消息时出错：{ex.Message}");
        //        }


        //        try
        //        {
        //            await Task.Delay(1000, cancellationToken);
        //        }
        //        catch (TaskCanceledException)
        //        {
        //            break;
        //        }
        //    }
        //}


        //private async void SendChatMessage_Click(object sender, RoutedEventArgs e)
        //{
        //    // 获取用户输入的消息内容
        //    string message = ChatInputTextBox.Text;
        //    if (!string.IsNullOrWhiteSpace(message) && !string.IsNullOrEmpty(_currentRoomId))
        //    {
        //        try
        //        {
        //            // 调用网络管理器的 SendMessageAsync 方法发送消息
        //            var sendResult = await _networkManager.SendMessageAsync(_currentRoomId, message, CancellationToken.None);
        //            if (sendResult.Success)
        //            {
        //                // 消息发送成功后，清空输入框
        //                ChatInputTextBox.Text = "";
        //            }
        //            else
        //            {
        //                Debug.WriteLine("发送消息失败：" + sendResult.Error);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine("发送消息时发生异常：" + ex.Message);
        //        }
        //    }
        //}


        private void ShowRoomDisbandedMessage()
        {
            // 提示用户房间已解散，比如通过 InfoBar 或弹窗
            JoinStatusInfoBar.Title = "房间解散";
            JoinStatusInfoBar.Message = "房主已解散房间，请重新加入新的房间。";
            JoinStatusInfoBar.Severity = InfoBarSeverity.Warning;
            JoinStatusInfoBar.IsOpen = true;
            JoinedRoomPanel.Visibility = Visibility.Collapsed;
            JoinedRoomStatusText.Text = "尚未加入房间";
            JoinedRoomIPText.Text = string.Empty;
        }

        private void UpdateClientList(List<ClientInfo> clients)
        {
            _clientList.Clear();
            foreach (var client in clients)
            {
                _clientList.Add(client);
            }
        }

        /// <summary>
        /// 创建房间后，通过调用加入房间逻辑来加入房间，而不是直接添加数据。
        /// </summary>
        private async void CreateRoom_Click(object sender, RoutedEventArgs e)
        {
            try
            {


                string configPath = Path.Combine(Directory.GetCurrentDirectory(), "configs", "EFT.yaml");

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


                if (!string.IsNullOrWhiteSpace(VNTServerAddressBox.Password))
                {
                    YamlConfigManager.UpdateServerAddress(configPath, VNTServerAddressBox.Password);
                }
                else
                {
                    // 可选：提示用户输入有效的服务器地址
                    Debug.WriteLine("服务器地址不能为空！");
                }



                CreatedRoomIdText.Text = "正在创建房间...";
                var password = CreatPasswordBox.Password;
                if (string.IsNullOrWhiteSpace(password))
                {
                    ShowCreationError("密码不能为空", "请输入房间密码。");
                    return;
                }

                // 从 UI 中获取最大房间人数，假设 MaxClientsBox 是 TextBox 控件
                string maxClientsText = MaxClientsBox.Text?.Trim();
                if (!int.TryParse(maxClientsText, out int maxClients) || maxClients < 1)
                {
                    ShowCreationError("无效人数", "请输入有效的房间最大人数。");
                    return;
                }

                // 使用 UI 中最新的服务器地址创建网络管理器实例
                var manager = GetNetworkManager();

                // 调用创建房间接口
                var room = await manager.CreateRoomAsync(password, maxClients, CancellationToken.None);
                if (room == null)
                {
                    ShowCreationError("创建失败", "未能创建房间。");
                    return;
                }

                // 记录房间信息，并设置当前客户端为房主
                _currentRoomId = room.Id;
                _currentRoomPassword = password;
                _isOwner = true;

                // 显示创建成功信息（此处可显示房间ID、子网等）
                CreatedRoomIdText.Text = $"房间创建成功！\n房间ID: {room.Id}\n子网: {room.Subnet}";
                CreatedRoomIdText.Text = $"房间创建成功！\n房间ID: {room.Id}\n子网: {room.Subnet}";
                ShowCreationSuccess(room.Id);

                // 通过调用加入房间逻辑加入房间，而不是直接添加数据
                var joinResult = await manager.JoinRoomAsync(room.Id, password, CancellationToken.None);
                if (!joinResult.Success)
                {
                    ShowCreationError("加入房间失败", joinResult.Error);
                    return;
                }

                

                // 更新加入成功的状态提示
                ShowJoinSuccess(joinResult.IpAddress);

                // 更新客户端列表，从服务器获取最新数据
                var status = await manager.GetRoomStatusAsync(room.Id, CancellationToken.None);
                if (status != null && status.Exists)
                {
                    UpdateClientList(status.Clients);
                    // 如果房主的完整登录地址已设置，则显示到 UI 中（例如显示在 HostLoginUrlLabel 控件中）
                    if (!string.IsNullOrEmpty(status.HostLoginUrl))
                    {
                        HostLoginUrlText.Text = $"房主服务端登录地址: {status.HostLoginUrl}";
                    }
                    else
                    {
                        HostLoginUrlText.Text = "房主未就绪";
                    }
                }

                //if (!string.IsNullOrEmpty(_currentRoomId))
                //{
                //    StartChatPolling(_currentRoomId);
                //}

                var EFTconfigPath = Path.Combine(Directory.GetCurrentDirectory(), "configs", "EFT.yaml");
                await ConnectOrDisconnectAsync(EFTconfigPath);

                // 显示房主管理面板（仅房主可见）
                HostOperationsPanel.Visibility = Visibility.Visible;
                UpdateKickButtonsVisibility();
                StartStatusPolling();
                
            }
            catch (Exception ex)
            {
                ShowCreationError("创建失败", ex.Message);
            }
        }

        private void ShowCreationError(string title, string message)
        {
            CreateStatusInfoBar.Title = title;
            CreateStatusInfoBar.Message = message;
            CreateStatusInfoBar.Severity = InfoBarSeverity.Error;
            CreateStatusInfoBar.IsOpen = true;

            // 重置创建状态
            CreatedRoomPanel.Visibility = Visibility.Collapsed;
            CreatedRoomStatusText.Text = "尚未创建房间";
            CreatedRoomIdText.Text = string.Empty;
        }

        /// <summary>
        /// 加入房间逻辑（非房主）  
        /// </summary>
        private async void JoinRoom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var roomId = JoinRoomIdBox.Text.Trim().ToUpper();
                if (!ValidateRoomId(roomId))
                {
                    ShowJoinError("房间ID格式无效", "请输入5位由字母和数字组成的房间ID。");
                    return;
                }

                var password = JoinPasswordBox.Password;
                if (string.IsNullOrWhiteSpace(password))
                {
                    ShowJoinError("密码不能为空", "请输入房间密码。");
                    return;
                }

                // 使用 UI 中最新的服务器地址创建网络管理器实例
                var manager = GetNetworkManager();
                var result = await manager.JoinRoomAsync(roomId, password, CancellationToken.None);

                if (result.Success)
                {
                    // 记录当前房间信息（加入房间后当前客户端通常不是房主）
                    _currentRoomId = roomId;
                    _currentRoomPassword = password;
                    _isOwner = false;  // 加入房间默认认为不是房主
                    _allocatedIp = result.IpAddress;


                    ShowJoinSuccess(result.IpAddress);
                    

                    // 模拟更新客户端列表，实际中应从服务器获取完整列表
                    var status = await manager.GetRoomStatusAsync(roomId, CancellationToken.None);
                    if (status != null && status.Exists)
                    {
                        UpdateClientList(status.Clients);
                        // 如果房主的完整登录地址已设置，则显示到 UI 中（例如显示在 HostLoginUrlLabel 控件中）
                        if (!string.IsNullOrEmpty(status.HostLoginUrl))
                        {
                            HostLoginUrlText.Text = $"房主服务端登录地址: {status.HostLoginUrl}";
                        }
                        else
                        {
                            HostLoginUrlText.Text = "房主未就绪";
                        }
                    }

                    _currentRoomId = roomId; // 例如

                    // 调试日志，确认当前线程和 _uiDispatcher 是否有效
                    Debug.WriteLine("JoinRoom_Click 线程ID：" + System.Threading.Thread.CurrentThread.ManagedThreadId);
                    Debug.WriteLine("_uiDispatcher 是否为 null: " + (_uiDispatcher == null));

                    // 启动聊天轮询
                    //StartChatPolling(_currentRoomId);
                    var EFTconfigPath = Path.Combine(Directory.GetCurrentDirectory(), "configs", "EFT.yaml");
                    await ConnectOrDisconnectAsync(EFTconfigPath);
                    // 房主管理面板仅房主可见
                    HostOperationsPanel.Visibility = Visibility.Collapsed;
                    UpdateKickButtonsVisibility();
                    StartStatusPolling();
                }
                else
                {
                    ShowJoinError("加入失败", result.Error ?? "未知错误");
                }
            }
            catch (Exception ex)
            {
                ShowJoinError("发生异常", ex.Message);
            }
        }

        private void ShowJoinError(string title, string message)
        {
            JoinStatusInfoBar.Title = title;
            JoinStatusInfoBar.Message = message;
            JoinStatusInfoBar.Severity = InfoBarSeverity.Error;
            JoinStatusInfoBar.IsOpen = true;

            // 重置加入状态
            JoinedRoomPanel.Visibility = Visibility.Collapsed;
            JoinedRoomStatusText.Text = "尚未加入房间";
            JoinedRoomIPText.Text = string.Empty;
        }

        private bool ValidateRoomId(string roomId)
        {
            const string validChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            return roomId.Length == 5 &&
                   roomId.All(c => validChars.Contains(c));
        }

        private void RoomId_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            var validChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            sender.Text = new string(sender.Text.Where(c => validChars.Contains(char.ToUpper(c))).ToArray());
        }

        private void ShowCreationSuccess(string roomId)
        {
            CreatedRoomPanel.Visibility = Visibility.Visible;
            CreatedRoomStatusText.Text = "创建房间成功";
            CreatedRoomIdText.Text = $"ID: {roomId}";
            CreatedRoomPasswordText.Text = $"密码: {_currentRoomPassword}";
            CreateStatusInfoBar.Title = "创建成功";
            CreateStatusInfoBar.Message = "房间已就绪，可分享ID给其他用户";
            CreateStatusInfoBar.Severity = InfoBarSeverity.Success;
            CreateStatusInfoBar.IsOpen = true;
        }





        private async void CopyRoomId_Click(object sender, RoutedEventArgs e)
        {
            // 提取房间ID和密码（已通过UI生成并验证）
            string roomId = CreatedRoomIdText.Text.Split(':').Last().Trim();
            string roomPassword = CreatedRoomPasswordText.Text.Split(':').Last().Trim();

            // 格式标记 + 分隔符
            string combined = $"ROOMIDv1|{roomId}|{roomPassword}";

            // Base64编码并替换敏感字符
            string base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(combined))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

            // 设置剪贴板
            var package = new DataPackage();
            package.SetText(base64Encoded);
            Clipboard.SetContent(package);

            // 显示成功提示
            CreateStatusInfoBar.Title = "已复制";
            CreateStatusInfoBar.Message = "分享码已复制到剪贴板";
            CreateStatusInfoBar.Severity = InfoBarSeverity.Success;
            CreateStatusInfoBar.IsOpen = true;
        }
        private void RoomIdKeyBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 获取用户输入的分享码并去除首尾空白字符
            string shareCode = RoomIdKeyBox.Text.Trim();
            Debug.WriteLine($"[RoomIdKeyBox_TextChanged] 输入的分享码: '{shareCode}'");

            if (!string.IsNullOrEmpty(shareCode))
            {
                try
                {
                    // 修复Base64兼容性问题：替换URL安全字符并补全等号
                    shareCode = shareCode
                        .Replace('-', '+')  // 恢复Base64标准字符
                        .Replace('_', '/'); // 恢复Base64标准字符

                    // 补全Base64末尾的等号（根据Base64规范）
                    switch (shareCode.Length % 4)
                    {
                        case 2: shareCode += "=="; break;
                        case 3: shareCode += "="; break;
                    }

                    // Base64 解码
                    byte[] decodedBytes = Convert.FromBase64String(shareCode);
                    string decoded = Encoding.UTF8.GetString(decodedBytes);
                    Debug.WriteLine($"[RoomIdKeyBox_TextChanged] 解码后的字符串: '{decoded}'");

                    // 格式验证：检查是否包含版本标记和分隔符
                    if (decoded.StartsWith("ROOMIDv1|") && decoded.Count(c => c == '|') == 2)
                    {
                        string[] parts = decoded.Split('|');
                        Debug.WriteLine($"[RoomIdKeyBox_TextChanged] 分割后的部分数量: {parts.Length}");

                        // 提取房间ID和密码（索引1为ID，索引2为密码）
                        string roomId = parts[1].Trim();
                        string password = parts[2].Trim();

                        // 验证房间ID格式（5位大写字母+数字）
                        if (Regex.IsMatch(roomId, "^[A-HJ-NP-Z2-9]{5}$"))
                        {
                            JoinRoomIdBox.Text = roomId;
                            JoinPasswordBox.Password = password;
                            Debug.WriteLine($"[RoomIdKeyBox_TextChanged] 成功解析：房间ID = '{roomId}', 房间密码 = '{password}'");

                            JoinStatusInfoBar.Title = "解析成功";
                            JoinStatusInfoBar.Message = "房间信息已填充";
                            JoinStatusInfoBar.Severity = InfoBarSeverity.Success;
                            JoinStatusInfoBar.IsOpen = true;
                        }
                        else
                        {
                            Debug.WriteLine("[RoomIdKeyBox_TextChanged] 房间ID格式非法");
                            JoinStatusInfoBar.Title = "无效的房间ID";
                            JoinStatusInfoBar.Message = "分享码中的房间ID不符合规则";
                            JoinStatusInfoBar.Severity = InfoBarSeverity.Error;
                            JoinStatusInfoBar.IsOpen = true;
                        }
                    }
                    else
                    {
                        Debug.WriteLine("[RoomIdKeyBox_TextChanged] 解码结果格式错误");
                        JoinStatusInfoBar.Title = "无效的分享码";
                        JoinStatusInfoBar.Message = "分享码格式不兼容，请检查";
                        JoinStatusInfoBar.Severity = InfoBarSeverity.Error;
                        JoinStatusInfoBar.IsOpen = true;
                    }
                }
                catch (FormatException ex)
                {
                    Debug.WriteLine($"[RoomIdKeyBox_TextChanged] Base64 格式错误: {ex.Message}");
                    JoinStatusInfoBar.Title = "分享码格式错误";
                    JoinStatusInfoBar.Message = "请检查是否为有效的分享码";
                    JoinStatusInfoBar.Severity = InfoBarSeverity.Error;
                    JoinStatusInfoBar.IsOpen = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[RoomIdKeyBox_TextChanged] 解析错误: {ex.Message}");
                    JoinStatusInfoBar.Title = "解析错误";
                    JoinStatusInfoBar.Message = "处理分享码时发生意外错误";
                    JoinStatusInfoBar.Severity = InfoBarSeverity.Error;
                    JoinStatusInfoBar.IsOpen = true;
                }
            }
            else
            {
                Debug.WriteLine("[RoomIdKeyBox_TextChanged] 输入框为空");
                JoinStatusInfoBar.IsOpen = false;
            }
        }



        // 复制房主分配的 IP 地址（需在 XAML 中添加相应按钮，并绑定此事件）
        private void CopyAssignedIP_Click(object sender, RoutedEventArgs e)
        {
            // 假设分配的 IP 已显示在 CreatedRoomStatusText 中，格式为 "分配IP地址: xxx.xxx.xxx.xxx"
            string assignedIP = ExtractAssignedIPFromStatusText(CreatedRoomStatusText.Text);
            if (!string.IsNullOrEmpty(assignedIP))
            {
                var package = new DataPackage();
                package.SetText(assignedIP);
                Clipboard.SetContent(package);

                CreateStatusInfoBar.Title = "已复制";
                CreateStatusInfoBar.Message = "分配IP地址已复制到剪贴板";
                CreateStatusInfoBar.Severity = InfoBarSeverity.Success;
                CreateStatusInfoBar.IsOpen = true;
            }
        }

        // 根据文本提取分配IP地址（请根据实际文本格式调整）
        private string ExtractAssignedIPFromStatusText(string text)
        {
            const string marker = "分配IP地址:";
            int idx = text.IndexOf(marker);
            if (idx >= 0)
            {
                return text.Substring(idx + marker.Length).Trim();
            }
            return string.Empty;
        }

        private void ShowJoinSuccess(string ipAddress)
        {
            JoinedRoomPanel.Visibility = Visibility.Visible;
            JoinedRoomStatusText.Text = "加入房间成功";
            JoinStatusInfoBar.Title = "加入成功";
            JoinStatusInfoBar.Message = $"已分配IP地址: {ipAddress}";
            JoinedRoomIPText.Text = $"已分配IP地址: {ipAddress}";
            JoinStatusInfoBar.Severity = InfoBarSeverity.Success;
            JoinStatusInfoBar.IsOpen = true;
        }


        private void ShowHostSuccess(string ipAddress)
        {
            
            JoinStatusInfoBar.Title = "主机已就绪";
            JoinStatusInfoBar.Message = $"主机IP地址: {ipAddress}";
            
            JoinStatusInfoBar.Severity = InfoBarSeverity.Success;
            JoinStatusInfoBar.IsOpen = true;
        }

        private async void CopyUserIP_Click(object sender, RoutedEventArgs e)
        {
            var package = new DataPackage();
            package.SetText(JoinedRoomIPText.Text.Split(':').Last().Trim());
            Clipboard.SetContent(package);

            CreateStatusInfoBar.Title = "已复制";
            CreateStatusInfoBar.Message = "分配IP已复制到剪贴板";
            CreateStatusInfoBar.Severity = InfoBarSeverity.Success;
            CreateStatusInfoBar.IsOpen = true;
        }

        
        #region 房主管理操作

        // 解散房间（仅房主可操作）
        private async void DestroyRoom_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentRoomId))
                return;

            var manager = GetNetworkManager();
            var result = await manager.DestroyRoomAsync(_currentRoomId, _currentRoomPassword, CancellationToken.None);
            if (result.Success)
            {
                _currentRoomId = string.Empty;
                _clientList.Clear();
                HostOperationsPanel.Visibility = Visibility.Collapsed;
                ShowCreationError("房间已解散", "房间已被房主解散。");
                ShowRoomDisbandedMessage();
                //StopChatPolling();
            }

            else
            {
                CreateStatusInfoBar.Title = "解散失败";
                CreateStatusInfoBar.Message = result.Error;
                CreateStatusInfoBar.Severity = InfoBarSeverity.Error;
                CreateStatusInfoBar.IsOpen = true;
            }
        }

        // 踢出某个客户端（仅房主可操作）
        private async void KickButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentRoomId))
                return;

            if ((sender as Button)?.DataContext is ClientInfo targetClient)
            {
                Debug.WriteLine($"尝试踢出客户端: {targetClient.IpAddress}");

                // 确保不会踢出自己
                if (targetClient.IpAddress == _clientIp)
                {
                    Debug.WriteLine("不能踢出自己！");
                    return;
                }

                var manager = GetNetworkManager();
                var result = await manager.KickClientAsync(_currentRoomId, _currentRoomPassword, targetClient.IpAddress, CancellationToken.None);

                Debug.WriteLine($"踢出请求结果: {result.Success}, 错误信息: {result.Error}");

                if (result.Success)
                {
                    _clientList.Remove(targetClient);
                    Debug.WriteLine("成功踢出，已从列表中移除。");
                }
                else
                {
                    CreateStatusInfoBar.Title = "踢出失败";
                    CreateStatusInfoBar.Message = result.Error;
                    CreateStatusInfoBar.Severity = InfoBarSeverity.Error;
                    CreateStatusInfoBar.IsOpen = true;
                }
            }
        }


        // 更新客户端列表中“踢出”按钮的可见性
        private void UpdateKickButtonsVisibility()
        {
            foreach (var item in ClientListView.Items)
            {
                if (ClientListView.ContainerFromItem(item) is ListViewItem container)
                {
                    var kickButton = FindVisualChildByName<Button>(container, "KickButton");
                    if (kickButton != null)
                    {
                        var clientInfo = item as ClientInfo;
                        kickButton.Visibility = (_isOwner && clientInfo.IpAddress != _clientIp)
                            ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }
        }

        // 辅助方法：在 Visual Tree 中查找指定名称的子控件
        private T FindVisualChildByName<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            if (parent == null)
                return null;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement fe && fe.Name == name)
                    return (T)child;

                var result = FindVisualChildByName<T>(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        // 退出房间（非房主提供退出功能）
        private async void ExitRoom_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentRoomId))
                return;

            try
            {
                var manager = GetNetworkManager();
                var result = await manager.ExitRoomAsync(_currentRoomId, _allocatedIp, CancellationToken.None);
                if (result.Success)
                {
                    _currentRoomId = string.Empty;
                    _currentRoomPassword = string.Empty;
                    _clientList.Clear();
                    JoinedRoomPanel.Visibility = Visibility.Collapsed;
                    JoinStatusInfoBar.Title = "退出成功";
                    JoinStatusInfoBar.Message = "您已退出房间";
                    JoinStatusInfoBar.Severity = InfoBarSeverity.Informational;
                    JoinStatusInfoBar.IsOpen = true;

                    //StopChatPolling();
                }
                else
                {
                    ShowJoinError("退出失败", result.Error ?? "未知错误");
                }
            }
            catch (Exception ex)
            {
                ShowJoinError("退出异常", ex.Message);
            }
        }

        #endregion

        //private void ConnetButton_Click(object sender, RoutedEventArgs e)
        //{



        //}



        private void ShowIsConnectingSuccess(string ipAddress)
        {
            
            JoinStatusInfoBar.Title = "正在连接至VNT组网网络...";
            JoinStatusInfoBar.Message = $"已分配的IP地址: {ipAddress}";
            
            JoinStatusInfoBar.Severity = InfoBarSeverity.Warning;
            JoinStatusInfoBar.IsOpen = true;
        }

        private void ShowIsConnectedSuccess(string ipAddress)
        {
            
            JoinStatusInfoBar.Title = "VNT组网网络连接成功";
            JoinStatusInfoBar.Message = $"已分配的IP地址: {ipAddress}";
            
            JoinStatusInfoBar.Severity = InfoBarSeverity.Success;
            JoinStatusInfoBar.IsOpen = true;
        }

        private void ShowIsDisConnectedSuccess(string ipAddress)
        {
            
            JoinStatusInfoBar.Title = "VNT组网网络连接已断开";
            JoinStatusInfoBar.Message = $"已分配IP地址: {ipAddress}";
            
            JoinStatusInfoBar.Severity = InfoBarSeverity.Error;
            JoinStatusInfoBar.IsOpen = true;
        }
        private void UpdateConnectButtonState(VntConfig vntConfig)
    {
        if (vntConfig.IsConnecting)
        {
                // 连接中：禁用按钮，显示“连接中...”，颜色变为灰色
                ShowIsConnectingSuccess(vntConfig.ip);
            ConnectButton.Content = "连接中...";
            ConnectButton.IsEnabled = false;
            ConnectButton.Background = new SolidColorBrush(Colors.Gray);
        }
        else if (vntConfig.IsConnected)
        {
                ShowIsConnectedSuccess(vntConfig.ip);
                // 已连接：按钮可用，显示“断开连接”，颜色变为红色
                ConnectButton.Content = "断开连接";
            ConnectButton.IsEnabled = true;
            ConnectButton.Background = new SolidColorBrush(Colors.Red);
        }
        else
        {

                ShowIsDisConnectedSuccess(vntConfig.ip);
                // 未连接：按钮可用，显示“连接组网”，颜色变为绿色
                ConnectButton.Content = "连接组网";
            ConnectButton.IsEnabled = true;
            ConnectButton.Background = new SolidColorBrush(Colors.Green);
        }
    }



    private async void DisConnect_Click(object sender, RoutedEventArgs e)
        {
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "configs", "EFT.yaml");
            var vntConfig = YamlConfigManager.LoadConfig(configPath); ;

            if (vntConfig != null)
            {
                await DisConnect(vntConfig);
                vntConfig.ConnectionStatus = "已断开";
                YamlConfigManager.SaveConfig(vntConfig, configPath);
            }
        }


        private async Task ConfigRevHost(VntConfig config)
        {
            if (config == null) return;

            var managerConfig = App.ManagerConfig; // 假设已初始化
            if (managerConfig == null)
            {
                ShowMessage("无法找到ManagerConfig实例！");
                return;
            }

            string installPath = managerConfig.InstallPath;


            if (string.IsNullOrEmpty(installPath))
            {
                ShowMessage("安装路径无效！");
                return;
            }
            string fikaPath = Path.Combine(installPath, "user", "mods", "fika-server", "assets", "configs", "fika.jsonc");

            if (!File.Exists(fikaPath))
            {

            }
            else
            {
                try
                {
                    string fikaConfig = await File.ReadAllTextAsync(fikaPath);
                    var fikaDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(fikaConfig);

                    if (fikaDict != null && fikaDict.ContainsKey("server"))
                    {
                        var serverNode = (JsonElement)fikaDict["server"];

                        if (serverNode.TryGetProperty("SPT", out JsonElement sptNode) &&
                            sptNode.TryGetProperty("http", out JsonElement httpNode))
                        {
                            // 修改 http 配置
                            var httpConfig = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(httpNode.GetRawText());
                            if (httpConfig != null)
                            {
                                httpConfig["ip"] = "0.0.0.0";
                                httpConfig["backendIp"] = "127.0.0.1";

                                // 更新 SPT 配置
                                var sptConfig = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(sptNode.GetRawText());
                                sptConfig["http"] = httpConfig;

                                // 更新 Server 配置
                                var serverConfig = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(serverNode.GetRawText());
                                serverConfig["SPT"] = sptConfig;

                                fikaDict["server"] = serverConfig;

                                // 写回修改后的 JSON
                                string updatedFikaJson = System.Text.Json.JsonSerializer.Serialize(fikaDict, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                                await File.WriteAllTextAsync(fikaPath, updatedFikaJson);


                            }
                        }
                    }
                    else
                    {

                    }
                }
                catch (Exception ex)
                {

                }

                string sptPath = Path.Combine(installPath, "SPT_Data", "Server", "configs", "http.json");
                string akiPath = Path.Combine(installPath, "aki_Data", "Server", "configs", "http.json");
                string configFilePath = File.Exists(sptPath) ? sptPath : akiPath;

                if (!File.Exists(configFilePath))
                {
                    ShowMessage("配置文件未找到！");
                    return;
                }

                try
                {
                    // 读取 JSON
                    string fileContent = await File.ReadAllTextAsync(configFilePath);
                    var configDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(fileContent);
                    if (configDict == null)
                    {
                        ShowMessage("配置文件解析失败！");
                        return;
                    }

                    // 修改配置项
                    configDict["ip"] = "0.0.0.0";
                    configDict["backendIp"] = "127.0.0.1";

                    string updatedJson = System.Text.Json.JsonSerializer.Serialize(configDict, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                    // 写回文件
                    await File.WriteAllTextAsync(configFilePath, updatedJson);

                    // 弹窗提示
                    var dialog = new ContentDialog
                    {
                        XamlRoot = this.XamlRoot,
                        Title = "配置已恢复",
                        Content = $"http.json 配置已恢复:\n backendIp 恢复为: \"127.0.0.1\"\n 配置文件已恢复默认值。",
                        PrimaryButtonText = "确定",
                        SecondaryButtonText = "取消",
                        DefaultButton = ContentDialogButton.Primary
                    };

                    var result = await dialog.ShowAsync();
                    //if (result == ContentDialogResult.Primary)
                    //{
                    //    OpeSPTServerManagerWindow(); // 打开服务端管理器
                    //}
                }
                catch (Exception ex)
                {
                    ShowMessage($"发生错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        ShowMessage($"内部异常: {ex.InnerException.Message}\n堆栈跟踪: {ex.InnerException.StackTrace}");
                    }
                }
            }
        }


        /// <summary>
        /// 连接或断开连接的核心逻辑
        /// </summary>
        /// <param name="configPath">配置文件路径</param>
        /// <param name="selectedMode">连接模式（可选，如果未提供则显示对话框）</param>
        /// <returns>是否成功执行操作</returns>
        public async Task<bool> ConnectOrDisconnectAsync(string configPath, string selectedMode = null)
        {
            var vntConfig = YamlConfigManager.LoadConfig(configPath);
            if (vntConfig == null) return false;

            // 如果当前未连接，则开始连接
            if (!vntConfig.IsConnected)
            {
                // 如果未提供连接模式，则显示对话框选择
                if (string.IsNullOrEmpty(selectedMode))
                {
                    selectedMode = await ShowConnectionModeDialog();
                    if (string.IsNullOrEmpty(selectedMode)) return false; // 用户取消
                }

                vntConfig.IsConnecting = true;  // 进入连接中状态
                UpdateConnectButtonState(vntConfig);  // 更新按钮状态

                bool success = false;
                switch (selectedMode)
                {
                    case "Host":
                        success = await ConnectToServer(vntConfig);
                        if (success)
                        {
                            await ConfigHostFIKA(vntConfig);
                        }
                        break;
                    case "Client":
                        success = await ConnectToServer(vntConfig);
                        if (success)
                        {
                            await ConfigClient();
                        }
                        break;
                    case "MatchHost":
                        success = await ConnectToServer(vntConfig);
                        if (success)
                        {
                            await ConfigFIKA(vntConfig);
                        }
                        break;
                    case "NetworkOnly":
                        success = await ConnectToServer(vntConfig);
                        break;
                }

                vntConfig.IsConnected = success;
                vntConfig.IsConnecting = false;
                UpdateConnectButtonState(vntConfig);  // 根据最终状态更新按钮


            }
            else // 已连接则断开
            {
                await DisConnect(vntConfig);
                vntConfig.IsConnected = false;
                UpdateConnectButtonState(vntConfig);


            }

            // 保存配置
            YamlConfigManager.SaveConfig(vntConfig, configPath);
            return true;
        }

        private async void ConnetButton_Click(object sender, RoutedEventArgs e)
        {
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "configs", "EFT.yaml");
            var vntConfig = YamlConfigManager.LoadConfig(configPath);
            if (vntConfig == null) return;

            // 如果当前未连接，则开始连接
            if (!vntConfig.IsConnected)
            {
                // 显示连接模式选择对话框
                string selectedMode = await ShowConnectionModeDialog();
                if (string.IsNullOrEmpty(selectedMode)) return; // 用户取消

                vntConfig.IsConnecting = true;  // 进入连接中状态
                UpdateConnectButtonState(vntConfig);  // 更新按钮状态

                bool success = false;
                switch (selectedMode)
                {
                    case "Host":
                        success = await ConnectToServer(vntConfig);
                        if (success)
                        {
                            await ConfigHostFIKA(vntConfig);
                        }
                        break;
                    case "Client":
                        success = await ConnectToServer(vntConfig);
                        if (success)
                        {
                            await ConfigClient();
                        }
                        break;
                    case "MatchHost":
                        success = await ConnectToServer(vntConfig);
                        if (success)
                        {
                            await ConfigFIKA(vntConfig);
                        }
                        break;
                    case "NetworkOnly":
                        success = await ConnectToServer(vntConfig);
                        break;
                }

                vntConfig.IsConnected = success;
                vntConfig.IsConnecting = false;
                UpdateConnectButtonState(vntConfig);  // 根据最终状态更新按钮

                YamlConfigManager.SaveConfig(vntConfig, configPath);
            }
            else // 已连接则断开
            {
                await DisConnect(vntConfig);
                vntConfig.IsConnected = false;
                UpdateConnectButtonState(vntConfig);
                YamlConfigManager.SaveConfig(vntConfig, configPath);

                await ConfigRevHost(vntConfig);
            }
        }






        private async Task<bool> ConnectToServer(VntConfig vntConfig)
        {
            cancellationTokenSource = new CancellationTokenSource();
            string configFilePath = Path.Combine("Configs", $"EFT.yaml");

            if (!File.Exists(configFilePath) || !File.Exists(vntCliPath))
            {
                Debug.WriteLine("配置文件或 vnt-cli.exe 不存在");
                return false;
            }

            // 🔹 **连接前 UI 更新**
            vntConfig.IsConnecting = true;
            UpdateConnectButtonState(vntConfig); // ✅ 更新按钮状态

            var startInfo = new ProcessStartInfo
            {
                FileName = vntCliPath,
                Arguments = $"-c -f \"{configFilePath}\"",
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true,
            };

            try
            {
                vntProcess = Process.Start(startInfo);
                if (vntProcess != null)
                {
                    vntProcess.EnableRaisingEvents = true;
                    vntProcess.Exited += (sender, args) =>
                    {
                        _dispatcherQueue?.TryEnqueue(() =>
                        {
                            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "configs", "EFT.yaml");
                            vntConfig.IsConnected = false;
                            YamlConfigManager.SaveConfig(vntConfig, configPath); // ✅ 进程退出时也保存状态
                            UpdateConnectButtonState(vntConfig); // ✅ 进程退出后更新按钮状态
                        });
                    };

                    await Task.Delay(3000);
                    string status = await GetDeviceList();
                    bool connected = !status.Contains("连接尚未就绪");
                    var configPath = Path.Combine(Directory.GetCurrentDirectory(), "configs", "EFT.yaml");
                    vntConfig.IsConnected = connected;
                    vntConfig.IsConnecting = false; // ✅ 解除按钮禁用

                    YamlConfigManager.SaveConfig(vntConfig, configPath); // ✅ 连接成功后保存


                    UpdateConnectButtonState(vntConfig); // ✅ 连接完成后更新按钮状态

                    if (connected)
                    {
                        StartPeriodicStatusUpdate(vntConfig);
                    }

                    return connected;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting vnt-cli.exe: {ex.Message}");
            }

            // 🔹 **连接失败时 UI 更新**
            vntConfig.IsConnecting = false;
            vntConfig.IsConnected = false;
            UpdateConnectButtonState(vntConfig); // ✅ 连接失败后更新按钮状态

            return false;
        }



        private async Task DisConnect(VntConfig vntConfig)
        {
            try
            {
                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = new CancellationTokenSource();
                }

                if (!File.Exists(vntCliPath))
                {
                    Debug.WriteLine($"vnt-cli.exe not found at: {vntCliPath}");
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = vntCliPath,
                    Arguments = "--stop",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    Debug.WriteLine($"Output: {output}");
                    if (!string.IsNullOrEmpty(error)) Debug.WriteLine($"Error: {error}");
                }

                // 断开后更新连接状态
                vntConfig.IsConnected = false;
                vntConfig.ConnectionStatus = "已断开";

                // 🔹 **保存 YAML 配置**
                var configPath = Path.Combine(Directory.GetCurrentDirectory(), "configs", "EFT.yaml");
                YamlConfigManager.SaveConfig(vntConfig, configPath);

                // 🔹 **更新 UI 按钮状态**
                await DispatcherQueue.GetForCurrentThread().EnqueueAsync(() =>
                {
                    UpdateConnectButtonState(vntConfig);
                });
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("🔴 任务已取消，安全退出");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error stopping vnt-cli.exe: {ex.Message}");
            }
        }



        private async Task<string> GetDeviceList()
        {
            if (!File.Exists(vntCliPath)) return "vnt-cli.exe不存在";

            var startInfo = new ProcessStartInfo
            {
                FileName = vntCliPath,
                Arguments = "--list",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            try
            {
                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(error)) Debug.WriteLine($"Error: {error}");
                    if (output.Contains("ConnectionReset")) return "连接尚未就绪";
                    return output;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting vnt-cli.exe: {ex.Message}");
            }
            return "获取设备列表失败";
        }



        private async void DeviceList_Click(object sender, RoutedEventArgs e)
        {

            await GetDeviceListShow();


        }

        private async Task GetDeviceListShow()
        {
            // 获取 vnt-cli.exe 的路径
            string vntCliPath = Path.Combine("Assets", "vnt", "vnt-cli.exe");

            if (File.Exists(vntCliPath))
            {
                // 启动 vnt-cli.exe 进程来获取设备列表
                var startInfo = new ProcessStartInfo
                {
                    FileName = vntCliPath,                // 使用 vnt-cli.exe 的正确路径
                    Arguments = "--list",                  // 获取设备列表
                    UseShellExecute = false,              // 不使用外壳执行
                    RedirectStandardOutput = true,        // 重定向标准输出
                    RedirectStandardError = true,         // 重定向标准错误
                    CreateNoWindow = true,                // 不显示命令行窗口
                    StandardOutputEncoding = Encoding.UTF8,  // 强制使用 UTF-8 编码
                    StandardErrorEncoding = Encoding.UTF8   // 强制使用 UTF-8 编码
                };

                try
                {
                    using (var process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            // 获取命令的输出并处理设备列表
                            string output = await process.StandardOutput.ReadToEndAsync();
                            string error = await process.StandardError.ReadToEndAsync();

                            if (!string.IsNullOrEmpty(error))
                            {
                                Debug.WriteLine($"Error: {error}");
                            }
                            else
                            {
                                // 如果输出包含 ConnectionReset 错误，替换为“连接尚未就绪”
                                if (output.Contains("ConnectionReset"))
                                {
                                    output = "连接尚未就绪";
                                }

                                Debug.WriteLine($"Device List: {output}");

                                // 显示设备列表在弹窗中
                                await ShowDeviceListDialog(output);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error starting vnt-cli.exe: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine($"vnt-cli.exe not found at: {vntCliPath}");
            }
        }

        private async Task ShowDeviceListDialog(string rawOutput)
        {
            var devices = ParseDeviceList(rawOutput);

            var dialog = new DeviceListDialog(devices)
            {
                XamlRoot = this.XamlRoot  // ✅ 确保 XamlRoot 设置正确
            };

            await dialog.ShowAsync();
        }

        private List<DeviceInfo> ParseDeviceList(string rawOutput)
        {
            var devices = new List<DeviceInfo>();
            var lines = rawOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines.Skip(1))  // 跳过第一行表头
            {
                var columns = Regex.Split(line.Trim(), @"\s{2,}");  // 以两个及以上空格分割

                if (columns.Length >= 2)
                {
                    devices.Add(new DeviceInfo
                    {
                        Name = columns[0],
                        VirtualIp = columns.Length > 1 ? columns[1] : "N/A",
                        Status = columns.Length > 2 ? columns[2] : "Unknown",
                        ConnectionType = columns.Length > 3 ? columns[3] : "N/A",
                        RTT = columns.Length > 4 ? columns[4] : "N/A"
                    });
                }
            }
            return devices;
        }

        private async

        Task
    ShowMessage(string message)
        {
            // 使用 ContentDialog 显示简单消息
            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "提示",
                Content = message,
                CloseButtonText = "确定"
            };

            await dialog.ShowAsync();
        }



        private async void StartPeriodicStatusUpdate(VntConfig vntConfig)
        {
            try
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    string deviceList = await GetDeviceList();

                    await DispatcherQueue.GetForCurrentThread().EnqueueAsync(() =>
                    {
                        bool wasConnected = vntConfig.IsConnected;
                        vntConfig.IsConnected = !deviceList.Contains("连接尚未就绪");
                        var status = vntConfig.IsConnected ? "已连接" : "未连接";
                        YamlConfigManager.SaveConfig(vntConfig, Path.Combine(Directory.GetCurrentDirectory(), "configs", "EFT.yaml"));
                        if (wasConnected != vntConfig.IsConnected)
                        {
                            Debug.WriteLine($"连接状态变化: {vntConfig.IsConnected}");
                        }
                    });

                    // **等待时，检查取消标志**
                    await Task.Delay(10000, cancellationTokenSource.Token);
                }
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("🔴 定时任务被取消，停止状态更新！");
            }
        }

        private async Task<string> ShowConnectionModeDialog()
{
    var dialog = new ContentDialog
    {
        Title = "请选择联机模式",
        XamlRoot = this.XamlRoot,
        CloseButtonText = "取消", // 仅保留取消按钮
        DefaultButton = ContentDialogButton.Close // 默认聚焦取消按钮
    };

    // 定义选项和描述
    var options = new List<(string Mode, string Title, string Description)>
    {
        ("Host", "全托管主机", "作为完整主机运行，管理所有游戏逻辑和网络连接。"),
        ("Client", "仅客户端", "仅作为客户端连接到服务器，不承担主机职责。"),
        ("MatchHost", "仅战局主机", "仅管理战局逻辑，网络连接由其他服务器处理。"),
        ("NetworkOnly", "仅网络主机", "仅处理网络连接，不参与游戏逻辑管理。")
    };

    // 布局容器
    StackPanel panel = new StackPanel
    {
        Orientation = Orientation.Vertical,
        HorizontalAlignment = HorizontalAlignment.Center,
        Spacing = 10 // 选项之间的间距
    };

    // 动态生成选项按钮和描述
    foreach (var option in options)
    {
        // 按钮
        var button = new Button
        {
            Content = option.Title,
            Width = 300,
            Margin = new Thickness(5),
            HorizontalAlignment = HorizontalAlignment.Center,
            Tag = option.Mode // 绑定模式值
        };

        // 描述文本
        var description = new TextBlock
        {
            Text = option.Description,
            TextWrapping = TextWrapping.Wrap,
            Width = 280,
            Margin = new Thickness(5, 0, 5, 10),
            Foreground = new SolidColorBrush(Colors.Gray),
            FontSize = 12
        };

        // 绑定点击事件
        button.Click += (s, e) =>
        {
            dialog.Tag = (s as Button)?.Tag?.ToString(); // 设置选择结果
            dialog.Hide();
        };

        // 添加到布局
        panel.Children.Add(button);
        panel.Children.Add(description);
    }

    dialog.Content = panel;

    // 显示对话框
    var result = await dialog.ShowAsync();

    return dialog.Tag?.ToString(); // 返回用户选择的值，取消返回 null
}

        private async void ShowMessageFika(string message)
        {
            // 使用 ContentDialog 显示简单消息
            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "提示",
                Content = message,
                CloseButtonText = "确定"
            };

            await dialog.ShowAsync();
        }
        private async Task ConfigFIKA(VntConfig config)
        {
            if (config == null) return;

            // 获取 ManagerConfig 实例
            var managerConfig = App.ManagerConfig; // 假设你通过资源字典引用了该实例
            if (managerConfig == null)
            {
                ShowMessageFika("无法找到 ManagerConfig 实例！");
                return;
            }

            string installPath = managerConfig.InstallPath; // 获取安装路径
            Debug.WriteLine($"InstallPath: {installPath}");

            if (string.IsNullOrEmpty(installPath))
            {
                ShowMessageFika("安装路径无效！");
                return;
            }

            // 拼接 FIKA 配置文件路径
            string configFilePath = Path.Combine(installPath, "BepInEx", "config", "com.fika.core.cfg");

            // 检查路径是否有效
            char[] invalidChars = Path.GetInvalidPathChars();
            if (installPath.Any(c => invalidChars.Contains(c)))
            {
                ShowMessageFika("安装路径包含无效字符！");
                return;
            }

            if (!File.Exists(configFilePath))
            {
                ShowMessageFika("FIKA 配置文件未找到！");
                return;
            }

            try
            {
                // 读取配置文件
                string fileContent = await File.ReadAllTextAsync(configFilePath);
                Debug.WriteLine($"原始文件内容:\n{fileContent}");

                // 修改 "Force IP" 选项
                string newForceIP = $"Force IP = {config.ip}";
                int forceIPIndex = fileContent.IndexOf("Force IP =");

                if (forceIPIndex != -1)
                {
                    // 替换 "Force IP" 这一行
                    int startOfLine = fileContent.LastIndexOf("\n", forceIPIndex) + 1;
                    int endOfLine = fileContent.IndexOf("\n", forceIPIndex);

                    if (endOfLine == -1)
                    {
                        fileContent = fileContent.Substring(0, startOfLine) + newForceIP;
                    }
                    else
                    {
                        fileContent = fileContent.Substring(0, startOfLine) + newForceIP + fileContent.Substring(endOfLine);
                    }
                }
                else
                {
                    // 如果文件中没有 "Force IP"，则添加
                    fileContent += $"\n{newForceIP}";
                }

                // 修改 "Force Bind IP"
                string oldForceBindIP = "Force Bind IP = 0.0.0.0";
                string newForceBindIP = "Force Bind IP = Disabled";

                if (fileContent.Contains(oldForceBindIP))
                {
                    fileContent = fileContent.Replace(oldForceBindIP, newForceBindIP);
                }

                // 保存修改后的文件
                await File.WriteAllTextAsync(configFilePath, fileContent);

                // 显示成功消息
                ShowMessage($"Fika 配置文件已更新：\nForce IP = {config.ip}\nForce Bind IP = Disabled\n现在你可以通过联机模块进行游戏");
            }
            catch (Exception ex)
            {
                ShowMessage($"发生错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    ShowMessage($"内部异常: {ex.InnerException.Message}\n堆栈跟踪: {ex.InnerException.StackTrace}");
                }
            }
        }

        private async Task ConfigClient()
        {
            // 获取 ManagerConfig 实例
            var managerConfig = App.ManagerConfig; // 假设你通过资源字典引用了该实例
            if (managerConfig == null)
            {
                ShowMessageFika("无法找到 ManagerConfig 实例！");
                return;
            }

            string installPath = managerConfig.InstallPath; // 获取安装路径
            Debug.WriteLine($"InstallPath: {installPath}");

            if (string.IsNullOrEmpty(installPath))
            {
                ShowMessageFika("安装路径无效！");
                return;
            }

            // 拼接 FIKA 配置文件路径
            string configFilePath = Path.Combine(installPath, "BepInEx", "config", "com.fika.core.cfg");

            // 检查路径是否有效
            char[] invalidChars = Path.GetInvalidPathChars();
            if (installPath.Any(c => invalidChars.Contains(c)))
            {
                ShowMessageFika("安装路径包含无效字符！");
                return;
            }

            if (!File.Exists(configFilePath))
            {
                ShowMessageFika("FIKA 配置文件未找到！");
                return;
            }

            try
            {
                // 读取配置文件
                string fileContent = await File.ReadAllTextAsync(configFilePath);
                Debug.WriteLine($"原始文件内容:\n{fileContent}");

                // 修改 "Force IP" 选项，设为空
                string newForceIP = "Force IP = \"\"";
                int forceIPIndex = fileContent.IndexOf("Force IP =");

                if (forceIPIndex != -1)
                {
                    // 替换 "Force IP" 这一行
                    int startOfLine = fileContent.LastIndexOf("\n", forceIPIndex) + 1;
                    int endOfLine = fileContent.IndexOf("\n", forceIPIndex);

                    if (endOfLine == -1)
                    {
                        fileContent = fileContent.Substring(0, startOfLine) + newForceIP;
                    }
                    else
                    {
                        fileContent = fileContent.Substring(0, startOfLine) + newForceIP + fileContent.Substring(endOfLine);
                    }
                }
                else
                {
                    // 如果文件中没有 "Force IP"，则添加
                    fileContent += $"\n{newForceIP}";
                }

                // 修改 "Force Bind IP"，确保它始终为 Disabled
                string oldForceBindIP = "Force Bind IP = 0.0.0.0";
                string newForceBindIP = "Force Bind IP = Disabled";

                if (fileContent.Contains(oldForceBindIP))
                {
                    fileContent = fileContent.Replace(oldForceBindIP, newForceBindIP);
                }
                else if (!fileContent.Contains("Force Bind IP = Disabled"))
                {
                    // 如果 "Force Bind IP" 这一项根本不存在，则添加
                    fileContent += $"\n{newForceBindIP}";
                }

                // 保存修改后的文件
                await File.WriteAllTextAsync(configFilePath, fileContent);

                // 显示成功消息
                ShowMessage($"Fika 配置文件已更新：\nForce IP = \"\"\nForce Bind IP = Disabled\n现在你可以进行客户端连接");
            }
            catch (Exception ex)
            {
                ShowMessage($"发生错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    ShowMessage($"内部异常: {ex.InnerException.Message}\n堆栈跟踪: {ex.InnerException.StackTrace}");
                }
            }
        }


        private async Task ConfigHostFIKA(VntConfig config)
        {
            if (config == null) return;

            var managerConfig = App.ManagerConfig; // 获取 ManagerConfig 实例
            if (managerConfig == null)
            {
                await ShowMessage("❌ 无法找到 ManagerConfig 实例！");
                return;
            }

            string installPath = managerConfig.InstallPath; // 获取安装路径
            Debug.WriteLine($"InstallPath: {installPath}");

            if (string.IsNullOrEmpty(installPath))
            {
                await ShowMessage("❌ 安装路径无效！");
                return;
            }

            // 🔹 记录所有日志信息，最后一次性显示
            StringBuilder logBuilder = new StringBuilder();

            // =======================
            // 1️⃣ 配置 FIKA
            // =======================
            string fikaConfigPath = Path.Combine(installPath, "BepInEx", "config", "com.fika.core.cfg");

            if (!File.Exists(fikaConfigPath))
            {
                logBuilder.AppendLine("⚠️ FIKA 配置文件未找到！");
            }
            else
            {
                try
                {
                    string fikaConfig = await File.ReadAllTextAsync(fikaConfigPath);
                    Debug.WriteLine($"原始 FIKA 配置:\n{fikaConfig}");

                    // 修改 "Force IP"
                    string newForceIP = $"Force IP = {config.ip}";
                    int forceIPIndex = fikaConfig.IndexOf("Force IP =");

                    if (forceIPIndex != -1)
                    {
                        // 替换 "Force IP" 这一行
                        int startOfLine = fikaConfig.LastIndexOf("\n", forceIPIndex) + 1;
                        int endOfLine = fikaConfig.IndexOf("\n", forceIPIndex);

                        fikaConfig = (endOfLine == -1)
                            ? fikaConfig.Substring(0, startOfLine) + newForceIP
                            : fikaConfig.Substring(0, startOfLine) + newForceIP + fikaConfig.Substring(endOfLine);
                    }
                    else
                    {
                        // 添加新行
                        fikaConfig += $"\n{newForceIP}";
                    }

                    // 修改 "Force Bind IP"
                    string oldForceBindIP = "Force Bind IP = 0.0.0.0";
                    string newForceBindIP = "Force Bind IP = Disabled";

                    if (fikaConfig.Contains(oldForceBindIP))
                    {
                        fikaConfig = fikaConfig.Replace(oldForceBindIP, newForceBindIP);
                    }

                    await File.WriteAllTextAsync(fikaConfigPath, fikaConfig);
                    logBuilder.AppendLine($"✅ FIKA 配置更新成功！Force IP = {config.ip}, Force Bind IP = Disabled");
                }
                catch (Exception ex)
                {
                    logBuilder.AppendLine($"❌ 配置 FIKA 失败: {ex.Message}");
                }
            }


            // =======================
            // 2️⃣ 配置 Host (http.json & server.json)
            // =======================
            string sptPath = Path.Combine(installPath, "SPT_Data", "Server", "configs", "http.json");
            string spt4Path = Path.Combine(installPath, "SPT_Data", "Server", "database", "server.json");
            string akiPath = Path.Combine(installPath, "aki_Data", "Server", "configs", "http.json");
            string fikaPath = Path.Combine(installPath, "user", "mods", "fika-server", "assets", "configs", "fika.jsonc");

            string hostConfigPath = File.Exists(sptPath) ? sptPath : akiPath;
            string serverJsonPath = spt4Path;

            if (!File.Exists(hostConfigPath))
            {
                logBuilder.AppendLine("⚠️ 服务器配置文件 http.json 未找到！");
            }
            else
            {
                try
                {
                    string hostConfig = await File.ReadAllTextAsync(hostConfigPath);
                    var configDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(hostConfig);

                    if (configDict != null)
                    {
                        configDict["ip"] = "0.0.0.0";
                        configDict["backendIp"] = config.ip;

                        string updatedJson = System.Text.Json.JsonSerializer.Serialize(configDict, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(hostConfigPath, updatedJson);

                        logBuilder.AppendLine($"✅ 服务器 http.json 配置更新成功！backendIp = {config.ip}");
                    }
                    else
                    {
                        logBuilder.AppendLine("❌ 服务器配置文件解析失败！");
                    }
                }
                catch (Exception ex)
                {
                    logBuilder.AppendLine($"❌ 配置服务器失败: {ex.Message}");
                }
            }

            // =======================
            // 3️⃣ 配置 Server (server.json)
            // =======================
            if (!File.Exists(serverJsonPath))
            {
                logBuilder.AppendLine("⚠️ 服务器配置文件 server.json 未找到！");
            }
            else
            {
                try
                {
                    string serverJson = await File.ReadAllTextAsync(serverJsonPath);
                    var serverDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(serverJson);

                    if (serverDict != null)
                    {
                        serverDict["ip"] = config.ip; // 修改 IP

                        string updatedServerJson = System.Text.Json.JsonSerializer.Serialize(serverDict, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(serverJsonPath, updatedServerJson);

                        logBuilder.AppendLine("✅ 服务器 server.json 配置更新成功！");
                    }
                    else
                    {
                        logBuilder.AppendLine("❌ 服务器 server.json 解析失败！");
                    }
                }
                catch (Exception ex)
                {
                    logBuilder.AppendLine($"❌ 配置 server.json 失败: {ex.Message}");
                }
            }

            // =======================
            // 4️⃣ 配置 Fika (fika.jsonc)
            // =======================
            if (!File.Exists(fikaPath))
            {
                logBuilder.AppendLine("⚠️ fika 配置文件 fika.jsonc 未找到！");
            }
            else
            {
                try
                {
                    string fikaConfig = await File.ReadAllTextAsync(fikaPath);
                    var fikaDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(fikaConfig);

                    if (fikaDict != null && fikaDict.ContainsKey("server"))
                    {
                        var serverNode = (JsonElement)fikaDict["server"];

                        if (serverNode.TryGetProperty("SPT", out JsonElement sptNode) &&
                            sptNode.TryGetProperty("http", out JsonElement httpNode))
                        {
                            // 修改 http 配置
                            var httpConfig = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(httpNode.GetRawText());
                            if (httpConfig != null)
                            {
                                httpConfig["ip"] = "0.0.0.0";
                                httpConfig["backendIp"] = config.ip;

                                // 更新 SPT 配置
                                var sptConfig = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(sptNode.GetRawText());
                                sptConfig["http"] = httpConfig;

                                // 更新 Server 配置
                                var serverConfig = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(serverNode.GetRawText());
                                serverConfig["SPT"] = sptConfig;

                                fikaDict["server"] = serverConfig;

                                // 写回修改后的 JSON
                                string updatedFikaJson = System.Text.Json.JsonSerializer.Serialize(fikaDict, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                                await File.WriteAllTextAsync(fikaPath, updatedFikaJson);

                                logBuilder.AppendLine("✅ fika.jsonc 配置更新成功！");
                            }
                        }
                    }
                    else
                    {
                        logBuilder.AppendLine("❌ fika 配置文件格式错误！");
                    }
                }
                catch (Exception ex)
                {
                    logBuilder.AppendLine($"❌ 配置 fika.jsonc 失败: {ex.Message}");
                }
            }



            // =======================
            // 3️⃣ 显示最终结果
            // =======================
            // 获取端口和 backendIp
            string backendIp = config.ip;
            int port = 6969; // 默认端口

            try
            {
                // 从 http.json 中读取端口
                
                if (File.Exists(hostConfigPath))
                {
                    string hostConfig = await File.ReadAllTextAsync(hostConfigPath);
                    var configDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(hostConfig);
                    if (configDict != null && configDict.ContainsKey("port"))
                    {
                        port = int.Parse(configDict["port"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                logBuilder.AppendLine($"⚠️ 读取端口失败: {ex.Message}");
            }

            // 构建完整的服务器登录地址
            string serverAddress = $"http://{backendIp}:{port}";
            logBuilder.AppendLine($"\n🌐 服务器登录地址: {serverAddress}");

            // 显示最终结果
            await ShowMessage(logBuilder.ToString());

            // 显示成功提示
            ShowHostSuccess(serverAddress);

            // 🔹 创建对话框
            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "配置完成",
                Content = $"服务器与 FIKA 配置已完成。\n服务器登录地址: {serverAddress}\n是否打开服务端管理器？",
                PrimaryButtonText = "确定",
                SecondaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            // 🔹 添加复制按钮
            var copyButton = new Button
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
        {
            new SymbolIcon(Symbol.Copy),
            new TextBlock { Text = "复制登录地址", Margin = new Thickness(5, 0, 0, 0) }
        }
                },
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            copyButton.Click += (s, e) =>
            {
                var package = new DataPackage();
                package.SetText(serverAddress);
                Clipboard.SetContent(package);
                copyButton.Content = "已复制！";
                copyButton.IsEnabled = false; // 禁用按钮，避免重复点击
            };

            // 将复制按钮添加到对话框内容
            var stackPanel = new StackPanel();
            stackPanel.Children.Add(new TextBlock { Text = dialog.Content.ToString() });
            stackPanel.Children.Add(copyButton);
            dialog.Content = stackPanel;

            // 显示对话框
            var result = await dialog.ShowAsync();

            var manager = GetNetworkManager();
            string fullLoginUrl = serverAddress;
            var updateResult = await manager.UpdateLoginAddressAsync(config.token, fullLoginUrl, CancellationToken.None);
            if (updateResult.Success)
            {
                Debug.WriteLine("房主完整登录地址更新成功！");
            }
            else
            {
                Debug.WriteLine($"更新失败：{"Error"}");
            }
            if (result == ContentDialogResult.Primary)
            {
                OpeSPTServerManagerWindow(); // 打开服务端管理器
            }
        }

        // 加载配置并调用 ConfigHostFIKA
        private async Task LoadAndConfigureFIKAAsync()
        {
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "configs", "EFT.yaml");
            try
            {
                VntConfig config = YamlConfigManager.LoadConfig(configPath);
                Console.WriteLine($"成功加载配置: {configPath}");
                Console.WriteLine($"Server Address: {config.server_address}");
                Console.WriteLine($"Token: {config.token}");
                Console.WriteLine($"Device ID: {config.device_id}");
                Console.WriteLine($"IP: {config.ip}");

                // 传入的 config 对象中，config.ip 值将用于写入 FIKA 配置
                await ConfigFIKA(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 加载 YAML 配置失败: {ex.Message}");
            }
        }


        // 加载配置并调用 ConfigHostFIKA
        private async Task LoadAndConfigureFIKAHostAsync()
        {
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "configs", "EFT.yaml");
            try
            {
                VntConfig config = YamlConfigManager.LoadConfig(configPath);
                Console.WriteLine($"成功加载配置: {configPath}");
                Console.WriteLine($"Server Address: {config.server_address}");
                Console.WriteLine($"Token: {config.token}");
                Console.WriteLine($"Device ID: {config.device_id}");
                Console.WriteLine($"IP: {config.ip}");

                // 传入的 config 对象中，config.ip 值将用于写入 FIKA 配置
                await ConfigHostFIKA(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 加载 YAML 配置失败: {ex.Message}");
            }
        }


        private async Task ConfigHost(VntConfig config)
        {
            if (config == null) return;

            var managerConfig = App.ManagerConfig; // 假设已初始化
            if (managerConfig == null)
            {
                ShowMessage("无法找到ManagerConfig实例！");
                return;
            }

            string installPath = managerConfig.InstallPath;
            if (string.IsNullOrEmpty(installPath))
            {
                ShowMessage("安装路径无效！");
                return;
            }

            string sptPath = Path.Combine(installPath, "SPT_Data", "Server", "configs", "http.json");
            string akiPath = Path.Combine(installPath, "aki_Data", "Server", "configs", "http.json");
            string configFilePath = File.Exists(sptPath) ? sptPath : akiPath;

            if (!File.Exists(configFilePath))
            {
                ShowMessage("配置文件未找到！");
                return;
            }

            try
            {
                // 读取 JSON
                string fileContent = await File.ReadAllTextAsync(configFilePath);
                var configDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(fileContent);
                if (configDict == null)
                {
                    ShowMessage("配置文件解析失败！");
                    return;
                }

                // 修改配置项
                configDict["ip"] = "0.0.0.0";
                configDict["backendIp"] = config.ip;

                string updatedJson = System.Text.Json.JsonSerializer.Serialize(configDict, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                // 写回文件
                await File.WriteAllTextAsync(configFilePath, updatedJson);

                // 弹窗提示
                var dialog = new ContentDialog
                {
                    XamlRoot = this.XamlRoot,
                    Title = "配置已更新",
                    Content = $"http.json 配置已更新:\nbackendIp 修改为: {config.ip}\n点击确定打开服务端管理器，取消返回。",
                    PrimaryButtonText = "确定",
                    SecondaryButtonText = "取消",
                    DefaultButton = ContentDialogButton.Primary
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    OpeSPTServerManagerWindow(); // 打开服务端管理器
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"发生错误: {ex.Message}\n堆栈跟踪: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    ShowMessage($"内部异常: {ex.InnerException.Message}\n堆栈跟踪: {ex.InnerException.StackTrace}");
                }
            }
        }


        private async void OpeSPTServerManagerWindow()
        {
            SPTServerManager SPTServerManager = new SPTServerManager();
            await Task.Delay(TimeSpan.FromSeconds(0.02));
            SPTServerManager.Activate();
        }

        


        private Process vntProcess = null;
        private readonly string vntCliPath = Path.Combine("Assets", "vnt", "vnt-cli.exe");
        private CancellationTokenSource cancellationTokenSource = new();

        

       
    }
}



public class VntConfig : INotifyPropertyChanged
{


    private bool _isConnected;
    private bool _isConnecting;

    private string mode; // ✅ 新增 Mode 属性

    public string Mode // ✅ 连接模式（Host/Client/NetworkOnly）
    {
        get => mode;
        set
        {
            if (mode != value)
            {
                mode = value;
                OnPropertyChanged(nameof(Mode));
            }
        }
    }
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (_isConnected != value)
            {
                _isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
            }
        }
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        set
        {
            if (_isConnecting != value)
            {
                _isConnecting = value;
                OnPropertyChanged(nameof(IsConnecting));
            }
        }
    }

    public string GetDeviceId()
    {
        string deviceId = string.Empty;
        try
        {
            // 获取物理磁盘的序列号作为设备 ID
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia");
            foreach (ManagementObject disk in searcher.Get())
            {
                deviceId = disk["SerialNumber"].ToString();
                if (!string.IsNullOrEmpty(deviceId))
                    break; // 获取到序列号后可以退出
            }
        }
        catch (Exception ex)
        {
            // 处理异常
            deviceId = Guid.NewGuid().ToString(); // 若出错使用 GUID
        }

        return deviceId;
    }
    // VntName 默认为空字符串
    private string _vntName = string.Empty;
    public string VntName
    {
        get => _vntName;
        set
        {
            if (_vntName != value)
            {
                _vntName = value;
                OnPropertyChanged(nameof(VntName));
            }
        }
    }

    // ConnectionStatus 默认为空字符串
    private string _connectionStatus = string.Empty;
    public string ConnectionStatus
    {
        get => _connectionStatus;
        set
        {
            if (_connectionStatus != value)
            {
                _connectionStatus = value;
                OnPropertyChanged(nameof(ConnectionStatus));
            }
        }
    }

    // tap 默认为 false
    private bool _tap = false;
    public bool tap
    {
        get => _tap;
        set
        {
            if (_tap != value)
            {
                _tap = value;
                OnPropertyChanged(nameof(tap));
            }
        }
    }

    // token 默认为空字符串
    private string _token = string.Empty;
    public string token
    {
        get => _token;
        set
        {
            if (_token != value)
            {
                _token = value;
                OnPropertyChanged(nameof(token));
            }
        }
    }

    // device_id 默认为空字符串
    private string _deviceId = string.Empty;

    public string device_id
    {
        get
        {
            // 自动获取设备 ID
            if (string.IsNullOrEmpty(_deviceId))
            {
                _deviceId = GetDeviceId();
            }
            return _deviceId;
        }
        set
        {
            if (_deviceId != value)
            {
                _deviceId = value;
                OnPropertyChanged(nameof(device_id));
            }
        }
    }

    // name 默认为空字符串
    private string _deviceName = string.Empty;

    public string name
    {
        get
        {
            // 自动获取设备名称
            if (string.IsNullOrEmpty(_deviceName))
            {
                _deviceName = GetDeviceName();
            }
            return _deviceName;
        }
        set
        {
            if (_deviceName != value)
            {
                _deviceName = value;
                OnPropertyChanged(nameof(name));
            }
        }
    }

    // 获取设备名称
    private string GetDeviceName()
    {
        // 使用 Environment.MachineName 获取本机的设备名称
        return Environment.MachineName;
    }

    // server_address 默认为空字符串
    private string _serverAddress = string.Empty;
    public string server_address
    {
        get => _serverAddress;
        set
        {
            if (_serverAddress != value)
            {
                _serverAddress = value;
                OnPropertyChanged(nameof(server_address));
            }
        }
    }

    // stun_server 默认为空列表
    private List<string> _stunServer = new List<string>
    {
        "stun1.l.google.com:19302",
        "stun2.l.google.com:19302",
        "stun.miwifi.com",
        "stun.chat.bilibili.com",
        "stun.hitv.com",
        "stun.cdnbye.com"
    };
    public List<string> stun_server
    {
        get => _stunServer;
        set
        {
            if (_stunServer != value)
            {
                _stunServer = value;
                OnPropertyChanged(nameof(stun_server));
            }
        }
    }

    // mtu 默认为 1420
    private int _mtu = 1420;
    public int mtu
    {
        get => _mtu;
        set
        {
            if (_mtu != value)
            {
                _mtu = value;
                OnPropertyChanged(nameof(mtu));
            }
        }
    }

    // tcp 默认为 false
    private bool _tcp = false;
    public bool tcp
    {
        get => _tcp;
        set
        {
            if (_tcp != value)
            {
                _tcp = value;
                OnPropertyChanged(nameof(tcp));
            }
        }
    }

    // ip 默认为空字符串
    private string _ip = string.Empty;
    public string ip
    {
        get => _ip;
        set
        {
            if (_ip != value)
            {
                _ip = value;
                OnPropertyChanged(nameof(ip));
            }
        }
    }

    // server_encrypt 默认为 false
    private bool _serverEncrypt = false;
    public bool server_encrypt
    {
        get => _serverEncrypt;
        set
        {
            if (_serverEncrypt != value)
            {
                _serverEncrypt = value;
                OnPropertyChanged(nameof(server_encrypt));
            }
        }
    }

    // cipher_model 默认为空字符串
    //private string _cipherModel = string.Empty;
    //public string cipher_model
    //{
    //    get => _cipherModel;
    //    set
    //    {
    //        if (_cipherModel != value)
    //        {
    //            _cipherModel = value;
    //            OnPropertyChanged(nameof(cipher_model));
    //        }
    //    }
    //}

    // punch_model 默认为 "all"
    private string _punchModel = "all";
    public string punch_model
    {
        get => _punchModel;
        set
        {
            if (_punchModel != value)
            {
                _punchModel = value;
                OnPropertyChanged(nameof(punch_model));
            }
        }
    }

    // use_channel 默认为 "all"
    private string _useChannel = "all";
    public string use_channel
    {
        get => _useChannel;
        set
        {
            if (_useChannel != value)
            {
                _useChannel = value;
                OnPropertyChanged(nameof(use_channel));
            }
        }
    }

    // cmd 默认为 false
    private bool _cmd = false;
    public bool cmd
    {
        get => _cmd;
        set
        {
            if (_cmd != value)
            {
                _cmd = value;
                OnPropertyChanged(nameof(cmd));
            }
        }
    }

    // 实现 INotifyPropertyChanged 接口
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class YamlConfigManager
{
    private static readonly IDeserializer deserializer = new DeserializerBuilder()
    .Build();  // ❌ 去掉 WithNamingConvention

    private static readonly YamlDotNet.Serialization.ISerializer serializer =
        new YamlDotNet.Serialization.SerializerBuilder()
        .Build();  // ❌ 去掉 WithNamingConvention



    // 读取 YAML 配置文件
    public static VntConfig LoadConfig(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("YAML 配置文件不存在", filePath);
        }

        string yamlContent = File.ReadAllText(filePath);
        return deserializer.Deserialize<VntConfig>(yamlContent);
    }

    // 保存 YAML 配置文件
    public static void SaveConfig(VntConfig config, string filePath)
    {
        string yamlContent = serializer.Serialize(config);
        File.WriteAllText(filePath, yamlContent);
    }

    // 修改 server_address 并保存
    public static void UpdateServerAddress(string filePath, string newServerAddress)
    {
        var config = LoadConfig(filePath);
        config.server_address = newServerAddress;
        SaveConfig(config, filePath);
    }

    // 修改 token 并保存
    public static void UpdateToken(string filePath, string newToken)
    {
        var config = LoadConfig(filePath);
        config.token = newToken;
        SaveConfig(config, filePath);
    }

    public static void UpdateName(string filePath, string newName)
    {
        var config = LoadConfig(filePath);
        config.name = newName;
        SaveConfig(config, filePath);
    }

    // 修改 device_id 并保存
    public static void UpdateDeviceId(string filePath, string newDeviceId)
    {
        var config = LoadConfig(filePath);
        config.device_id = newDeviceId;
        SaveConfig(config, filePath);
    }

    // 修改 ip 并保存
    public static void UpdateIp(string filePath, string newIp)
    {
        var config = LoadConfig(filePath);
        config.ip = newIp;
        SaveConfig(config, filePath);
    }
}
