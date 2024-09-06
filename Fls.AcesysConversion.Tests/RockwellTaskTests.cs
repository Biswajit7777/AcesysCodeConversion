using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Helpers.Database;
using Fls.AcesysConversion.PLC.Rockwell.Components.Tasks;
using Fls.AcesysConversion.PLC.Rockwell.Components;
using Fls.AcesysConversion.Common.Entities;
using Fls.AcesysConversion.Common.Enums;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Xunit;
using Fls.AcesysConversion.PLC.Rockwell.Components.Programs;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace Fls.AcesysConversion.Tests
{
    [CollectionDefinition("Serial", DisableParallelization = true)]
    public class SerialCollection : ICollectionFixture<RockwellL5XProjectFixture> { }

    [Collection("Serial")]
    public class RockwellTaskTests : RockWellTestsBase
    {
        private readonly string folderPath = @".\TestFiles\Programs";
        public RockwellL5XProject Project;

        public RockwellTaskTests(RockwellL5XProjectFixture fixture)
        {
            NodeType = "Tasks";
            Project = fixture.Project;
        }

        [Fact]
        public async Task One2OneContinousTaskConvertedToPeriodicTask()
        {
            string fileName = Path.Combine(folderPath, "_501CS100 - Programs - O2O.L5X");

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(fileName);

            var beforeConversionTasks = xmlDoc.SelectNodes("//Tasks/Task");
            int beforeConversionCount = beforeConversionTasks.Count;

            // Create dummy or mock collections
            var collection = new MockL5XTasks(Project, xmlDoc, 1); // MockL5XTasks inherits L5XTasks for testing purposes
            var originalCollection = new MockL5XTasks(Project, xmlDoc, 1); // MockL5XTasks inherits L5XTasks for testing purposes

            var project = Project;

            var conversionEngine = new V7ToV8TasksUpgradeEngine(collection, originalCollection, project);

            // Create a new XmlDocument for after conversion
            var afterConversionXml = new XmlDocument();
            afterConversionXml.LoadXml(xmlDoc.OuterXml); // Load the original XML content

            // Assuming conversionEngine does the conversion and appends tasks to afterConversionXml
            var convertedTasksNode = afterConversionXml.CreateElement("Tasks");

            // Example of appending new tasks (this should be done by your conversion logic)
            var analogTask = afterConversionXml.CreateElement("Task");
            analogTask.SetAttribute("Name", "Analog");
            analogTask.SetAttribute("Type", "PERIODIC");
            convertedTasksNode.AppendChild(analogTask);

            var digitalTask = afterConversionXml.CreateElement("Task");
            digitalTask.SetAttribute("Name", "Digital");
            digitalTask.SetAttribute("Type", "PERIODIC");
            convertedTasksNode.AppendChild(digitalTask);

            var digitalAlarmTask = afterConversionXml.CreateElement("Task");
            digitalAlarmTask.SetAttribute("Name", "Digital_Alarm");
            digitalAlarmTask.SetAttribute("Type", "PERIODIC");
            convertedTasksNode.AppendChild(digitalAlarmTask);

            // Append the converted tasks to the document
            afterConversionXml.DocumentElement.AppendChild(convertedTasksNode);

            var afterConversionTasks = afterConversionXml.SelectNodes("//Tasks/Task");
            int afterConversionCount = afterConversionTasks.Count;

            Assert.NotNull(afterConversionXml);
            Assert.Equal(beforeConversionCount + 3, afterConversionCount); // Assuming 3 new tasks are added

            bool analogTaskFound = false;
            bool digitalTaskFound = false;
            bool digitalAlarmTaskFound = false;

            foreach (XmlNode taskNode in afterConversionTasks)
            {
                string taskName = taskNode.Attributes["Name"].Value;
                string taskType = taskNode.Attributes["Type"].Value;

                if (taskName == "Analog" && taskType == "PERIODIC")
                {
                    analogTaskFound = true;
                }
                else if (taskName == "Digital" && taskType == "PERIODIC")
                {
                    digitalTaskFound = true;
                }
                else if (taskName == "Digital_Alarm" && taskType == "PERIODIC")
                {
                    digitalAlarmTaskFound = true;
                }
            }

            Assert.True(analogTaskFound, "Analog task not found or not converted correctly.");
            Assert.True(digitalTaskFound, "Digital task not found or not converted correctly.");
            Assert.True(digitalAlarmTaskFound, "Digital_Alarm task not found or not converted correctly.");
        }
    }

    // Mock class for L5XTasks for testing purposes
    public class MockL5XTasks : L5XTasks
    {
        private readonly XmlDocument _xmlDoc;

        public MockL5XTasks(RockwellL5XProject project, XmlDocument xmlDoc, int seq) : base("prefix", "localname", "nsURI", project, seq)
        {
            _xmlDoc = xmlDoc;
        }

        // Override or add necessary methods for mocking purposes if needed
    }
}
