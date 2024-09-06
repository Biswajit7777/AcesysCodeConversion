namespace Fls.AcesysConversion.Common;

public class RockwellUpgradeOptions : UpgradeOptionsBase
{
    public bool IsExtendedSelect { get; set; }
    public bool IsExtendedInterlock { get; set; }
    public bool IsRedundantPlc { get; set; }
    public bool IsMapByFunction { get; set; }
    public string NewFileName { get; set; }

    public List<string> ECSFilePaths { get; set; }

    public RockwellUpgradeOptions()
    {
        IsExtendedSelect = false;
        IsExtendedInterlock = false;
        IsRedundantPlc = false;
        IsMapByFunction = false;
        NewFileName = string.Empty;
        ECSFilePaths = new List<string>();
    }
}
