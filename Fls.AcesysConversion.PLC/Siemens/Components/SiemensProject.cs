using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Common.Entities;
using Fls.AcesysConversion.Common.Enums;
using Fls.AcesysConversion.PLC.Rockwell.Components;
using Fls.AcesysConversion.PLC.Siemens.Components.DataBlock;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public SiemensDataBlocks DataBlocks { get; set; }

        public SiemensProject()
        {

        }

        public void LoadAwlFile(string awlContent)
        {
            if (awlContent != null)
            {             
                ProcessAwlContent(awlContent);
            }
            else
            {
                throw new FileNotFoundException("The specified AWL file does not exist.");
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

        private void ParseAwlLine(string line)
        {            
            if (line.StartsWith("A")) 
            {
                
            }
            else if (line.StartsWith("O"))
            {
                
            }
            
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

        public void UpgradeVersion(string awlContent, SiemensUpgradeOptions options, IProgress<string> progress)
        {
            // Regex pattern to match everything between DATA_BLOCK and END_DATA_BLOCK
            string pattern = @"DATA_BLOCK[\s\S]*?END_DATA_BLOCK";

            // Find all matches
            var matches = Regex.Matches(awlContent, pattern);

            List<string> dataBlocks = new List<string>();

            foreach (Match match in matches)
            {
                dataBlocks.Add(match.Value);
            }
            DataBlocks = CreateSiemensDataBlocks(dataBlocks);

            if (DataBlocks != null) 
            {
                progress.Report("Data Block Upgrade Started");                
            }
        }

        private SiemensDataBlocks CreateSiemensDataBlocks(IEnumerable<string> dataBlocks)
        {
            // Create a new instance of SiemensDataBlocks
            var siemensDataBlocks = new SiemensDataBlocks("prefix", "localname", "nsURI");

            // Process each data block and add it to the SiemensDataBlocks
            foreach (var block in dataBlocks)
            {
                siemensDataBlocks.AddByContent(block); // Assuming AddByContent is a method to add blocks
            }

            return siemensDataBlocks;
        }
    }
}
