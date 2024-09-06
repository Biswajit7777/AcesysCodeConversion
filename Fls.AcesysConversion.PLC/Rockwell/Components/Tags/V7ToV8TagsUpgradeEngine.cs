using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Common.DTOs;
using Fls.AcesysConversion.Common.Enums;
using Fls.AcesysConversion.Helpers.Database;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.Tags;

public partial class V7ToV8TagsUpgradeEngine : UpgradeEngine
{
    public L5XTags Tags;
    public L5XTags OriginalTags;
    public RockwellL5XProject Project;

    public V7ToV8TagsUpgradeEngine(L5XCollection collection, L5XCollection originalCollection, RockwellL5XProject proj)
    {
        Tags = (L5XTags)collection;
        OriginalTags = (L5XTags)originalCollection;
        Project = proj;
    }

    public override void ProcessOne2One(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        ProcessOne2OneTagsForDataTypes(dbHelper, options, progress);
        ProcessOne2OneTagsForAddOns(dbHelper, options, progress);
        ProcessOne2OneTagsForHmiStandardTags(dbHelper, options, progress);
        ProcessTagsForHmiInterlock(dbHelper, options, progress);
    }

    private void ProcessOne2OneTagsForDataTypes(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        List<Dto> one2one = dbHelper.GetOneToOneTagsDataType(options);
        List<FpMemberDto> fpMembers = dbHelper.GetFpMembers(options);

        ProcessTagsForDataType(one2one, fpMembers, "O2O", progress);
    }

    private void ProcessTagsForDataType(List<Dto> standards, List<FpMemberDto> members, string replacementType, IProgress<string> progress)
    {
        if (Project.Content?.Controller?.Tags != null)
        {
            L5XTag? originalTag = null;                        

            foreach (L5XTag tagItem in Project.Content?.Controller?.Tags!)
            {
                originalTag = OriginalTags.TryGetTagByName(tagItem.TagName!);                
                
                    progress.Report($"Processing Tag {tagItem.TagName}");
                    string? tagDataType = tagItem.DataType;
                    Dto? filteredRows = standards.Where(i => i.FromObject == tagDataType).FirstOrDefault();
                    if (filteredRows != null)
                    {
                        if (tagDataType != null && tagDataType.Equals(filteredRows.FromObject))
                        {
                            tagItem.DataType = filteredRows.ToObject;

                            L5XCollection.AddUserMessage(Project, tagItem, originalTag, UserMessageTypes.Information,
                                "Tag DataType Replace", replacementType,
                                $"Tag {tagItem.TagName} DataType Modified from {tagDataType} to {filteredRows.ToObject}");

                            if (tagItem.RemoveL5K() != null)
                            {
                                L5XCollection.AddUserMessage(Project, tagItem, originalTag, UserMessageTypes.Information,
                                    "Tag Data Format=L5K Remove", replacementType,
                                $"Tag {tagItem.TagName} Data Format=L5K removed");
                            }

                            try
                            {
                                if (!string.IsNullOrEmpty(filteredRows.XmlStandard))
                                {
                                    tagItem.UpdateDecoratedDataWithNewStandard(filteredRows.XmlStandard);

                                    L5XCollection.AddUserMessage(Project, tagItem, originalTag, UserMessageTypes.Information,
                                        "Tag Data Format=Decorated Replace", replacementType,
                                        $"Tag {tagDataType} Data Format=Decorated Replaced");

                                    MoveFpMembersAttribute(OriginalTags, tagItem, members, progress);
                                }
                            }
                            catch
                            {
                                L5XCollection.AddUserMessage(Project, tagItem, originalTag, UserMessageTypes.Error, //todo: remove add user message from l5x collection and move to engine
                                    "Tag Data Format=Decorated Replace", replacementType,
                                    $"Error While Updating Tag Data Format=Decorated");
                            }

                        }
                    
                }                
            }            
        }

        CreateAndAppendAnalogCounterTag();
        CreateAndAppendDummyIntlTag();        
        CreateAndAppendDummySourceTag();
        CreateAndAppendMsgCpuUsageL7XTag();
        CreateAndAppendMsgCpuUsageL8XTag();
        CreateAndAppendDataCpuUsageL7XTag();
    }

