using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Helpers.Database;
using Fls.AcesysConversion.PLC.Rockwell.Components.Programs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.Tasks
{
    public class L5XTasks : L5XCollection
    {
        public RockwellL5XProject Project;

        public L5XTasks(string prefix, string localName, string namespaceURI, XmlDocument doc, int seq)
        : base(prefix, localName, namespaceURI, doc)
        {
            MessageboardReference = seq;
            Project = (RockwellL5XProject)OwnerDocument;
        }

        public override void UpgradeVersion(L5XCollection? original, RockwellUpgradeOptions options, IProgress<string> progress)
        {
            DbHelper? dbh = DbHelper.Instance.GetDbHelper((int)options.FromVersion, (int)options.ToVersion, Project.Manufacturer);
            L5XTasks? originalCollection = (L5XTasks?)original;
            UpgradeEngine? processor = null;

            if (originalCollection != null)
            {
                processor = new UpgradeEngineFactory()
                                .SetCollections(this, originalCollection, Project)
                                .SetUpgradeProperties(options.FromVersion, options.ToVersion, Project.Manufacturer, typeof(L5XTasks))
                                .Create();
            }

            if (processor != null)
            {
                UpgradeManager.ProcessUpgrade(processor, dbh, options, progress);
            }

            return;
        }

        public L5XTask? Item(int index)
        {
            return (L5XTask?)base[index];
        }

        public L5XTask? Item(string id)
        {
            return (L5XTask?)base[id];
        }
    }
}
