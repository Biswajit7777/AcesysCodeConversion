using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Common.Enums;
using Fls.AcesysConversion.Helpers.Database;
using Fls.AcesysConversion.PLC.Rockwell.Components.Programs;
using Fls.AcesysConversion.PLC.Rockwell.Components.Tags;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.Tasks
{
    public partial class V7ToV8TasksUpgradeEngine : UpgradeEngine
    {
        public L5XTasks Tasks;
        public L5XTasks OriginalTasks;
        public RockwellL5XProject Project;
        public L5XPrograms OriginalPrograms;
        string? CoreRefXml = null;
        string? CLXConfigXml = null;
        string? PntConfigXml = null;


        public V7ToV8TasksUpgradeEngine(L5XCollection collection, L5XCollection originalCollection, RockwellL5XProject proj)
        {
            Tasks = (L5XTasks)collection;
            OriginalTasks = (L5XTasks)originalCollection;            
            Project = proj;            
        }

        public override void ProcessMandatory(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
        {
            
        }

        public override void ProcessMany2One(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
        {
            
        }

        public override void ProcessOne2Many(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
        {
            // Get the path to the App.Data folder
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AcesysConversion");

            // Define the expected file names
            var expectedFiles = new List<string> { "Pnt.Config.xml", "Core.Ref.xml", "CLX.Config.xml" };

            var fileContentMap = new Dictionary<string, string>();

            // Ensure the App.Data folder exists
            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }

            // Process each expected file
            foreach (var expectedFile in expectedFiles)
            {
                string filePath = Path.Combine(appDataFolder, expectedFile);

                // Check if the file exists
                if (File.Exists(filePath))
                {
                    // Read the content of the file
                    string content = File.ReadAllText(filePath);

                    // Process and update the file content based on the file name
                    if (expectedFile == "Pnt.Config.xml")
                    {
                        var pointTypeIdsAndUnits = ExtractPointTypeIdsAndUnits(content);
                        var updatedContent = ProcessPointTypeIdsAndReplace(content, pointTypeIdsAndUnits, dbHelper, options);

                        // Remove the xmlns="" attribute from <Reference> elements
                        updatedContent = RemoveEmptyXmlnsAttributes(updatedContent);

                        // Remove <Reference> nodes with ReferenceId containing ##UnitTag..## or ##SourceTag##
                        updatedContent = RemoveInvalidReferenceNodes(updatedContent);

                        PntConfigXml = FormatXml(updatedContent);
                        fileContentMap[expectedFile] = PntConfigXml;
                    }
                    else if (expectedFile == "Core.Ref.xml")
                    {
                        CoreRefXml = RemoveEmptyXmlnsAttributes(content);
                        fileContentMap[expectedFile] = CoreRefXml;
                    }
                    else if (expectedFile == "CLX.Config.xml")
                    {
                        CLXConfigXml = RemoveEmptyXmlnsAttributes(content);
                        UpdatePointsInCLXConfigXml(dbHelper);
                        CLXConfigXml = FormatXml(CLXConfigXml);
                        fileContentMap[expectedFile] = CLXConfigXml;
                    }
                }
            }

            // Save all updated XML files to the App.Data location
            foreach (var entry in fileContentMap)
            {
                string destinationPath = Path.Combine(appDataFolder, entry.Key);
                File.WriteAllText(destinationPath, entry.Value);
            }
        }

        private string RemoveInvalidReferenceNodes(string xmlContent)
        {
            var xmlDoc = XDocument.Parse(xmlContent);

            // Find and remove <Reference> nodes where <ReferenceId> contains ##UnitTag..## or ##SourceTag##
            var invalidReferences = xmlDoc.Descendants("Reference")
                .Where(refNode =>
                {
                    var referenceId = refNode.Element("ReferenceId")?.Value ?? string.Empty;
                    return referenceId.Contains("##UnitTag", StringComparison.OrdinalIgnoreCase) ||
                           referenceId.Contains("##SourceTag##", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            foreach (var invalidReference in invalidReferences)
            {
                invalidReference.Remove();
            }

            return xmlDoc.ToString(SaveOptions.DisableFormatting);
        }


        private void UpdatePointsInCLXConfigXml(DbHelper dbHelper)
        {
            XmlDocument clxConfigDoc = new XmlDocument();
            clxConfigDoc.LoadXml(CLXConfigXml);

            XmlDocument pntConfigDoc = new XmlDocument();
            pntConfigDoc.LoadXml(PntConfigXml);

            XmlNodeList pointsNodes = clxConfigDoc.SelectNodes("//Points");

            // Store designations and associated nodes for faster access
            var designationToEntityMap = new Dictionary<string, XmlNode>();

            foreach (XmlNode entityNode in pntConfigDoc.SelectNodes("//Entity"))
            {
                var designation = entityNode.SelectSingleNode("Designation")?.InnerText;
                if (!string.IsNullOrEmpty(designation) && !designationToEntityMap.ContainsKey(designation))
                {
                    designationToEntityMap[designation] = entityNode;
                }
            }

            // Using a parallel loop to speed up execution, only applicable if dbHelper is thread-safe
            Parallel.ForEach(pointsNodes.Cast<XmlNode>(), pointsNode =>
            {
                // Step 1: Extract the Designation value
                string designation = pointsNode.SelectSingleNode("Designation")?.InnerText;
                if (string.IsNullOrEmpty(designation))
                {
                    return;
                }

                // Step 2: Find the corresponding Entity node in PntConfigXml using cached map
                if (!designationToEntityMap.TryGetValue(designation, out XmlNode? entityNode))
                {
                    return;
                }

                // Step 3: Extract the PointTypeId value
                string pointTypeId = entityNode.SelectSingleNode("PointTypeId")?.InnerText;
                if (string.IsNullOrEmpty(pointTypeId))
                {
                    return;
                }

                // Step 4: Retrieve the FormatXml based on the PointTypeId
                XmlNode? formatXml = dbHelper.GetFormatNodeECSPoints(pointTypeId);

                // Check if PointTypeId contains "Interlock" and formatXml is null
                if (pointTypeId.Contains("Interlock", StringComparison.OrdinalIgnoreCase) && formatXml == null)
                {
                    lock (clxConfigDoc) // Use locking to ensure thread safety when removing nodes
                    {
                        // Delete the current Points node
                        pointsNode.ParentNode?.RemoveChild(pointsNode);
                    }
                    return;
                }

                if (formatXml != null)
                {
                    lock (clxConfigDoc) // Use locking for node manipulations
                    {
                        // Insert and replace nodes in formatXml
                        InsertElementAtPosition(formatXml, "Designation", pointsNode.SelectSingleNode("Designation")?.InnerText, 0);
                        InsertElementAtPosition(formatXml, "PointCode", pointsNode.SelectSingleNode("PointCode")?.InnerText, 1);
                        InsertElementAtPosition(formatXml, "PLC", pointsNode.SelectSingleNode("PLC")?.InnerText, 2);
                        InsertElementAtPosition(formatXml, "IsAnalog", pointsNode.SelectSingleNode("IsAnalog")?.InnerText, 3);

                        // Replace values in formatXml with the current pointsNode values
                        ReplaceNodeValue(formatXml, "InpType", pointsNode);
                        ReplaceNodeValue(formatXml, "InpAddr", pointsNode);
                        ReplaceNodeValue(formatXml, "OutputType1", pointsNode);
                        ReplaceNodeValue(formatXml, "OutAddr1", pointsNode);
                        ReplaceNodeValue(formatXml, "OutputType2", pointsNode);
                        ReplaceNodeValue(formatXml, "OutAddr2", pointsNode);
                        ReplaceNodeValue(formatXml, "ParameterType", pointsNode);
                        ReplaceNodeValue(formatXml, "PrmAddr", pointsNode);
                        ReplaceNodeValue(formatXml, "PLC", pointsNode);

                        // Overwrite the pointsNode with the updated formatXml
                        pointsNode.ParentNode?.ReplaceChild(clxConfigDoc.ImportNode(formatXml, true), pointsNode);
                    }
                }
            });

            // Save the updated CLXConfigXml back to the original string
            CLXConfigXml = clxConfigDoc.OuterXml;
        }

        private void InsertElementAtPosition(XmlNode parentNode, string elementName, string elementValue, int position)
        {
            if (!string.IsNullOrEmpty(elementValue))
            {
                XmlDocument doc = parentNode.OwnerDocument;
                XmlElement newElement = doc.CreateElement(elementName);
                newElement.InnerText = elementValue;

                // Insert element at specified position
                XmlNodeList childNodes = parentNode.ChildNodes;
                if (position < childNodes.Count)
                {
                    parentNode.InsertBefore(newElement, childNodes[position]);
                }
                else
                {
                    parentNode.AppendChild(newElement);
                }
            }
        }

        private void ReplaceNodeValue(XmlNode targetNode, string elementName, XmlNode sourceNode)
        {
            XmlNode sourceChild = sourceNode.SelectSingleNode(elementName);
            if (sourceChild != null)
            {
                XmlNode targetChild = targetNode.SelectSingleNode(elementName);
                if (targetChild != null)
                {
                    string newValue = sourceChild.InnerText;

                    // Special case for PrmAddr
                    if (elementName == "PrmAddr" && string.IsNullOrEmpty(newValue))
                    {
                        string designation = sourceNode.SelectSingleNode("Designation")?.InnerText;
                        if (!string.IsNullOrEmpty(designation))
                        {
                            newValue = $"{designation}_FP";
                        }
                    }

                    // Special case for OutAddr1
                    if (elementName == "OutAddr1")
                    {
                        string designation = sourceNode.SelectSingleNode("Designation")?.InnerText;
                        if (!string.IsNullOrEmpty(designation))
                        {
                            newValue = $"{designation}_FP.Faceplate";
                        }
                    }

                    // Special case for InpAddr
                    if (elementName == "InpAddr" && !string.IsNullOrEmpty(newValue))
                    {
                        // Example: "HMI_INTERLOCK[00].14"
                        var match = Regex.Match(newValue, @"\[([0-9]+)\]\.([0-9]+)");
                        if (match.Success)
                        {
                            int innerIndex = int.Parse(match.Groups[1].Value);
                            int outerIndex = int.Parse(match.Groups[2].Value);
                            int calculatedIndex = innerIndex * 16 + outerIndex;

                            // Replace placeholder XXX[innerIndex] with the calculated result
                            newValue = Regex.Replace(newValue, @"\[([0-9]+)\]\.([0-9]+)", $"[{calculatedIndex}]");
                        }
                    }

                    // Special case for OutAddr1
                    if (elementName == "OutAddr1" && !string.IsNullOrEmpty(newValue))
                    {
                        // Example: "HMI_INTERLOCK[00].14"
                        var match = Regex.Match(newValue, @"\[([0-9]+)\]\.([0-9]+)");
                        if (match.Success)
                        {
                            int innerIndex = int.Parse(match.Groups[1].Value);
                            int outerIndex = int.Parse(match.Groups[2].Value);
                            int calculatedIndex = innerIndex * 16 + outerIndex;

                            // Replace placeholder XXX[innerIndex] with the calculated result
                            newValue = Regex.Replace(newValue, @"\[([0-9]+)\]\.([0-9]+)", $"[{calculatedIndex}]");
                        }
                    }

                    targetChild.InnerText = newValue;
                }
            }
        }

        //private void UpdatePointsNode(XmlNode pointsNode, PointData pointData)
        //{
        //    pointsNode.SelectSingleNode("InpType").InnerText = pointData.InpType;
        //    pointsNode.SelectSingleNode("InpAddr").InnerText = pointData.InpAddr;
        //    pointsNode.SelectSingleNode("OutputType1").InnerText = pointData.OutputType1;
        //    pointsNode.SelectSingleNode("OutAddr1").InnerText = pointData.OutAddr1;
        //    pointsNode.SelectSingleNode("OutputType2").InnerText = pointData.OutputType2;
        //    pointsNode.SelectSingleNode("OutAddr2").InnerText = pointData.OutAddr2;
        //    pointsNode.SelectSingleNode("ParameterType").InnerText = pointData.ParameterType;
        //    pointsNode.SelectSingleNode("PrmAddr").InnerText = pointData.PrmAddr;
        //    pointsNode.SelectSingleNode("PLC").InnerText = pointData.PLC;
        //}

        private string RemoveEmptyXmlnsAttributes(string xml)
        {
            XmlDocument doc = new XmlDocument();

            try
            {
                // Load the XML content into the XmlDocument
                doc.LoadXml(xml);
            }
            catch (XmlException ex)
            {
                // Handle the XML exception, such as invalid characters or malformed XML
                Console.WriteLine($"XML parsing error: {ex.Message}");
                return xml; // Return or handle the error appropriately
            }

            // Iterate over all elements in the document
            foreach (XmlElement element in doc.DocumentElement.GetElementsByTagName("*"))
            {
                // Check for the xmlns attribute with an empty value
                XmlAttribute xmlnsAttribute = element.Attributes["xmlns"];
                if (xmlnsAttribute != null && string.IsNullOrEmpty(xmlnsAttribute.Value))
                {
                    element.Attributes.Remove(xmlnsAttribute);
                }
            }

            // Serialize the document back to a string
            using (StringWriter stringWriter = new StringWriter())
            using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = false,
                Encoding = System.Text.Encoding.UTF8 // Ensure proper encoding
            }))
            {
                doc.WriteTo(xmlWriter);
                xmlWriter.Flush();
                return stringWriter.ToString();
            }
        }

        private string FormatXml(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter))
            {
                xmlTextWriter.Formatting = Formatting.Indented;
                doc.WriteTo(xmlTextWriter);
                return stringWriter.ToString();
            }
        }
        

        private List<(string PointTypeId, string Unit)> ExtractPointTypeIdsAndUnits(string xmlContent)
        {
            var pointTypeIdsAndUnits = new List<(string PointTypeId, string Unit)>();

            // Parse the XML content
            XDocument xmlDoc = XDocument.Parse(xmlContent);
            var entities = xmlDoc.Descendants("{http://tempuri.org/Fls.Ecc.Pnt.Config.xsd}Entity");

            // Extract PointTypeId and Unit values
            foreach (var entity in entities)
            {
                var pointTypeId = entity.Element("{http://tempuri.org/Fls.Ecc.Pnt.Config.xsd}PointTypeId")?.Value;
                var unit = entity.Element("{http://tempuri.org/Fls.Ecc.Pnt.Config.xsd}Unit")?.Value ?? string.Empty;

                if (!string.IsNullOrEmpty(pointTypeId))
                {
                    pointTypeIdsAndUnits.Add((pointTypeId, unit));
                }
            }

            return pointTypeIdsAndUnits;
        }

        private string ProcessPointTypeIdsAndReplace(string xmlContent, List<(string PointTypeId, string Unit)> pointTypeIdsAndUnits, DbHelper dbHelper, RockwellUpgradeOptions options)
        {
            // Remove namespaces
            var xmlDoc = XDocument.Parse(RemoveAllNamespaces(xmlContent));
            var entities = xmlDoc.Descendants("Entity");

            foreach (var entity in entities)
            {
                var pointTypeIdElement = entity.Element("PointTypeId");
                if (pointTypeIdElement != null)
                {
                    string pointTypeId = pointTypeIdElement.Value;
                    string searchParameter;
                    string newPointTypeIdValue = null;
                    string appendDataXmlContent = null;
                    string appendLanguageDataXmlContent = null;

                    // Determine whether to use Unit or Type for the search based on PointTypeId
                    if (pointTypeId.Contains("Alternate", StringComparison.OrdinalIgnoreCase))
                    {
                        string designationValue = entity.Element("Designation")?.Value ?? string.Empty;
                        searchParameter = entity.Element("Type")?.Value ?? string.Empty;

                        // Iterate from Unit01 to Unit10 and replace each corresponding placeholder
                        for (int i = 1; i <= 10; i++)
                        {
                            string unitSuffix = i.ToString("D2");
                            string unitDesignationValue = $@"\{designationValue}.FB.Unit{unitSuffix}_OK";
                            string fullDesignationValue = $"OTE({unitDesignationValue})";

                            // Extract the UnitTag using the current designation value
                            string UnitTag = MethodToExtractAlternateUnitTagElement(fullDesignationValue, dbHelper);

                            if (!string.IsNullOrEmpty(UnitTag))
                            {
                                if (appendDataXmlContent == null)
                                {
                                    appendDataXmlContent = dbHelper.GetAppendDataXml(pointTypeId);
                                }

                                if (!string.IsNullOrEmpty(UnitTag))
                                {
                                    appendDataXmlContent = appendDataXmlContent.Replace($"##UnitTag{unitSuffix}##", UnitTag);
                                }
                            }
                        }
                    }                    
                    else
                    {
                        if (pointTypeId.Contains("Select", StringComparison.OrdinalIgnoreCase))
                        {
                            searchParameter = entity.Element("Unit")?.Value ?? string.Empty;

                            // Retrieve the replacement value
                            newPointTypeIdValue = dbHelper.GetECSPointTypeReplacement(pointTypeId, searchParameter, dbHelper);

                            if (newPointTypeIdValue != null)
                            {
                                // Split the result using regex to match any valid identifier (e.g., PointTypeAcesysSelect8.0)
                                var matches = Regex.Matches(newPointTypeIdValue, @"\b[\w\d\.]+\b");

                                if (matches.Count >= 2)
                                {
                                    if (options.IsExtendedSelect)
                                    {
                                        newPointTypeIdValue = matches[1].Value; // Extracts the second operand (e.g., PointTypeAcesysExtSelect8.0)
                                    }
                                    else
                                    {
                                        newPointTypeIdValue = matches[0].Value; // Extracts the first operand (e.g., PointTypeAcesysSelect8.0)
                                    }
                                }
                            }

                            
                        }

                        else
                        {
                            searchParameter = entity.Element("Unit")?.Value ?? string.Empty;
                            newPointTypeIdValue = dbHelper.GetECSPointTypeReplacement(pointTypeId, searchParameter, dbHelper);                            
                        }

                        appendDataXmlContent = dbHelper.GetAppendDataXml(pointTypeId);
                        appendLanguageDataXmlContent = dbHelper.GetLanguageDataXml(pointTypeId);


                    }

                    if (!string.IsNullOrEmpty(newPointTypeIdValue))
                    {
                        pointTypeIdElement.Value = newPointTypeIdValue;
                    }

                    else
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(pointTypeIdElement.ToString());  // Load the XElement as XML string
                        XmlElement xmlElement = doc.DocumentElement; // Get the XmlElement

                        // Now you can use xmlElement in the AddUserMessage method
                        L5XCollection.AddUserMessage(Project, xmlElement, null, UserMessageTypes.Warning,
                            $"{pointTypeId} equivalent in V8 is not available", "",
                            $"{pointTypeId} is not converted");
                    }

                    if (!string.IsNullOrEmpty(appendDataXmlContent))
                    {
                        var designationNode = entity.Element("Designation");
                        if (designationNode != null)
                        {
                            string designation = designationNode.Value;
                            appendDataXmlContent = appendDataXmlContent.Replace("##Tag##", designation);

                            string wrappedXmlContent = $"<Root>{appendDataXmlContent}</Root>";
                            var additionalXml = XElement.Parse(wrappedXmlContent);

                            foreach (var element in additionalXml.Elements())
                            {
                                entity.Add(element);
                            }
                        }
                    }

                    // Handle appending appendLanguageDataXmlContent before </Fls.Ecc.Pnt.Config>
                    if (!string.IsNullOrEmpty(appendLanguageDataXmlContent))
                    {
                        var designationNode = entity.Element("Designation");
                        if (designationNode != null)
                        {
                            string designation = designationNode.Value;
                            // Replace ##Tag## in appendLanguageDataXmlContent with the designation value
                            appendLanguageDataXmlContent = appendLanguageDataXmlContent.Replace("##Tag##", designation);

                            var configElement = xmlDoc.Descendants("Fls.Ecc.Pnt.Config").FirstOrDefault();
                            if (configElement != null)
                            {
                                // Wrap the appendLanguageDataXmlContent in a root element
                                string wrappedLanguageXmlContent = $"<Root>{appendLanguageDataXmlContent}</Root>";
                                var additionalLanguageXml = XElement.Parse(wrappedLanguageXmlContent);

                                // Append each child element of the wrapped content before the closing </Fls.Ecc.Pnt.Config> tag
                                foreach (var element in additionalLanguageXml.Elements())
                                {
                                    configElement.Add(element);
                                }
                            }
                        }
                    }
                }
            }

            // Return the updated XML as a string, including the XML declaration
            return xmlDoc.Declaration + xmlDoc.ToString(SaveOptions.DisableFormatting);
        }

        private string MethodToExtractAlternateUnitTagElement(string designationValue, DbHelper dbHelper)
        {
            var programsToModify = Project.Content?.Controller?.Programs;

            if (programsToModify != null)
            {
                foreach (L5XProgram program in programsToModify)
                {
                    foreach (L5XRoutine routine in program.Routines)
                    {
                        foreach (XmlElement textElement in routine.SelectNodes(".//Text"))
                        {
                            string textContent = textElement.InnerText;

                            if (textContent.Contains(designationValue))
                            {
                                var match = Regex.Match(textContent, @"XIC\(([^)]+)\)");

                                if (match.Success)
                                {
                                    string extractcontent = match.Groups[1].Value;

                                    string result = Regex.Replace(extractcontent, @"_.+$", "");

                                    return result;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }



        // Helper method to remove namespaces
        public static string RemoveAllNamespaces(string xmlDocument)
        {
            XElement xmlDocumentWithoutNs = RemoveAllNamespaces(XElement.Parse(xmlDocument));
            return xmlDocumentWithoutNs.ToString();
        }

        // Core recursion function to remove namespaces
        private static XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            if (!xmlDocument.HasElements)
            {
                XElement xElement = new XElement(xmlDocument.Name.LocalName);
                xElement.Value = xmlDocument.Value;

                foreach (XAttribute attribute in xmlDocument.Attributes())
                    xElement.Add(attribute);

                return xElement;
            }
            return new XElement(xmlDocument.Name.LocalName, xmlDocument.Elements().Select(el => RemoveAllNamespaces(el)));
        }



        public override void ProcessOne2One(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
        {            
            ProcessOne2OneTasks(dbHelper, options, progress);
        }

        

        private void ProcessOne2OneTasks(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
        {
            RemoveFBIntTags();
            var tasksToModify = Project.Content?.Controller?.Tasks;
            var programToModify = Project.Content?.Controller?.Programs;

            _ = Project.Content?.Controller?.Tasks.RemoveByXPath("//Tasks/Task");

            XmlDocument doc = new XmlDocument();
            List<XmlElement> tasksElements = GenerateTasksXml(doc);

            if (tasksToModify == null)
            {
                progress.Report("No tasks to modify.");
                return;
            }

            XmlElement digitalTask = tasksElements.FirstOrDefault(t => t.GetAttribute("Name") == "Digital");
            XmlElement analogTask = tasksElements.FirstOrDefault(t => t.GetAttribute("Name") == "Analog");
            XmlElement digitalAlarmTask = tasksElements.FirstOrDefault(t => t.GetAttribute("Name") == "Digital_Alarm");

            foreach (L5XTask task in OriginalTasks)
            {
                XmlNodeList scheduledProgramNodes = task.SelectNodes("//ScheduledPrograms/ScheduledProgram");
                XmlNodeList scheduledProgramNodes1 = programToModify.SelectNodes("//Programs/Program");

                foreach (XmlNode scheduledProgramNode in scheduledProgramNodes)
                {
                    string name = scheduledProgramNode.Attributes["Name"]?.Value;

                    if (name == "MasterProgram")
                    {
                        int sequence = 0; // Replace with the appropriate sequence value
                        L5XScheduledProgram scheduledProgram = L5XScheduledProgram.FromXmlNode(scheduledProgramNode, doc, sequence);

                        XmlNode scheduledProgramsNode = digitalTask["ScheduledPrograms"];
                        scheduledProgramsNode.AppendChild(scheduledProgram);

                        progress.Report($"Processing Task {name}. {name} is added to Task named Digital");
                        L5XCollection.AddUserMessage(Project, scheduledProgram, null, UserMessageTypes.Information,
                                         $"Task {name}", "O2O", $"Task {name} has been added to Digital Task");
                    }
                    else
                    {
                        string TaskName = name;
                        string DataType = ExtractDatatypeElement(TaskName);

                        if (DataType == "AsysGroup")
                        {
                            int sequence = 0; // Replace with the appropriate sequence value
                            L5XScheduledProgram scheduledProgram = L5XScheduledProgram.FromXmlNode(scheduledProgramNode, doc, sequence);

                            XmlNode scheduledProgramsNode = digitalTask["ScheduledPrograms"];
                            scheduledProgramsNode.AppendChild(scheduledProgram);

                            progress.Report($"Processing Task {name}. {name} is added to Task named Digital");
                            L5XCollection.AddUserMessage(Project, scheduledProgram, null, UserMessageTypes.Information,
                                             $"Task {name}", "O2O", $"Task {name} has been added to Digital Task");
                        }
                    }

                    progress.Report($"Processed ScheduledProgram: Name={name}");
                }

                foreach (XmlNode scheduledProgramNode1 in scheduledProgramNodes1)
                {
                    string taskName = scheduledProgramNode1.Attributes["Name"]?.Value;

                    // Fetch the MainRoutineName attribute
                    string mainRoutineName = scheduledProgramNode1.Attributes["MainRoutineName"]?.Value;

                    // Check if taskName exists in scheduledProgramNodes
                    bool taskNameExists = scheduledProgramNodes.Cast<XmlNode>().Any(n => n.Attributes["Name"]?.Value == taskName);

                    if (!taskNameExists)
                    {
                        // TaskName does not exist in scheduledProgramNodes, proceed with processing
                        int sequence = 0; // Replace with the appropriate sequence value
                        L5XScheduledProgram newScheduledProgram = L5XScheduledProgram.CreateNew(taskName, doc, sequence);
                        newScheduledProgram = L5XScheduledProgram.FromXmlNode(newScheduledProgram, doc, sequence);


                        string Datatype = scheduledProgramNode1.Attributes["MainRoutineName"]?.Value;

                        if (Datatype == "Unimotor" || Datatype == "AsysSel" || Datatype == "AsysExtSel" || Datatype == "Valve" || Datatype == "Bimotor" || Datatype == "Department" || Datatype == "Motor" || Datatype == "Gate" || Datatype == "Positioner" || Datatype == "Recipe" || Datatype == "DiagDigital"|| Datatype == "CPUStatus" || Datatype == "DiagNode" || Datatype == "Indication")
                        {
                            XmlNode scheduledProgramsNode1 = digitalTask["ScheduledPrograms"];
                            scheduledProgramsNode1.AppendChild(newScheduledProgram);

                            progress.Report($"Processing Task {taskName}. {taskName} is added to Task named Digital");
                            L5XCollection.AddUserMessage(Project, newScheduledProgram, null, UserMessageTypes.Information,
                                             $"Task {taskName}", "O2O", $"Task {taskName} has been added to Digital Task");
                        }

                        else if (Datatype == "Alarm")
                        {
                            XmlNode scheduledProgramsNode1 = digitalAlarmTask["ScheduledPrograms"];
                            scheduledProgramsNode1.AppendChild(newScheduledProgram);

                            progress.Report($"Processing Task {taskName}. {taskName} is added to Task named Digital Alarm");
                            L5XCollection.AddUserMessage(Project, newScheduledProgram, null, UserMessageTypes.Information,
                                             $"Task {taskName}", "O2O", $"Task {taskName} has been added to Digital Alarm Task");
                        }

                        else if (Datatype == "Analog" || Datatype == "PID" || Datatype == "Totalizer" || Datatype == "HLC" || Datatype == "DiagAnalog" || Datatype == "Main")
                        {
                            XmlNode scheduledProgramsNode1 = analogTask["ScheduledPrograms"];
                            scheduledProgramsNode1.AppendChild(newScheduledProgram);

                            progress.Report($"Processing Task {taskName}. {taskName} is added to Task named Digital Alarm");
                            L5XCollection.AddUserMessage(Project, newScheduledProgram, null, UserMessageTypes.Information,
                                             $"Task {taskName}", "O2O", $"Task {taskName} has been added to Analog Task");
                        }                        

                        progress.Report($"Processed ScheduledProgram: Name={taskName}");
                    }
                }
            }

            // Add each generated task element to tasksElement
            foreach (XmlElement taskElement in tasksElements)
            {
                Project.Content?.Controller?.Tasks.AddByElement(taskElement);
            }
        }

        private void RemoveFBIntTags()
        {
            if (Project.Content?.Controller?.Tags != null)
            {
                Regex fbintRegex = new Regex("_FBINT", RegexOptions.Compiled);
                List<XmlNode> nodesToRemove = new List<XmlNode>();

                foreach (L5XTag tagItem in Project.Content?.Controller?.Tags!)
                {
                    if (tagItem != null && fbintRegex.IsMatch(tagItem.TagName!))
                    {
                        XmlNode? nodeToRemove = Project.Content?.Controller?.Tags!.SelectSingleNode($"Tag[@Name='{tagItem.TagName!}']");

                        if (nodeToRemove != null)
                        {
                            nodesToRemove.Add(nodeToRemove);
                        }
                    }
                }

                foreach (XmlNode node in nodesToRemove)
                {
                    _ = Project.Content?.Controller?.Tags!.RemoveChild(node);
                }
            }
        }


        public static XmlNode ConvertToXmlNode(L5XScheduledProgram newScheduledProgram)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(L5XScheduledProgram));
            XmlDocument xmlDoc = new XmlDocument();

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(memoryStream))
                {
                    serializer.Serialize(writer, newScheduledProgram);
                    memoryStream.Position = 0;
                    xmlDoc.Load(memoryStream);
                }
            }

            return xmlDoc.DocumentElement;
        }

        private string ExtractDatatypeElement(string routineName)
        {
            OriginalPrograms = (L5XPrograms?)Project.Content?.Controller?.Programs;

            if (OriginalPrograms != null)
            {
                XmlNodeList tagNodes = OriginalPrograms.SelectNodes("//Program");
                foreach (XmlNode tagNode in tagNodes)
                {
                    if (routineName == tagNode.Attributes["Name"].Value)
                    {
                        foreach (XmlNode ProgramTagNode in tagNode)
                        {
                            if (ProgramTagNode.HasChildNodes)
                            {
                                // Get the first child node
                                XmlNode firstChild = ProgramTagNode.FirstChild;

                                // Check if the first child node has attributes and if the 'DataType' attribute exists
                                if (firstChild.Attributes != null && firstChild.Attributes["DataType"] != null)
                                {
                                    // Access the 'DataType' attribute value
                                    string datatype = firstChild.Attributes["DataType"].Value;
                                    return datatype;
                                }
                            }

                        }
                    }                                     
                }
            }
            return null;
        }

        public List<XmlElement> GenerateTasksXml(XmlDocument doc)
        {
            List<XmlElement> taskElements = new List<XmlElement>();

            // Create the Analog task
            XmlElement analogTask = CreateTaskElement(doc, "Analog", "PERIODIC", "75", "2", "500");
            taskElements.Add(analogTask);

            // Create the Digital task
            XmlElement digitalTask = CreateTaskElement(doc, "Digital", "PERIODIC", "100", "4", "500");
            taskElements.Add(digitalTask);

            // Create the Digital_Alarm task
            XmlElement digitalAlarmTask = CreateTaskElement(doc, "Digital_Alarm", "PERIODIC", "100", "3", "500");
            taskElements.Add(digitalAlarmTask);

            return taskElements;
        }

        private XmlElement CreateTaskElement(XmlDocument doc, string name, string type, string rate, string priority, string watchdog)
        {
            XmlElement taskElement = doc.CreateElement("Task");
            taskElement.SetAttribute("Name", name);
            taskElement.SetAttribute("Type", type);
            taskElement.SetAttribute("Rate", rate);
            taskElement.SetAttribute("Priority", priority);
            taskElement.SetAttribute("Watchdog", watchdog);
            taskElement.SetAttribute("DisableUpdateOutputs", "false");
            taskElement.SetAttribute("InhibitTask", "false");

            XmlElement scheduledPrograms = doc.CreateElement("ScheduledPrograms");
            scheduledPrograms.AppendChild(doc.CreateTextNode("")); // Ensure <ScheduledPrograms></ScheduledPrograms>
            taskElement.AppendChild(scheduledPrograms);

            return taskElement;
        }

        private string GetNewFileName(string filePath)
        {
            var fileName = Path.GetFileName(filePath);

            if (Regex.IsMatch(fileName, @"Fls\.Core\.Ref\.xml", RegexOptions.IgnoreCase))
            {
                return "Core.Ref.xml";
            }
            if (Regex.IsMatch(fileName, @"Fls\.Ecc\.PLC\.Config\.xml", RegexOptions.IgnoreCase))
            {
                return "CLX.Config.xml";
            }
            if (Regex.IsMatch(fileName, @"Fls\.Ecc\.Pnt\.Config\.xml", RegexOptions.IgnoreCase))
            {
                return "Pnt.Config.xml";
            }

            return string.Empty; // Return empty if no match is found
        }

        public override void ProcessRemoval(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
        {
            
        }
    }
}
