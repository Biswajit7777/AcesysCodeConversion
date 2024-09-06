using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components;

public class L5XUnknownElement : RockwellL5XItemBase
{
    public L5XUnknownElement(string? prefix, string localName, string? namespaceURI, XmlDocument doc, int seq)
        : base(prefix, localName, namespaceURI, doc)
    {
        MessageboardReference = seq;
    }
}
