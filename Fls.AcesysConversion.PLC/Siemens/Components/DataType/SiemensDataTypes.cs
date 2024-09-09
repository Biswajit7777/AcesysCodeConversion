using System;
using System.Collections.Generic;
using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Helpers.Database;
using Fls.AcesysConversion.PLC.Rockwell.Components.DataTypes;
using Fls.AcesysConversion.PLC.Rockwell.Components;
using Fls.AcesysConversion.PLC.Rockwell;
using Fls.AcesysConversion.PLC.Siemens.Components;
using Fls.AcesysConversion.Common.Enums;

namespace Fls.AcesysConversion.PLC.Siemens.Components.DataBlock
{
    public class SiemensDataTypes : SiemensCollection
    {
        public SiemensProject Project = new();
        public SiemensDataTypes(string prefix, string localname, string nsURI)
            : base(prefix, localname, nsURI)
        {
            
        }

        // Constructor to initialize from a list of strings
        public SiemensDataTypes(string prefix, string localname, string nsURI, IEnumerable<string> dataBlocks)
            : base(prefix, localname, nsURI)
        {
            foreach (var block in dataBlocks)
            {
                // Add each block to the collection
                AddByContent(block);
            }
        }        

        public override void UpgradeVersion(SiemensProject Project, SiemensUpgradeOptions options, IProgress<string> progress)
        {
            DbHelper? dbh = DbHelper.Instance.GetDbHelper((int)options.FromVersion, (int)options.ToVersion, PlcManufacturer.Siemens);

            string awlContent = Project.ExtractAwlFile();


            _ = new UpgradeManagerSiemens();
            UpgradeEngineSiemens? processor = null;

            if (awlContent != null)
            {
                processor = new UpgradeEngineFactorySiemens()
                                .SetCollectionsSiemens(awlContent, awlContent, Project)
                                .SetUpgradeProperties(options.FromVersion, options.ToVersion, Project.Manufacturer, typeof(SiemensDataTypes))
                                .Create();
            }

            if (processor != null)
            {
                UpgradeManagerSiemens.ProcessUpgrade(processor, dbh, options, progress,Project);
            }
        }


        public override string GenerateAwl()
        {
            // Implement this method if needed, or provide a stub
            throw new NotImplementedException();
        }
    }
}
