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
                    // ʹ�� UDP Э������ Supernode
                    client.Connect("103.40.13.88", 28663); // �滻Ϊʵ�ʵ� IP �Ͷ˿�

                    // ����һ���յ� UDP ���ݰ�����������
                    byte[] sendBuffer = new byte[0];
                    client.Send(sendBuffer, sendBuffer.Length);

                    // ���Խ�����Ӧ
                    var serverEndpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
                    var receiveTask = client.ReceiveAsync();

                    // ���ý��ճ�ʱʱ��
                    if (receiveTask.Wait(5000))
                    {
                        byte[] response = receiveTask.Result.Buffer;
                        ShowErrorDialog("���", "Supernode ���ӳɹ���");
                    }
                    else
                    {
                        ShowErrorDialog("��ʱ", "���� Supernode ��ʱ��");
                    }
                }
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                string errorMessage = $"�޷����ӵ� Supernode��Socket ����: {ex.Message} (�������: {ex.SocketErrorCode})";
                ShowErrorDialog("�������", errorMessage);
            }
            catch (System.TimeoutException ex)
            {
                string errorMessage = $"���� Supernode ��ʱ������: {ex.Message}";
                ShowErrorDialog("��ʱ����", errorMessage);
            }
            catch (System.Exception ex)
            {
                string errorMessage = $"������δ֪����: {ex.Message}";
                ShowErrorDialog("δ֪����", errorMessage);
            }
        }




        private void CreateRoom_Click(object sender, RoutedEventArgs e)
        {
            CheckSupernodeStatus();  // �������

            string roomName = RoomNameTextBox.Text;
            string memberName = MemberNameTextBox.Text;

            if (string.IsNullOrEmpty(roomName) || string.IsNullOrEmpty(memberName))
            {
                ShowErrorDialog("����", "�������ƺͳ�Ա���Ʋ���Ϊ��");
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
                ShowErrorDialog("����", "�������ƺͳ�Ա���Ʋ���Ϊ��");
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
                Messages.Add($"��: {message}");
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
            // ����Ƿ��жԻ���������ʾ
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
                    CloseButtonText = "ȷ��"
                };

                await _currentDialog.ShowAsync(ContentDialogPlacement.InPlace);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // �����쳣������������޷����������Ҫ���Լ�¼��־
            }
            finally
            {
                _currentDialog = null; // ȷ���Ի���رպ�����ٴ���ʾ�µĶԻ���
            }
        }


    }
}
