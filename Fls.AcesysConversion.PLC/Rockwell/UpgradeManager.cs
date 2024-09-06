using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Helpers.Database;

namespace Fls.AcesysConversion.PLC.Rockwell;

public class UpgradeManager
{
    public static void ProcessUpgrade(UpgradeEngine engine, DbHelper? dbHelper,
        RockwellUpgradeOptions options, IProgress<string> progress)
    {
        if (dbHelper != null)
        {           
            engine.ProcessOne2Many(dbHelper, options, progress);
            engine.ProcessMany2One(dbHelper, options, progress);
            engine.ProcessOne2One(dbHelper, options, progress);
            engine.ProcessRemoval(dbHelper, options, progress);
            engine.ProcessMandatory(dbHelper, options, progress);

        }

    }
}