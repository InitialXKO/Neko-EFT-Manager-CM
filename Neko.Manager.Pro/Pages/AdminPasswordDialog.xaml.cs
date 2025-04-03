using Microsoft.UI.Xaml.Controls;

namespace Neko.EFT.Manager.X.Pages
{
    public sealed partial class AdminPasswordDialog : ContentDialog
    {
        public string AdminPassword { get; private set; }

        public AdminPasswordDialog()
        {
            this.InitializeComponent();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            AdminPassword = AdminPasswordBox.Password;
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            AdminPassword = null;
        }
    }
}
