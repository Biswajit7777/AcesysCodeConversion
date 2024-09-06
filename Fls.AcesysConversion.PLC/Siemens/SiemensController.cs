using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.PLC.Rockwell.Components;
using Fls.AcesysConversion.PLC.Siemens.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fls.AcesysConversion.PLC.Siemens
{
    public class SiemensController : SiemensItemBase
    {
        public SiemensController(string prefix, string localname, string nsURI) : base(prefix, localname, nsURI)
        {
        }

        public override string GenerateAwl()
        {
            throw new NotImplementedException();
        }
        
    }
}
