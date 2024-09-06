using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fls.AcesysConversion.PLC.Siemens.Components.DataBlock
{
    public class SiemensDataBlock : SiemensItemBase
    {
        public bool IsUpgraded { get; set; } = false;
        public SiemensDataBlock(string prefix, string localName, string nsURI, string awlContent, int seq) : base(prefix, localName, nsURI, awlContent, seq)
        {
            MessageboardReference = seq;
        }

        public override string GenerateAwl()
        {
            throw new NotImplementedException();
        }
    }
}
