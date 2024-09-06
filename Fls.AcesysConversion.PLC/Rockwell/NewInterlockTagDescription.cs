using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fls.AcesysConversion.PLC.Rockwell
{
    public class NewInterlockTagDescription
    {
        public string TagName { get; set; }
        public string Description { get; set; }

        public NewInterlockTagDescription(string tagName, string description)
        {
            TagName = tagName;
            Description = description;
        }
    }
}
