using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.Tags;

public class L5XTag : L5XCollection
{
    public L5XTag(string? prefix, string localName, string? namespaceURI, XmlDocument doc, int seq)
        : base(prefix ?? string.Empty, localName, namespaceURI ?? string.Empty, doc)
    {
        MessageboardReference = seq;
    }

    public string? TagName => base.Attributes?.GetNamedItem("Name")?.Value;

    public string? DataType
    {
        get => base.Attributes?.GetNamedItem("DataType")?.Value;

        set
        {
            XmlNode attr = base.Attributes?.GetNamedItem("DataType")!;
            attr.Value = value;
            _ = (base.Attributes?.SetNamedItem(attr));
        }
    }

    public XmlAttribute AddUsagePublic()
    {
        XmlAttribute publicAttribute = OwnerDocument.CreateAttribute("Usage");
        publicAttribute.Value = "Public";
        return base.Attributes!.Append(publicAttribute);
    }

    public XmlAttribute AddConstantFalse()
    {
        XmlAttribute publicAttribute = OwnerDocument.CreateAttribute("Constant");
        publicAttribute.Value = "false";
        return base.Attributes!.Append(publicAttribute);
    }

    public XmlNode? RemoveL5K()
    {
        XmlNode? nodeToRemove = base.SelectSingleNode("Data[@Format='L5K']");
        return nodeToRemove == null ? null : RemoveChild(nodeToRemove);
    }

    public void UpdateDecoratedDataWithNewStandard(string newStandard)
    {
        XmlNode? nodeToModify = base.SelectSingleNode("Data[@Format='Decorated']");  //assuming decorated will be present
        if (nodeToModify == null)
        {
            return;
        }

        nodeToModify.ParentNode!.InnerXml = newStandard;
    }
    public void InsertDecoratedDataForInterlock(string standard)
    {
        XmlNode nodeToAdd = OwnerDocument.CreateElement("DUMMY");
        nodeToAdd.InnerXml = standard;
        _ = base.AppendChild(nodeToAdd.FirstChild!);
    }

    public XmlNodeList? GetAllDataValueMembers()
    {
        return base.SelectNodes("Data[@Format='Decorated']/Structure/DataValueMember | Data[@Format='Decorated']/StructureMember/DataValueMember");
    }    

    public XmlNode? GetSingleDataValueMember(string nameAttribute)
    {
        XmlNode? node = base.SelectSingleNode($"Data[@Format='Decorated']/Structure/DataValueMember[@Name='{nameAttribute}']");
        node ??= base.SelectSingleNode($"Data[@Format='Decorated']/StructureMember/DataValueMember[@Name='{nameAttribute}']");

        return node;
    }

    public string? AliasFor
    {
        get => base.Attributes?.GetNamedItem("AliasFor")?.Value;

        set
        {
            XmlNode attr = base.Attributes?.GetNamedItem("AliasFor")!;
            attr.Value = value;
            _ = (base.Attributes?.SetNamedItem(attr));
        }
    }

    public string? TagType
    {
        get => base.Attributes?.GetNamedItem("TagType")?.Value;

        set
        {
            XmlNode attr = base.Attributes?.GetNamedItem("TagType")!;
            attr.Value = value;
            _ = (base.Attributes?.SetNamedItem(attr));
        }
    }

    public static L5XTag FromXmlNode(XmlNode node, XmlDocument doc, int seq)
    {
        L5XTag tag = new L5XTag(
            prefix: null,
            localName: node.LocalName,
            namespaceURI: node.NamespaceURI,
            doc: doc,
            seq: seq
        );

        // Copy attributes from the XmlNode to the L5XTag instance
        foreach (XmlAttribute attr in node.Attributes)
        {
            XmlAttribute newAttr = doc.CreateAttribute(attr.Name);
            newAttr.Value = attr.Value;
            ((XmlElement)tag).SetAttributeNode(newAttr);
        }

        // Copy child nodes from the XmlNode to the L5XTag instance
        foreach (XmlNode childNode in node.ChildNodes)
        {
            XmlNode importedChildNode = doc.ImportNode(childNode, true);
            tag.AppendChild(importedChildNode);
        }

        return tag;
    }

    public XmlNode? GetSingleStructureValueMember(string structureName, string dataValueMemberName)
    {
        XmlNode? structureMemberNode = base.SelectSingleNode($"Data[@Format='Decorated']/Structure/StructureMember[@Name='{structureName}']");

        dataValueMemberName = RemovePrefix(dataValueMemberName, "SIM");

        if (structureMemberNode != null)
        {
            // Locate the DataValueMember node within the StructureMember node by name
            XmlNode? dataValueMemberNode = structureMemberNode.SelectSingleNode($"DataValueMember[@Name='{dataValueMemberName}']");
            return dataValueMemberNode;
        }

        return null;
    }

    public string RemovePrefix(string prefixedName, string prefix)
    {
        if (prefixedName.StartsWith(prefix + "."))
        {
            return prefixedName.Substring(prefix.Length + 1);
        }

        return prefixedName;
    }

}
