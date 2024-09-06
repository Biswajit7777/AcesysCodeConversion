namespace Fls.AcesysConversion.Common.Entities;

public interface IMessageBoardSubscriber
{
    void AnnounceNewUserMessage(UserMessage userMessage);
}