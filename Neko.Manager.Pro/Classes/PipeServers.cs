//using System.IO.Pipes;
//using System.IO;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.UI.Xaml.Controls;
//using Windows.UI.Core;
//using Windows.ApplicationModel.Core;
//using Neko.EFT.Manager.X.Pages;
//using System;

//public class PipeServers
//{
//    private const string PipeName = "IPPipe";
//    private NamedPipeServerStream pipeServer;
//    public ConnectPage connectPage;
//    public void StartServer()
//    {
//        pipeServer = new NamedPipeServerStream(
//            PipeName,
//            PipeDirection.In,
//            NamedPipeServerStream.MaxAllowedServerInstances,
//            PipeTransmissionMode.Byte,
//            PipeOptions.Asynchronous
//        );

//        Task.Run(async () =>
//        {
//            await pipeServer.WaitForConnectionAsync();

//            using (var reader = new StreamReader(pipeServer, Encoding.UTF8))
//            {
//                while (pipeServer.IsConnected)
//                {
//                    string ipAddress = await reader.ReadLineAsync();
//                    if (!string.IsNullOrEmpty(ipAddress))
//                    {
//                        // 更新 UI 线程上的 TextBox
//                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
//                            CoreDispatcherPriority.Normal,
//                            () =>
//                            {
//                                // 假设你有一个名为 AddressBox 的 TextBox
//                                // 将 TextBox 设为你实际的 TextBox 控件
//                                connectPage.AddressBox.Text = ipAddress;
//                            }
//                        );
//                    }
//                }
//            }
//        });
//    }

//    public void StopServer()
//    {
//        pipeServer?.Dispose();
//    }
//}
