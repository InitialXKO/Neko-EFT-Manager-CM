using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Controls
{
    public sealed partial class SelectEditionDialog : ContentDialog
    {
        public string edition = "";

        public SelectEditionDialog()
        {
            this.InitializeComponent();
            EditionBox.SelectedIndex = 0;
        }

        private void EditionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EditionBox.SelectedIndex != -1)
            {
                // 获取选中的项
                ComboBoxItem selectedItem = (ComboBoxItem)EditionBox.SelectedItem;

                // 获取选中项的描述
                string editionDescription = selectedItem.Tag.ToString();

                // 更新描述区域的文本
                DescriptionTextBlock.Text = editionDescription;

                // 你可以继续获取选中的内容
                edition = selectedItem.Content.ToString();
            }
        }

    }
}
