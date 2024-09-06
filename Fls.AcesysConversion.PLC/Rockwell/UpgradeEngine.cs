using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Helpers.Database;

namespace Fls.AcesysConversion.PLC.Rockwell;

public abstract class UpgradeEngine
{
    
    public abstract void ProcessOne2Many(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress);
    public abstract void ProcessOne2One(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress);
    public abstract void ProcessMany2One(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress);
    public abstract void ProcessRemoval(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress);
    public abstract void ProcessMandatory(DbHelper dbHelper, RockwellUpgradeOptions options, IProgress<string> progress);


}