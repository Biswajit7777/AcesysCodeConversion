using Fls.AcesysConversion.PLC.Rockwell.Components.Tags;
using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.Programs;

public class L5XProgram : L5XCollection
{
    public L5XProgram(string? prefix, string localName, string? namespaceURI, XmlDocument doc, int seq)
        : base(prefix ?? string.Empty, localName, namespaceURI ?? string.Empty, doc)
    {
        MessageboardReference = seq;
    }

    public string ProgramName
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

    public IEnumerable<L5XRoutine> Routines
    {
        get
        {
            XmlNodeList? nl = SelectNodes("Routines/Routine");

            if (nl != null)
            {
                foreach (L5XRoutine node in nl)
                {
                    yield return node;
                }
            }
        }

    }

    public L5XTag? GetAssociatedTagForRoutine(string routineName)
    {
        XmlNode? tag = SelectSingleNode($"Tags/Tag[@Name='{routineName + "_FB"}']");
        if (tag != null)
        {
            return (L5XTag)tag;
        }
        else
        {
            return null;
        }
    }

}
