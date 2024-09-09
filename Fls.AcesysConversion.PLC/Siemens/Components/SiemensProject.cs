using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Common.Entities;
using Fls.AcesysConversion.Common.Enums;
using Fls.AcesysConversion.PLC.Rockwell.Components;
using Fls.AcesysConversion.PLC.Siemens.Components.DataBlock;
using Fls.AcesysConversion.PLC.Siemens.Components.FunctionBlocks_FB;
using Fls.AcesysConversion.PLC.Siemens.Components.SystemFunctionBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fls.AcesysConversion.PLC.Siemens.Components
{
    public class SiemensProject
    {
        private const string FlsUiIdentifier = "FLS_UI_IDENTIFIER";
        public readonly PlcManufacturer Manufacturer = PlcManufacturer.Siemens;
        private readonly List<IMessageBoardSubscriber> Subscribers = new();
        private int sequence = 0;
        public SiemensDataTypes DataTypes { get; set; }
        public SiemensFBs FunctionBlocks { get; set; }
        public SiemensSFBs SFBs { get; set; }

        // Store the loaded AWL content privately
        private string? loadedAwlContent;

        // Store the loaded SDF content privately
        private string? loadedSdfContent;

        public string? LoadedAwlContent
        {
            get => loadedAwlContent;
            private set => loadedAwlContent = value; // Keep the setter private
        }

        public string? LoadedSdfContent
        {
            get => loadedSdfContent;
            private set => loadedSdfContent = value; // Keep the setter private
        }

        public SiemensProject()
        {
        }

        // Load AWL file content
        public void LoadAwlFile(string awlContent)
        {
            if (!string.IsNullOrEmpty(awlContent))
            {
                LoadedAwlContent = awlContent;
                ProcessAwlContent(awlContent);
            }
            else
            {
                throw new FileNotFoundException("The specified AWL file does not exist.");
            }
        }

        // Load SDF file content
        public void LoadSdfFile(string sdfContent)
        {
            if (!string.IsNullOrEmpty(sdfContent))
            {
                LoadedSdfContent = sdfContent;
                ProcessSdfContent(sdfContent);
            }
            else
            {
                throw new FileNotFoundException("The specified SDF file does not exist.");
            }
        }

        // Method to extract the loaded AWL file content
        public string? ExtractAwlFile()
        {
            if (!string.IsNullOrEmpty(LoadedAwlContent))
            {
                return LoadedAwlContent;
            }
            else
            {
                throw new InvalidOperationException("No AWL file is loaded.");
            }
        }

        // Method to extract the loaded SDF file content
        public string? ExtractSdfFile()
        {
            if (!string.IsNullOrEmpty(LoadedSdfContent))
            {
                return LoadedSdfContent;
            }
            else
            {
                throw new InvalidOperationException("No SDF file is loaded.");
            }
        }

        private void ProcessAwlContent(string awlContent)
        {
            string[] awlLines = awlContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (string line in awlLines)
            {
                ParseAwlLine(line);
            }
        }

        private void ProcessSdfContent(string sdfContent)
        {
            // Implement the logic to process the SDF file content
            string[] sdfLines = sdfContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (string line in sdfLines)
            {
                ParseSdfLine(line);
            }
        }

        private void ParseAwlLine(string line)
        {
            if (line.StartsWith("A"))
            {
                // Parse logic for AWL
            }
            else if (line.StartsWith("O"))
            {
                // Parse logic for AWL
            }
        }

        private void ParseSdfLine(string line)
        {
            // Implement parsing logic for SDF lines
        }

        public int GetNewMessageboardReference()
        {
            sequence++;
            return sequence;
        }

        public void Attach(IMessageBoardSubscriber subscriber)
        {
            Subscribers.Add(subscriber);
        }

        public void Detach(IMessageBoardSubscriber subscriber)
        {
            _ = Subscribers.Remove(subscriber);
        }

        public void Announce(UserMessage um)
        {
            foreach (IMessageBoardSubscriber subscriber in Subscribers)
            {
                subscriber.AnnounceNewUserMessage(um);
            }
        }

        public void UpgradeVersion(SiemensProject project, SiemensUpgradeOptions options, IProgress<string> progress)
        {
            DataTypes = new SiemensDataTypes(null, null, null);

            if (DataTypes != null)
            {
                progress.Report("DataTypes Upgrade Started");
                DataTypes.UpgradeVersion(project, options, progress);
                progress.Report("DataTypes Upgrade Completed");
            }

            FunctionBlocks = new SiemensFBs(null, null, null);

            if (FunctionBlocks != null)
            {
                progress.Report("Function Blocks Upgrade Started");
                FunctionBlocks.UpgradeVersion(project, options, progress);
                progress.Report("Function Blocks Upgrade Completed");
            }

            SFBs = new SiemensSFBs(null, null, null);

            if (SFBs != null)
            {
                progress.Report("System Function Blocks Upgrade Started");
                SFBs.UpgradeVersion(project, options, progress);
                progress.Report("System Function Blocks Upgrade Completed");
            }




        }

        private SiemensDataTypes CreateSiemensDataBlocks(IEnumerable<string> dataBlocks)
        {
            // Create a new instance of SiemensDataBlocks
            var siemensDataBlocks = new SiemensDataTypes("prefix", "localname", "nsURI");

            // Process each data block and add it to the SiemensDataBlocks
            foreach (var block in dataBlocks)
            {
                siemensDataBlocks.AddByContent(block); // Assuming AddByContent is a method to add blocks
            }

            return siemensDataBlocks;
        }

        // Method to update the AWL content
        public void UpdateAwlContent(string awlContent)
        {
            LoadedAwlContent = awlContent;
        }

        // Method to update the SDF content
        public void UpdateSdfContent(string sdfContent)
        {
            LoadedSdfContent = sdfContent;
        }
    }
}