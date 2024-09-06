using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.AddOns;

public class L5XAddOnInstructionDefinition : RockwellL5XItemBase
{
    public L5XAddOnInstructionDefinition(string? prefix, string localName, string? namespaceURI, XmlDocument doc, int seq)
        : base(prefix, localName, namespaceURI, doc)
    {
        MessageboardReference = seq;
    }
}
