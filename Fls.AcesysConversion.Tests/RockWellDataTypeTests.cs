using System.Xml;
using System.Xml.Linq;

namespace Fls.AcesysConversion.Tests;

[Collection("Serial")]
public class RockWellDataTypeTests : RockWellTestsBase
{
    private readonly string folderPath = @".\TestFiles\DataTypes";
    public RockWellDataTypeTests()
    {
        NodeType = "DataTypes";
    }

    [Fact]
    public async void MandatoryDatatypesAreAddedWithRootNodeWithoutAnyChildNodes()
    {
        string fileName = Path.Combine(folderPath, @"_501CS100 - DATATYPE-MAN.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        string? mandatoryNodeName = afterConversion?.FirstChild?.Attributes?["Name"]?.Value;
        Assert.True(beforeConversionCount == 0);
        Assert.True(afterConversionCount == 1);
        Assert.True(mandatoryNodeName?.Equals("ACESYS_FACEPLATE_PLCDIAGV2"));
    }

    [Fact]
    public async void One2OneDatatypesReplacementWithSameName()
    {
        string fileName = Path.Combine(folderPath, @"_501CS100 - DATATYPE-O2O.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {
            "ACESYS_CMD",
            "ACESYS_DEPT_CMD",
            "ACESYS_CMD_unaffected"
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "ACESYS_FACEPLATE_PLCDIAGV2"
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

        Assert.True(beforeConversionCount == toBePresentNodes.Count);
        Assert.True(beforeConversionCount + toBePresentMandatoryNodes.Count == afterConversionCount);
        Assert.True(allNodesPresent);
    }

    [Fact]
    public async void One2OneDatatypesReplacementWithDifferentName()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - DATATYPE-O2O-Diff.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {
            "ACESYS_ECS_STATUS"
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "ACESYS_FACEPLATE_PLCDIAGV2"
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

        Assert.True(beforeConversionCount == toBePresentNodes.Count);
        Assert.True(beforeConversionCount + toBePresentMandatoryNodes.Count == afterConversionCount);
        Assert.True(allNodesPresent);
    }

    [Fact]
    public async void One2OneDatatypesReplacementWithSameAsWellAsDifferentName()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - DATATYPE-O2O-DiffPlusSame.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {
            "ACESYS_ECS_STATUS",
            "ACESYS_CMD"
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "ACESYS_FACEPLATE_PLCDIAGV2"
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

        Assert.True(beforeConversionCount == toBePresentNodes.Count);
        Assert.True(beforeConversionCount + toBePresentMandatoryNodes.Count == afterConversionCount);
        Assert.True(allNodesPresent);
    }

    [Fact]
    public async void One2OneDatatypesReplacementNodesNotPresentInMappingAreLeftUnaffected()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - DATATYPE-O2O.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsExtSelDefInt);

        List<string> toBePresentNodes = new()
        {
            "ACESYS_CMD",
            "ACESYS_DEPT_CMD",
            "ACESYS_CMD_unaffected"
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "ACESYS_FACEPLATE_PLCDIAGV2"
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

        XElement xElem = XElement.Load(afterConversion!.CreateNavigator()!.ReadSubtree());

        Assert.Contains(xElem.Descendants("DataType"), n => n!.Attribute("Name")!.Value.Equals("ACESYS_CMD_unaffected"));
        Assert.True(beforeConversionCount == toBePresentNodes.Count);
        Assert.True(beforeConversionCount + toBePresentMandatoryNodes.Count == afterConversionCount);
        Assert.True(allNodesPresent);
    }

    [Fact]
    public async void One2OneDatatypesInputFileWithoutDataTypesRootNode()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - DATATYPE-MANNoRootNode.L5X");
        (int beforeConversionCount, _, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        Assert.True(beforeConversionCount == 0 && afterConversionCount == 0);
    }

    [Fact]
    public async void One2ManyDatatypesReplacementWithAllDifferentNames()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - DATATYPE-O2M-ALL_DIFF_NODES.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {
            "TestDiffNameReplacement1",
            "TestDiffNameReplacement2",
            "TestDiffNameReplacement21",
            "TestDiffNameReplacement22"
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "ACESYS_FACEPLATE_PLCDIAGV2"
        };

        List<string> notToBePresentNodes = new()
        {
            "TestO2M_AllDiffName"
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

        Assert.True(beforeConversionCount == 2);
        Assert.True(afterConversionCount == 4 + toBePresentMandatoryNodes.Count);
        Assert.True(allNodesPresent);
        Assert.True(allNodesNotToBePresentAreRemoved, "Nodes to be removed are still present");
    }

    [Fact]
    public async void One2ManyDatatypesOneReplacementNodesHasSameName()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - DATATYPE-O2M-ONE_SAME_NODE.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {
            "ACESYS_PLCtoPLC",
            "ACESYS_FACEPLATE_RECIEVE"
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "ACESYS_FACEPLATE_PLCDIAGV2"
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
        XElement xElem = XElement.Load(afterConversion!.CreateNavigator()!.ReadSubtree());

        Assert.Contains(xElem.Descendants("DataType"), n => n.Attribute("Name")!.Value.Equals("ACESYS_PLCtoPLC"));
        Assert.True(beforeConversionCount == 1);
        Assert.True(afterConversionCount == 2 + toBePresentMandatoryNodes.Count);
        Assert.True(allNodesPresent);
    }

    [Fact]
    public async void One2ManyDatatypesOneReplacementNodesWithDifferentAndSameNameCombined()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - DATATYPE-O2M-SamePlusALL_DIFF_NODES_combined.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {
            "ACESYS_PLCtoPLC",
            "ACESYS_FACEPLATE_RECIEVE",
            "TestDiffNameReplacement1",
            "TestDiffNameReplacement2",
            "TestDiffNameReplacement21",
            "TestDiffNameReplacement22"
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "ACESYS_FACEPLATE_PLCDIAGV2"
        };

        List<string> notToBePresentNodes = new()
        {
            "TestO2M_AllDiffName"
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

        Assert.Contains(xElem.Descendants("DataType"), n => n.Attribute("Name")!.Value.Equals("ACESYS_PLCtoPLC"));
        Assert.True(beforeConversionCount == 3);
        Assert.True(afterConversionCount == 6 + toBePresentMandatoryNodes.Count);
        Assert.True(allNodesPresent);
        Assert.True(allNodesNotToBePresentAreRemoved, "Nodes to be removed are still present");
    }

    [Fact]
    public async void DeleteDatatypesDeleteReplacement()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - DATATYPE-DEL.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {

        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "ACESYS_FACEPLATE_PLCDIAGV2"
        };

        List<string> notToBePresentNodes = new()
        {
            "ACESYS_ECS_PID_HLC"
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

        Assert.True(beforeConversionCount == 1);
        Assert.True(afterConversionCount == 0 + toBePresentMandatoryNodes.Count);
        Assert.True(allNodesPresent);
        Assert.True(allNodesNotToBePresentAreRemoved, "Nodes to be removed are still present");
    }

    [Fact]
    public async void One2OneDatatypesReplacementWithDefaultSelector()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - DATATYPE-O2OD.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {
            "ACESYS_FACEPLATE_SELECT"
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "ACESYS_FACEPLATE_PLCDIAGV2"
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

        Assert.True(beforeConversionCount == toBePresentNodes.Count);
        Assert.True(beforeConversionCount + toBePresentMandatoryNodes.Count == afterConversionCount);
        Assert.True(allNodesPresent);
    }

    [Fact]
    public async void One2OneDatatypesReplacementWithExtendedSelector()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - DATATYPE-O2OD.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsExtSelDefInt);

        List<string> toBePresentNodes = new()
        {
            "ACESYS_FACEPLATE_EXTSELECT"
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "ACESYS_FACEPLATE_PLCDIAGV2"
        };

        List<string> notToBePresentNodes = new()
        {
            "ACESYS_FACEPLATE_SELECT"
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

        Assert.True(beforeConversionCount == 1);
        Assert.True(afterConversionCount == 1 + toBePresentMandatoryNodes.Count);
        Assert.True(allNodesPresent);
        Assert.True(allNodesNotToBePresentAreRemoved, "Nodes to be removed are still present");
    }
}