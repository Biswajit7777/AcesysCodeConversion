using System;
using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.Tasks
{
    public class L5XScheduledProgram : L5XCollection
    {
        public L5XScheduledProgram(string? prefix, string localName, string? namespaceURI, XmlDocument doc, int seq)
        : base(prefix ?? string.Empty, localName, namespaceURI ?? string.Empty, doc)
        {
            MessageboardReference = seq;
        }

        public string? ScheduledProgramName
        {
            get => Attributes["Name"]?.Value;
            set
            {
                if (Attributes["Name"] == null)
                {
                    XmlAttribute nameAttribute = OwnerDocument.CreateAttribute("Name");
                    nameAttribute.Value = value;
                    Attributes.Append(nameAttribute);
                }
                else
                {
                    Attributes["Name"].Value = value;
                }
            }
        }

        public static L5XScheduledProgram FromXmlNode(XmlNode node, XmlDocument doc, int seq)
        {
            if (node.Attributes["Name"] == null)
            {
                throw new ArgumentException("The provided XmlNode does not have a Name attribute.");
            }

            var scheduledProgram = new L5XScheduledProgram(
                node.Prefix,
                node.LocalName,
                node.NamespaceURI,
                doc,
                seq);

            foreach (XmlAttribute attr in node.Attributes)
            {
                var importedAttr = (XmlAttribute)doc.ImportNode(attr, true);
                scheduledProgram.Attributes.Append(importedAttr);
            }

            foreach (XmlNode childNode in node.ChildNodes)
            {
                var importedNode = doc.ImportNode(childNode, true);
                scheduledProgram.AppendChild(importedNode);
            }

            return scheduledProgram;
        }

        public static L5XScheduledProgram CreateNew(string name, XmlDocument doc, int seq)
        {
            var scheduledProgram = new L5XScheduledProgram(string.Empty, "ScheduledProgram", string.Empty, doc, seq);

            scheduledProgram.ScheduledProgramName = name;
            return scheduledProgram;
        }
    }
}