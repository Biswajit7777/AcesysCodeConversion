using Fls.AcesysConversion.PLC.Rockwell.Components.Programs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.Tasks
{
    public class L5XScheduledPrograms : L5XCollection
    {
        public L5XScheduledPrograms(string? prefix, string localName, string? namespaceURI, XmlDocument doc, int seq)
        : base(prefix ?? string.Empty, localName, namespaceURI ?? string.Empty, doc)
        {
            MessageboardReference = seq;
        }

        public L5XScheduledProgram? Item(int index)
        {
            return (L5XScheduledProgram?)base[index];
        }

        public L5XScheduledProgram? Item(string id)
        {
            return (L5XScheduledProgram?)base[id];
        }

    }
}
