using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Common.DTOs;
using Fls.AcesysConversion.Helpers.Database;
using System.Text.RegularExpressions;
using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.AddOns;

public class V7ToV8AddOnUpgradeEngine : UpgradeEngine
{
    public L5XAddOnInstructionDefinitions AddOns;
    public L5XAddOnInstructionDefinitions OriginalAddOns;
    public RockwellL5XProject Project;

    public V7ToV8AddOnUpgradeEngine(L5XCollection collection, L5XCollection originalCollection, RockwellL5XProject proj)
    {
        AddOns = (L5XAddOnInstructionDefinitions)collection;
        OriginalAddOns = (L5XAddOnInstructionDefinitions)originalCollection;
        Project = proj;
    }

    public override void ProcessMandatory(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        //Dictionary<string, string> mandatory = dbHelper.GetMandatoryAddOnInstructionDefinitions(options);

        //foreach (KeyValuePair<string, string> m in mandatory)
        //{
        //    _ = Project.Content?.Controller?.AddOns?.Remove(m.Key);
        //    _ = Project.Content?.Controller?.AddOns?.Add(m.Key, m.Value, "MAN", null);
        //}
    }

    public override void ProcessMany2One(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        List<Dto> many2OneAddOns = dbHelper.GetManyToOneAddOnInstructionDefinitions(options);

        if (OriginalAddOns != null)
        {
            foreach (XmlElement item in OriginalAddOns)
            {
                string addOnName = item.GetAttribute("Name");
                IEnumerable<Dto> filteredM2OPresent = many2OneAddOns.Where(i => i.FromObject == addOnName);
                IEnumerable<Dto> filteredM2ONotPresent = many2OneAddOns.Where(i => i.FromObject != addOnName && i.ToObject == addOnName);

                foreach (Dto? dt in filteredM2OPresent)
                {
                    _ = (Project.Content?.Controller?.AddOns?.Remove(dt.ToObject));
                    if (dt.XmlStandard.ToUpper() != "X")
                    {
                        _ = (Project.Content?.Controller?.AddOns?.Add(dt.ToObject, dt.XmlStandard, "M2O", item));
                    }
                }

                foreach (Dto? dt in filteredM2ONotPresent)
                {
                    if (dt.XmlStandard.ToUpper() == "X")
                    {
                        _ = (Project.Content?.Controller?.AddOns?.Remove(dt.ToObject));
                        Dto? current = many2OneAddOns.Where(i => i.ToObject == addOnName).FirstOrDefault();
                        string cur = current?.FromObject ?? "";
                        Dto? parent = many2OneAddOns.Where(i => i.ToObject == cur).FirstOrDefault(); ;
                        if (parent != null)
                        {
                            _ = (Project.Content?.Controller?.AddOns?.Remove(parent.FromObject));
                            _ = (Project.Content?.Controller?.AddOns?.Add(parent.FromObject, parent.XmlStandard, "M2O", item));
                        }
                    }
                }
            }
        }
    }

    public override void ProcessOne2Many(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        Dictionary<string, string> mandatory = dbHelper.GetMandatoryAddOnInstructionDefinitions(options);

        foreach (KeyValuePair<string, string> m in mandatory)
        {
            _ = Project.Content?.Controller?.AddOns?.Remove(m.Key);
            _ = Project.Content?.Controller?.AddOns?.Add(m.Key, m.Value, "MAN", null);
        }

        List<Dto> one2many = dbHelper.GetOneToManyAddOnInstructionDefinitions(options);

        if (OriginalAddOns != null)
        {
            foreach (XmlElement item in OriginalAddOns)
            {
                string addOnName = item.GetAttribute("Name");
                IEnumerable<Dto> filteredO2O = one2many.Where(i => i.FromObject == addOnName);

                if (filteredO2O.Any())
                {
                    bool sameNamePresent = filteredO2O.Where(_ => _.FromObject == _.ToObject).Any();

                    if (!sameNamePresent)
                    {
                        _ = (Project.Content?.Controller?.AddOns?.Remove(addOnName));
                    }

                    foreach (Dto? dt in filteredO2O.Reverse())
                    {
                        _ = (Project.Content?.Controller?.AddOns?.Remove(dt.ToObject));
                        _ = (Project.Content?.Controller?.AddOns?.Add(dt.ToObject, dt.XmlStandard, "O2M", item));
                    }
                }
            }
        }
    }

    public override void ProcessOne2One(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        List<Dto> one2one = dbHelper.GetOneToOneAddOnInstructionDefinitions(options);
        string extractedValue = string.Empty;

        if (OriginalAddOns != null)
        {
            foreach (XmlElement item in OriginalAddOns)
            {
                string addOn = item.GetAttribute("Name");

                // Regex to capture the part between "ADPT" and "_GATE" or "_MOTOR"
                var regex = new Regex(@"^ADPT(?<value>.*?)_(?<suffix>GATE|MOTOR)$");
                var match = regex.Match(addOn);

                if (match.Success)
                {
                    // Extract the value between "ADPT" and the suffix "_GATE" or "_MOTOR"
                    extractedValue = match.Groups["value"].Value;
                    string suffix = match.Groups["suffix"].Value;

                    // Construct the new AddOn name based on the extracted suffix (_GATE or _MOTOR)
                    addOn = $"ADPTxxxx_{suffix}";  // Use fixed "xxxx" with the correct suffix
                    item.SetAttribute("Name", addOn);
                }

                // Look for the corresponding Dto based on the updated AddOn name
                Dto? filteredO2O = one2one.Where(i => i.FromObject == addOn).FirstOrDefault();

                if (filteredO2O != null)
                {
                    // Replace the 'xxxx' placeholder with the extractedValue in the XmlStandard
                    string updatedXmlStandard = filteredO2O.XmlStandard.Replace("xxxx", extractedValue);

                    // Remove the old AddOn
                    _ = Project.Content?.Controller?.AddOns?.Remove(filteredO2O.FromObject);

                    // Add the new AddOn with the updated XML content and the modified addOn name
                    _ = Project.Content?.Controller?.AddOns?.Add(filteredO2O.ToObject, updatedXmlStandard, "O2O", item);
                }
            }
        }
    }

    public override void ProcessRemoval(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        List<string> toBeDeleted = dbHelper.GetToBeDeletedAddOnInstructionDefinitions();

        foreach (string tbd in toBeDeleted)
        {
            _ = Project.Content?.Controller?.AddOns?.Remove(tbd, true, "DEL");
        }
    }
}