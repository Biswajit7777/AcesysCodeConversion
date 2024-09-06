using Fls.AcesysConversion.Common.Entities;
using Fls.AcesysConversion.Common.Enums;
using Fls.AcesysConversion.PLC.Rockwell.Components.AddOns;
using Fls.AcesysConversion.PLC.Rockwell.Components.DataTypes;
using Fls.AcesysConversion.PLC.Rockwell.Components.Programs;
using Fls.AcesysConversion.PLC.Rockwell.Components.Tags;
using Fls.AcesysConversion.PLC.Rockwell.Components.Tasks;
using Fls.AcesysConversion.PLC.Siemens.Components.DataBlock;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components
{
    public class RockwellL5XProject : XmlDocument
    {
        private const string FlsUiIdentifier = "FLS_UI_IDENTIFIER";
        public readonly PlcManufacturer Manufacturer = PlcManufacturer.Rockwell;
        private readonly List<IMessageBoardSubscriber> Subscribers = new();
        private int sequence = 0;        

        public RockwellL5XProject()
        {
        }

        public L5XContent? Content => (L5XContent?)SelectSingleNode("RSLogix5000Content");

        public override void Load(string pathToXml)
        {
            if (pathToXml != null && File.Exists(pathToXml))
            {
                base.Load(pathToXml);
            }
        }

        public int GetNewMessageboardReference()
        {
            sequence++;
            return sequence;
        }

        public override XmlElement CreateElement(string? prefix, string localName, string? namespaceURI)
        {
            int seq = GetNewMessageboardReference();
            XmlElement element = (XmlElement)CreateXmlElement(prefix ?? string.Empty, localName, namespaceURI ?? string.Empty, seq);
            AddFlsUiAttribute(element);
            return element;
        }

        private void AddFlsUiAttribute(XmlElement element)
        {
            if (element.Attributes![FlsUiIdentifier] != null)
            {
                element.Attributes[FlsUiIdentifier]!.Value = sequence.ToString();
            }
            else
            {
                XmlAttribute idAttribute = element.OwnerDocument.CreateAttribute(FlsUiIdentifier);
                idAttribute.Value = sequence.ToString();
                _ = element.Attributes.Append(idAttribute);
            }
        }

        private XmlNode CreateXmlElement(string prefix, string localName, string namespaceURI, int seq)
        {
            RockwellL5XItemBase? node = localName.ToUpper() switch
            {
                "RSLOGIX5000CONTENT" => new L5XContent(prefix, localName, namespaceURI, this, seq),
                "CONTROLLER" => new L5XController(prefix, localName, namespaceURI, this, seq),
                "DATATYPE" => new L5XDataType(prefix, localName, namespaceURI, this, seq),
                "DATATYPES" => new L5XDataTypes(prefix, localName, namespaceURI, this, seq),
                "TAG" => new L5XTag(prefix, localName, namespaceURI, this, seq),
                "TAGS" => new L5XTags(prefix, localName, namespaceURI, this, seq),
                "ADDONINSTRUCTIONDEFINITIONS" => new L5XAddOnInstructionDefinitions(prefix, localName, namespaceURI, this, seq),
                "ADDONINSTRUCTIONDEFINITION" => new L5XAddOnInstructionDefinition(prefix, localName, namespaceURI, this, seq),
                "PROGRAM" => new L5XProgram(prefix, localName, namespaceURI, this, seq),
                "PROGRAMS" => new L5XPrograms(prefix, localName, namespaceURI, this, seq),
                "ROUTINE" => new L5XRoutine(prefix, localName, namespaceURI, this, seq),
                "ROUTINES" => new L5XRoutines(prefix, localName, namespaceURI, this, seq),
                "RLLCONTENT" => new L5XRllContent(prefix, localName, namespaceURI, this, seq),
                "RUNG" => new L5XRung(prefix, localName, namespaceURI, this, seq),
                "TASK" => new L5XTask(prefix, localName, namespaceURI, this, seq),
                "TASKS" => new L5XTasks(prefix, localName, namespaceURI, this, seq),
                _ => new L5XUnknownElement(prefix, localName, namespaceURI, this, seq),
            };
            return node;
        }

        public void Attach(IMessageBoardSubscriber subscriber)
        {
            Subscribers.Add(subscriber);
        }

        public void Detach(IMessageBoardSubscriber subscriber)
        {
            _ = Subscribers.Remove(subscriber);
        }

        public void Announce(UserMessage um)
        {
            foreach (IMessageBoardSubscriber subscriber in Subscribers)
            {
                subscriber.AnnounceNewUserMessage(um);
            }
        }

        // New Method to Extract ProcessorType
        public string GetProcessorType()
        {
            XmlNode controllerNode = SelectSingleNode("/RSLogix5000Content/Controller");
            if (controllerNode != null)
            {
                XmlAttribute processorTypeAttr = controllerNode.Attributes["ProcessorType"];
                return processorTypeAttr?.Value;
            }
            return null;
        }

        // New Method to Extract Address from Module
        public string GetAddressFromModule(string processorType)
        {
            XmlNode moduleNode = SelectSingleNode($"/RSLogix5000Content/Controller/Modules/Module[@CatalogNumber='{processorType}']");
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

        // Method to Replace Placeholder in Content
        public static string ReplacePlaceholder(string content, string placeholder, string replacement)
        {
            return content.Replace(placeholder, replacement);
        }

        public void UpdateCPUSlotInRLLContent(string programName, string controllerSlotNo)
        {
            XmlNode programNode = SelectSingleNode($"/RSLogix5000Content/Controller/Programs/Program[@Name='{programName}']");
            if (programNode != null)
            {
                XmlNodeList rllContentNodes = programNode.SelectNodes("Routines/Routine/RLLContent/Rung/Text");
                foreach (XmlNode rllContentNode in rllContentNodes)
                {
                    if (rllContentNode != null && rllContentNode.InnerText.Contains("##CPUSlot##"))
                    {
                        string modifiedText = rllContentNode.InnerText.Replace("##CPUSlot##", controllerSlotNo);
                        XmlCDataSection cdataSection = rllContentNode.OwnerDocument.CreateCDataSection(modifiedText);
                        rllContentNode.InnerXml = cdataSection.OuterXml;
                    }
                }
            }
        }

        public void UpdateHmiUnitIndicesControllerName(XmlDocument xmlDocument, string programName, int HMI_Max_Unit_Index, string controllerName)
        {
            XmlNode? programNode = xmlDocument.SelectSingleNode($"//Program[@Name='{programName}']");

            if (programNode != null)
            {
                XmlNodeList rungNodes = programNode.SelectNodes("Routines/Routine/RLLContent/Rung/Text");

                if (rungNodes != null)
                {
                    int maxIndex = HMI_Max_Unit_Index;
                    Regex hmiUnitRegex = new Regex(@"HMI_UNIT\[(\d+)\]");

                    // Replace ##NextUnitNo## and ##ControllerName## in all Text nodes
                    foreach (XmlNode textNode in rungNodes)
                    {
                        if (textNode != null)
                        {
                            string modifiedText = textNode.InnerText;

                            if (modifiedText.Contains("##NextUnitNo##"))
                            {
                                modifiedText = modifiedText.Replace("##NextUnitNo##", maxIndex.ToString());
                            }

                            if (modifiedText.Contains("##ControllerName##"))
                            {
                                modifiedText = modifiedText.Replace("##ControllerName##", controllerName);
                            }

                            XmlCDataSection cdataSection = xmlDocument.CreateCDataSection(modifiedText);
                            textNode.InnerXml = cdataSection.OuterXml;
                        }
                    }
                }
            }
        }
    }
}