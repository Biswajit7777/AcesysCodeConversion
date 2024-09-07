using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Helpers.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fls.AcesysConversion.PLC.Siemens
{
    public abstract class UpgradeEngineSiemens
    {
        public abstract void ProcessOne2Many(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress);
        public abstract void ProcessOne2One(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress);
        public abstract void ProcessMany2One(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress);
        public abstract void ProcessRemoval(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress);
        public abstract void ProcessMandatory(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress);
    }
}
