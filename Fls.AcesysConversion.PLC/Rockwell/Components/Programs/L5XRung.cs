using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.Programs
{
    public class L5XRung : L5XCollection
    {
        public L5XRung(string? prefix, string localName, string? namespaceURI, XmlDocument doc, int seq)
            : base(prefix ?? string.Empty, localName, namespaceURI ?? string.Empty, doc)
        {
            MessageboardReference = seq;
        }

        public string Number
        {
            get
            {
                if (Attributes["Number"] != null)
                    return Attributes["Number"].Value;
                else
                    return string.Empty;
            }
            set
            {
                if (Attributes["Number"] == null)
                {
                    XmlAttribute newAttribute = OwnerDocument.CreateAttribute("Number");
                    Attributes.Append(newAttribute);
                }
                Attributes["Number"].Value = value;
            }
        }

        public static explicit operator L5XRung(string v)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement element = doc.CreateElement("", "Rung", "");
            element.InnerXml = v;

            L5XRung rung = new L5XRung("", "Rung", "", doc, 0);
            rung.InnerXml = element.InnerXml;

            return rung;
        }
    }
}
