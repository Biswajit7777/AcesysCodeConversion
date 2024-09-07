using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Helpers.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fls.AcesysConversion.PLC.Siemens.Components.DataBlock
{
    public class V7ToV8DataBlockUpgradeEngine : UpgradeEngineSiemens
    {
        private string Current { get; set; }
        private string Original { get; set; }
        private SiemensProject Project { get; set; }

        public V7ToV8DataBlockUpgradeEngine(string current, string original, SiemensProject project)
        {
            Current = current;
            Original = original;
            Project = project;
        }

        // Other methods remain unchanged
        public override void ProcessMandatory(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress)
        {
            throw new NotImplementedException();
        }

        public override void ProcessMany2One(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress)
        {
            throw new NotImplementedException();
        }

        public override void ProcessOne2Many(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress)
        {
            throw new NotImplementedException();
        }

        public override void ProcessOne2One(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress)
        {
            throw new NotImplementedException();
        }

        public override void ProcessRemoval(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress)
        {
            throw new NotImplementedException();
        }
    }
}
