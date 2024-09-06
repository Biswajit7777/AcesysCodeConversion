using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Fls.AcesysConversion.Common.Entities;

namespace Fls.AcesysConversion.UI.Messages;

public partial class MessageBroker : ObservableRecipient, IMessageBoardSubscriber
{
    public void FileClosed()
    {
        _ = Messenger.Send(new UserMessageClearUpdates());
    }
    public void AnnounceNewUserMessage(UserMessage userMessage)
    {
        if (userMessage == null)
        {
            return;
        }
        _ = Messenger.Send(new UserMessageUpdates(userMessage));
    }
}
