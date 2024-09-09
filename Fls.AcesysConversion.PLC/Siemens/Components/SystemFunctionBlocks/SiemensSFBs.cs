using Fls.AcesysConversion.Common.Enums;
using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Helpers.Database;
using Fls.AcesysConversion.PLC.Siemens.Components.DataBlock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fls.AcesysConversion.PLC.Siemens.Components.SystemFunctionBlocks
{
    public class SiemensSFBs : SiemensCollection
    {
        public SiemensSFBs(string prefix, string localname, string nsURI) : base(prefix, localname, nsURI)
        {
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
                                .SetUpgradeProperties(options.FromVersion, options.ToVersion, Project.Manufacturer, typeof(SiemensSFBs))
                                .Create();
            }

            if (processor != null)
            {
                UpgradeManagerSiemens.ProcessUpgrade(processor, dbh, options, progress, Project);
            }
        }

        public override string GenerateAwl()
        {
            throw new NotImplementedException();
        }
    }
}
