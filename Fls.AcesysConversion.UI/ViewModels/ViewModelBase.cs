using CommunityToolkit.Mvvm.ComponentModel;
using Fls.AcesysConversion.UI.Services.Interface;
using System;
using System.Windows;

namespace Fls.AcesysConversion.UI.ViewModels;

public abstract partial class ViewModelBase : ObservableRecipient
{
    public static void ShowErrorMessage(IMsgBoxService? ms, Exception e)
    {
        if (ms != null)
        {
            _ = ms.Show(e.Message + $"\n\n{e.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public static void ShowInfoMessage(IMsgBoxService? ms, string message)
    {
        if (ms != null)
        {
            _ = ms.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
