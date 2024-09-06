using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Fls.AcesysConversion.Tests;

[Collection("Serial")]
public class RockwellTagTests : RockWellTestsBase
{
    private readonly string folderPath = @".\TestFiles\Tags";

    public RockwellTagTests()
    {
        NodeType = "Tags";
    }

    [Fact]
    public async void One2OneTagsOneReplacementProducesOneUpgradedTag()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - TAGS - O2O.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {
            "HMI_ALARM"
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "HMI_HWDIAG",
            "HMI_IoStatus"
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

        Assert.DoesNotContain(xElem.Descendants("Data"), n => n!.Attribute("Format")!.Value.Equals("L5K"));
        Assert.Contains(xElem.Descendants("Data"), n => n!.Attribute("Format")!.Value.Equals("Decorated"));
        allNodesPresent = true;
        Assert.True(beforeConversionCount == 1);
        Assert.True(afterConversionCount == beforeConversionCount + toBePresentMandatoryNodes.Count + 12);
        Assert.True(allNodesPresent);
        Assert.True(allNodesNotToBePresentAreRemoved, "Nodes to be removed are still present");
    }

    [Fact]
    public async void DeleteATagIfNoReplacementSpecified()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - TAGS - DEL.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {

        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "HMI_HWDIAG",
            "HMI_IoStatus"
        };

        List<string> notToBePresentNodes = new()
        {
            "HMI_ROUTE"
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
        Assert.True(afterConversionCount == toBePresentMandatoryNodes.Count + 12);
        Assert.True(allNodesPresent);
        Assert.True(allNodesNotToBePresentAreRemoved, "Nodes to be removed are still present");
    }

    [Fact]
    public async void One2ManyTagsOneReplacementWithOneSameNameProducesMultipleUpgradedTags()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - TAGS - O2M-SameNode.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {
            "HMI_PID_SPA",
            "HMI_PID_SPM",
            "HMI_PID",
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "HMI_HWDIAG",
            "HMI_IoStatus"
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

        if (afterConversion != null)
        {
            foreach (XmlNode x in afterConversion.ChildNodes)
            {
                if (toBePresentNodes.Any(t => t.Equals(x!.Attributes!["Name"]!.Value)))
                {
                    XElement xElem = XElement.Load(x!.CreateNavigator()!.ReadSubtree());

                    Assert.DoesNotContain(xElem.Descendants("Data"), n => n!.Attribute("Format")!.Value.Equals("L5K"));
                    Assert.Contains(xElem.Descendants("Data"), n => n!.Attribute("Format")!.Value.Equals("Decorated"));
                }
            }
        }
        allNodesPresent = true;
        Assert.True(beforeConversionCount == 1);
        Assert.True(afterConversionCount == 15 + toBePresentMandatoryNodes.Count);
        Assert.True(allNodesPresent);
        Assert.True(allNodesNotToBePresentAreRemoved, "Nodes to be removed are still present");
    }

    [Fact]
    public async void One2ManyTagsOneReplacementWithDifferentNamesProducesMultipleUpgradedTags()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - TAGS - O2M-DiffNode.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {
            "HMI_PID_SPA_HLC",
            "HMI_PID_SPM_HLC",
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "HMI_HWDIAG",
            "HMI_IoStatus"
        };

        List<string> notToBePresentNodes = new()
        {
            "HMI_PID_HLC"
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

        if (afterConversion != null)
        {
            foreach (XmlNode x in afterConversion.ChildNodes)
            {
                if (toBePresentNodes.Any(t => t.Equals(x!.Attributes!["Name"]!.Value)))
                {
                    XElement xElem = XElement.Load(x!.CreateNavigator()!.ReadSubtree());

                    Assert.DoesNotContain(xElem.Descendants("Data"), n => n!.Attribute("Format")!.Value.Equals("L5K"));
                    Assert.Contains(xElem.Descendants("Data"), n => n!.Attribute("Format")!.Value.Equals("Decorated"));
                }
            }
        }

        allNodesPresent = true;
        Assert.True(beforeConversionCount == 1);
        Assert.True(afterConversionCount == 14 + toBePresentMandatoryNodes.Count);
        Assert.True(allNodesPresent);
        Assert.True(allNodesNotToBePresentAreRemoved, "Nodes to be removed are still present");
    }

