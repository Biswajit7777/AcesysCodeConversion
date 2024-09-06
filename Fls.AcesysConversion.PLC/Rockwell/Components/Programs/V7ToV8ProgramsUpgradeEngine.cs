using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Common.DTOs;
using Fls.AcesysConversion.Common.Enums;
using Fls.AcesysConversion.Helpers.Database;
using Fls.AcesysConversion.PLC.Rockwell.Components.DataTypes;
using Fls.AcesysConversion.PLC.Rockwell.Components.Tags;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using static System.Net.Mime.MediaTypeNames;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.Programs;

public partial class V7ToV8ProgramsUpgradeEngine : UpgradeEngine
{
    public L5XPrograms Programs;
    public L5XPrograms OriginalPrograms;
    public RockwellL5XProject Project;
    public L5XTags OriginalTags;

    //Global
    string newExtStart_Reset = "";
    string newExt_Stop = "";
    private int InterlockIndex = 0;
    private static int HMI_INTERLOCK_MAX_INDEX = 0;
    private static int HMI_INTERLOCK_MAX_GROUP = 0;
    XElement childPrograms;
    private static int New_Program_Index = 0;
    private XmlNodeList InterlockXmlNodeList;
    private XmlDocument xmlDoc;
    private XmlDocument xmlDocNew;
    L5XTags InterlockTags;
    L5XTags DSETags;
    string DepartmentName = "";
    string GroupName = "";
    string ControllerName = String.Empty;
    XDocument docNew;
    int HMI_Max_Unit_Index = 0;
    List<string> NetworkModules = new List<string>();
    List<string> NodeModules = new List<string>();
    List<string> RemoteIOModules = new List<string>();
    List<AnalogInputModule> AnalogInputModules = new List<AnalogInputModule>();
    List<AnalogOutputModule> AnalogOutputModules = new List<AnalogOutputModule>();
    List<DigitalInputModule> DigitalInputModules = new List<DigitalInputModule>();
    List<DigitalOutputModule> DigitalOutputModules = new List<DigitalOutputModule>();
    List<string> TotalIOModules = new List<string>();
    public static int hwDiagIndex = 1;
    int majorIndex = 0; // Major index for ioStatus
    int minorIndex = 1; // Minor index for ioStatus
    int HMImajorIndex = 0; // Major index for HMIioStatus
    int HMIminorIndex = 1; // Minor index for HMIioStatus
    string ControllerNameGlobal = String.Empty;
    List<string> ReceiveTagGlobal = new List<string>();
    string? CoreRefXml = null;
    string? CLXConfigXml = null;
    string? PntConfigXml = null;
    string SourceTagInterlock = string.Empty;
    int CPU_Index_HMI = 0;
    List<string> HMI_HWDIAG_NET = new List<string>();
    List<string> HMI_HWDIAG_Node = new List<string>();
    List<string> HMI_IoStatus_Node = new List<string>();
    List<string> PLC_Rx_HMI_Unit = new List<string>();
    List<string> InterlockNewTagsHMI = new List<string>();
    List<NewInterlockTagDescription> newInterlockTags = new List<NewInterlockTagDescription>();
    List<NewInterlockParameter> newInterlockParameters = new List<NewInterlockParameter>();
    List<string> PointTypeIusys = new List<string>();
    List<string> FirstOperands = new List<string>();
    List<string> SecondOperands = new List<string>();
    Dictionary<string, string> hmiRouteToGroup = new Dictionary<string, string>();



    public V7ToV8ProgramsUpgradeEngine(L5XCollection collection, L5XCollection originalCollection, RockwellL5XProject proj)
    {
        Programs = (L5XPrograms)collection;
        OriginalPrograms = (L5XPrograms)originalCollection;
        Project = proj;
        HMI_INTERLOCK_MAX_INDEX = GetMaxIndexHMIInterlock();
        HMI_INTERLOCK_MAX_GROUP = GetMaxIndexHMIGroup();
        childPrograms = new XElement("ChildPrograms");
        InterlockTags = (L5XTags)Project.CreateElement("", "Tags", "");
        DSETags = (L5XTags)Project.CreateElement("", "Tags", "");
        MethodToDeleteFilesAppDataFolder();
    }

    private void MethodToDeleteFilesAppDataFolder()
    {
        string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AcesysConversion");

        if (Directory.Exists(appDataFolder))
        {
            try
            {                
                foreach (string file in Directory.GetFiles(appDataFolder))
                {
                    File.Delete(file);
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while deleting files: {ex.Message}");
            }
        }
        else
        {
            
        }
    }

    public override void ProcessOne2One(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        ProcessOne2OnePrograms(dbHelper, options, progress);
    }

    public override void ProcessOne2Many(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        ProcessOne2ManyPrograms(dbHelper, options, progress, New_Program_Index);
        
    }

    private string UpdateCoreRefXmlContent(
    string content,
    string controllerName,
    List<string> networkModules,
    List<string> nodeModules,
    List<string> analogInputModules,
    List<string> analogOutputModules,
    List<string> digitalInputModules,
    List<string> digitalOutputModules)
    {
        // Define the base entity structure with placeholders
        string cpuEntity = $@"
    <Entity>
        <Delta>{controllerName}CPU</Delta>
        <Description>{controllerName}CPU</Description>
    </Entity>";

        string netEntityTemplate = $@"
    <Entity>
        <Delta>{controllerName}NET_##NetName##</Delta>
        <Description>{controllerName}NET_##NetName##</Description>
    </Entity>";

        string nodeEntityTemplate = $@"
    <Entity>
        <Delta>{controllerName}DIAG_##NodeName##</Delta>
        <Description>{controllerName}DIAG_##NodeName##</Description>
    </Entity>";

        string moduleEntityTemplate = $@"
    <Entity>
        <Delta>{controllerName}DIAG_##ModuleName##</Delta>
        <Description>{controllerName}DIAG_##ModuleName##</Description>
    </Entity>";

        // Replace placeholders with actual values from the lists
        var netEntities = networkModules.Select(net => netEntityTemplate.Replace("##NetName##", net)).ToList();
        var nodeEntities = nodeModules.Select(node => nodeEntityTemplate.Replace("##NodeName##", node)).ToList();

        var analogInputEntities = analogInputModules.Select(module => moduleEntityTemplate.Replace("##ModuleName##", module)).ToList();
        var analogOutputEntities = analogOutputModules.Select(module => moduleEntityTemplate.Replace("##ModuleName##", module)).ToList();
        var digitalInputEntities = digitalInputModules.Select(module => moduleEntityTemplate.Replace("##ModuleName##", module)).ToList();
        var digitalOutputEntities = digitalOutputModules.Select(module => moduleEntityTemplate.Replace("##ModuleName##", module)).ToList();

        // Combine all entities into a single string with proper XML formatting
        string combinedEntities = cpuEntity + Environment.NewLine
            + string.Join(Environment.NewLine, netEntities) + Environment.NewLine
            + string.Join(Environment.NewLine, nodeEntities) + Environment.NewLine
            + string.Join(Environment.NewLine, analogInputEntities) + Environment.NewLine
            + string.Join(Environment.NewLine, analogOutputEntities) + Environment.NewLine
            + string.Join(Environment.NewLine, digitalInputEntities) + Environment.NewLine
            + string.Join(Environment.NewLine, digitalOutputEntities) + Environment.NewLine;

       

        // Load the existing XML content
        XmlDocument doc = new XmlDocument();
        try
        {
            doc.LoadXml(content);
        }
        catch (XmlException ex)
        {
            throw new Exception("Invalid XML content", ex);
        }

        XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("ns", doc.DocumentElement.NamespaceURI); // Adjust namespace prefix as needed

        // Locate the first <Entity> tag
        XmlNode firstEntityNode = doc.SelectSingleNode("//ns:Entity", nsManager);
        if (firstEntityNode != null)
        {
            // Find the <Delta> element within the first <Entity>
            XmlNode deltaNode = firstEntityNode.SelectSingleNode("ns:Delta", nsManager);
            if (deltaNode != null)
            {
                // Create a new document fragment with combined entities
                XmlDocumentFragment docFragment = doc.CreateDocumentFragment();
                docFragment.InnerXml = combinedEntities;

                // Insert the combined entities after the <Delta> element
                XmlNode parentNode = deltaNode.ParentNode;
                parentNode.InsertAfter(docFragment, deltaNode);
            }
        }

        // Return formatted XML
        using (StringWriter stringWriter = new StringWriter())
        using (XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter))
        {
            xmlTextWriter.Formatting = Formatting.Indented;
            doc.WriteTo(xmlTextWriter);
            return stringWriter.ToString();
        }
    }

    public string UpdateCLXNewInterlockPoints(string xmlContent, List<NewInterlockParameter> newInterlockParameters, string ControllerNameGlobal)
    {
        // Template for the <Points> element
        string pointsTemplate = @"
<Points>
    <Designation>##InterlockTag##</Designation>
    <PointCode>##InterlockTag##</PointCode>
    <IsAnalog>false</IsAnalog>
    <InpType>SINT</InpType>
    <InpAddr>##HMI_Interlock##</InpAddr>
    <OutputType1>NONE</OutputType1>
    <OutAddr1></OutAddr1>
    <OutputType2>NONE</OutputType2>
    <OutAddr2></OutAddr2>
    <ParameterType>NONE</ParameterType>
    <PrmAddr></PrmAddr>
    <PLC>##PLCName##</PLC>
</Points>";

        // Create a StringBuilder to hold the new <Points> elements
        StringBuilder pointsElements = new StringBuilder();

        // Generate the <Points> elements based on the NewInterlockParameter list
        foreach (var parameter in newInterlockParameters)
        {
            // Replace the placeholders in the template
            string pointsElement = pointsTemplate
                .Replace("##InterlockTag##", parameter.Name)
                .Replace("##HMI_Interlock##", parameter.HMI_Interlock)
                .Replace("##PLCName##", ControllerNameGlobal);

            // Append the generated <Points> element to the StringBuilder
            pointsElements.Append(pointsElement);
        }

        // Find the position where the new <Points> elements will be inserted before </Fls.Ecc.PLC.Config>
        int insertionPosition = xmlContent.IndexOf("</Fls.Ecc.CLX.Config>");
        if (insertionPosition == -1)
        {
            throw new Exception("</Fls.Ecc.PLC.Config> node not found in the XML content.");
        }

        // Insert the new <Points> elements before the </Fls.Ecc.PLC.Config> node
        xmlContent = xmlContent.Insert(insertionPosition, pointsElements.ToString());

        return xmlContent;
    }

    private string UpdatePntConfigDiagXmlContent(
    string content,
    string controllerName,
    List<string> networkModules,
    List<string> nodeModules,
    List<string> analogInputModules,
    List<string> analogOutputModules,
    List<string> digitalInputModules,
    List<string> digitalOutputModules)
    {
        // Define the base entity structures with placeholders
        string cpuEntity = $@"
<Entity>	<!-- POINTTYPEACESYSPLCSTATUSPLC_DIAGV28.0-->
    <Designation>{controllerName}CPU</Designation>
    <IoType>CLX</IoType>
    <PointTypeId>PointTypeAcesysPLCStatusPLC_DiagV28.0</PointTypeId>
    <TimeSeries>true</TimeSeries>
    <Compression>-1</Compression>
    <PiecewiseConstant>true</PiecewiseConstant>
    <DataType>2</DataType>
</Entity>";

        string netEntityTemplate = $@"
<Entity>	<!-- POINTTYPEACESYSPLCSTATUSRLX_NETDIAG8.0-->
    <Designation>{controllerName}NET_##NetName##</Designation>
    <IoType>CLX</IoType>
    <PointTypeId>PointTypeAcesysPLCStatusRLX_NetDiag8.0</PointTypeId>
    <TimeSeries>true</TimeSeries>
    <Compression>-1</Compression>
    <PiecewiseConstant>true</PiecewiseConstant>
    <DataType>2</DataType>
</Entity>";

        string nodeEntityTemplate = $@"
<Entity>	<!-- POINTTYPEACESYSPLCSTATUSRLX_NODEDIAG8.0-->
    <Designation>{controllerName}DIAG_##NodeName##</Designation>
    <IoType>CLX</IoType>
    <PointTypeId>PointTypeAcesysPLCStatusRLX_NodeDiag8.0</PointTypeId>
    <TimeSeries>true</TimeSeries>
    <Compression>-1</Compression>
    <PiecewiseConstant>true</PiecewiseConstant>
    <DataType>2</DataType>
</Entity>";

        string moduleEntityTemplate = $@"
<Entity>	<!-- POINTTYPEACESYSPLCSTATUSMODULEDIAG8.0-->
    <Designation>{controllerName}DIAG_##ModuleName##</Designation>
    <IoType>CLX</IoType>
    <PointTypeId>PointTypeAcesysPLCStatusModuleDiag8.0</PointTypeId>
    <TimeSeries>true</TimeSeries>
    <Compression>-1</Compression>
    <PiecewiseConstant>true</PiecewiseConstant>
    <DataType>2</DataType>
</Entity>";

        // Replace placeholders with actual values from the lists
        var netEntities = networkModules.Select(net => netEntityTemplate.Replace("##NetName##", net)).ToList();
        var nodeEntities = nodeModules.Select(node => nodeEntityTemplate.Replace("##NodeName##", node)).ToList();

        var analogInputEntities = analogInputModules.Select(module => moduleEntityTemplate.Replace("##ModuleName##", module)).ToList();
        var analogOutputEntities = analogOutputModules.Select(module => moduleEntityTemplate.Replace("##ModuleName##", module)).ToList();
        var digitalInputEntities = digitalInputModules.Select(module => moduleEntityTemplate.Replace("##ModuleName##", module)).ToList();
        var digitalOutputEntities = digitalOutputModules.Select(module => moduleEntityTemplate.Replace("##ModuleName##", module)).ToList();

        // Combine all entities into a single string with proper XML formatting
        string combinedEntities = cpuEntity + Environment.NewLine
            + string.Join(Environment.NewLine, netEntities) + Environment.NewLine
            + string.Join(Environment.NewLine, nodeEntities) + Environment.NewLine
            + string.Join(Environment.NewLine, analogInputEntities) + Environment.NewLine
            + string.Join(Environment.NewLine, analogOutputEntities) + Environment.NewLine
            + string.Join(Environment.NewLine, digitalInputEntities) + Environment.NewLine
            + string.Join(Environment.NewLine, digitalOutputEntities) + Environment.NewLine;

        // Load the existing XML content
        XmlDocument doc = new XmlDocument();
        try
        {
            doc.LoadXml(content);
        }
        catch (XmlException ex)
        {
            throw new Exception("Invalid XML content", ex);
        }

        // Insert the combined entities before the <UnitGroup> element
        XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("ns", doc.DocumentElement.NamespaceURI); // Adjust namespace prefix as needed

        // Locate the <UnitGroup> element
        XmlNode unitGroupNode = doc.SelectSingleNode("//ns:UnitGroup", nsManager);
        if (unitGroupNode != null)
        {
            // Create a new document fragment with combined entities
            XmlDocumentFragment docFragment = doc.CreateDocumentFragment();
            docFragment.InnerXml = combinedEntities;

            // Insert the combined entities before the <UnitGroup> element
            XmlNode parentNode = unitGroupNode.ParentNode;
            parentNode.InsertBefore(docFragment, unitGroupNode);
        }

        // Return formatted XML
        using (StringWriter stringWriter = new StringWriter())
        using (XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter))
        {
            xmlTextWriter.Formatting = Formatting.Indented;
            doc.WriteTo(xmlTextWriter);
            return stringWriter.ToString();
        }
    }

    public string UpdateCoreRefNewInterlockEntity(string xmlContent, List<string> interlockNewTagsHMI)
    {
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xmlContent);

        // Define the <Entity> structure with placeholders
        string entityTemplate = @"
<Entity>
    <Delta>##InterlockTag##</Delta>
    <Description>##InterlockTag##</Description>
