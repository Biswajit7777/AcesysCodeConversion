using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.Programs;

public class L5XRoutines : L5XCollection
{
    public L5XRoutines(string? prefix, string localName, string? namespaceURI, XmlDocument doc, int seq)
        : base(prefix ?? string.Empty, localName, namespaceURI ?? string.Empty, doc)
    {
        MessageboardReference = seq;
    }
}
