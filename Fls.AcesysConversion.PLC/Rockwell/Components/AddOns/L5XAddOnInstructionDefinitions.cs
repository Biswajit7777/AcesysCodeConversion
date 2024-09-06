using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Helpers.Database;
using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.AddOns;

public class L5XAddOnInstructionDefinitions : L5XCollection
{
    public RockwellL5XProject Project;

    public L5XAddOnInstructionDefinitions(string prefix, string localName, string namespaceURI, XmlDocument doc, int seq)
        : base(prefix, localName, namespaceURI, doc)
    {
        MessageboardReference = seq;
        Project = (RockwellL5XProject)OwnerDocument;
    }

    public L5XAddOnInstructionDefinition? Item(int index)
    {
        return (L5XAddOnInstructionDefinition?)base[index];
    }

    public L5XAddOnInstructionDefinition? Item(string id)
    {
        return (L5XAddOnInstructionDefinition?)base[id];
    }

    public override void UpgradeVersion(L5XCollection? original, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        DbHelper? dbh = DbHelper.Instance.GetDbHelper((int)options.FromVersion, (int)options.ToVersion, Project.Manufacturer);
        L5XAddOnInstructionDefinitions? originalCollection = (L5XAddOnInstructionDefinitions?)original;
        _ = new UpgradeManager();
        UpgradeEngine? processor = null;

        if (originalCollection != null)
        {
            processor = new UpgradeEngineFactory()
                            .SetCollections(this, originalCollection, Project)
                            .SetUpgradeProperties(options.FromVersion, options.ToVersion, Project.Manufacturer, typeof(L5XAddOnInstructionDefinitions))
                            .Create();
        }

        if (processor != null)
        {
            UpgradeManager.ProcessUpgrade(processor, dbh, options, progress);
        }

        return;
    }
}
