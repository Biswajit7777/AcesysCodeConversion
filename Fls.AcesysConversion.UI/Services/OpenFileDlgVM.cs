using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fls.AcesysConversion.UI.Services.Interface;
using Microsoft.Win32;
using System;
using System.Windows;

namespace Fls.AcesysConversion.UI.Services;

public class OpenFileDlgVM : ObservableRecipient, IOpenFileDlgVM
{
    public OpenFileDlgVM()
    {

    }

    public string? OpenFileDlg(string defaultExtension, string filter)
    {

        FileDialog dialog = new OpenFileDialog();
        try
        {
            dialog.DefaultExt = defaultExtension;
            dialog.Filter = filter;
            dialog.FilterIndex = 1;
            dialog.Title = "Acesys File Conversion - Open File";

            _ = dialog.ShowDialog();
            if (string.IsNullOrEmpty(dialog.FileName))
            {
                return default;
            }
        }
        catch (Exception ex)
        {
            RelayCommand<string?> MsgCmd = new(m => MessageBox.Show("Unexpected error:\\n\\n" + ex.ToString()));
            MsgCmd.Execute("");
        }

        return dialog.FileName;
    }

}
