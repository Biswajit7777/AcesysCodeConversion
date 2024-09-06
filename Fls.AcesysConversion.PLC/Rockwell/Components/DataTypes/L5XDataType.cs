using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.DataTypes;

public class L5XDataType : RockwellL5XItemBase
{
    public bool IsUpgraded { get; set; } = false;

    public L5XDataType(string? prefix, string localName, string? namespaceURI, XmlDocument doc, int seq)
        : base(prefix, localName, namespaceURI, doc)
    {
        MessageboardReference = seq;
    }
}
