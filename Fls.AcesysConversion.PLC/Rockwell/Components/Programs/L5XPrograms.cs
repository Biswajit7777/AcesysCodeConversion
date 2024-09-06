using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Helpers.Database;
using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.Programs;

public class L5XPrograms : L5XCollection
{
    public RockwellL5XProject Project;

    public L5XPrograms(string prefix, string localName, string namespaceURI, XmlDocument doc, int seq)
        : base(prefix, localName, namespaceURI, doc)
    {
        MessageboardReference = seq;
        Project = (RockwellL5XProject)OwnerDocument;
    }

    public override void UpgradeVersion(L5XCollection? original, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        DbHelper? dbh = DbHelper.Instance.GetDbHelper((int)options.FromVersion, (int)options.ToVersion, Project.Manufacturer);
        L5XPrograms? originalCollection = (L5XPrograms?)original;
        UpgradeEngine? processor = null;

        if (originalCollection != null)
        {
            processor = new UpgradeEngineFactory()
                            .SetCollections(this, originalCollection, Project)
                            .SetUpgradeProperties(options.FromVersion, options.ToVersion, Project.Manufacturer, typeof(L5XPrograms))
                            .Create();
        }

        if (processor != null)
        {
            UpgradeManager.ProcessUpgrade(processor, dbh, options, progress);
        }

        return;
    }

    public L5XProgram? Item(int index)
    {
        return (L5XProgram?)base[index];
    }

    public L5XProgram? Item(string id)
    {
        return (L5XProgram?)base[id];
    }

}