    private void CreateAndAppendDataCpuUsageL7XTag()
    {
        // Create a new XmlDocument
        XmlDocument xmlDoc = new XmlDocument();

        // Create Tag node
        XmlNode tagNode = xmlDoc.CreateElement("Tag");

        // Add attributes to the Tag node
        XmlAttribute nameAttr = xmlDoc.CreateAttribute("Name");
        nameAttr.Value = "DATA_CPU_USAGE_L7X";
        tagNode.Attributes.Append(nameAttr);

        XmlAttribute tagTypeAttr = xmlDoc.CreateAttribute("TagType");
        tagTypeAttr.Value = "Base";
        tagNode.Attributes.Append(tagTypeAttr);

        XmlAttribute dataTypeAttr = xmlDoc.CreateAttribute("DataType");
        dataTypeAttr.Value = "DINT";
        tagNode.Attributes.Append(dataTypeAttr);

        XmlAttribute dimensionsAttr = xmlDoc.CreateAttribute("Dimensions");
        dimensionsAttr.Value = "60";
        tagNode.Attributes.Append(dimensionsAttr);

        XmlAttribute radixAttr = xmlDoc.CreateAttribute("Radix");
        radixAttr.Value = "Decimal";
        tagNode.Attributes.Append(radixAttr);

        XmlAttribute constantAttr = xmlDoc.CreateAttribute("Constant");
        constantAttr.Value = "false";
        tagNode.Attributes.Append(constantAttr);

        XmlAttribute externalAccessAttr = xmlDoc.CreateAttribute("ExternalAccess");
        externalAccessAttr.Value = "Read/Write";
        tagNode.Attributes.Append(externalAccessAttr);

        // Create Data node
        XmlNode dataNode = xmlDoc.CreateElement("Data");

        // Add attributes to the Data node
        XmlAttribute formatAttr = xmlDoc.CreateAttribute("Format");
        formatAttr.Value = "Decorated";
        dataNode.Attributes.Append(formatAttr);

        // Create Array node
        XmlNode arrayNode = xmlDoc.CreateElement("Array");

        // Add attributes to the Array node
        XmlAttribute arrayDataTypeAttr = xmlDoc.CreateAttribute("DataType");
        arrayDataTypeAttr.Value = "DINT";
        arrayNode.Attributes.Append(arrayDataTypeAttr);

        XmlAttribute arrayDimensionsAttr = xmlDoc.CreateAttribute("Dimensions");
        arrayDimensionsAttr.Value = "60";
        arrayNode.Attributes.Append(arrayDimensionsAttr);

        XmlAttribute arrayRadixAttr = xmlDoc.CreateAttribute("Radix");
        arrayRadixAttr.Value = "Decimal";
        arrayNode.Attributes.Append(arrayRadixAttr);

        // Create and add Element nodes
        for (int i = 0; i < 60; i++)
        {
            XmlNode elementNode = xmlDoc.CreateElement("Element");

            XmlAttribute indexAttr = xmlDoc.CreateAttribute("Index");
            indexAttr.Value = $"[{i}]";
            elementNode.Attributes.Append(indexAttr);

            XmlAttribute valueAttr = xmlDoc.CreateAttribute("Value");
            valueAttr.Value = "0";
            elementNode.Attributes.Append(valueAttr);

            arrayNode.AppendChild(elementNode);
        }

        // Append Array node to Data node
        dataNode.AppendChild(arrayNode);

        // Append Data node to Tag node
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

    private void CreateAndAppendMsgCpuUsageL8XTag()
    {
        // Create a new XmlDocument
        XmlDocument xmlDoc = new XmlDocument();

        // Create Tag node
        XmlNode tagNode = xmlDoc.CreateElement("Tag");

        // Add attributes to the Tag node
        XmlAttribute nameAttr = xmlDoc.CreateAttribute("Name");
        nameAttr.Value = "MSG_CPU_USAGE_L8X";
        tagNode.Attributes.Append(nameAttr);

        XmlAttribute tagTypeAttr = xmlDoc.CreateAttribute("TagType");
        tagTypeAttr.Value = "Base";
        tagNode.Attributes.Append(tagTypeAttr);

        XmlAttribute dataTypeAttr = xmlDoc.CreateAttribute("DataType");
        dataTypeAttr.Value = "MESSAGE";
        tagNode.Attributes.Append(dataTypeAttr);

        XmlAttribute externalAccessAttr = xmlDoc.CreateAttribute("ExternalAccess");
        externalAccessAttr.Value = "Read/Write";
        tagNode.Attributes.Append(externalAccessAttr);

        // Create Data node
        XmlNode dataNode = xmlDoc.CreateElement("Data");

        // Add attributes to the Data node
        XmlAttribute formatAttr = xmlDoc.CreateAttribute("Format");
        formatAttr.Value = "Message";
        dataNode.Attributes.Append(formatAttr);

        // Create MessageParameters node
        XmlNode messageParametersNode = xmlDoc.CreateElement("MessageParameters");

        // Add attributes to the MessageParameters node
        XmlAttribute messageTypeAttr = xmlDoc.CreateAttribute("MessageType");
        messageTypeAttr.Value = "CIP Generic";
        messageParametersNode.Attributes.Append(messageTypeAttr);

        XmlAttribute requestedLengthAttr = xmlDoc.CreateAttribute("RequestedLength");
        requestedLengthAttr.Value = "0";
        messageParametersNode.Attributes.Append(requestedLengthAttr);

        XmlAttribute connectedFlagAttr = xmlDoc.CreateAttribute("ConnectedFlag");
        connectedFlagAttr.Value = "2";
        messageParametersNode.Attributes.Append(connectedFlagAttr);

        XmlAttribute connectionPathAttr = xmlDoc.CreateAttribute("ConnectionPath");
        connectionPathAttr.Value = "1, ##CPUSlot##";
        messageParametersNode.Attributes.Append(connectionPathAttr);

        XmlAttribute commTypeCodeAttr = xmlDoc.CreateAttribute("CommTypeCode");
        commTypeCodeAttr.Value = "0";
        messageParametersNode.Attributes.Append(commTypeCodeAttr);

        XmlAttribute serviceCodeAttr = xmlDoc.CreateAttribute("ServiceCode");
        serviceCodeAttr.Value = "16#0058";
        messageParametersNode.Attributes.Append(serviceCodeAttr);

        XmlAttribute objectTypeAttr = xmlDoc.CreateAttribute("ObjectType");
        objectTypeAttr.Value = "16#0335";
        messageParametersNode.Attributes.Append(objectTypeAttr);

        XmlAttribute targetObjectAttr = xmlDoc.CreateAttribute("TargetObject");
        targetObjectAttr.Value = "1";
        messageParametersNode.Attributes.Append(targetObjectAttr);

        XmlAttribute attributeNumberAttr = xmlDoc.CreateAttribute("AttributeNumber");
        attributeNumberAttr.Value = "16#0000";
        messageParametersNode.Attributes.Append(attributeNumberAttr);

        XmlAttribute localIndexAttr = xmlDoc.CreateAttribute("LocalIndex");
        localIndexAttr.Value = "0";
        messageParametersNode.Attributes.Append(localIndexAttr);

        XmlAttribute destinationTagAttr = xmlDoc.CreateAttribute("DestinationTag");
        destinationTagAttr.Value = "DATA_CPU_USAGE_L8X";
        messageParametersNode.Attributes.Append(destinationTagAttr);

        XmlAttribute largePacketUsageAttr = xmlDoc.CreateAttribute("LargePacketUsage");
        largePacketUsageAttr.Value = "false";
        messageParametersNode.Attributes.Append(largePacketUsageAttr);

        // Append MessageParameters node to Data node
        dataNode.AppendChild(messageParametersNode);

        // Append Data node to Tag node
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


    private void CreateAndAppendAnalogCounterTag()
    {
        // Create a new XmlDocument
        XmlDocument xmlDoc = new XmlDocument();

        // Create Tag node
        XmlNode tagNode = xmlDoc.CreateElement("Tag");

        // Add attributes to the Tag node
        XmlAttribute nameAttr = xmlDoc.CreateAttribute("Name");
        nameAttr.Value = "AnalogCounter";
        tagNode.Attributes.Append(nameAttr);

        XmlAttribute tagTypeAttr = xmlDoc.CreateAttribute("TagType");
        tagTypeAttr.Value = "Base";
        tagNode.Attributes.Append(tagTypeAttr);

        XmlAttribute dataTypeAttr = xmlDoc.CreateAttribute("DataType");
        dataTypeAttr.Value = "DINT";
        tagNode.Attributes.Append(dataTypeAttr);

        XmlAttribute radixAttr = xmlDoc.CreateAttribute("Radix");
        radixAttr.Value = "Decimal";
        tagNode.Attributes.Append(radixAttr);

        XmlAttribute constantAttr = xmlDoc.CreateAttribute("Constant");
        constantAttr.Value = "false";
        tagNode.Attributes.Append(constantAttr);

        XmlAttribute externalAccessAttr = xmlDoc.CreateAttribute("ExternalAccess");
        externalAccessAttr.Value = "Read/Write";
        tagNode.Attributes.Append(externalAccessAttr);

        // Create Description node with CDATA
        XmlNode descriptionNode = xmlDoc.CreateElement("Description");
        XmlCDataSection cdata = xmlDoc.CreateCDataSection("Time Execution Interval");
        descriptionNode.AppendChild(cdata);
        tagNode.AppendChild(descriptionNode);

        // Create Data node with nested DataValue node
        XmlNode dataNode = xmlDoc.CreateElement("Data");
        XmlAttribute formatAttr = xmlDoc.CreateAttribute("Format");
        formatAttr.Value = "Decorated";
        dataNode.Attributes.Append(formatAttr);

        XmlNode dataValueNode = xmlDoc.CreateElement("DataValue");
        XmlAttribute dataTypeValueAttr = xmlDoc.CreateAttribute("DataType");
        dataTypeValueAttr.Value = "DINT";
        dataValueNode.Attributes.Append(dataTypeValueAttr);
        XmlAttribute radixValueAttr = xmlDoc.CreateAttribute("Radix");
        radixValueAttr.Value = "Decimal";
        dataValueNode.Attributes.Append(radixValueAttr);
        XmlAttribute valueAttr = xmlDoc.CreateAttribute("Value");
        valueAttr.Value = "0";
        dataValueNode.Attributes.Append(valueAttr);

        dataNode.AppendChild(dataValueNode);
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
    private void CreateAndAppendDummyIntlTag()
    {
        // Create a new XmlDocument
        XmlDocument xmlDoc = new XmlDocument();

        // Create Tag node
        XmlNode tagNode = xmlDoc.CreateElement("Tag");

        // Add attributes to the Tag node
        XmlAttribute nameAttr = xmlDoc.CreateAttribute("Name");
        nameAttr.Value = "DUMMY_Intl";
        tagNode.Attributes.Append(nameAttr);

        XmlAttribute tagTypeAttr = xmlDoc.CreateAttribute("TagType");
        tagTypeAttr.Value = "Base";
        tagNode.Attributes.Append(tagTypeAttr);

        XmlAttribute dataTypeAttr = xmlDoc.CreateAttribute("DataType");
        dataTypeAttr.Value = "SINT";
        tagNode.Attributes.Append(dataTypeAttr);

        XmlAttribute radixAttr = xmlDoc.CreateAttribute("Radix");
        radixAttr.Value = "Decimal";
        tagNode.Attributes.Append(radixAttr);

        XmlAttribute constantAttr = xmlDoc.CreateAttribute("Constant");
        constantAttr.Value = "false";
        tagNode.Attributes.Append(constantAttr);

        XmlAttribute externalAccessAttr = xmlDoc.CreateAttribute("ExternalAccess");
        externalAccessAttr.Value = "Read/Write";
        tagNode.Attributes.Append(externalAccessAttr);

        // Create Data node with nested DataValue node
        XmlNode dataNode = xmlDoc.CreateElement("Data");
        XmlAttribute formatAttr = xmlDoc.CreateAttribute("Format");
        formatAttr.Value = "Decorated";
        dataNode.Attributes.Append(formatAttr);

        XmlNode dataValueNode = xmlDoc.CreateElement("DataValue");
        XmlAttribute dataTypeValueAttr = xmlDoc.CreateAttribute("DataType");
        dataTypeValueAttr.Value = "SINT";
        dataValueNode.Attributes.Append(dataTypeValueAttr);
        XmlAttribute radixValueAttr = xmlDoc.CreateAttribute("Radix");
        radixValueAttr.Value = "Decimal";
        dataValueNode.Attributes.Append(radixValueAttr);
        XmlAttribute valueAttr = xmlDoc.CreateAttribute("Value");
        valueAttr.Value = "0";
        dataValueNode.Attributes.Append(valueAttr);

        dataNode.AppendChild(dataValueNode);
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
    private void CreateAndAppendDummySourceTag()
    {
        // Create a new XmlDocument
        XmlDocument xmlDoc = new XmlDocument();

        // Create Tag node
        XmlNode tagNode = xmlDoc.CreateElement("Tag");

        // Add attributes to the Tag node
        XmlAttribute nameAttr = xmlDoc.CreateAttribute("Name");
        nameAttr.Value = "Dummy_Source";
        tagNode.Attributes.Append(nameAttr);

        XmlAttribute tagTypeAttr = xmlDoc.CreateAttribute("TagType");
        tagTypeAttr.Value = "Base";
        tagNode.Attributes.Append(tagTypeAttr);

        XmlAttribute dataTypeAttr = xmlDoc.CreateAttribute("DataType");
        dataTypeAttr.Value = "INT";
        tagNode.Attributes.Append(dataTypeAttr);

        XmlAttribute radixAttr = xmlDoc.CreateAttribute("Radix");
        radixAttr.Value = "Decimal";
        tagNode.Attributes.Append(radixAttr);

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

        // Create DataValue node
        XmlNode dataValueNode = xmlDoc.CreateElement("DataValue");
        XmlAttribute dataValueTypeAttr = xmlDoc.CreateAttribute("DataType");
        dataValueTypeAttr.Value = "INT";
        dataValueNode.Attributes.Append(dataValueTypeAttr);

        XmlAttribute dataValueRadixAttr = xmlDoc.CreateAttribute("Radix");
        dataValueRadixAttr.Value = "Decimal";
        dataValueNode.Attributes.Append(dataValueRadixAttr);

        XmlAttribute valueAttr = xmlDoc.CreateAttribute("Value");
        valueAttr.Value = "1";
        dataValueNode.Attributes.Append(valueAttr);

        // Append DataValue node to Data node
        dataNode.AppendChild(dataValueNode);

        // Append Data node to Tag node
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

    private void CreateAndAppendMsgCpuUsageL7XTag()
    {
        // Create a new XmlDocument
        XmlDocument xmlDoc = new XmlDocument();

        // Create Tag node
        XmlNode tagNode = xmlDoc.CreateElement("Tag");

        // Add attributes to the Tag node
        XmlAttribute nameAttr = xmlDoc.CreateAttribute("Name");
        nameAttr.Value = "MSG_CPU_USAGE_L7X";
        tagNode.Attributes.Append(nameAttr);

        XmlAttribute tagTypeAttr = xmlDoc.CreateAttribute("TagType");
        tagTypeAttr.Value = "Base";
        tagNode.Attributes.Append(tagTypeAttr);

        XmlAttribute dataTypeAttr = xmlDoc.CreateAttribute("DataType");
        dataTypeAttr.Value = "MESSAGE";
        tagNode.Attributes.Append(dataTypeAttr);

        XmlAttribute externalAccessAttr = xmlDoc.CreateAttribute("ExternalAccess");
        externalAccessAttr.Value = "Read/Write";
        tagNode.Attributes.Append(externalAccessAttr);

        // Create Data node
        XmlNode dataNode = xmlDoc.CreateElement("Data");

        // Add attributes to the Data node
        XmlAttribute formatAttr = xmlDoc.CreateAttribute("Format");
        formatAttr.Value = "Message";
        dataNode.Attributes.Append(formatAttr);

        // Create MessageParameters node
        XmlNode messageParametersNode = xmlDoc.CreateElement("MessageParameters");

        // Add attributes to the MessageParameters node
        XmlAttribute messageTypeAttr = xmlDoc.CreateAttribute("MessageType");
        messageTypeAttr.Value = "CIP Generic";
        messageParametersNode.Attributes.Append(messageTypeAttr);

        XmlAttribute requestedLengthAttr = xmlDoc.CreateAttribute("RequestedLength");
        requestedLengthAttr.Value = "2";
        messageParametersNode.Attributes.Append(requestedLengthAttr);

        XmlAttribute connectedFlagAttr = xmlDoc.CreateAttribute("ConnectedFlag");
        connectedFlagAttr.Value = "2";
        messageParametersNode.Attributes.Append(connectedFlagAttr);

        XmlAttribute connectionPathAttr = xmlDoc.CreateAttribute("ConnectionPath");
        connectionPathAttr.Value = "1, ##CPUSlot##";
        messageParametersNode.Attributes.Append(connectionPathAttr);

        XmlAttribute commTypeCodeAttr = xmlDoc.CreateAttribute("CommTypeCode");
        commTypeCodeAttr.Value = "0";
        messageParametersNode.Attributes.Append(commTypeCodeAttr);

        XmlAttribute serviceCodeAttr = xmlDoc.CreateAttribute("ServiceCode");
        serviceCodeAttr.Value = "16#004c";
        messageParametersNode.Attributes.Append(serviceCodeAttr);

        XmlAttribute objectTypeAttr = xmlDoc.CreateAttribute("ObjectType");
        objectTypeAttr.Value = "16#0335";
        messageParametersNode.Attributes.Append(objectTypeAttr);

        XmlAttribute targetObjectAttr = xmlDoc.CreateAttribute("TargetObject");
        targetObjectAttr.Value = "1";
        messageParametersNode.Attributes.Append(targetObjectAttr);

        XmlAttribute attributeNumberAttr = xmlDoc.CreateAttribute("AttributeNumber");
        attributeNumberAttr.Value = "16#0000";
        messageParametersNode.Attributes.Append(attributeNumberAttr);

        XmlAttribute localIndexAttr = xmlDoc.CreateAttribute("LocalIndex");
        localIndexAttr.Value = "0";
        messageParametersNode.Attributes.Append(localIndexAttr);

        XmlAttribute localElementAttr = xmlDoc.CreateAttribute("LocalElement");
        localElementAttr.Value = "Dummy_Source";
        messageParametersNode.Attributes.Append(localElementAttr);

        XmlAttribute destinationTagAttr = xmlDoc.CreateAttribute("DestinationTag");
        destinationTagAttr.Value = "DATA_CPU_USAGE_L7X";
        messageParametersNode.Attributes.Append(destinationTagAttr);

        XmlAttribute largePacketUsageAttr = xmlDoc.CreateAttribute("LargePacketUsage");
        largePacketUsageAttr.Value = "false";
        messageParametersNode.Attributes.Append(largePacketUsageAttr);

        // Append MessageParameters node to Data node
        dataNode.AppendChild(messageParametersNode);

        // Append Data node to Tag node
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

    private void MoveFpMembersAttribute(L5XTags originalTags, L5XTag tagToModify, List<FpMemberDto> members, IProgress<string> progress)
    {
        progress.Report($"Processing Face Plate Members for Tag {tagToModify.Attributes!["Name"]!.Value}");

        L5XTag? originalTag = originalTags.TryGetTagByName(tagToModify.TagName!);

        string SelVolmetric = "";

        if (originalTag != null && originalTag.DataType!.StartsWith("ACESYS_FACEPLATE_"))
        {
            XmlNodeList? dataValueMembers = originalTag.GetAllDataValueMembers();

            if (dataValueMembers != null)
            {
                foreach (XmlNode originalDataValueMember in dataValueMembers)
                {
                    string originalMemberName = originalDataValueMember.Attributes!["Name"]!.Value;
                    FpMemberDto? dtoMember = members.FirstOrDefault(m => m.From_Attribute == originalMemberName);

                    if (dtoMember != null)
                    {
                        XmlNode? nodeToModifyDataValueMember = tagToModify.GetSingleDataValueMember(dtoMember.To_Attribute);
                        if (nodeToModifyDataValueMember != null)
                        {
                            string originalValue = originalDataValueMember.Attributes!["Value"]!.Value;
                            string newValue = nodeToModifyDataValueMember.Attributes!["Value"]!.Value;

                            if (originalValue != newValue)
                            {
                                L5XCollection.AddUserMessage(Project, (XmlElement)nodeToModifyDataValueMember, originalTag, UserMessageTypes.Information,
                                    "Tag DataMember Replace", "O2O",
                                    $"Tag DataMember Replaced From {dtoMember.From_Attribute} To {dtoMember.To_Attribute} " +
                                    $"With Value From {newValue} To {originalValue}");

                                nodeToModifyDataValueMember.Attributes!["Value"]!.Value = originalValue;
                            }

                            // Handle special case for "MD_Type"
                            if (dtoMember.From_Attribute == "MD_Type")
                            {
                                if (originalValue == "0")
                                {
                                    nodeToModifyDataValueMember.Attributes!["Value"]!.Value = "1";

                                    L5XCollection.AddUserMessage(Project, (XmlElement)nodeToModifyDataValueMember, originalTag, UserMessageTypes.Information,
                                    "Tag DataMember Updated", "O2O",
                                    $"Tag DataMember Updated From {dtoMember.From_Attribute} To {dtoMember.To_Attribute} " +
                                    $"With Value From {originalValue} To 1");
                                }
                                else if (originalValue == "1")
                                {
                                    nodeToModifyDataValueMember.Attributes!["Value"]!.Value = "0";

                                    L5XCollection.AddUserMessage(Project, (XmlElement)nodeToModifyDataValueMember, originalTag, UserMessageTypes.Information,
                                    "Tag DataMember Updated", "O2O",
                                    $"Tag DataMember Updated From {dtoMember.From_Attribute} To {dtoMember.To_Attribute} " +
                                    $"With Value From {originalValue} To 0");
                                }
                            }

                            //Not of Sel_Volumetric
                            if (dtoMember.From_Attribute == "Grav_Vol_Mode")
                            {
                                SelVolmetric =  nodeToModifyDataValueMember.Attributes!["Value"]!.Value;  
                            }

                            FpMemberDto? dtoMember1 = members.FirstOrDefault(m => m.To_Attribute == "Sel_Gravametric");

                            if (dtoMember1 != null)
                            {
                                if (dtoMember1.From_Attribute == "Rule_17")
                                {
                                    XmlNode? SelGravametricDataValueMember = tagToModify.GetSingleDataValueMember(dtoMember1.To_Attribute);
                                    if (SelVolmetric == "0")
                                    {
                                        if (SelGravametricDataValueMember != null)
                                        {
                                            SelGravametricDataValueMember.Attributes!["Value"]!.Value = "1";

                                            L5XCollection.AddUserMessage(Project, (XmlElement)SelGravametricDataValueMember, originalTag, UserMessageTypes.Information,
                                              "Tag DataMember Updated", "O2O",
                                              $"Tag DataMember Updated From {dtoMember.From_Attribute} To {dtoMember.To_Attribute} " +
                                              $"With Value From {originalValue} To 1");
                                        }
                                    }

                                    else
                                    {
                                        if (SelGravametricDataValueMember != null)
                                        {
                                            SelGravametricDataValueMember.Attributes!["Value"]!.Value = "0";

                                            L5XCollection.AddUserMessage(Project, (XmlElement)SelGravametricDataValueMember, originalTag, UserMessageTypes.Information,
                                              "Tag DataMember Updated", "O2O",
                                              $"Tag DataMember Updated From {dtoMember.From_Attribute} To {dtoMember.To_Attribute} " +
                                              $"With Value From {originalValue} To 0");
                                        }

                                    }
                                }
                            }

                            
                        }
                    }
                }

                XmlNode? simStructureMember = originalTag.SelectSingleNode("Data[@Format='Decorated']/Structure/StructureMember[@Name='SIM']");
                if (simStructureMember != null)
                {
                    foreach (XmlNode originalSimDataValueMember in simStructureMember.ChildNodes)
                    {
                        if (originalSimDataValueMember.Name == "DataValueMember")
                        {
                            string prefixedName = "SIM." + originalSimDataValueMember.Attributes!["Name"]!.Value;
                            FpMemberDto? dtoMember1 = members.FirstOrDefault(m => m.From_Attribute == prefixedName);

                            if (dtoMember1 != null)
                            {
                                XmlNode? nodeToModifySimDataValueMember = tagToModify.GetSingleStructureValueMember("SIM", dtoMember1.To_Attribute);
                                if (nodeToModifySimDataValueMember != null)
                                {
                                    string originalValue = originalSimDataValueMember.Attributes!["Value"]!.Value;
                                    string newValue = nodeToModifySimDataValueMember.Attributes!["Value"]!.Value;

                                    if (originalValue != newValue)
                                    {
                                        L5XCollection.AddUserMessage(Project, (XmlElement)nodeToModifySimDataValueMember, originalTag, UserMessageTypes.Information,
                                            "Tag DataMember Replace", "O2O",
                                            $"Tag DataMember Replaced From {dtoMember1.From_Attribute} To {dtoMember1.To_Attribute} " +
                                            $"With Value From {newValue} To {originalValue}");

                                        nodeToModifySimDataValueMember.Attributes!["Value"]!.Value = originalValue;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (Apply_Rule_14(originalTag, tagToModify))
            {
                L5XCollection.AddUserMessage(Project, tagToModify, originalTag, UserMessageTypes.Information,
                    "Tag DataMember Replace Rule 14", "O2O", "Successful");
            }
            else
            {
                L5XCollection.AddUserMessage(Project, tagToModify, originalTag, UserMessageTypes.Error,
                    "Tag DataMember Replace Rule 14", "O2O", "Failure");
            }
        }
    }

    private void MoveAddOnMembersAttribute(L5XTags originalTags, L5XTag tagToModify, List<FpMemberDto> members, IProgress<string> progress)
    {
        progress.Report($"Processing Add On Members for Tag {tagToModify.TagName!}");

        L5XTag? originalTag = originalTags.TryGetTagByName(tagToModify.TagName!);

        if (originalTag != null)
        {
            XmlNodeList? dataValueMembers = originalTag.GetAllDataValueMembers();

            if (dataValueMembers != null)
            {
                foreach (XmlNode originalDataValueMember in dataValueMembers)
                {
                    FpMemberDto dtoMember = members.First(m => m.From_Attribute.Equals(originalDataValueMember.Attributes!["Name"]!.Value));
                    if (dtoMember != null)
                    {
                        XmlNode? nodeToModifyDataValueMember = tagToModify.GetSingleDataValueMember(dtoMember.To_Attribute);

                        if (nodeToModifyDataValueMember != null && originalDataValueMember.Attributes!["Name"]!.Value == dtoMember.From_Attribute)
                        {
                            L5XCollection.AddUserMessage(Project, (XmlElement)nodeToModifyDataValueMember, originalTag, UserMessageTypes.Information,
                                    "Tag Add On DataMember Replace", "O2O",
                                    $"Tag Add On DataMember Replaced From {dtoMember.From_Attribute} To {dtoMember.To_Attribute} " +
                                    $"With Value From {nodeToModifyDataValueMember.Attributes!["Value"]!.Value} To {originalDataValueMember.Attributes["Value"]!.Value}");

                            nodeToModifyDataValueMember.Attributes!["Value"]!.Value = originalDataValueMember.Attributes["Value"]!.Value;
                        }
                    }
                }
            }
        }
    }

    private static void ProcessOne2OneTagsForAddOns(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        //Tags are handled one to one only for AddOns, please see ProcessOne2ManyTagsForAddOns(dbHelper, options, progress);
        //Intentionally keeping this method empty for now
    }

    private void ProcessOne2OneTagsForHmiStandardTags(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        List<Dto> one2one = dbHelper.GetOneToOneTagsHmiTags(options);
        XmlElement? originalTag = null;
        if (OriginalTags != null)
        {
            foreach (XmlElement item in OriginalTags)
            {
                if (!item.GetAttribute("TagType").Equals("Base"))
                {
                    continue;
                }

                string tagNameAttribute = item.GetAttribute("Name");
                Dto? filteredO2O = one2one.Where(i => i.FromObject == tagNameAttribute).FirstOrDefault();
                progress.Report($"Processing Tag {item.Attributes!["Name"]!.Value}");
                if (!string.IsNullOrEmpty(filteredO2O?.XmlStandard))
                {
                    XmlNode? nodeToRemove = Project.Content?.Controller?.Tags!.SelectSingleNode($"Tag[@Name='{filteredO2O.ToObject}']");
                    originalTag = (XmlElement?)OriginalTags.SelectSingleNode($"Tag[@Name='{item.Attributes!["Name"]!.Value}']");

                    if (nodeToRemove != null)
                    {
                        _ = Project.Content?.Controller?.Tags!.RemoveChild(nodeToRemove);

                        L5XCollection.AddUserMessage(Project, item, originalTag, UserMessageTypes.Information,
                            $"HMITags Tag Name={tagNameAttribute} Removed", "O2O", "");
                    }

                    _ = (Project.Content?.Controller?.Tags!.Add(filteredO2O.ToObject, filteredO2O.XmlStandard, "O2O", item));
                }
            }
        }
    }

    public override void ProcessOne2Many(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        ProcessOne2ManyTagsForDataTypes(dbHelper, options, progress);
        ProcessOne2ManyTagsForAddOns(dbHelper, options, progress);
        ProcessOne2ManyTagsForHmiTags(dbHelper, options, progress);
    }

    private void ProcessOne2ManyTagsForDataTypes(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        List<Dto> one2many = dbHelper.GetOneToManyTagsDataTypes(options);
        //Tags are handled one to one only
        List<FpMemberDto> fpMembers = dbHelper.GetFpMembers(options);

        ProcessTagsForDataType(one2many, fpMembers, "O2M", progress);
    }

    private void ProcessOne2ManyTagsForAddOns(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        IEnumerable<Dto> one2many = dbHelper.GetOneToManyTagsAddOns(options);
        List<FpMemberDto> addOnMembers = dbHelper.GetAddOnMembers(options);

        L5XTag? originalTag = null;
        if (Project.Content?.Controller?.Tags != null)
        {
            foreach (L5XTag tagItem in Project.Content?.Controller?.Tags!)
            {
                originalTag = OriginalTags.TryGetTagByName(tagItem.TagName!);

                string tagDataType = tagItem.DataType!;
                progress.Report($"Processing Tag For Addon {tagItem.TagName!}");
                //Tags are handled one to one only
                Dto? filteredO2O = one2many.Where(i => i.FromObject == tagDataType).FirstOrDefault();
                if (filteredO2O != null)
                {
                    _ = tagItem!.AddUsagePublic();

                    if (tagDataType != null && tagDataType.Equals(filteredO2O.FromObject))
                    {
                        tagItem.DataType = filteredO2O.ToObject;

                        L5XCollection.AddUserMessage(Project, tagItem, originalTag, UserMessageTypes.Information,
                            "Tag AddOn DataType Replace", "O2O",
                            $"Tag AddOn {tagItem.TagName} DataType Modified from {tagDataType} to {filteredO2O.ToObject}");

                        XmlNode? nodeToRemove = tagItem.SelectSingleNode("Data[@Format='L5K']");
                        if (tagItem.RemoveL5K() != null)
                        {
                            L5XCollection.AddUserMessage(Project, tagItem, originalTag, UserMessageTypes.Information,
                                "Tag AddOn Data Format=L5K Remove", "O2O",
                            $"Tag AddOn {tagItem.TagName} Data Format=L5K removed");
                        }

                        try
                        {
                            if (!string.IsNullOrEmpty(filteredO2O.XmlStandard))
                            {
                                tagItem.UpdateDecoratedDataWithNewStandard(filteredO2O.XmlStandard);
                                L5XCollection.AddUserMessage(Project, tagItem, originalTag, UserMessageTypes.Information,
                                    "Tag AddOn Data Format=Decorated Replace", "O2O",
                                    $"Tag AddOn {tagDataType} Data Format=Decorated Replaced");
                            }
                            MoveAddOnMembersAttribute(OriginalTags, tagItem, addOnMembers, progress);
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
    }

    private void ProcessOne2ManyTagsForHmiTags(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        List<Dto> one2many = dbHelper.GetOneToManyTagsHmiTags(options);
        IEnumerable<string?> distinct = one2many.GroupBy(i => new { i.FromObject }).Select(i => i.FirstOrDefault()?.FromObject);
        XmlElement? originalTag = null;
        foreach (string? d in distinct)
        {
            if (string.IsNullOrEmpty(d))
            {
                continue;
            }
            if (OriginalTags != null)
            {
                foreach (XmlElement item in OriginalTags)
                {
                    originalTag = (XmlElement?)OriginalTags.SelectSingleNode($"Tag[@Name='{item.Attributes!["Name"]!.Value}']");
                    string tagNameAttribute = item.GetAttribute("Name");
                    progress.Report($"Processing Tag for HMI Tags {tagNameAttribute}");
                    if (!item.GetAttribute("TagType").Equals("Base")
                        || !one2many.Any(i => i.FromObject.Equals(tagNameAttribute))
                        || !tagNameAttribute.Equals(d))
                    {
                        continue;
                    }

                    IEnumerable<Dto>? filtered = one2many.Where(i => i.FromObject.Equals(d));

                    if (filtered != null)
                    {
                        XmlNode? nodeToRemove = Project.Content?.Controller?.Tags!.SelectSingleNode($"Tag[@Name='{d}']");

                        if (nodeToRemove != null)
                        {
                            _ = Project.Content?.Controller?.Tags!.RemoveChild(nodeToRemove);

                            L5XCollection.AddUserMessage(Project, item, originalTag, UserMessageTypes.Information,
                                $"HMITags Tag Name={tagNameAttribute} Removed", "O2M", "");
                        }

                        foreach (Dto? child in one2many.Where(i => i.FromObject.Equals(d)))
                        {
                            if (!string.IsNullOrEmpty(child?.XmlStandard))
                            {
                                _ = (Project.Content?.Controller?.Tags!.Add(child.ToObject, child.XmlStandard, "O2M", item));
                            }
                        }
                    }

                }
            }
        }
    }

    public override void ProcessMany2One(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        // do nothing
    }

    public override void ProcessMandatory(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        ProcessOne2OneTagsMandatoryHmiTags(dbHelper, options, progress);        
    }

    private void ProcessOne2OneTagsMandatoryHmiTags(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        List<Dto> mandatory = dbHelper.GetMandatoryHmiTags(options);

        foreach (Dto child in mandatory)
        {
            progress.Report($"Processing Mandatory Tag {child.ToObject}");
            if (!string.IsNullOrEmpty(child?.XmlStandard))
            {
                _ = (Project.Content?.Controller?.Tags!.Add(child.ToObject, child.XmlStandard, "MAN", null));
            }
        }

    }

    public override void ProcessRemoval(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        ProcessDeleteTagsForHmiTags(dbHelper, options, progress);
    }

    private void ProcessDeleteTagsForHmiTags(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        List<string> deleted = dbHelper.GetDeletedHmiTags(options);

        foreach (string tbd in deleted)
        {
            progress.Report($"Processing Delete Tag {tbd}");
            if (!string.IsNullOrEmpty(tbd))
            {
                progress.Report("Deleting HMI Tags");
                _ = (Project.Content?.Controller?.Tags!.Remove(tbd, true, "DEL", $"Tag with Name {tbd} is deleted"));
            }
        }
    }

    private void ProcessTagsForHmiInterlock(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress)
    {
        IEnumerable<Dto> interlock = dbHelper.GetMandatoryTagsAddOns(options);

        if (Project.Content?.Controller?.Tags != null)
        {
            foreach (L5XTag tagItem in Project.Content?.Controller?.Tags!)
            {
                if (tagItem.AliasFor == null)
                {
                    continue;
                }
                if (tagItem.AliasFor!.StartsWith("HMI_INTERLOCK"))
                {
                    tagItem.TagType = "Base";
                    tagItem.RemoveAttribute("Radix");
                    tagItem.AliasFor = "HMI_INTERLOCK[" + GetArrayPosition(tagItem.AliasFor) + "]";
                    XmlNode dataTypeAttribute = tagItem.OwnerDocument!.CreateAttribute("DataType");
                    dataTypeAttribute.Value = options.IsExtendedInterlock ? "AsysExtInterlock" : "AsysInterlock";
                    _ = tagItem.Attributes.SetNamedItem(dataTypeAttribute);

                    _ = tagItem.AddConstantFalse();

                    Dto? filtered = interlock.Where(i => i.ToObject == dataTypeAttribute.Value).FirstOrDefault();
                    if (filtered != null)
                    {
                        tagItem.InsertDecoratedDataForInterlock(filtered.XmlStandard);
                    }
                }
                progress.Report($"Processing Tag Type Alias for HMI Interlock {tagItem.TagName!}");
            }
        }
    }

    private static int GetArrayPosition(string? input)  //todo : check if the name is correct
    {
        //todo : check functionality, is -1 return ok? or should it be 0 or anything else?
        const int returnValueOnCalculationError = -1;

        if (string.IsNullOrEmpty(input))
        {
            return returnValueOnCalculationError;
        }

        int startIndex = input.AsSpan().IndexOf('[');
        int endIndex = input.AsSpan().IndexOf(']');
        if (startIndex == -1 || endIndex == -1)
        {
            return returnValueOnCalculationError;  //cannot get w
        }

        _ = int.TryParse(input.AsSpan(startIndex + 1, endIndex - startIndex - 1), CultureInfo.InvariantCulture, out int w);

        startIndex = input.AsSpan().LastIndexOf('.');
        if (startIndex == -1)
        {
            return returnValueOnCalculationError;  //cannot get b
        }

        _ = int.TryParse(input.AsSpan(startIndex + 1), CultureInfo.InvariantCulture, out int b);

        return (16 * w) + b;

    }

}