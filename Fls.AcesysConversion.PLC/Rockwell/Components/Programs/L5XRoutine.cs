using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.Programs;

public class L5XRoutine : L5XCollection
{
    public L5XRoutine(string? prefix, string localName, string? namespaceURI, XmlDocument doc, int seq)
        : base(prefix ?? string.Empty, localName, namespaceURI ?? string.Empty, doc)
    {
        MessageboardReference = seq;
    }

    public string RoutineName
    {
        get => Attributes["Name"]!.Value;
        set => Attributes["Name"]!.Value = value;
    }
}