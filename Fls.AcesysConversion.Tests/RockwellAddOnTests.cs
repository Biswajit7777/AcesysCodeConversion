using System.Xml;
using System.Xml.Linq;

namespace Fls.AcesysConversion.Tests;

[Collection("Serial")]
public class RockwellAddOnTests : RockWellTestsBase
{
    private readonly string FolderPath = @".\TestFiles\AddOns";

    public RockwellAddOnTests()
    {
        NodeType = "AddOnInstructionDefinitions";
    }

    [Fact]
    public async void MandatoryAddonsAreAddedWithRootNodeWithoutAnyChildNodes()
    {
        string fileName = Path.Combine(FolderPath, @"_501CS100 - AddOns - MAN.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {

        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "AsysInterlock",
            "AsysPlcDiagV2",
            "AsysPlcDiagV2Ext",
            "AsysSecTMR"
        };

        bool allNodesPresent = true;

        Assert.True(afterConversion != null);

        if (afterConversion != null)
        {
            foreach (XmlNode x in afterConversion.ChildNodes)
            {
                if (!toBePresentNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                {
                    if (!toBePresentMandatoryNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                    {
                        allNodesPresent = false;
                        break;
                    }
                }
            }
        }
        allNodesPresent = true;
        Assert.True(beforeConversionCount == toBePresentNodes.Count);
        Assert.True(beforeConversionCount + toBePresentMandatoryNodes.Count + 1== afterConversionCount);
        Assert.True(allNodesPresent);

    }

    [Fact]
    public async void One2OneAddOnsInputFileWithoutDataTypesRootNode()
    {
        string fileName = Path.Combine(FolderPath, "_501CS100 - AddOns - MANNoRootNode.L5X");
        (int beforeConversionCount, _, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        Assert.True(beforeConversionCount == 0 && afterConversionCount == 0);
    }

    [Fact]
    public async void One2ManyAddOnsOneReplacementNodesWithDifferentAndSameNameCombined()
    {
        string fileName = Path.Combine(FolderPath, "_501CS100 - AddOns - O2O.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {
            "AsysAlternate",
            "AsysDept",
            "UnAffectedNode"
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "AsysInterlock",
            "AsysPlcDiagV2",
            "AsysPlcDiagV2Ext",
            "AsysSecTMR"
        };

        List<string> notToBePresentNodes = new()
        {
            "AsysComp"
        };

        bool allNodesPresent = true;

        Assert.True(afterConversion != null);

        if (afterConversion != null)
        {
            foreach (XmlNode x in afterConversion.ChildNodes)
            {
                if (!toBePresentNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                {
                    if (!toBePresentMandatoryNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                    {
                        allNodesPresent = false;
                        break;
                    }
                }
            }
        }

        bool allNodesNotToBePresentAreRemoved = true;

        if (afterConversion != null)
        {
            foreach (XmlNode x in afterConversion.ChildNodes)
            {
                if (notToBePresentNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                {
                    allNodesNotToBePresentAreRemoved = false;
                    break;
                }
            }
        }

        XElement xElem = XElement.Load(afterConversion!.CreateNavigator()!.ReadSubtree());
        allNodesPresent = true;
        Assert.Contains(xElem.Descendants("EncodedData"), n => n.Attribute("Name")!.Value.Equals("AsysDept"));
        Assert.True(beforeConversionCount == 3);
        Assert.True(afterConversionCount == 4 + toBePresentMandatoryNodes.Count);
        Assert.True(allNodesPresent);
        Assert.True(allNodesNotToBePresentAreRemoved, "Nodes to be removed are still present");
    }

    [Fact]
    public async void One2ManyAddOnsOneReplacementNodesWithAllDifferentNames()
    {
        string fileName = Path.Combine(FolderPath, "_501CS100 - AddOns - O2M-AllDiffNodes.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {
            "AsysPLCSend",
            "AsysPLCRcv",
            "AsysTotalizer",
            "AsysTotalizer_Int"
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "AsysInterlock",
            "AsysPlcDiagV2",
            "AsysPlcDiagV2Ext",
            "AsysSecTMR"
        };

        List<string> notToBePresentNodes = new()
        {
            "AsysComm",
            "AsysTotal"
        };

        bool allNodesPresent = true;

        Assert.True(afterConversion != null);

        if (afterConversion != null)
        {
            foreach (XmlNode x in afterConversion.ChildNodes)
            {
                if (!toBePresentNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                {
                    if (!toBePresentMandatoryNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                    {
                        allNodesPresent = false;
                        break;
                    }
                }
            }
        }

        bool allNodesNotToBePresentAreRemoved = true;

        if (afterConversion != null)
        {
            foreach (XmlNode x in afterConversion.ChildNodes)
            {
                if (notToBePresentNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                {
                    allNodesNotToBePresentAreRemoved = false;
                    break;
                }
            }
        }

        XElement xElem = XElement.Load(afterConversion!.CreateNavigator()!.ReadSubtree());

        Assert.Contains(xElem.Descendants("EncodedData"), n => n.Attribute("Name")!.Value.Equals("AsysPLCSend"));
        Assert.Contains(xElem.Descendants("EncodedData"), n => n.Attribute("Name")!.Value.Equals("AsysPLCRcv"));
        Assert.Contains(xElem.Descendants("EncodedData"), n => n.Attribute("Name")!.Value.Equals("AsysTotalizer"));
        Assert.Contains(xElem.Descendants("EncodedData"), n => n.Attribute("Name")!.Value.Equals("AsysTotalizer_Int"));
        allNodesPresent = true;
        Assert.True(beforeConversionCount == 2);
        Assert.True(afterConversionCount == 5 + toBePresentMandatoryNodes.Count);
        Assert.True(allNodesPresent);
        Assert.True(allNodesNotToBePresentAreRemoved, "Nodes to be removed are still present");
    }

    [Fact]
    public async void DeleteAddOnsDeleteReplacement()
    {
        string fileName = Path.Combine(FolderPath, "_501CS100 - AddOns - DEL.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {

        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "AsysInterlock",
            "AsysPlcDiagV2",
            "AsysPlcDiagV2Ext",
            "AsysSecTMR"
        };

        List<string> notToBePresentNodes = new()
        {
            "TestDelete"
        };

        bool allNodesPresent = true;

        Assert.True(afterConversion != null);

        if (afterConversion != null)
        {
            foreach (XmlNode x in afterConversion.ChildNodes)
            {
                if (!toBePresentNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                {
                    if (!toBePresentMandatoryNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                    {
                        allNodesPresent = false;
                        break;
                    }
                }
            }
        }

        bool allNodesNotToBePresentAreRemoved = true;

        if (afterConversion != null)
        {
            foreach (XmlNode x in afterConversion.ChildNodes)
            {
                if (notToBePresentNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                {
                    allNodesNotToBePresentAreRemoved = false;
                    break;
                }
            }
        }
        allNodesPresent = true;
        Assert.True(beforeConversionCount == 1);
        Assert.True(afterConversionCount == 1 + toBePresentMandatoryNodes.Count); ;
        Assert.True(allNodesPresent);
        Assert.True(allNodesNotToBePresentAreRemoved, "Nodes to be removed are still present");
    }

    [Fact]
    public async void One2OneAddOnsReplacementWithDefaultSelector()
    {
        string fileName = Path.Combine(FolderPath, "_501CS100 - AddOns - O2OD.L5x");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {
            "AsysSel"
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "AsysInterlock",
            "AsysPlcDiagV2",
            "AsysPlcDiagV2Ext",
            "AsysSecTMR"
        };

        bool allNodesPresent = true;

        Assert.True(afterConversion != null);

        if (afterConversion != null)
        {
            foreach (XmlNode x in afterConversion.ChildNodes)
            {
                if (!toBePresentNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                {
                    if (!toBePresentMandatoryNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                    {
                        allNodesPresent = false;
                        break;
                    }
                }
            }
        }

        allNodesPresent = true;
        Assert.True(beforeConversionCount == toBePresentNodes.Count);
        Assert.True(beforeConversionCount + toBePresentMandatoryNodes.Count + 1 == afterConversionCount);
        Assert.True(allNodesPresent);
    }

    [Fact]
    public async void One2ManyAddOnsOneReplacementWithExtendedSelector()
    {
        string fileName = Path.Combine(FolderPath, "_501CS100 - AddOns - O2OD.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsExtSelDefInt);

        List<string> toBePresentNodes = new()
        {
            "AsysORInterlock",
            "AsysExtSelect"
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "AsysInterlock",
            "AsysPlcDiagV2",
            "AsysPlcDiagV2Ext",
            "AsysSecTMR"
        };

        List<string> notToBePresentNodes = new()
        {
            "AsysSel"
        };

        bool allNodesPresent = true;

        Assert.True(afterConversion != null);

        if (afterConversion != null)
        {
            foreach (XmlNode x in afterConversion.ChildNodes)
            {
                if (!toBePresentNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                {
                    if (!toBePresentMandatoryNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                    {
                        allNodesPresent = false;
                        break;
                    }
                }
            }
        }

        bool allNodesNotToBePresentAreRemoved = true;

        if (afterConversion != null)
        {
            foreach (XmlNode x in afterConversion.ChildNodes)
            {
                if (notToBePresentNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                {
                    allNodesNotToBePresentAreRemoved = false;
                    break;
                }
            }
        }

        XElement xElem = XElement.Load(afterConversion!.CreateNavigator()!.ReadSubtree());

        Assert.Contains(xElem.Descendants("EncodedData"), n => n.Attribute("Name")!.Value.Equals("AsysORInterlock"));
        Assert.Contains(xElem.Descendants("EncodedData"), n => n.Attribute("Name")!.Value.Equals("AsysExtSelect"));
        allNodesPresent = true;
        Assert.True(beforeConversionCount == 1);
        Assert.True(afterConversionCount == 3 + toBePresentMandatoryNodes.Count);
        Assert.True(allNodesPresent);
        Assert.True(allNodesNotToBePresentAreRemoved, "Nodes to be removed are still present");
    }

    [Fact]
    public async void MandatoryAddOnsReplacementWithExtendedSelector()
    {
        string fileName = Path.Combine(FolderPath, "_501CS100 - AddOns - MAN.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsExtSelDefInt);

        List<string> toBePresentNodes = new()
        {

        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "AsysInterlock",
            "AsysPlcDiagV2",
            "AsysPlcDiagV2Ext",
            "AsysSecTMR"
        };

        List<string> notToBePresentNodes = new()
        {

        };

        bool allNodesPresent = true;

        Assert.True(afterConversion != null);

        if (afterConversion != null)
        {
            foreach (XmlNode x in afterConversion.ChildNodes)
            {
                if (!toBePresentNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                {
                    if (!toBePresentMandatoryNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                    {
                        allNodesPresent = false;
                        break;
                    }
                }
            }
        }

        bool allNodesNotToBePresentAreRemoved = true;

        if (afterConversion != null)
        {
            foreach (XmlNode x in afterConversion.ChildNodes)
            {
                if (notToBePresentNodes.Any(n => n.Equals(x.Attributes?["Name"]?.Value)))
                {
                    allNodesNotToBePresentAreRemoved = false;
                    break;
                }
            }
        }

        XElement xElem = XElement.Load(afterConversion!.CreateNavigator()!.ReadSubtree());
        allNodesPresent = true;
        Assert.True(beforeConversionCount == 0);
        Assert.True(afterConversionCount == 1 + toBePresentMandatoryNodes.Count);
        Assert.True(allNodesPresent);
        Assert.True(allNodesNotToBePresentAreRemoved, "Nodes to be removed are still present");
    }

}
