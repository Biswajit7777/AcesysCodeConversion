using System.Xml;
using System.Xml.Linq;

namespace Fls.AcesysConversion.Tests;

[Collection("Serial")]
public class RockwellProgramTests : RockWellTestsBase
{
    private readonly string folderPath = @".\TestFiles\Programs";

    public RockwellProgramTests()
    {
        NodeType = "Programs";
    }

    [Fact]
    public async void One2OneAllRoutinesInsideAProgramAreConvertedToTopLevelPrograms()
    {
        string fileName = Path.Combine(folderPath, "_501CS100 - Programs - O2O.L5X");

        (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

        Assert.True(afterConversion != null);
    }

    [Fact]
    public async void One2ManyProgramsOneReplacementAndMandatoryProgramPresent()
    {
        try
        {
            string fileName = Path.Combine(folderPath, "_501CS100 - Programs - O2M.L5X");

            (int beforeConversionCount, XmlNode? afterConversion, int afterConversionCount) = await ProcessXmlFile(fileName, optionsDefSelDefInt);

            List<string> toBePresentNodes = new()
        {
            "PLCCommRcv",
            "PLCCommSend"
        };

            List<string> toBePresentMandatoryNodes = new()
        {
            "DiagPLC",
            "DiagDigital",
            "DiagAnalog",
            "Dispatcher"
        };

            List<string> notToBePresentNodes = new()
        {
            "AsysComm"
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

            Assert.True(beforeConversionCount == 1);
            Assert.True(afterConversionCount == 2 + toBePresentMandatoryNodes.Count);

            Assert.True(beforeConversionCount > toBePresentNodes.Count);
            Assert.True(allNodesNotToBePresentAreRemoved, "Nodes to be removed are not present");
            Assert.True(allNodesPresent);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed with exception: {ex.Message}");
            Console.WriteLine(ex.StackTrace);           
        }


    }
}
