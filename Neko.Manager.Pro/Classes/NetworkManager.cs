using System.Net;
using System.Net.Sockets;
using System.Text;

public class NetworkManager
{
    private UdpClient _udpClient;
    private IPEndPoint _serverEndPoint;

    public NetworkManager(string serverIp, int port)
    {
        _udpClient = new UdpClient();
        _serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), port);
    }

    public void SendMessage(string message)
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        _udpClient.Send(messageBytes, messageBytes.Length, _serverEndPoint);
    }

    public string ReceiveMessage()
    {
        IPEndPoint remoteEndPoint = null;
        byte[] receivedBytes = _udpClient.Receive(ref remoteEndPoint);
        return Encoding.UTF8.GetString(receivedBytes);
    }
}
