using Fls.AcesysConversion.Common.Enums;

namespace Fls.AcesysConversion.Common;
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
