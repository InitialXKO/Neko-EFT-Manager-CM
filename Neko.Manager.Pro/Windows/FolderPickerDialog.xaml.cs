using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using RoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using Window = Microsoft.UI.Xaml.Window;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Neko.EFT.Manager.X.Windows;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class FolderPickerDialog : Window
{
    public string SelectedPath { get; private set; }

    public FolderPickerDialog()
    {
        this.InitializeComponent();
        LoadDrives();
    }

    private void LoadDrives()
    {
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.IsReady)
            {
                var rootNode = new TreeViewNode { Content = drive.RootDirectory.FullName };
                rootNode.Children.Add(new TreeViewNode()); // 占位以触发展开事件
                FolderTree.RootNodes.Add(rootNode);
            }
        }
    }

    private void FolderTree_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
    {
        // 检查是否已经加载
        if (args.Node.Children.Count == 1)
        {
            args.Node.Children.Clear(); // 清空占位符

            string path = args.Node.Content.ToString();
            try
            {
                foreach (var directory in Directory.GetDirectories(path))
                {
                    var childNode = new TreeViewNode { Content = Path.GetFileName(directory) };
                    childNode.Children.Add(new TreeViewNode()); // 占位
                    args.Node.Children.Add(childNode);
                }
            }
            catch { } // 忽略权限错误
        }
    }

    private void FolderTree_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (sender.SelectedNode is TreeViewNode selectedNode)
        {
            SelectedPath = selectedNode.Content.ToString();
        }
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedPath = null; // 取消时清空路径
        this.Close();
    }
}
