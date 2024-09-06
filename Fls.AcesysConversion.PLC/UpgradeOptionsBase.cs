using Fls.AcesysConversion.Common.Enums;

namespace Fls.AcesysConversion.PLC;

public abstract class UpgradeOptionsBase
{
    public AcesysVersions FromVersion { get; set; }
    public AcesysVersions ToVersion { get; set; }
    public UpgradeOptionsBase()
    {
        FromVersion = AcesysVersions.Unknown;
        ToVersion = AcesysVersions.Unknown;
    }

}
