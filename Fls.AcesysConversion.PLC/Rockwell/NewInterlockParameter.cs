using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fls.AcesysConversion.PLC.Rockwell
{
    public class NewInterlockParameter
    {
        public string Name { get; set; }
        public string LINK { get; set; }
        public string HMI_Interlock { get; set; }

        public NewInterlockParameter(string name, string lINK, string Hmi_Interlock)
        {
            Name = name;
            LINK = lINK;
            HMI_Interlock = Hmi_Interlock;
        }
    }
}
