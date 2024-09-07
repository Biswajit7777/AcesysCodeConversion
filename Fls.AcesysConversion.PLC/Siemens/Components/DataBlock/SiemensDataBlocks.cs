using System;
using System.Collections.Generic;
using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Helpers.Database;
using Fls.AcesysConversion.PLC.Rockwell.Components.DataTypes;
using Fls.AcesysConversion.PLC.Rockwell.Components;
using Fls.AcesysConversion.PLC.Rockwell;
using Fls.AcesysConversion.PLC.Siemens.Components;

namespace Fls.AcesysConversion.PLC.Siemens.Components.DataBlock
{
    public class SiemensDataBlocks : SiemensCollection
    {
        public SiemensProject Project;
        public SiemensDataBlocks(string prefix, string localname, string nsURI)
            : base(prefix, localname, nsURI)
        {
            
        }

        // Constructor to initialize from a list of strings
        public SiemensDataBlocks(string prefix, string localname, string nsURI, IEnumerable<string> dataBlocks)
            : base(prefix, localname, nsURI)
        {
            foreach (var block in dataBlocks)
            {
                // Add each block to the collection
                AddByContent(block);
            }
        }

        public void UpgradeVersionSiemens(string awlContent, SiemensUpgradeOptions options, IProgress<string> progress, SiemensProject Project)
        {
            DbHelper? dbh = DbHelper.Instance.GetDbHelper((int)options.FromVersion, (int)options.ToVersion, Project.Manufacturer);
            
            _ = new UpgradeManager();
            UpgradeEngineSiemens? processor = null;

            if (awlContent != null)
            {
                processor = new UpgradeEngineFactorySiemens()
                                .SetCollectionsSiemens(awlContent,awlContent, Project)
                                .SetUpgradeProperties(options.FromVersion, options.ToVersion, Project.Manufacturer, typeof(L5XDataTypes))
                                .Create();
            }

            if (processor != null)
            {
                UpgradeManagerSiemens.ProcessUpgrade(processor, dbh, options, progress);
            }

            return;
        }


        public override string GenerateAwl()
        {
            // Implement this method if needed, or provide a stub
            throw new NotImplementedException();
        }
    }
}
