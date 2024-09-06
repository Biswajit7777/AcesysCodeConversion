using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Common.DTOs;
using Fls.AcesysConversion.Helpers.Database;
using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.DataTypes;

public class V7ToV8DataTypesUpgradeEngine : UpgradeEngine
{
    public L5XDataTypes DataTypes;
    public L5XDataTypes OriginalDataTypes;
    public RockwellL5XProject Project;

    public V7ToV8DataTypesUpgradeEngine(L5XCollection collection, L5XCollection originalCollection, RockwellL5XProject proj)
    {
        DataTypes = (L5XDataTypes)collection;
        OriginalDataTypes = (L5XDataTypes)originalCollection;
        Project = proj;
    }

    public override void ProcessMandatory(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        Dictionary<string, string> mandatory = dbHelper.GetMandatoryDataTypes(options);

        foreach (KeyValuePair<string, string> m in mandatory)
        {
            _ = Project.Content?.Controller?.DataTypes?.Remove(m.Key);
            _ = Project.Content?.Controller?.DataTypes?.Add(m.Key, m.Value, "MAN", null);
        }
    }

    public override void ProcessMany2One(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        List<Dto> many2OneDataTypes = dbHelper.GetManyToOneDataTypes(options);

        if (OriginalDataTypes != null)
        {
            foreach (XmlElement item in OriginalDataTypes)
            {
                string dataTypeName = item.GetAttribute("Name");
                IEnumerable<Dto> filteredM2OPresent = many2OneDataTypes.Where(i => i.FromObject == dataTypeName);
                IEnumerable<Dto> filteredM2ONotPresent = many2OneDataTypes.Where(i => i.FromObject != dataTypeName && i.ToObject == dataTypeName);

                foreach (Dto? dt in filteredM2OPresent)
                {
                    _ = (Project.Content?.Controller?.DataTypes?.Remove(dt.ToObject));
                    if (dt.XmlStandard.ToUpper() != "X")
                    {
                        _ = (Project.Content?.Controller?.DataTypes?.Add(dt.ToObject, dt.XmlStandard, "M2O", item));
                    }
                }

                foreach (Dto? dt in filteredM2ONotPresent)
                {
                    if (dt.XmlStandard.ToUpper() == "X")
                    {
                        _ = (Project.Content?.Controller?.DataTypes?.Remove(dt.ToObject));
                        Dto? current = many2OneDataTypes.Where(i => i.ToObject == dataTypeName).FirstOrDefault();
                        string cur = current?.FromObject ?? "";
                        Dto? parent = many2OneDataTypes.Where(i => i.ToObject == cur).FirstOrDefault(); ;
                        if (parent != null)
                        {
                            _ = (Project.Content?.Controller?.DataTypes?.Remove(parent.FromObject));
                            _ = (Project.Content?.Controller?.DataTypes?.Add(parent.FromObject, parent.XmlStandard, "M2O", item));
                        }
                    }
                }
            }
        }
    }

    public override void ProcessOne2Many(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        List<Dto> one2many = dbHelper.GetOneToManyDataTypes(options);

        if (OriginalDataTypes != null)
        {
            foreach (XmlElement item in OriginalDataTypes)
            {
                string dataTypeName = item.GetAttribute("Name");
                IEnumerable<Dto> filteredO2O = one2many.Where(i => i.FromObject == dataTypeName);

                if (filteredO2O.Any())
                {
                    bool sameNamePresent = filteredO2O.Where(_ => _.FromObject == _.ToObject).Any();

                    if (!sameNamePresent)
                    {
                        _ = (Project.Content?.Controller?.DataTypes?.Remove(dataTypeName));
                    }

                    foreach (Dto? dt in filteredO2O)
                    {
                        _ = (Project.Content?.Controller?.DataTypes?.Remove(dt.ToObject));
                        _ = (Project.Content?.Controller?.DataTypes?.Add(dt.ToObject, dt.XmlStandard, "O2M", item));
                    }
                }
            }
        }
    }

    public override void ProcessOne2One(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        List<Dto> one2one = dbHelper.GetOneToOneDataTypes(options);
        if (OriginalDataTypes != null)
        {
            foreach (XmlElement item in OriginalDataTypes)
            {
                string dataType = item.GetAttribute("Name");
                Dto? filteredO2O = one2one.Where(i => i.FromObject == dataType).FirstOrDefault();
                if (filteredO2O != null)
                {
                    _ = (Project.Content?.Controller?.DataTypes?.Remove(filteredO2O.FromObject));
                    _ = (Project.Content?.Controller?.DataTypes?.Add(filteredO2O.ToObject, filteredO2O.XmlStandard, "O2O", item));
                }
            }
        }
    }

    public override void ProcessRemoval(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        List<string> toBeDeleted = dbHelper.GetToBeDeletedDataTypes();

        foreach (string tbd in toBeDeleted)
        {
            _ = Project.Content?.Controller?.DataTypes?.Remove(tbd, true, "DEL");
        }
    }
}