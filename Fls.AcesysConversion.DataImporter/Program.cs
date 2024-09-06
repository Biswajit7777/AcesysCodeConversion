using Fls.AcesysConversion.Helpers.Database;
using System.Text.RegularExpressions;
using System.Xml;

ImportStandardsIntoDatabase("ACESYSv8_Standards.xml");

Console.ReadLine();

static void ImportStandardsIntoDatabase(string path)
{
    if (File.Exists(path))
    {
        //Reading XML
        XmlDocument xmlDoc = new();
        xmlDoc.Load(path);
        //ImportDataTypes(xmlDoc);
        //ImportAddonInstructions(xmlDoc);
        //ImportFaceplateDecoratedData(xmlDoc);
        //ImportAddOnDecoratedData(xmlDoc);
        ImportHMITags(xmlDoc);
    }
}

internal partial class Program
{
    [GeneratedRegex("\\s")]
    private static partial Regex MyRegex();

    private static void ImportDataTypes(XmlDocument xmlDoc)
    {
        XmlNode? dataTypeNode = xmlDoc.SelectSingleNode("/ACESYSv8Standard/DataTypes");
        if (dataTypeNode != null)
        {
            foreach (XmlNode nodex in dataTypeNode.ChildNodes)
            {
                string xml = MyRegex().Replace(nodex.OuterXml, " ");
                string? nameAttribute = nodex?.Attributes?["Name"]?.Value;
                if (nameAttribute != null)
                {
                    DbHelper.UpdateMappingDetails(nameAttribute, xml);
                    Console.WriteLine($"Name = {nameAttribute}, Type = {nodex?.Name}");
                }
            }
        }
    }

    private static void ImportAddonInstructions(XmlDocument xmlDoc)
    {
        XmlNode? dataTypeNode = xmlDoc.SelectSingleNode("/ACESYSv8Standard/AddOnInstructionDefinitions");
        if (dataTypeNode != null)
        {
            foreach (XmlNode nodex in dataTypeNode.ChildNodes)
            {
                string xml = MyRegex().Replace(nodex.OuterXml, " ");
                string? nameAttribute = nodex?.Attributes?["Name"]?.Value;
                if (nameAttribute != null)
                {
                    DbHelper.UpdateMappingDetails(nameAttribute, xml);
                    Console.WriteLine($"Name = {nameAttribute}, Type = {nodex?.Name}");
                }
            }
        }
    }

    private static void ImportFaceplateDecoratedData(XmlDocument xmlDoc)
    {
        XmlNode? dataTypeNode = xmlDoc.SelectSingleNode("/ACESYSv8Standard/FaceplateDecoratedData");
        if (dataTypeNode != null)
        {
            foreach (XmlNode nodex in dataTypeNode.ChildNodes)
            {
                string xml = MyRegex().Replace(nodex.InnerXml, " ");
                string? decoratedDataName = nodex?.Name;
                if (decoratedDataName != null)
                {
                    DbHelper.UpdateMappingDetailsFacePlateDecoratedData(decoratedDataName, xml);
                    Console.WriteLine($"Name = {decoratedDataName}, Type = {nodex?.Name}");
                }
            }
        }
    }

    private static void ImportAddOnDecoratedData(XmlDocument xmlDoc)
    {
        XmlNode? dataTypeNode = xmlDoc.SelectSingleNode("/ACESYSv8Standard/AddOnDecoratedData");
        if (dataTypeNode != null)
        {
            foreach (XmlNode nodex in dataTypeNode.ChildNodes)
            {
                string xml = MyRegex().Replace(nodex.InnerXml, " ");
                string? decoratedDataName = nodex?.Name;
                if (decoratedDataName != null)
                {
                    DbHelper.UpdateMappingDetailsAddOnDecoratedData(decoratedDataName, xml);
                    Console.WriteLine($"Name = {decoratedDataName}, Type = {nodex?.Name}");
                }
            }
        }
    }

    private static void ImportHMITags(XmlDocument xmlDoc)
    {
        XmlNode? dataTypeNode = xmlDoc.SelectSingleNode("/ACESYSv8Standard/HMITags");
        if (dataTypeNode != null)
        {
            foreach (XmlNode nodex in dataTypeNode.ChildNodes)
            {
                string xml = MyRegex().Replace(nodex.OuterXml, " ");
                string? HmiTagName = nodex!.Attributes!["Name"]!.Value;
                if (HmiTagName != null)
                {
                    DbHelper.UpdateMappingDetailsHmiTags(HmiTagName, xml);
                    Console.WriteLine($"Name = {HmiTagName}, Type = {nodex?.Name}");
                }
            }
        }
    }

}

