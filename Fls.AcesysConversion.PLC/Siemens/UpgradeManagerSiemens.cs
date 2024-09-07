using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Helpers.Database;
using Fls.AcesysConversion.PLC.Rockwell;
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
        SiemensUpgradeOptions options, IProgress<string> progress)
        {
            if (dbHelper != null)
            {
                engine.ProcessOne2Many(dbHelper, options, progress);
                engine.ProcessMany2One(dbHelper, options, progress);
                engine.ProcessOne2One(dbHelper, options, progress);
                engine.ProcessRemoval(dbHelper, options, progress);
                engine.ProcessMandatory(dbHelper, options, progress);

            }

        }
    }
}
