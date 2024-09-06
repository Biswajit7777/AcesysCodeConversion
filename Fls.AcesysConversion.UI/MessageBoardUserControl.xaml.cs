using CommunityToolkit.Mvvm.DependencyInjection;
using Fls.AcesysConversion.UI.CustomControls;
using Fls.AcesysConversion.UI.Services.Interface;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Fls.AcesysConversion.UI;

public partial class MessageBoardUserControl : UserControl
{
    private GridViewColumnHeader? listViewConvertedSortCol = null;
    private SortAdorner? listViewConvertedSortAdorner = null;

    public MessageBoardUserControl()
    {
        InitializeComponent();
        DataContext = new MessageBoardViewModel();
    }

    private void MyListViewConvertedColumnHeader_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            GridViewColumnHeader? column = sender as GridViewColumnHeader;
            string? sortBy = string.Empty;

            if (column != null)
            {
                sortBy = column.Tag.ToString();
            }

            if (listViewConvertedSortCol != null)
            {
                AdornerLayer.GetAdornerLayer(listViewConvertedSortCol).Remove(listViewConvertedSortAdorner);
                MyListViewConverted.Items.SortDescriptions.Clear();
            }

            ListSortDirection newDir = ListSortDirection.Ascending;
            if (listViewConvertedSortCol == column && listViewConvertedSortAdorner?.Direction == newDir)
            {
                newDir = ListSortDirection.Descending;
            }

            listViewConvertedSortCol = column;
            if (listViewConvertedSortCol != null)
            {
                listViewConvertedSortAdorner = new SortAdorner(listViewConvertedSortCol, newDir);
            }
            AdornerLayer.GetAdornerLayer(listViewConvertedSortCol).Add(listViewConvertedSortAdorner);
            MyListViewConverted.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
        }
        catch (Exception ex)
        {
            IMsgBoxService? msgBoxService = Ioc.Default.GetService<IMsgBoxService>();
            if (msgBoxService != null)
            {
                _ = msgBoxService.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void MyListViewConverted_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ((MessageBoardViewModel)DataContext).LocateMessageCommand!.Execute(sender);
    }
}
