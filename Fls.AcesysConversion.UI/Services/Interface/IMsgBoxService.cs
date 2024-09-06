using System.Windows;

namespace Fls.AcesysConversion.UI.Services.Interface
{
    public interface IMsgBoxService
    {
        MessageBoxResult Show(string msgBoxText, string title = "",
            MessageBoxButton button = MessageBoxButton.OK,
            MessageBoxImage img = MessageBoxImage.Information,
            MessageBoxResult defaultReturn = MessageBoxResult.None);
    }

}
