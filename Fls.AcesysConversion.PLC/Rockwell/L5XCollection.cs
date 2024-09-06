using Fls.AcesysConversion.Common.Entities;
using Fls.AcesysConversion.Common.Enums;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Fls.AcesysConversion.PLC.Rockwell.Components;

public abstract partial class L5XCollection : RockwellL5XItemBase
{
    private const string FlsUiIdentifier = "FLS_UI_IDENTIFIER";

    public L5XCollection(string prefix, string localname, string nsURI, XmlDocument l5Project) : base(prefix, localname, nsURI, l5Project)
    {
    }

    public XmlElement? this[int index] => (XmlElement?)ChildNodes.Item(index);

    public new XmlElement? this[string id]
    {
        get
        {
            XmlElement? nodex;
            try
            {
                id = id.Trim();
                nodex = (XmlElement?)SelectSingleNode(id);
            }
            catch (Exception)
            {
                nodex = null;
            }
            return nodex;
        }
    }

    public bool Exist(string newName)
    {
        XmlElement? nodex;
        try
        {
            nodex = (XmlElement?)SelectSingleNode("./*[@Name='" + newName + "']");
        }
        catch (Exception)
        {
            nodex = null;
        }
        return nodex != null;
    }

    public bool Remove(int index)
    {
        XmlElement? nodex;
        try
        {
            nodex = (XmlElement?)ChildNodes.Item(index);
        }
        catch (Exception)
        {
            nodex = null;
        }
        if (nodex == null)
        {
            return false;
        }
        else
        {
            _ = (RockwellL5XProject)nodex.OwnerDocument;
            //AddUserMessage(project, nodex, UserMessageTypes.Information, "Remove", "xx", "yy");
            //_ = (nodex?.ParentNode?.RemoveChild(nodex));
            return true;
        }
    }

    public bool Remove(string id, bool isAddMessage = false, string operation = "", string optionalMessage = "")
    {
        XmlElement? nodex;
        nodex = (XmlElement?)SelectSingleNode("./*[@Name='" + id + "']");

        if (nodex != null)
        {
            if (isAddMessage)
            {
                RockwellL5XProject project = (RockwellL5XProject)nodex.OwnerDocument;
                AddUserMessage(project, null, nodex, UserMessageTypes.Information, "Remove", operation, optionalMessage);
            }
            _ = (nodex?.ParentNode?.RemoveChild(nodex));
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool RemoveByXPath(string xpath, bool isAddMessage = false, string operation = "", string optionalMessage = "")
    {
        XmlElement? nodex;
        try
        {
            nodex = (XmlElement?)SelectSingleNode(xpath);
        }
        catch (Exception)
        {
            nodex = null;
        }

        if (nodex != null)
        {
            if (isAddMessage)
            {
                RockwellL5XProject project = (RockwellL5XProject)nodex.OwnerDocument;
                AddUserMessage(project, null, nodex, UserMessageTypes.Information, "Remove", operation, optionalMessage);
            }
            _ = (nodex?.ParentNode?.RemoveChild(nodex));
            return true;
        }
        else
        {
            return false;
        }
    }

    public int Count => ChildNodes.Count;

    public void Clear()
    {
        InnerXml = "";
    }

    public virtual bool Add(string newName, string xmlStdStructure, string replacementType, XmlElement? originalElement)
    {
        if (Exist(newName))
        {
            _ = Remove(newName);
        }

        RockwellL5XItemBase dummyNode = (RockwellL5XItemBase)((RockwellL5XProject)OwnerDocument).CreateElement("DUMMY");
        dummyNode.InnerXml = RemoveTabAndNewLineRegex().Replace(xmlStdStructure, "");
        RockwellL5XItemBase? newNode = (RockwellL5XItemBase?)dummyNode.ChildNodes[0];
        if (newNode != null)
        {
            RockwellL5XProject project = (RockwellL5XProject)newNode.OwnerDocument;
            newNode.MessageboardReference = project.GetNewMessageboardReference();
            _ = AppendChild(newNode);

            if (SelectSingleNode("./*[@Name='" + newName + "']") == null)
            {
                AddUserMessage(project, newNode, originalElement, UserMessageTypes.Information, "Add", replacementType, "");
            }
            else
            {
                AddUserMessage(project, newNode, originalElement, UserMessageTypes.Information, "Add", replacementType);
            }
        }
        else
        {
            AddUserMessage((RockwellL5XProject)dummyNode.OwnerDocument, null, null, UserMessageTypes.Error, "Add", $"Cannot Create Node {newName}");
        }
        return true;
    }

    public void AddByElement(XmlElement xmlElement)
    {
        string newName = xmlElement.GetAttribute("Name");
        if (Exist(newName))
        {
            _ = Remove(newName);
        }

        XmlDocument xmlDocument = OwnerDocument;
        XmlElement newElement = (XmlElement)xmlDocument.ImportNode(xmlElement, true);

        AppendChild(newElement);

        RockwellL5XProject project = (RockwellL5XProject)newElement.OwnerDocument;        

    }


    public static void AddUserMessage(RockwellL5XProject project, XmlElement? newNode, XmlElement? originalNode, UserMessageTypes umt, string operation, string replacementType, string message = "")
    {
        int newNodeFlsUiIdentifier = -1;
        int originalNodeFlsUiIdentifier = -1;
        string newNodeTypeName = MakeNodeTypeName(newNode);
        string newNodeName = "Unknown";

        if (newNode != null)
        {
            if (int.TryParse(newNode.GetAttribute(FlsUiIdentifier), out int parsedNewNodeFlsUiIdentifier))
            {
                newNodeFlsUiIdentifier = parsedNewNodeFlsUiIdentifier;
            }

            if (newNode.Attributes?["Name"]?.Value != null)
            {
                newNodeName = newNode.Attributes["Name"].Value;
            }
        }

        if (originalNode != null)
        {
            if (int.TryParse(originalNode.GetAttribute(FlsUiIdentifier), out int parsedOriginalNodeFlsUiIdentifier))
            {
                originalNodeFlsUiIdentifier = parsedOriginalNodeFlsUiIdentifier;
            }
        }

        UserMessage msg = new UserMessage(
            newNodeFlsUiIdentifier,
            originalNodeFlsUiIdentifier,
            newNodeTypeName,
            newNodeName,
            umt,
            operation,
            replacementType,
            message
        );

        project.Announce(msg);
    }

    private static string MakeNodeTypeName(XmlElement? newNode)
    {
        string parentName = newNode?.ParentNode?.Name ?? "";
        return $"{parentName}>{newNode?.Name}";
    }

    [GeneratedRegex("\\t|\\n|\\r")]
    private static partial Regex RemoveTabAndNewLineRegex();
}
