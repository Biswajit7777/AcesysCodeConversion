using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.PLC.Rockwell.Components.AddOns;
using Fls.AcesysConversion.PLC.Rockwell.Components.DataTypes;
using Fls.AcesysConversion.PLC.Rockwell.Components.Programs;
using Fls.AcesysConversion.PLC.Rockwell.Components.Tags;
using Fls.AcesysConversion.PLC.Rockwell.Components.Tasks;
using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components;

public class L5XController : RockwellL5XItemBase
{
    public L5XDataTypes? DataTypes => (L5XDataTypes?)SelectSingleNode("DataTypes");
    private L5XDataTypes? OriginalDataTypes;
    public L5XAddOnInstructionDefinitions? AddOns => (L5XAddOnInstructionDefinitions?)SelectSingleNode("AddOnInstructionDefinitions");
    public L5XAddOnInstructionDefinitions? OriginalAddOns;
    public L5XTags? Tags => (L5XTags?)SelectSingleNode("Tags");
    private L5XTags? OriginalTags;
    public L5XPrograms? Programs => (L5XPrograms?)SelectSingleNode("Programs");
    private L5XPrograms? OriginalPrograms;

    public L5XTasks? Tasks => (L5XTasks?)SelectSingleNode("Tasks");
    private L5XTasks? OriginalTasks;


    public L5XController(string? prefix, string localName, string? namespaceURI, XmlDocument doc, int seq)
        : base(prefix, localName, namespaceURI, doc)
    {
        MessageboardReference = seq;
    }

    public override void UpgradeVersion(RockwellL5XProject originalProject, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        OriginalDataTypes = originalProject.Content?.Controller?.DataTypes;
        OriginalAddOns = originalProject.Content?.Controller?.AddOns;
        OriginalTags = originalProject.Content?.Controller?.Tags;
        OriginalPrograms = originalProject.Content?.Controller?.Programs;
        OriginalTasks = originalProject.Content?.Controller?.Tasks;

        //todo : check why these are null

        if (DataTypes != null)
        {
            progress.Report("Data Type Upgrade Started");
            DataTypes.UpgradeVersion(OriginalDataTypes, options, progress);
            progress.Report("Data Type Upgrade Completed");
        }

        if (AddOns != null)
        {
            progress.Report("Addon Upgrade Started");
            AddOns.UpgradeVersion(OriginalAddOns, options, progress);
            progress.Report("Addon Upgrade Completed");
        }

        if (Tags != null)
        {
            progress.Report("Tag Upgrade Started");
            Tags.UpgradeVersion(OriginalTags, options, progress);
            progress.Report("Tag Upgrade Completed");
        }

        if (Programs != null)
        {
            progress.Report("Program Upgrade Started");
            Programs.UpgradeVersion(OriginalPrograms, options, progress);
            progress.Report("Program Upgrade Completed");
        }

        if (Tasks != null)
        {
            progress.Report("Task Upgrade Started");
            Tasks.UpgradeVersion(OriginalTasks, options, progress);
            progress.Report("Task Upgrade Completed");
        }

    }
}






