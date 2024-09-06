using Fls.AcesysConversion.PLC.Rockwell.Components.Programs;
using Fls.AcesysConversion.PLC.Rockwell.Components.Tags;
using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.Tasks
{
    public class L5XTask : L5XCollection
    {
        public L5XTask(string? prefix, string localName, string? namespaceURI, XmlDocument doc, int seq)
        : base(prefix ?? string.Empty, localName, namespaceURI ?? string.Empty, doc)
        {
            MessageboardReference = seq;
        }

        public string TaskName
        {
            get
            {
                return this.Attributes["Name"]!.Value;
            }
            set
            {
                this.Attributes["Name"]!.Value = value;
            }
        }
        
    }
}
