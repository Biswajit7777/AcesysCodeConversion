using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Common.Enums;
using Fls.AcesysConversion.Helpers.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fls.AcesysConversion.PLC.Siemens.Components.FunctionBlocks_FB
{
    public class V7ToV8FBUpgradeEngine : UpgradeEngineSiemens
    {
        private string Current { get; set; }
        private string Original { get; set; }
        private SiemensProject Project { get; set; }

        public V7ToV8FBUpgradeEngine(string current, string original, SiemensProject project)
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
            
            string? awlContent = project.LoadedAwlContent;
            string? sdfContent = project.LoadedSdfContent;
           
            List<string> FunctionalBlocksToBeDeleted = dbHelper.FunctionalBlockListToBeDeleted();
            
            if (string.IsNullOrEmpty(awlContent) || string.IsNullOrEmpty(sdfContent) || FunctionalBlocksToBeDeleted == null || FunctionalBlocksToBeDeleted.Count == 0)
            {
                progress.Report("No AWL or SDF content or Functional Blocks to be deleted.");
                return;
            }
            
            foreach (string dataType in FunctionalBlocksToBeDeleted)
            {                
                string fbPattern = $@"\r?\n?FUNCTION_BLOCK\s+""{dataType}""\s*(.*?)\s*END_FUNCTION_BLOCK\s*\r?\n?";
                
                var awlRegex = new Regex(fbPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                Match awlMatch = awlRegex.Match(awlContent);

                if (awlMatch.Success)
                {                    
                    awlContent = awlRegex.Replace(awlContent, "\n\n"); 

                    SiemensCollection.AddUserMessage(dataType, null, UserMessageTypes.Information,
                                             $"Deleted Functiona Block {dataType}", "DEL", $"Functional Block {dataType} has been deleted from AWL Content");

                    // Optionally, log or report the deletion
                    progress.Report($"Deleted Functional \"{dataType}\" block from AWL content.");
                }
                else
                {
                    // If no match is found, report it
                    progress.Report($"TYPE \"{dataType}\" not found in AWL content.");
                }

                // Process SDF content: Loop through the list of data types to be deleted
                // Assuming sdfContent contains rows and you want to delete lines that match the dataType
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

            // Update the project's AWL content after deletion
            project.UpdateAwlContent(awlContent);

            // Update the project's SDF content after deletion
            project.UpdateSdfContent(sdfContent);
        }

        public override void ProcessRemoval(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress, SiemensProject project)
        {
            
        }
    }
}
