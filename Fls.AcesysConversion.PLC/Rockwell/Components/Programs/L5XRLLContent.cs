using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.Programs;

public class L5XRllContent : L5XCollection
{
    public L5XRllContent(string? prefix, string localName, string? namespaceURI, XmlDocument doc, int seq)
        : base(prefix ?? string.Empty, localName, namespaceURI ?? string.Empty, doc)
    {
        MessageboardReference = seq;
    }
}