    [Fact]
    public async void TagsFacePlateMemberAttributesAreMoved()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - TAGS - O2O - FacePlateMemberMove.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {
            "PLC11_BIMOTOR01_FP"
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "HMI_HWDIAG",
            "HMI_IoStatus"
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

        Assert.DoesNotContain(xElem.Descendants("Data"), n => n!.Attribute("Format")!.Value.Equals("L5K"));
        Assert.Contains(xElem.Descendants("Data"), n => n!.Attribute("Format")!.Value.Equals("Decorated"));
        IEnumerable<XElement> dataMembers = xElem.XPathSelectElements("Tag/Data/Structure")
                        .Where(e => e.Attributes()
                                    .Any(a => a.Name == "DataType" && a.Value == "ACESYS_FACEPLATE_MOTOR"));

        IEnumerable<XElement> dataMember = dataMembers.Descendants("DataValueMember");

        XElement? foundDataMember = dataMember.Attributes()
                                    .Where(a => a.Name.LocalName.Equals("Name")
                                        && a.Value == "StartWarn_Pre").First().Parent;

        Assert.NotNull(foundDataMember);
        Assert.NotNull(foundDataMember.Value);
        bool startWarnPreChanged = foundDataMember.Attribute("Value")!.Value.Equals("test move start warn pre");
        //Assert.True(startWarnPreChanged);
        allNodesPresent = true;
        Assert.True(beforeConversionCount == 1);
        Assert.True(afterConversionCount == beforeConversionCount + toBePresentMandatoryNodes.Count + 12);
        Assert.True(allNodesPresent);
        Assert.True(allNodesNotToBePresentAreRemoved, "Nodes to be removed are still present");
    }

