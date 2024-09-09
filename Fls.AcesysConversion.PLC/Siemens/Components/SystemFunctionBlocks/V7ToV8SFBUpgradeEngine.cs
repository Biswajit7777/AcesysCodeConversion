using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Helpers.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fls.AcesysConversion.PLC.Siemens.Components.SystemFunctionBlocks
{
    public class V7ToV8SFBUpgradeEngine : UpgradeEngineSiemens
    {
        private string Current { get; set; }
        private string Original { get; set; }
        private SiemensProject Project { get; set; }

        public V7ToV8SFBUpgradeEngine(string current, string original, SiemensProject project)
        {
            Current = current;
            Original = original;
            Project = project;
        }
        public override void ProcessMandatory(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress, SiemensProject project)
        {
            
        }

        public override void ProcessMany2One(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress, SiemensProject project)
        {
            
        }

        public override void ProcessOne2Many(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress, SiemensProject project)
        {
            
        }

        public override void ProcessOne2One(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress, SiemensProject project)
        {
            string? sdfContent = project.LoadedSdfContent;

            List<string> SFBToBeDeleted = dbHelper.SFBListToBeDeleted();

            if (string.IsNullOrEmpty(sdfContent))
            {
                progress.Report("No AWL or SDF content or Functional Blocks to be deleted.");
                return;
            }

            foreach (string dataType in SFBToBeDeleted)
            {
                string sdfPattern = $@"^.*\b{dataType}\b.*$\r?\n?";

                // Try to find a match in the SDF content
                var sdfRegex = new Regex(sdfPattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
                Match sdfMatch = sdfRegex.Match(sdfContent);

                if (sdfMatch.Success)
                {
                    // If a match is found, delete the entire line (row) containing the dataType
                    sdfContent = sdfRegex.Replace(sdfContent, ""); // Remove the matching lines

                    // Optionally, log or report the deletion
                    progress.Report($"Deleted row containing \"{dataType}\" from SDF content.");
                }
                else
                {
                    // If no match is found, report it
                    progress.Report($"Row containing \"{dataType}\" not found in SDF content.");
                }
            }

            project.UpdateSdfContent(sdfContent);

        }

        public override void ProcessRemoval(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress, SiemensProject project)
        {
            
        }
    }
}
