using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;
using WinUIEx;

namespace Neko.EFT.Manager.X.Windows
{
    public sealed partial class UserAgreementWindow : Window
    {
        // TaskCompletionSource 用于等待用户选择
        private TaskCompletionSource<bool> _agreementCompletionSource;

        public UserAgreementWindow()
        {
            this.InitializeComponent();
            // 设置窗口的标题和尺寸
            this.Title = "用户协议";
            WindowManager manager = WindowManager.Get(this);
            manager.MinHeight = 600;
            manager.MaxHeight = 600;
            manager.MinWidth = 900;
            manager.MaxWidth = 900;
            manager.IsResizable = false;
            manager.IsMaximizable = false;
            _agreementCompletionSource = new TaskCompletionSource<bool>();
            this.CenterOnScreen();
            AcceptButton.IsEnabled = false;

            // 订阅窗口关闭事件
            this.Closed += UserAgreementWindow_Closed;
            SetUserAgreementContent();
        }

        private void SetUserAgreementContent()
        {
            // 清空已有的 Inlines
            UserAgreementTextBlock.Inlines.Clear();

            // 添加协议内容
            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "欢迎使用 Neko EFT Manager Pro 专业版！\n\n请您在使用本软件前仔细阅读以下用户协议。本协议包含了您使用 Neko EFT Manager Pro（以下简称“本软件”）的所有条款和条件。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "安装使用及注意事项\n\n1. 安装选择：\n   - 本软件建议选择 个人安装 模式。安装完成后，请在设置页选择《逃离塔克夫》游戏的安装目录，即可完成部署。现支持通过本地压缩包资源自动安装游戏文件完成部署\n\n",
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "2. 管理员模式注意事项：\n   - 如果您在个人安装时无法选择游戏目录，请检查是否以管理员模式运行。如果您以管理员模式运行本软件，可能会导致无法调起文件选择器进行目录选择。\n\n",
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "3. 安装目录建议：\n   - 请勿将本软件安装至默认的 C 盘目录。由于 Windows 的权限隔离机制，安装在 C 盘可能会导致无法正常选择游戏的安装目录。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "用户协议\n\n1. 接受协议：\n   使用本软件即表示您同意并接受本协议的所有条款。如果您不同意本协议的条款，请勿使用本软件。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "2. 修改协议：\n   本软件保留随时修改本协议条款的权利。修改后的协议条款将会在本软件内发布，您继续使用本软件即表示您接受修改后的协议条款。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "3. 隐私保护：\n   我们承诺保护您的个人信息。有关个人信息的收集、使用和保护，请参阅我们的隐私政策。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "4. 知识产权：\n   本软件及其所有相关的版权、商标和其他知识产权均归 Neko EFT Manager Pro 所有。未经授权，不得复制、修改、传播或以其他方式使用本软件的内容。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "软件功能\n\nNeko EFT Manager Pro 是一个综合的《逃离塔克夫》离线版管理器，旨在提供便捷的游戏管理和增强体验。主要功能包括：\n"
                       + "   - 游戏一键启动与管理：简化游戏启动过程，提供一键启动和游戏管理功能。\n"
                       + "   - 服务端启动与管理：支持启动和管理游戏服务端，确保服务端的正常运行。\n"
                       + "   - 服务端 Mod 管理：方便地添加、删除和管理服务端的 Mod。\n"
                       + "   - 客户端 Mod 管理：提供客户端 Mod 的管理功能，使您可以轻松安装和卸载客户端 Mod。\n"
                       + "   - N2N MX 联机平台：支持 N2N MX 联机平台功能，方便玩家进行在线游戏。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "N2N MX 联机平台用户协议\n\nN2N MX 是专门为《逃离塔克夫》离线版提供的联机平台，旨在通过虚拟局域网联机，实现异地玩家的稳定联机体验。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "1. 使用条件：\n   - 您必须首先安装 Project Fika 离线合作 Mod 作为联机功能的前置条件。此 Mod 是实现《逃离塔克夫》离线合作功能的必要组件。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "2. 功能描述：\n   - 一键联机配置：N2N MX 提供简化的联机配置过程，用户可以通过本软件一键完成所有联机相关的配置项。\n"
                       + "   - 异地组网：通过 N2N 的虚拟局域网功能，您可以与异地玩家组建局域网，模拟局域网环境实现离线版游戏的多人联机。\n"
                       + "   - 联机稳定性：N2N MX 平台采用低延迟网络连接，提供较为稳定的联机体验，但由于网络环境的复杂性，联机效果仍取决于各玩家的网络条件。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "3. 联机服务的使用限制：\n   - N2N MX 联机平台仅用于合法的《逃离塔克夫》离线合作游戏场景。禁止任何形式的作弊、滥用、商业化操作或其他违反相关法律的行为。\n"
                       + "   - 禁止利用平台进行与游戏无关的非法活动或不当行为。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "4. 责任声明：\n   - Neko EFT Manager Pro 不对联机过程中的网络稳定性、数据丢失、延迟问题等作出任何担保。使用 N2N MX 平台的风险由用户自行承担。\n"
                       + "   - Neko EFT Manager Pro 不对因使用联机平台而产生的任何直接或间接的损害承担责任，包括但不限于数据丢失、游戏进度丢失、网络中断等。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "使用条款\n\n1. 授权许可：\n   我们授予您一个非独占、不可转让的许可，以在您的设备上使用本软件。您不得将本软件用于商业目的或未经授权的用途。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "2. 禁止行为：\n   您不得逆向工程、反编译、拆解本软件或进行任何形式的破解。不得未经授权使用本软件进行非法活动。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "3. 更新和维护：\n   我们可能会提供软件的更新和维护服务。您同意定期更新本软件，以获取最新的功能和修复。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "免责声明\n\n1. 软件性能：\n   我们不对本软件的性能、稳定性、兼容性或适用性作出任何保证。使用本软件的风险由您自行承担。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "2. 责任限制：\n   在法律允许的范围内，我们不对因使用本软件或 N2N MX 联机平台而产生的任何直接、间接、附带或后果性损害承担责任。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "终止条款\n\n1. 协议终止：\n   如您违反本协议的任何条款，我们有权立即终止您的许可，并采取适当的法律措施。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "2. 数据删除：\n   在协议终止后，您应立即停止使用本软件，并删除所有本软件的副本及相关文件。\n\n"
            });

            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "协议须知\n\n   同意既代表你完全理解用户协议及其使用须知内容。后续因个人原因导致的问题开发者没有义务为你解决出现的问题。\n\n"
            });

            // 红色标识的部分
            UserAgreementTextBlock.Inlines.Add(new Run
            {
                Text = "请在下方输入“我完全理解用户协议及其使用须知内容”以接受协议",
                Foreground = new SolidColorBrush(Colors.Red),  // 设置文本颜色为红色
                FontWeight = FontWeights.Bold  // 设置为粗体
            });
        }




        // 当用户关闭窗口时，默认行为是拒绝协议
        private void UserAgreementWindow_Closed(object sender, WindowEventArgs args)
        {
            _agreementCompletionSource.TrySetResult(false);
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            _agreementCompletionSource.SetResult(true);
            this.Close();
        }

        private void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            _agreementCompletionSource.SetResult(false);
            this.Close();
        }

        private void AgreementScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (AgreementScrollViewer.VerticalOffset >= AgreementScrollViewer.ScrollableHeight)
            {
                ConfirmationTextBox.IsEnabled = true; // 启用输入框
            }
        }

        private void ConfirmationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AcceptButton.IsEnabled = ConfirmationTextBox.Text == "我完全理解用户协议及其使用须知内容";
        }

        public Task<bool> WaitForUserAgreementAsync()
        {
            return _agreementCompletionSource.Task;
        }
    }
}
