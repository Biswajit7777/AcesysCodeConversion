using Fls.AcesysConversion.Common.Enums;
using System.Collections.ObjectModel;
using System.Xml;

namespace Fls.AcesysConversion.Common.Entities;

public class UserMessage
{
    public UserMessage(int id, int originalId, string nodeType, XmlNode? node, UserMessageTypes userMessageType, string operation, string replacementType, string message)
    {
        Id = id;
        OriginalId = originalId;
        UserMessageType = userMessageType;
        ReplacementType = replacementType;
        Operation = operation;
        Message = message;
        Sequence = id;
        NodeType = nodeType;
        Node = node;
        Name = node?.Attributes?["Name"]?.Value ?? "-";
        XmlString = node?.OuterXml;
    }
    public UserMessage(int id, int originalId, string nodeType, string name, UserMessageTypes userMessageType, string operation, string replacementType, string message)
    {
        Id = id;
        OriginalId = originalId;
        UserMessageType = userMessageType;
        ReplacementType = replacementType;
        Operation = operation;
        Message = message;
        Sequence = id;
        NodeType = nodeType;
        Name = name;
    }

    public int Id { get; set; }
    public int OriginalId { get; set; }
    public UserMessageTypes UserMessageType { get; set; } = UserMessageTypes.Information;
    public string NodeType { get; set; } = "Unknown";
    public string ReplacementType { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Sequence { get; set; } = 0;
    public XmlNode? Node { get; set; }
    public string? XmlString { get; set; }
}

public class UserMessageList : ObservableCollection<UserMessage>
{
    public UserMessageList() : base() { }
}
