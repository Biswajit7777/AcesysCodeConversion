using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components;

public class L5XContent : RockwellL5XItemBase
{
    public L5XContent(string? prefix, string localName, string? namespaceURI, XmlDocument doc, int seq)
        : base(prefix, localName, namespaceURI, doc)
    {
        MessageboardReference = seq;
    }

    public L5XController? Controller => (L5XController?)SelectSingleNode("Controller");



}