</Entity>";

        // Create a document fragment to hold the new <Entity> elements
        XmlDocumentFragment docFragment = doc.CreateDocumentFragment();

        // Generate the <Entity> elements based on the InterlockNewTagsHMI list
        foreach (var interlockTag in interlockNewTagsHMI)
        {
            docFragment.InnerXml += entityTemplate.Replace("##InterlockTag##", interlockTag);
        }

        // Find the <EntityRelation> node to insert the <Entity> elements before it
        XmlNode entityRelationNode = doc.SelectSingleNode("//EntityRelation");

        if (entityRelationNode != null)
        {
            // Insert the new <Entity> elements before the <EntityRelation> node
            entityRelationNode.ParentNode.InsertBefore(docFragment, entityRelationNode);
        }
        else
        {
            // If no <EntityRelation> node is found, append the <Entity> elements at the root level or at a specified location
            doc.DocumentElement.AppendChild(docFragment);
        }

        // Return the updated XML content as a string
        using (var stringWriter = new StringWriter())
        using (var xmlTextWriter = XmlWriter.Create(stringWriter))
        {
            doc.WriteTo(xmlTextWriter);
            xmlTextWriter.Flush();
            return stringWriter.GetStringBuilder().ToString();
        }
    }

    public string UpdatePntConfigXMLWithNewInterlockEntities(string xmlContent, List<NewInterlockParameter> newInterlockParameters, DbHelper dbHelper)
    {
        // Template for the <Entity> element
        string entityTemplate = @"
<Entity>   <!-- POINTTYPEACESYSINTERLOCK##LinkType##8.0-->
    <Designation>##InterlockTag##</Designation>
    <IoType>CLX</IoType>
    <PointTypeId>PointTypeAcesysInterlock##LinkType##8.0</PointTypeId>
    <TimeSeries>true</TimeSeries>
    <Compression>-1</Compression>
    <PiecewiseConstant>true</PiecewiseConstant>
    <DataType>2</DataType>
    <Reference>
        <Item>InterlockSource</Item>
        <ReferenceTypeId>RefPoint1</ReferenceTypeId>
        <ReferenceId>##SourceTag##</ReferenceId>
    </Reference>
</Entity>";

        // Create a StringBuilder to hold the new <Entity> elements
        StringBuilder entityElements = new StringBuilder();

        // Generate the <Entity> elements based on the NewInterlockParameter list
        foreach (var parameter in newInterlockParameters)
        {
            // Get the SourceTag for the current parameter
            string sourceTag = MethodToNewInterlockSouceTag(parameter.Name);

            // Replace the placeholders in the entity template
            string entityElement = entityTemplate
                .Replace("##InterlockTag##", parameter.Name)
                .Replace("##LinkType##", parameter.LINK)
                .Replace("##SourceTag##", sourceTag); // Replace the SourceTag placeholder

            // Append the entity element to the StringBuilder
            entityElements.Append(entityElement);
        }

        // Find the position of the <Language> node to insert the new <Entity> elements before it
        int languageNodePosition = xmlContent.IndexOf("<Language>");
        if (languageNodePosition == -1)
        {
            throw new Exception("<Language> node not found in the XML content.");
        }

        // Insert the new <Entity> elements before the <Language> node
        xmlContent = xmlContent.Insert(languageNodePosition, entityElements.ToString());

        return xmlContent;
    }

    private string MethodToNewInterlockSouceTag(string name)
    {
        string result = name + ".IN";

        result = $"OTE({result})";

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

                        if (textContent.Contains(result))
                        {
                            var match2 = Regex.Match(textContent, @"\b(?:XIC|XIO)\(([^)]+)\)");

                            if (match2.Success)
                            {
                                string extractcontent = match2.Groups[1].Value;

                                string result2 = Regex.Replace(extractcontent, @"_.+$", "");

                                return result2;
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    public string UpdateCoreRefXMLNewInterlockRelations(string xmlContent, List<string> interlockNewTagsHMI)
{
    // Define the <EntityRelation> structure with a placeholder
    string entityRelationTemplate = @"
<EntityRelation>   <!-- Interlock Begin -->
    <EntityId>##InterlockTag##</EntityId>
</EntityRelation>  <!-- Interlock End -->";

    // Create a StringBuilder to hold the new <EntityRelation> elements
    StringBuilder newEntityRelations = new StringBuilder();

    // Generate the <EntityRelation> elements based on the InterlockNewTagsHMI list
    foreach (var interlockTag in interlockNewTagsHMI)
    {
        string entityRelationEntry = entityRelationTemplate.Replace("##InterlockTag##", interlockTag);
        newEntityRelations.Append(entityRelationEntry);
    }

    // Find the index of the closing </EntityRelation> tag or a similar marker if needed
    int closingTagIndex = xmlContent.LastIndexOf("</EntityRelation>");
    if (closingTagIndex == -1)
    {
        throw new Exception("Closing </EntityRelation> tag not found in the XML content.");
    }

    // Insert the new <EntityRelation> elements before the closing </EntityRelation> tag
    string updatedContent = xmlContent.Insert(closingTagIndex, newEntityRelations.ToString());

    return updatedContent;
}

    public string UpdateCoreRefLanguageReceivedXmlContent(string coreRefXmlContent, List<string> receiveTagGlobal)
    {
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(coreRefXmlContent);

        // Find the </EntityRelation> closing node
        XmlNode? entityRelationNode = doc.SelectSingleNode("//EntityRelation");

        if (entityRelationNode == null)
        {
            throw new InvalidOperationException("The <EntityRelation> node was not found in the XML content.");
        }

        // Iterate over each ReceiveTagGlobal item and create the corresponding <Language> element
        foreach (var receiveTag in receiveTagGlobal)
        {
            // Create the <Language> element
            XmlElement languageElement = doc.CreateElement("Language");

            // Create and append the <Designation> element
            XmlElement designationElement = doc.CreateElement("Designation");
            designationElement.InnerText = receiveTag;
            languageElement.AppendChild(designationElement);

            // Create and append the <LanguageText> element
            XmlElement languageTextElement = doc.CreateElement("LanguageText");
            languageTextElement.InnerText = "PLC Receive";
            languageElement.AppendChild(languageTextElement);

            // Insert the <Language> element after the </EntityRelation> node
            entityRelationNode.ParentNode?.InsertAfter(languageElement, entityRelationNode);
        }

        // Return the updated XML content as a string
        using (var stringWriter = new StringWriter())
        using (var xmlTextWriter = XmlWriter.Create(stringWriter))
        {
            doc.WriteTo(xmlTextWriter);
            xmlTextWriter.Flush();
            return stringWriter.GetStringBuilder().ToString();
        }
    }

    private string UpdateCoreRefEntityRelationCPUXmlContent(
    string content,
    string controllerNameGlobal,
    List<string> networkModules,
    List<string> nodeModules,
    List<string> analogInputModules,
    List<string> analogOutputModules,
    List<string> digitalInputModules,
    List<string> digitalOutputModules)
    {
        // Define the base EntityRelation structure with placeholders
        string entityRelationTemplate = @"
<EntityRelation>
    <EntityId>##PLCName##CPU</EntityId>
    ##NetEntities##
</EntityRelation>";

        string netEntityTemplate = @"
    <EntityRelation>
        <EntityId>##PLCName##NET_##NetName##</EntityId>
        ##NodeEntities##
    </EntityRelation>";

        string nodeEntityTemplate = @"
        <EntityRelation>
            <EntityId>##PLCName##DIAG_##NodeName##</EntityId>
            ##ModuleEntities##
        </EntityRelation>";

        string moduleEntityTemplate = @"
            <EntityRelation>
                <EntityId>##PLCName##DIAG_##ModuleName##</EntityId>
            </EntityRelation>";

        // Replace placeholders for ##PLCName##
        entityRelationTemplate = entityRelationTemplate.Replace("##PLCName##", controllerNameGlobal);
        netEntityTemplate = netEntityTemplate.Replace("##PLCName##", controllerNameGlobal);
        nodeEntityTemplate = nodeEntityTemplate.Replace("##PLCName##", controllerNameGlobal);
        moduleEntityTemplate = moduleEntityTemplate.Replace("##PLCName##", controllerNameGlobal);

        // Create module entities
        var moduleEntities = new List<string>();
        moduleEntities.AddRange(analogInputModules.Select(module => moduleEntityTemplate.Replace("##ModuleName##", module)));
        moduleEntities.AddRange(analogOutputModules.Select(module => moduleEntityTemplate.Replace("##ModuleName##", module)));
        moduleEntities.AddRange(digitalInputModules.Select(module => moduleEntityTemplate.Replace("##ModuleName##", module)));
        moduleEntities.AddRange(digitalOutputModules.Select(module => moduleEntityTemplate.Replace("##ModuleName##", module)));

        // Create node entities
        var nodeEntities = nodeModules.Select(node =>
            nodeEntityTemplate.Replace("##NodeName##", node)
                              .Replace("##ModuleEntities##", string.Join(Environment.NewLine, moduleEntities)))
                              .ToList();

        // Create net entities
        var netEntities = networkModules.Select(net =>
            netEntityTemplate.Replace("##NetName##", net)
                             .Replace("##NodeEntities##", string.Join(Environment.NewLine, nodeEntities)))
                             .ToList();

        // Combine everything into the final EntityRelation structure
        string finalEntityRelation = entityRelationTemplate.Replace("##NetEntities##", string.Join(Environment.NewLine, netEntities));

        // Load the existing XML content
        XmlDocument doc = new XmlDocument();
        try
        {
            doc.LoadXml(content);
        }
        catch (XmlException ex)
        {
            throw new Exception("Invalid XML content", ex);
        }

        // Locate the last <EntityRelation> tag or <Fls.Core.Ref> as fallback
        XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("ns", doc.DocumentElement.NamespaceURI); // Adjust namespace prefix as needed

        // Locate the last <EntityRelation> tag
        XmlNode lastEntityNode = doc.SelectSingleNode("//ns:EntityRelation[last()]", nsManager);

        // Create a new document fragment with the final EntityRelation content
        XmlDocumentFragment docFragment = doc.CreateDocumentFragment();
        docFragment.InnerXml = finalEntityRelation;

        if (lastEntityNode == null)
        {
            // If no <EntityRelation> is found, fallback to the <Fls.Core.Ref> node
            lastEntityNode = doc.SelectSingleNode("//ns:Fls.Core.Ref", nsManager);

            if (lastEntityNode != null)
            {
                // Append the new entity relations before the closing </Fls.Core.Ref> tag
                lastEntityNode.AppendChild(docFragment);
            }
            else
            {
                throw new Exception("Neither <EntityRelation> nor <Fls.Core.Ref> tag found in the XML content.");
            }
        }
        else
        {
            // Insert the new entity relations after the last <EntityRelation> tag
            lastEntityNode.ParentNode.InsertAfter(docFragment, lastEntityNode);
        }

        // Return formatted XML
        using (StringWriter stringWriter = new StringWriter())
        using (XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter))
        {
            xmlTextWriter.Formatting = Formatting.Indented;
            doc.WriteTo(xmlTextWriter);
            return stringWriter.ToString();
        }
    }

    private string UpdateCoreRefEntityReceivedXmlContent(
    string content,
    List<string> receiveTags)
    {
        // Define the base Entity structure with placeholders
        string entityTemplate = @"
<Entity>
    <Delta>##ReceiveTag##</Delta>
    <Description>##ReceiveTag##</Description>
    <Importance>3</Importance>
</Entity>";

        // Replace placeholders with actual values from the receiveTags list
        var entityEntries = receiveTags.Select(tag =>
            entityTemplate.Replace("##ReceiveTag##", tag)).ToList();

        // Combine all Entity entries into a single string with proper XML formatting
        string combinedEntities = string.Join(Environment.NewLine, entityEntries);

        // Load the existing XML content
        XmlDocument doc = new XmlDocument();
        try
        {
            doc.LoadXml(content);
        }
        catch (XmlException ex)
        {
            throw new Exception("Invalid XML content", ex);
        }

        // Create a new document fragment with combined entities
        XmlDocumentFragment docFragment = doc.CreateDocumentFragment();
        docFragment.InnerXml = combinedEntities;

        // Namespace management       

        XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("ns", doc.DocumentElement.NamespaceURI); 

        // Locate the first <EntityRelation> tag
        XmlNode entityRelationNode = doc.SelectSingleNode("//ns:EntityRelation", nsManager);

        if (entityRelationNode != null)
        {
            entityRelationNode = doc.SelectSingleNode("//EntityRelation");
            if (entityRelationNode == null)
            {
                throw new Exception("The <EntityRelation> element could not be found in the provided XML content.");
            }
        }

        // Insert the combined entities before the start of <EntityRelation>
        entityRelationNode.ParentNode.InsertBefore(docFragment, entityRelationNode);

        // Return the formatted XML
        using (StringWriter stringWriter = new StringWriter())
        using (XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter))
        {
            xmlTextWriter.Formatting = Formatting.Indented;
            doc.WriteTo(xmlTextWriter);
            return stringWriter.ToString();
        }
    }

    private string UpdateClxConfigPointsXmlContent(
    string content,
    string controllerName,
    List<string> networkModules,
    List<string> nodeModules,
    List<string> analogInputModules,
    List<string> analogOutputModules,
    List<string> digitalInputModules,
    List<string> digitalOutputModules)
    {
        // Define the base Points structure for CPU with dynamic replacements
        string cpuPoints = $@"
<Points>
    <Designation>{controllerName}CPU</Designation>
    <PointCode>{controllerName}CPU</PointCode>
    <IsAnalog>false</IsAnalog>
    <InpType>MSW32TIME</InpType>
    <InpAddr>HMI_UNIT[{CPU_Index_HMI}]</InpAddr>
    <OutputType1>MSW16</OutputType1>
    <OutAddr1>{controllerName}CPU_FP.Faceplate</OutAddr1>
    <OutputType2>NONE</OutputType2>
    <OutAddr2></OutAddr2>
    <ParameterType>PARAMBLOCK</ParameterType>
    <PrmAddr>{controllerName}CPU_FP</PrmAddr>
    <PLC>{controllerName}</PLC>
</Points>";

        // Define the Points template for network modules
        string netPointsTemplate = $@"
<Points>
    <Designation>{controllerName}NET_##NetName##</Designation>
    <PointCode>{controllerName}NET_##NetName##</PointCode>
    <IsAnalog>false</IsAnalog>
    <InpType>MSW16</InpType>
    <InpAddr>HMI_HWDIAG[###]</InpAddr>
    <OutputType1>NONE</OutputType1>
    <OutAddr1></OutAddr1>
    <OutputType2>NONE</OutputType2>
    <OutAddr2></OutAddr2>
    <ParameterType>NONE</ParameterType>
    <PrmAddr></PrmAddr>
    <PLC>{controllerName}</PLC>
</Points>";

        // Define the Points template for node modules
        string nodePointsTemplate = $@"
<Points>
    <Designation>{controllerName}DIAG_##NodeName##</Designation>
    <PointCode>{controllerName}DIAG_##NodeName##</PointCode>
    <IsAnalog>false</IsAnalog>
    <InpType>MSW16</InpType>
    <InpAddr>HMI_HWDIAG[###]</InpAddr>
    <OutputType1>NONE</OutputType1>
    <OutAddr1></OutAddr1>
    <OutputType2>NONE</OutputType2>
    <OutAddr2></OutAddr2>
    <ParameterType>NONE</ParameterType>
    <PrmAddr></PrmAddr>
    <PLC>{controllerName}</PLC>
</Points>";

        // Define the Points template for other modules (analog and digital)
        string modulePointsTemplate = $@"
<Points>
    <Designation>{controllerName}DIAG_##ModuleName##</Designation>
    <PointCode>{controllerName}DIAG_##ModuleName##</PointCode>
    <IsAnalog>false</IsAnalog>
    <InpType>MSW16</InpType>
    <InpAddr>HMI_IoStatus##HMI_IoStatus##</InpAddr>
    <OutputType1>NONE</OutputType1>
    <OutAddr1></OutAddr1>
    <OutputType2>NONE</OutputType2>
    <OutAddr2></OutAddr2>
    <ParameterType>NONE</ParameterType>
    <PrmAddr></PrmAddr>
    <PLC>{controllerName}</PLC>
</Points>";

        // Initialize the current index to start numbering Points nodes
        int currentIndex = 1;

        // Replace placeholders with actual values from the lists and set the InpAddr field
        var netPoints = networkModules.Select(net =>
        {
            string point = netPointsTemplate.Replace("##NetName##", net)
                                            .Replace("HMI_HWDIAG[###]", $"HMI_HWDIAG[{currentIndex.ToString("D3")}]");
            currentIndex++; // Increment the index for each new entry
            return point;
        }).ToList();

        var nodePoints = nodeModules.Select(node =>
        {
            string point = nodePointsTemplate.Replace("##NodeName##", node)
                                             .Replace("HMI_HWDIAG[###]", $"HMI_HWDIAG[{currentIndex.ToString("D3")}]");
            currentIndex++; // Increment the index for each new entry
            return point;
        }).ToList();

        var analogInputPoints = analogInputModules.Select(module =>
            modulePointsTemplate.Replace("##ModuleName##", module)
                                .Replace("##HMI_IoStatus##", GenerateHMIIoStatusIndex())).ToList();

        var analogOutputPoints = analogOutputModules.Select(module =>
            modulePointsTemplate.Replace("##ModuleName##", module)
                                .Replace("##HMI_IoStatus##", GenerateHMIIoStatusIndex())).ToList();

        var digitalInputPoints = digitalInputModules.Select(module =>
            modulePointsTemplate.Replace("##ModuleName##", module)
                                .Replace("##HMI_IoStatus##", GenerateHMIIoStatusIndex())).ToList();

        var digitalOutputPoints = digitalOutputModules.Select(module =>
            modulePointsTemplate.Replace("##ModuleName##", module)
                                .Replace("##HMI_IoStatus##", GenerateHMIIoStatusIndex())).ToList();

        // Combine all Points into a single string with proper XML formatting
        string combinedPoints = cpuPoints + Environment.NewLine
            + string.Join(Environment.NewLine, netPoints) + Environment.NewLine
            + string.Join(Environment.NewLine, nodePoints) + Environment.NewLine
            + string.Join(Environment.NewLine, analogInputPoints) + Environment.NewLine
            + string.Join(Environment.NewLine, analogOutputPoints) + Environment.NewLine
            + string.Join(Environment.NewLine, digitalInputPoints) + Environment.NewLine
            + string.Join(Environment.NewLine, digitalOutputPoints) + Environment.NewLine;


        // Load the existing XML content
        XmlDocument doc = new XmlDocument();
        try
        {
            doc.LoadXml(content);
        }
        catch (XmlException ex)
        {
            throw new Exception("Invalid XML content", ex);
        }

        // Insert the combined Points before the <PLCs> element
        XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("ns", doc.DocumentElement.NamespaceURI); // Adjust namespace prefix as needed

        // Locate the <PLCs> element
        XmlNode configNode = doc.SelectSingleNode("//PLCs", nsManager);
        if (configNode != null)
        {
            // Create a new document fragment with combined Points
            XmlDocumentFragment docFragment = doc.CreateDocumentFragment();
            docFragment.InnerXml = combinedPoints;

            // Insert the combined Points before the <PLCs> element
            XmlNode parentNode = configNode.ParentNode;
            parentNode.InsertAfter(docFragment, configNode);
        }

        // Return formatted XML
        using (StringWriter stringWriter = new StringWriter())
        using (XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter))
        {
            xmlTextWriter.Formatting = Formatting.Indented;
            doc.WriteTo(xmlTextWriter);
            return stringWriter.ToString();
        }
    }

    private string GenerateHMIIoStatusIndex()
    {        
        string index = $"[{HMImajorIndex:D2}].{HMIminorIndex:D2}";

        if (HMIminorIndex < 15)
        {
            HMIminorIndex++;
        }
        else
        {
            HMImajorIndex++;
            HMIminorIndex = 0;
        }
        return index;
    }

    public string UpdatePLCConfigReceiveXmlContent(
    string xmlContent,
    List<string> receiveTagGlobal,
    List<string> plcRxHmiUnit,
    string controllerNameGlobal)
    {
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xmlContent);

        // Define the new <Points> structure with placeholders
        string newPointsTemplate = $@"
<Points>
    <Designation>##ReceiveTag##</Designation>
    <PointCode>##ReceiveTag##</PointCode>
    <IsAnalog>false</IsAnalog>
    <InpType>MSW32TIME</InpType>
    <InpAddr>##RuleHMI01##</InpAddr>					
    <OutputType1>MSW16</OutputType1>
    <OutAddr1>##RuleHMI02##</OutAddr1>					
    <OutputType2>NONE</OutputType2>
    <OutAddr2></OutAddr2>					
    <ParameterType>PARAMBLOCK</ParameterType>
    <PrmAddr>##RuleHMI03##</PrmAddr>
    <PLC>##PLCName##</PLC>
</Points>";

        // Find the <Fls.Ecc.CLX.Config> node
        XmlNode flsEccNode = doc.SelectSingleNode("//Fls.Ecc.CLX.Config");

        if (flsEccNode == null)
        {
            // Return the original content if <Fls.Ecc.CLX.Config> node is not found
            return xmlContent;
        }

        // Create a document fragment with the new <Points> elements
        XmlDocumentFragment docFragment = doc.CreateDocumentFragment();

        for (int i = 0; i < receiveTagGlobal.Count; i++)
        {
            var receiveTag = receiveTagGlobal[i];
            var ruleHmi01 = plcRxHmiUnit[i];
            var ruleHmi02 = receiveTag + "_FP.Faceplate";
            var ruleHmi03 = receiveTag + "_FP";

            docFragment.InnerXml += newPointsTemplate
                .Replace("##ReceiveTag##", receiveTag)
                .Replace("##PLCName##", controllerNameGlobal)
                .Replace("##RuleHMI01##", ruleHmi01)
                .Replace("##RuleHMI02##", ruleHmi02)
                .Replace("##RuleHMI03##", ruleHmi03);
        }

        // Insert the new <Points> element before the end of <Fls.Ecc.CLX.Config>
        XmlNode endNode = flsEccNode.SelectSingleNode(".");

        if (endNode != null)
        {
            flsEccNode.InsertBefore(docFragment, endNode.NextSibling);
        }

        // Return the updated XML content as a string
        using (var stringWriter = new StringWriter())
        using (var xmlTextWriter = XmlWriter.Create(stringWriter))
        {
            doc.WriteTo(xmlTextWriter);
            xmlTextWriter.Flush();
            return stringWriter.GetStringBuilder().ToString();
        }
    }

    private string GetNewFileName(string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        if (Regex.IsMatch(fileName, @"Fls\.Core\.Ref\.xml", RegexOptions.IgnoreCase))
        {
            return "Core.Ref.xml";
        }
        if (Regex.IsMatch(fileName, @"Fls\.Ecc\.PLC\.Config\.xml", RegexOptions.IgnoreCase) || Regex.IsMatch(fileName, @"Fls\.Ecc\.CLX\.Config\.xml", RegexOptions.IgnoreCase))
        {
            return "CLX.Config.xml";
        }
        if (Regex.IsMatch(fileName, @"Fls\.Ecc\.Pnt\.Config\.xml", RegexOptions.IgnoreCase))
        {
            return "Pnt.Config.xml";
        }

        return string.Empty; // Return empty if no match is found
    }

    private void ProcessOne2OnePrograms(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        string filePath = options.NewFileName;

        if (filePath != string.Empty)
        {
            xmlDocNew = new XmlDocument();
            xmlDocNew.Load(filePath);
        }       

        List<Dto> one2one = dbHelper.GetOneToOneMainRoutines(options);
        var programsToModify = Project.Content?.Controller?.Programs;
        string oldRoutineName = string.Empty;
        int sequence = 0; // Replace with the appropriate sequence value
        ClearInterlockXmlNodeList();

        // Initialize the XmlDocument
        XmlDocument xmlDoc = new XmlDocument();

        if (programsToModify != null && OriginalPrograms != null)
        {
            // Add MasterProgram node
            AddMasterProgramNode(programsToModify, progress);
            AddPLCtoPLCProgramNode(programsToModify, progress);

            foreach (L5XProgram originalProgram in OriginalPrograms)
            {              

                //programsToModify.PrependChild(programsToModify.OwnerDocument.ImportNode(originalProgram, true));
                foreach (L5XRoutine routine in originalProgram.Routines)
                {
                    progress.Report($"Processing Program {originalProgram.ProgramName}. Converting Routine {routine.RoutineName} to Program");

                    if (routine.RoutineName.Equals("Dispatcher"))
                    {
                        continue;
                    }                    

                    string Routinename = routine.RoutineName;

                    L5XTag? associatedTag = originalProgram.GetAssociatedTagForRoutine(routine.RoutineName);
                    string dataType = associatedTag?.DataType ?? string.Empty;                    

                    bool dataTypeExists = one2one.Any(dto => dto.FromObject == dataType);

                    if (dataType == "AsysE_Ga1")
                    {
                        dataType = "AsysE_Ga";
                        dataTypeExists = true;
                    }

                    if (Routinename == "DevSimulation")
                    {
                        dataType = "Simulation";
                        dataTypeExists = true;
                    }

                    if (dataType == "AsysComp")
                    {
                        dataType = "AsysComp";
                        dataTypeExists = true;
                    }

                    if (dataType == "AsysPfister")
                    {
                        dataType = "Pfister";
                        dataTypeExists = true;
                    }

                    if (dataType == "AsysDosax")
                    {
                        dataType = "Dosax";
                        dataTypeExists = true;
                    }                    

                    if (dataType == "AsysPIACSV2")
                    {
                        dataType = "PIACS";
                        dataTypeExists = true;
                    }

                    if (routine.RoutineName.IndexOf("INDICATIONS", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        dataTypeExists = true;
                        dataType = "Indication";
                    }                    

                    if (routine.RoutineName.IndexOf("RECIPE", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        dataTypeExists = true;
                        dataType = "AsysRcp";
                    }

                    if (routine.RoutineName.IndexOf("TOTALIZER", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        dataTypeExists = true;
                        dataType = "AsysTotal";
                    }

                    if (routine.RoutineName.IndexOf("SCHENCK", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        dataTypeExists = true;
                        dataType = "Schenck";
                    }

                    if (routine.RoutineName.IndexOf("SPCFILTER", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        dataTypeExists = true;
                        dataType = "SPC";
                    }

                    if (routine.RoutineName.IndexOf("SEQUENCE", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        dataTypeExists = true;
                        dataType = "Sequence";
                    }

                    if (routine.RoutineName.IndexOf("PIACS", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        dataTypeExists = true;
                        dataType = "PIACS";
                    }

                    if (routine.RoutineName.IndexOf("PIACS", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        dataTypeExists = true;
                        dataType = "PIACS";
                    }

                    if (routine.RoutineName.IndexOf("HLC", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        dataTypeExists = true;
                        dataType = "AsysHLC";
                    }

                    if (routine.RoutineName.Equals("_542ER1_TT1"))
                    {
                        dataTypeExists = true;
                        dataType = routine.RoutineName;
                    }

                    if (routine.RoutineName.Equals("GOC_BIMOTOR01"))
                    {
                        dataTypeExists = true;
                        dataType = "AsysMot2";
                    }

                    if (routine.RoutineName.Equals("GOC_OPERATORSP"))
                    {
                        dataTypeExists = true;
                        dataType = "HMI_OPERATOR_SP";
                    }

                    if (dataTypeExists)
                    {
                        if (string.IsNullOrEmpty(dataType))
                        {
                            dataType = "Main";
                            oldRoutineName = routine.RoutineName;
                            routine.RoutineName = "Main";
                        }
                        else if (dataType == "Indication")
                        {
                            dataType = "Indication";
                        }

                        else if (dataType == "Simulation")
                        {
                            dataType = "Simulation";
                        }

                        else if (dataType == "AsysE_Ga")
                        {
                            dataType = "AsysE_Ga";
                        }
                        else if (dataType == "Schenck")
                        {
                            dataType = "Schenck";
                        }
                        else if (dataType == "Pfister")
                        {
                            dataType = "Pfister";
                        }

                        else if (dataType == "AsysComp")
                        {
                            dataType = "AsysComp";
                        }

                        else if (dataType == "Sequence")
                        {
                            dataType = "Sequence";
                        }

                        else if (dataType == "PIACS")
                        {
                            dataType = "PIACS";
                        }

                        else if (dataType == "Dosax")
                        {
                            dataType = "Dosax";
                        }

                        else if (dataType == "SPC")
                        {
                            dataType = "SPC";
                        }
                        
                        else
                        {
                            dataType = GetMainRoutineName(one2one, dataType);
                        }

                        L5XProgram newProgram = CreateProgramFromRoutine(dataType, routine, programsToModify, dbHelper, options, New_Program_Index, Routinename, xmlDocNew, progress);
                        New_Program_Index = New_Program_Index + 1;

                        if (originalProgram.ProgramName.Equals(routine.RoutineName))
                        {
                            // Create ChildPrograms node and append ChildProgram nodes to it
                            XElement childPrograms = new XElement("ChildPrograms");

                            foreach (L5XRoutine childRoutine in originalProgram.Routines)
                            {
                                if (childRoutine.RoutineName.Equals("Dispatcher") || originalProgram.ProgramName.Equals(childRoutine.RoutineName))
                                {
                                    continue;
                                }

                                // Create ChildProgram node and add it to ChildPrograms node
                                XElement childProgramElement = new XElement("ChildProgram", new XAttribute("Name", childRoutine.RoutineName));
                                childPrograms.Add(childProgramElement);
                            }

                            // Create XmlDocument
                            XmlDocument xmlDocument = new XmlDocument();
                            // Convert XElement to XmlNode
                            XmlNode importedChildPrograms = xmlDocument.ReadNode(childPrograms.CreateReader());
                            // Import node into context of newProgram document
                            XmlNode newChildProgramsNode = newProgram.OwnerDocument.ImportNode(importedChildPrograms, true);
                            // Append imported node
                            newProgram.AppendChild(newChildProgramsNode);

                            // Convert newChildProgramsNode to L5XChildProgram instances and process them
                            foreach (XmlNode childProgramNode in newChildProgramsNode.ChildNodes)
                            {
                                if (childProgramNode.NodeType == XmlNodeType.Element && childProgramNode.Name == "ChildProgram")
                                {
                                    L5XChildProgram l5xChildProgram = L5XChildProgram.FromXmlNode(childProgramNode, newProgram.OwnerDocument, sequence);
                                    // Increment sequence number for each child program
                                    sequence++;

                                    L5XCollection.AddUserMessage(Project, l5xChildProgram, null, UserMessageTypes.Information,
                                        $"Child Program ", "O2O", $"Child Program has been added to Program name {routine.RoutineName}");

                                    // If you need to add l5xChildProgram to a collection or perform other operations, do it here
                                    // For example, adding to a collection
                                    // l5xChildProgramCollection.Add(l5xChildProgram);
                                }
                            }
                        }                       

                        programsToModify.AppendChild(newProgram);

                        if (routine.RoutineName.Equals("GOC_DEPT01"))
                        {
                            MethodToAddChildProgramToDepartment(DepartmentName, GroupName, progress);
                        }

                        if (dataType.Equals("Department", StringComparison.OrdinalIgnoreCase))
                        {
                            // Add empty ChildPrograms node if dataType is "Department"
                            XmlNode emptyChildProgramsNode = newProgram.OwnerDocument.CreateElement("ChildPrograms");
                            XmlNode textNode = newProgram.OwnerDocument.CreateTextNode(string.Empty); // Add this line to ensure non-self-closing element
                            emptyChildProgramsNode.AppendChild(textNode);
                            newProgram.AppendChild(emptyChildProgramsNode);

                            MethodToAddChildProgramToMasterProgram(routine.RoutineName,progress);
                        }

                        if (associatedTag == null)
                        {
                            L5XCollection.AddUserMessage(Project, newProgram, routine, UserMessageTypes.Information,
                                                        $"Routine {oldRoutineName} Not Associated With Tag Naming to Main", "O2O");
                        }
                    }
                }
            }
        }
        
    }

    private void AddMasterProgramNode(XmlNode parentNode, IProgress<string> progress)
    {
        docNew = XDocument.Parse(Project.InnerXml);
        ControllerName = docNew.Root.Attribute("TargetName")?.Value + "Master";
        XmlDocument xmlDoc = parentNode.OwnerDocument;
        XmlElement masterProgram = xmlDoc.CreateElement("Program");
        masterProgram.SetAttribute("Name", ControllerName);
        masterProgram.SetAttribute("TestEdits", "false");
        masterProgram.SetAttribute("MainRoutineName", "Dispatcher");
        masterProgram.SetAttribute("Disabled", "false");
        masterProgram.SetAttribute("UseAsFolder", "false");

        // Create blank ChildPrograms node and append it to MasterProgram
        XmlElement childPrograms = xmlDoc.CreateElement("ChildPrograms");
        XmlNode textNode = xmlDoc.CreateTextNode(string.Empty); // Add an empty text node inside to prevent self-closing
        childPrograms.AppendChild(textNode);
        masterProgram.AppendChild(childPrograms);

        // Append MasterProgram as the first child of parentNode
        parentNode.PrependChild(masterProgram);

        progress.Report($"Master Program names {ControllerName} has been added");

    }

    private void AddPLCtoPLCProgramNode(XmlNode parentNode, IProgress<string> progress)
    {
        XmlDocument xmlDoc = parentNode.OwnerDocument;
        XmlElement plcToPlcProgram = xmlDoc.CreateElement("Program");
        plcToPlcProgram.SetAttribute("Name", "PLCtoPLC");
        plcToPlcProgram.SetAttribute("TestEdits", "false");
        plcToPlcProgram.SetAttribute("Disabled", "false");
        plcToPlcProgram.SetAttribute("UseAsFolder", "true");

        // Create and append blank Tags node
        XmlElement tags = xmlDoc.CreateElement("Tags");
        XmlNode tagsTextNode = xmlDoc.CreateTextNode(string.Empty); // Add an empty text node inside to prevent self-closing
        tags.AppendChild(tagsTextNode);
        plcToPlcProgram.AppendChild(tags);

        // Create and append blank Routines node
        XmlElement routines = xmlDoc.CreateElement("Routines");
        XmlNode routinesTextNode = xmlDoc.CreateTextNode(string.Empty); // Add an empty text node inside to prevent self-closing
        routines.AppendChild(routinesTextNode);
        plcToPlcProgram.AppendChild(routines);

        // Create and append blank ChildPrograms node
        XmlElement childPrograms = xmlDoc.CreateElement("ChildPrograms");
        XmlNode childProgramsTextNode = xmlDoc.CreateTextNode(string.Empty); // Add an empty text node inside to prevent self-closing
        childPrograms.AppendChild(childProgramsTextNode);
        plcToPlcProgram.AppendChild(childPrograms);

        // Append PLCtoPLC Program as the first child of parentNode
        parentNode.AppendChild(plcToPlcProgram);

        progress.Report("PLCtoPLC Program has been added");
    }

    private XmlNode CreateHWDiagProgram(XmlDocument doc, IProgress<string> progress)
    {
        XmlElement hwDiagProgram;
        if (doc != null)
        {
            // Create the HW_Diag program element
            hwDiagProgram = doc.CreateElement("Program");
            hwDiagProgram.SetAttribute("Name", "HW_Diag");
            hwDiagProgram.SetAttribute("TestEdits", "false");
            hwDiagProgram.SetAttribute("Disabled", "false");
            hwDiagProgram.SetAttribute("UseAsFolder", "true");

            // Create and append the Tags element
            XmlElement tags = doc.CreateElement("Tags");
            hwDiagProgram.AppendChild(tags);

            // Create and append the Routines element
            XmlElement routines = doc.CreateElement("Routines");
            hwDiagProgram.AppendChild(routines);

            // Create and append the ChildPrograms element
            XmlElement childPrograms = doc.CreateElement("ChildPrograms");

            // Add the DiagPLC child program in all cases
            XmlElement diagPlcProgram = doc.CreateElement("ChildProgram");
            diagPlcProgram.SetAttribute("Name", "DiagPLC");
            childPrograms.AppendChild(diagPlcProgram);

            // Add other child programs based on the module counts
            if (NetworkModules.Count > 0)
            {
                if (NodeModules.Count == 0)
                {
                    XmlElement diagNetProgram = doc.CreateElement("ChildProgram");
                    diagNetProgram.SetAttribute("Name", "DiagNet");
                    childPrograms.AppendChild(diagNetProgram);
                }
                else
                {
                    string[] additionalChildProgramNames = { "DiagAnalog", "DiagDigital", "DiagNet", "DiagNode" };
                    foreach (string childProgramName in additionalChildProgramNames)
                    {
                        XmlElement childProgram = doc.CreateElement("ChildProgram");
                        childProgram.SetAttribute("Name", childProgramName);
                        childPrograms.AppendChild(childProgram);
                    }
                }
            }

            hwDiagProgram.AppendChild(childPrograms);

            progress.Report($"Hardware Diagnostic Program names HW_Diag has been added");

            return hwDiagProgram;
        }

        return null;       

        
    }

    private L5XProgram CreateProgramFromRoutine(string dataType, L5XRoutine routine, L5XPrograms programsToModify, DbHelper dbHelper, RockwellUpgradeOptions options, int NewProgramIndex, string routinename, XmlDocument xmlDocNew, IProgress<string> progress)
    {
        // Create elements for the program, routines, and tags
        L5XProgram program = (L5XProgram)Project.CreateElement("", "Program", "");
        L5XRoutines routines = (L5XRoutines)Project.CreateElement("", "Routines", "");
        L5XTags tags = (L5XTags)Project.CreateElement("", "Tags", "");

        // Create required attributes for the program
        CreateRequiredAttributes(dataType, routine, program);

        // Import the existing routine into the new program
        L5XRoutine newRoutine = (L5XRoutine)program.OwnerDocument.ImportNode(routine, true);
        newRoutine.RoutineName = dataType;
        string routineName = routine.RoutineName + "_FB";
        string CMDName = routine.RoutineName + "_CMD";
        string TKName = routine.RoutineName + "_TK";

        // Import the existing routine into the new program
        L5XRoutine newInterlockRoutine = (L5XRoutine)program.OwnerDocument.ImportNode(routine, true);
        newInterlockRoutine.RoutineName = "Interlock";

        // Find the RLLContent element in the new routine
        XmlNode? rllContent = newRoutine.SelectSingleNode("RLLContent");

        if (rllContent == null)
        {
            // If RLLContent does not exist, create it
            rllContent = program.OwnerDocument.CreateElement("RLLContent");
            newRoutine.AppendChild(rllContent);
        }

        // Find the RLLContent element in the new routine
        XmlNode? interlockRllContent = newInterlockRoutine.SelectSingleNode("RLLContent");

        if (interlockRllContent == null)
        {
            // If Interlock RLLContent does not exist, create it
            interlockRllContent = program.OwnerDocument.CreateElement("RLLContent");
            newInterlockRoutine.AppendChild(interlockRllContent);
        }
        else
        {
            // If RLLContent exists, clear existing rungs
            interlockRllContent.RemoveAll();
        }

        // Handle specific data types: Analog, HLC, PID
        if (dataType == "Analog" || dataType == "HLC" || dataType == "PID")
        {
            // Extract the control index from the routine
            string controlIndex = ExtractControlIndexFromRoutine(routine, dataType, dbHelper);

            // Get the control rung string from the database
            string controlRungString = dbHelper.GetControlRung(dataType);

            // Replace the placeholder with the actual control index
            controlRungString = controlRungString.Replace("##ControlIndex##", controlIndex);

            // Wrap the control rung string in a <Rung> element to avoid multiple root element exception
            string wrappedControlRungString = $"<Rung Number='0' Type='N'>{controlRungString}</Rung>";

            // Load the wrapped control rung string into an XML document
            XmlDocument tempDoc = new XmlDocument();
            tempDoc.LoadXml(wrappedControlRungString);
            XmlNode controlRungNode = tempDoc.DocumentElement;

            // Import the control rung node into the new routine
            XmlNode importedControlRungNode = newRoutine.OwnerDocument.ImportNode(controlRungNode, true);

            // Remove any existing <Rung> elements with Number="0" from the RLLContent
            RemoveDuplicateRungWithNumberZero(rllContent);

            // Insert the new control rung at the beginning of the RLLContent
            rllContent.PrependChild(importedControlRungNode);

            // Update the numbers of all rungs in the RLLContent
            UpdateRungNumbers(rllContent);

            progress.Report("ControlIndexRung Routine is added for this datatype named " + dataType);
        }

        // Extract and convert rungs, keeping the first rung unchanged for specific data types
        List<XmlNode> convertedRungs = ConvertRungs(newRoutine, dbHelper, dataType, options, New_Program_Index,routinename, progress, program, tags);

        // Create a new RLLContent element and add the converted rungs to it
        XmlElement newRLLContent = program.OwnerDocument.CreateElement("RLLContent");

        // Regex pattern to match .EN, .IN, or AsysInterlock
        Regex interlockPattern = new Regex(@"\.EN|\.IN|AsysInterlock|AsysExtInterlock", RegexOptions.IgnoreCase);

        foreach (XmlNode convertedRung in convertedRungs)
        {
            // Check if the convertedRung contains .EN, .IN, or AsysInterlock using regex
            bool containsInterlock = interlockPattern.IsMatch(convertedRung.InnerXml);

            bool checkcondition = containsInterlock && (dataType == "Motor" || dataType == "Unimotor" || dataType == "Bimotor" || dataType == "Group");

            // Exclude rungs containing .EN, .IN, or AsysInterlock from interlockRllContent for specific data types
            if (true)
            {
                if (checkcondition)
                {
                    // Add the converted rung to the interlockRllContent
                    interlockRllContent.AppendChild(convertedRung.CloneNode(true));

                    string pattern = @"Asys(?:Interlock|ExtInterlock)\(([^,]+),([^,]+),([^,]+)\)";
                    string Description = string.Empty;

                    Match match = Regex.Match(convertedRung.InnerText, pattern);

                    if (match.Success)
                    {
                        string firstOperand = match.Groups[1].Value.Trim();
                        string secondOperand = match.Groups[2].Value.Trim();
                        string thirdOperand = match.Groups[3].Value.Trim();
                        NewInterlockParameter newInterlockParameter = new NewInterlockParameter(firstOperand, secondOperand, thirdOperand);
                        newInterlockParameters.Add(newInterlockParameter);
                        progress.Report("New Interlock tag has been added named " + firstOperand);
                        string convertedThirdOperand = ConvertThirdOperand(thirdOperand);

                        L5XTags OriginalTags = (L5XTags?)Project.Content?.Controller?.Tags;

                        if (OriginalTags != null)
                        {
                            foreach (XmlElement item in OriginalTags)
                            {
                                if (item.Attributes["Name"] != null)
                                {
                                    if (item.Attributes["AliasFor"] != null)
                                    {
                                        if (item.Attributes["AliasFor"].Value == thirdOperand)
                                        {
                                            Description = item.FirstChild?.InnerText;
                                        }
                                    }
                                }
                            }
                        }

                        XmlNode InterlockTag = InterlockTagCreation(firstOperand, options, Description, progress);
                        
                        XmlNode parentNode = InterlockTags; 

                        if (parentNode != null)
                        {                            
                            XmlNode importedNode = parentNode.OwnerDocument.ImportNode(InterlockTag, true);                            
                            bool nodeExists = false;

                            foreach (XmlNode childNode in parentNode.ChildNodes)
                            {
                                if (childNode.OuterXml == importedNode.OuterXml) 
                                {
                                    nodeExists = true;
                                    break;
                                }
                            }
                            
                            if (!nodeExists)
                            {
                                parentNode.AppendChild(importedNode);
                            }
                        }
                    }

                }
                else
                {
                    string pattern1 = @"OTE\(\w+_FBINT\d+\)";
                    string patternXIC = @"XIC\(\w+_FBINT\d+\)";
                    string patternXIO = @"XIO\(\w+_FBINT\d+\)";
                    string pattern10 = @"\b\w+_FBINT\w+\b";
                    bool tagCreated = false;

                    Match match11 = Regex.Match(convertedRung.InnerText, pattern10);

                    if (match11.Success)
                    {
                        string content11 = match11.Groups[0].Value;
                        L5XTags OriginalTags = (L5XTags?)Project.Content?.Controller?.Tags;

                        L5XTag tag = OriginalTags?.TryGetTagByName(content11);

                        if (tag != null)
                        {
                            // Modify TagType attribute to Public
                            XmlAttribute? tagTypeAttribute = tag.Attributes?["TagType"];
                            if (tagTypeAttribute != null)
                            {
                                tagTypeAttribute.Value = "Public";
                            }

                            // Remove AliasFor attribute
                            XmlAttribute? aliasForAttribute = tag.Attributes?["AliasFor"];
                            if (aliasForAttribute != null)
                            {
                                tag.Attributes.Remove(aliasForAttribute);
                            }

                            bool tagExists = false;

                            foreach (XmlNode existingTag in tags.ChildNodes)
                            {
                                if (existingTag.OuterXml == tag.OuterXml) // Assuming the comparison of OuterXml for uniqueness
                                {
                                    tagExists = true;
                                    break;
                                }
                            }

                            if (!tagExists)
                            {
                                if (tag.OwnerDocument != program.OwnerDocument)
                                {
                                    XmlNode importedTag = program.OwnerDocument.ImportNode(tag, true);
                                    tags.AppendChild(importedTag);
                                    program.AppendChild(tags);
                                    progress.Report("Tag named " + tag.Name + "is added to Program named " + program.Name);
                                    tagCreated = true;
                                }
                                else
                                {
                                    tags.AppendChild(tag);
                                    program.AppendChild(tags);
                                }
                            }

                        }
                    }

                        if (!Regex.IsMatch(convertedRung.InnerText, pattern1))
                        {
                        if (!(Regex.IsMatch(convertedRung.InnerText, patternXIC)))
                        {
                            string pattern = @"(XIC|XIO|OTE|MOV|TON)\(([^)]*?_FB_[^)]*?)\)";
                            Match match = Regex.Match(convertedRung.InnerText, pattern);
                            if (match.Success)
                            {
                                string content = match.Groups[2].Value;

                                L5XTag tag = CreateProgramTag(content);
                                if (tag != null)
                                {
                                    bool tagExists = false;

                                    foreach (XmlNode existingTag in tags.ChildNodes)
                                    {
                                        if (existingTag.OuterXml == tag.OuterXml) // Assuming the comparison of OuterXml for uniqueness
                                        {
                                            tagExists = true;
                                            break;
                                        }
                                    }

                                    if (!tagExists)
                                    {
                                        if (tag.OwnerDocument != program.OwnerDocument)
                                        {
                                            XmlNode importedTag = program.OwnerDocument.ImportNode(tag, true);
                                            tags.AppendChild(importedTag);
                                            program.AppendChild(tags);
                                            progress.Report("Tag named " + tag.Name + "is added to Program named " + program.Name);
                                            tagCreated = true;
                                        }
                                        else
                                        {
                                            tags.AppendChild(tag);
                                            program.AppendChild(tags);
                                        }
                                    }
                                }
                            }

                            if (tagCreated == false)
                            {
                                string pattern2 = @"(\w+)\(.*?,([^,]*?_FB[^,]*?),.*?\)";
                                var match2 = Regex.Match(convertedRung.InnerText, pattern2);

                                if (match2.Success)
                                {
                                    string content = match2.Groups[2].Value;

                                    L5XTag tag = CreateProgramTag(content);
                                    if (tag != null)
                                    {
                                        bool tagExists = false;

                                        foreach (XmlNode existingTag in tags.ChildNodes)
                                        {
                                            if (existingTag.OuterXml == tag.OuterXml) // Assuming the comparison of OuterXml for uniqueness
                                            {
                                                tagExists = true;
                                                break;
                                            }
                                        }

                                        if (!tagExists)
                                        {
                                            if (tag.OwnerDocument != program.OwnerDocument)
                                            {
                                                XmlNode importedTag = program.OwnerDocument.ImportNode(tag, true);
                                                tags.AppendChild(importedTag);
                                                program.AppendChild(tags);
                                                progress.Report("Tag named " + tag.Name + "is added to Program named " + program.Name);
                                                tagCreated = true;
                                            }
                                            else
                                            {
                                                tags.AppendChild(tag);
                                                program.AppendChild(tags);
                                            }
                                        }
                                    }
                                }
                            }

                            if (tagCreated == false)
                            {
                                string pattern3 = @"\b(?!Asys)(XIC|XIO|OTE|MOV|TON|\w+)\([^()]*?([^(),]*_FB[^(),]*?)\)";

                                var match3 = Regex.Match(convertedRung.InnerText, pattern3);

                                if (match3.Success)
                                {
                                    string content = match3.Groups[2].Value;

                                    L5XTag tag = CreateProgramTag(content);
                                    if (tag != null)
                                    {
                                        bool tagExists = false;

                                        foreach (XmlNode existingTag in tags.ChildNodes)
                                        {
                                            if (existingTag.OuterXml == tag.OuterXml) // Assuming the comparison of OuterXml for uniqueness
                                            {
                                                tagExists = true;
                                                break;
                                            }
                                        }

                                        if (!tagExists)
                                        {
                                            if (tag.OwnerDocument != program.OwnerDocument)
                                            {
                                                XmlNode importedTag = program.OwnerDocument.ImportNode(tag, true);
                                                tags.AppendChild(importedTag);
                                                program.AppendChild(tags);
                                                progress.Report("Tag named " + tag.Name + "is added to Program named " + program.Name);
                                                tagCreated = true;
                                            }
                                            else
                                            {
                                                tags.AppendChild(tag);
                                                program.AppendChild(tags);
                                            }
                                        }
                                    }
                                }
                            }

                            if (tagCreated == false)
                            {
                                string pattern4 = @"[\)\]]\s*TON\(([^,]+),";

                                // Create a Regex object
                                Regex regex = new Regex(pattern4);

                                // Find matches in the input string
                                Match match4 = regex.Match(convertedRung.InnerText);

                                if (match4.Success)
                                {
                                    string content = match4.Groups[1].Value;

                                    L5XTag tag = CreateProgramTag(content);
                                    if (tag != null)
                                    {
                                        bool tagExists = false;

                                        foreach (XmlNode existingTag in tags.ChildNodes)
                                        {
                                            if (existingTag.OuterXml == tag.OuterXml) // Assuming the comparison of OuterXml for uniqueness
                                            {
                                                tagExists = true;
                                                break;
                                            }
                                        }

                                        if (!tagExists)
                                        {
                                            if (tag.OwnerDocument != program.OwnerDocument)
                                            {
                                                XmlNode importedTag = program.OwnerDocument.ImportNode(tag, true);
                                                tags.AppendChild(importedTag);
                                                program.AppendChild(tags);
                                                progress.Report("Tag named " + tag.Name + "is added to Program named " + program.Name);
                                                tagCreated = true;
                                            }
                                            else
                                            {
                                                tags.AppendChild(tag);
                                                program.AppendChild(tags);
                                            }
                                        }
                                    }
                                }
                            }
                            
                        }
                    }

                    newRLLContent.AppendChild(convertedRung.CloneNode(true));
                }
            }
        }


        // Add <Comment> node to rungs in interlockRllContent containing .IN
        foreach (XmlNode interlockRung in interlockRllContent.SelectNodes("Rung"))
        {
            if (interlockPattern.IsMatch(interlockRung.InnerXml))
            {
                // Check if OTE with .IN exists in the rung
                Regex otePattern = new Regex(@"OTE\([^)]*\.IN\)", RegexOptions.IgnoreCase);
                if (otePattern.IsMatch(interlockRung.InnerXml))
                {
                    // Extract the input operand using regex
                    Regex inputOperandPattern = new Regex(@"\[CDATA\[(.*?)[\)\]]\s*OTE", RegexOptions.Singleline);
                    Match match = inputOperandPattern.Match(interlockRung.InnerXml);

                    if (match.Success)
                    {
                        string inputOperand = match.Groups[1].Value.Trim();
                        inputOperand = inputOperand + ")OTE";
                        string pattern1 = @"AL_L[23]";
                        string pattern2 = @"AL_H[23]";
                        string pattern3 = @".Loc";
                        string replacement1 = "AL_LL";
                        string replacement2 = "AL_HH";


                        // Perform replacements
                        string result = Regex.Replace(inputOperand, pattern1, replacement1);
                        inputOperand = Regex.Replace(result, pattern2, replacement2);

                        if (inputOperand.Contains(","))
                        {  
                            string pattern = @"XIC\([^\]]*?\)";

                            // Use Regex to find the match
                            Match match1 = Regex.Match(inputOperand, pattern);

                            if (match1.Success)
                            {
                                inputOperand = match1.Value;
                            }
                        }                          

                        if (OriginalPrograms != null)
                        {
                            bool commentAdded = false; // Flag to track if a Comment node has been added

                            foreach (L5XProgram originalProgram in OriginalPrograms)
                            {
                                foreach (L5XRoutine originalRoutine in originalProgram.Routines)
                                {
                                    foreach (XmlElement textElement in originalRoutine.SelectNodes(".//Text"))
                                    {
                                        string textContent = textElement.InnerText;

                                        // Check if the textContent contains inputOperand
                                        if (textContent.Contains(inputOperand))
                                        {
                                            // Find the corresponding Comment node
                                            XmlNode commentNode = textElement.SelectSingleNode("../Comment");
                                            if (textContent == "GEQ(_511BC600BT1AT01_FP.X_SCAL,6.0)OTE(_511BC600MA01_FBINT04);")
                                            {
                                                commentNode.InnerText = "_511BC600MA01_FBINT05: STI2 :Temp Not OK - INTERLOCK Point";
                                            }
                                            if (commentNode != null && !commentAdded) // Check if Comment node exists and has not been added yet
                                            {
                                                // Create a new Comment node directly under interlockRung
                                                XmlElement newCommentNode = interlockRung.OwnerDocument.CreateElement("Comment");
                                                XmlCDataSection commentText = interlockRung.OwnerDocument.CreateCDataSection(commentNode.InnerText);

                                                // Append the new Comment node as the first child of interlockRung
                                                interlockRung.InsertBefore(newCommentNode, interlockRung.LastChild);
                                                newCommentNode.AppendChild(commentText);
                                                commentText = null;

                                                commentAdded = true; // Set flag to true indicating Comment node has been added
                                            }
                                            break; // Break out of innermost loop once a match is found
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }        

        // Replace the old RLLContent with the new RLLContent
        XmlNode? existingRLLContent = newRoutine.SelectSingleNode("RLLContent");
        if (existingRLLContent != null)
        {
            newRoutine.ReplaceChild(newRLLContent, existingRLLContent);
        }
        else
        {
            newRoutine.AppendChild(newRLLContent);
        }

        //foreach (XmlNode newRung in newRoutine)
        //{
        //    XmlNodeList newRungsList = newRung.SelectNodes(".//Rung"); // Adjust the XPath as needed

        //    foreach (L5XProgram originalProgram in OriginalPrograms)
        //    {
        //        foreach (L5XRoutine originalRoutine in originalProgram.Routines)
        //        {
        //            if (originalRoutine.RoutineName == routinename)
        //            {
        //                XmlNodeList originalRungsList = originalRoutine.SelectNodes(".//Rung");

        //                for (int i = 0; i < newRungsList.Count; i++)
        //                {
        //                    XmlNode newRungNode = newRungsList[i];
        //                    XmlNode originalRungNode = originalRungsList[i];

        //                    if (originalRungNode != null)
        //                    {
        //                        foreach (XmlElement textElement in originalRungNode.SelectNodes(".//Text"))
        //                        {
        //                            string textContent = textElement.InnerText;

        //                            XmlNode commentNode = textElement.SelectSingleNode("../Comment");

        //                            if (commentNode != null)
        //                            {
        //                                // Check if the comment contains "_FBINT"
        //                                if (!Regex.IsMatch(commentNode.InnerText, "_FBINT"))
        //                                {
        //                                    // Create new Comment node and CDATA section
        //                                    XmlElement newCommentNode = newRungNode.OwnerDocument.CreateElement("Comment");
        //                                    XmlCDataSection commentText = newRungNode.OwnerDocument.CreateCDataSection(commentNode.InnerText);

        //                                    // Insert the new Comment node at the beginning of the new rung
        //                                    newRungNode.InsertBefore(newCommentNode, newRungNode.FirstChild);
        //                                    newCommentNode.AppendChild(commentText);
        //                                }
        //                            }
        //                        }
        //                    }
                            
        //                }
        //            }
        //        }
        //    }
        //}

        // Update the rung numbers in newRLLContent to be sequential
        UpdateRungNumbers(newRLLContent);

        // Update the rung numbers in interlockRllContent to be sequential starting from 0
        UpdateRungNumbers(interlockRllContent);

        // Append the new routine and other elements to the program
        routines.AppendChild(newRoutine);

        // Check if the Interlock routine should be added
        if (interlockRllContent.HasChildNodes)
        {
            routines.AppendChild(newInterlockRoutine);
        }

        // Get the tag data from the database
        DataRow[] rows = dbHelper.GetTagsNameDataType(dataType, options);
        DataRow[] selectedRows;

        if (dataType == "Recipe")
        {
            var doc = program.OwnerDocument;
            XmlElement newTag = program.OwnerDocument.CreateElement("Tag");
            newTag = CreateTagRecipe(doc, progress);
            progress.Report("Tag named " + newTag.Name + "is added to Program named " + program.Name);
            tags.AppendChild(newTag);
            program.AppendChild(tags);
        }

        else
        {
            if (rows.Length > 0)
            {
                string selectedfield = rows[0]["From_FB"].ToString();

                selectedRows = dbHelper.GetProgramTags(dataType, selectedfield, options);

                // Create new tags and append to the tags element
                foreach (DataRow row in selectedRows)
                {
                    if (row.ItemArray[10].ToString() == "FB")
                    {
                        row["To_Operator_Type"] = row["To_FB"];
                        row["To_Rename"] = "FB";
                        row["To_Pin_Name"] = "FB";
                    }
                    string pinName = row["To_Pin_Name"].ToString();
                    string operatorType = row["To_Operator_Type"].ToString();

                    if (pinName == "MSW" || pinName == "FACEPLATE" || operatorType == "constant")
                    {
                        continue;
                    }

                    // Skip creating the "Token" tag except for the "Group" data type
                    if (pinName == "TOKEN" && dataType != "Group")
                    {
                        continue;
                    }

                    if (row.ItemArray[10].ToString() == "FB")
                    {
                        row["To_Operator_Type"] = row["To_FB"];
                        row["To_Rename"] = "FB";
                    }

                    XmlElement newTag = program.OwnerDocument.CreateElement("Tag");

                    if ((!string.IsNullOrEmpty(row["To_Operator_Type"].ToString())))
                    {
                        if (row["To_Operator_Type"].ToString() == "constant")
                        {
                            continue;
                        }
                        string tagType = row["To_Rename"].ToString() == "FB" ? "Base" : string.Empty;

                        // Set the Name attribute based on specific conditions
                        string tagName = row["To_New_Operator"].ToString();
                        if (string.IsNullOrEmpty(tagName))
                        {
                            tagName = row["To_Rename"].ToString();
                        }

                        if (tagName == "TOKEN")
                        {
                            tagName = dataType == "Group" ? "Token" : $"{routine.RoutineName}.Token";
                        }

                        SetTagAttributes(newTag, tagName, tagType, row["To_Operator_Type"].ToString(), "Public", false, "Read/Write");

                        // Add Description element
                        XmlElement descriptionElement = program.OwnerDocument.CreateElement("Description");
                        XmlCDataSection cdataSection = program.OwnerDocument.CreateCDataSection("");
                        descriptionElement.AppendChild(cdataSection);
                        newTag.AppendChild(descriptionElement);

                        // Add DataFormat element only if tag name is "FB"
                        if (row["To_Rename"].ToString() == "FB")
                        {

                            XmlNode dataFormatElement = ExtractFBDataFormatElement(routineName, dbHelper, options, dataType, program);
                            if (dataFormatElement != null)
                            {
                                int sequence = 0; // Initialize sequence number

                                // Process each child node in dataFormatElement
                                foreach (XmlNode childNode in dataFormatElement.ChildNodes)
                                {
                                    if (childNode.NodeType == XmlNodeType.Element &&
                                        childNode.Name != "Tag" &&
                                        childNode.Name != "Description" &&
                                        !(childNode.Name == "Data" && childNode.Attributes["Format"]?.Value == "L5K"))
                                    {
                                        // Convert childNode to L5XTag
                                        L5XTag l5xTag = L5XTag.FromXmlNode(childNode, newTag.OwnerDocument, sequence);

                                        descriptionElement.RemoveChild(cdataSection);

                                        string descriptionText = ExtractDescriptionText(routine.RoutineName, xmlDocNew);

                                        cdataSection = program.OwnerDocument.CreateCDataSection(descriptionText);

                                        descriptionElement.AppendChild(cdataSection);

                                        newTag.AppendChild(descriptionElement);

                                        // Append the converted L5XTag to newTag
                                        newTag.AppendChild(l5xTag);

                                        // Increment sequence number for each L5XTag
                                        sequence++;

                                        // Example: Add user message
                                        L5XCollection.AddUserMessage(Project, l5xTag, null, UserMessageTypes.Information,
                                            $"Program tag has been added", "O2O", $"Program tag has been added to Program name {routine.RoutineName}");
                                    }
                                }
                            }
                        }


                        else if (row["To_Rename"].ToString() == "Grp_CMD")
                        {
                            XmlNode dataFormatElement = ExtractOthersDataFormatElement(CMDName, dbHelper, options);
                            if (dataFormatElement != null)
                            {
                                // Import the child nodes of dataFormatElement except for Tag, Description, and Data elements with Format="L5K"
                                foreach (XmlNode childNode in dataFormatElement.ChildNodes)
                                {
                                    if (childNode.Name != "Tag" && childNode.Name != "Description" && !(childNode.Name == "Data" && childNode.Attributes["Format"]?.Value == "L5K"))
                                    {
                                        XmlNode importedChildNode = newTag.OwnerDocument.ImportNode(childNode, true);
                                        newTag.AppendChild(importedChildNode);
                                    }
                                }
                            }
                        }

                        else if (row["To_Rename"].ToString() == "Dept_Link")
                        {
                            XmlNode dataFormatElement = ExtractOthersDataFormatElement("DUMMY_DEPT_CMD", dbHelper, options);
                            if (dataFormatElement != null)
                            {
                                // Import the child nodes of dataFormatElement except for Tag, Description, and Data elements with Format="L5K"
                                foreach (XmlNode childNode in dataFormatElement.ChildNodes)
                                {
                                    if (childNode.Name != "Tag" && childNode.Name != "Description" && !(childNode.Name == "Data" && childNode.Attributes["Format"]?.Value == "L5K"))
                                    {
                                        XmlNode importedChildNode = newTag.OwnerDocument.ImportNode(childNode, true);
                                        newTag.AppendChild(importedChildNode);
                                    }
                                }
                            }
                        }

                        else if (row["To_Pin_Name"].ToString() == "TOKEN")
                        {
                            XmlNode dataFormatElement = ExtractDataFormatElement(TKName);
                            if (dataFormatElement != null)
                            {
                                XmlNode importedDataFormatElement = newTag.OwnerDocument.ImportNode(dataFormatElement, true);
                                newTag.AppendChild(importedDataFormatElement);
                            }
                        }
                        else
                        {
                            XmlNode dataFormatElement = ExtractDataFormatElement(routineName);
                            if (dataFormatElement != null)
                            {
                                XmlNode importedDataFormatElement = newTag.OwnerDocument.ImportNode(dataFormatElement, true);
                                newTag.AppendChild(importedDataFormatElement);
                            }
                        }

                        if (newTag.Name == "Tag" && newTag.GetAttribute("Name") == "FB")
                        {
                            tags.PrependChild(newTag);
                        }

                        else
                        {
                            tags.AppendChild(newTag);
                        }                        

                    }
                }
            }

            program.AppendChild(tags);
        }

        

        if (dataType == "Analog")
        {
            XmlNode existingAenTag = tags.SelectSingleNode("Tag[@Name='AEN']");

            if (existingAenTag != null)
            {
                // Update the existing AEN tag
                ((XmlElement)existingAenTag).SetAttribute("TagType", "Base");
                ((XmlElement)existingAenTag).SetAttribute("DataType", "BOOL");
                ((XmlElement)existingAenTag).SetAttribute("Radix", "Decimal");
                ((XmlElement)existingAenTag).SetAttribute("Constant", "false");
                ((XmlElement)existingAenTag).SetAttribute("ExternalAccess", "Read/Write");

                XmlNode descriptionElement1 = existingAenTag.SelectSingleNode("Description");
                if (descriptionElement1 == null)
                {
                    descriptionElement1 = program.OwnerDocument.CreateElement("Description");
                    existingAenTag.AppendChild(descriptionElement1);
                }
                else
                {
                    descriptionElement1.RemoveAll();  // Remove existing content
                }
                descriptionElement1.AppendChild(program.OwnerDocument.CreateCDataSection(""));

                XmlNode dataElementNode = existingAenTag.SelectSingleNode("Data");
                XmlElement dataElement;
                if (dataElementNode == null)
                {
                    dataElement = program.OwnerDocument.CreateElement("Data");
                    dataElement.SetAttribute("Format", "Decorated");
                    existingAenTag.AppendChild(dataElement);
                }
                else
                {
                    dataElement = (XmlElement)dataElementNode;
                    dataElement.SetAttribute("Format", "Decorated");
                }

                XmlNode dataValueElementNode = dataElement.SelectSingleNode("DataValue");
                XmlElement dataValueElement;
                if (dataValueElementNode == null)
                {
                    dataValueElement = program.OwnerDocument.CreateElement("DataValue");
                    dataElement.AppendChild(dataValueElement);
                }
                else
                {
                    dataValueElement = (XmlElement)dataValueElementNode;
                }
                dataValueElement.SetAttribute("DataType", "BOOL");
                dataValueElement.SetAttribute("Radix", "Decimal");
                dataValueElement.SetAttribute("Value", "1");
            }
            else
            {
                // Create a new AEN tag
                XmlElement aenTag = program.OwnerDocument.CreateElement("Tag");
                aenTag.SetAttribute("Name", "AEN");
                aenTag.SetAttribute("TagType", "Base");
                aenTag.SetAttribute("DataType", "BOOL");
                aenTag.SetAttribute("Radix", "Decimal");
                aenTag.SetAttribute("Constant", "false");
                aenTag.SetAttribute("ExternalAccess", "Read/Write");

                XmlElement descriptionElement1 = program.OwnerDocument.CreateElement("Description");
                descriptionElement1.AppendChild(program.OwnerDocument.CreateCDataSection(""));

                XmlElement dataElement = program.OwnerDocument.CreateElement("Data");
                dataElement.SetAttribute("Format", "Decorated");

                XmlElement dataValueElement = program.OwnerDocument.CreateElement("DataValue");
                dataValueElement.SetAttribute("DataType", "BOOL");
                dataValueElement.SetAttribute("Radix", "Decimal");
                dataValueElement.SetAttribute("Value", "1");

                dataElement.AppendChild(dataValueElement);
                aenTag.AppendChild(descriptionElement1);
                aenTag.AppendChild(dataElement);

                tags.AppendChild(aenTag);
            }
        }
        if (dataType == "Alarm")
        {
            // Create a new PWR tag
            XmlElement pwrTag = program.OwnerDocument.CreateElement("Tag");
            pwrTag.SetAttribute("Name", "PWR");
            pwrTag.SetAttribute("TagType", "Base");
            pwrTag.SetAttribute("DataType", "BOOL");
            pwrTag.SetAttribute("Radix", "Decimal");
            pwrTag.SetAttribute("Constant", "false");
            pwrTag.SetAttribute("ExternalAccess", "Read/Write");

            XmlElement descriptionElement = program.OwnerDocument.CreateElement("Description");
            descriptionElement.AppendChild(program.OwnerDocument.CreateCDataSection(""));

            XmlElement dataElement = program.OwnerDocument.CreateElement("Data");
            dataElement.SetAttribute("Format", "Decorated");

            XmlElement dataValueElement = program.OwnerDocument.CreateElement("DataValue");
            dataValueElement.SetAttribute("DataType", "BOOL");
            dataValueElement.SetAttribute("Radix", "Decimal");
            dataValueElement.SetAttribute("Value", "1");

            dataElement.AppendChild(dataValueElement);
            pwrTag.AppendChild(descriptionElement);
            pwrTag.AppendChild(dataElement);

            tags.AppendChild(pwrTag);
        }

        else
        {
            XmlNode existingTag_Grp_Cmd = tags.SelectSingleNode("Tag[@Name='Grp_CMD']");

            if (existingTag_Grp_Cmd != null)
            {
                // Update the existing "Grp_CMD" tag attributes
                XmlElement grpCmdTag = (XmlElement)existingTag_Grp_Cmd;
                grpCmdTag.SetAttribute("TagType", "Base");
                grpCmdTag.SetAttribute("DataType", "ACESYS_CMD");
                grpCmdTag.SetAttribute("Usage", "Public");
                grpCmdTag.SetAttribute("Constant", "false");
                grpCmdTag.SetAttribute("ExternalAccess", "Read/Write");

                // Update or add the Description element
                XmlNode descriptionElement = grpCmdTag.SelectSingleNode("Description");
                if (descriptionElement == null)
                {
                    descriptionElement = program.OwnerDocument.CreateElement("Description");
                    grpCmdTag.AppendChild(descriptionElement);
                }
                descriptionElement.RemoveAll(); // Remove existing content
                descriptionElement.AppendChild(program.OwnerDocument.CreateCDataSection("Sequence Start"));

                // Update or add the Data element
                XmlNode dataElementNode = grpCmdTag.SelectSingleNode("Data");
                XmlElement dataElement;
                if (dataElementNode == null)
                {
                    dataElement = program.OwnerDocument.CreateElement("Data");
                    dataElement.SetAttribute("Format", "Decorated");
                    grpCmdTag.AppendChild(dataElement);
                }
                else
                {
                    dataElement = (XmlElement)dataElementNode;
                    dataElement.SetAttribute("Format", "Decorated");
                }

                // Update or add DataValueMember elements within Structure
                XmlNode structureNode = dataElement.SelectSingleNode("Structure");
                if (structureNode == null)
                {
                    structureNode = program.OwnerDocument.CreateElement("Structure");
                    structureNode.Attributes.Append(program.OwnerDocument.CreateAttribute("DataType")).Value = "ACESYS_CMD";
                    dataElement.AppendChild(structureNode);
                }

                // Clear existing DataValueMember elements
                structureNode.RemoveAll();

                // Add new DataValueMember elements
                string[] memberNames = { "GSEL", "GSTR", "GSTP", "GQSTP", "GSS_FP", "GSS_Field", "GLTP", "GASTW", "GVSTW" };
                foreach (var memberName in memberNames)
                {
                    XmlElement dataValueMember = program.OwnerDocument.CreateElement("DataValueMember");
                    dataValueMember.SetAttribute("Name", memberName);
                    dataValueMember.SetAttribute("DataType", "BOOL");
                    dataValueMember.SetAttribute("Value", "0");
                    structureNode.AppendChild(dataValueMember);
                }
            }

            XmlNode existingTokenTag = tags.SelectSingleNode("Tag[@Name='Token']");
            XmlElement tokenTag;
            if (existingTokenTag != null)
            {
                // Update the existing Token tag
                tokenTag = (XmlElement)existingTokenTag;
                tokenTag.SetAttribute("TagType", "Base");
                tokenTag.SetAttribute("DataType", "ACESYS_Token");
                tokenTag.SetAttribute("Usage", "Public");
                tokenTag.SetAttribute("Constant", "false");
                tokenTag.SetAttribute("ExternalAccess", "Read/Write");

                XmlNode descriptionElement = tokenTag.SelectSingleNode("Description");
                if (descriptionElement == null)
                {
                    descriptionElement = program.OwnerDocument.CreateElement("Description");
                    tokenTag.AppendChild(descriptionElement);
                }
                descriptionElement.RemoveAll(); // Remove existing content
                descriptionElement.AppendChild(program.OwnerDocument.CreateCDataSection("Token"));

                XmlNode dataElementNode = tokenTag.SelectSingleNode("Data");
                XmlElement dataElement;
                if (dataElementNode == null)
                {
                    dataElement = program.OwnerDocument.CreateElement("Data");
                    dataElement.SetAttribute("Format", "Decorated");
                    tokenTag.AppendChild(dataElement);
                }
                else
                {
                    dataElement = (XmlElement)dataElementNode;
                    dataElement.SetAttribute("Format", "Decorated");
                }

                // Define DataValueMember elements for Token
                string[] dataMembers = {
                "Acknowledge", "AL_UNIT", "StartWarn", "GON", "GOFF", "RRDY", "Interlock_Unit",
                "DeSelButRun", "Sim_Enabled", "Flash_Slow", "Flash_Fast", "Pulse_1sec", "Pulse_1min",
                "Pulse_1Hour", "Pulse_1Day", "Pulse_1Month", "Pulse_1Year", "EmergencyStop", "ModeSwap",
                "SS_Field_Enable", "SS_FP_Enable", "Man_Enable", "Loc_Enable", "Test_Enable", "LOC_Type",
                "REM_Type", "QSTOP_FP_Unit", "AllUnitsAuto", "NotEmpty", "CheckNotEmpty", "ResetNotEmpty",
                "AlarmSuppres", "SACK_On", "SACK_Off", "SEC_Toggle", "UnitSelect", "TimeStamp", "OneUnitAuto"
            };

                // Clear existing DataValueMember elements
                XmlNode structureNode = dataElement.SelectSingleNode("Structure");
                if (structureNode == null)
                {
                    structureNode = program.OwnerDocument.CreateElement("Structure");
                    structureNode.Attributes.Append(program.OwnerDocument.CreateAttribute("DataType")).Value = "ACESYS_Token";
                    dataElement.AppendChild(structureNode);
                }
                structureNode.RemoveAll();

                // Add new DataValueMember elements
                foreach (var memberName in dataMembers)
                {
                    XmlElement dataValueMember = program.OwnerDocument.CreateElement("DataValueMember");
                    dataValueMember.SetAttribute("Name", memberName);
                    dataValueMember.SetAttribute("DataType", "BOOL");
                    dataValueMember.SetAttribute("Value", "0");
                    structureNode.AppendChild(dataValueMember);
                }
            }

            if (dataType == "Group" || dataType == "Motor" || dataType == "Unimotor" || dataType == "Bimotor" || dataType == "Valve" || dataType == "Positioner" || dataType == "Gate")
            {
                // Update or create tags
                UpdateOrCreateTag(tags, "INTL_SEQ_STP", "Description", "Sequence Stop");
                UpdateOrCreateTag(tags, "INTL_SEQ_STR1", "Description", "Sequence Start Direction 1");
                UpdateOrCreateTag(tags, "INTL_SEQ_STR2", "Description", "Sequence Start Direction 2");
                UpdateOrCreateTag(tags, "INTL", "Description", "Interlock");
                UpdateOrCreateTag(tags, "INTL_SEQ_STR", "Description", "Sequence Start");
            }

            if (dataType == "AsysSel" && options.IsExtendedSelect)
            {
                UpdateOrCreateTag(tags, "INTL_ENAB", "Description", "");
                UpdateOrCreateTag(tags, "INTL_FCON", "Description", "");
                UpdateOrCreateTag(tags, "INTL_FCOFF", "Description", "");
            }

            void UpdateOrCreateTag(XmlNode tags, string tagName, string description, string cdataDescription, string dataType = "SINT", string radix = "Decimal", string value = "0", string[] memberNames = null)
            {
                XmlNode existingTag = tags.SelectSingleNode($"Tag[@Name='{tagName}']");
                if (existingTag != null)
                {
                    tags.RemoveChild(existingTag);
                }

                XmlElement newTag = program.OwnerDocument.CreateElement("Tag");
                newTag.SetAttribute("Name", tagName);
                newTag.SetAttribute("TagType", "Base");
                newTag.SetAttribute("DataType", dataType);
                newTag.SetAttribute("Radix", radix);
                newTag.SetAttribute("Constant", "false");
                newTag.SetAttribute("ExternalAccess", "Read/Write");

                XmlElement descriptionElement = program.OwnerDocument.CreateElement("Description");
                descriptionElement.AppendChild(program.OwnerDocument.CreateCDataSection(cdataDescription));
                newTag.AppendChild(descriptionElement);

                XmlElement dataElement = program.OwnerDocument.CreateElement("Data");
                dataElement.SetAttribute("Format", "Decorated");
                newTag.AppendChild(dataElement);

                if (memberNames == null)
                {
                    XmlElement dataValueElement = program.OwnerDocument.CreateElement("DataValue");
                    dataValueElement.SetAttribute("DataType", dataType);
                    dataValueElement.SetAttribute("Radix", radix);
                    dataValueElement.SetAttribute("Value", value);
                    dataElement.AppendChild(dataValueElement);
                }
                else
                {
                    XmlElement structureNode = program.OwnerDocument.CreateElement("Structure");
                    structureNode.SetAttribute("DataType", dataType);
                    foreach (var memberName in memberNames)
                    {
                        XmlElement dataValueMember = program.OwnerDocument.CreateElement("DataValueMember");
                        dataValueMember.SetAttribute("Name", memberName);
                        dataValueMember.SetAttribute("DataType", "BOOL");
                        dataValueMember.SetAttribute("Value", "0");
                        structureNode.AppendChild(dataValueMember);
                    }
                    dataElement.AppendChild(structureNode);
                }

                tags.AppendChild(newTag);
            }            
        }

        if (dataType == "Motor" || dataType == "Unimotor" || dataType == "BiMotor" || dataType == "Group" || dataType == "Gate" || dataType == "Valve")
        {
            foreach (XmlNode item in InterlockTags)
            {
                AppendInterlockXmlNode(tags, item);
            }

            InterlockTags.Clear();
            
        }

        program.AppendChild(routines);

        return program;
    }

    private string ConvertThirdOperand(string thirdOperand)
    {
        var match = Regex.Match(thirdOperand, @"HMI_INTERLOCK\[(\d+)\]");
        if (match.Success)
        {
            int index = int.Parse(match.Groups[1].Value);
            int part1 = index / 16;
            int part2 = index % 16;
            return $"HMI_INTERLOCK[{part1}].{part2}";
        }
        return thirdOperand; // Return the original operand if it doesn't match the expected format
    }

    private L5XTag CreateProgramTag(string content)
    {
        if (OriginalPrograms != null)
        {
            foreach (L5XProgram originalProgram in OriginalPrograms)
            {
                foreach (var item in originalProgram)
                {
                    if (item is L5XTags tags)
                    {
                        foreach (L5XTag tag in tags)
                        {
                            XmlAttribute nameAttribute = tag.Attributes?["Name"];

                            if (nameAttribute != null && nameAttribute.Value == content)
                            {
                                return tag;
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    public XmlElement CreateTagRecipe(XmlDocument? doc, IProgress<string> progress)
    {
        XmlElement tagElement = doc.CreateElement("Tag");
        tagElement.SetAttribute("Name", "FB");
        tagElement.SetAttribute("TagType", "Base");
        tagElement.SetAttribute("DataType", "AsysRcp");
        tagElement.SetAttribute("Usage", "Public");
        tagElement.SetAttribute("Constant", "false");
        tagElement.SetAttribute("ExternalAccess", "Read/Write");

        XmlElement descriptionElement = doc.CreateElement("Description");
        descriptionElement.AppendChild(doc.CreateCDataSection(""));
        tagElement.AppendChild(descriptionElement);

        XmlElement dataElement = doc.CreateElement("Data");
        dataElement.SetAttribute("Format", "Decorated");

        XmlElement structureElement = doc.CreateElement("Structure");
        structureElement.SetAttribute("DataType", "AsysRcp");
        dataElement.AppendChild(structureElement);

        AddDataValueMember(doc, structureElement, "EnableIn", "BOOL", "1");
        AddDataValueMember(doc, structureElement, "EnableOut", "BOOL", "0");
        AddDataValueMember(doc, structureElement, "CV", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F1_QCX", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F2_QCX", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F3_QCX", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F4_QCX", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F5_QCX", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F6_QCX", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F7_QCX", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F8_QCX", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F1_RUN", "BOOL", "0");
        AddDataValueMember(doc, structureElement, "F2_RUN", "BOOL", "0");
        AddDataValueMember(doc, structureElement, "F3_RUN", "BOOL", "0");
        AddDataValueMember(doc, structureElement, "F4_RUN", "BOOL", "0");
        AddDataValueMember(doc, structureElement, "F5_RUN", "BOOL", "0");
        AddDataValueMember(doc, structureElement, "F6_RUN", "BOOL", "0");
        AddDataValueMember(doc, structureElement, "F7_RUN", "BOOL", "0");
        AddDataValueMember(doc, structureElement, "F8_RUN", "BOOL", "0");
        AddDataValueMember(doc, structureElement, "MILL_ON", "BOOL", "0");
        AddDataValueMember(doc, structureElement, "CCA", "BOOL", "1");
        AddDataValueMember(doc, structureElement, "OK", "BOOL", "1");
        AddDataValueMember(doc, structureElement, "MAX_OUT", "BOOL", "0");
        AddDataValueMember(doc, structureElement, "CV_ENG", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F1_ENG", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F2_ENG", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F3_ENG", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F4_ENG", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F5_ENG", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F6_ENG", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F7_ENG", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F8_ENG", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F1_PCT", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F2_PCT", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F3_PCT", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F4_PCT", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F5_PCT", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F6_PCT", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F7_PCT", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "F8_PCT", "REAL", "0.0", "Float");
        AddDataValueMember(doc, structureElement, "SPCTL", "BOOL", "0");

        tagElement.AppendChild(dataElement);
        progress.Report($"Receipe tag name {tagElement.Name} has been added");

        return tagElement;
    }

    private void AddDataValueMember(XmlDocument doc, XmlElement parent, string name, string dataType, string value, string radix = null)
    {
        XmlElement dataValueMember = doc.CreateElement("DataValueMember");
        dataValueMember.SetAttribute("Name", name);
        dataValueMember.SetAttribute("DataType", dataType);
        dataValueMember.SetAttribute("Value", value);
        if (radix != null)
        {
            dataValueMember.SetAttribute("Radix", radix);
        }
        parent.AppendChild(dataValueMember);
    }

    private XmlElement ConvertToXmlElement(XElement xElement, XmlDocument doc)
    {
        using (var reader = xElement.CreateReader())
        {
            XmlElement xmlElement = doc.CreateElement(xElement.Name.LocalName);

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        var elem = doc.CreateElement(reader.Name);
                        if (reader.HasAttributes)
                        {
                            while (reader.MoveToNextAttribute())
                            {
                                elem.SetAttribute(reader.Name, reader.Value);
                            }
                            reader.MoveToElement();
                        }
                        if (!reader.IsEmptyElement)
                        {
                            var innerElement = XElement.Load(reader.ReadSubtree());
                            elem.InnerXml = innerElement.Value;
                        }
                        xmlElement.AppendChild(elem);
                        break;
                    case XmlNodeType.Text:
                        xmlElement.InnerText = reader.Value;
                        break;
                    case XmlNodeType.CDATA:
                        var cdata = doc.CreateCDataSection(reader.Value);
                        xmlElement.AppendChild(cdata);
                        break;
                }
            }

            return xmlElement;
        }
    }

    private string ExtractDescriptionText(string routineName, XmlDocument xmlDocumentNew)
    {
        string routineName_With_FB = routineName + "_FB";
        XmlNodeList tagNodes;

        if (xmlDocumentNew != null)
        {
            // Select all tag nodes from xmlDocumentNew
            tagNodes = xmlDocumentNew.SelectNodes("//Tag");

            if (tagNodes != null)
            {
                foreach (XmlNode tagNode in tagNodes)
                {
                    XmlAttribute nameAttribute = tagNode.Attributes?["Name"];

                    if (nameAttribute != null && nameAttribute.Value == routineName_With_FB)
                    {
                        XmlNode descriptionNode = tagNode.SelectSingleNode("Description");
                        if (descriptionNode != null && descriptionNode.FirstChild is XmlCDataSection cdataSection)
                        {
                            return cdataSection.InnerText;
                        }
                    }
                }
            }
        }

        return "";
    }

    public void ClearInterlockXmlNodeList()
    {
        InterlockTags.Clear();
    }

    public void AppendInterlockXmlNode(XmlNode targetDocumentNode, XmlNode interlockXmlNode)
    {
        if (targetDocumentNode.OwnerDocument != null)
        {
            // Import the node to the target document
            XmlNode importedNode = targetDocumentNode.OwnerDocument.ImportNode(interlockXmlNode, true);

            // Append the imported node to the target node
            targetDocumentNode.AppendChild(importedNode);
        }
    }

    public XmlNode InterlockTagCreation(string tagName, RockwellUpgradeOptions options, string DescriptionText, IProgress<string> progress)
    {
        bool isExtInterlock = options.IsExtendedInterlock;

        string Description = MethodToExtractNewInterlockDescription(tagName,xmlDocNew);

        InterlockNewTagsHMI.Add(tagName);

        NewInterlockTagDescription interlockTagDescription = new NewInterlockTagDescription(tagName, Description);

        newInterlockTags.Add(interlockTagDescription);

        // Determine the DataType based on the boolean parameter
        string dataType = isExtInterlock ? "AsysExtInterlock" : "AsysInterlock";

        // Create XML document
        XmlDocument xmlDoc = new XmlDocument();

        // Determine if underscore prefix is needed based on tagName
        if (char.IsDigit(tagName[0])) // Check if first character is a digit
        {
            tagName = "_" + tagName; // Add underscore prefix
        }

        // Create Tag node
        XmlElement tagElement = xmlDoc.CreateElement("Tag");
        tagElement.SetAttribute("Name", tagName);
        tagElement.SetAttribute("TagType", "Base");
        tagElement.SetAttribute("DataType", dataType);
        tagElement.SetAttribute("Constant", "false");
        tagElement.SetAttribute("ExternalAccess", "Read/Write");

        // Create Description node
        XmlElement descriptionElement = xmlDoc.CreateElement("Description");
        descriptionElement.AppendChild(xmlDoc.CreateCDataSection(Description)); // Append DescriptionText as CDATA
        tagElement.AppendChild(descriptionElement);

        // Create Data node
        XmlElement dataElement = xmlDoc.CreateElement("Data");
        dataElement.SetAttribute("Format", "Decorated");

        // Create Structure node
        XmlElement structureElement = xmlDoc.CreateElement("Structure");
        structureElement.SetAttribute("DataType", dataType);

        // Determine DataValueMembers based on DataType
        string[] dataMembers = isExtInterlock
            ? new[] { "EnableIn", "EnableOut", "EN", "IN", "VIS" }
            : new[] { "EnableIn", "EnableOut", "EN", "IN" };

        foreach (var member in dataMembers)
        {
            XmlElement dataMemberElement = xmlDoc.CreateElement("DataValueMember");
            dataMemberElement.SetAttribute("Name", member);
            dataMemberElement.SetAttribute("DataType", "BOOL");
            dataMemberElement.SetAttribute("Value", member == "EnableOut" ? "0" : "1"); // EnableOut is set to "0", others to "1"
            structureElement.AppendChild(dataMemberElement);
        }

        // Append Structure to Data
        dataElement.AppendChild(structureElement);

        // Append Data to Tag
        tagElement.AppendChild(dataElement);

        // Append Tag to XML document
        xmlDoc.AppendChild(tagElement);

        progress.Report("Interlock Tag named " + tagElement.Name + "is added to Program");

        // Return the created Tag element
        return tagElement;
    }

    public XmlNode DSETagCreation(string tagName, string descriptionText, IProgress<string> progress)
    {
        // Create XML document
        XmlDocument xmlDoc = new XmlDocument();

        // Ensure tagName starts with a letter (prefix with an underscore if it starts with a digit)
        if (char.IsDigit(tagName[0]))
        {
            tagName = "_" + tagName;
        }

        // Create Tag node with the required attributes
        XmlElement tagElement = xmlDoc.CreateElement("Tag");
        tagElement.SetAttribute("Name", tagName);
        tagElement.SetAttribute("TagType", "Base");
        tagElement.SetAttribute("DataType", "BOOL");
        tagElement.SetAttribute("Radix", "Decimal");
        tagElement.SetAttribute("Constant", "false");
        tagElement.SetAttribute("ExternalAccess", "Read/Write");

        // Create and append Description node with CDATA section
        XmlElement descriptionElement = xmlDoc.CreateElement("Description");
        descriptionElement.AppendChild(xmlDoc.CreateCDataSection(descriptionText));
        tagElement.AppendChild(descriptionElement);

        // Create Data node with Format attribute
        XmlElement dataElement = xmlDoc.CreateElement("Data");
        dataElement.SetAttribute("Format", "Decorated");

        // Create DataValue node with the required attributes and value
        XmlElement dataValueElement = xmlDoc.CreateElement("DataValue");
        dataValueElement.SetAttribute("DataType", "BOOL");
        dataValueElement.SetAttribute("Radix", "Decimal");
        dataValueElement.SetAttribute("Value", "1");

        // Append DataValue node to Data node
        dataElement.AppendChild(dataValueElement);

        // Append Data node to Tag node
        tagElement.AppendChild(dataElement);

        // Append Tag node to the XML document
        xmlDoc.AppendChild(tagElement);

        // Report progress (if necessary)
        progress?.Report($"DSE Tag named {tagElement.GetAttribute("Name")} is added to Program");

        // Return the created Tag element
        return tagElement;
    }

    public string UpdateCoreRefXMLNewInterlockLang(string xmlContent, List<NewInterlockTagDescription> newInterlockTags)
    {
        // Define the language template
        string languageTemplate = @"
<Language>
    <Designation>##InterlockTag##</Designation>
    <LanguageText>##InterlockDescription##</LanguageText>
</Language>";

        // Create a StringBuilder to hold the new <Language> elements
        StringBuilder newLanguageElements = new StringBuilder();

        // Generate the <Language> elements based on the NewInterlockTagDescription list
        foreach (var interlockTag in newInterlockTags)
        {
            if (interlockTag.Description != null)
            {
                // Replace '&' with 'and' in the description to avoid XML errors
                string sanitizedDescription = interlockTag.Description.Replace("&", "and");

                string languageEntry = languageTemplate
                    .Replace("##InterlockTag##", interlockTag.TagName)
                    .Replace("##InterlockDescription##", sanitizedDescription);

                // Append the generated <Language> element
                newLanguageElements.Append(languageEntry);
            }
            
        }

        // Find the index of the closing </Fls.Core.Ref> tag
        int closingTagIndex = xmlContent.LastIndexOf("</Fls.Core.Ref>");
        if (closingTagIndex == -1)
        {
            throw new Exception("Closing </Fls.Core.Ref> tag not found in the XML content.");
        }

        // Insert the new <Language> elements before the closing </Fls.Core.Ref> tag
        string updatedContent = xmlContent.Insert(closingTagIndex, newLanguageElements.ToString());

        return updatedContent;
    }

    private string MethodToExtractNewInterlockDescription(string tagName, XmlDocument xmlDocumentNew)
    {
        var match = Regex.Match(tagName, @"^(.*?)(STR|INT|STP|ENA|FON|FOF)");

        if (match.Success)
        {
            string result = match.Groups[1].Value + "_FP";

            if (xmlDocumentNew != null)
            {
                // Select all tag nodes from xmlDocumentNew
                XmlNodeList tagNodes = xmlDocumentNew.SelectNodes("//Tag");

                if (tagNodes != null)
                {
                    foreach (XmlNode tagNode in tagNodes)
                    {
                        XmlAttribute nameAttribute = tagNode.Attributes?["Name"];

                        if (nameAttribute != null && nameAttribute.Value == result)
                        {
                            XmlNode descriptionNode = tagNode.SelectSingleNode("Description");
                            if (descriptionNode != null && descriptionNode.FirstChild is XmlCDataSection cdataSection)
                            {
                                string resultstring = cdataSection.InnerText;

                                string updatedText = Regex.Replace(cdataSection.InnerText, @",\s*Faceplate", "");
                                cdataSection.InnerText = updatedText;

                                return updatedText;
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    public int? FindMaxInterlockIndex(XmlNode interlockRllContent)
    {
        // Define a regex pattern to match AsysInterlock with indices
        string pattern = @"AsysInterlock\(_\w+STR(\d+)";
        Regex regex = new Regex(pattern);

        int? maxIndex = null; // Initialize maxIndex as nullable int to handle case with no matches

        // Recursively search for matches within the node and its children
        void SearchNodes(XmlNode node)
        {
            if (node.NodeType == XmlNodeType.Element || node.NodeType == XmlNodeType.CDATA)
            {
                // Find matches within the node's inner text
                MatchCollection matches = regex.Matches(node.InnerXml);

                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        // Extract the index part and parse it as an integer
                        int index = int.Parse(match.Groups[1].Value);

                        // Update the maxIndex if the current index is greater or if maxIndex is null
                        if (!maxIndex.HasValue || index > maxIndex)
                        {
                            maxIndex = index;                            
                        }
                    }
                }

                // Recursively search child nodes
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    SearchNodes(childNode);
                }
            }
        }

        // Start the recursive search from the root node
        SearchNodes(interlockRllContent);

        return maxIndex;
    }

    private XmlNode ExtractDataFormatElement(string routineName)
    {
        if (OriginalPrograms != null)
        {
            // Check if Project, Content, Controller, and Tags are not null
            if (Project?.Content?.Controller?.Tags != null)
            {
                XmlNodeList tagNodes = Project.Content.Controller.Tags.SelectNodes("//Tag");
                if (tagNodes != null)
                {
                    foreach (XmlNode tagNode in tagNodes)
                    {
                        XmlAttribute nameAttribute = tagNode.Attributes["Name"];
                        if (nameAttribute != null && nameAttribute.Value == routineName)
                        {
                            XmlNode dataFormatNode = tagNode.SelectSingleNode("Data[@Format='Decorated']");
                            return dataFormatNode;
                        }
                    }
                }
            }
        }
        return null;
    }

    private XmlNode ExtractFBDataFormatElement(string routineName, DbHelper dbHelper, RockwellUpgradeOptions options, string Datatype, L5XProgram program)
    {
        IEnumerable<Dto> one2one = dbHelper.GetOneToOneTagsAddOns(options);
        IEnumerable<Dto> one2many = dbHelper.GetOneToManyTagsAddOns(options);

        if (Datatype == "Positioner")
        {
            var doc = program.OwnerDocument;
            XmlNode GateNode = CreateAsysGateData(doc);
            return GateNode;
        }

        if (Datatype == "Totalizer")
        {
            var doc = program.OwnerDocument;
            XmlNode TotalizerNode = CreateAsysTotalizerData(doc);
            return TotalizerNode;
        }

        if (Datatype == "PID")
        {
            var doc = program.OwnerDocument;
            XmlNode PIDNode = CreateAsysPIDData(doc);
            return PIDNode;
        }

        if (Datatype == "MultiDiverter")
        {
            var doc = program.OwnerDocument;
            XmlNode MultiDiverterNode = CreateAsysMultiDiverterData(doc);
            return MultiDiverterNode;
        }

        L5XTag? originalTag = null;

        if (Datatype != "Positioner" || Datatype != "Totalizer" || Datatype != "PID")
        {
            if (OriginalPrograms != null)
            {
                XmlNodeList tagNodes = OriginalPrograms.SelectNodes("//Tag");
                if (tagNodes != null)
                {
                    foreach (L5XTag tagItem in tagNodes)
                    {
                        XmlAttribute nameAttribute = tagItem.Attributes?["Name"];
                        if (nameAttribute != null && nameAttribute.Value == routineName)
                        {
                            XmlNode dataFormatNode = tagItem;
                            if (dataFormatNode != null)
                            {
                                // Extract the DataType attribute from the dataFormatNode
                                XmlAttribute dataTypeAttribute = dataFormatNode.Attributes?["DataType"];
                                if (dataTypeAttribute != null)
                                {
                                    tagItem.DataType = dataTypeAttribute.Value;

                                    if (routineName == "GOC_BIMOTOR01_FB")
                                    {
                                        tagItem.DataType = "AsysMot2";
                                    }

                                    if (tagItem.DataType == "AsysTotal" || tagItem.DataType == "AsysRcp")
                                    {
                                        Dto? filteredO2O = one2many.FirstOrDefault(i => i.FromObject == tagItem.DataType);

                                        if (filteredO2O != null)
                                        {
                                            if (tagItem.DataType != null && tagItem.DataType.Equals(filteredO2O.FromObject))
                                            {
                                                tagItem.DataType = filteredO2O.ToObject;
                                                XmlNode? nodeToRemove = tagItem.SelectSingleNode("Data[@Format='L5K']");

                                                try
                                                {
                                                    if (!string.IsNullOrEmpty(filteredO2O.XmlStandard))
                                                    {
                                                        tagItem.UpdateDecoratedDataWithNewStandard(filteredO2O.XmlStandard);
                                                        L5XCollection.AddUserMessage(Project, tagItem, originalTag, UserMessageTypes.Information,
                                            "Tag AddOn Data Format=Decorated Replace", "O2O",
                                            $"Tag AddOn {tagItem.DataType} Data Format=Decorated Replaced");
                                                    }
                                                }
                                                catch
                                                {
                                                    L5XCollection.AddUserMessage(Project, tagItem, originalTag, UserMessageTypes.Error,
                                        "Tag AddOn Data Format=Decorated Replace", "O2O",
                                        $"Error While Updating Tag Data Format=Decorated");

                                                }
                                            }
                                        }
                                    }

                                    else
                                    {
                                        Dto? filteredO2O = one2one.FirstOrDefault(i => i.FromObject == tagItem.DataType);

                                        if (filteredO2O != null)
                                        {
                                            if (tagItem.DataType != null && tagItem.DataType.Equals(filteredO2O.FromObject))
                                            {
                                                tagItem.DataType = filteredO2O.ToObject;
                                                XmlNode? nodeToRemove = tagItem.SelectSingleNode("Data[@Format='L5K']");

                                                try
                                                {
                                                    if (!string.IsNullOrEmpty(filteredO2O.XmlStandard))
                                                    {
                                                        tagItem.UpdateDecoratedDataWithNewStandard(filteredO2O.XmlStandard);
                                                        L5XCollection.AddUserMessage(Project, tagItem, originalTag, UserMessageTypes.Information,
                                            "Tag AddOn Data Format=Decorated Replace", "O2O",
                                            $"Tag AddOn {tagItem.DataType} Data Format=Decorated Replaced");
                                                    }
                                                }
                                                catch
                                                {
                                                    L5XCollection.AddUserMessage(Project, tagItem, originalTag, UserMessageTypes.Error,
                                        "Tag AddOn Data Format=Decorated Replace", "O2O",
                                        $"Error While Updating Tag Data Format=Decorated");

                                                }
                                            }
                                        }
                                    }

                                }
                                return dataFormatNode;
                            }
                        }
                    }
                }
            }
        }

        
        return null;
    }

    public XmlNode CreateAsysTotalizerData(XmlDocument doc)
    {
        XmlElement dataElement = doc.CreateElement("Data");
        dataElement.SetAttribute("Format", "Decorated");

        XmlElement structureElement = doc.CreateElement("Structure");
        structureElement.SetAttribute("DataType", "AsysTotalizer");
        dataElement.AppendChild(structureElement);

        Add_DataValueMember(doc, structureElement, "EnableIn", "BOOL", "1");
        Add_DataValueMember(doc, structureElement, "EnableOut", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "CNT_EN", "BOOL", "1");
        Add_DataValueMember(doc, structureElement, "CNT", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "RESET", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "DI_AI", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "AI", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "COUNT", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "TOT_HOUR", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "TOT_DAY", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "TOT_MONTH", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "TOT_YEAR", "REAL", "0.0", "Float");

        return dataElement;
    }

    public XmlNode CreateAsysPIDData(XmlDocument doc)
    {
        XmlElement dataElement = doc.CreateElement("Data");
        dataElement.SetAttribute("Format", "Decorated");

        XmlElement structureElement = doc.CreateElement("Structure");
        structureElement.SetAttribute("DataType", "AsysPID");
        dataElement.AppendChild(structureElement);

        Add_DataValueMember(doc, structureElement, "EnableIn", "BOOL", "1");
        Add_DataValueMember(doc, structureElement, "EnableOut", "BOOL", "1");
        Add_DataValueMember(doc, structureElement, "PV", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "PV_OK", "BOOL", "1");
        Add_DataValueMember(doc, structureElement, "PV_TRACK", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "AUTO_EN", "BOOL", "1");
        Add_DataValueMember(doc, structureElement, "AUTO_FR", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "AUTO_HLC", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "MAN_FR", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "INV", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "EXT1_EN", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "EXT1_FR", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "EXT1_SP", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "FR1_FR", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "FR1_SP", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "FR2_FR", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "FR2_SP", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "EXT2_FR", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "EXT2_SP", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "EXTPV_EN", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "EXTPV", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "EXT_FAULT", "BOOL", "1");
        Add_DataValueMember(doc, structureElement, "PARA_SET1", "BOOL", "1");
        Add_DataValueMember(doc, structureElement, "PARA_SET2", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "PARA_SET3", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "PARA_SET4", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "CV", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "CO", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "AUTO", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "AINORM", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "SPNORM", "REAL", "0.0", "Float");

        return dataElement;
    }

    public XmlNode CreateAsysMultiDiverterData(XmlDocument doc)
    {
        // Create the root Data element with the Format attribute
        XmlElement dataElement = doc.CreateElement("Data");
        dataElement.SetAttribute("Format", "Decorated");

        // Create the Structure element with the DataType attribute
        XmlElement structureElement = doc.CreateElement("Structure");
        structureElement.SetAttribute("DataType", "AsysMultiDiverter");
        dataElement.AppendChild(structureElement);

        // Add DataValueMember elements with respective attributes
        Add_DataValueMember(doc, structureElement, "EnableIn", "BOOL", "1");
        Add_DataValueMember(doc, structureElement, "EnableOut", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "FIXED_POSA", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "FIXED_POSB", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "FIXED_POSC", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "FIXED_POSD", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "ENAB_EXTA", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "ENAB_EXTB", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "ENAB_EXTC", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "ENAB_EXTD", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "CLEAN_EN", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "CLEAN_FR", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "EXT_SP_A", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "EXT_SP_B", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "EXT_SP_C", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "EXT_SP_D", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "ACT_POS1", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "ACT_POS2", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "ACT_POS3", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "ACT_POS4", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "ACTUATOR1_SP", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "ACTUATOR2_SP", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "ACTUATOR3_SP", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "ACTUATOR4_SP", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "PV_PIPE_A", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "PV_PIPE_B", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "PV_PIPE_C", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "PV_PIPE_D", "REAL", "0.0", "Float");
        Add_DataValueMember(doc, structureElement, "CLEAN_ACTIVE", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "FORCE_ACTIVE", "BOOL", "1");
        Add_DataValueMember(doc, structureElement, "OK", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "SP_FAULT", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "_2WAY_SEL", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "_3WAY_SEL", "BOOL", "0");
        Add_DataValueMember(doc, structureElement, "_4WAY_SEL", "BOOL", "0");

        return dataElement;
    }



    private void Add_DataValueMember(XmlDocument doc, XmlElement parent, string name, string dataType, string value, string radix = null)
    {
        XmlElement dataValueMember = doc.CreateElement("DataValueMember");
        dataValueMember.SetAttribute("Name", name);
        dataValueMember.SetAttribute("DataType", dataType);
        dataValueMember.SetAttribute("Value", value);
        if (radix != null)
        {
            dataValueMember.SetAttribute("Radix", radix);
        }
        parent.AppendChild(dataValueMember);
    }

    public XmlNode CreateAsysGateData(XmlDocument doc)
    {
        XmlElement dataElement = doc.CreateElement("Data");
        dataElement.SetAttribute("Format", "Decorated");

        XmlElement structureElement = doc.CreateElement("Structure");
        structureElement.SetAttribute("DataType", "AsysGate");
        dataElement.AppendChild(structureElement);

        _AddDataValueMember(doc, structureElement, "EnableIn", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "EnableOut", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "SEL", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "PREQ1", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "PREQ2", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "INCR", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "MCC", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "RDY", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "OVL", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "RET1", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "RET2", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "LSP1", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "LSP2", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "LSA1", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "LSA2", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "TRQ1", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "TRQ2", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "LSTR1", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "LSTR2", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "LSTP", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "REM", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "SSW", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "TFA", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "DRDY", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "STALL", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "EARTH", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "UBAL", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "GFA", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "GWA", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "BUSF", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "DOPR", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "DTEST", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "ACK", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "AI1", "REAL", "0.0", "Float");
        _AddDataValueMember(doc, structureElement, "AI2", "REAL", "0.0", "Float");
        _AddDataValueMember(doc, structureElement, "AI3", "REAL", "0.0", "Float");
        _AddDataValueMember(doc, structureElement, "SP", "REAL", "0.0", "Float");
        _AddDataValueMember(doc, structureElement, "POS", "REAL", "0.0", "Float");
        _AddDataValueMember(doc, structureElement, "POS_OK", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "CON1", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "CON2", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "LAMP1", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "LAMP2", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "SACK", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "POS1", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "POS2", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "RUN1", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "RUN2", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "OK", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "LOC", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "MAN", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "AUT", "BOOL", "1");
        _AddDataValueMember(doc, structureElement, "SS", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "RES", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "TESTP", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "ASTW", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "AL_SUPR", "BOOL", "0");
        _AddDataValueMember(doc, structureElement, "AO1a", "REAL", "0.0", "Float");
        _AddDataValueMember(doc, structureElement, "AO1b", "REAL", "0.0", "Float");
        _AddDataValueMember(doc, structureElement, "AO2a", "REAL", "0.0", "Float");
        _AddDataValueMember(doc, structureElement, "AO2b", "REAL", "0.0", "Float");
        _AddDataValueMember(doc, structureElement, "AO3a", "REAL", "0.0", "Float");
        _AddDataValueMember(doc, structureElement, "AO3b", "REAL", "0.0", "Float");

        return dataElement;
    }

    private void _AddDataValueMember(XmlDocument doc, XmlElement parent, string name, string dataType, string value, string radix = null)
    {
        XmlElement dataValueMember = doc.CreateElement("DataValueMember");
        dataValueMember.SetAttribute("Name", name);
        dataValueMember.SetAttribute("DataType", dataType);
        dataValueMember.SetAttribute("Value", value);
        if (radix != null)
        {
            dataValueMember.SetAttribute("Radix", radix);
        }
        parent.AppendChild(dataValueMember);
    }

    private XmlNode ExtractOthersDataFormatElement(string routineName, DbHelper dbHelper, RockwellUpgradeOptions options)
    {
        List<Dto> one2one = dbHelper.GetOneToOneTagsDataType(options);
        List<FpMemberDto> fpMembers = dbHelper.GetFpMembers(options);

        L5XTag? originalTag = null;

        if (OriginalPrograms != null)
        {
            XmlNodeList tagNodes = OriginalPrograms.SelectNodes("//Tag");
            if (tagNodes != null)
            {
                foreach (L5XTag tagItem in tagNodes)
                {
                    XmlAttribute nameAttribute = tagItem.Attributes?["Name"];
                    if (nameAttribute != null && nameAttribute.Value == routineName)
                    {
                        XmlNode dataFormatNode = tagItem;
                        if (dataFormatNode != null)
                        {
                            // Extract the DataType attribute from the dataFormatNode
                            XmlAttribute dataTypeAttribute = dataFormatNode.Attributes?["DataType"];
                            if (dataTypeAttribute != null)
                            {
                                tagItem.DataType = dataTypeAttribute.Value;

                                Dto? filteredO2O = one2one.FirstOrDefault(i => i.FromObject == tagItem.DataType);

                                if (filteredO2O != null)
                                {
                                    if (tagItem.DataType != null && tagItem.DataType.Equals(filteredO2O.FromObject))
                                    {
                                        tagItem.DataType = filteredO2O.ToObject;
                                        XmlNode? nodeToRemove = tagItem.SelectSingleNode("Data[@Format='L5K']");

                                        try
                                        {
                                            if (!string.IsNullOrEmpty(filteredO2O.XmlStandard))
                                            {
                                                tagItem.UpdateDecoratedDataWithNewStandard(filteredO2O.XmlStandard);
                                                L5XCollection.AddUserMessage(Project, tagItem, originalTag, UserMessageTypes.Information,
                                    "Tag AddOn Data Format=Decorated Replace", "O2O",
                                    $"Tag AddOn {tagItem.DataType} Data Format=Decorated Replaced");
                                            }
                                        }
                                        catch
                                        {
                                            L5XCollection.AddUserMessage(Project, tagItem, originalTag, UserMessageTypes.Error,
                                "Tag AddOn Data Format=Decorated Replace", "O2O",
                                $"Error While Updating Tag Data Format=Decorated");

                                        }
                                    }
                                }


                            }
                            return dataFormatNode;
                        }
                    }
                }
            }
        }
        return null;
    }

    private void SetTagAttributes(XmlElement tag, string name, string tagType, string dataType, string usage, bool constant, string externalAccess)
    {
        tag.SetAttribute("Name", name);

        if (!string.IsNullOrEmpty(tagType))
        {
            tag.SetAttribute("TagType", tagType);
        }

        tag.SetAttribute("DataType", dataType);
        tag.SetAttribute("Usage", usage);
        tag.SetAttribute("Constant", constant.ToString().ToLower());
        tag.SetAttribute("ExternalAccess", externalAccess);        
    }

    public List<string> GetPwrRung(string oldText, DbHelper dbHelper, RockwellUpgradeOptions options, string dataType, IProgress<string> progress)
    {
        // Define a regex pattern to capture the operator
        string pattern = @"^(\w+)\s*\((.*)\);";

       
            // Use regex to match the pattern
            Match match = Regex.Match(oldText, pattern);

            if (match.Success)
            {
                // Extract the operator
                string oldOperator = match.Groups[1].Value;

                // Get the conversion mapping for the operator from the database
                string newOperator = dbHelper.GetOperatorConversionMapping(oldOperator, options);

                if (!string.IsNullOrEmpty(newOperator))
                {
                    string? pwr_rung = dbHelper.GetPowerOperand(newOperator, oldText, dataType);

                    List<string> newRungText = new List<string>();

                    if (pwr_rung == "1")
                    {
                        newRungText.Add($"OTE(PWR);");
                        newRungText.Add($"XIC(PWR)OTE(FB.PWR);");
                        progress.Report("Rule No. 13 Applied XIC(<ACESYSv7.7. FB APS pin operand>)OTE(FB.PWR) when contant is 1");
                    }

                    else if (pwr_rung == "0")
                    {
                        newRungText.Add($"AFI()OTE(PWR);");
                        newRungText.Add($"XIC(PWR)OTE(FB.PWR);");
                        progress.Report("Rule No. 13 Applied XIC(<ACESYSv7.7. FB APS pin operand>)OTE(FB.PWR) when contant is 0");
                }

                    else if ((pwr_rung != "1") && (pwr_rung != "0") && (pwr_rung != null))
                    {
                        newRungText.Add($"XIC(FB.{pwr_rung})OTE(PWR);");
                        newRungText.Add($"XIC(PWR)OTE(FB.PWR);");
                        progress.Report("Rule No. 13 Applied XIC(<ACESYSv7.7. FB APS pin operand>)OTE(FB.PWR) when contant neither 0 or 1");
                }

                    else
                    {
                        return null;
                    }

                    // Replace the old operator with the new operator and the old operands with the fetched operands
                    return newRungText;
                }
            }
        return null;     
    }

    private List<XmlNode> ConvertRungs(L5XRoutine routine, DbHelper dbHelper, string dataType, RockwellUpgradeOptions options, int NewProgramIndex, string Routinename, IProgress<string> progress, L5XProgram program, L5XTags tags)
    {
        List<XmlNode> convertedRungs = new List<XmlNode>();
        XmlNodeList rungNodes = routine.SelectNodes("RLLContent/Rung");
        XmlDocument doc = routine.OwnerDocument;
        List<string> DSERung = new List<string>();
        IEnumerable<string> newText = new List<string>();
        List<string> newTextList = newText.Distinct().ToList();


        if (rungNodes != null)
        {
            int rungCounter = 1; // Counter for identifying the second rung
            int newRungNumber = 1; // Initialize the new rung counter

            foreach (XmlNode rungNode in rungNodes)
            {
                if (rungCounter == 1 && (dataType == "Analog" || dataType == "HLC" || dataType == "PID"))
                {
                    // Add the second rung without conversion
                    XmlNode clonedRungNode = rungNode.CloneNode(true);
                    UpdateRungNumber(clonedRungNode, newRungNumber);
                    convertedRungs.Add(clonedRungNode);
                    newRungNumber++; // Increment the rung counter
                    rungCounter++; // Increment the rungCounter
                    continue; // Skip to the next iteration to avoid processing this rung further
                }

                XmlNode textNode = rungNode.SelectSingleNode("Text");
                XmlNode commentNode = rungNode.SelectSingleNode("Comment");
                if (textNode != null)
                {
                    string oldText = textNode.InnerText;
                    string firstOperand = string.Empty;
                    string secondOperand = string.Empty;

                    // Apply specific conversion rules using regex
                    string processedText = ApplySpecificConversions(oldText, dataType, progress);

                    processedText = dbHelper.ApplyTextConversionSimulationRungs(dataType, processedText);

                    string patternGON = @"\bOTE\(([^)]+\.GON)\)";
                    Match matchGON = Regex.Match(processedText, patternGON);

                    if (matchGON.Success)
                    {
                        string pattern = @"\[(?<first>[^\[\],]+)\s*,\s*(?<second>[^\[\]]+)\]";
                        MatchCollection matches = Regex.Matches(processedText, pattern);

                        // Dictionary to group SecondOperands by FirstOperand
                        Dictionary<string, List<string>> operandGroups = new Dictionary<string, List<string>>();

                        foreach (Match match in matches)
                        {
                            firstOperand = match.Groups["first"].Value.Trim();
                            secondOperand = match.Groups["second"].Value.Trim();

                            string extractionPattern3 = @"(XIC|XIO)\((?<code>.*?)_FP";
                            Match extractionMatch2 = Regex.Match(firstOperand, extractionPattern3);

                            if (extractionMatch2.Success)
                            {
                                string routineName = extractionMatch2.Groups["code"].Value.Trim();
                                
                                if (!operandGroups.ContainsKey(routineName))
                                {
                                    operandGroups[routineName] = new List<string>();
                                }
                                
                                operandGroups[routineName].Add(secondOperand);
                            }
                        }

                        // Iterate through the operand groups and call the MethodToAppendSelectorRung
                        foreach (var kvp in operandGroups)
                        {
                            string routineName = kvp.Key;
                            List<string> secondOperandList = kvp.Value;

                            // Join the second operands as a string in the format [SecondOperand1, SecondOperand2]
                            string joinedSecondOperands = $"[{string.Join(", ", secondOperandList)}]";

                            // Call the method with the routine name and the joined second operands
                            MethodToAppendSelectorRung(routineName, joinedSecondOperands);
                        }
                    }

                    // Retrieve the PwrRung
                    List<string> PwrRungs = GetPwrRung(processedText, dbHelper, options, dataType, progress);

                    if (PwrRungs != null)
                    {
                        foreach (var rung in PwrRungs)
                        {
                            // Create a new rung node with the PwrRung if it exists
                            if (!string.IsNullOrEmpty(rung))
                            {
                                XmlDocument tempDoc = new XmlDocument();
                                tempDoc.LoadXml($"<Rung Number='{newRungNumber}' Type='{rungNode.Attributes["Type"].Value}'></Rung>");
                                XmlNode pwrRungNode = doc.ImportNode(tempDoc.DocumentElement, true);

                                // Append the comment node if it exists
                                if (commentNode != null)
                                {
                                    XmlNode importedCommentNode = doc.ImportNode(commentNode, true);
                                    pwrRungNode.AppendChild(importedCommentNode);
                                }

                                // Append the text node
                                XmlNode importedTextNode = doc.CreateElement("Text");
                                importedTextNode.InnerXml = $"<![CDATA[{rung}]]>";
                                pwrRungNode.AppendChild(importedTextNode);

                                convertedRungs.Add(pwrRungNode);
                                newRungNumber++; // Increment the rung counter
                            }
                        }
                    }

                    // Extract and transform AsysGroup operands
                    if (dataType == "Group")
                    {
                        string transformedText = TransformAsysGroupOperands(processedText);

                        if (transformedText != null)
                        {
                            MethodToAddChildProgramToDepartment(transformedText, Routinename,progress);
                        }
                    }

                    string patternOTE = @"\bOTE\(([^)]+\.DSE)\)";

                    Match matchOTE = Regex.Match(processedText, patternOTE);


                    if (matchOTE.Success)
                    {
                        string operand = matchOTE.Groups[1].Value.Trim();
                        string beforeOTE = processedText.Substring(0, matchOTE.Index);

                        if (!Regex.IsMatch(beforeOTE, @"\bAFI\b"))
                        {
                            string InputOperand = beforeOTE;
                            string firstRung = InputOperand + "OTE(DSE);";
                            string secondRung = $"XIO(DSE)OTE({operand});";

                            XmlNode DSETag = DSETagCreation("DSE","Delayed Stop",progress);
                            
                            XmlDocument programDoc = program.OwnerDocument;                            
                            XmlNode importedDSETag = programDoc.ImportNode(DSETag, true); 

                            // Append the imported node to the tags node
                            tags.AppendChild(importedDSETag);

                            // Now append tags to program
                            program.AppendChild(tags);


                            // Directly process these rungs
                            DSERung.Add(firstRung);
                            DSERung.Add(secondRung);

                            List<string> tempDSERung = new List<string>(DSERung);

                            // Process DSERung without iterating multiple times
                            foreach (var item in tempDSERung)
                            {
                                var ConvertedRungs = ConvertRungText(item, dbHelper, options, dataType, routine, NewProgramIndex, Routinename, progress, program, tags);

                                // Add the converted text to the newTextList
                                newTextList.AddRange(ConvertedRungs);
                            }

                            // Clear DSERung once processed to prevent future duplication
                            DSERung.Clear();

                            foreach (var item in newTextList)
                            {
                                // Create a new rung node with the converted text
                                XmlDocument tempDocForNewText = new XmlDocument();
                                tempDocForNewText.LoadXml($"<Rung Number='{newRungNumber}' Type='{rungNode.Attributes["Type"].Value}'></Rung>");
                                XmlNode newRungNode = doc.ImportNode(tempDocForNewText.DocumentElement, true);

                                // Append the comment node if it exists
                                if (commentNode != null)
                                {
                                    XmlNode importedCommentNode = doc.ImportNode(commentNode, true);
                                    newRungNode.AppendChild(importedCommentNode);
                                }

                                // Append the text node
                                XmlNode importedTextNode = doc.CreateElement("Text");
                                importedTextNode.InnerXml = $"<![CDATA[{item}]]>";
                                newRungNode.AppendChild(importedTextNode);

                                convertedRungs.Add(newRungNode);
                                newRungNumber++; // Increment the rung counter
                            }

                        }
                    }
                    else
                    {
                        newText = ConvertRungText(processedText, dbHelper, options, dataType, routine, NewProgramIndex, Routinename, progress, program, tags);
                        newTextList = newText.Distinct().ToList();

                        newText = newText.Select(text => Regex.Replace(text, @"\bRESET\b", "RST"));
                        // Check if processedText contains "AsysSchenck" or "AsysPfister"
                        Match match = Regex.Match(processedText, @"\b(AsysSchenck|AsysPfister|AsysDosax)\b");
                        if (match.Success)
                        {
                            string operatorFound = match.Value;

                            if (operatorFound == "AsysSchenck")
                            {
                                string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", 7))}),";

                                Match match2 = Regex.Match(processedText, pattern);

                                if (match2.Success)
                                {
                                    string eighthOperand = match2.Groups[8].Value.Trim();

                                    if (eighthOperand == "1")
                                    {
                                        string rung1 = "OTE(FB.SSW);";
                                        newTextList.Insert(0, rung1);
                                    }

                                    else if (eighthOperand == "0")
                                    {
                                        string rung2 = "AFI()OTE(FB.SSW);";
                                        newTextList.Insert(0, rung2);
                                    }

                                    else
                                    {
                                        string rung3 = $"XIC({eighthOperand})OTE(FB.SSW);";
                                        newTextList.Insert(0, rung3);
                                    }

                                }

                                string pattern2 = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", 9))}),";

                                Match match3 = Regex.Match(processedText, pattern2);

                                if (match3.Success)
                                {
                                    string tenthOperand = match3.Groups[10].Value.Trim();

                                    if (tenthOperand == "1")
                                    {
                                        string rung1 = "OTE(FB.FSTOP);";
                                        newTextList.Insert(1, rung1);
                                    }

                                    else if (tenthOperand == "0")
                                    {
                                        string rung2 = "AFI()OTE(FB.STOP);";
                                        newTextList.Insert(1, rung2);
                                    }

                                    else
                                    {
                                        string rung3 = $"XIC({tenthOperand})OTE(FB.STOP);";
                                        newTextList.Insert(1, rung3);
                                    }
                                }
                            }

                            else if (operatorFound == "AsysPfister")
                            {
                                string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", 7))}),";

                                Match match2 = Regex.Match(processedText, pattern);

                                if (match2.Success)
                                {
                                    string eighthOperand = match2.Groups[8].Value.Trim();

                                    if (eighthOperand == "1")
                                    {
                                        string rung1 = "OTE(FB.SSW);";
                                        newTextList.Insert(0, rung1);
                                    }

                                    else if (eighthOperand == "0")
                                    {
                                        string rung2 = "AFI()OTE(FB.SSW);";
                                        newTextList.Insert(0, rung2);
                                    }

                                    else
                                    {
                                        string rung3 = $"XIC({eighthOperand})OTE(FB.SSW);";
                                        newTextList.Insert(0, rung3);
                                    }

                                }

                                string pattern2 = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", 8))}),";

                                Match match3 = Regex.Match(processedText, pattern2);

                                if (match3.Success)
                                {
                                    string ninthOperand = match3.Groups[9].Value.Trim();

                                    if (ninthOperand == "1")
                                    {
                                        string rung1 = "OTE(FB.FSTOP);";
                                        newTextList.Insert(1, rung1);
                                    }

                                    else if (ninthOperand == "0")
                                    {
                                        string rung2 = "AFI()OTE(FB.STOP);";
                                        newTextList.Insert(1, rung2);
                                    }

                                    else
                                    {
                                        string rung3 = $"XIC({ninthOperand})OTE(FB.STOP);";
                                        newTextList.Insert(1, rung3);
                                    }
                                }
                            }

                            else if (operatorFound == "AsysDosax")
                            {
                                string pattern2 = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", 9))}),";

                                Match match3 = Regex.Match(processedText, pattern2);

                                if (match3.Success)
                                {
                                    string tenthOperand = match3.Groups[10].Value.Trim();

                                    if (tenthOperand == "1")
                                    {
                                        string rung1 = "OTE(FB.FSTOP);";
                                        newTextList.Insert(0, rung1);
                                    }

                                    else if (tenthOperand == "0")
                                    {
                                        string rung2 = "AFI()OTE(FB.STOP);";
                                        newTextList.Insert(0, rung2);
                                    }

                                    else
                                    {
                                        string rung3 = $"XIC({tenthOperand})OTE(FB. STOP);";
                                        newTextList.Insert(0, rung3);
                                    }
                                }
                            }

                        }

                        // If the list is null, empty, or contains only empty strings, use the processed text
                        foreach (var item in newTextList)
                        {
                            // Create a new rung node with the converted text
                            XmlDocument tempDocForNewText = new XmlDocument();
                            tempDocForNewText.LoadXml($"<Rung Number='{newRungNumber}' Type='{rungNode.Attributes["Type"].Value}'></Rung>");
                            XmlNode newRungNode = doc.ImportNode(tempDocForNewText.DocumentElement, true);

                            // Append the comment node if it exists
                            if (commentNode != null)
                            {
                                XmlNode importedCommentNode = doc.ImportNode(commentNode, true);
                                newRungNode.AppendChild(importedCommentNode);
                            }

                            // Append the text node
                            XmlNode importedTextNode = doc.CreateElement("Text");
                            importedTextNode.InnerXml = $"<![CDATA[{item}]]>";
                            newRungNode.AppendChild(importedTextNode);

                            convertedRungs.Add(newRungNode);
                            newRungNumber++; // Increment the rung counter
                        }
                    }
                }                   
                rungCounter++; // Increment the rung counter after processing each rung
            }
        }

        // Log the number of converted rungs for debugging
        Console.WriteLine($"Converted {convertedRungs.Count} rungs.");

        return convertedRungs;
    }

    private void MethodToAppendSelectorRung(string routineName, string secondOperand)
    {
        var programsToModify = Project.Content?.Controller?.Programs;

        if (!string.IsNullOrEmpty(secondOperand))
        {
            string patternXIC = @"XIC\((?<content>.+?)\)";
            string patternXIO = @"XIO\((?<content>.+?)\)";

            // Extract operands within square brackets (e.g., [XIO(...), XIO(...)])
            string bracketedPattern = @"\[(?<operands>[^\[\]]+)\]";
            Match bracketMatch = Regex.Match(secondOperand, bracketedPattern);

            string updatedSecondOperand = secondOperand;

            // Handle transformation for operands inside square brackets
            if (bracketMatch.Success)
            {
                string operands = bracketMatch.Groups["operands"].Value;
                string[] individualOperands = operands.Split(new[] { ", " }, StringSplitOptions.None);

                List<string> transformedOperands = new List<string>();

                // Iterate over each operand inside the brackets
                foreach (string operand in individualOperands)
                {
                    string updatedOperand = operand;

                    // Toggle between XIC and XIO for each operand
                    if (Regex.IsMatch(operand, patternXIC))
                    {
                        updatedOperand = Regex.Replace(operand, patternXIC, "XIO(${content})");
                    }
                    else if (Regex.IsMatch(operand, patternXIO))
                    {
                        updatedOperand = Regex.Replace(operand, patternXIO, "XIC(${content})");
                    }

                    // Add transformed operand to the list
                    transformedOperands.Add(updatedOperand);
                }

                if (transformedOperands.Count == 1)
                {                    
                    updatedSecondOperand = $"{string.Join(", ", transformedOperands)}";
                }

                else
                {
                    updatedSecondOperand = $"[{string.Join(", ", transformedOperands)}]";
                }                
                
            }
            else
            {
                // Single operand transformation if not in brackets
                if (Regex.IsMatch(secondOperand, patternXIC))
                {
                    updatedSecondOperand = Regex.Replace(secondOperand, patternXIC, "XIO(${content})");
                }
                else if (Regex.IsMatch(secondOperand, patternXIO))
                {
                    updatedSecondOperand = Regex.Replace(secondOperand, patternXIO, "XIC(${content})");
                }
            }

            // Append OTE command to the updated operand string
            updatedSecondOperand += "OTE(FB.SEL);";

            // Continue processing by adding this updated operand to the rung
            if (programsToModify != null)
            {
                foreach (L5XProgram originalProgram in programsToModify)
                {
                    if (originalProgram.ProgramName.Equals(routineName))
                    {
                        foreach (L5XRoutine routine in originalProgram.Routines)
                        {
                            XmlNodeList rungNodes = routine.SelectNodes("RLLContent/Rung");
                            if (rungNodes != null)
                            {
                                bool rungExists = false;
                                int insertionIndex = -1;
                                XmlNode asysMoRungNode = null;
                                int nextRungNumber = 0;

                                // Check if the updated operand already exists in any Rung
                                foreach (XmlNode rungNode in rungNodes)
                                {
                                    XmlNode textContentNode = rungNode.SelectSingleNode("Text");
                                    if (textContentNode != null && textContentNode.InnerText.Contains(updatedSecondOperand))
                                    {
                                        rungExists = true;
                                        break;
                                    }
                                }

                                // If the rung already exists, skip adding it
                                if (rungExists)
                                {
                                    continue;
                                }

                                // Find the Rung with "AsysMotor" in its Text node and determine next rung number
                                for (int i = 0; i < rungNodes.Count; i++)
                                {
                                    XmlNode textContentNode = rungNodes[i].SelectSingleNode("Text");
                                    if (textContentNode != null && textContentNode.InnerText.Contains("AsysMotor"))
                                    {
                                        asysMoRungNode = rungNodes[i];
                                        insertionIndex = i;
                                        break;
                                    }

                                    // Increment nextRungNumber to be the maximum of the current rungs
                                    XmlAttribute numberAttribute = rungNodes[i].Attributes["Number"];
                                    if (numberAttribute != null && int.TryParse(numberAttribute.Value, out int currentRungNumber))
                                    {
                                        nextRungNumber = Math.Max(nextRungNumber, currentRungNumber + 1);
                                    }
                                }

                                // Create the new Rung node with the next available rung number
                                XmlDocument tempDoc = new XmlDocument();
                                XmlElement selRungNode = tempDoc.CreateElement("Rung");

                                // Set the Number attribute first
                                selRungNode.SetAttribute("Number", nextRungNumber.ToString());
                                // Set the Type attribute second
                                selRungNode.SetAttribute("Type", "N");

                                // Append the Text node with the updated operand inside a CDATA block
                                XmlElement textNode = tempDoc.CreateElement("Text");
                                textNode.InnerXml = $"<![CDATA[{updatedSecondOperand}]]>";
                                selRungNode.AppendChild(textNode);

                                // Insert the new SelRungNode before the identified "AsysMotor" rung
                                if (insertionIndex != -1 && asysMoRungNode != null)
                                {
                                    XmlNode rllContentNode = asysMoRungNode.ParentNode;
                                    XmlNode importedSelRungNode = rllContentNode.OwnerDocument.ImportNode(selRungNode, true);

                                    // Insert the imported node before the AsysMotor rung
                                    rllContentNode.InsertBefore(importedSelRungNode, asysMoRungNode);

                                    // Renumber the remaining rungs sequentially, starting from the inserted rung
                                    int newRungNumber = nextRungNumber + 1;
                                    for (int i = insertionIndex; i < rungNodes.Count; i++)
                                    {
                                        XmlAttribute numberAttribute = rungNodes[i].Attributes["Number"];
                                        if (numberAttribute != null)
                                        {
                                            numberAttribute.Value = newRungNumber.ToString();
                                            newRungNumber++;
                                        }
                                    }
                                }

                                break; // Exit after processing the correct routine
                            }
                        }
                    }
                }
            }
        }
    }

    private void MethodToAddChildProgramToDepartment(string transformedText, string routineName, IProgress<string> progress)
    {
        DepartmentName = transformedText;
        GroupName = routineName;

        var programsToModify = Project.Content?.Controller?.Programs;

        if (programsToModify != null)
        {
            foreach (L5XProgram program in programsToModify)
            {
                // Check if the program name matches the transformed text
                if (program.ProgramName == transformedText)
                {
                    // Create a new ChildProgram node
                    XmlDocument doc = program.OwnerDocument;
                    int seq = GetNewSequenceNumber(); // Assume you have a method to get a new sequence number
                    var childProgramNode = new L5XChildProgram(null, "ChildProgram", null, doc, seq);
                    childProgramNode.ChildProgramName = routineName;

                    // Find the ChildPrograms node or create it if it doesn't exist
                    XmlNode? childProgramsNode = program.SelectSingleNode("ChildPrograms");
                    if (childProgramsNode == null)
                    {
                        childProgramsNode = doc.CreateElement("ChildPrograms");
                        program.AppendChild(childProgramsNode);
                    }

                    // Append the new child program node to the ChildPrograms node
                    childProgramsNode.AppendChild(childProgramNode);
                    progress.Report("Group ChildProgram name " + childProgramNode.ChildProgramName + " has been to Department Program names" + transformedText);
                    break;
                }
            }
        }        
    }

    private void MethodToAddChildProgramToMasterProgram(string DeptProgramName, IProgress<string> progress)
    {
        DepartmentName = DeptProgramName;        

        var programsToModify = Project.Content?.Controller?.Programs;
        ControllerName = docNew.Root.Attribute("TargetName")?.Value + "Master";

        if (programsToModify != null)
        {
            foreach (L5XProgram program in programsToModify)
            {
                // Check if the program name matches the transformed text
                if (program.ProgramName == ControllerName)
                {
                    // Create a new ChildProgram node
                    XmlDocument doc = program.OwnerDocument;
                    int seq = GetNewSequenceNumber(); // Assume you have a method to get a new sequence number
                    var childProgramNode = new L5XChildProgram(null, "ChildProgram", null, doc, seq);
                    childProgramNode.ChildProgramName = DeptProgramName;

                    // Find the ChildPrograms node or create it if it doesn't exist
                    XmlNode? childProgramsNode = program.SelectSingleNode("ChildPrograms");
                    if (childProgramsNode == null)
                    {
                        childProgramsNode = doc.CreateElement("ChildPrograms");
                        program.AppendChild(childProgramsNode);
                    }

                    // Append the new child program node to the ChildPrograms node
                    childProgramsNode.AppendChild(childProgramNode);
                    progress.Report("Department ChildProgram name " + childProgramNode.ChildProgramName + " has been to Department Program names" + ControllerName);
                    break;
                }
            }
        }
    }

    // Assume you have a method to get a new sequence number
    private int GetNewSequenceNumber()
    {
        // Implementation for generating a new sequence number
        return 1; // Placeholder implementation
    }

    private string TransformAsysGroupOperands(string text)
    {
        string pattern = @"AsysGrp\([^,]+,([^,]+),[^)]+\)";
        var match = Regex.Match(text, pattern);

        if (match.Success)
        {
            string secondOperand = match.Groups[1].Value;
            // Use regex to extract the content between the underscore and the dot
            var regexMatch = Regex.Match(secondOperand, @"(.*?)_CMD");
            if (regexMatch.Success)
            {
                return regexMatch.Groups[1].Value;
            }
        }

        return null;
    }

    // Helper method to update the rung number attribute
    private void UpdateRungNumber(XmlNode rungNode, int newRungNumber)
    {
        if (rungNode.Attributes != null && rungNode.Attributes["Number"] != null)
        {
            rungNode.Attributes["Number"].Value = newRungNumber.ToString();
        }
    }

    // Helper method to apply specific conversions using regex
    private string ApplySpecificConversions(string text, string datatype, IProgress<string> progress)
    {
        Match match = Regex.Match(text, @"(XIC|XIO|[\w]+)\((.*?)\)OTE\((.*?)_FB\.EXT_CTRL\)");

        if (match.Success)
        {
            string elementType = match.Groups[1].Value;
            string extractedElement = match.Groups[2].Value;
            newExtStart_Reset = $"{match.Groups[1].Value}({match.Groups[2].Value})OTE(EXT_CTRL);";
            text = newExtStart_Reset;
        }

        // Check and replace _FP.MD_None with _FP.Sel_MD and convert XIO --> XIC, XIC --> XIO & OTE --> OTU
        if (Regex.IsMatch(text, @"_FP\.MD_None\b"))
        {
            text = Regex.Replace(text, @"_FP\.MD_None\b", "_FP.Sel_MD");

            if (Regex.IsMatch(text, @"\bXIO\b"))
            {
                text = Regex.Replace(text, @"\bXIO\b", "XIC");
            }

            else if (Regex.IsMatch(text, @"\bXIC\b"))
            {
                text = Regex.Replace(text, @"\bXIC\b", "XIO");
            }

            if (Regex.IsMatch(text, @"\bOTE\b"))
            {
                text = Regex.Replace(text, @"\bOTE\b", "OTU");
            }

            progress.Report("Rule No. 3 is applied in _FP.MD_None conversion");

        }

        if (Regex.IsMatch(text, @"OTE\(.*?_FB\.EXT_RESET\b\)"))            
        {
            text = Regex.Replace(text, @"_FB\.EXT_RESET\b", ".FB.RST");            

            // Check if AFI() is present
            bool containsAFI = Regex.IsMatch(text, @"AFI\(\)");

            if (containsAFI == false)
            {
                if (Regex.IsMatch(text, @".FB\.RST\b"))
                {
                    text = "XIC(EXT_CTRL)" + text;
                }
            }

            text = Regex.Replace(text, @"OTE\((.*?)\.FB\.RST\b\)", @"OTE(\$1.FB.RST)");
            progress.Report("Rule No. 3 is applied in EXT_RESET conversion");

        }

        // Replace _FB.EXT_START with _FB.START and handle XIC/XIO outer elements, but exclude OTE
        if (Regex.IsMatch(text, @"OTE\(.*?_FB\.EXT_START\b\)"))
        {
            text = Regex.Replace(text, @"_FB\.EXT_START\b", ".FB.START");

            // Check if AFI() is present
            bool containsAFI = Regex.IsMatch(text, @"AFI\(\)");

            if (containsAFI == false)
            {
                if (Regex.IsMatch(text, @".FB.START\b"))
                {
                    text = "XIC(EXT_CTRL)" + text;
                }
            }

            // Add backslash before OTE element containing .FB.STOP
            text = Regex.Replace(text, @"OTE\((.*?)\.FB\.START\b\)", @"OTE(\$1.FB.START)");
            progress.Report("Rule No. 3 is applied in EXT_START conversion");

        }

        // Replace _FB.EXT_STOP with _FB.STOP and handle XIC/XIO outer elements, but exclude OTE
        if (Regex.IsMatch(text, @"OTE\(.*?_FB\.EXT_STOP\b\)"))
        {
            text = Regex.Replace(text, @"_FB\.EXT_STOP\b", ".FB.STOP");            

            // Check if AFI() is present
            bool containsAFI = Regex.IsMatch(text, @"AFI\(\)");

            if (containsAFI == false)
            {
                if (Regex.IsMatch(text, @".FB\.STOP\b"))
                {
                    if (Regex.IsMatch(text, @"^OTE\(.*?\)"))
                    {
                        text = $"XIO(EXT_CTRL)" + text;
                    }
                    else
                    {
                        text = $"[XIO(EXT_CTRL)," + text;
                        text = Regex.Replace(text, @"(OTE\(.*?\))", "]$1");
                    }
                }
            }

            // Add backslash before OTE element containing .FB.STOP
            text = Regex.Replace(text, @"OTE\((.*?)\.FB\.STOP\b\)", @"OTE(\$1.FB.STOP)");
            progress.Report("Rule No. 3 is applied in EXT_STOP conversion");

        }


        if ((Regex.IsMatch(text, @"XIC\(.*?_FB\.EXT_START\b\)") || Regex.IsMatch(text, @"XIO\(.*?_FB\.EXT_START\b\)")) &&
    !Regex.IsMatch(text, @"OTE\(.*?_FB\.EXT_START\b\)"))
        {
            string pattern1 = @"(?<=\w+)_FB\.";
            string replacement1 = ".FB.";
            text = Regex.Replace(text, pattern1, replacement1);

            string pattern2 = @"(RLXV7CS101G01\.FB\.\w+)";
            string replacement2 = @"\$1";
            text = Regex.Replace(text, pattern2, replacement2);

            string pattern3 = @"EXT_START";
            string replacement3 = "START";
            text = Regex.Replace(text, pattern3, replacement3);

            string pattern4 = @"EXT_STOP";
            string replacement4 = "STOP";
            text = Regex.Replace(text, pattern4, replacement4);

            string pattern5 = @"EXT_RESET";
            string replacement5 = "RST";
            text = Regex.Replace(text, pattern5, replacement5);

            progress.Report("Rule No. 3 is applied in OTE(EXT_START) conversion");
        }

        // Replace _FB.EXT_STOP with _FB.STOP and handle XIC/XIO outer elements, but exclude OTE
        if ((Regex.IsMatch(text, @"XIC\(.*?_FB\.EXT_STOP\b\)") || Regex.IsMatch(text, @"XIO\(\.*?_FB\.EXT_STOP\b\)")) &&
            !Regex.IsMatch(text, @"OTE\(.*?_FB\.EXT_STOP\b\)"))
        {
            text = Regex.Replace(text, @"XIC\((.*?)_FB\.EXT_STOP\b\)", @"XIC(\$1.FB.STOP)");
            text = Regex.Replace(text, @"XIO\((.*?)_FB\.EXT_STOP\b\)", @"XIO(\$1.FB.STOP)");
            progress.Report("Rule No. 3 is applied in OTE(EXT_STOP) conversion");
        }

        // Replace _FB.EXT_RESET with _FB.RST and handle XIC/XIO outer elements, but exclude OTE
        if ((Regex.IsMatch(text, @"XIC\(.*?_FB\.EXT_RESET\b\)") || Regex.IsMatch(text, @"XIO\(.*?_FB\.EXT_RESET\b\)")) &&
            !Regex.IsMatch(text, @"OTE\(.*?_FB\.EXT_RESET\b\)"))
        {
            text = Regex.Replace(text, @"XIC\((.*?)_FB\.EXT_RESET\b\)", @"XIC(\$1.FB.RST)");
            text = Regex.Replace(text, @"XIO\((.*?)_FB\.EXT_RESET\b\)", @"XIO(\$1.FB.RST)");
            progress.Report("Rule No. 3 is applied in OTE(EXT_RESET) conversion");
        }

        // Replace _FP.EXT_START with _FP.START
        if (Regex.IsMatch(text, @"_FP\.EXT_START\b"))
        {
            text = Regex.Replace(text, @"_FP\.EXT_START\b", "_FP.START");
        }

        // Replace _FP.EXT_STOP with _FP.STOP
        if (Regex.IsMatch(text, @"_FP\.EXT_STOP\b"))
        {
            text = Regex.Replace(text, @"_FP\.EXT_STOP\b", "_FP.STOP");
        }

        // Replace _FP.EXT_RESET with _FP.RST
        if (Regex.IsMatch(text, @"_FP\.EXT_RESET\b"))
        {
            text = Regex.Replace(text, @"_FP\.EXT_RESET\b", "_FP.RST");
        }

        // Check and replace _FB.MCOFF with _FB.CON1 and convert XIO --> XIC, XIC --> XIO
        if (Regex.IsMatch(text, @"_FB\.MCOFF\b"))
        {
            text = Regex.Replace(text, @"_FB\.MCOFF\b", "_FB.CON1");

            if (Regex.IsMatch(text, @"\bXIO\b"))
            {
                text = Regex.Replace(text, @"\bXIO\b", "XIC");
            }
            else if (Regex.IsMatch(text, @"\bXIC\b"))
            {
                text = Regex.Replace(text, @"\bXIC\b", "XIO");
            }

            progress.Report("Rule No. 5 is applied in FB.MCOFF conversion");
        }

        // Check and replace _FP.MCOFF with _FP.CON1 and convert XIO --> XIC, XIC --> XIO
        if (Regex.IsMatch(text, @"_FP\.MCOFF\b"))
        {
            text = Regex.Replace(text, @"_FP\.MCOFF\b", "_FP.CON1");

            if (Regex.IsMatch(text, @"\bXIO\b"))
            {
                text = Regex.Replace(text, @"\bXIO\b", "XIC");
            }
            else if (Regex.IsMatch(text, @"\bXIC\b"))
            {
                text = Regex.Replace(text, @"\bXIC\b", "XIO");
            }

            progress.Report("Rule No. 5 is applied in FB.MCOFF conversion");
        }

        // Check and replace _FP.MCOFF with _FP.CON1 and convert XIO --> XIC, XIC --> XIO 
        if (Regex.IsMatch(text, @"_FP\.MCOFF\b"))
        {
            text = Regex.Replace(text, @"_FP\.MCOFF\b", "_FP.CON1");

            if (Regex.IsMatch(text, @"\bXIO\b"))
            {
                text = Regex.Replace(text, @"\bXIO\b", "XIC");
            }

            else if (Regex.IsMatch(text, @"\bXIC\b"))
            {
                text = Regex.Replace(text, @"\bXIC\b", "XIO");
            }

            progress.Report("Rule No. 5 is applied in FP.MCOFF conversion");
        }

        // New condition: Replace XXX_FB.AEN or XXX_FB.AL_EN with AEN
        if (Regex.IsMatch(text, @"_FB\.AEN\b") || Regex.IsMatch(text, @"_FB\.AL_EN\b"))
        { 
            text = Regex.Replace(text, @"\b(XIC|XIO|OTE)\(([^)]+)_FB\.AL_EN\)", "$1(AEN))");
            text = Regex.Replace(text, @"\b(XIC|XIO|OTE)\(([^)]+)_FB\.AEN\)", "$1(AEN)");

            progress.Report("Rule No. 11 is applied in AEB/AL_EN conversion");
        }

        if (datatype == "Valve")
        {
            if ((Regex.IsMatch(text, @"XIC\(.*?_FB\.DIR\b\)") || Regex.IsMatch(text, @"XIO\(.*?_FB\.DIR\b\)")) &&
           !Regex.IsMatch(text, @"OTE\(.*?_FB\.DIR\b\)"))
            {
                text = Regex.Replace(text, @"\b(XIC|XIO)\((.*?)_FB\.DIR\b\)", "$1($2_FB.PREQ1)");
                progress.Report("Rule No. 12 is applied to DIR to PREQ1 and PREQ2 conversion");
            }
        }      


        return text;
    }

    // Method to convert rung text using database mappings
    private IEnumerable<string> ConvertRungText(string oldText, DbHelper dbHelper, RockwellUpgradeOptions options, string dataType, L5XRoutine routine, int NewProgramIndex, string Routinename, IProgress<string> progress, L5XProgram program, L5XTags tags)
    {
        
        // Define a regex pattern to capture the operator
        string pattern = @"^(\w+)\s*\((.*)\);";        

        // Use regex to match the pattern
        Match match = Regex.Match(oldText, pattern);        

        if (match.Success)
        {
            // Extract the operator
            string oldOperator = match.Groups[1].Value;

            // Get the conversion mapping for the operator from the database
            string newOperator = dbHelper.GetOperatorConversionMapping(oldOperator, options);

            // Conversion for Acesys Type Operand and Operator
            if (!string.IsNullOrEmpty(newOperator))
            {
                // Fetch operands from the database based on the new operator
                string operands = dbHelper.GetOperandsForOperator(newOperator, oldText, dataType,options, Routinename, null, null,"", HMI_INTERLOCK_MAX_GROUP);

                // Replace the old operator with the new operator and the old operands with the fetched operands
                string newText = $"{newOperator}({operands});";

                if (oldOperator == "AsysRout")
                {
                    HMI_INTERLOCK_MAX_GROUP = HMI_INTERLOCK_MAX_GROUP + 1;

                    string patternAsysRout = @"AsysRout\(([^,]+),([^,]+),([^,]+),(HMI_ROUTE\[\d+\]),([^,]+),([^,]+),([^,]+)\);";
                    Match match2 = Regex.Match(oldText, patternAsysRout);

                    if (match2.Success)
                    {
                        string hmiRoute = match2.Groups[4].Value;

                        string patternAsysGroup = @"AsysGroup\(([^,]+),([^,]+),([^,]+),([^,]+),([^,]+),([^,]+),(HMI_GROUP\[\d+\]),([^,]+),([^,]+)\);";

                        Match match3 = Regex.Match(newText, patternAsysGroup);

                        if (match3.Success)
                        {
                            string hmiGroup = match3.Groups[7].Value; // Get HMI_GROUP valu

                            hmiRouteToGroup[hmiRoute] = hmiGroup;
                        }
                    }
                }

                if (newText.Contains("IUSYS_3UF70_MOTOR", StringComparison.OrdinalIgnoreCase))
                {
                    string pattern1 = @"\b\w*_FP\.\w*\b";
                    var match1 = Regex.Match(newText, pattern1);

                    if (match1.Success)
                    {
                        string extractedOperand = match1.Value;

                        int indexOfFp = extractedOperand.IndexOf("_FP");
                        if (indexOfFp >= 0)
                        {
                            string truncatedOperand = extractedOperand.Substring(0, indexOfFp + 3); // +3 to include "_FP"

                            L5XTags OriginalTags = (L5XTags?)Project.Content?.Controller?.Tags;

                            if (OriginalTags != null)
                            {
                                XmlNodeList originalTags = OriginalTags.SelectNodes("//Tag");

                                if (OriginalTags != null)
                                {
                                    foreach (XmlElement item in originalTags)
                                    {
                                        if (item.Attributes["Name"]?.Value == truncatedOperand)
                                        {
                                            // Call the method to extract the node based on the nameAttribute
                                            XmlNode? unimotorNode = ExtractDataValueMemberNode(item, "Sel_Type_Unimotor");
                                            XmlNode? bimotorNode = ExtractDataValueMemberNode(item, "Sel_Type_Bimotor");
                                            string unimotorValue = string.Empty;
                                            string bimotorValue = string.Empty;
                                            string v8PointType = string.Empty;

                                            if (unimotorNode != null && unimotorNode.Attributes["Value"] != null)
                                            {
                                                unimotorValue = unimotorNode.Attributes["Value"].Value;                                                
                                            }                                            

                                            if (bimotorNode != null && bimotorNode.Attributes["Value"] != null)
                                            {
                                                bimotorValue = bimotorNode.Attributes["Value"].Value;                                                
                                            }

                                            if (unimotorValue == "1" && bimotorValue == "0")
                                            {
                                                v8PointType = "PointTypeAcesysUnitUnimotor8.0";
                                                PointTypeIusys.Add(v8PointType);
                                            }

                                            if (unimotorValue == "0" && bimotorValue == "1")
                                            {
                                                v8PointType = "PointTypeAcesysUnitBimotor8.0";
                                                PointTypeIusys.Add(v8PointType);
                                            }

                                            if (unimotorValue == "1" && bimotorValue == "1")
                                            {
                                                v8PointType = "PointTypeAcesysUnitUnimotor8.0";
                                                PointTypeIusys.Add(v8PointType);
                                            }
                                            
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (newText.Contains("IUSYS_3UF70_GATE", StringComparison.OrdinalIgnoreCase))
                {
                    string pattern1 = @"\b\w*_FP\.\w*\b";
                    var match1 = Regex.Match(newText, pattern1);

                    if (match1.Success)
                    {
                        string extractedOperand = match1.Value;

                        int indexOfFp = extractedOperand.IndexOf("_FP");
                        if (indexOfFp >= 0)
                        {
                            string truncatedOperand = extractedOperand.Substring(0, indexOfFp + 3); // +3 to include "_FP"

                            L5XTags OriginalTags = (L5XTags?)Project.Content?.Controller?.Tags;

                            if (OriginalTags != null)
                            {
                                XmlNodeList originalTags = OriginalTags.SelectNodes("//Tag");

                                if (OriginalTags != null)
                                {
                                    foreach (XmlElement item in originalTags)
                                    {
                                        if (item.Attributes["Name"]?.Value == truncatedOperand)
                                        {
                                            // Call the method to extract the node based on the nameAttribute
                                            XmlNode? motorgateNode = ExtractDataValueMemberNode(item, "Sel_Type_Motorgate");
                                            XmlNode? positionerNode = ExtractDataValueMemberNode(item, "Sel_Type_Positioner");
                                            string motorgateNodeValue = string.Empty;
                                            string positionerNodeValue = string.Empty;
                                            string v8PointType = string.Empty;

                                            if (motorgateNode != null && motorgateNode.Attributes["Value"] != null)
                                            {
                                                motorgateNodeValue = motorgateNode.Attributes["Value"].Value;
                                            }

                                            if (positionerNode != null && positionerNode.Attributes["Value"] != null)
                                            {
                                                positionerNodeValue = positionerNode.Attributes["Value"].Value;
                                            }

                                            if (motorgateNodeValue == "1" && positionerNodeValue == "0")
                                            {
                                                v8PointType = "PointTypeAcesysUnitMotorgate8.0";
                                                PointTypeIusys.Add(v8PointType);
                                            }

                                            if (motorgateNodeValue == "0" && positionerNodeValue == "1")
                                            {
                                                v8PointType = "PointTypeAcesysUnitPositioner8.0";
                                                PointTypeIusys.Add(v8PointType);
                                            }

                                            if (motorgateNodeValue == "1" && positionerNodeValue == "1")
                                            {
                                                v8PointType = "PointTypeAcesysUnitMotorgate8.0";
                                                PointTypeIusys.Add(v8PointType);
                                            }

                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                progress.Report($"New Asys Operator'{newOperator}' is called.");

                // Yield the new text as the only modification needed
                yield return newText;
            }
            else
            {
                List<string> fpDataType = GetDataTypeForFpElements(oldText);
                string MaxHMIInterlock = $"HMI_INTERLOCK[{HMI_INTERLOCK_MAX_INDEX}]";
                oldText = dbHelper.GetElementAndSubElementReplacement(oldText, fpDataType, options);
                List<string> InputOperandForInterlock = GetInputOperandForInterlock(oldText,routine,dbHelper, fpDataType,options, dataType, progress);
                List<string> HMI_Interlock_Value = GetHMIInterlock(oldText);
                string Asyspattern = options.IsExtendedSelect ? @"AsysExtInterlock" : @"AsysInterlock";
                oldText = dbHelper.GetElementConversionFB(oldText,dataType,progress);
                oldText = dbHelper.GetElementConversionADPT(oldText, dataType, progress);
                // Get Operands Conversion for Non Acesys Operator
                if (!string.IsNullOrEmpty(oldText))
                {
                    List<string> conversionText = dbHelper.GetOperandsConversionForNonAseysOperator(options, oldText, dataType, HMI_Interlock_Value, MaxHMIInterlock, InputOperandForInterlock, New_Program_Index, progress);

                    if (conversionText != null)
                    {
                        conversionText = conversionText.Distinct().ToList();
                    }                    

                    if (conversionText != null)
                    {
                        // Iterate over the conversion texts and yield the new text for each
                        foreach (var newText in conversionText)
                        {
                            string afterfpConversion = newText;
                            List<string> newfpDataType = GetDataTypeForFpElements(afterfpConversion);
                            afterfpConversion = dbHelper.GetElementAndSubElementReplacement(afterfpConversion, newfpDataType, options);

                            // Assume the new text is a full replacement or part modification
                            yield return afterfpConversion;
                        }
                    }
                } 
            }
        }
        else
        {
            List<string> fpDataType = GetDataTypeForFpElements(oldText);
            string MaxHMIInterlock = $"HMI_INTERLOCK[{HMI_INTERLOCK_MAX_INDEX}]";
            oldText = dbHelper.GetElementAndSubElementReplacement(oldText, fpDataType, options);
            List<string> InputOperandForInterlock = GetInputOperandForInterlock(oldText, routine, dbHelper, fpDataType, options, dataType, progress);
            List<string> HMI_Interlock_Value = GetHMIInterlock(oldText);
            string Asyspattern = options.IsExtendedSelect ? @"AsysExtInterlock" : @"AsysInterlock";
            oldText = dbHelper.GetElementConversionFB(oldText, dataType, progress);
            // Get Operands Conversion for Non Acesys Operator
            if (!string.IsNullOrEmpty(oldText))
            {
                List<string> conversionText = dbHelper.GetOperandsConversionForNonAseysOperator(options, oldText, dataType, HMI_Interlock_Value, MaxHMIInterlock, InputOperandForInterlock, New_Program_Index, progress);

                if (conversionText != null)
                {
                    // Iterate over the conversion texts and yield the new text for each
                    foreach (var newText in conversionText)
                    {
                        string afterfpConversion = newText;
                        List<string> newfpDataType = GetDataTypeForFpElements(afterfpConversion);
                        afterfpConversion = dbHelper.GetElementAndSubElementReplacement(afterfpConversion, newfpDataType, options);

                        // Assume the new text is a full replacement or part modification
                        yield return afterfpConversion;
                    }
                }               
            }
            
        }
    }

    private XmlNode? ExtractDataValueMemberNode(XmlElement item, string nameAttribute)
    {
        // Try to find the DataValueMember node in Structure or StructureMember
        XmlNode? node = item.SelectSingleNode($"Data[@Format='Decorated']/Structure/DataValueMember[@Name='{nameAttribute}']");
        node ??= item.SelectSingleNode($"Data[@Format='Decorated']/StructureMember/DataValueMember[@Name='{nameAttribute}']");

        return node;
    }

    private List<string> GetDataTypeForFpElements(string oldText)
    {
        // Define a regex pattern to match the content inside parentheses
        string pattern = @"\(([^)]+)\)";

        List<string> resultList = new List<string>();

        // Find all matches of the pattern in the oldText
        foreach (Match match in Regex.Matches(oldText, pattern))
        {
            // Call ReplaceMatch method and store the result
            string result = ReplaceMatch(match);

            // If result is not null, add it to the resultList
            if (result != null)
            {
                resultList.Add(result);
            }
        }

        return resultList;
    }

    private string ReplaceMatch(Match match)
    {
        // Extract the matched text, which is inside the parentheses
        string matchedText = match.Groups[1].Value;

        // Define a regex pattern to find '_FP.' followed by the desired element
        string subPattern = @"(_FP\.(\w+))";

        // Check if the matched text contains '_FP.'
        var regex = new Regex(subPattern);
        var subMatch = regex.Match(matchedText);

        if (subMatch.Success)
        {
            // Extract the full match and the captured element
            string fullMatch = subMatch.Groups[1].Value;
            string element = subMatch.Groups[2].Value;
            string pattern = Regex.Escape(element);
            string extractedstring = Regex.Replace(matchedText, pattern, string.Empty).Trim('.');
            

            L5XTags OriginalTags = (L5XTags?)Project.Content?.Controller?.Tags;

            if (OriginalTags != null)
            {
                XmlNodeList originalTags = OriginalTags.SelectNodes("//Tag");

                if (OriginalTags != null)
                {
                    foreach (XmlElement item in OriginalTags)
                    {
                        if (item.Attributes["Name"]?.Value == extractedstring)
                        {
                            string dataType = item.Attributes["DataType"]?.Value;
                            if (dataType != null)
                            {
                                return dataType;
                            }
                        }
                    }
                }
            }
            
        }

       return null;
    }

    public List<string> GetInputOperandForInterlock(string oldText, L5XRoutine routine, DbHelper dbHelper, List<string> fpDataType, RockwellUpgradeOptions options, string datatype, IProgress<string> progress)
    {
        List<string> fPDataType = GetDataTypeForFpElements(oldText);       

        // Initialize the list of input operands and OTE matches
        List<string> inputOperands = new List<string>();
        List<string> oteMatches = new List<string>();       


        // Regex to find XIC instructions
        Regex xicRegex = new Regex(@"XIC\(([^)]+)\)");

        if (oldText.Contains("FBINT"))
        {
            // Find all XIC matches in the old text
            foreach (Match match in xicRegex.Matches(oldText))
            {
                string content = match.Groups[1].Value;
                oteMatches.Add($"OTE({content});"); // Modify as needed
            }

            if (OriginalPrograms != null)
            {
                // Regex to find CDATA sections
                Regex cdataRegex = new Regex(@"<!\[CDATA\[(.*?)\]\]>", RegexOptions.Singleline);

                // Regex to find OTE instructions
                Regex oteRegex = new Regex(@"OTE\(([^)]+)\);");

                foreach (L5XProgram originalProgram in OriginalPrograms)
                {
                    foreach (L5XRoutine newRoutine in originalProgram.Routines)
                    {
                        if (newRoutine == null) continue;

                        foreach (XmlElement item in newRoutine)
                        {
                            string itemXml = item.InnerXml;

                            // Find all CDATA sections
                            foreach (Match cdataMatch in cdataRegex.Matches(itemXml))
                            {
                                string cdataContent = cdataMatch.Groups[1].Value;

                                // Find all OTE instructions
                                foreach (Match oteMatch in oteRegex.Matches(cdataContent))
                                {
                                    string oteContent = oteMatch.Value;

                                    // Check if the OTE instruction matches any of the OTE matches from XIC
                                    if (oteMatches.Contains(oteContent))
                                    {
                                        // Add the content before the OTE instruction to the input operands list
                                        string beforeOteContent = cdataContent.Substring(0, oteMatch.Index).Trim();
                                        beforeOteContent = dbHelper.GetElementAndSubElementReplacement(beforeOteContent, fpDataType, options);

                                        // Check if beforeOteContent contains XIC/XIO with FBINT pattern
                                        Regex xicXioFbintRegex = new Regex(@"XIC\([^)]+_FBINT\d+\)|XIO\([^)]+_FBINT\d+\)");
                                        if (xicXioFbintRegex.IsMatch(beforeOteContent))
                                        {
                                            beforeOteContent = GetGroupInputOperand(beforeOteContent, fpDataType, options, dbHelper);
                                            beforeOteContent = dbHelper.GetElementConversionFB(beforeOteContent, datatype, progress);
                                            beforeOteContent = dbHelper.GetElementAndSubElementReplacement(beforeOteContent, fPDataType, options);
                                            inputOperands.Add(beforeOteContent);
                                        }
                                        else
                                        {
                                            beforeOteContent = dbHelper.GetElementConversionFB(beforeOteContent, datatype, progress);
                                            beforeOteContent = dbHelper.GetElementAndSubElementReplacement(beforeOteContent, fPDataType, options);
                                            inputOperands.Add(beforeOteContent);
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
            }
            
        }

        else
        {
            string pattern = @"^(.*?)(?=OTE)";

            Match match = Regex.Match(oldText, pattern);

            if (match.Success)
            {
                string beforeOTEContent = match.Groups[1].Value;
                if (!string.IsNullOrEmpty(beforeOTEContent))
                {
                    List<string> inputOperands1 = ExtractClusters(beforeOTEContent);
                    foreach (string inputOperand in inputOperands1) 
                    {
                        inputOperands.Add(inputOperand);
                    }
                    
                }                
            }
        }

        return inputOperands;
    }

    private List<string> ExtractClusters(string input)
    {
        List<string> clusters = new List<string>();

        // Pattern to match XIC(...), XIO(...), MOV(...), etc.
        string pattern = @"\b\w+\([^)]*\)|\[.*?\]";

        // Use Regex to find matches
        MatchCollection matches = Regex.Matches(input, pattern);

        foreach (Match match in matches)
        {
            clusters.Add(match.Value);
        }

        return clusters;
    }

    private string GetGroupInputOperand(string beforeOteContent, List<string> fpDataType, RockwellUpgradeOptions options, DbHelper dbHelper)
    {
        List<string> oteGrpMatches = new List<string>();
        // Regex to match XIC or XIO instructions and capture the content inside parentheses
        Regex xicXioRegex = new Regex(@"(XIC|XIO)\(([^)]+)\)");
        Match match = xicXioRegex.Match(beforeOteContent);

        if (match.Success)
        {
            string content = match.Groups[2].Value;
            string OTEContent = $"OTE({content});";
            oteGrpMatches.Add(OTEContent);

            if (OriginalPrograms != null)
            {
                // Regex to find CDATA sections
                Regex cdata_Regex = new Regex(@"<!\[CDATA\[(.*?)\]\]>", RegexOptions.Singleline);

                // Regex to find OTE instructions
                Regex ote_Regex = new Regex(@"OTE\(([^)]+)\);");

                foreach (L5XProgram originalProgram in OriginalPrograms)
                {
                    foreach (L5XRoutine newRoutine in originalProgram.Routines)
                    {
                        if (newRoutine == null) continue;

                        foreach (XmlElement item in newRoutine)
                        {
                            string itemXml = item.InnerXml;

                            foreach (Match cdata_Match in cdata_Regex.Matches(itemXml))
                            {
                                string cdataContent = cdata_Match.Groups[1].Value;

                                foreach (Match oteMatch in ote_Regex.Matches(cdataContent))
                                {
                                    string oteContent = oteMatch.Value;

                                    if (oteGrpMatches.Contains(oteContent))
                                    {
                                        beforeOteContent = cdataContent.Substring(0, oteMatch.Index).Trim();
                                        beforeOteContent = dbHelper.GetElementAndSubElementReplacement(beforeOteContent, fpDataType, options);

                                        return beforeOteContent;
                                    }

                                }
                            }
                        }
                    }
                }

            }
        }

        return beforeOteContent;
    }    

    private int GetMaxIndexHMIInterlock()
    {
        L5XTags OriginalTags = (L5XTags?)Project.Content?.Controller?.Tags;
        int maxIndex = -1; // Initialize maxIndex to -1

        if (OriginalTags != null)
        {
            string pattern = @"HMI_INTERLOCK\[(\d+)\]";

            foreach (XmlElement item in OriginalTags)
            {
                var tagAliasForAttribute = item.GetAttribute("AliasFor"); // Get the AliasFor attribute value of the tag

                if (!string.IsNullOrEmpty(tagAliasForAttribute))
                {
                    Match match = Regex.Match(tagAliasForAttribute, pattern); // Match the pattern with the tagAliasFor

                    if (match.Success)
                    {
                        int wordIndex = int.Parse(match.Groups[1].Value); // Get the word index
                        //int bitIndex = int.Parse(match.Groups[2].Value); // Get the bit index
                        int index = wordIndex; // Calculate the overall index

                        maxIndex = Math.Max(maxIndex, index); // Update maxIndex if necessary
                    }
                }
            }
        }

        return maxIndex + 1;
    }

    private int GetMaxIndexHMIGroup()
    {
        string textContent = "";
        int MaxIndex = -1; // Initialize to -1 to indicate no HMI_UNIT found
        if (Project.Content?.Controller?.Programs != null)
        {
            foreach (L5XProgram program in Project.Content.Controller.Programs)
            {
                foreach (var routine in program.Routines)
                {
                    foreach (XmlNode rung in routine.SelectNodes("RLLContent/Rung"))
                    {
                        XmlNode? textNode = rung.SelectSingleNode("Text");
                        if (textNode != null)
                        {
                            textContent = textNode.InnerText;
                            int currentMaxIndex = CalculateMaxHMIGroupIndex(textContent);
                            if (currentMaxIndex > MaxIndex)
                            {
                                MaxIndex = currentMaxIndex;
                            }
                        }
                    }
                }
            }
        }
        return MaxIndex + 1;
    }

    private List<string> GetHMIInterlock(string oldText)
    {
        List<string> hmiInterlockList = new List<string>();

        // Create a regex to match XIC elements
        Regex xicRegex = new Regex(@"XIC\(([^)]+)\)");
        MatchCollection xicMatches = xicRegex.Matches(oldText);

        if (xicMatches.Count == 0)
        {
            return null;
        }

        // Create a dictionary to store the replacements
        Dictionary<string, string> replacements = new Dictionary<string, string>();

        L5XTags OriginalTags = (L5XTags?)Project.Content?.Controller?.Tags;

        if (OriginalTags != null)
        {
            foreach (XmlElement item in OriginalTags)
            {
                foreach (Match xicMatch in xicMatches)
                {
                    string xicContent = xicMatch.Groups[1].Value;

                    if (item.Attributes["Name"] != null && item.Attributes["Name"].Value == xicContent)
                    {
                        if (item.Attributes["AliasFor"] != null)
                        {
                            var aliasFor = item.Attributes["AliasFor"].Value;
                            // Determine the HMI interlock index for the XIC content
                            string hmiInterlock = aliasFor;

                            // Store the replacement in the dictionary
                            replacements[xicContent] = hmiInterlock;
                        }
                    }
                }
            }

            if (replacements.Count == 0)
            {
                return null;
            }

            StringBuilder newTextBuilder = new StringBuilder(oldText);

            // Replace each XIC content with the corresponding HMI interlock index
            foreach (var replacement in replacements)
            {
                newTextBuilder.Replace($"XIC({replacement.Key})", replacement.Value);
            }

            string newText = newTextBuilder.ToString();

            // Extract HMI interlock elements
            Regex hmiInterlockRegex = new Regex(@"HMI_INTERLOCK\[\d+\]");
            MatchCollection hmiInterlockMatches = hmiInterlockRegex.Matches(newText);
            foreach (Match match in hmiInterlockMatches)
            {
                hmiInterlockList.Add(match.Value);
            }

            return hmiInterlockList;
        }

        return null;
    }


    private string ExtractControlIndexFromRoutine(L5XRoutine routine, string dataType, DbHelper dbHelper)
    {
        string routineText = routine.InnerXml; // Assuming the routine is stored as inner XML string

        // Handle CDATA section
        if (routine.FirstChild is XmlCDataSection cdataSection)
        {
            routineText = cdataSection.InnerText;
        }

        if (dataType == "Analog")
        {
            // Extract the control index from the AsysAI function call in the routine
            string pattern = @"AsysAI\([^,]+,HMI_ANALOG\[(\d+)\],";
            Match match = Regex.Match(routineText, pattern);
            if (match.Success)
            {
                int controlIndex = int.Parse(match.Groups[1].Value);
                return MapAnalogControlIndex(controlIndex, dbHelper, dataType);
            }
           
        }
        else if (dataType == "PID")
        {
            // Extract the control index from the AsysPID function call in the routine (7th operand)
            string pattern = @"AsysPID\([^,]+,[^,]+,[^,]+,[^,]+,[^,]+,[^,]+,HMI_PID\[(\d+)\],";
            Match match = Regex.Match(routineText, pattern);
            if (match.Success)
            {
                int controlIndex = int.Parse(match.Groups[1].Value);
                return MapPIDControlIndex(controlIndex,dbHelper,dataType);
            }
            
        }
        else if (dataType == "HLC")
        {
            // For HLC, control index is always "1"
            return "1";
        }

        return null;
    }

    private string MapAnalogControlIndex(int controlIndex,DbHelper dbHelper, string dataType)
    {
        string key = "ControlIndex" + dataType;
        string configData = dbHelper.GetConfigData(key);

        // Split the config data into an array
        string[] configValues = configData.Split(',');


        // Iterate through the config values and find the appropriate range
        for (int i = 1; i < configValues.Length; i += 4)
        {
            int rangeStart = int.Parse(configValues[i]);
            int rangeEnd = int.Parse(configValues[i + 1]);
            string mappedValue = configValues[i + 2];

            if (controlIndex >= rangeStart && controlIndex <= rangeEnd)
            {
                return mappedValue;
            }
        }

        throw new ArgumentOutOfRangeException("Control index is out of the expected range.");
    }

    private string MapPIDControlIndex(int controlIndex, DbHelper dbHelper, string dataType)
    {
        string key = "ControlIndex" + dataType;
        string configData = dbHelper.GetConfigData(key);

        // Split the config data into an array
        string[] configValues = configData.Split(',');              

        // Iterate through the config values and find the appropriate range
        for (int i = 1; i < configValues.Length; i += 3)
        {
            int rangeStart = int.Parse(configValues[i]);
            int rangeEnd = int.Parse(configValues[i + 1]);
            string mappedValue = configValues[i + 2];

            if (controlIndex >= rangeStart && controlIndex <= rangeEnd)
            {
                return mappedValue;
            }
        }

        throw new ArgumentOutOfRangeException("Control index is out of the expected range.");
    }


    private void UpdateRungNumbers(XmlNode rllContent)
    {
        int rungNumber = 0; // Start from 0 for the first rung
        foreach (XmlNode node in rllContent.ChildNodes)
        {
            if (node.Name == "Rung")
            {
                XmlAttribute? numberAttribute = node.Attributes["Number"];
                if (numberAttribute == null)
                {
                    numberAttribute = rllContent.OwnerDocument.CreateAttribute("Number");
                    node.Attributes.Append(numberAttribute);
                }
                numberAttribute.Value = rungNumber.ToString();

                // If Type attribute doesn't exist, create it and append it after Value attribute
                XmlAttribute? typeAttribute = node.Attributes["Type"];
                if (typeAttribute == null)
                {
                    typeAttribute = rllContent.OwnerDocument.CreateAttribute("Type");
                    typeAttribute.Value = "N";
                    node.Attributes.Append(typeAttribute);
                }
                else
                {
                    // Remove the existing Type attribute and re-add it after Value attribute
                    node.Attributes.Remove(typeAttribute);
                    node.Attributes.Append(typeAttribute);
                }
                rungNumber++;
            }
        }
    }

    private void RemoveDuplicateRungWithNumberZero(XmlNode rllContent)
    {
        XmlNode? duplicateRung = rllContent.SelectSingleNode("Rung[@Number='0']");
        if (duplicateRung != null)
        {
            rllContent.RemoveChild(duplicateRung);
        }
    }

    private static void CreateRequiredAttributes(string dataType, L5XRoutine routine, L5XProgram program)
    {
        CreateAttributeWithValue(program, "Name", routine.RoutineName);
        CreateAttributeWithValue(program, "TestEdits", "false");
        CreateAttributeWithValue(program, "MainRoutineName", dataType);
        CreateAttributeWithValue(program, "Disabled", "false");
        CreateAttributeWithValue(program, "UseAsFolder", "false");
    }

    private static void CreateAttributeWithValue(L5XProgram program, string attributeName, string attributeValue)
    {
        program.Attributes.Append(program.OwnerDocument.CreateAttribute(attributeName));
        program.Attributes[attributeName]!.Value = attributeValue;
    }

    private static string GetMainRoutineName(List<Dto> routineNameMap, string tagDataType)
    {
        string? mainRoutineName = routineNameMap.Where(x => x.FromObject == tagDataType).FirstOrDefault()?.ToObject;
        if (string.IsNullOrEmpty(mainRoutineName))
        {
            mainRoutineName = "Main";
        }
        return mainRoutineName;
    }

    private void ProcessOne2ManyPrograms(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress, int NewProgramIndex)
    {
        List<Dto> one2many = dbHelper.GetOneToManyMainRoutines(options);
        var programsToModify = Project.Content?.Controller?.Programs;
        string oldRoutineName = string.Empty;

        if (programsToModify != null)
        {
            programsToModify.Clear();

            if (OriginalPrograms != null)
            {
                foreach (L5XProgram originalProgram in OriginalPrograms)
                {
                    foreach (L5XRoutine routine in originalProgram.Routines)
                    {
                        progress.Report($"Processing Program {originalProgram.ProgramName}. Converting Routine {routine.RoutineName} to Program");

                        if (routine.RoutineName.Equals("Dispatcher"))
                        {
                            continue;
                        }

                        string Routinename = routine.RoutineName;

                        L5XTag? associatedTag = originalProgram.GetAssociatedTagForRoutine(routine.RoutineName);
                        string dataType = associatedTag?.DataType ?? string.Empty;

                        bool dataTypeExists = one2many.Any(dto => dto.FromObject == dataType);

                        if (dataTypeExists) 
                        {
                            foreach (var dt in one2many)
                            {
                                dataType = dt.ToObject;
                                L5XProgram newProgram = CreateProgramFromRoutine(dataType, routine, programsToModify, dbHelper, options, New_Program_Index, Routinename, xmlDoc, progress);

                                _ = (Project.Content?.Controller?.Programs?.AppendChild(newProgram));                               


                                if (associatedTag == null)
                                {
                                    L5XCollection.AddUserMessage(Project, newProgram, routine, UserMessageTypes.Information,
                                         $"Routine {oldRoutineName} Not Associated With Tag Naming to Main", "O2M");
                                }

                            }                            

                        }                        

                    }
                }
            }
        }
    }


    public override void ProcessMany2One(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {

    }

    public override void ProcessRemoval(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {

    }

    public override void ProcessMandatory(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        Dictionary<string, string> mandatory = dbHelper.GetMandatoryPrograms(options);

        foreach (KeyValuePair<string, string> m in mandatory)
        {
            _ = Project.Content?.Controller?.Programs?.Remove(m.Key);
            _ = Project.Content?.Controller?.Programs?.Add(m.Key, m.Value, "MAN", null);
        }

        MethodToExtractPLCtoPLC(dbHelper, options, progress);
        MethodToExtractCPUSlotStatusHMIIndexControllerName(progress);
        MethodToExtractNetworkNode(Project, "DiagNet", progress);

        if (NetworkModules.Count > 0)
        {
            MethodToExtractNodeModule(Project, "DiagNode", progress);

            if (NodeModules.Count > 0)
            {
                MethodToExtractAnalogInputOutputModule(Project, "DiagAnalog", progress);
                MethodToExtractDigitalInputOutputModule(Project, "DiagDigital", progress);
            }
        }

        MethodToGiveWarningOnManipulatedGroupCmd();

        List<string> ECSFilePaths = options.ECSFilePaths;
        var fileContentMap = new Dictionary<string, string>();

        // Get the path to the App.Data folder
        string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AcesysConversion");

        // Ensure the App.Data folder exists
        if (!Directory.Exists(appDataFolder))
        {
            Directory.CreateDirectory(appDataFolder);
        }

        // Define the expected file names
        var expectedFiles = new List<string> { "Pnt.Config.xml", "Core.Ref.xml", "CLX.Config.xml" };

        // Check if each expected file exists in the App.Data folder, and copy it if it doesn't
        foreach (var expectedFile in expectedFiles)
        {
            string destinationPath = Path.Combine(appDataFolder, expectedFile);

            if (!File.Exists(destinationPath))
            {
                // Find the source file in the provided ECSFilePaths
                var sourceFilePath = ECSFilePaths.FirstOrDefault(path => GetNewFileName(path) == expectedFile);
                if (sourceFilePath != null)
                {
                    File.Copy(sourceFilePath, destinationPath);
                }
            }
        }

        // Process and update the files
        foreach (var filePath in ECSFilePaths)
        {
            string newFileName = GetNewFileName(filePath);
            if (string.IsNullOrEmpty(newFileName))
            {
                return;
            }

            string content = File.ReadAllText(filePath);

            // Remove namespaces from the content using the provided method
            content = RemoveAllNamespaces(content);

            if (newFileName == "Pnt.Config.xml")
            {
                var pointTypeIdsAndUnits = ExtractPointTypeIdsAndUnits(content);
                var updatedContent = ProcessPointTypeIdsAndReplace(content, pointTypeIdsAndUnits, dbHelper);

                // Update Pnt.Config.xml with diagnostic modules
                updatedContent = UpdatePntConfigDiagXmlContent(
                    updatedContent,
                    ControllerNameGlobal,
                    NetworkModules,
                    NodeModules,
                    AnalogInputModules.Select(module => module.ModuleName).ToList(),
                    AnalogOutputModules.Select(module => module.ModuleName).ToList(),
                    DigitalInputModules.Select(module => module.ModuleName).ToList(),
                    DigitalOutputModules.Select(module => module.ModuleName).ToList()
                );

                // Update EntityReceived section in Pnt.Config.xml
                updatedContent = UpdatePntConfigEntityReceivedXmlContent(
                    updatedContent,
                    ReceiveTagGlobal
                );

                // Integrate the method to add new Interlock Entities
                updatedContent = UpdatePntConfigXMLWithNewInterlockEntities(
                    updatedContent, newInterlockParameters, dbHelper);

                updatedContent = UpdatePntIusysXmlContent(updatedContent, dbHelper, PointTypeIusys);

                // Save the updated content
                PntConfigXml = updatedContent;
                fileContentMap[newFileName] = PntConfigXml;
            }
            else if (newFileName == "Core.Ref.xml")
            {
                content = UpdateCoreRefXmlContent(
                    content,
                    ControllerNameGlobal,
                    NetworkModules,
                    NodeModules,
                    AnalogInputModules.Select(module => module.ModuleName).ToList(),
                    AnalogOutputModules.Select(module => module.ModuleName).ToList(),
                    DigitalInputModules.Select(module => module.ModuleName).ToList(),
                    DigitalOutputModules.Select(module => module.ModuleName).ToList()
                );

                content = UpdateCoreRefEntityRelationXmlContent(
                    content,
                    ReceiveTagGlobal
                );

                content = UpdateCoreRefEntityRelationCPUXmlContent(
                    content,
                    ControllerNameGlobal,
                    NetworkModules,
                    NodeModules,
                    AnalogInputModules.Select(module => module.ModuleName).ToList(),
                    AnalogOutputModules.Select(module => module.ModuleName).ToList(),
                    DigitalInputModules.Select(module => module.ModuleName).ToList(),
                    DigitalOutputModules.Select(module => module.ModuleName).ToList()
                );

                content = UpdateCoreRefLanguageXmlContent(
                    content,
                    ControllerNameGlobal,
                    NetworkModules,
                    NodeModules,
                    AnalogInputModules,
                    AnalogOutputModules,
                    DigitalInputModules,
                    DigitalOutputModules
                );

                // Add new Entity tags for InterlockNewTagsHMI
                content = UpdateCoreRefNewInterlockEntity(
                    content,
                    InterlockNewTagsHMI
                );
                

                // Add new Language tags for ReceiveTagGlobal
                content = AddLanguageTagsForReceiveTagGlobal(content, ReceiveTagGlobal);

                // Update CoreRef XML with new Language tags based on Interlock Tags
                content = UpdateCoreRefXMLNewInterlockLang(
                    content,
                    newInterlockTags
                );

                content = UpdateCoreRefXMLNewInterlockEntityRelation(
                    content,
                    newInterlockTags
                );
                

                CoreRefXml = content;
                fileContentMap[newFileName] = content;
            }
            else if (newFileName == "CLX.Config.xml")
            {
                content = UpdateInpAddrHmiRouteToGroup(content, hmiRouteToGroup);

                content = UpdateClxConfigPointsXmlContent(
                    content,
                    ControllerNameGlobal,
                    NetworkModules,
                    NodeModules,
                    AnalogInputModules.Select(module => module.ModuleName).ToList(),
                    AnalogOutputModules.Select(module => module.ModuleName).ToList(),
                    DigitalInputModules.Select(module => module.ModuleName).ToList(),
                    DigitalOutputModules.Select(module => module.ModuleName).ToList()
                );

                // Update the content with Receive information
                content = UpdatePLCConfigReceiveXmlContent(
                    content,
                    ReceiveTagGlobal,
                    PLC_Rx_HMI_Unit,
                    ControllerNameGlobal
                );

                // Add the Interlock Points for the CLX Config
                content = UpdateCLXNewInterlockPoints(
                    content,
                    newInterlockParameters,
                    ControllerNameGlobal
                );

                CLXConfigXml = content;
                fileContentMap[newFileName] = content;
            }
        }

        // Save all updated XML files to the App.Data location
        foreach (var entry in fileContentMap)
        {
            string destinationPath = Path.Combine(appDataFolder, entry.Key);
            File.WriteAllText(destinationPath, entry.Value);
        }
    }

    

    private void MethodToGiveWarningOnManipulatedGroupCmd()
    {
        var programsToModify = Project.Content?.Controller?.Programs;

        if (programsToModify != null)
        {
            foreach (L5XProgram originalProgram in programsToModify)
            {
                foreach (L5XRoutine routine in originalProgram.Routines)
                {
                    XmlNodeList rungNodes = routine.SelectNodes("RLLContent/Rung");

                    foreach (XmlNode rungNode in rungNodes)
                    {
                        XmlNode textContentNode = rungNode.SelectSingleNode("Text");
                        if (textContentNode != null)
                        {                            
                            string pattern = @"COP\(\s*([^)]+?),\s*DUMMY_CMD,\s*1\s*\)";
                            Regex regex = new Regex(pattern);
                            
                            if (regex.IsMatch(textContentNode.InnerText))
                            {
                                L5XCollection.AddUserMessage(Project, originalProgram, originalProgram, UserMessageTypes.Warning,
                                   "Manipulated Group Cmd Detected", "",
                               $"Program name {originalProgram.ProgramName} Manipulation Group Cmd Detected");
                            }
                        }
                    }


                }
            }
        }
    }

    private string UpdateInpAddrHmiRouteToGroup(string xmlContent, Dictionary<string, string> hmiRouteToGroup)
    {
        // Load the XML content into an XmlDocument
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlContent);

        // Loop through all the Points nodes
        XmlNodeList pointsNodes = xmlDoc.SelectNodes("//Points");
        foreach (XmlNode pointsNode in pointsNodes)
        {
            XmlNode inpAddrNode = pointsNode.SelectSingleNode("InpAddr");
            if (inpAddrNode != null)
            {
                string inpAddrValue = inpAddrNode.InnerText;

                // Check if the current InpAddr matches any key in the dictionary
                if (hmiRouteToGroup.ContainsKey(inpAddrValue))
                {
                    // Replace HMI_ROUTE with HMI_GROUP based on the mapping
                    inpAddrNode.InnerText = hmiRouteToGroup[inpAddrValue];
                }
            }
        }

        // Return the updated XML content as a string
        return xmlDoc.OuterXml;
    }

    private string UpdatePntIusysXmlContent(string updatedContent, DbHelper dbHelper, List<string> PointTypeIusys)
    {
        // Parse the updatedContent XML into an XDocument
        var xmlDoc = XDocument.Parse(RemoveAllNamespaces(updatedContent));
        string appendLanguageDataXmlContent = null;
        string appendDataXmlContent = null;

        // Get all "Entity" elements
        var entities = xmlDoc.Descendants("Entity").ToList();

        int iusysIndex = 0;

        // Loop through all "Entity" elements
        foreach (var entity in entities)
        {
            // Find the "PointTypeId" element inside each "Entity"
            var pointTypeIdElement = entity.Element("PointTypeId");

            if (pointTypeIdElement != null)
            {
                string pointTypeId = pointTypeIdElement.Value;

                // Check if the PointTypeId contains "PointTypeConv"
                if (pointTypeId.Contains("PointTypeConv", StringComparison.OrdinalIgnoreCase))
                {
                    // Replace PointTypeId with the corresponding item from PointTypeIusys
                    if (iusysIndex < PointTypeIusys.Count)
                    {
                        pointTypeIdElement.Value = PointTypeIusys[iusysIndex];
                        iusysIndex++; // Move to the next PointTypeIusys item for the next replacement
                    }

                    appendDataXmlContent = dbHelper.GetAppendDataXml(pointTypeIdElement.Value);
                    appendLanguageDataXmlContent = dbHelper.GetLanguageDataXml(pointTypeIdElement.Value);

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

        }

        // Return the modified XML content as a string
        return xmlDoc.Declaration + xmlDoc.ToString(SaveOptions.DisableFormatting);
    }
    private string ProcessEntity(string entityXml, DbHelper dbHelper)
        {            
            entityXml = entityXml.Replace("<NoLogging>true</NoLogging>", "<NoLogging>false</NoLogging>");
            return entityXml;
        }

        public string UpdateCoreRefXMLNewInterlockEntityRelation(string xmlContent, List<NewInterlockTagDescription> newInterlockTags)
    {
        string entityRelationTemplate = @"
<EntityRelation>   <!-- Interlock Begin -->
    <EntityId>##InterlockTag##</EntityId>
</EntityRelation>  <!-- Interlock End -->";

        // Create a StringBuilder to hold the new <EntityRelation> elements
        StringBuilder entityRelationElements = new StringBuilder();

        // Generate the <EntityRelation> elements based on the NewInterlockTagDescription list
        foreach (var interlockTag in newInterlockTags)
        {
            string result = interlockTag.TagName;

            var match = Regex.Match(result, @"^(.*?)(STR|INT|STP)");

            string appendNode = string.Empty;

            // If a match is found, return the captured group
            if (match.Success)
            {
                appendNode = match.Groups[1].Value;
            }

            // Create the entity relation entry
            string entityRelationElement = entityRelationTemplate
                .Replace("##InterlockTag##", interlockTag.TagName);

            entityRelationElements.Append(entityRelationElement);

            // Find the position of the <EntityRelation> with the specific <EntityId>
            int entityRelationStartPosition = xmlContent.IndexOf($"<EntityId>{appendNode}</EntityId>");
            if (entityRelationStartPosition == -1)
            {
                
            }

            else
            {
                // Find the end position of the enclosing <EntityRelation> element
                int entityRelationEndPosition = xmlContent.IndexOf("</EntityRelation>", entityRelationStartPosition) + "</EntityRelation>".Length;

                // Insert the new <EntityRelation> elements after the existing <EntityRelation> element
                xmlContent = xmlContent.Insert(entityRelationEndPosition, entityRelationElements.ToString());

                // Reset the StringBuilder for the next iteration
                entityRelationElements.Clear();
            }

            
        }

        return xmlContent;
    }

    private string AddLanguageTagsForReceiveTagGlobal(string content, List<string> receiveTagGlobal)
    {
        // Define the Language template
        string languageTemplate = @"
<Language>
    <Designation>##ReceiveTag##</Designation>
    <LanguageText>PLC Receive</LanguageText>
</Language>";

        // Build the new Language elements based on the receiveTagGlobal list
        StringBuilder newLanguageElements = new StringBuilder();

        foreach (var receiveTag in receiveTagGlobal)
        {
            string languageElement = languageTemplate.Replace("##ReceiveTag##", receiveTag);
            newLanguageElements.AppendLine(languageElement);
        }

        // Locate the closing </Fls.Core.Ref> tag
        int closingTagIndex = content.LastIndexOf("</Fls.Core.Ref>");
        if (closingTagIndex == -1)
        {
            throw new Exception("Closing </Fls.Core.Ref> tag not found in the XML content.");
        }

        // Insert the new Language elements before the closing </Fls.Core.Ref> tag
        content = content.Insert(closingTagIndex, newLanguageElements.ToString());

        return content;
    }

    private string ProcessPointTypeIdsAndReplace(string xmlContent, List<(string PointTypeId, string Unit)> pointTypeIdsAndUnits, DbHelper dbHelper)
    {
        // Remove namespaces
        var xmlDoc = XDocument.Parse(RemoveAllNamespaces(xmlContent));
        var entities = xmlDoc.Descendants("Entity");
        string appendLanguageDataXmlContent = null;

        foreach (var entity in entities)
        {
            var pointTypeIdElement = entity.Element("PointTypeId");
            if (pointTypeIdElement != null)
            {
                string pointTypeId = pointTypeIdElement.Value;
                string searchParameter;

                // Determine whether to use Unit or Type for the search based on PointTypeId
                if (pointTypeId.Contains("Interlock", StringComparison.OrdinalIgnoreCase))
                {
                    string designationValue = entity.Element("Designation")?.Value ?? string.Empty;
                    designationValue = Regex.Replace(designationValue, "(INT\\d{2})", "_FB$1");                    
                    designationValue = $"XIC({designationValue})";
                    string Type = MethodToExtractInterlockTypeElement(designationValue, dbHelper);
                    string newPointTypeIdValue = dbHelper.GetECSPointTypeReplacement(pointTypeId, Type, dbHelper);
                    string appendDataXmlContent = dbHelper.GetInterlockAppendDataXml(newPointTypeIdValue);
                    appendLanguageDataXmlContent = dbHelper.GetLanguageDataXml(pointTypeId);

                    if (!string.IsNullOrEmpty(newPointTypeIdValue))
                    {
                        pointTypeIdElement.Value = newPointTypeIdValue;
                    }

                    if (!string.IsNullOrEmpty(appendDataXmlContent))
                    {
                        // Assuming 'entity' is an XElement representing the XML node where replacement occurs
                        var designationNode = entity.Element("Designation");
                        if (designationNode != null)
                        {
                            string designation = designationNode.Value;

                            if (SourceTagInterlock != null)
                            {
                                appendDataXmlContent = appendDataXmlContent.Replace("##SourceTag##", SourceTagInterlock);
                            }                            

                            // Wrap the additional XML content with a root element
                            string wrappedXmlContent = $"<Root>{appendDataXmlContent}</Root>";
                            var additionalXml = XElement.Parse(wrappedXmlContent);

                            // Append each child element of the wrapped content to the entity
                            foreach (var element in additionalXml.Elements())
                            {
                                entity.Add(element);
                            }
                        }
                    }

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
           
        }

        return xmlDoc.Declaration + xmlDoc.ToString(SaveOptions.DisableFormatting);
       
    }

    private string MethodToExtractSourceTagInterlock(string designationValue, string DotContent, DbHelper dbHelper)
    {
        string pattern = @"XIC\((.*?)\)";
        var match = System.Text.RegularExpressions.Regex.Match(designationValue, pattern);

        if (match.Success)
        {
            string extractedValue = match.Groups[1].Value;
            
            string part1 = extractedValue.Split(new[] { "_FB" }, StringSplitOptions.None)[0];
            
            string part2 = extractedValue.Substring(extractedValue.LastIndexOf("INT") + 3);

            string IntSuffix = dbHelper.ExtractIntSuffixValueInterlock(DotContent);

            string result = part1 + IntSuffix + part2 + ".IN";

            result = $"OTE({result})";

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

                            if (textContent.Contains(result))
                            {
                                var match2 = Regex.Match(textContent, @"\b(?:XIC|XIO)\(([^)]+)\)");

                                if (match2.Success)
                                {
                                    string extractcontent = match2.Groups[1].Value;

                                    string result2 = Regex.Replace(extractcontent, @"_.+$", "");

                                    return result2;
                                }
                            }
                        }
                    }
                }
            }            
        }
        
        return null;
    }

    private string MethodToExtractInterlockTypeElement(string designationValue, DbHelper dbHelper)
    {
        if (OriginalPrograms != null)
        {
            foreach (L5XProgram originalProgram in OriginalPrograms)
            {
                foreach (L5XRoutine originalRoutine in originalProgram.Routines)
                {
                    foreach (XmlElement textElement in originalRoutine.SelectNodes(".//Text"))
                    {
                        string textContent = textElement.InnerText;

                        if (textContent.Contains(designationValue))
                        {
                            var match = Regex.Match(textContent, @"OTE\(([^)]+)\)");

                            if (match.Success)
                            {
                                string extractcontent = match.Groups[1].Value;

                                var dotIndex = extractcontent.IndexOf('.');
                                if (dotIndex != -1 && dotIndex < extractcontent.Length - 1)
                                {
                                    string DotContent = extractcontent.Substring(dotIndex + 1);
                                    SourceTagInterlock = MethodToExtractSourceTagInterlock(designationValue, DotContent, dbHelper);
                                    string pLink = dbHelper.ExtractpLinkInterlockElement(DotContent);
                                    return pLink;
                                }
                            }
                        }
                    }
                }
            }
        }

        return null;
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

    public string UpdateCoreRefEntityRelationXmlContent(string coreRefXmlContent, List<string> receiveTagGlobal)
    {
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(coreRefXmlContent);

        // Define the new <EntityRelation> structure with placeholders
        string newEntityRelationTemplate = @"
<EntityRelation> 
    <EntityId>##ReceiveTag##</EntityId>
</EntityRelation>";

        // Create a StringBuilder to hold the new <EntityRelation> elements
        StringBuilder entityRelationElements = new StringBuilder();

        // Generate the <EntityRelation> elements based on the receiveTagGlobal list
        foreach (var receiveTag in receiveTagGlobal)
        {
            // Replace the placeholders in the template
            string entityRelationElement = newEntityRelationTemplate
                .Replace("##ReceiveTag##", receiveTag);

            // Append the generated element to the StringBuilder
            entityRelationElements.Append(entityRelationElement);
        }

        // Find the <EntityRelation> node where <EntityId> contains 'PLC'
        XmlNode? targetNode = doc.SelectSingleNode("//EntityRelation[contains(EntityId, 'PLC')]");

        if (targetNode == null)
        {
            // Define namespace manager to handle any potential namespaces in the XML
            XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
            string nsUri = doc.DocumentElement.NamespaceURI;

            if (!string.IsNullOrEmpty(nsUri))
            {
                nsManager.AddNamespace("ns", nsUri); // Add the root namespace if present
            }

            else
            {
                // If there is no namespace, adjust XPath to ignore namespaces
                nsManager.AddNamespace("ns", ""); // Empty namespace
            }

            // Attempt to find a fallback node in the document (e.g., root or a specific fallback node)
            targetNode = doc.SelectSingleNode("//ns:Fls.Core.Ref", nsManager);

            if (targetNode != null)
            {
                // Insert the new <EntityRelation> elements after the fallback node
                targetNode.InnerXml += entityRelationElements.ToString();
            }
            else
            {
                // If no <EntityRelation> or <Fls.Core.Ref> is found, append the new <EntityRelation> elements to the document root
                XmlNode? root = doc.DocumentElement;
                if (root != null)
                {
                    root.InnerXml += entityRelationElements.ToString();
                }
                else
                {
                    throw new Exception("No appropriate node found for inserting <EntityRelation> elements.");
                }
            }
        }
        else
        {
            // Insert the new <EntityRelation> elements after the found target node
            foreach (var receiveTag in receiveTagGlobal)
            {
                XmlDocumentFragment docFragment = doc.CreateDocumentFragment();
                docFragment.InnerXml = newEntityRelationTemplate.Replace("##ReceiveTag##", receiveTag);
                targetNode.ParentNode?.InsertAfter(docFragment, targetNode);
            }
        }

        // Return the updated XML content as a string
        using (var stringWriter = new StringWriter())
        using (var xmlTextWriter = XmlWriter.Create(stringWriter))
        {
            doc.WriteTo(xmlTextWriter);
            xmlTextWriter.Flush();
            return stringWriter.GetStringBuilder().ToString();
        }
    }

    public string UpdatePntConfigEntityReceivedXmlContent(
    string pntConfigXmlContent,
    List<string> receiveTagGlobal)
    {
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(pntConfigXmlContent);

        // Define the new <Entity> structure with placeholders
        string newEntityTemplate = $@"
    <Entity> <!-- POINTTYPEACESYSPLC2PLC8.0 -->
        <Designation>##ReceiveTag##</Designation>
        <IoType>CLX</IoType>
        <PointTypeId>PointTypeAcesysPlc2Plc8.0</PointTypeId>
        <TimeSeries>true</TimeSeries>
        <Compression>-1</Compression>
        <PiecewiseConstant>true</PiecewiseConstant>
        <DataType>2</DataType>
    </Entity>";

        // Find the <UnitGroup> node
        XmlNode unitGroupNode = doc.SelectSingleNode("//UnitGroup");

        if (unitGroupNode == null)
        {
            // Return original content if <UnitGroup> node is not found
            return pntConfigXmlContent;
        }

        // Create a fragment to hold all new <Entity> elements
        XmlDocumentFragment docFragment = doc.CreateDocumentFragment();

        foreach (var receiveTag in receiveTagGlobal)
        {
            // Create new <Entity> element
            string entityXml = newEntityTemplate.Replace("##ReceiveTag##", receiveTag);
            docFragment.InnerXml += entityXml;
        }

        // Insert the new <Entity> elements before the <UnitGroup> node
        unitGroupNode.ParentNode.InsertBefore(docFragment, unitGroupNode);

        // Return the updated XML content as a string
        using (var stringWriter = new StringWriter())
        using (var xmlTextWriter = XmlWriter.Create(stringWriter))
        {
            doc.WriteTo(xmlTextWriter);
            xmlTextWriter.Flush();
            return stringWriter.GetStringBuilder().ToString();
        }
    }



    // Method to remove all namespaces from the XML content using the provided approach
    public static string RemoveAllNamespaces(string xmlDocument)
    {
        XElement xmlDocumentWithoutNs = RemoveAllNamespaces(XElement.Parse(xmlDocument));
        return xmlDocumentWithoutNs.ToString();
    }

    // Core recursion function to remove namespaces from an XElement
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

    private string UpdateCoreRefLanguageXmlContent(
    string content,
    string controllerName,
    List<string> networkModules,
    List<string> nodeModules,
    List<AnalogInputModule> analogInputModules,
    List<AnalogOutputModule> analogOutputModules,
    List<DigitalInputModule> digitalInputModules,
    List<DigitalOutputModule> digitalOutputModules)
    {
        // Define the base Language structure with placeholders
        string cpuLanguage = $@"
<Language>
    <Designation>{controllerName}CPU</Designation>
    <LanguageText>PLC Status ({controllerName})</LanguageText>
</Language>";

        string netLanguageTemplate = $@"
<Language>
    <Designation>{controllerName}NET_##NetName##</Designation>
    <LanguageText>Net Status (##NetName##)</LanguageText>
</Language>";

        string nodeLanguageTemplate = $@"
<Language>
    <Designation>{controllerName}DIAG_##NodeName##</Designation>
    <LanguageText>Node Status (##NodeName##)</LanguageText>
</Language>";

        string moduleLanguageTemplate = $@"
<Language>
    <Designation>{controllerName}DIAG_##ModuleName##</Designation>
    <LanguageText>Slot ##ModuleSlot##: ##ModuleType##</LanguageText>
</Language>";

        // Helper function to replace '&' with 'and'
        string ReplaceAmpersand(string text)
        {
            return text.Replace("&", "and");
        }

        // Replace placeholders with actual values from the lists and apply ampersand replacement
        var netLanguages = networkModules.Select(net => ReplaceAmpersand(netLanguageTemplate.Replace("##NetName##", net))).ToList();
        var nodeLanguages = nodeModules.Select(node => ReplaceAmpersand(nodeLanguageTemplate.Replace("##NodeName##", node))).ToList();

        var analogInputLanguages = analogInputModules.Select(module => ReplaceAmpersand(moduleLanguageTemplate
            .Replace("##ModuleName##", module.Name)
            .Replace("##ModuleSlot##", module.Slot_No)
            .Replace("##ModuleType##", module.ModuleName))).ToList();

        var analogOutputLanguages = analogOutputModules.Select(module => ReplaceAmpersand(moduleLanguageTemplate
            .Replace("##ModuleName##", module.Name)
            .Replace("##ModuleSlot##", module.Slot_No)
            .Replace("##ModuleType##", module.ModuleName))).ToList();

        var digitalInputLanguages = digitalInputModules.Select(module => ReplaceAmpersand(moduleLanguageTemplate
            .Replace("##ModuleName##", module.Name)
            .Replace("##ModuleSlot##", module.Slot_No)
            .Replace("##ModuleType##", module.ModuleName))).ToList();

        var digitalOutputLanguages = digitalOutputModules.Select(module => ReplaceAmpersand(moduleLanguageTemplate
            .Replace("##ModuleName##", module.Name)
            .Replace("##ModuleSlot##", module.Slot_No)
            .Replace("##ModuleType##", module.ModuleName))).ToList();

        // Combine all Language entries into a single string with proper XML formatting
        string combinedLanguages = cpuLanguage + Environment.NewLine
            + string.Join(Environment.NewLine, netLanguages) + Environment.NewLine
            + string.Join(Environment.NewLine, nodeLanguages) + Environment.NewLine
            + string.Join(Environment.NewLine, analogInputLanguages) + Environment.NewLine
            + string.Join(Environment.NewLine, analogOutputLanguages) + Environment.NewLine
            + string.Join(Environment.NewLine, digitalInputLanguages) + Environment.NewLine
            + string.Join(Environment.NewLine, digitalOutputLanguages);

        // Load the existing XML content
        XmlDocument doc = new XmlDocument();
        try
        {
            doc.LoadXml(content);
        }
        catch (XmlException ex)
        {
            throw new Exception("Invalid XML content", ex);
        }

        XmlNamespaceManager nsManager = new XmlNamespaceManager(doc.NameTable);
        string nsUri = doc.DocumentElement.NamespaceURI;

        if (!string.IsNullOrEmpty(nsUri))
        {
            nsManager.AddNamespace("ns", nsUri); // Add the root namespace
        }
        else
        {
            nsManager.AddNamespace("ns", ""); // Empty namespace
        }

        // Attempt to locate the closing </Fls.Ecc.PLC.Config> tag
        XmlNode? closingNode = doc.SelectSingleNode("//ns:Fls.Ecc.PLC.Config", nsManager);

        if (closingNode == null)
        {
            // Attempt to find a fallback node if <Fls.Ecc.PLC.Config> is not found
            closingNode = doc.SelectSingleNode("//ns:Fls.Core.Ref", nsManager);

            if (closingNode == null)
            {
                // Fallback to document root if no other node is found
                XmlNode? root = doc.DocumentElement;
                if (root != null)
                {
                    root.InnerXml += combinedLanguages;
                }
                else
                {
                    throw new Exception("No appropriate node found for inserting <Language> elements.");
                }
            }
            else
            {
                // Append the combined languages after the <Fls.Core.Ref> node
                closingNode.InnerXml += combinedLanguages;
            }
        }
        else
        {
            // Insert the combined languages before the closing </Fls.Ecc.PLC.Config> tag
            XmlDocumentFragment docFragment = doc.CreateDocumentFragment();
            docFragment.InnerXml = combinedLanguages;
            closingNode.ParentNode?.InsertBefore(docFragment, closingNode);
        }

        // Return the updated XML content as a formatted string
        using (StringWriter stringWriter = new StringWriter())
        using (XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter))
        {
            xmlTextWriter.Formatting = Formatting.Indented;
            doc.WriteTo(xmlTextWriter);
            return stringWriter.ToString();
        }
    }

    private void MethodToExtractPLCtoPLC(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        if (Project != null)
        {            
            XmlNode masterProgramNode = OriginalPrograms.SelectSingleNode(".//Program[@Name='MasterProgram']");
            if (masterProgramNode != null)
            {
                XmlNode plcToPlcRoutineNode = masterProgramNode.SelectSingleNode(".//Routine[@Name='PLCtoPLC']");

                if (plcToPlcRoutineNode != null)
                {
                    XmlNodeList rungNodes = plcToPlcRoutineNode.SelectNodes(".//Rung");

                    foreach (XmlNode rungNode in rungNodes)
                    {
                        XmlNode textNode = rungNode.SelectSingleNode("Text");

                        if (textNode != null)
                        {
                            if (Regex.IsMatch(textNode.InnerText, @"\bAFI\(\)\b"))
                            {
                                continue; // Skip processing if AFI() is found
                            }

                            Match match = Regex.Match(textNode.InnerText, @"AsysComm\(([^,]+),([^,]+),([^,]+),");

                            if (match.Success)
                            {
                                string firstOperand = match.Groups[1].Value.Trim();
                                string secondOperand = match.Groups[2].Value.Trim();
                                string thirdOperand = match.Groups[3].Value.Trim();
                                ReceiveTagGlobal.Add(firstOperand + "_Rx");

                                string PrimaryIPAdr = ExtractIPAddress(secondOperand);
                                string SecondaryIPAdr = ExtractIPAddress(thirdOperand);

                                var programsToModify = Project.Content?.Controller?.Programs;

                                List<string> AsysPLCRcvValues = ExtractStructureDataValueMembers(firstOperand);

                                int MaxUNIT_HMI_Index = GetMaxHmiUnitIndex();

                                string wrappedHMIUnit = $"HMI_UNIT[{MaxUNIT_HMI_Index}]";

                                PLC_Rx_HMI_Unit.Add(wrappedHMIUnit);

                                XmlNode AsysPLCRcvFBTag = CreateAsysPLCRcvFBTagNode(xmlDoc);

                                XmlNode AsysPLCSendFBTag = CreateAsysPLCSendFBTagNode(firstOperand);

                                CreateFPInstance_Rx_FpTagNode(firstOperand,progress);

                                string ConvertedAsysPLCRcv = ConvertToAsysPLCRcvOperatorOperands(textNode.InnerText, dbHelper, "AsysComm", options,"", AsysPLCRcvValues, wrappedHMIUnit, firstOperand);

                                string ConvertedAsysPLCSend = ConvertToAsysPLCSendOperatorOperands(textNode.InnerText, dbHelper, "AsysComm", options, "", AsysPLCRcvValues, wrappedHMIUnit, firstOperand);

                                CreatePLCtoPLCInstanceProgram(firstOperand, PrimaryIPAdr, SecondaryIPAdr, programsToModify, ConvertedAsysPLCRcv, ConvertedAsysPLCSend, AsysPLCRcvFBTag, AsysPLCSendFBTag);

                            }
                        }
                    }
                }
            }

        }
    }

    private XmlNode CreateAsysPLCSendFBTagNode(string fbInstance)
    {
        // Create the root element
        XmlNode tagNode = xmlDocNew.CreateElement("Tag");
        tagNode.Attributes.Append(CreateAttribute(xmlDocNew, "Name", $"PLC_SEND_{fbInstance}"));
        tagNode.Attributes.Append(CreateAttribute(xmlDocNew, "TagType", "Base"));
        tagNode.Attributes.Append(CreateAttribute(xmlDocNew, "DataType", "AsysPLCSend"));
        tagNode.Attributes.Append(CreateAttribute(xmlDocNew, "Usage", "Public"));
        tagNode.Attributes.Append(CreateAttribute(xmlDocNew, "Constant", "false"));
        tagNode.Attributes.Append(CreateAttribute(xmlDocNew, "ExternalAccess", "Read/Write"));        

        // Create and add Description element
        XmlNode descriptionNode = xmlDocNew.CreateElement("Description");
        descriptionNode.InnerXml = $"<![CDATA[PLC-PLC Send Data]]>";
        tagNode.AppendChild(descriptionNode);

        // Create Data element with child elements
        XmlNode dataNode = xmlDocNew.CreateElement("Data");
        XmlNode structureNode = xmlDocNew.CreateElement("Structure");
        structureNode.Attributes.Append(CreateAttribute(xmlDocNew, "DataType", "AsysPLCSend"));

        // Add DataValueMember elements
        structureNode.AppendChild(CreateDataValueMember(xmlDocNew, "EnableIn", "BOOL", "1"));
        structureNode.AppendChild(CreateDataValueMember(xmlDocNew, "EnableOut", "BOOL", "0"));
        structureNode.AppendChild(CreateDataValueMember(xmlDocNew, "NetA_OK", "BOOL", "0"));
        structureNode.AppendChild(CreateDataValueMember(xmlDocNew, "NetB_OK", "BOOL", "0"));
        structureNode.AppendChild(CreateDataValueMember(xmlDocNew, "NetA_Active", "BOOL", "0"));
        structureNode.AppendChild(CreateDataValueMember(xmlDocNew, "NetB_Active", "BOOL", "0"));

        dataNode.AppendChild(structureNode);
        tagNode.AppendChild(dataNode);

        return tagNode;
    }

    public void InsertToPLCToPLCChildProgramNode(string PLC_Rx_Tx)
    {
        var program = Project.Content?.Controller?.Programs;
        var doc = program.OwnerDocument;
        int seq = GetNewSequenceNumber(); // Assume you have a method to get a new sequence number
        var childProgramNode = new L5XChildProgram(null, "ChildProgram", null,doc, seq);
        childProgramNode.ChildProgramName = PLC_Rx_Tx;

        // Find the ChildPrograms node or create it if it doesn't exist
        XmlNode? childProgramsNode = program.SelectSingleNode($"//Program[@Name='PLCtoPLC']").SelectSingleNode("ChildPrograms");
        if (childProgramsNode == null)
        {
            childProgramsNode = doc.CreateElement("ChildPrograms");
            childProgramsNode.AppendChild(childProgramNode);
        }

        // Append the new child program node to the ChildPrograms node
        childProgramsNode.AppendChild(childProgramNode);
    }

    public XmlNode CreateAsysPLCRcvFBTagNode(XmlDocument xmlDoc)
    {
        // Create the Tag element and set its attributes
        XmlElement tag = xmlDocNew.CreateElement("Tag");
        tag.SetAttribute("Name", "FB");
        tag.SetAttribute("TagType", "Base");
        tag.SetAttribute("DataType", "AsysPLCRcv");
        tag.SetAttribute("Usage", "Public");
        tag.SetAttribute("Constant", "false");
        tag.SetAttribute("ExternalAccess", "Read/Write");

        // Create the Description element and set its inner content
        XmlElement description = xmlDocNew.CreateElement("Description");
        description.InnerXml = "<![CDATA[Receive Data From PLC]]>";
        tag.AppendChild(description);

        // Create the Data element and set its Format attribute
        XmlElement data = xmlDocNew.CreateElement("Data");
        data.SetAttribute("Format", "Decorated");
        tag.AppendChild(data);

        // Create the Structure element and set its DataType attribute
        XmlElement structure = xmlDocNew.CreateElement("Structure");
        structure.SetAttribute("DataType", "AsysPLCRcv");
        data.AppendChild(structure);

        // Create DataValueMember elements with respective attributes and values
        structure.AppendChild(CreateDataValueMember(xmlDocNew, "EnableIn", "BOOL", "1"));
        structure.AppendChild(CreateDataValueMember(xmlDocNew, "EnableOut", "BOOL", "0"));
        structure.AppendChild(CreateDataValueMember(xmlDocNew, "Clear_Bool", "BOOL", "1"));
        structure.AppendChild(CreateDataValueMember(xmlDocNew, "Clear_Int", "BOOL", "1"));
        structure.AppendChild(CreateDataValueMember(xmlDocNew, "Clear_Real", "BOOL", "1"));
        structure.AppendChild(CreateDataValueMember(xmlDocNew, "Rcv_OK", "BOOL", "0"));

        return tag;
    }

    private XmlElement CreateDataValueMember(XmlDocument xmlDoc, string name, string dataType, string value)
    {
        XmlElement dataValueMember = xmlDoc.CreateElement("DataValueMember");
        dataValueMember.SetAttribute("Name", name);
        dataValueMember.SetAttribute("DataType", dataType);
        dataValueMember.SetAttribute("Value", value);
        return dataValueMember;
    }

    private string ConvertToAsysPLCSendOperatorOperands(string innerText, DbHelper dbHelper, string dataType, RockwellUpgradeOptions options, string Routinename, List<string> AsysPLCRcvValues, string wrappedHMIUnit, string firstOperand)
    {
        // Define a regex pattern to capture the operator
        string pattern = @"^(\w+)\s*\((.*)\);";

        // Use regex to match the pattern
        Match match = Regex.Match(innerText, pattern);

        if (match.Success)
        {
            string newOperator = "AsysPLCSend";

            string operands = dbHelper.GetOperandsForOperator(newOperator, innerText, dataType, options, Routinename, AsysPLCRcvValues, wrappedHMIUnit, firstOperand, HMI_INTERLOCK_MAX_GROUP);

            // Replace the old operator with the new operator and the old operands with the fetched operands
            string newText = $"{newOperator}({operands});";

            return newText;
        }

        // Return the original text if no match was found
        return innerText;
    }

    private List<string> ExtractStructureDataValueMembers(string firstOperand)
    {
        if (OriginalPrograms != null)
        {
            foreach (L5XProgram originalProgram in OriginalPrograms)
            {
                foreach (var item in originalProgram)
                {
                    if (item is L5XTags tags)
                    {
                        foreach (L5XTag tag in tags)
                        {
                            XmlAttribute nameAttribute = tag.Attributes?["Name"];

                            if (nameAttribute != null && nameAttribute.Value == firstOperand)
                            {
                                List<string> values = new List<string>();

                                foreach (XmlNode dataNode in tag.SelectNodes("Data"))
                                {
                                    XmlAttribute formatAttribute = dataNode.Attributes?["Format"];
                                    if (formatAttribute != null && formatAttribute.Value == "Decorated")
                                    {
                                        XmlNode structureNode = dataNode.SelectSingleNode("Structure");
                                        if (structureNode != null)
                                        {
                                            foreach (XmlNode dataValueMemberNode in structureNode.SelectNodes("DataValueMember"))
                                            {
                                                XmlAttribute nameAttr = dataValueMemberNode.Attributes?["Name"];
                                                XmlAttribute valueAttr = dataValueMemberNode.Attributes?["Value"];

                                                if (nameAttr != null && valueAttr != null)
                                                {
                                                    if (nameAttr.Value == "Clear_Bool" || nameAttr.Value == "Clear_Int" || nameAttr.Value == "Clear_Real")
                                                    {
                                                        values.Add(valueAttr.Value);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                return values;
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    private string ConvertToAsysPLCRcvOperatorOperands(string innerText, DbHelper dbHelper, string dataType, RockwellUpgradeOptions options, string Routinename, List<string> AsysPLCRcvValues, string wrappedHMIUnit, string firstOperand)
    {
        // Define a regex pattern to capture the operator
        string pattern = @"^(\w+)\s*\((.*)\);";

        // Use regex to match the pattern
        Match match = Regex.Match(innerText, pattern);

        if (match.Success)
        {
            string newOperator = "AsysPLCRcv";

            string operands = dbHelper.GetOperandsForOperator(newOperator, innerText, dataType, options, Routinename, AsysPLCRcvValues, wrappedHMIUnit, firstOperand, HMI_INTERLOCK_MAX_GROUP);

            // Replace the old operator with the new operator and the old operands with the fetched operands
            string newText = $"{newOperator}({operands});";

            return newText;
        }

        // Return the original text if no match was found
        return innerText;
    }

    private void CreatePLCtoPLCInstanceProgram(string fbInstance, string primaryIPAdr, string secondaryIPAdr, XmlNode parentNode, string convertedAsysPLCRcv, string convertedAsysPLCSend, XmlNode AsysPLCRcvFBTag, XmlNode AsysPLCSendFBTag)
    {
        XmlDocument xmlDoc = parentNode.OwnerDocument;

        // Create Rx Program
        XmlElement rxProgram = xmlDoc.CreateElement("Program");
        rxProgram.SetAttribute("Name", $"{fbInstance}_Rx");
        rxProgram.SetAttribute("TestEdits", "false");
        rxProgram.SetAttribute("MainRoutineName", "PLCCommRcv");
        rxProgram.SetAttribute("Disabled", "false");
        rxProgram.SetAttribute("UseAsFolder", "false");       

        XmlElement rxDescription = xmlDoc.CreateElement("Description");
        rxDescription.InnerXml = "<![CDATA[ACESYS Send Block]]>";
        rxProgram.AppendChild(rxDescription);

        XmlElement rxTags = xmlDoc.CreateElement("Tags");
        XmlNode rxTagsTextNode = xmlDoc.CreateTextNode(string.Empty); // Add an empty text node inside to prevent self-closing
        rxTags.AppendChild(rxTagsTextNode);
        rxTags.AppendChild(xmlDoc.ImportNode(AsysPLCRcvFBTag, true)); // Append the AsysPLCRcvFBTag to rxTags
        rxProgram.AppendChild(rxTags);

        XmlElement rxRoutines = xmlDoc.CreateElement("Routines");
        rxProgram.AppendChild(rxRoutines);

        XmlElement rxRoutine = xmlDoc.CreateElement("Routine");
        rxRoutine.SetAttribute("Name", "PLCCommRcv");
        rxRoutine.SetAttribute("Type", "RLL");
        rxRoutines.AppendChild(rxRoutine);

        XmlElement rxRLLContent = xmlDoc.CreateElement("RLLContent");
        rxRoutine.AppendChild(rxRLLContent);

        XmlElement rxRung = xmlDoc.CreateElement("Rung");
        rxRung.SetAttribute("Number", "0");
        rxRung.SetAttribute("Type", "N");
        rxRLLContent.AppendChild(rxRung);

        XmlElement rxComment = xmlDoc.CreateElement("Comment");
        rxComment.InnerXml = $@"<![CDATA[
____________________________________________________

ACESYS PLC to PLC Communication (RECEIVE)

>>> {fbInstance}_Rx <<<

____________________________________________________]]>";
        rxRung.AppendChild(rxComment);



        XmlElement rxText = xmlDoc.CreateElement("Text");
        rxText.InnerXml = "<![CDATA[NOP();]]>";     //{convertedAsysPLCRcv}]]>";
        rxRung.AppendChild(rxText);

        XmlElement rxRung2 = xmlDoc.CreateElement("Rung");
        rxRung2.SetAttribute("Number", "1");
        rxRung2.SetAttribute("Type", "N");
        rxRLLContent.AppendChild(rxRung2);

        XmlElement rxText2 = xmlDoc.CreateElement("Text");
        rxText2.InnerXml = $"<![CDATA[{convertedAsysPLCRcv}]]>";
        rxRung2.AppendChild(rxText2);


        // Append Rx Program to the parent node
        parentNode.AppendChild(rxProgram);
        InsertToPLCToPLCChildProgramNode($"{fbInstance}_Rx");//Append to Program Node PLCtoPLC

        // Create Tx Program
        XmlElement txProgram = xmlDoc.CreateElement("Program");
        txProgram.SetAttribute("Name", $"{fbInstance}_Tx");
        txProgram.SetAttribute("TestEdits", "false");
        txProgram.SetAttribute("MainRoutineName", "PLCCommSend");
        txProgram.SetAttribute("Disabled", "false");
        txProgram.SetAttribute("UseAsFolder", "false");

        XmlElement txDescription = xmlDoc.CreateElement("Description");
        txDescription.InnerXml = "<![CDATA[ACESYS Receive Block]]>";
        txProgram.AppendChild(txDescription);

        XmlElement txTags = xmlDoc.CreateElement("Tags");
        XmlNode txTagsTextNode = xmlDoc.CreateTextNode(string.Empty); // Add an empty text node inside to prevent self-closing
        txTags.AppendChild(txTagsTextNode);
        txTags.AppendChild(xmlDoc.ImportNode(AsysPLCSendFBTag, true)); // Append the AsysPLCSendFBTag to txTags
        txProgram.AppendChild(txTags);

        XmlElement txRoutines = xmlDoc.CreateElement("Routines");
        txProgram.AppendChild(txRoutines);

        XmlElement txRoutine = xmlDoc.CreateElement("Routine");
        txRoutine.SetAttribute("Name", "PLCCommSend");
        txRoutine.SetAttribute("Type", "RLL");
        txRoutines.AppendChild(txRoutine);

        XmlElement txRLLContent = xmlDoc.CreateElement("RLLContent");
        txRoutine.AppendChild(txRLLContent);

        XmlElement txRung1 = xmlDoc.CreateElement("Rung");
        txRung1.SetAttribute("Number", "0");
        txRung1.SetAttribute("Type", "N");
        txRLLContent.AppendChild(txRung1);

        XmlElement txComment1 = xmlDoc.CreateElement("Comment");
        txComment1.InnerXml = $@"<![CDATA[
____________________________________________________

ACESYS PLC to PLC Communication (SEND)

>>> {fbInstance}_Tx <<<

____________________________________________________]]>";
        txRung1.AppendChild(txComment1);

        XmlElement txText1 = xmlDoc.CreateElement("Text");
        txText1.InnerXml = "<![CDATA[NOP();]]>";
        txRung1.AppendChild(txText1);

        XmlElement txRung2 = xmlDoc.CreateElement("Rung");
        txRung2.SetAttribute("Number", "1");
        txRung2.SetAttribute("Type", "N");
        txRLLContent.AppendChild(txRung2);

        XmlElement txComment2 = xmlDoc.CreateElement("Comment");
        txComment2.InnerXml = $@"<![CDATA[
____________________________________________________

>>> To PLC: XXXXX <<<
IP-addr Primary  : {primaryIPAdr}
IP-addr Secondary: {secondaryIPAdr}

____________________________________________________]]>";
        txRung2.AppendChild(txComment2);

        XmlElement txText2 = xmlDoc.CreateElement("Text");
        txText2.InnerXml = "<![CDATA[NOP();]]>"; 
        txRung2.AppendChild(txText2);

        XmlElement txRung3 = xmlDoc.CreateElement("Rung");
        txRung3.SetAttribute("Number", "2");
        txRung3.SetAttribute("Type", "N");
        txRLLContent.AppendChild(txRung3);

        XmlElement txText3 = xmlDoc.CreateElement("Text");
        txText3.InnerXml = $"<![CDATA[{convertedAsysPLCSend}]]>";
        txRung3.AppendChild(txText3);

        // Append Tx Program to the parent node
        parentNode.AppendChild(txProgram);
        InsertToPLCToPLCChildProgramNode($"{fbInstance}_Tx");//Append to Program Node PLCtoPLC
    }

    public string ExtractIPAddress(string TagName)
    {
        string IpAddress = "";
        L5XTags OriginalTags = (L5XTags?)Project.Content?.Controller?.Tags;

        if (OriginalTags != null)
        {
            foreach (L5XTag tag in OriginalTags)
            {
                if (tag != null && tag.TagName == TagName)
                {
                    if (tag.FirstChild != null && tag.FirstChild.FirstChild != null)
                    {
                        XmlElement messageParameters = tag.FirstChild.FirstChild as XmlElement;
                        if (messageParameters != null)
                        {
                            string connectionPath = messageParameters.GetAttribute("ConnectionPath");
                            if (!string.IsNullOrEmpty(connectionPath))
                            {
                                // Use regex to extract the content between the 2nd and 3rd commas
                                Match match = Regex.Match(connectionPath, @"(?:[^,]*,){2}\s*([^,]*)");
                                if (match.Success)
                                {
                                    IpAddress = match.Groups[1].Value.Trim();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        return IpAddress;
    }

    private void MethodToExtractDigitalInputOutputModule(RockwellL5XProject project, string programName, IProgress<string> progress)
    {
        var digitalInputModules = project.SelectNodes("//Module")
     .Cast<XmlNode>()
     .Where(module =>
     {
         string catalogNumber = module.Attributes["CatalogNumber"]?.Value;
         return catalogNumber != null && (
             catalogNumber.Contains("1756-IA") ||
             catalogNumber.Contains("1756-IB") ||
             catalogNumber.Contains("1756-IC") ||
             catalogNumber.Contains("1756-IG") ||
             catalogNumber.Contains("1756-IH") ||
             catalogNumber.Contains("1756-IM") ||
             catalogNumber.Contains("1756-IN") ||
             catalogNumber.Contains("1756-IV") ||
             catalogNumber.Contains("1715-IB") ||
             catalogNumber.Contains("1734-IA") ||
             catalogNumber.Contains("1734-IB") ||
             catalogNumber.Contains("1734-IM") ||
             catalogNumber.Contains("1734-IV") ||
             catalogNumber.Contains("1738-IA") ||
             catalogNumber.Contains("1738-IB") ||
             catalogNumber.Contains("1738-IV") ||
             catalogNumber.Contains("1746-IA") ||
             catalogNumber.Contains("1746-IB") ||
             catalogNumber.Contains("1746-IC") ||
             catalogNumber.Contains("1746-IG") ||
             catalogNumber.Contains("1746-IH") ||
             catalogNumber.Contains("1746-IM") ||
             catalogNumber.Contains("1746-IN") ||
             catalogNumber.Contains("1746-ITB") ||
             catalogNumber.Contains("1746-ITV") ||
             catalogNumber.Contains("1746-IV") ||
             catalogNumber.Contains("1769-IA") ||
             catalogNumber.Contains("1769-IG") ||
             catalogNumber.Contains("1769-IM") ||
             catalogNumber.Contains("1769-IQ") ||
             catalogNumber.Contains("1794-IA") ||
             catalogNumber.Contains("1794-IB") ||
             catalogNumber.Contains("1794-IC") ||
             catalogNumber.Contains("1794-IG") ||
             catalogNumber.Contains("1794-IH") ||
             catalogNumber.Contains("1794-IM") ||
             catalogNumber.Contains("1794-IV") ||
             catalogNumber.Contains("1797-IB"));
     })
     .Select(module =>
     {
         // Capture required details including Address from Ports
         var address = module.SelectSingleNode("Ports/Port")?.Attributes["Address"]?.Value;

         return new DigitalInputModule
         {
             Name = module.Attributes["Name"]?.Value,
             ModuleName = module.Attributes["CatalogNumber"]?.Value,
             NodeModule = module.Attributes["ParentModule"]?.Value,
             NetworkModule = FindNetName(module.Attributes["ParentModule"]?.Value),
             Slot_No = address
         };
     })
     .ToList();

        // Add the identified DigitalInput modules to the list
        DigitalInputModules.AddRange(digitalInputModules);

        var digitalOutputModules = project.SelectNodes("//Module")
    .Cast<XmlNode>()
    .Where(module =>
    {
        string catalogNumber = module.Attributes["CatalogNumber"]?.Value;
        return catalogNumber != null && (
            catalogNumber.Contains("1756-OA") ||
            catalogNumber.Contains("1756-OB") ||
            catalogNumber.Contains("1756-OC") ||
            catalogNumber.Contains("1756-OG") ||
            catalogNumber.Contains("1756-OH") ||
            catalogNumber.Contains("1756-ON") ||
            catalogNumber.Contains("1756-OV") ||
            catalogNumber.Contains("1756-OW") ||
            catalogNumber.Contains("1756-OX") ||
            catalogNumber.Contains("1715-OB") ||
            catalogNumber.Contains("1734-OA") ||
            catalogNumber.Contains("1734-OB") ||
            catalogNumber.Contains("1734-OV") ||
            catalogNumber.Contains("1734-OW") ||
            catalogNumber.Contains("1734-OX") ||
            catalogNumber.Contains("1738-OA") ||
            catalogNumber.Contains("1738-OB") ||
            catalogNumber.Contains("1738-OV") ||
            catalogNumber.Contains("1738-OW") ||
            catalogNumber.Contains("1746-OA") ||
            catalogNumber.Contains("1746-OB") ||
            catalogNumber.Contains("1746-OG") ||
            catalogNumber.Contains("1746-OV") ||
            catalogNumber.Contains("1746-OW") ||
            catalogNumber.Contains("1746-OX") ||
            catalogNumber.Contains("1769-OA") ||
            catalogNumber.Contains("1769-OB") ||
            catalogNumber.Contains("1769-OG") ||
            catalogNumber.Contains("1769-OV") ||
            catalogNumber.Contains("1769-OW") ||
            catalogNumber.Contains("1794-OA") ||
            catalogNumber.Contains("1794-OB") ||
            catalogNumber.Contains("1794-OC") ||
            catalogNumber.Contains("1794-OG") ||
            catalogNumber.Contains("1794-OM") ||
            catalogNumber.Contains("1794-OV") ||
            catalogNumber.Contains("1794-OW") ||
            catalogNumber.Contains("1797-OB"));
    })
    .Select(module =>
    {
        // Capture required details including Address from Ports
        var address = module.SelectSingleNode("Ports/Port")?.Attributes["Address"]?.Value;

        return new DigitalOutputModule
        {
            Name = module.Attributes["Name"]?.Value,
            ModuleName = module.Attributes["CatalogNumber"]?.Value,
            NodeModule = module.Attributes["ParentModule"]?.Value,
            NetworkModule = FindNetName(module.Attributes["ParentModule"]?.Value),
            Slot_No = address
        };
    })
    .ToList();

        // Add the identified DigitalOutput modules to the list
        DigitalOutputModules.AddRange(digitalOutputModules);

        UpdateDiagDigitalProgram(Project, DigitalInputModules, DigitalOutputModules);
    }

    private void UpdateDiagDigitalProgram(RockwellL5XProject project, List<DigitalInputModule> digitalInputModules, List<DigitalOutputModule> digitalOutputModules)
    {
        var programNode = project.SelectSingleNode("//Program[@Name='DiagDigital']");
        if (programNode == null) return;

        var tagsNode = programNode.SelectSingleNode("Tags");
        var routineNode = programNode.SelectSingleNode("Routines/Routine[@Name='DiagDigital']/RLLContent");

        if (tagsNode == null || routineNode == null) return;

        // Remove existing placeholders for ##ModuleName##_STATUS and ##ModuleName##_Timer tags
        var existingStatusTags = tagsNode.SelectNodes("Tag[contains(@Name, '_STATUS')]");
        foreach (XmlNode tag in existingStatusTags)
        {
            tagsNode.RemoveChild(tag);
        }

        var existingTimerTags = tagsNode.SelectNodes("Tag[contains(@Name, '_Timer')]");
        foreach (XmlNode tag in existingTimerTags)
        {
            tagsNode.RemoveChild(tag);
        }

        // Remove existing Rungs 2a and 2b
        var existingRungs2a = routineNode.SelectNodes("Rung[@Number='2a']");
        foreach (XmlNode rung in existingRungs2a)
        {
            routineNode.RemoveChild(rung);
        }

        var existingRungs2b = routineNode.SelectNodes("Rung[@Number='2b']");
        foreach (XmlNode rung in existingRungs2b)
        {
            routineNode.RemoveChild(rung);
        }

        int rungNumber = 2; // Start numbering for new rungs       

        // Helper method to generate IoStatusIndex
        string GenerateIoStatusIndex()
        {
            string index = $"[{majorIndex:D2}].{minorIndex:D2}";

            if (minorIndex < 15)
            {
                minorIndex++;
            }
            
            if (minorIndex >= 15)
            {                
                majorIndex++;
                minorIndex = 0;
            }
            return index;
        }

        AddDigitalInputModules(project, tagsNode, routineNode, digitalInputModules, ref rungNumber, GenerateIoStatusIndex);
        AddDigitalOutputModules(project, tagsNode, routineNode, digitalOutputModules, ref rungNumber, GenerateIoStatusIndex);
    }

    private void AddDigitalInputModules(RockwellL5XProject project, XmlNode tagsNode, XmlNode routineNode, List<DigitalInputModule> modules, ref int rungNumber, Func<string> generateIoStatusIndex)
    {
        int index = 1; // Start index at 1
        foreach (var module in modules)
        {
            string moduleName = module.Name;
            string catalogNumber = module.ModuleName;
            string netName = module.NetworkModule;
            string nodeName = module.NodeModule;
            string moduleSlot = module.Slot_No;

            if (string.IsNullOrEmpty(moduleName))
            {
                moduleName = $"{nodeName}_{index:D2}";
                index++;
            }

            string ioStatusIndex = generateIoStatusIndex();

            // Create new Tags for each module
            XmlElement statusTag = project.CreateElement("Tag");
            statusTag.SetAttribute("Name", $"{moduleName}_STATUS");
            statusTag.SetAttribute("TagType", "Alias");
            statusTag.SetAttribute("Radix", "Decimal");
            statusTag.SetAttribute("AliasFor", $"HMI_IoStatus{ioStatusIndex}");
            HMI_IoStatus_Node.Add($"HMI_IoStatus{ioStatusIndex}");
            statusTag.SetAttribute("Usage", "Public");
            statusTag.SetAttribute("ExternalAccess", "Read/Write");

            XmlElement statusDescription = project.CreateElement("Description");
            statusDescription.AppendChild(project.CreateCDataSection("Module Status OK"));
            statusTag.AppendChild(statusDescription);
            tagsNode.AppendChild(statusTag);

            XmlElement timerTag = project.CreateElement("Tag");
            timerTag.SetAttribute("Name", $"{moduleName}_Timer");
            timerTag.SetAttribute("TagType", "Base");
            timerTag.SetAttribute("DataType", "TIMER");
            timerTag.SetAttribute("Constant", "false");
            timerTag.SetAttribute("ExternalAccess", "Read/Write");

            XmlElement timerDescription = project.CreateElement("Description");
            timerDescription.AppendChild(project.CreateCDataSection($"Delay For fault On card {moduleName}"));
            timerTag.AppendChild(timerDescription);

            XmlElement timerData = project.CreateElement("Data");
            timerData.SetAttribute("Format", "Decorated");

            XmlElement timerStructure = project.CreateElement("Structure");
            timerStructure.SetAttribute("DataType", "TIMER");

            XmlElement timerPRE = project.CreateElement("DataValueMember");
            timerPRE.SetAttribute("Name", "PRE");
            timerPRE.SetAttribute("DataType", "DINT");
            timerPRE.SetAttribute("Radix", "Decimal");
            timerPRE.SetAttribute("Value", "1000");

            XmlElement timerACC = project.CreateElement("DataValueMember");
            timerACC.SetAttribute("Name", "ACC");
            timerACC.SetAttribute("DataType", "DINT");
            timerACC.SetAttribute("Radix", "Decimal");
            timerACC.SetAttribute("Value", "0");

            XmlElement timerEN = project.CreateElement("DataValueMember");
            timerEN.SetAttribute("Name", "EN");
            timerEN.SetAttribute("DataType", "BOOL");
            timerEN.SetAttribute("Value", "0");

            XmlElement timerTT = project.CreateElement("DataValueMember");
            timerTT.SetAttribute("Name", "TT");
            timerTT.SetAttribute("DataType", "BOOL");
            timerTT.SetAttribute("Value", "0");

            XmlElement timerDN = project.CreateElement("DataValueMember");
            timerDN.SetAttribute("Name", "DN");
            timerDN.SetAttribute("DataType", "BOOL");
            timerDN.SetAttribute("Value", "0");

            timerStructure.AppendChild(timerPRE);
            timerStructure.AppendChild(timerACC);
            timerStructure.AppendChild(timerEN);
            timerStructure.AppendChild(timerTT);
            timerStructure.AppendChild(timerDN);

            timerData.AppendChild(timerStructure);
            timerTag.AppendChild(timerData);
            tagsNode.AppendChild(timerTag);

            // Create new Rung for DigitalInputModules
            XmlElement newRung = project.CreateElement("Rung");
            newRung.SetAttribute("Number", $"{rungNumber}a");
            newRung.SetAttribute("Type", "N");

            XmlElement comment = project.CreateElement("Comment");
            comment.AppendChild(project.CreateCDataSection(
                $"Module Type: {catalogNumber} Net Name: {netName} Node Name: {nodeName} Slot: {moduleSlot} DIGITAL INPUT"
            ));
            newRung.AppendChild(comment);

            XmlElement text = project.CreateElement("Text");
            text.AppendChild(project.CreateCDataSection(
                $"[GSV(Module,{moduleName},EntryStatus,IOFaultStatus) MVM(IOFaultStatus,61440,IOFaultStatusMask) ,NEQ(IOFaultStatusMask,16384) TON({moduleName}_Timer,?,?) ,XIO({moduleName}_Timer.DN) OTE({moduleName}_STATUS) ,XIC({moduleName}_TIMER.DN) FLL(0,{moduleSlot},1) ];"
            ));
            newRung.AppendChild(text);

            routineNode.AppendChild(newRung);
            rungNumber++;
        }
    }

    private void AddDigitalOutputModules(RockwellL5XProject project, XmlNode tagsNode, XmlNode routineNode, List<DigitalOutputModule> modules, ref int rungNumber, Func<string> generateIoStatusIndex)
    {
        int index = 1; // Start index at 1
        foreach (var module in modules)
        {
            string moduleName = module.Name;
            string catalogNumber = module.ModuleName;
            string netName = module.NetworkModule;
            string nodeName = module.NodeModule;
            string moduleSlot = module.Slot_No;

            if (string.IsNullOrEmpty(moduleName))
            {
                moduleName = $"{nodeName}_{index:D2}";
                index++;
            }

            string ioStatusIndex = generateIoStatusIndex();

            // Create new Tags for each module
            XmlElement statusTag = project.CreateElement("Tag");
            statusTag.SetAttribute("Name", $"{moduleName}_STATUS");
            statusTag.SetAttribute("TagType", "Alias");
            statusTag.SetAttribute("Radix", "Decimal");
            statusTag.SetAttribute("AliasFor", $"HMI_IoStatus{ioStatusIndex}");
            HMI_IoStatus_Node.Add($"HMI_IoStatus{ioStatusIndex}");
            statusTag.SetAttribute("Usage", "Public");
            statusTag.SetAttribute("ExternalAccess", "Read/Write");

            XmlElement statusDescription = project.CreateElement("Description");
            statusDescription.AppendChild(project.CreateCDataSection("Module Status OK"));
            statusTag.AppendChild(statusDescription);
            tagsNode.AppendChild(statusTag);

            XmlElement timerTag = project.CreateElement("Tag");
            timerTag.SetAttribute("Name", $"{moduleName}_Timer");
            timerTag.SetAttribute("TagType", "Base");
            timerTag.SetAttribute("DataType", "TIMER");
            timerTag.SetAttribute("Constant", "false");
            timerTag.SetAttribute("ExternalAccess", "Read/Write");

            XmlElement timerDescription = project.CreateElement("Description");
            timerDescription.AppendChild(project.CreateCDataSection($"Delay For fault On card {moduleName}"));
            timerTag.AppendChild(timerDescription);

            XmlElement timerData = project.CreateElement("Data");
            timerData.SetAttribute("Format", "Decorated");

            XmlElement timerStructure = project.CreateElement("Structure");
            timerStructure.SetAttribute("DataType", "TIMER");

            XmlElement timerPRE = project.CreateElement("DataValueMember");
            timerPRE.SetAttribute("Name", "PRE");
            timerPRE.SetAttribute("DataType", "DINT");
            timerPRE.SetAttribute("Radix", "Decimal");
            timerPRE.SetAttribute("Value", "1000");

            XmlElement timerACC = project.CreateElement("DataValueMember");
            timerACC.SetAttribute("Name", "ACC");
            timerACC.SetAttribute("DataType", "DINT");
            timerACC.SetAttribute("Radix", "Decimal");
            timerACC.SetAttribute("Value", "0");

            XmlElement timerEN = project.CreateElement("DataValueMember");
            timerEN.SetAttribute("Name", "EN");
            timerEN.SetAttribute("DataType", "BOOL");
            timerEN.SetAttribute("Value", "0");

            XmlElement timerTT = project.CreateElement("DataValueMember");
            timerTT.SetAttribute("Name", "TT");
            timerTT.SetAttribute("DataType", "BOOL");
            timerTT.SetAttribute("Value", "0");

            XmlElement timerDN = project.CreateElement("DataValueMember");
            timerDN.SetAttribute("Name", "DN");
            timerDN.SetAttribute("DataType", "BOOL");
            timerDN.SetAttribute("Value", "0");

            timerStructure.AppendChild(timerPRE);
            timerStructure.AppendChild(timerACC);
            timerStructure.AppendChild(timerEN);
            timerStructure.AppendChild(timerTT);
            timerStructure.AppendChild(timerDN);

            timerData.AppendChild(timerStructure);
            timerTag.AppendChild(timerData);
            tagsNode.AppendChild(timerTag);

            // Create new Rung for DigitalOutputModules
            XmlElement newRung = project.CreateElement("Rung");
            newRung.SetAttribute("Number", $"{rungNumber}b");
            newRung.SetAttribute("Type", "N");

            XmlElement comment = project.CreateElement("Comment");
            comment.AppendChild(project.CreateCDataSection(
                $"Module Type: {catalogNumber} Net Name: {netName} Node Name: {nodeName} Slot: {moduleSlot} DIGITAL OUTPUT"
            ));
            newRung.AppendChild(comment);

            XmlElement text = project.CreateElement("Text");
            text.AppendChild(project.CreateCDataSection(
                $"[GSV(Module,{moduleName},EntryStatus,IOFaultStatus) MVM(IOFaultStatus,61440,IOFaultStatusMask) ,EQU(IOFaultStatusMask,16384) OTE({moduleName}_STATUS) ];"
            ));
            newRung.AppendChild(text);

            routineNode.AppendChild(newRung);
            rungNumber++;
        }
    }

    // Helper methods to create XML nodes
    private XmlNode CreateTagNode(string name, string tagType, string aliasFor, string usage, string access, string description)
    {
        XmlDocument doc = new XmlDocument();
        XmlNode tagNode = doc.CreateElement("Tag");
        tagNode.Attributes.Append(CreateAttribute(doc, "Name", name));
        tagNode.Attributes.Append(CreateAttribute(doc, "TagType", tagType));
        tagNode.Attributes.Append(CreateAttribute(doc, "DataType", "TIMER"));
        tagNode.Attributes.Append(CreateAttribute(doc, "Radix", "Decimal"));
        tagNode.Attributes.Append(CreateAttribute(doc, "Constant", "false"));
        tagNode.Attributes.Append(CreateAttribute(doc, "ExternalAccess", access));

        XmlNode dataNode = doc.CreateElement("Data");
        XmlNode dataValueNode = doc.CreateElement("DataValue");
        dataValueNode.Attributes.Append(CreateAttribute(doc, "DataType", "DINT"));
        dataValueNode.Attributes.Append(CreateAttribute(doc, "Radix", "Decimal"));
        dataValueNode.InnerText = "0";
        dataNode.AppendChild(dataValueNode);
        tagNode.AppendChild(dataNode);

        if (!string.IsNullOrEmpty(description))
        {
            XmlNode descriptionNode = doc.CreateElement("Description");
            descriptionNode.InnerXml = $"<![CDATA[{description}]]>";
            tagNode.AppendChild(descriptionNode);
        }

        return tagNode;
    }

    private XmlNode CreateRungNode(int number, string type, string commentText, string text)
    {
        XmlDocument doc = new XmlDocument();
        XmlNode rungNode = doc.CreateElement("Rung");
        rungNode.Attributes.Append(CreateAttribute(doc, "Number", number.ToString()));
        rungNode.Attributes.Append(CreateAttribute(doc, "Type", type));

        XmlNode commentNode = doc.CreateElement("Comment");
        commentNode.InnerXml = $"<![CDATA[{commentText}]]>";
        rungNode.AppendChild(commentNode);

        XmlNode textNode = doc.CreateElement("Text");
        textNode.InnerXml = $"<![CDATA[{text}]]>";
        rungNode.AppendChild(textNode);

        return rungNode;
    }

    private XmlAttribute CreateAttribute(XmlDocument doc, string name, string value)
    {
        XmlAttribute attr = doc.CreateAttribute(name);
        attr.Value = value;
        return attr;
    }

    private void MethodToExtractAnalogInputOutputModule(RockwellL5XProject project, string programName, IProgress<string> progress)
    {
        var analogInputModules = project.SelectNodes("//Module")
            .Cast<XmlNode>()
            .Where(module =>
            {
                string catalogNumber = module.Attributes["CatalogNumber"]?.Value;
                return catalogNumber != null && (
                    catalogNumber.Contains("1756-IF") ||
                    catalogNumber.Contains("1756-IR") ||
                    catalogNumber.Contains("1756-IT") ||
                    catalogNumber.Contains("1715-IF") ||
                    catalogNumber.Contains("1734-IE") ||
                    catalogNumber.Contains("1734-IR") ||
                    catalogNumber.Contains("1734-IT") ||
                    catalogNumber.Contains("1738-IE") ||
                    catalogNumber.Contains("1738-IR") ||
                    catalogNumber.Contains("1738-IT") ||
                    catalogNumber.Contains("1746-IN") ||
                    catalogNumber.Contains("1746-NI") ||
                    catalogNumber.Contains("1746-NR") ||
                    catalogNumber.Contains("1746-NT") ||
                    catalogNumber.Contains("1769-IF") ||
                    catalogNumber.Contains("1769-IR") ||
                    catalogNumber.Contains("1769-IT") ||
                    catalogNumber.Contains("1794-IE") ||
                    catalogNumber.Contains("1794-IR") ||
                    catalogNumber.Contains("1794-IT") ||
                    catalogNumber.Contains("1797-IE") ||
                    catalogNumber.Contains("1797-IR"));
            })
            .Select(module =>
            {
                // Capture required details including Address from Ports
                var address = module.SelectSingleNode("Ports/Port")?.Attributes["Address"]?.Value;

                return new AnalogInputModule
                {
                    Name = module.Attributes["Name"]?.Value,
                    ModuleName = module.Attributes["CatalogNumber"]?.Value,
                    NodeModule = module.Attributes["ParentModule"]?.Value,
                    NetworkModule = FindNetName(module.Attributes["ParentModule"]?.Value),
                    Slot_No = address
                };
            })
            .ToList();

        // Add the identified AnalogInput modules to the list
        AnalogInputModules.AddRange(analogInputModules);        

        var analogOutputModules = project.SelectNodes("//Module")
            .Cast<XmlNode>()
            .Where(module =>
            {
                string catalogNumber = module.Attributes["CatalogNumber"]?.Value;
                return catalogNumber != null && (
                    catalogNumber.Contains("1756-OF") ||
                    catalogNumber.Contains("1715-OF") ||
                    catalogNumber.Contains("1734-OE") ||
                    catalogNumber.Contains("1738-OE") ||
                    catalogNumber.Contains("1746-NO") ||
                    catalogNumber.Contains("1769-OF") ||
                    catalogNumber.Contains("1794-OE") ||
                    catalogNumber.Contains("1794-OF") ||
                    catalogNumber.Contains("1797-OE"));
                    
            })
            .Select(module =>
            {
                // Capture required details including Address from Ports
                var address = module.SelectSingleNode("Ports/Port")?.Attributes["Address"]?.Value;

                return new AnalogOutputModule
                {
                    Name = module.Attributes["Name"]?.Value,
                    ModuleName = module.Attributes["CatalogNumber"]?.Value,
                    NodeModule = module.Attributes["ParentModule"]?.Value,
                    NetworkModule = FindNetName(module.Attributes["ParentModule"]?.Value),
                    Slot_No = address
                };
            })
            .ToList();

        // Add the identified AnalogInput modules to the list
        AnalogOutputModules.AddRange(analogOutputModules);

        UpdateDiagAnalogProgram(Project, AnalogInputModules, AnalogOutputModules);

    }

    private void UpdateDiagAnalogProgram(RockwellL5XProject project, List<AnalogInputModule> analogInputModules, List<AnalogOutputModule> analogOutputModules)
    {
        var programNode = project.SelectSingleNode("//Program[@Name='DiagAnalog']");
        if (programNode == null) return;

        var tagsNode = programNode.SelectSingleNode("Tags");
        var routineNode = programNode.SelectSingleNode("Routines/Routine[@Name='DiagAnalog']/RLLContent");

        if (tagsNode == null || routineNode == null) return;

        // Remove existing placeholders for ##ModuleName##_STATUS and ##ModuleName##_Timer tags
        var existingStatusTags = tagsNode.SelectNodes("Tag[contains(@Name, '_STATUS')]");
        foreach (XmlNode tag in existingStatusTags)
        {
            tagsNode.RemoveChild(tag);
        }

        var existingTimerTags = tagsNode.SelectNodes("Tag[contains(@Name, '_Timer')]");
        foreach (XmlNode tag in existingTimerTags)
        {
            tagsNode.RemoveChild(tag);
        }

        // Remove specific Rung 3a and 3b
        var rung3a = routineNode.SelectSingleNode("Rung[@Number='3a']");
        if (rung3a != null)
        {
            routineNode.RemoveChild(rung3a);
        }

        var rung3b = routineNode.SelectSingleNode("Rung[@Number='3b']");
        if (rung3b != null)
        {
            routineNode.RemoveChild(rung3b);
        }

        int rungNumber = 3; // Start numbering for new rungs

        // Helper method to generate IoStatusIndex
        string GenerateIoStatusIndex()
        {
            if (minorIndex >= 16)
            {
                minorIndex = 0;
                majorIndex++;
            }
            string index = $"[{majorIndex:D2}].{minorIndex:D2}";
            minorIndex++;
            return index;
        }

        AddAnalogInputModules(project, tagsNode, routineNode, analogInputModules, ref rungNumber, GenerateIoStatusIndex);
        AddAnalogOutputModules(project, tagsNode, routineNode, analogOutputModules, ref rungNumber, GenerateIoStatusIndex);
    }

    private void AddAnalogInputModules(RockwellL5XProject project, XmlNode tagsNode, XmlNode routineNode, List<AnalogInputModule> modules, ref int rungNumber, Func<string> generateIoStatusIndex)
    {
        int index = 0; // Start index at 0
        foreach (var module in modules)
        {
            string moduleName = module.Name;
            string catalogNumber = module.ModuleName;
            string netName = module.NetworkModule;
            string nodeName = module.NodeModule;
            string moduleSlot = module.Slot_No;

            if (string.IsNullOrEmpty(moduleName))
            {
                moduleName = $"{nodeName}_{index:D2}";
                index++;
            }

            string ioStatusIndex = generateIoStatusIndex();

            // Create new Tags for each module
            XmlElement statusTag = project.CreateElement("Tag");
            statusTag.SetAttribute("Name", $"{moduleName}_STATUS");
            statusTag.SetAttribute("TagType", "Alias");
            statusTag.SetAttribute("Radix", "Decimal");
            statusTag.SetAttribute("AliasFor", $"HMI_IoStatus{ioStatusIndex}");
            HMI_IoStatus_Node.Add($"HMI_IoStatus{ioStatusIndex}");
            statusTag.SetAttribute("Usage", "Public");
            statusTag.SetAttribute("ExternalAccess", "Read/Write");

            XmlElement statusDescription = project.CreateElement("Description");
            statusDescription.AppendChild(project.CreateCDataSection("Module Status OK"));
            statusTag.AppendChild(statusDescription);
            tagsNode.AppendChild(statusTag);

            XmlElement timerTag = project.CreateElement("Tag");
            timerTag.SetAttribute("Name", $"{moduleName}_Timer");
            timerTag.SetAttribute("TagType", "Base");
            timerTag.SetAttribute("DataType", "TIMER");
            timerTag.SetAttribute("Constant", "false");
            timerTag.SetAttribute("ExternalAccess", "Read/Write");

            XmlElement timerDescription = project.CreateElement("Description");
            timerDescription.AppendChild(project.CreateCDataSection($"Delay For fault On card {moduleName}"));
            timerTag.AppendChild(timerDescription);

            XmlElement timerData = project.CreateElement("Data");
            timerData.SetAttribute("Format", "Decorated");

            XmlElement timerStructure = project.CreateElement("Structure");
            timerStructure.SetAttribute("DataType", "TIMER");

            XmlElement timerPRE = project.CreateElement("DataValueMember");
            timerPRE.SetAttribute("Name", "PRE");
            timerPRE.SetAttribute("DataType", "DINT");
            timerPRE.SetAttribute("Radix", "Decimal");
            timerPRE.SetAttribute("Value", "1000");

            XmlElement timerACC = project.CreateElement("DataValueMember");
            timerACC.SetAttribute("Name", "ACC");
            timerACC.SetAttribute("DataType", "DINT");
            timerACC.SetAttribute("Radix", "Decimal");
            timerACC.SetAttribute("Value", "0");

            XmlElement timerEN = project.CreateElement("DataValueMember");
            timerEN.SetAttribute("Name", "EN");
            timerEN.SetAttribute("DataType", "BOOL");
            timerEN.SetAttribute("Value", "0");

            XmlElement timerTT = project.CreateElement("DataValueMember");
            timerTT.SetAttribute("Name", "TT");
            timerTT.SetAttribute("DataType", "BOOL");
            timerTT.SetAttribute("Value", "0");

            XmlElement timerDN = project.CreateElement("DataValueMember");
            timerDN.SetAttribute("Name", "DN");
            timerDN.SetAttribute("DataType", "BOOL");
            timerDN.SetAttribute("Value", "0");

            timerStructure.AppendChild(timerPRE);
            timerStructure.AppendChild(timerACC);
            timerStructure.AppendChild(timerEN);
            timerStructure.AppendChild(timerTT);
            timerStructure.AppendChild(timerDN);

            timerData.AppendChild(timerStructure);
            timerTag.AppendChild(timerData);
            tagsNode.AppendChild(timerTag);

            // Create new Rung for AnalogInputModules
            XmlElement newRung = project.CreateElement("Rung");
            newRung.SetAttribute("Number", $"{rungNumber}a");
            newRung.SetAttribute("Type", "N");

            XmlElement comment = project.CreateElement("Comment");
            comment.AppendChild(project.CreateCDataSection(
                $"Module Type: {catalogNumber} Net Name: {netName} Node Name: {nodeName} Slot: {moduleSlot} ANALOG INPUT"
            ));
            newRung.AppendChild(comment);

            XmlElement text = project.CreateElement("Text");
            text.AppendChild(project.CreateCDataSection(
               $"[GSV(Module,{moduleName},EntryStatus,IOFaultStatus) MVM(IOFaultStatus,61440,IOFaultStatusMask) ,NEQ(IOFaultStatusMask,16384) TON({moduleName}_Timer,?,?) ,XIO({moduleName}_Timer.DN) OTE({moduleName}_STATUS) ,XIC({moduleName}_TIMER.DN) FLL(0,{moduleSlot},1) ];"
            ));
            newRung.AppendChild(text);

            routineNode.AppendChild(newRung);
            rungNumber++;
        }
    }

    private void AddAnalogOutputModules(RockwellL5XProject project, XmlNode tagsNode, XmlNode routineNode, List<AnalogOutputModule> modules, ref int rungNumber, Func<string> generateIoStatusIndex)
    {
        int index = 1; // Start index at 1
        foreach (var module in modules)
        {
            string moduleName = module.Name;
            string catalogNumber = module.ModuleName;
            string netName = module.NetworkModule;
            string nodeName = module.NodeModule;
            string moduleSlot = module.Slot_No;

            if (string.IsNullOrEmpty(moduleName))
            {
                moduleName = $"{nodeName}_{index:D2}";
                index++;
            }

            string ioStatusIndex = generateIoStatusIndex();

            // Create new Tags for each module
            XmlElement statusTag = project.CreateElement("Tag");
            statusTag.SetAttribute("Name", $"{moduleName}_STATUS");
            statusTag.SetAttribute("TagType", "Alias");
            statusTag.SetAttribute("Radix", "Decimal");
            statusTag.SetAttribute("AliasFor", $"HMI_IoStatus{ioStatusIndex}");
            HMI_IoStatus_Node.Add($"HMI_IoStatus{ioStatusIndex}");
            statusTag.SetAttribute("Usage", "Public");
            statusTag.SetAttribute("ExternalAccess", "Read/Write");

            XmlElement statusDescription = project.CreateElement("Description");
            statusDescription.AppendChild(project.CreateCDataSection("Module Status OK"));
            statusTag.AppendChild(statusDescription);
            tagsNode.AppendChild(statusTag);

            XmlElement timerTag = project.CreateElement("Tag");
            timerTag.SetAttribute("Name", $"{moduleName}_Timer");
            timerTag.SetAttribute("TagType", "Base");
            timerTag.SetAttribute("DataType", "TIMER");
            timerTag.SetAttribute("Constant", "false");
            timerTag.SetAttribute("ExternalAccess", "Read/Write");

            XmlElement timerDescription = project.CreateElement("Description");
            timerDescription.AppendChild(project.CreateCDataSection($"Delay For fault On card {moduleName}"));
            timerTag.AppendChild(timerDescription);

            XmlElement timerData = project.CreateElement("Data");
            timerData.SetAttribute("Format", "Decorated");

            XmlElement timerStructure = project.CreateElement("Structure");
            timerStructure.SetAttribute("DataType", "TIMER");

            XmlElement timerPRE = project.CreateElement("DataValueMember");
            timerPRE.SetAttribute("Name", "PRE");
            timerPRE.SetAttribute("DataType", "DINT");
            timerPRE.SetAttribute("Radix", "Decimal");
            timerPRE.SetAttribute("Value", "1000");

            XmlElement timerACC = project.CreateElement("DataValueMember");
            timerACC.SetAttribute("Name", "ACC");
            timerACC.SetAttribute("DataType", "DINT");
            timerACC.SetAttribute("Radix", "Decimal");
            timerACC.SetAttribute("Value", "0");

            XmlElement timerEN = project.CreateElement("DataValueMember");
            timerEN.SetAttribute("Name", "EN");
            timerEN.SetAttribute("DataType", "BOOL");
            timerEN.SetAttribute("Value", "0");

            XmlElement timerTT = project.CreateElement("DataValueMember");
            timerTT.SetAttribute("Name", "TT");
            timerTT.SetAttribute("DataType", "BOOL");
            timerTT.SetAttribute("Value", "0");

            XmlElement timerDN = project.CreateElement("DataValueMember");
            timerDN.SetAttribute("Name", "DN");
            timerDN.SetAttribute("DataType", "BOOL");
            timerDN.SetAttribute("Value", "0");

            timerStructure.AppendChild(timerPRE);
            timerStructure.AppendChild(timerACC);
            timerStructure.AppendChild(timerEN);
            timerStructure.AppendChild(timerTT);
            timerStructure.AppendChild(timerDN);

            timerData.AppendChild(timerStructure);
            timerTag.AppendChild(timerData);
            tagsNode.AppendChild(timerTag);

            // Create new Rung for AnalogOutputModules
            XmlElement newRung = project.CreateElement("Rung");
            newRung.SetAttribute("Number", $"{rungNumber}b");
            newRung.SetAttribute("Type", "N");

            XmlElement comment = project.CreateElement("Comment");
            comment.AppendChild(project.CreateCDataSection(
                $"Module Type: {catalogNumber} Net Name: {netName} Node Name: {nodeName} Slot: {moduleSlot} ANALOG OUTPUT"
            ));
            newRung.AppendChild(comment);

            XmlElement text = project.CreateElement("Text");
            text.AppendChild(project.CreateCDataSection(
                $"[GSV(Module,{moduleName},EntryStatus,IOFaultStatus) MVM(IOFaultStatus,61440,IOFaultStatusMask) ,EQU(IOFaultStatusMask,16384) OTE({moduleName}_STATUS) ];"
            ));
            newRung.AppendChild(text);

            routineNode.AppendChild(newRung);
            rungNumber++;
        }
    }


    public string FindNetName(string nodeName)
    {
        // Find the module where Name matches the given NodeModuleName
        var nodeModule = Project.SelectSingleNode($"//Module[@Name='{nodeName}']");

        if (nodeModule != null)
        {
            // Retrieve the ParentModule value from the NodeModule
            string parentModule = nodeModule.Attributes["ParentModule"]?.Value;
            return parentModule;
        }

        // Return null or an empty string if no NetName is found
        return null;
    }

    private void MethodToExtractNodeModule(RockwellL5XProject project, string programName, IProgress<string> progress)
    {
        if (NetworkModules != null)
        {
            // Extract NodeModule names based on the ParentModule
            NodeModules = project.SelectNodes("//Module")
                .Cast<XmlNode>()
                .Where(module => NetworkModules.Any(nm => module.Attributes["ParentModule"]?.Value.Contains(nm) == true))
                .Select(module => module.Attributes["Name"]?.Value)
                .ToList();

            var programNode = project.SelectSingleNode($"//Program[@Name='{programName}']");
            if (programNode != null)
            {
                var routine = programNode.SelectSingleNode("Routines/Routine");
                if (routine != null)
                {
                    var rllContent = routine.SelectSingleNode("RLLContent");
                    if (rllContent != null)
                    {
                        // Remove existing Rung No. 2
                        var rungNode = rllContent.SelectSingleNode("Rung[@Number='2']");
                        if (rungNode != null)
                        {
                            rllContent.RemoveChild(rungNode);
                        }

                        int newRungNumber = 2; // Start numbering from 3
                       

                        foreach (var netModuleName in NetworkModules)
                        {
                            var nodeModulesForNet = project.SelectNodes("//Module")
                            .Cast<XmlNode>()
                            .Where(module => module.Attributes["ParentModule"]?.Value == netModuleName)
                            .Select(module => module.Attributes["Name"]?.Value)
                            .ToList();

                            foreach (var nodeModuleName in nodeModulesForNet)
                            {
                                // Create new Rung elements based on the count of net modules
                                string hwDiagNoStr = hwDiagIndex.ToString("D3"); // Format to 3 digits

                                XmlElement newRung = project.CreateElement("Rung");
                                newRung.SetAttribute("Number", newRungNumber.ToString());
                                newRung.SetAttribute("Type", "N");

                                // Create Comment element
                                XmlElement newComment = project.CreateElement("Comment");
                                XmlCDataSection commentCData = project.CreateCDataSection($"Net Name: {netModuleName} Node Name: {nodeModuleName}");
                                newComment.AppendChild(commentCData);
                                newRung.AppendChild(newComment);

                                // Create Text element
                                XmlElement newText = project.CreateElement("Text");
                                XmlCDataSection textCData = project.CreateCDataSection(
                                    $"GSV(Module,{nodeModuleName},EntryStatus,IOFaultStatus)MVM(IOFaultStatus,61440,HMI_HWDIAG[{hwDiagNoStr}]);"
                                );
                                newText.AppendChild(textCData);
                                newRung.AppendChild(newText);
                                HMI_HWDIAG_Node.Add($"HMI_HWDIAG[{hwDiagNoStr}]");
                                rllContent.AppendChild(newRung);
                                newRungNumber++; // Increment Rung number for the next entry
                                hwDiagIndex++; // Increment hardware diagnostic number for the next entry                                
                            }
                        }

                        // Replace the existing RLLContent with the new one
                        var oldRLLContent = routine.SelectSingleNode("RLLContent");
                        if (oldRLLContent != null)
                        {
                            routine.ReplaceChild(rllContent, oldRLLContent);
                        }
                        else
                        {
                            routine.AppendChild(rllContent);
                        }

                        var programsToModify = Project.Content?.Controller?.Programs;
                        XmlNode hwDiagProgram = CreateHWDiagProgram(xmlDocNew, progress);
                        programsToModify?.AppendChild(programsToModify.OwnerDocument.ImportNode(hwDiagProgram, true));
                    }
                }
            }
        }
    }

    public void MethodToExtractNetworkNode(RockwellL5XProject project, string programName, IProgress<string> progress)
    {
        List<string> catalogNumbers = new List<string>
    {
        "1756-EN",
        "1756-CN",
        "1756-DHRIO",
        "1756-DNB",
        "1756-RIO",
        "1788-EN2DN"
    };

        // Find the specific program node by the programName
        var programNode = project.SelectSingleNode($"//Program[@Name='{programName}']");
        if (programNode != null)
        {
            NetworkModules = docNew.Descendants("Module")
                                       .Where(module => catalogNumbers.Any(cn => module.Attribute("CatalogNumber")?.Value.Contains(cn) == true) &&
                                        module.Attribute("ParentModule")?.Value == "Local")
                                        .Select(module => module.Attribute("Name")?.Value)
                                        .ToList();

            if (NetworkModules.Count > 0)
            {
                var routine = programNode.SelectSingleNode("Routines/Routine");
                if (routine != null)
                {
                    var rllContent = routine.SelectSingleNode("RLLContent");
                    if (rllContent != null)
                    {
                        // Remove existing Rung No. 2
                        var rungNode = rllContent.SelectSingleNode("Rung[@Number='2']");
                        if (rungNode != null)
                        {
                            rllContent.RemoveChild(rungNode);
                        }

                        int newRungNumber = 2; // Start numbering from 3
                                               // Start HW DIAG index from 001

                        foreach (var moduleName in NetworkModules)
                        {
                            // Format the HW DIAG index to be three digits
                            string hwDiagIndexFormatted = hwDiagIndex.ToString("D3");

                            // Create new Rung elements based on the count of module names
                            XmlElement newRung = project.CreateElement("Rung");
                            newRung.SetAttribute("Number", newRungNumber.ToString());
                            newRung.SetAttribute("Type", "N");

                            // Create Comment element
                            XmlElement newComment = project.CreateElement("Comment");
                            XmlCDataSection commentCData = project.CreateCDataSection($"Net Name: {moduleName}");
                            progress.Report($"{moduleName} has been added to DiagNet Program");
                            newComment.AppendChild(commentCData);
                            newRung.AppendChild(newComment);

                            // Create Text element
                            XmlElement newText = project.CreateElement("Text");
                            XmlCDataSection textCData = project.CreateCDataSection(
                                $"GSV(Module,{moduleName},EntryStatus,IOFaultStatus)MVM(IOFaultStatus,61440,HMI_HWDIAG[{hwDiagIndexFormatted}]);"
                            );
                            newText.AppendChild(textCData);
                            newRung.AppendChild(newText);

                            rllContent.AppendChild(newRung);
                            HMI_HWDIAG_NET.Add($"HMI_HWDIAG[{ hwDiagIndexFormatted}]");
                            newRungNumber++; // Increment Rung number for the next entry
                            hwDiagIndex++; // Increment HW DIAG index for the next entry
                        }                        

                        // Replace the existing RLLContent with the new one
                        var oldRLLContent = routine.SelectSingleNode("RLLContent");
                        if (oldRLLContent != null)
                        {
                            routine.ReplaceChild(rllContent, oldRLLContent);
                        }
                        else
                        {
                            routine.AppendChild(rllContent);
                        }
                    }
                }                

            }
            else
            {
                var parentNode = programNode;
                var DiagProgNode = project.SelectSingleNode($"//Program[@Name='DiagNode']");
                var DiagAnalogNode = project.SelectSingleNode($"//Program[@Name='DiagAnalog']");
                var DiagDigitalNode = project.SelectSingleNode($"//Program[@Name='DiagDigital']");
                if (parentNode != null)
                {
                    _ = Project.Content?.Controller?.Programs!.RemoveChild(programNode);
                    _ = Project.Content?.Controller?.Programs!.RemoveChild(DiagAnalogNode);
                    _ = Project.Content?.Controller?.Programs!.RemoveChild(DiagDigitalNode);
                    _ = Project.Content?.Controller?.Programs!.RemoveChild(DiagProgNode);

                }

                var programsToModify = Project.Content?.Controller?.Programs;
                XmlNode hwDiagProgram = CreateHWDiagProgram(xmlDocNew, progress);
                if (hwDiagProgram != null)
                {
                    programsToModify?.AppendChild(programsToModify.OwnerDocument.ImportNode(hwDiagProgram, true));
                }               

            }

        }
    }

    public void MethodToExtractCPUSlotStatusHMIIndexControllerName(IProgress<string> progress)
    {
        string processorType = Project.GetProcessorType();
        string ControllerSlotNo = GetAddressFromModule(processorType);
        string DiagPLC_ProgramName = "DiagPLC";
        string controllerName = docNew.Root.Attribute("TargetName")?.Value;
        ControllerNameGlobal = controllerName;

        HMI_Max_Unit_Index = GetMaxHmiUnitIndex();
        CPU_Index_HMI = HMI_Max_Unit_Index;
        CreateAppendControllerNameFaceplateTag(controllerName);

        Project.UpdateCPUSlotInRLLContent(DiagPLC_ProgramName, ControllerSlotNo);
        Project.UpdateHmiUnitIndicesControllerName(Project, DiagPLC_ProgramName, HMI_Max_Unit_Index, controllerName);
        progress.Report("DiagPLC program is created");
    }

    public string GetAddressFromModule(string processorType)
    {
        XmlNode moduleNode = Project.SelectSingleNode($"/RSLogix5000Content/Controller/Modules/Module[@CatalogNumber='{processorType}']");
        if (moduleNode != null)
        {
            XmlNode portNode = moduleNode.SelectSingleNode("Ports/Port");
            if (portNode != null)
            {
                XmlAttribute addressAttr = portNode.Attributes["Address"];
                return addressAttr?.Value;
            }
        }
        return null;
    }

    private int GetMaxHmiUnitIndex()
    {
        string textContent = "";
        int MaxIndex = -1; // Initialize to -1 to indicate no HMI_UNIT found
        if (Project.Content?.Controller?.Programs != null)
        {
            foreach (L5XProgram program in Project.Content.Controller.Programs)
            {
                foreach (var routine in program.Routines)
                {
                    foreach (XmlNode rung in routine.SelectNodes("RLLContent/Rung"))
                    {
                        XmlNode? textNode = rung.SelectSingleNode("Text");
                        if (textNode != null)
                        {
                            textContent = textNode.InnerText;
                            int currentMaxIndex = CalculateMaxHMIUnitIndex(textContent);
                            if (currentMaxIndex > MaxIndex)
                            {
                                MaxIndex = currentMaxIndex;
                            }
                        }
                    }
                }
            }
        }
        return MaxIndex + 1;
    }

    public int CalculateMaxHMIUnitIndex(string textContent)
    {
        Regex regex = new Regex(@"HMI_UNIT\[(\d+)\]");
        MatchCollection matches = regex.Matches(textContent);
        int maxIndex = -1;

        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out int index))
            {
                if (index > maxIndex)
                {
                    maxIndex = index;
                }
            }
        }

        return maxIndex;
    }

    public int CalculateMaxHMIGroupIndex(string textContent)
    {
        Regex regex = new Regex(@"HMI_GROUP\[(\d+)\]");
        MatchCollection matches = regex.Matches(textContent);
        int maxIndex = -1;

        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out int index))
            {
                if (index > maxIndex)
                {
                    maxIndex = index;
                }
            }
        }

        return maxIndex;
    }

    public void CreateFPInstance_Rx_FpTagNode(string fbInstance, IProgress<string> progress)
    {
        XmlElement tagNode = xmlDocNew.CreateElement("Tag");
        tagNode.SetAttribute("Name", $"{fbInstance}_Rx_FP");
        tagNode.SetAttribute("TagType", "Base");
        tagNode.SetAttribute("DataType", "ACESYS_FACEPLATE_RECIEVE");
        tagNode.SetAttribute("Constant", "false");
        tagNode.SetAttribute("ExternalAccess", "Read/Write");

        XmlElement descriptionNode = xmlDocNew.CreateElement("Description");
        descriptionNode.AppendChild(xmlDocNew.CreateCDataSection("Recive Data From PLC, Faceplate"));
        tagNode.AppendChild(descriptionNode);

        XmlElement dataNode = xmlDocNew.CreateElement("Data");
        dataNode.SetAttribute("Format", "Decorated");
        tagNode.AppendChild(dataNode);

        XmlElement structureNode = xmlDocNew.CreateElement("Structure");
        structureNode.SetAttribute("DataType", "ACESYS_FACEPLATE_RECIEVE");
        dataNode.AppendChild(structureNode);

        AppendDataValueMember(xmlDocNew, structureNode, "ACESYS_Version", "DINT", "Decimal", "0");
        AppendDataValueMember(xmlDocNew, structureNode, "ACESYS_Build", "DINT", "Decimal", "0");
        AppendDataValueMember(xmlDocNew, structureNode, "Faceplate", "DINT", "Decimal", "0");
        AppendDataValueMember(xmlDocNew, structureNode, "WATCHDOG_PRE", "INT", "Decimal", "15");
        AppendDataValueMember(xmlDocNew, structureNode, "WATCHDOG_ACC", "INT", "Decimal", "0");

        XmlElement structureMemberNode = xmlDocNew.CreateElement("StructureMember");
        structureMemberNode.SetAttribute("Name", "DATA");
        structureMemberNode.SetAttribute("DataType", "ACESYS_PLCtoPLC");
        structureNode.AppendChild(structureMemberNode);

        AppendDataValueMember(xmlDocNew, structureMemberNode, "WatchDog", "BOOL", "", "0");
        AppendArrayMember(xmlDocNew, structureMemberNode, "Bools", "BOOL", "64", "Decimal", 64);
        AppendArrayMember(xmlDocNew, structureMemberNode, "Integers", "INT", "16", "Decimal", 16);
        AppendArrayMember(xmlDocNew, structureMemberNode, "Reals", "REAL", "16", "Float", 16);

        AppendDataValueMember(xmlDocNew, structureNode, "SIM_Enable", "BOOL", "", "0");
        AppendDataValueMember(xmlDocNew, structureNode, "Clear_Bool", "BOOL", "", "0");
        AppendDataValueMember(xmlDocNew, structureNode, "Clear_Int", "BOOL", "", "0");
        AppendDataValueMember(xmlDocNew, structureNode, "Clear_Real", "BOOL", "", "0");

        AppendArrayMember(xmlDocNew, structureNode, "DATA_Bools1", "BOOL", "32", "Decimal", 32);

        XmlNode importedNode = Project.Content.Controller.Tags.OwnerDocument.ImportNode(tagNode, true);

        Project.Content?.Controller?.Tags.AppendChild(importedNode);

        progress.Report($"Tag is created named as {fbInstance}_Rx_FP");
        
    }

    private void AppendDataValueMember(XmlDocument xmlDoc, XmlElement parent, string name, string dataType, string radix, string value)
    {
        XmlElement dataValueMemberNode = xmlDoc.CreateElement("DataValueMember");
        dataValueMemberNode.SetAttribute("Name", name);
        dataValueMemberNode.SetAttribute("DataType", dataType);
        if (!string.IsNullOrEmpty(radix))
        {
            dataValueMemberNode.SetAttribute("Radix", radix);
        }
        dataValueMemberNode.SetAttribute("Value", value);
        parent.AppendChild(dataValueMemberNode);
    }

    private void AppendArrayMember(XmlDocument xmlDoc, XmlElement parent, string name, string dataType, string dimensions, string radix, int elementCount)
    {
        XmlElement arrayMemberNode = xmlDoc.CreateElement("ArrayMember");
        arrayMemberNode.SetAttribute("Name", name);
        arrayMemberNode.SetAttribute("DataType", dataType);
        arrayMemberNode.SetAttribute("Dimensions", dimensions);
        arrayMemberNode.SetAttribute("Radix", radix);

        for (int i = 0; i < elementCount; i++)
        {
            XmlElement elementNode = xmlDoc.CreateElement("Element");
            elementNode.SetAttribute("Index", $"[{i}]");
            elementNode.SetAttribute("Value", dataType == "REAL" ? "0.0" : "0");
            arrayMemberNode.AppendChild(elementNode);
        }
        parent.AppendChild(arrayMemberNode);
    }

    private void CreateAppendControllerNameFaceplateTag(string controllerName)
    {
        // Create a new XmlDocument
        XmlDocument xmlDoc = new XmlDocument();

        // Create Tag node
        XmlNode tagNode = xmlDoc.CreateElement("Tag");

        // Add attributes to the Tag node
        XmlAttribute nameAttr = xmlDoc.CreateAttribute("Name");
        nameAttr.Value = $"{controllerName}_FP";
        tagNode.Attributes.Append(nameAttr);

        XmlAttribute tagTypeAttr = xmlDoc.CreateAttribute("TagType");
        tagTypeAttr.Value = "Base";
        tagNode.Attributes.Append(tagTypeAttr);

        XmlAttribute dataTypeAttr = xmlDoc.CreateAttribute("DataType");
        dataTypeAttr.Value = "ACESYS_FACEPLATE_PLCDIAGV2";
        tagNode.Attributes.Append(dataTypeAttr);

        XmlAttribute constantAttr = xmlDoc.CreateAttribute("Constant");
        constantAttr.Value = "false";
        tagNode.Attributes.Append(constantAttr);

        XmlAttribute externalAccessAttr = xmlDoc.CreateAttribute("ExternalAccess");
        externalAccessAttr.Value = "Read/Write";
        tagNode.Attributes.Append(externalAccessAttr);

        // Create Data node
        XmlNode dataNode = xmlDoc.CreateElement("Data");
        XmlAttribute formatAttr = xmlDoc.CreateAttribute("Format");
        formatAttr.Value = "Decorated";
        dataNode.Attributes.Append(formatAttr);

        // Create Structure node
        XmlNode structureNode = xmlDoc.CreateElement("Structure");
        XmlAttribute structureDataTypeAttr = xmlDoc.CreateAttribute("DataType");
        structureDataTypeAttr.Value = "ACESYS_FACEPLATE_PLCDIAGV2";
        structureNode.Attributes.Append(structureDataTypeAttr);

        // Add DataValueMember nodes to Structure node
        string[] memberNames = { "ACESYS_Version", "ACESYS_Build", "Faceplate", "PLCHeartBeat", "Battery", "CPU_OK",
                         "Type_Fault", "MajorFLT_Recoverable", "MajorFLT_UnRecoverable", "MinorFLT_Recoverable",
                         "MinorFLT_Unrecoverable", "Show_PLCHeartBeat", "Show_Battery", "Show_MajorFLT_Recoverable",
                         "Show_MajorFLT_UnRecoverable", "Show_MinorFLT_Recoverable", "Show_MinorFLT_Unrecoverable",
                         "Show_Revision", "ProductCode", "Revision", "CPUClock_Sec", "CPUClock_Min", "CPUClock_Hour",
                         "CPUClock_Day", "CPUClock_Month", "CPUClock_Year", "CPU_Red", "HSBYSwap_HMI_En", "Show_CPU_Red",
                         "Show_Scan_AnalogPreset", "Show_Scan_AnalogActual", "Show_Scan_DigitalPreset", "Show_Scan_DigitalActual",
                         "Show_Scan_DigitalAlarmPreset", "Show_Scan_DigitalAlarmActual", "Show_Scan_FastPreset",
                         "Show_Scan_FastActual", "Show_Scan_PNetPreset", "Show_Scan_PNetActual", "Scan_AnalogPreset",
                         "Scan_AnalogActual", "Scan_DigitalPreset", "Scan_DigitalActual", "Scan_DigitalAlarmPreset",
                         "Scan_DigitalAlarmActual", "Scan_FastPreset", "Scan_FastActual", "Scan_PNetPreset",
                         "Scan_PNetActual", "Show_CPU_UtilizeFree", "Show_CPU_UtilizeUsed", "FP_CMD_SwitchCPU",
                         "DimOut_FP_CMD_SwitchCPU", "RedConfigEn", "RedCPUStatus", "RedChassisStatus", "RedChassisID",
                         "RedCompatPartner", "RedPartnerMode", "RedKeySwMismatch", "RedPartnerKeySw", "HSBY_En",
                         "CPU_UtilizeUsed", "CPU_UtilizeFree", "Show_RedConfigEn" };

        string[] memberDataTypes = { "DINT", "DINT", "DINT", "BOOL", "BOOL", "BOOL", "BOOL", "BOOL", "BOOL", "BOOL", "BOOL",
                             "BOOL", "BOOL", "BOOL", "BOOL", "BOOL", "BOOL", "BOOL", "DINT", "REAL", "SINT", "SINT",
                             "SINT", "SINT", "SINT", "INT", "BOOL", "BOOL", "BOOL", "BOOL", "BOOL", "BOOL", "BOOL",
                             "BOOL", "BOOL", "BOOL", "BOOL", "REAL", "REAL", "REAL", "REAL", "REAL", "REAL", "REAL",
                             "REAL", "REAL", "REAL", "BOOL", "BOOL", "BOOL", "BOOL", "SINT", "DINT", "INT", "INT",
                             "INT", "DINT", "DINT", "DINT", "INT", "DINT", "DINT", "DINT", "BOOL" };

        string[] memberRadix = { "Decimal", "Decimal", "Decimal", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "",
                         "Decimal", "Float", "Decimal", "Decimal", "Decimal", "Decimal", "Decimal", "Decimal", "", "",
                         "", "", "", "", "", "", "", "", "", "", "", "Decimal", "Decimal", "Decimal", "Decimal", "Decimal",
                         "Decimal", "Decimal", "Decimal", "Decimal", "Decimal", "", "", "", "", "Decimal", "Decimal",
                         "Decimal", "Decimal", "Decimal", "Decimal", "Decimal", "Decimal", "Decimal", "Decimal", "", "" };

        string[] memberValues = { "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0",
                          "0", "0.0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0",
                          "0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0.0", "0", "0",
                          "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0", "0.0" };

        // Ensure all arrays have the same length
        if (memberNames.Length != memberDataTypes.Length ||
            memberNames.Length != memberRadix.Length ||
            memberNames.Length != memberValues.Length)
        {
           
        }

        for (int i = 0; i < memberValues.Length; i++)
        {
            XmlNode dataValueMemberNode = xmlDoc.CreateElement("DataValueMember");
            XmlAttribute memberNameAttr = xmlDoc.CreateAttribute("Name");
            memberNameAttr.Value = memberNames[i];
            dataValueMemberNode.Attributes.Append(memberNameAttr);

            XmlAttribute memberDataTypeAttr = xmlDoc.CreateAttribute("DataType");
            memberDataTypeAttr.Value = memberDataTypes[i];
            dataValueMemberNode.Attributes.Append(memberDataTypeAttr);

            if (!string.IsNullOrEmpty(memberRadix[i]))
            {
                XmlAttribute memberRadixAttr = xmlDoc.CreateAttribute("Radix");
                memberRadixAttr.Value = memberRadix[i];
                dataValueMemberNode.Attributes.Append(memberRadixAttr);
            }

            XmlAttribute memberValueAttr = xmlDoc.CreateAttribute("Value");
            memberValueAttr.Value = memberValues[i];
            dataValueMemberNode.Attributes.Append(memberValueAttr);

            structureNode.AppendChild(dataValueMemberNode);
        }

        dataNode.AppendChild(structureNode);
        tagNode.AppendChild(dataNode);

        // Append the tagNode to Project.Content?.Controller?.Tags
        // Assuming Project.Content?.Controller?.Tags is accessible
        if (Project.Content?.Controller?.Tags != null)
        {
            // Import the tagNode into the Project.Content?.Controller?.Tags XmlDocument
            XmlNode importedNode = Project.Content.Controller.Tags.OwnerDocument.ImportNode(tagNode, true);
            Project.Content.Controller.Tags.AppendChild(importedNode);            
        }
    }
}