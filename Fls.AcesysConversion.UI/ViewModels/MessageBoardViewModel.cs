using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Fls.AcesysConversion.Common.Entities;
using Fls.AcesysConversion.UI.Messages;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;

namespace Fls.AcesysConversion.UI.ViewModels;

public partial class MessageBoardViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<UserMessage> messages = new();

    [ObservableProperty]
    public UserMessage? selectedUserMessage;

    public IRelayCommand? LocateMessageCommand { get; }
    public IRelayCommand? SaveMessagesCommand { get; }
    public IRelayCommand? SelectionChangedCommand { get; }

    public MessageBoardViewModel()
    {
        LocateMessageCommand = new RelayCommand(OnLocateMessage, CanLocateMessage);
        SaveMessagesCommand = new RelayCommand(OnSaveAllMessages, CanSaveAllMessages);
        SelectionChangedCommand = new RelayCommand(OnSelectionChanged);

        Messenger.Register<MessageBoardViewModel, UserMessageUpdates>(this, (r, m) => r.UserMessageUpdatesHandler(m.UserMessage));
        Messenger.Register<MessageBoardViewModel, UserMessageClearUpdates>(this, (r, m) => r.ClearMessages());
        BindingOperations.EnableCollectionSynchronization(Messages, new object());
    }

    private void OnSelectionChanged()
    {
        RaiseCanExecuteChanged();
    }

    private void UserMessageUpdatesHandler(UserMessage? userMessage)
    {
        App.Current.Dispatcher.Invoke(
            //todo : possible to remove dispatcher? check
            () =>
            {
                if (userMessage == null)
                {
                    return;
                }

                string key = MakeKey(userMessage);
                UserMessage? itemToRemove = Messages.Where(m => MakeKey(m).Equals(key)).FirstOrDefault();
                if (itemToRemove != null)
                {
                    _ = Messages.Remove(itemToRemove);
                }
                Messages.Add(userMessage);
                RaiseCanExecuteChanged();
            });
    }

    private static string MakeKey(UserMessage userMessage)
    {
        return $"{userMessage.Operation}_{userMessage.ReplacementType}_{userMessage.UserMessageType}_{userMessage.Name}".ToUpper();
    }

    private void ClearMessages()
    {
        Messages.Clear();
        Messages = new();
        RaiseCanExecuteChanged();
    }

    private bool CanSaveAllMessages()
    {
        return Messages.Any();
    }

    private bool CanLocateMessage()
    {
        return SelectedUserMessage != null;
    }

    private void OnLocateMessage()
    {
        _ = Messenger.Send(new UserMessageSelectionChanged(SelectedUserMessage!.Id, SelectedUserMessage!.OriginalId));
    }

    private void OnSaveAllMessages()
    {
        //IMsgBoxService? msgBoxService = Ioc.Default.GetService<IMsgBoxService>();

        //try
        //{
        //    string? sPath;
        //    ISaveFileDlgVM? dialog = Ioc.Default.GetService<ISaveFileDlgVM>();

        //    if (dialog != null)
        //    {
        //        sPath = dialog.SaveFileDlg("L5X", "L5X|*.l5x|XML|*.xml");
        //        if (!string.IsNullOrEmpty(sPath))
        //        {
        //            xmlDocumentReplaced?.Save(sPath);
        //            ShowInfoMessage(msgBoxService, "File saved successfully");
        //            IsDirty = false;
        //        }
        //    }
        //}
        //catch (Exception ex)
        //{
        //    ShowErrorMessage(msgBoxService, ex);
        //}
        //finally
        //{
        //    RaiseCanExecuteChanged();
        //}
    }

    private void RaiseCanExecuteChanged()
    {
        LocateMessageCommand?.NotifyCanExecuteChanged();
        SaveMessagesCommand?.NotifyCanExecuteChanged();
    }
}
