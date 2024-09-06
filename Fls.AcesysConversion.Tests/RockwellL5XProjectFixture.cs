using Fls.AcesysConversion.PLC.Rockwell.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fls.AcesysConversion.Tests
{
    public class RockwellL5XProjectFixture
    {
        public RockwellL5XProject Project { get; private set; }

        public RockwellL5XProjectFixture()
        {
            // Initialize the RockwellL5XProject instance
            Project = new RockwellL5XProject();
        }        
    }
}
