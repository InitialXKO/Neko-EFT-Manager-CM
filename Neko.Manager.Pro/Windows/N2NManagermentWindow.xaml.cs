using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UnityEngine.Networking;

namespace Neko.EFT.Manager.X.Windows
{
    public sealed partial class N2NManagermentWindow : Window
    {
        private N2NManager _n2nManager = new N2NManager();
        public ObservableCollection<string> Members { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> Messages { get; set; } = new ObservableCollection<string>();
        private NetworkManager _networkManager;
        private string _currentRoom;

        public N2NManagermentWindow()
        {
            this.InitializeComponent();
            MemberList.ItemsSource = Members;
            MessageList.ItemsSource = Messages;
        }

        private void CheckSupernodeStatus()
        {
            try
            {
                using (var client = new System.Net.Sockets.UdpClient())
                {
                    // 使用 UDP 协议连接 Supernode
                    client.Connect("103.40.13.88", 28663); // 替换为实际的 IP 和端口

                    // 发送一个空的 UDP 数据包来测试连接
                    byte[] sendBuffer = new byte[0];
                    client.Send(sendBuffer, sendBuffer.Length);

                    // 尝试接收响应
                    var serverEndpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
                    var receiveTask = client.ReceiveAsync();

                    // 设置接收超时时间
                    if (receiveTask.Wait(5000))
                    {
                        byte[] response = receiveTask.Result.Buffer;
                        ShowErrorDialog("完成", "Supernode 连接成功。");
                    }
                    else
                    {
                        ShowErrorDialog("超时", "连接 Supernode 超时。");
                    }
                }
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                string errorMessage = $"无法连接到 Supernode。Socket 错误: {ex.Message} (错误代码: {ex.SocketErrorCode})";
                ShowErrorDialog("网络错误", errorMessage);
            }
            catch (System.TimeoutException ex)
            {
                string errorMessage = $"连接 Supernode 超时。错误: {ex.Message}";
                ShowErrorDialog("超时错误", errorMessage);
            }
            catch (System.Exception ex)
            {
                string errorMessage = $"发生了未知错误: {ex.Message}";
                ShowErrorDialog("未知错误", errorMessage);
            }
        }




        private void CreateRoom_Click(object sender, RoutedEventArgs e)
        {
            CheckSupernodeStatus();  // 添加这行

            string roomName = RoomNameTextBox.Text;
            string memberName = MemberNameTextBox.Text;

            if (string.IsNullOrEmpty(roomName) || string.IsNullOrEmpty(memberName))
            {
                ShowErrorDialog("错误", "房间名称和成员名称不能为空");
                return;
            }

            _currentRoom = roomName;
            _n2nManager.StartEdgeNode(_currentRoom, "103.40.13.88", "172.10.1.1", "password");

            _networkManager = new NetworkManager("103.40.13.88", 7654);
            Members.Add(memberName);
            BroadcastMemberName(memberName);
        }


        private void JoinRoom_Click(object sender, RoutedEventArgs e)
        {
            string roomName = RoomNameTextBox.Text;
            string memberName = MemberNameTextBox.Text;

            if (string.IsNullOrEmpty(roomName) || string.IsNullOrEmpty(memberName))
            {
                ShowErrorDialog("错误", "房间名称和成员名称不能为空");
                return;
            }

            _currentRoom = roomName;
            _n2nManager.StartEdgeNode(_currentRoom, "127.0.0.1", "edge_ip", "password");

            _networkManager = new NetworkManager("127.0.0.1", 7654);
            Members.Add(memberName);
            BroadcastMemberName(memberName);

            StartListening();
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            string message = MessageTextBox.Text;
            if (!string.IsNullOrEmpty(message))
            {
                _networkManager.SendMessage($"MESSAGE:{message}");
                Messages.Add($"我: {message}");
                MessageTextBox.Text = string.Empty;
            }
        }

        private void BroadcastMemberName(string memberName)
        {
            string message = $"NEW_MEMBER:{memberName}";
            _networkManager.SendMessage(message);
        }

        private void StartListening()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    string message = _networkManager.ReceiveMessage();
                    if (message.StartsWith("NEW_MEMBER:"))
                    {
                        string newMember = message.Substring("NEW_MEMBER:".Length);
                        DispatcherQueue.TryEnqueue(() => Members.Add(newMember));
                    }
                    else if (message.StartsWith("MESSAGE:"))
                    {
                        string chatMessage = message.Substring("MESSAGE:".Length);
                        DispatcherQueue.TryEnqueue(() => Messages.Add(chatMessage));
                    }
                }
            });
        }



        private ContentDialog _currentDialog;

        private async Task ShowErrorDialog(string title, string content)
        {
            // 检查是否有对话框正在显示
            if (_currentDialog != null)
            {
                _currentDialog.Hide();
            }

            try
            {
                _currentDialog = new ContentDialog
                {
                    XamlRoot = Content.XamlRoot,
                    Title = title,
                    Content = content,
                    CloseButtonText = "确定"
                };

                await _currentDialog.ShowAsync(ContentDialogPlacement.InPlace);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // 捕获异常，但这里可能无法处理，如果需要可以记录日志
            }
            finally
            {
                _currentDialog = null; // 确保对话框关闭后可以再次显示新的对话框
            }
        }


    }
}
