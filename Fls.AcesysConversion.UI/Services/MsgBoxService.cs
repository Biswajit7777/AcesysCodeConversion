using Fls.AcesysConversion.UI.Services.Interface;
using System.Windows;

namespace Fls.AcesysConversion.UI.Services;

public class MsgBoxService : IMsgBoxService
{
    MessageBoxResult IMsgBoxService.Show(string msgBoxText, string title, MessageBoxButton button, MessageBoxImage img, MessageBoxResult defaultReturn)
    {
        return MessageBox.Show(msgBoxText, title, button, img);
    }
}
