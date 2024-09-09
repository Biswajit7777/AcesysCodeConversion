using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Helpers.Database;
using Fls.AcesysConversion.PLC.Rockwell;
using Fls.AcesysConversion.PLC.Siemens.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fls.AcesysConversion.PLC.Siemens
{
    public class UpgradeManagerSiemens
    {
        public static void ProcessUpgrade(UpgradeEngineSiemens engine, DbHelper? dbHelper,
        SiemensUpgradeOptions options, IProgress<string> progress, SiemensProject project)
        {
            if (dbHelper != null)
            {
                engine.ProcessOne2Many(dbHelper, options, progress, project);
                engine.ProcessMany2One(dbHelper, options, progress,project);
                engine.ProcessOne2One(dbHelper, options, progress, project);
                engine.ProcessRemoval(dbHelper, options, progress, project);
                engine.ProcessMandatory(dbHelper, options, progress, project);

            }

        }
    }
}
