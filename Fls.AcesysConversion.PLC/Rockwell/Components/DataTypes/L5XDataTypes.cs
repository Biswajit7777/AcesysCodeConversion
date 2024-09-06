using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Helpers.Database;
using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.DataTypes;

public class L5XDataTypes : L5XCollection
{
    public RockwellL5XProject Project;

    public L5XDataTypes(string prefix, string localName, string namespaceURI, XmlDocument doc, int seq)
        : base(prefix, localName, namespaceURI, doc)
    {
        MessageboardReference = seq;
        Project = (RockwellL5XProject)OwnerDocument;
    }

    public override void UpgradeVersion(L5XCollection? original, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        DbHelper? dbh = DbHelper.Instance.GetDbHelper((int)options.FromVersion, (int)options.ToVersion, Project.Manufacturer);
        L5XDataTypes? originalCollection = (L5XDataTypes?)original;
        _ = new UpgradeManager();
        UpgradeEngine? processor = null;

        if (originalCollection != null)
        {
            processor = new UpgradeEngineFactory()
                            .SetCollections(this, originalCollection, Project)
                            .SetUpgradeProperties(options.FromVersion, options.ToVersion, Project.Manufacturer, typeof(L5XDataTypes))
                            .Create();
        }

        if (processor != null)
        {
            UpgradeManager.ProcessUpgrade(processor, dbh, options, progress);
        }

        return;
    }

    public L5XDataType? Item(int index)
    {
        return (L5XDataType?)base[index];
    }

    public L5XDataType? Item(string id)
    {
        return (L5XDataType?)base[id];
    }
}
