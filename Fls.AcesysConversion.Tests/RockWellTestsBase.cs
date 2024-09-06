using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Common.Enums;
using Fls.AcesysConversion.PLC.Rockwell.Components;
using System.Xml;

namespace Fls.AcesysConversion.Tests;

public abstract class RockWellTestsBase
{
    protected RockwellUpgradeOptions optionsDefSelDefInt = new()
    {
        FromVersion = AcesysVersions.V77,
        ToVersion = AcesysVersions.V80,
        IsExtendedSelect = false,
        IsExtendedInterlock = false
    };

    protected RockwellUpgradeOptions optionsDefSelExtInt = new()
    {
        FromVersion = AcesysVersions.V77,
        ToVersion = AcesysVersions.V80,
        IsExtendedSelect = false,
        IsExtendedInterlock = true
    };

    protected RockwellUpgradeOptions optionsExtSelDefInt = new()
    {
        FromVersion = AcesysVersions.V77,
        ToVersion = AcesysVersions.V80,
        IsExtendedSelect = true,
        IsExtendedInterlock = false
    };

    protected RockwellUpgradeOptions optionsExtSelExtInt = new()
    {
        FromVersion = AcesysVersions.V77,
        ToVersion = AcesysVersions.V80,
        IsExtendedSelect = true,
        IsExtendedInterlock = true
    };

    protected RockwellUpgradeOptions optionsDefSelDefIntMapByName = new()
    {
        FromVersion = AcesysVersions.V77,
        ToVersion = AcesysVersions.V80,
        IsExtendedSelect = false,
        IsExtendedInterlock = false,
        IsMapByFunction = false
    };

    protected RockwellUpgradeOptions optionsDefSelDefIntMapByFunction = new()
    {
        FromVersion = AcesysVersions.V77,
        ToVersion = AcesysVersions.V80,
        IsExtendedSelect = false,
        IsExtendedInterlock = false,
        IsMapByFunction = true
    };
    protected static string NodeType { get; set; } = "";

    public static async Task<(int, XmlNode?, int)> ProcessXmlFile(string fileName, RockwellUpgradeOptions options)
    {
        RockwellL5XProject xmlDocumentReplaced = new();
        RockwellL5XProject xmlDocumentOriginal = new();
        IProgress<string> progress = new Progress<string>(ProgressHandler);

        xmlDocumentReplaced.Load(fileName);
        xmlDocumentOriginal.Load(fileName);
        XmlNode? beforeConversion = xmlDocumentReplaced?.SelectSingleNode($@"/RSLogix5000Content/Controller/{NodeType}");
        int beforeConversionCount = beforeConversion?.ChildNodes?.Count ?? 0;
        L5XController? rockwellController = xmlDocumentReplaced?.Content?.Controller;
        if (rockwellController != null)
        {
            await Task.Run(() => rockwellController.UpgradeVersion(xmlDocumentOriginal, options, progress));
        }

        XmlNode? afterConversionNode = xmlDocumentReplaced?.SelectSingleNode($@"/RSLogix5000Content/Controller/{NodeType}");
        int afterConversionCount = afterConversionNode?.ChildNodes?.Count ?? 0;

        return (beforeConversionCount, afterConversionNode, afterConversionCount);
    }

    private static void ProgressHandler(string message)
    {
    }
}