//using System;
//using System.Collections.Generic;
//using System.IO.Pipes;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Windows.ApplicationModel.Core;
//using Windows.UI.Core;
//using Neko.EFT.Manager.X.Pages;

//namespace Neko.EFT.Manager.X.Classes
//{
//    internal class PipeServer
//    {
//        private const string PipeName = "IPPipe";
//        private NamedPipeServerStream pipeServer;
//        private ConnectPage connectPage;

//        public void StartServer()
//        {
//            pipeServer = new NamedPipeServerStream(
//                PipeName,
//                PipeDirection.In,
//                NamedPipeServerStream.MaxAllowedServerInstances,
//                PipeTransmissionMode.Byte,
//                PipeOptions.Asynchronous
//            );

//            Task.Run(async () =>
//            {
//                await pipeServer.WaitForConnectionAsync();

//                using (var reader = new StreamReader(pipeServer, Encoding.UTF8))
//                {
//                    while (pipeServer.IsConnected)
//                    {
//                        string ipAddress = await reader.ReadLineAsync();
//                        if (!string.IsNullOrEmpty(ipAddress))
//                        {
//                            // 更新 UI 线程上的 TextBox
//                            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
//                                CoreDispatcherPriority.Normal,
//                                () =>
//                                {
//                                    // 假设你有一个名为 AddressBox 的 TextBox
//                                    // 将 TextBox 设为你实际的 TextBox 控件
//                                    connectPage.AddressBox.Text = ipAddress;
//                                }
//                            );
//                        }
//                    }
//                }
//            });
//        }

//        public void StopServer()
//        {
//            pipeServer?.Dispose();
//        }
//    }
//}
