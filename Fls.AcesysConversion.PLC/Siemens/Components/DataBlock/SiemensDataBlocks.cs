using System;
using System.Collections.Generic;
using Fls.AcesysConversion.PLC.Siemens.Components;

namespace Fls.AcesysConversion.PLC.Siemens.Components.DataBlock
{
    public class SiemensDataBlocks : SiemensCollection
    {
        public SiemensDataBlocks(string prefix, string localname, string nsURI)
            : base(prefix, localname, nsURI)
        {
        }

        // Constructor to initialize from a list of strings
        public SiemensDataBlocks(string prefix, string localname, string nsURI, IEnumerable<string> dataBlocks)
            : base(prefix, localname, nsURI)
        {
            foreach (var block in dataBlocks)
            {
                // Add each block to the collection
                AddByContent(block);
            }
        }

        public override string GenerateAwl()
        {
            // Implement this method if needed, or provide a stub
            throw new NotImplementedException();
        }
    }
}
