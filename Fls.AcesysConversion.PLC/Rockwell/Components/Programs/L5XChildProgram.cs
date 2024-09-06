using Fls.AcesysConversion.PLC.Rockwell.Components.Tags;
using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.Programs
{
    public class L5XChildProgram : L5XCollection
    {
        public L5XChildProgram(string? prefix, string localName, string? namespaceURI, XmlDocument doc, int seq)
            : base(prefix ?? string.Empty, localName, namespaceURI ?? string.Empty, doc)
        {
            MessageboardReference = seq;
        }

        public string ChildProgramName
        {
            get
            {
                return this.Attributes["Name"]!.Value;
            }
            set
            {
                if (this.Attributes["Name"] != null)
                {
                    this.Attributes["Name"]!.Value = value;
                }
                else
                {
                    XmlAttribute nameAttr = this.OwnerDocument.CreateAttribute("Name");
                    nameAttr.Value = value;
                    this.Attributes.Append(nameAttr);
                }
            }
        }

        public int MessageboardReference { get; private set; }

        // Static method to create L5XChildProgram from XmlNode
        public static L5XChildProgram FromXmlNode(XmlNode node, XmlDocument doc, int seq)
        {
            var childProgram = new L5XChildProgram(node.Prefix, node.LocalName, node.NamespaceURI, doc, seq);
            childProgram.ChildProgramName = node.Attributes["Name"].Value;
            foreach (XmlAttribute attr in node.Attributes)
            {
                childProgram.Attributes.Append(attr.Clone() as XmlAttribute);
            }
            foreach (XmlNode childNode in node.ChildNodes)
            {
                childProgram.AppendChild(childNode.Clone() as XmlNode);
            }
            return childProgram;
        }

        // Additional properties and methods can be added here as needed
    }
}