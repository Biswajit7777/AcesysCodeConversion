using Fls.AcesysConversion.Common.Entities;

namespace Fls.AcesysConversion.UI.Messages;

public record class UserMessageUpdates(UserMessage? UserMessage);
public record class UserMessageClearUpdates();
public record class UserMessageSelectionChanged(int Reference, int OriginalReference);