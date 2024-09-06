using CommunityToolkit.Mvvm.Input;
using Fls.AcesysConversion.UI.Services.Interface;
using Microsoft.Win32;
using System;
using System.Windows;

namespace Fls.AcesysConversion.UI.Services;

public class SaveFileDlgVM : ISaveFileDlgVM
{
    public string? SaveFileDlg(string defaultExtension, string filter)
    {
        FileDialog dialog = new SaveFileDialog
        {
            DefaultExt = defaultExtension,
            Filter = filter,
            FilterIndex = 1
        };
        try
        {
            _ = dialog.ShowDialog();
            if (string.IsNullOrEmpty(dialog.FileName))
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            RelayCommand<string> MsgCmd = new(m => MessageBox.Show("Unexpected error:\\n\\n" + ex.ToString()));
            MsgCmd.Execute("");
        }
        return dialog.FileName;
    }
}
