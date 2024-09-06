using CommunityToolkit.Mvvm.DependencyInjection;
using Fls.AcesysConversion.UI.Services.Interface;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace Fls.AcesysConversion.UI;

public partial class MainWindow : System.Windows.Window
{


    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        SetVersion();
    }

    private void SetVersion()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        string version = fileVersionInfo.ProductVersion ?? "";
        Title += $" - v{version}";
    }

    private bool CanClosePrompt()
    {
        bool canClose = true;
        if (((MainWindowViewModel)DataContext).IsDirty)
        {
            MessageBoxResult result = MessageBoxResult.None;
            IMsgBoxService? msgBoxService = Ioc.Default.GetService<IMsgBoxService>();
            if (msgBoxService != null)
            {
                result = msgBoxService.Show("Data might have been changed.  Do you want to discard changes ?", "Error", MessageBoxButton.YesNo, MessageBoxImage.Error);
            }
            canClose = result == MessageBoxResult.Yes;
        }
        return canClose;
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (!CanClosePrompt())
        {
            e.Cancel = true;
        }
    }

    private void WindowClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
