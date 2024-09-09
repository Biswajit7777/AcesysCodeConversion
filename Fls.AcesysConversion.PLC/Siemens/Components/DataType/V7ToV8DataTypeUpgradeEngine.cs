﻿using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Common.Enums;
using Fls.AcesysConversion.Helpers.Database;
using Fls.AcesysConversion.PLC.Rockwell.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fls.AcesysConversion.PLC.Siemens.Components.DataBlock
{
    public class V7ToV8DataTypeUpgradeEngine : UpgradeEngineSiemens
    {
        private string Current { get; set; }
        private string Original { get; set; }
        private SiemensProject Project { get; set; }

        public V7ToV8DataTypeUpgradeEngine(string current, string original, SiemensProject project)
        {
            Current = current;
            Original = original;
            Project = project;
        }        

        public override void ProcessOne2Many(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress, SiemensProject project)
        {
            
        }

        public override void ProcessOne2One(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress, SiemensProject project)
        {
            // Get the loaded AWL and SDF content from the project
            string? awlContent = project.LoadedAwlContent;
            string? sdfContent = project.LoadedSdfContent;

            // Get the list of data types to be deleted from the database
            List<string> dataTypeToBeDeleted = dbHelper.DataTypeListToBeDeleted();

            // If no AWL content or DataTypeToBeDeleted, log the issue and exit
            if (string.IsNullOrEmpty(awlContent) || string.IsNullOrEmpty(sdfContent) || dataTypeToBeDeleted == null || dataTypeToBeDeleted.Count == 0)
            {
                progress.Report("No AWL or SDF content or Data Types to be deleted.");
                return;
            }

            // Process AWL content: Loop through the list of data types to be deleted
            foreach (string dataType in dataTypeToBeDeleted)
            {
                // Create a regex pattern to match the specific TYPE block including the surrounding newlines in AWL content
                string typePattern = $@"\r?\n?TYPE\s+""{dataType}""\s*(.*?)\s*END_TYPE\s*\r?\n?";

                // Try to find a match in the AWL content
                var awlRegex = new Regex(typePattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                Match awlMatch = awlRegex.Match(awlContent);

                if (awlMatch.Success)
                {
                    // If a match is found, delete the entire block and leave exactly two newlines in place of it
                    awlContent = awlRegex.Replace(awlContent, "\n\n"); // Ensure two newlines replace the block

                    SiemensCollection.AddUserMessage(dataType, null, UserMessageTypes.Information,
                                             $"Deleted TYPE Block {dataType}", "DEL", $"TYPE Block {dataType} has been deleted from AWL Content");

                    // Optionally, log or report the deletion
                    progress.Report($"Deleted TYPE \"{dataType}\" block from AWL content.");
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
        public override void ProcessMany2One(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress, SiemensProject project)
        {
            
        }

        public override void ProcessRemoval(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress, SiemensProject project)
        {
            
        }

        public override void ProcessMandatory(DbHelper dbHelper, SiemensUpgradeOptions options, IProgress<string> progress, SiemensProject project)
        {
            
        }
    }
}
