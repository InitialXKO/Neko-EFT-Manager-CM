using System.Configuration;
using System.Data;
using System.Windows;
using FileSelector;

namespace FileSelector;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        
    }
    public static ManagerConfig ManagerConfig { get; set; }

}