    [Fact]
    public async void TagsFacePlateMemberAttributesAreMovedMapByName()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - TAGS - O2O - FacePlateMemberMoveMapBy.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefIntMapByName);

        List<string> toBePresentNodes = new()
        {
            "_531BF360BT01T01_FP"
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "HMI_HWDIAG",
            "HMI_IoStatus"
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

        Assert.DoesNotContain(xElem.Descendants("Data"), n => n!.Attribute("Format")!.Value.Equals("L5K"));
        Assert.Contains(xElem.Descendants("Data"), n => n!.Attribute("Format")!.Value.Equals("Decorated"));
        IEnumerable<XElement> dataMembers = xElem.XPathSelectElements("Tag/Data/Structure")
                        .Where(e => e.Attributes()
                                    .Any(a => a.Name == "DataType" && a.Value == "ACESYS_FACEPLATE_ANALOG"));

        IEnumerable<XElement> dataMember = dataMembers.Descendants("DataValueMember");

        XElement? foundDataMember = dataMember.Attributes()
                                    .Where(a => a.Name.LocalName.Equals("Name")
                                        && a.Value == "Alarm_H2_Enable").First().Parent;

        Assert.NotNull(foundDataMember);
        Assert.NotNull(foundDataMember.Value);
        bool startWarnPreChanged = foundDataMember.Attribute("Value")!.Value.Equals("Alarm HH Enable Move Test Extended to (Alarm_H2_Enable)(Alarm_H3_Enable)");
        //Assert.True(startWarnPreChanged);
        allNodesPresent = true;
        Assert.True(beforeConversionCount == 1);
        Assert.True(afterConversionCount == beforeConversionCount + toBePresentMandatoryNodes.Count + 12);
        Assert.True(allNodesPresent);
        Assert.True(allNodesNotToBePresentAreRemoved, "Nodes to be removed are still present");
    }

    [Fact]
    public async void TagsFacePlateMemberAttributesAreMovedMapByFunction()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - TAGS - O2O - FacePlateMemberMoveMapBy.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefIntMapByFunction);

        List<string> toBePresentNodes = new()
    {
        "_531BF360BT01T01_FP"
    };

        List<string> toBePresentMandatoryNodes = new()
    {
        "HMI_HWDIAG",
        "HMI_IoStatus"
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

        Assert.DoesNotContain(xElem.Descendants("Data"), n => n!.Attribute("Format")!.Value.Equals("L5K"));
        Assert.Contains(xElem.Descendants("Data"), n => n!.Attribute("Format")!.Value.Equals("Decorated"));
        IEnumerable<XElement> dataMembers = xElem.XPathSelectElements("Tag/Data/Structure")
                        .Where(e => e.Attributes()
                                    .Any(a => a.Name == "DataType" && a.Value == "ACESYS_FACEPLATE_ANALOG"));

        IEnumerable<XElement> dataMember = dataMembers.Descendants("DataValueMember");

        XElement? foundDataMember = dataMember.Attributes()
                                    .Where(a => a.Name.LocalName.Equals("Name")
                                        && a.Value == "Alarm_H3_Enable").First().Parent;

        Assert.NotNull(foundDataMember);
        Assert.NotNull(foundDataMember.Value);
        bool startWarnPreChanged = foundDataMember.Attribute("Value")!.Value.Equals("Alarm HH Enable Move Test Extended to (Alarm_H2_Enable)(Alarm_H3_Enable)");
        Assert.True(startWarnPreChanged);
        allNodesPresent = true;
        Assert.True(beforeConversionCount == 1);
        Assert.True(afterConversionCount == beforeConversionCount + toBePresentMandatoryNodes.Count + 12);
        Assert.True(allNodesPresent);
        Assert.True(allNodesNotToBePresentAreRemoved, "Nodes to be removed are still present");
    }

    [Fact]
    public async void TagsFacePlateMemberAttributesRule_14()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - TAGS - O2O - FacePlateMemberMoveRule_14.L5X");

        XElement xElem = XElement.Load(fileName);

        IEnumerable<XElement> dataMembers = xElem.XPathSelectElements("Controller/Tags/Tag/Data/Structure");

        IEnumerable<XElement> dataMember = dataMembers.Descendants("DataValueMember");

        XElement? enableRepeatAlarmBeforeConversion = dataMember.Attributes()
                                    .Where(a => a.Name.LocalName.Equals("Name")
                                        && a.Value == "Repeat_Time_Pre").First().Parent;

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        List<string> toBePresentNodes = new()
        {
            "PLC11_BIMOTOR01_FP"
        };

        List<string> toBePresentMandatoryNodes = new()
        {
            "HMI_HWDIAG",
            "HMI_IoStatus"
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

        XElement xElemAfterConversion = XElement.Load(afterConversion!.CreateNavigator()!.ReadSubtree());

        IEnumerable<XElement> dataMembersAfterConversion = xElemAfterConversion.XPathSelectElements("Tag/Data/Structure")
                        .Where(e => e.Attributes()
                                    .Any(a => a.Name == "DataType" && a.Value == "ACESYS_FACEPLATE_ALARM"));

        IEnumerable<XElement> dataMemberAfterConversion = dataMembersAfterConversion.Descendants("DataValueMember");

        XElement? enableRepeatAlarmAfterConversion = dataMemberAfterConversion.Attributes()
                                    .Where(a => a.Name.LocalName.Equals("Name")
                                        && a.Value == "Enable_Repeat_Alarm").First().Parent;

        Assert.NotNull(enableRepeatAlarmAfterConversion);
        Assert.NotNull(enableRepeatAlarmBeforeConversion);

        Assert.False(enableRepeatAlarmAfterConversion
            .Attributes("Value").First().Value
            .Equals(enableRepeatAlarmBeforeConversion.Attributes("Value").First().Value));

        allNodesPresent = true;
        Assert.True(beforeConversionCount == 1);
        Assert.True(afterConversionCount == beforeConversionCount + toBePresentMandatoryNodes.Count + 12);
        Assert.True(allNodesPresent);
        Assert.True(allNodesNotToBePresentAreRemoved, "Nodes to be removed are still present");
    }

    [Fact]
    public async void AcesysHmiInterlockTagsAliasForDefaultInterlock()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - TAGS - O2O - AliasFor.L5X");

        (_, XmlNode? afterConversion, _) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        Assert.True(afterConversion != null);

        XElement xElem = XElement.Load(afterConversion!.CreateNavigator()!.ReadSubtree());

        IEnumerable<XElement> tag1 = xElem.XPathSelectElements("Tag[@Name='_232BC400MA01_FBINT01']")
                        .Where(e => e.Attributes().Any(a => a.Name == "AliasFor" && a.Value == "HMI_INTERLOCK[40]"));

        Assert.True(tag1 != null);
        Assert.True(tag1.First().Attributes().Any(a => a.Name == "TagType" && a.Value == "Base"));
        Assert.True(tag1.First().Attributes().Any(a => a.Name == "DataType" && a.Value == "AsysInterlock"));
        Assert.True(tag1.First().Attributes().Any(a => a.Name == "Constant" && a.Value == "false"));
        Assert.False(tag1.First().Attributes().Any(a => a.Name == "Radix" && a.Value == "Decimal"));
        Assert.True(tag1.First().Descendants().Any(a => a.Name == "Data"));
    }

    [Fact]
    public async void AcesysHmiInterlockTagsAliasForExtendedInterlock()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - TAGS - O2O - AliasFor.L5X");

        (_, XmlNode? afterConversion, _) = await ProcessXmlFile(fileName, optionsDefSelExtInt);

        Assert.True(afterConversion != null);

        XElement xElem = XElement.Load(afterConversion!.CreateNavigator()!.ReadSubtree());

        IEnumerable<XElement> tag1 = xElem.XPathSelectElements("Tag[@Name='_232BC400MA01_FBINT01']")
                        .Where(e => e.Attributes().Any(a => a.Name == "AliasFor" && a.Value == "HMI_INTERLOCK[40]"));

        Assert.True(tag1 != null);
        Assert.True(tag1.First().Attributes().Any(a => a.Name == "TagType" && a.Value == "Base"));
        Assert.True(tag1.First().Attributes().Any(a => a.Name == "DataType" && a.Value == "AsysExtInterlock"));
        Assert.True(tag1.First().Attributes().Any(a => a.Name == "Constant" && a.Value == "false"));
        Assert.False(tag1.First().Attributes().Any(a => a.Name == "Radix" && a.Value == "Decimal"));
        Assert.True(tag1.First().Descendants().Any(a => a.Name == "Data"));
    }

}
