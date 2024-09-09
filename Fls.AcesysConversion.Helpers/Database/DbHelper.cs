using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Common.DTOs;
using Fls.AcesysConversion.Common.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.Extensions.Options;
using System.Data;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Fls.AcesysConversion.Helpers.Database;

public partial class DbHelper
{
    private static DbHelper? instance; // Singleton
    private const string connectionString = @"Data Source=Mapping.db";
    private const string one2One = "O2O";
    private const string one2Many = "O2M";
    private const string many2One = "M2O";
    private const string delete = "DEL";
    private const string mandatory = "MAN";
    private readonly DataTable dtMaster = new("MASTER");
    private readonly DataTable dtMasterDefSel = new("MASTER_DEFAULT_SELECT");
    private readonly DataTable dtMasterExtSel = new("MASTER_EXTENDED_SELECT");
    private readonly DataTable dtMasterDefInt = new("MASTER_DEFAULT_INTERLOCK");
    private readonly DataTable dtMasterExtInt = new("MASTER_EXTENDED_INTERLOCK");
    private readonly DataTable dtFPMember = new("FP_MEMBER");
    private readonly DataTable dtDetail = new("DETAIL");
    private readonly DataTable dtControlRung = new("CONTROL_RUNGS");
    private readonly DataTable dtRungConversion = new("RUNG_CONVERSION");
    private readonly DataTable dtAnalogScanCounter = new("Analog_Scan_Counter");
    private readonly DataTable dtInterlock = new("Interlock");
    private readonly DataTable dtPointType = new("Point_Type");
    private readonly DataTable dtUDTSiemens = new("UDT_Siemens");
    private readonly DataTable dtFBSiemens = new("FB_Siemens");
    private readonly DataTable dtSFBSiemens = new("SFB_Siemens");
    private readonly Dictionary<string, DbHelper> dbHelperCache = new();
    private static int outerIndex = 0;
    private static int innerIndex = 0;
    private static int InterlockIndex;
    private static int CounterIndex;
    private static readonly Regex interlockRegex = new Regex(@"\[(\d+)\]");
    private static string Max_HMIInterlock = string.Empty;
    private static Dictionary<string, int> prefixLastIndex = new Dictionary<string, int>();
    private static int currentIndex = 1;
    private static int CurrentInterLockIndex = 1;
    private static int HMI_INTERLOCK_INDEX = 0;
    string LINK = "";
    string lastLink = null;
    private Dictionary<string, int> prefixIndexMap = new Dictionary<string, int>();
    string DosaxBusData = "RLXV7DOSAXA01_FB_BusData";

    protected DbHelper()
    {
    }

    public static DbHelper Instance
    {
        get
        {
            instance ??= new DbHelper();
            return instance;
        }
    }

    public DbHelper? GetDbHelper(int fromVersion, int toVersion, PlcManufacturer plc)
    {
        InterlockIndex = 1;
        string key = MakeKey(fromVersion, toVersion, plc.ToString());

        if (instance != null && !instance.dbHelperCache.ContainsKey(key))
        {
            DbHelper dbHelper = new();

            string sqlDetail = @$"SELECT m.id mid, d.id did, To_Object, Standard, 
                                    Faceplate_Decorated_Data, Addon_Decorated_Data,
                                    M21_Parent,Replacement_Type
                                    FROM MAPPING_MASTER m
                                    INNER JOIN MAPPING_DETAIL d ON m.id = d.parent
                                    WHERE m.from_version={fromVersion} AND m.to_version={toVersion}
                                    AND m.plc_type='{plc.ToString().ToUpper()}'";

            string sqlMaster = @$"SELECT m.id mid, Entity_Type, From_Object, Replacement_Type
                                    FROM MAPPING_MASTER m
                                    WHERE m.from_version = {fromVersion} AND 
                                    m.to_version = {toVersion} AND 
                                    m.plc_type = '{plc.ToString().ToUpper()}'
                                    AND m.extended_select=-1 and m.extended_interlock=-1";

            string sqlMasterDefaultSelect = @$"SELECT m.id mid, Entity_Type, From_Object, Replacement_Type
                                    FROM MAPPING_MASTER m
                                    WHERE m.from_version = {fromVersion} AND 
                                    m.to_version = {toVersion} AND 
                                    m.plc_type = '{plc.ToString().ToUpper()}'
                                    AND m.extended_select=0";

            string sqlMasterExtendedSelect = @$"SELECT m.id mid, Entity_Type, From_Object, Replacement_Type
                                    FROM MAPPING_MASTER m
                                    WHERE m.from_version = {fromVersion} AND 
                                    m.to_version = {toVersion} AND 
                                    m.plc_type = '{plc.ToString().ToUpper()}'
                                    AND m.extended_select=1";

            string sqlMasterDefaultInterlock = @$"SELECT m.id mid, Entity_Type, From_Object, Replacement_Type
                                    FROM MAPPING_MASTER m
                                    WHERE m.from_version = {fromVersion} AND 
                                    m.to_version = {toVersion} AND 
                                    m.plc_type = '{plc.ToString().ToUpper()}'
                                    AND m.extended_interlock=0";

            string sqlMasterExtendedInterlock = @$"SELECT m.id mid, Entity_Type, From_Object, Replacement_Type
                                    FROM MAPPING_MASTER m
                                    WHERE m.from_version = {fromVersion} AND 
                                    m.to_version = {toVersion} AND 
                                    m.plc_type = '{plc.ToString().ToUpper()}'
                                    AND m.extended_interlock=1";

            string sqlFpMembers = @$"SELECT MM.From_Object,
                                    MD.To_Object,
                                    FPM.From_Attribute,
                                    FPM.To_Attribute_MapByName,
                                    FPM.To_Attribute_MapByFunction
                                FROM FP_MEMBERS FPM 
                                INNER JOIN MAPPING_MASTER MM ON FPM.Parent = MM.Id 
                                INNER JOIN MAPPING_DETAIL MD ON MM.Id = MD.Parent and FPM.Parent = MD.Parent
                                WHERE FPM.From_Version=77 AND FPM.To_Version=80 AND FPM.Is_Attribute_Replace='X'";

            string sqlFPMember = $@"SELECT Id,Parent,From_FB,To_FB,
                                        From_Version,To_Version,From_Attribute,Entity,To_Attribute_MapByName,To_Attribute_MapByFunction,
                                        Is_Attribute_Replace
                                       FROM FP_MEMBER";

            string sqlAnalogControlRung = @$"SELECT Control_Rung_Name,Standard
                                        FROM CONTROL_RUNG";

            string sqlRungConversion = $@"SELECT Id,From_FB,
                                        From_Version,To_Version,From_Pin_Order,From_Pin_Name,To_FB,To_Pin_Order,
                                        To_Pin_Name,To_New_Operator,To_Rename,To_Operator_Type,To_Parameters
                                       FROM FB_PINS";

            string sqlAnalogScanCounter = $@"SELECT Id,From_Version,
                                        To_Version,Plc_Type,Block_Name,Key,Config_Data,
                                        Config_Data_Structure
                                       FROM AnalogScanCounter";

            string sqlInterlock = $@"SELECT Element,pLINK,pEN,IntSuffix                                        
                                   FROM Interlock";

            string sqlPointType = $@"SELECT Point_Type_V7,Type,Point_Type_V8,Remove,Append,Append_Data,Format,Format_Data,Language_Data                                   
                                   FROM ECS_Points";
            string sqlUDTSiemens = $@"SELECT From_V77_Number,From_V77_Name,To_V80_Number,To_V80_Name                                  
                                   FROM UDT_Siemens";
            string sqlFBSiemens = $@"SELECT V77_Number,V77_Name,V77_FC_DBIndex,V80_Number,V80_Name,V80_FC_DBIndex                                  
                                   FROM FB_Siemens";

            string sqlSFBSiemens = $@"SELECT V77_Number,V77_Name,V80_Number,V80_Name                                  
                                   FROM SFB_Siemens";

            using SqliteConnection conn = new(connectionString);
            using SqliteCommand cmd = conn.CreateCommand();
            conn.Open();

            cmd.CommandText = sqlMaster;
            using SqliteDataReader readerMaster = cmd.ExecuteReader();
            dbHelper.dtMaster.Load(readerMaster);

            cmd.CommandText = sqlMasterDefaultSelect;
            using SqliteDataReader readerMasterDefSel = cmd.ExecuteReader();
            dbHelper.dtMasterDefSel.Load(readerMasterDefSel);

            cmd.CommandText = sqlMasterExtendedSelect;
            using SqliteDataReader readerMasterExtSel = cmd.ExecuteReader();
            dbHelper.dtMasterExtSel.Load(readerMasterExtSel);

            cmd.CommandText = sqlMasterDefaultInterlock;
            using SqliteDataReader readerMasterDefInt = cmd.ExecuteReader();
            dbHelper.dtMasterDefInt.Load(readerMasterDefInt);

            cmd.CommandText = sqlMasterExtendedInterlock;
            using SqliteDataReader readerMasterExtInt = cmd.ExecuteReader();
            dbHelper.dtMasterExtInt.Load(readerMasterExtInt);

            cmd.CommandText = sqlDetail;
            using SqliteDataReader readerDetail = cmd.ExecuteReader();
            dbHelper.dtDetail.Load(readerDetail);

            cmd.CommandText = sqlFpMembers;
            using SqliteDataReader readerFpMembers = cmd.ExecuteReader();
            dbHelper.dtFpMembers.Load(readerFpMembers);

            cmd.CommandText = sqlFPMember;
            using SqliteDataReader readerFPMember = cmd.ExecuteReader();
            dbHelper.dtFPMember.Load(readerFPMember);

            cmd.CommandText = sqlAnalogControlRung;
            using SqliteDataReader readerAnalogControlRung = cmd.ExecuteReader();
            dbHelper.dtControlRung.Load(readerAnalogControlRung);

            cmd.CommandText = sqlRungConversion;
            using SqliteDataReader readerRungConversion = cmd.ExecuteReader();
            dbHelper.dtRungConversion.Load(readerRungConversion);

            cmd.CommandText = sqlAnalogScanCounter;
            using SqliteDataReader readerAnalogScanCounter = cmd.ExecuteReader();
            dbHelper.dtAnalogScanCounter.Load(readerAnalogScanCounter);

            cmd.CommandText = sqlInterlock;
            using SqliteDataReader readerInterlock = cmd.ExecuteReader();
            dbHelper.dtInterlock.Load(readerInterlock);

            cmd.CommandText = sqlPointType;
            using SqliteDataReader readerPointType = cmd.ExecuteReader();
            dbHelper.dtPointType.Load(readerPointType);

            cmd.CommandText = sqlUDTSiemens;
            using SqliteDataReader readerUDTSiemens = cmd.ExecuteReader();
            dbHelper.dtUDTSiemens.Load(readerUDTSiemens);

            cmd.CommandText = sqlFBSiemens;
            using SqliteDataReader readerFBSiemens = cmd.ExecuteReader();
            dbHelper.dtFBSiemens.Load(readerFBSiemens);

            cmd.CommandText = sqlSFBSiemens;
            using SqliteDataReader readerSFBSiemens = cmd.ExecuteReader();
            dbHelper.dtSFBSiemens.Load(readerSFBSiemens);

            instance.dbHelperCache.Add(key, dbHelper);
            return instance.dbHelperCache[key];
        }
        else
        {
            return instance?.dbHelperCache[key];
        }
    }

    private static string MakeKey(int fromVersion, int toVersion, string plc)
    {
        return $"{plc.ToLower()}_{fromVersion}_{toVersion}";
    }

    public List<string> GetToBeDeletedByEntityType(EntityTypes entityType)
    {
        IEnumerable<DataRow>? rows = dtMaster.AsEnumerable().Where(m => m.Field<long>("Entity_Type") == (long)entityType && m.Field<string>("Replacement_Type")!.Equals(delete));
        List<string> toBeDeleted = new();
        foreach (DataRow? masterRow in rows)
        {
            if (!string.IsNullOrEmpty(masterRow.Field<string>("From_Object")))
            {
                string? fromObject = masterRow.Field<string>("From_Object");
                if (!string.IsNullOrEmpty(fromObject))
                {
                    toBeDeleted.Add(fromObject);
                }
            }
        }
        return toBeDeleted;
    }

    private IEnumerable<DataRow> GetFilteredMasterRows(EntityTypes entityType, string replacementType, RockwellUpgradeOptions options)
    {
        IEnumerable<DataRow> rows = dtMaster.AsEnumerable()
            .Where(m => m.Field<long>("Entity_Type") == (long)entityType && m.Field<string>("Replacement_Type")!.StartsWith(replacementType));

        if (options.IsExtendedSelect)
        {
            IEnumerable<DataRow>? rowsExtSel = dtMasterExtSel.AsEnumerable()
                    .Where(m => m.Field<long>("Entity_Type") == (long)entityType && m.Field<string>("Replacement_Type")!.StartsWith(replacementType));

            rows = rows.Concat(rowsExtSel);
        }
        else
        {
            IEnumerable<DataRow>? rowsDefSel = dtMasterDefSel.AsEnumerable()
                    .Where(m => m.Field<long>("Entity_Type") == (long)entityType && m.Field<string>("Replacement_Type")!.StartsWith(replacementType));
            rows = rows.Concat(rowsDefSel);
        }

        if (options.IsExtendedInterlock)
        {
            IEnumerable<DataRow>? rowsExtInt = dtMasterExtInt.AsEnumerable()
                    .Where(m => m.Field<long>("Entity_Type") == (long)entityType && m.Field<string>("Replacement_Type")!.StartsWith(replacementType));

            rows = rows.Concat(rowsExtInt);
        }
        else
        {
            IEnumerable<DataRow>? rowsDefInt = dtMasterDefInt.AsEnumerable()
                    .Where(m => m.Field<long>("Entity_Type") == (long)entityType && m.Field<string>("Replacement_Type")!.StartsWith(replacementType));
            rows = rows.Concat(rowsDefInt);
        }

        return rows;
    }

    private List<Dto> GetOneToOneByEntityType(EntityTypes entityType, RockwellUpgradeOptions options)
    {
        IEnumerable<DataRow> rows = GetFilteredMasterRows(entityType, one2One, options);

        List<Dto> one2OneChanges = new();

        foreach (DataRow? masterRow in rows!)
        {
            if (!string.IsNullOrEmpty(masterRow.Field<string>("From_Object")))
            {
                DataRow detail = dtDetail.AsEnumerable().Where(d => d.Field<long>("mid") == masterRow.Field<long>("mid")).First();

                string fromObject = masterRow.Field<string>("From_Object") ?? string.Empty;
                string toObject = detail.Field<string>("To_Object") ?? string.Empty;
                string standard = detail.Field<string>("Standard") ?? string.Empty;

                one2OneChanges.Add(new Dto(fromObject, toObject, standard));
            }
        }
        return one2OneChanges;
    }

    private List<Dto> GetOneToOneTagsDataType(EntityTypes entityType, RockwellUpgradeOptions options)
    {
        IEnumerable<DataRow> rows = GetFilteredMasterRows(entityType, one2One, options);

        List<Dto> one2OneChanges = new();

        foreach (DataRow? masterRow in rows!)
        {
            if (!string.IsNullOrEmpty(masterRow.Field<string>("From_Object")))
            {
                DataRow detail = dtDetail.AsEnumerable().Where(d => d.Field<long>("mid") == masterRow.Field<long>("mid")).First();

                string fromObject = masterRow.Field<string>("From_Object") ?? string.Empty;
                string toObject = detail.Field<string>("To_Object") ?? string.Empty;
                string decoratedData = detail.Field<string>("Faceplate_Decorated_Data") ?? string.Empty;

                one2OneChanges.Add(new Dto(fromObject, toObject, decoratedData));
            }
        }
        return one2OneChanges;
    }

    private List<Dto> GetOneToOneTagsAddOns(EntityTypes entityType, RockwellUpgradeOptions options)
    {
        IEnumerable<DataRow> rows = GetFilteredMasterRows(entityType, one2One, options);

        List<Dto> one2OneChanges = new();

        foreach (DataRow? masterRow in rows!)
        {
            if (!string.IsNullOrEmpty(masterRow.Field<string>("From_Object")))
            {
                DataRow detail = dtDetail.AsEnumerable().Where(d => d.Field<long>("mid") == masterRow.Field<long>("mid")).First();

                string fromObject = masterRow.Field<string>("From_Object") ?? string.Empty;
                string toObject = detail.Field<string>("To_Object") ?? string.Empty;
                string decoratedData = detail.Field<string>("Addon_Decorated_Data") ?? string.Empty;

                one2OneChanges.Add(new Dto(fromObject, toObject, decoratedData));
            }
        }
        return one2OneChanges;
    }

    private List<Dto> GetMandatoryTagsAddOns(EntityTypes entityType, RockwellUpgradeOptions options)
    {
        IEnumerable<DataRow> rows = GetFilteredMasterRows(entityType, mandatory, options);

        List<Dto> mandatoryChanges = new();

        foreach (DataRow? masterRow in rows!)
        {
            if (!string.IsNullOrEmpty(masterRow.Field<string>("From_Object")))
            {
                DataRow? detail = dtDetail.AsEnumerable().Where(d => d.Field<long>("mid") == masterRow.Field<long>("mid")).FirstOrDefault();
                if (detail != null)
                {
                    string fromObject = masterRow.Field<string>("From_Object") ?? string.Empty;
                    string toObject = detail.Field<string>("To_Object") ?? string.Empty;
                    string decoratedData = detail.Field<string>("Addon_Decorated_Data") ?? string.Empty;

                    mandatoryChanges.Add(new Dto(fromObject, toObject, decoratedData));
                }
            }
        }
        return mandatoryChanges;
    }

    private List<Dto> GetOneToOneTagsHmiTags(EntityTypes entityType, RockwellUpgradeOptions options)
    {
        IEnumerable<DataRow> rows = GetFilteredMasterRows(entityType, one2One, options);

        List<Dto> one2OneChanges = new();

        foreach (DataRow? masterRow in rows!)
        {
            if (!string.IsNullOrEmpty(masterRow.Field<string>("From_Object")))
            {
                DataRow detail = dtDetail.AsEnumerable().Where(d => d.Field<long>("mid") == masterRow.Field<long>("mid")).First();

                string fromObject = masterRow.Field<string>("From_Object") ?? string.Empty;
                string toObject = detail.Field<string>("To_Object") ?? string.Empty;
                string hmiTagStandard = detail.Field<string>("Standard") ?? string.Empty;

                one2OneChanges.Add(new Dto(fromObject, toObject, hmiTagStandard));
            }
        }
        return one2OneChanges;
    }

    private List<Dto> GetOneToManyTagsHmiTags(EntityTypes entityType, RockwellUpgradeOptions options)
    {
        IEnumerable<DataRow> rows = GetFilteredMasterRows(entityType, one2Many, options);

        List<Dto> one2ManyChanges = new();

        foreach (DataRow? masterRow in rows)
        {
            if (!string.IsNullOrEmpty(masterRow.Field<string>("From_Object")))
            {
                IEnumerable<DataRow>? detail = dtDetail.AsEnumerable().Where(d => d.Field<long>("mid") == masterRow.Field<long>("mid"));
                foreach (DataRow? d in detail)
                {
                    string fromObject = masterRow.Field<string>("From_Object") ?? string.Empty;
                    string toObject = d.Field<string>("To_Object") ?? string.Empty;
                    string decoratedData = d.Field<string>("Standard") ?? string.Empty;

                    one2ManyChanges.Add(new Dto(fromObject, toObject, decoratedData));
                }
            }
        }
        return one2ManyChanges;
    }

    private List<Dto> GetOneToManyByEntityType(EntityTypes entityType, RockwellUpgradeOptions options)
    {
        IEnumerable<DataRow> rows = GetFilteredMasterRows(entityType, one2Many, options);

        List<Dto> one2ManyChanges = new();

        foreach (DataRow? masterRow in rows)
        {
            if (!string.IsNullOrEmpty(masterRow.Field<string>("From_Object")))
            {
                IEnumerable<DataRow>? detail = dtDetail.AsEnumerable().Where(d => d.Field<long>("mid") == masterRow.Field<long>("mid"));
                foreach (DataRow? d in detail)
                {
                    string fromObject = masterRow.Field<string>("From_Object") ?? string.Empty;
                    string toObject = d.Field<string>("To_Object") ?? string.Empty;
                    string standard = d.Field<string>("Standard") ?? string.Empty;

                    one2ManyChanges.Add(new Dto(fromObject, toObject, standard));
                }
            }
        }
        return one2ManyChanges;
    }

    private List<Dto> GetOneToManyTagsDataType(EntityTypes entityType, RockwellUpgradeOptions options)
    {
        IEnumerable<DataRow> rows = GetFilteredMasterRows(entityType, one2Many, options);

        List<Dto> one2ManyChanges = new();

        foreach (DataRow? masterRow in rows)
        {
            if (!string.IsNullOrEmpty(masterRow.Field<string>("From_Object")))
            {
                IEnumerable<DataRow>? detail = dtDetail.AsEnumerable().Where(d => d.Field<long>("mid") == masterRow.Field<long>("mid"));
                foreach (DataRow? d in detail)
                {
                    string fromObject = masterRow.Field<string>("From_Object") ?? string.Empty;
                    string toObject = d.Field<string>("To_Object") ?? string.Empty;
                    string decoratedData = d.Field<string>("Faceplate_Decorated_Data") ?? string.Empty;

                    one2ManyChanges.Add(new Dto(fromObject, toObject, decoratedData));
                }
            }
        }
        return one2ManyChanges;
    }

    private List<Dto> GetMandatoryHmiTags(EntityTypes entityType, RockwellUpgradeOptions options)
    {
        IEnumerable<DataRow> rows = GetFilteredMasterRows(entityType, mandatory, options);

        List<Dto> mandatoryTypes = new();

        foreach (DataRow? masterRow in rows)
        {
            if (!string.IsNullOrEmpty(masterRow.Field<string>("From_Object")))
            {
                IEnumerable<DataRow>? detail = dtDetail.AsEnumerable().Where(d => d.Field<long>("mid") == masterRow.Field<long>("mid"));
                foreach (DataRow? d in detail)
                {
                    string fromObject = masterRow.Field<string>("From_Object") ?? string.Empty;
                    string toObject = d.Field<string>("To_Object") ?? string.Empty;
                    string decoratedData = d.Field<string>("Standard") ?? string.Empty;

                    mandatoryTypes.Add(new Dto(fromObject, toObject, decoratedData));
                }
            }
        }
        return mandatoryTypes;
    }

    private List<string> GetDeletedHmiTags(EntityTypes entityType, RockwellUpgradeOptions options)
    {
        IEnumerable<DataRow> rows = GetFilteredMasterRows(entityType, delete, options);

        List<string> toBeDeleted = new();

        foreach (DataRow? masterRow in rows)
        {
            if (!string.IsNullOrEmpty(masterRow.Field<string>("From_Object")))
            {
                string? fromObject = masterRow.Field<string>("From_Object");
                if (!string.IsNullOrEmpty(fromObject))
                {
                    toBeDeleted.Add(fromObject);
                }
            }
        }
        return toBeDeleted;
    }

    private List<Dto> GetOneToManyTagsAddOns(EntityTypes entityType, RockwellUpgradeOptions options)
    {
        IEnumerable<DataRow> rows = GetFilteredMasterRows(entityType, one2Many, options);

        List<Dto> one2ManyChanges = new();

        foreach (DataRow? masterRow in rows)
        {
            if (!string.IsNullOrEmpty(masterRow.Field<string>("From_Object")))
            {
                IEnumerable<DataRow>? detail = dtDetail.AsEnumerable().Where(d => d.Field<long>("mid") == masterRow.Field<long>("mid"));
                foreach (DataRow? d in detail)
                {
                    string fromObject = masterRow.Field<string>("From_Object") ?? string.Empty;
                    string toObject = d.Field<string>("To_Object") ?? string.Empty;
                    string decoratedData = d.Field<string>("Addon_Decorated_Data") ?? string.Empty;

                    one2ManyChanges.Add(new Dto(fromObject, toObject, decoratedData));
                }
            }
        }
        return one2ManyChanges;
    }

    public Dictionary<string, string> GetMandatoryByEntityType(EntityTypes entityType, RockwellUpgradeOptions options)
    {
        IEnumerable<DataRow> rows = GetFilteredMasterRows(entityType, mandatory, options);

        Dictionary<string, string> mandatoryChanges = new();

        foreach (DataRow? masterRow in rows)
        {
            if (!string.IsNullOrEmpty(masterRow.Field<string>("From_Object")))
            {
                DataRow? detail = dtDetail.AsEnumerable().Where(d => d.Field<long>("mid") == masterRow.Field<long>("mid")).FirstOrDefault();
                if (detail != null)
                {
                    string toObject = detail.Field<string>("To_Object") ?? string.Empty;
                    string standard = detail.Field<string>("Standard") ?? string.Empty;
                    mandatoryChanges.Add(toObject, standard);
                }
            }
        }
        return mandatoryChanges;
    }

    public List<Dto> GetManyToOneByEntityType(EntityTypes entityType, RockwellUpgradeOptions options)
    {
        IEnumerable<DataRow> rows = GetFilteredMasterRows(entityType, many2One, options);

        List<Dto> many2OneChanges = new();

        foreach (DataRow? masterRow in rows)
        {
            IEnumerable<DataRow>? details = dtDetail.AsEnumerable().Where(d => d.Field<long>("mid") == masterRow.Field<long>("mid"));

            string fromObject = masterRow.Field<string>("From_Object") ?? string.Empty;
            foreach (DataRow? d in details)
            {
                string toObject = d.Field<string>("To_Object") ?? string.Empty;
                string standard = d.Field<string>("Standard") ?? string.Empty;
                many2OneChanges.Add(new Dto(fromObject, toObject, standard));
            }
        }
        return many2OneChanges;
    }

    public List<Dto> GetOneToOneDataTypes(RockwellUpgradeOptions options)
    {
        return GetOneToOneByEntityType(EntityTypes.DataType, options);
    }

    public Dictionary<string, string> GetMandatoryDataTypes(RockwellUpgradeOptions options)
    {
        return GetMandatoryByEntityType(EntityTypes.DataType, options);
    }

    public List<Dto> GetOneToManyDataTypes(RockwellUpgradeOptions options)
    {
        return GetOneToManyByEntityType(EntityTypes.DataType, options);
    }

    public List<Dto> GetManyToOneDataTypes(RockwellUpgradeOptions options)
    {
        return GetManyToOneByEntityType(EntityTypes.DataType, options);
    }

    public List<string> GetToBeDeletedDataTypes()
    {
        return GetToBeDeletedByEntityType(EntityTypes.DataType);
    }

    public List<Dto> GetOneToOneAddOnInstructionDefinitions(RockwellUpgradeOptions options)
    {
        return GetOneToOneByEntityType(EntityTypes.AddOnInstruction, options);
    }

    public List<Dto> GetOneToManyAddOnInstructionDefinitions(RockwellUpgradeOptions options)
    {
        return GetOneToManyByEntityType(EntityTypes.AddOnInstruction, options);
    }

    public Dictionary<string, string> GetMandatoryAddOnInstructionDefinitions(RockwellUpgradeOptions options)
    {
        return GetMandatoryByEntityType(EntityTypes.AddOnInstruction, options);
    }

    public List<Dto> GetManyToOneAddOnInstructionDefinitions(RockwellUpgradeOptions options)
    {
        return GetManyToOneByEntityType(EntityTypes.AddOnInstruction, options);
    }

    public List<string> GetToBeDeletedAddOnInstructionDefinitions()
    {
        return GetToBeDeletedByEntityType(EntityTypes.AddOnInstruction);
    }

    public List<Dto> GetOneToOneTagsDataType(RockwellUpgradeOptions options)
    {
        return GetOneToOneTagsDataType(EntityTypes.DataType, options);  // intentional get data type and not tags 
    }

    public List<Dto> GetOneToOneTagsAddOns(RockwellUpgradeOptions options)
    {
        return GetOneToOneTagsAddOns(EntityTypes.AddOnInstruction, options);  // intentional get addon and not tags 
    }

    public List<Dto> GetOneToManyTagsDataTypes(RockwellUpgradeOptions options)
    {
        return GetOneToManyTagsDataType(EntityTypes.DataType, options);  // intentional get data type and not tags 
    }

    public List<Dto> GetOneToManyTagsAddOns(RockwellUpgradeOptions options)
    {
        return GetOneToManyTagsAddOns(EntityTypes.AddOnInstruction, options);  // intentional get addon and not tags 
    }

    public List<Dto> GetMandatoryTagsAddOns(RockwellUpgradeOptions options)
    {
        return GetMandatoryTagsAddOns(EntityTypes.AddOnInstruction, options);  // intentional get addon and not tags 
    }

    public List<Dto> GetOneToOneTagsHmiTags(RockwellUpgradeOptions options)
    {
        return GetOneToOneTagsHmiTags(EntityTypes.HmiTags, options);
    }

    public List<Dto> GetOneToManyTagsHmiTags(RockwellUpgradeOptions options)
    {
        return GetOneToManyTagsHmiTags(EntityTypes.HmiTags, options);
    }

    public List<Dto> GetMandatoryHmiTags(RockwellUpgradeOptions options)
    {
        return GetMandatoryHmiTags(EntityTypes.HmiTags, options);
    }

    public List<string> GetDeletedHmiTags(RockwellUpgradeOptions options)
    {
        return GetDeletedHmiTags(EntityTypes.HmiTags, options);
    }

    public List<Dto> GetOneToOneMainRoutines(RockwellUpgradeOptions options)
    {
        return GetOneToOneMainRoutines(EntityTypes.MainRoutine, options);
    }

    public List<Dto> GetOneToManyMainRoutines(RockwellUpgradeOptions options)
    {
        return GetOneToManyByEntityType(EntityTypes.MainRoutine, options);
    }

    public Dictionary<string, string> GetMandatoryPrograms(RockwellUpgradeOptions options)
    {
        return GetMandatoryByEntityType(EntityTypes.MainRoutine, options);
    }

    private List<Dto> GetOneToOneMainRoutines(EntityTypes entityType, RockwellUpgradeOptions options)
    {
        IEnumerable<DataRow> rows = GetFilteredMasterRows(entityType, one2One, options);

        List<Dto> one2OneChanges = new();

        foreach (DataRow? masterRow in rows!)
        {
            if (!string.IsNullOrEmpty(masterRow.Field<string>("From_Object")))
            {
                DataRow detail = dtDetail.AsEnumerable().Where(d => d.Field<long>("mid") == masterRow.Field<long>("mid")).First();

                string fromObject = masterRow.Field<string>("From_Object") ?? string.Empty;
                string toObject = detail.Field<string>("To_Object") ?? string.Empty;

                one2OneChanges.Add(new Dto(fromObject, toObject, string.Empty));
            }
        }
        return one2OneChanges;
    }



    private List<Dto> GetOneToManyMainRoutines(EntityTypes entityType, RockwellUpgradeOptions options)
    {
        IEnumerable<DataRow> rows = GetFilteredMasterRows(entityType, one2Many, options);

        List<Dto> one2ManyChanges = new();

        foreach (DataRow? masterRow in rows!)
        {
            if (!string.IsNullOrEmpty(masterRow.Field<string>("From_Object")))
            {
                DataRow detail = dtDetail.AsEnumerable().Where(d => d.Field<long>("mid") == masterRow.Field<long>("mid")).First();

                string fromObject = masterRow.Field<string>("From_Object") ?? string.Empty;
                string toObject = detail.Field<string>("To_Object") ?? string.Empty;

                one2ManyChanges.Add(new Dto(fromObject, toObject, string.Empty));
            }
        }
        return one2ManyChanges;
    }

    public List<string> GetAssociatedRoutines(string mainRoutineName, RockwellUpgradeOptions options)
    {
        IEnumerable<DataRow> rows = dtDetail.AsEnumerable().Where(d => d.Field<string>("From_Object") == mainRoutineName);
        List<string> associatedRoutines = new();

        foreach (DataRow row in rows)
        {
            string associatedRoutine = row.Field<string>("To_Object") ?? string.Empty;
            associatedRoutines.Add(associatedRoutine);
        }

        return associatedRoutines;
    }

    public string GetControlRung(string dataType)
    {
        IEnumerable<DataRow> rows = dtControlRung.AsEnumerable().Where(d => d.Field<string>("Control_Rung_Name") == dataType);
        string Standard = "";

        foreach (DataRow row in rows)
        {
            Standard = row.Field<string>("Standard") ?? string.Empty;
        }

        return Standard;
    }

    public string? GetPowerOperand(string newOperator, string oldText, string dataType)
    {

        // Select rows where To_FB matches newOperator and From_Pin_Name is 'PWR'
        DataRow[] AsysAlarm_Rows = dtRungConversion.Select($"To_FB = '{newOperator}' AND From_Pin_Name = 'PWR'");

        if (AsysAlarm_Rows.Length > 0)
        {
            string fromPinOrderString = AsysAlarm_Rows[0]["From_Pin_Order"]?.ToString();
            int fromPinOrder = 0;
            // Extract the From_Pin_Order value
            if (!string.IsNullOrEmpty(fromPinOrderString))
            {
                fromPinOrder = Convert.ToInt32(fromPinOrderString);
            }

            // Check if fromPinOrder is within the valid range
            if (fromPinOrder >= 2 && fromPinOrder <= 40)
            {
                // Generate the pattern dynamically based on fromPinOrder
                string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))}),";
                Match match = Regex.Match(oldText, pattern);

                if (match.Success)
                {
                    // Extract the operand based on fromPinOrder
                    var pwr_operand = match.Groups[fromPinOrder].Value.Trim();
                    string pwr_value = pwr_operand.ToString();
                    return pwr_value;
                }
            }
        }


        // Return null if conditions are not met or match is unsuccessful
        return null;
    }


    public string GetOperandsForOperator(string newOperator, string oldText, string dataType, RockwellUpgradeOptions options, string RoutineName, List<string> AsysPLCRcvValues, string wrappedHMIUnit, string Instance, int HMI_INTERLOCK_MAX_GROUP)
    {       
        string firstOperand = "";
        string secondOperand = "";
        string thirdOperand = "";
        string fourthOperand = "";
        string fifthOperand = "";
        string sixthOperand = "";
        string seventhOperand = "";
        string eighthOperand = "";
        string ninthOperand = "";
        string tenthOperand = "";
        string eleventhOperand = "";
        string twelfthOperand = "";
        string thirteenthOperand = "";
        string fourteenthOperand = "";
        string fifteenthOperand = "";
        string sixteenthOperand = "";
        string seventeenthOperand = "";
        string eighteenthOperand = "";
        string nineteenthOperand = "";
        string twentiethOperand = "";
        string twentyFirstOperand = "";
        string twentySecondOperand = "";
        string twentyThirdOperand = "";
        string twentyFourthOperand = "";
        string twentyFifthOperand = "";
        string twentySixthOperand = "";
        string twentySeventhOperand = "";
        string twentyEighthOperand = "";
        string twentyNinthOperand = "";
        string thirtiethOperand = "";
        string thirtyFirstOperand = "";
        string thirtySecondOperand = "";
        string thirtyThirdOperand = "";
        string thirtyFourthOperand = "";
        string thirtyFifthOperand = "";
        string thirtySixthOperand = "";
        string newOperand = "";

        List<string> listfp = new List<string>();

        if (dataType == "Bimotor" || dataType == "Unimotor")
        {
            string NewDataType = "Motor";
            string fp = "ACESYS_FACEPLATE_" + NewDataType.ToUpper();            
            listfp.Add(fp);
        }

        else
        {
            string fp = "ACESYS_FACEPLATE_" + dataType.ToUpper();            
            listfp.Add(fp);
        }        


        string oldoperatotpattern = @"^\w+(?=\()";

        Match oldoperatormatch = Regex.Match(oldText, oldoperatotpattern);

        string oldOperatorName = "";

        if (oldoperatormatch.Success)
        {
            // Extract the matched operator
            oldOperatorName = oldoperatormatch.Value;
        }        

        DataRow[] rows;

        if (dataType == "AsysSel" && options.IsExtendedSelect)
        {
            // Select rows where To_FB matches 'AsysExtSelect' and To_Pin_Order is not null or empty
            rows = dtRungConversion.Select($"To_FB = 'AsysExtSelect' AND To_Pin_Order IS NOT NULL AND To_Pin_Order <> ''");
        }
        else if (newOperator == "AsysComm")
        {
            rows = dtRungConversion.Select($"To_FB = '{newOperator}' AND To_Pin_Order IS NOT NULL AND To_Pin_Order <> ''");
        }
        else if (newOperator == "AsysPLCSend")
        {
            rows = dtRungConversion.Select($"To_FB = '{newOperator}' AND To_Pin_Order IS NOT NULL AND To_Pin_Order <> ''");
        }
        else if (oldOperatorName == "Sim_ExtMot")
        {
            oldOperatorName = "Sim_ExtMotor";
            rows = dtRungConversion.Select($"From_FB = '{oldOperatorName}' AND To_Pin_Order IS NOT NULL AND To_Pin_Order <> ''");
        }
        
        else
        {
            // Select rows where From_FB matches oldOperatorName and To_Pin_Order is not null or empty
            rows = dtRungConversion.Select($"From_FB = '{oldOperatorName}' AND To_Pin_Order IS NOT NULL AND To_Pin_Order <> ''");

            string pattern = @"(ADPT)(.*)(_MOTOR|_GATE)";

            if (!rows.Any() && Regex.IsMatch(oldOperatorName, pattern))
            {
                // Replace the content between "ADPT" and either "_MOTOR" or "_GATE" with "xxxx"
                oldOperatorName = Regex.Replace(oldOperatorName, pattern, "ADPTxxxx$3");
                rows = dtRungConversion.Select($"From_FB = '{oldOperatorName}' AND To_Pin_Order IS NOT NULL AND To_Pin_Order <> ''");
            }

        }

        if (rows.Length == 0)
        {
            return "";
        }

        var Rows = rows.ToList();

        // First operand

        firstOperand = Rows[0]["To_Rename"].ToString();

        if (newOperator == "AsysPLCSend")
        {
            firstOperand = "PLC_SEND_" + Instance;
        }

        if (firstOperand == "Rule_Keep")
        {
            int fromPinOrder = Convert.ToInt32(Rows[0]["From_Pin_Order"]);

            // Check if fromPinOrder is within the valid range
            if (fromPinOrder >= 1 && fromPinOrder <= 40)
            {
                string pattern = string.Empty;

                if (newOperator == "Sim_Dosax" || newOperator == "Sim_Motor" || newOperator == "Sim_Gate" || newOperator == "Sim_Alarm" || newOperator == "Sim_Valve" || newOperator == "Sim_Schenck"
                     || newOperator == "Sim_Alarm")
                {
                    pattern = @"\(([^,]+),([^,]+)\)";
                }

                else
                {
                    pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                }

                Match match = Regex.Match(oldText, pattern);

                if (match.Success)
                {
                    // Extract the operand based on fromPinOrder
                    firstOperand = match.Groups[fromPinOrder].Value.Trim();
                    firstOperand = GetElementAndSubElementReplacement(firstOperand, listfp, options);
                    firstOperand = firstOperand.Replace("(", "").Replace(")", "");
                }
            }
        }

        // Second operand
        if (Rows.Count > 1 && Convert.ToInt32(Rows[1]["To_Pin_Order"]) == 2 || Rows.Count > 2 && Convert.ToInt32(Rows[2]["To_Pin_Order"]) == 2)
        {
            if (Rows[1]["To_Operator_Type"].ToString() == "constant")
            {
                secondOperand = Rows[1]["To_New_Operator"].ToString();
            }
            else if (Rows[1]["To_Rename"].ToString() == "Rule_2")
            {
                string pattern = @"(\w+)_CMD";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    secondOperand = $@"\{prefix}.Dept_LINK";
                }
            }

            else if (Rows[1]["To_Rename"].ToString() == "Rule_Keep")
            {
                // Pattern to capture up to 40 operands
                int fromPinOrder = Convert.ToInt32(Rows[1]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 40)
                {
                    string pattern = string.Empty;

                    if (newOperator == "Sim_Dosax" || newOperator == "Sim_Motor" || newOperator == "Sim_Gate" || newOperator == "Sim_Alarm" || newOperator == "Sim_Valve" || newOperator == "Sim_Schenck"
                         || newOperator == "Sim_Alarm")
                    {
                        pattern = $@"\(([^,]+(?:,([^,]+)){{{fromPinOrder - 1}}})\)";
                    }

                    else
                    {
                        pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    }
                   
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        secondOperand = match.Groups[fromPinOrder].Value.Trim();

                        secondOperand = GetElementAndSubElementReplacement(secondOperand, listfp, options);
                        secondOperand = secondOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[1]["To_Rename"].ToString() == "WatchDog")
            {
                secondOperand = Rows[1]["To_Pin_Name"].ToString();
            }

            else if (Rows[1]["To_Rename"].ToString() == "Master_Link")
            {
                secondOperand = Rows[1]["To_Pin_Name"].ToString();
            }

            else if (Rows[1]["To_Rename"].ToString() == "Rule_8")
            {
                string pattern = @"(\w+)_CMD";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    secondOperand = $@"\{prefix}.Grp_CMD";
                }
            }

            else if (Rows[1]["To_Operator_Type"].ToString() == "SINT")
            {
                secondOperand = Rows[1]["To_New_Operator"].ToString();
            }

            else if (Rows[1]["To_Pin_Name"].ToString() == "MSW")
            {
                string pattern = "";
                // Dynamically create the pattern using the dataType
                if (dataType == "AsysSel")
                {
                    pattern = $@"HMI_SELECT\[\d+\]";
                }
                else if (dataType == "Sequence" || dataType == "MultiDivider" || dataType == "AsysComp")
                {
                    pattern = $@"HMI_UNIT\[\d+\]";
                }
                else
                {
                    pattern = $@"HMI_{dataType.ToUpper()}\[\d+\]";
                }

                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    secondOperand = match.Value;
                }
            }

            else if (Rows[1]["To_Rename"].ToString() == "Rule_16")
            {
                string pattern = @"HMI_PID\[(\d+)\]\.SPA";
                Match match = Regex.Match(oldText, pattern);

                if (match.Success)
                {
                    string indexValue = match.Groups[1].Value;
                    secondOperand = $"HMI_PID_SPA[{indexValue}]";
                }
            }

            if (newOperator == "AsysPLCRcv")
            {
                secondOperand = AsysPLCRcvValues[0];
            }
            
        }

        // Third operand
        if (Rows.Count > 2 && Convert.ToInt32(Rows[2]["To_Pin_Order"]) == 3 || Rows.Count > 3 && Convert.ToInt32(Rows[3]["To_Pin_Order"]) == 3)
        {
            if (Rows[2]["To_Operator_Type"].ToString() == "constant")
            {
                thirdOperand = Rows[2]["To_New_Operator"].ToString();
            }

            else if (Rows[2]["To_Operator_Type"].ToString() == "SINT")
            {
                thirdOperand = Rows[2]["To_New_Operator"].ToString();
            }

            else if (Rows[2]["To_Rename"].ToString() == "Master_Link")
            {
                thirdOperand = Rows[2]["To_Pin_Name"].ToString();
            }

            else if (Rows[2]["To_Rename"].ToString().ToUpper() == "DEPT_LINK")
            {
                thirdOperand = Rows[2]["To_Rename"].ToString();
            }

            else if ((Rows[2]["To_Rename"].ToString() == "Rule_Keep") && ((Rows[2]["To_Pin_Name"].ToString() != "FACEPLATE")))
            {
                // Pattern to capture up to 40 operands
                int fromPinOrder = Convert.ToInt32(Rows[2]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 40)
                {
                    string pattern = "";
                    if (newOperator == "Sim_Pfister")
                    {
                        pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})\)";
                    }

                    else
                    {
                        pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    }
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        thirdOperand = match.Groups[fromPinOrder].Value.Trim();
                        thirdOperand = GetElementAndSubElementReplacement(thirdOperand, listfp, options);
                        thirdOperand = thirdOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[2]["To_Pin_Name"].ToString() == "FACEPLATE")
            {
                if (dataType == "Sequence" || dataType == "MultiDivider" || dataType == "AsysComp")
                {
                    string pattern2 = @"(\w+)_FP";
                    Match match2 = Regex.Match(oldText, pattern2);
                    if (match2.Success)
                    {
                        string prefix = match2.Groups[1].Value;
                        thirdOperand = $"{prefix}_FP";
                    }
                }
                else
                {
                    string pattern = @"(\w+)_FB";
                    Match match = Regex.Match(oldText, pattern);
                    if (match.Success)
                    {
                        string prefix = match.Groups[1].Value;
                        thirdOperand = $"{prefix}_FP";
                    }
                }
                
            }

            else if (Rows[2]["To_Rename"].ToString() == "Rule_16")
            {
                string pattern = @"HMI_PID\[(\d+)\]\.SPM";
                Match match = Regex.Match(oldText, pattern);

                if (match.Success)
                {
                    string indexValue = match.Groups[1].Value;
                    thirdOperand = $"HMI_PID_SPM[{indexValue}]";
                }
            }

            if (newOperator == "AsysPLCRcv")
            {
                thirdOperand = AsysPLCRcvValues[1];
            }

            //Rule_20
            if (newOperator == "Sim_Dosax")
            {
                thirdOperand = DosaxBusData;
            }
        }

        // Fourth operand
        if (Rows.Count > 3 && Convert.ToInt32(Rows[3]["To_Pin_Order"]) == 4 || Rows.Count > 3 && Convert.ToInt32(Rows[7]["To_Pin_Order"]) == 4 || Rows.Count > 4 && Convert.ToInt32(Rows[4]["To_Pin_Order"]) == 4)
        {            
            if (!string.IsNullOrEmpty(Rows[3]["To_Operator_Type"].ToString()))
            {
                fourthOperand = Rows[3]["To_New_Operator"].ToString();
            }
            
            
            else if (Rows[3]["To_Pin_Name"].ToString() == "MSW")
            {
                // Dynamically create the pattern using the dataType

                string pattern = string.Empty;

                if (dataType == "Motor" || dataType == "Recipe")
                {
                    pattern = $@"HMI_UNIT\[\d+\]";
                }
                else
                {
                    pattern = $@"HMI_{dataType.ToUpper()}\[\d+\]";
                }

                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    fourthOperand = match.Value;
                }
            }

            else if (Rows[3]["To_Operator_Type"].ToString() == "SINT")
            {
                fourthOperand = Rows[3]["To_New_Operator"].ToString();
            }                   

            else if (Rows[3]["To_Rename"].ToString() == "Rule_Keep")
            {
                // Pattern to capture up to 40 operands
                int fromPinOrder = Convert.ToInt32(Rows[3]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 40)
                {
                    string pattern = string.Empty;
                    if (newOperator == "Sim_Analog")
                    {
                        pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})\)";
                    }
                    else
                    {
                        pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))}),";
                    }
                    
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        fourthOperand = match.Groups[fromPinOrder].Value.Trim();
                        fourthOperand = GetElementAndSubElementReplacement(fourthOperand, listfp, options);
                        fourthOperand = fourthOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }
            else if (Rows[3]["To_Pin_Name"].ToString() == "TOKEN")
            {
                if (dataType == "Totalizer")
                {
                    fourthOperand = $@"\{RoutineName}.Token";
                }
                else if (dataType == "Sequence")
                {
                    string pattern2 = @"(\w+)_TOKEN";
                    Match match = Regex.Match(oldText, pattern2);
                    if (match.Success)
                    {
                        string prefix = match.Groups[1].Value;
                        fourthOperand = $@"\{prefix}.Token";
                    }
                }
                else
                {
                    string pattern = @"(\w+)_TK";
                    Match match = Regex.Match(oldText, pattern);
                    if (match.Success)
                    {
                        string prefix = match.Groups[1].Value;
                        fourthOperand = $@"\{prefix}.Token";
                    }
                }
                
            }

            else if (Rows[3]["To_Rename"].ToString() == "Rule_16")
            {
                string pattern = @"HMI_PID_HLC\[(\d+)\]\.SPA";
                Match match = Regex.Match(oldText, pattern);

                if (match.Success)
                {
                    string indexValue = match.Groups[1].Value;
                    fourthOperand = $"HMI_PID_SPA_HLC[{indexValue}]";
                }
            }

            if (dataType == "Analog" && Rows.Count > 7)
            {
                if (Rows[7]["To_Operator_Type"].ToString() == "BOOL")
                {
                    fourthOperand = Rows[7]["To_New_Operator"].ToString();
                }
            }

            if (newOperator == "AsysPLCRcv")
            {
                fourthOperand = AsysPLCRcvValues[2];
            }

            if (newOperator == "AsysPLCSend")
            {
                fourthOperand = Instance + "_Send";
            }

        }

        // Fifth operand
        if ((Rows.Count > 4 && Convert.ToInt32(Rows[4]["To_Pin_Order"]) == 5) || (Rows.Count > 4 && Convert.ToInt32(Rows[3]["To_Pin_Order"]) == 5) || Rows.Count > 4 && Convert.ToInt32(Rows[5]["To_Pin_Order"]) == 5)
        {
            if ((Rows[4]["To_Operator_Type"].ToString() == "constant"))
            {
                fifthOperand = Rows[4]["To_New_Operator"].ToString();
            }
            else if (Rows[4]["To_Pin_Name"].ToString() == "FACEPLATE")
            {
                string pattern = @"(\w+)_FB";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    fifthOperand = $"{prefix}_FP";
                }
            }            

            else if (Rows[4]["To_Operator_Type"].ToString() == "SINT")
            {
                fifthOperand = Rows[4]["To_New_Operator"].ToString();
            }

            else if (Rows[4]["To_Rename"].ToString() == "Master_Link")
            {
                fifthOperand = Rows[4]["To_Pin_Name"].ToString();
            }

            else if (Rows[4]["To_Rename"].ToString() == "Rule_Keep")
            {
                // Pattern to capture up to 40 operands
                int fromPinOrder = Convert.ToInt32(Rows[4]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 40)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        fifthOperand = match.Groups[fromPinOrder].Value.Trim();
                        fifthOperand = GetElementAndSubElementReplacement(fifthOperand, listfp, options);
                        fifthOperand = fifthOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[4]["To_Pin_Name"].ToString() == "MSW")
            {
                // Dynamically create the pattern using the dataType
                if (dataType == "AsysSel")
                {
                    dataType = "Select";
                }
                string pattern = $@"HMI_{dataType.ToUpper()}\[\d+\]";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    fifthOperand = match.Value;

                }
            }

            else if (Rows[4]["To_Rename"].ToString() == "Rule_16")
            {
                string pattern = @"HMI_PID_HLC\[(\d+)\]\.SPM";
                Match match = Regex.Match(oldText, pattern);

                if (match.Success)
                {
                    string indexValue = match.Groups[1].Value;
                    fifthOperand = $"HMI_PID_SPM_HLC[{indexValue}]";
                }
            }

            if (dataType == "Analog")
            {
                if ((Rows[3]["To_Operator_Type"].ToString() == "constant"))
                {
                    fifthOperand = Rows[3]["To_New_Operator"].ToString();
                }
            }

            if (newOperator == "AsysPLCRcv")
            {
                if (Rows[5]["To_Rename"].ToString() == "Master_Link")
                {
                    fifthOperand = Rows[5]["To_Rename"].ToString();
                }
            }

        }

        // Sixth operand
        if (Rows.Count > 5 && Convert.ToInt32(Rows[5]["To_Pin_Order"]) == 6 || Rows.Count > 5 && Convert.ToInt32(Rows[4]["To_Pin_Order"]) == 6 || Rows.Count > 2 && Convert.ToInt32(Rows[1]["To_Pin_Order"]) == 6)
        {           

            if (Rows[5]["To_Pin_Name"].ToString() == "MSW")
            {
                // Dynamically create the pattern using the dataType
                string pattern = $@"HMI_{dataType.ToUpper()}\[\d+\]";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    sixthOperand = match.Value;
                }
            }
            else if (Rows[5]["To_Pin_Name"].ToString() == "TOKEN")
            {
                string pattern = @"(\w+)_TK";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    sixthOperand = $@"\{prefix}.Token";
                }
            }

            else if ((Rows[5]["To_Rename"].ToString() == "Rule_Keep") && (Rows[5]["To_Operator_Type"].ToString() != string.Empty))
            {
                // Pattern to capture up to 40 operands
                int fromPinOrder = Convert.ToInt32(Rows[5]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 40)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        sixthOperand = match.Groups[fromPinOrder].Value.Trim();
                        sixthOperand = GetElementAndSubElementReplacement(sixthOperand, listfp, options);
                        sixthOperand = sixthOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[5]["To_Operator_Type"].ToString() == "SINT")
            {
                sixthOperand = Rows[5]["To_New_Operator"].ToString();
            }

            else if (Rows[5]["To_Rename"].ToString() == "Grp_CMD")
            {
                sixthOperand = Rows[5]["To_Rename"].ToString();
            }

            else if (Rows[5]["To_Pin_Name"].ToString() == "FACEPLATE")
            {
                string pattern = @"(\w+)_FB";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    sixthOperand = $"{prefix}_FP";
                }
            }

            else if (Rows[5]["To_Rename"].ToString() == "Rule_Keep")
            {
                // Pattern to capture up to 40 operands
                int fromPinOrder = Convert.ToInt32(Rows[5]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 40)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        sixthOperand = match.Groups[fromPinOrder].Value.Trim();
                        sixthOperand = GetElementAndSubElementReplacement(sixthOperand, listfp, options);
                        sixthOperand = sixthOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            if (dataType == "Analog")
            {
                if (Rows[4]["To_Pin_Name"].ToString() == "MSW")
                {
                    // Dynamically create the pattern using the dataType
                    string pattern = $@"HMI_{dataType.ToUpper()}\[\d+\]";
                    Match match = Regex.Match(oldText, pattern);
                    if (match.Success)
                    {
                        sixthOperand = match.Value;
                    }
                }
            }

            if (newOperator == "AsysPLCRcv")
            {
                if (Rows[1]["To_Rename"].ToString() == "Rule_Keep")
                {
                    // Pattern to capture up to 40 operands
                    int fromPinOrder = Convert.ToInt32(Rows[1]["From_Pin_Order"]);

                    // Check if fromPinOrder is within the valid range
                    if (fromPinOrder >= 2 && fromPinOrder <= 40)
                    {
                        // Generate the pattern dynamically based on fromPinOrder
                        string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                        Match match = Regex.Match(oldText, pattern);

                        if (match.Success)
                        {
                            // Extract the operand based on fromPinOrder
                            sixthOperand = match.Groups[fromPinOrder].Value.Trim();
                            sixthOperand = GetElementAndSubElementReplacement(sixthOperand, listfp, options);
                            sixthOperand = sixthOperand.Replace("(", "").Replace(")", "");
                        }
                    }
                }
            }
        }

        // Seventh operand
        if (Rows.Count > 6 && Convert.ToInt32(Rows[6]["To_Pin_Order"]) == 7 || Rows.Count > 5 && Convert.ToInt32(Rows[5]["To_Pin_Order"]) == 7)
        {
            if (Rows[6]["To_Pin_Name"].ToString() == "FACEPLATE")
            {
                string pattern = @"(\w+)_FB";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    seventhOperand = $"{prefix}_FP";
                }
            }
            else if (Rows[6]["To_Pin_Name"].ToString() == "MSW")
            {
                // Dynamically create the pattern using the dataType
                string pattern = $@"HMI_{dataType.ToUpper()}\[\d+\]";

                if (dataType == "PIACS")
                {
                    pattern = $@"HMI_UNIT\[\d+\]";
                }

                if (oldOperatorName == "AsysRout")
                {
                    seventhOperand = $"HMI_GROUP[{HMI_INTERLOCK_MAX_GROUP}]";
                }

                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    seventhOperand = match.Value;
                }
            }

            if (Rows[6]["To_Operator_Type"].ToString() == "constant")
            {
                seventhOperand = Rows[6]["To_New_Operator"].ToString();
            }

            else if ((Rows[6]["To_Rename"].ToString() == "Rule_Keep") && (Rows[6]["To_Pin_Name"].ToString() != "FACEPLATE"))
            {
                // Pattern to capture up to 40 operands
                int fromPinOrder = Convert.ToInt32(Rows[6]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 40)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        seventhOperand = match.Groups[fromPinOrder].Value.Trim();
                        seventhOperand = GetElementAndSubElementReplacement(seventhOperand, listfp, options);
                        seventhOperand = seventhOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[6]["To_Pin_Name"].ToString() == "TOKEN")
            {
                string pattern = @"(\w+)_TK";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    seventhOperand = $@"\{prefix}.Token";
                }
            }

            else if (Rows[6]["To_Operator_Type"].ToString() == "SINT")
            {
                seventhOperand = Rows[6]["To_New_Operator"].ToString();
            }

            if (dataType == "Analog")
            {
                if (Rows[5]["To_Pin_Name"].ToString() == "FACEPLATE")
                {
                    string pattern = @"(\w+)_FB";
                    Match match = Regex.Match(oldText, pattern);
                    if (match.Success)
                    {
                        string prefix = match.Groups[1].Value;
                        seventhOperand = $"{prefix}_FP";
                    }
                }
            }

            if (newOperator == "AsysPLCRcv")
            {
                seventhOperand = wrappedHMIUnit;
            }

        }

        // Eigth operand
        if (Rows.Count > 7 && Convert.ToInt32(Rows[7]["To_Pin_Order"]) == 8 || Rows.Count > 6 && Convert.ToInt32(Rows[6]["To_Pin_Order"]) == 8)
        {
            if (Rows[7]["To_Pin_Name"].ToString() == "TOKEN")
            {
                string pattern = @"(\w+)_TK";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    eighthOperand = $@"\{prefix}.Token";
                }
            }
            else if (Rows[7]["To_Pin_Name"].ToString() == "FACEPLATE")
            {
                if (dataType == "PIACS")
                {
                    string pattern1 = @"(\w+)_FP";
                    Match match2 = Regex.Match(oldText, pattern1);
                    if (match2.Success)
                    {
                        eighthOperand = match2.Groups[0].Value;                         
                    }

                }
                else
                {
                    string pattern = @"(\w+)_FB";
                    Match match = Regex.Match(oldText, pattern);
                    if (match.Success)
                    {
                        string prefix = match.Groups[1].Value;
                        eighthOperand = $"{prefix}_FP";
                    }
                }                
            }

            else if (Rows[7]["To_Operator_Type"].ToString() == "SINT")
            {
                eighthOperand = Rows[7]["To_New_Operator"].ToString();
            }

            else if (Rows[7]["To_Rename"].ToString() == "Rule_Keep")
            {
                // Pattern to capture up to 40 operands
                int fromPinOrder = Convert.ToInt32(Rows[7]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 40)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        eighthOperand = match.Groups[fromPinOrder].Value.Trim();
                        eighthOperand = GetElementAndSubElementReplacement(eighthOperand, listfp, options);
                        eighthOperand = eighthOperand.Replace("(", "").Replace(")", "");

                    }
                }
            }

            if (dataType == "Analog")
            {
                if (Rows[6]["To_Pin_Name"].ToString() == "TOKEN")
                {
                    string pattern = @"(\w+)_TK";
                    Match match = Regex.Match(oldText, pattern);
                    if (match.Success)
                    {
                        string prefix = match.Groups[1].Value;
                        eighthOperand = $@"\{prefix}.Token";
                    }
                }
            }

            if (newOperator == "AsysPLCRcv")
            {
                eighthOperand = Instance + "_Rx_FP";
            }

        }

        // Ninth operand
        if (Rows.Count > 8 && Convert.ToInt32(Rows[8]["To_Pin_Order"]) == 9)
        {
            if (Rows[8]["To_Pin_Name"].ToString() == "TOKEN")
            {
                if (dataType == "PIACS")
                {
                    string pattern2 = @"(\w+)_TOKEN";
                    Match match2 = Regex.Match(oldText, pattern2);
                    if (match2.Success)
                    {
                        string prefix = match2.Groups[1].Value;
                        if (dataType == "Group")
                        {
                            ninthOperand = "Token";
                        }
                        else
                        {
                            ninthOperand = $@"\{prefix}.Token";
                        }

                    }
                }
                else 
                {
                    string pattern = @"(\w+)_TK";
                    Match match = Regex.Match(oldText, pattern);
                    if (match.Success)
                    {
                        string prefix = match.Groups[1].Value;
                        if (dataType == "Group")
                        {
                            ninthOperand = "Token";
                        }
                        else
                        {
                            ninthOperand = $@"\{prefix}.Token";
                        }

                    }
                }
                
            }

            else if (Rows[8]["To_Rename"].ToString() == "Rule_Keep")
            {
                // Pattern to capture up to 40 operands
                int fromPinOrder = Convert.ToInt32(Rows[8]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 40)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        ninthOperand = match.Groups[fromPinOrder].Value.Trim();
                        ninthOperand = GetElementAndSubElementReplacement(ninthOperand, listfp, options);
                        ninthOperand = ninthOperand.Replace("(", "").Replace(")", "");
                    }
                }

            }

            else if (Rows[8]["To_Operator_Type"].ToString() == "SINT")
            {
                ninthOperand = Rows[8]["To_New_Operator"].ToString();
            }
        }

        //Tenth Operand
        if (Rows.Count > 9 && Convert.ToInt32(Rows[9]["To_Pin_Order"]) == 10)
        {
            if (Rows[9]["To_Rename"].ToString() == "Rule_Keep")
            {
                int fromPinOrder = Convert.ToInt32(Rows[9]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        tenthOperand = match.Groups[fromPinOrder].Value.Trim();
                        tenthOperand = GetElementAndSubElementReplacement(tenthOperand, listfp, options);
                        tenthOperand = tenthOperand.Replace("(", "").Replace(")", "");
                    }
                }

            }

            else if (Rows[9]["To_Rename"].ToString() == "Rule_10")
            {
                tenthOperand = @"\HLC." + Rows[9]["From_Pin_Name"].ToString();
            }

            else if (Rows[9]["To_Pin_Name"].ToString() == "MSW")
            {
                // Dynamically create the pattern using the dataType
                string pattern = $@"HMI_UNIT\[\d+\]";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    tenthOperand = match.Value;
                }
            }

            else if (Rows[9]["To_Operator_Type"].ToString() == "SINT")
            {
                tenthOperand = Rows[9]["To_New_Operator"].ToString();
            }
        }

        // Eleventh Operand 
        if (Rows.Count > 10 && Convert.ToInt32(Rows[10]["To_Pin_Order"]) == 11)
        {
            if (Rows[10]["To_Rename"].ToString() == "Rule_Keep")
            {
                int fromPinOrder = Convert.ToInt32(Rows[10]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        eleventhOperand = match.Groups[fromPinOrder].Value.Trim();
                        eleventhOperand = GetElementAndSubElementReplacement(eleventhOperand, listfp, options);
                        eleventhOperand = eleventhOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[10]["To_Operator_Type"].ToString() == "constant")
            {
                eleventhOperand = Rows[10]["To_New_Operator"].ToString();
            }

            else if (Rows[10]["To_Pin_Name"].ToString() == "MSW")
            {
                // Dynamically create the pattern using the dataType
                string pattern = $@"HMI_UNIT\[\d+\]";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    eleventhOperand = match.Value;
                }
            }

            else if (Rows[10]["To_Operator_Type"].ToString() == "SINT")
            {
                eleventhOperand = Rows[10]["To_New_Operator"].ToString();
            }
        }        


        // Twelfth Operand 
        if (Rows.Count > 11 && Convert.ToInt32(Rows[11]["To_Pin_Order"]) == 12)
        {
            if (Rows[11]["To_Rename"].ToString() == "Rule_Keep" && Rows[11]["To_Pin_Name"].ToString() != "FACEPLATE")
            {
                int fromPinOrder = Convert.ToInt32(Rows[11]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        twelfthOperand = match.Groups[fromPinOrder].Value.Trim();
                        twelfthOperand = GetElementAndSubElementReplacement(twelfthOperand, listfp, options);
                        twelfthOperand = twelfthOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[11]["To_Pin_Name"].ToString() == "FACEPLATE")
            {
                if (dataType == "Schenck" || dataType == "Pfister")
                {
                    string pattern = @"(\w+)_FP";
                    Match match = Regex.Match(oldText, pattern);
                    if (match.Success)
                    {
                        twelfthOperand = match.Value;                                              
                    }
                }
            }
        
            else if (Rows[10]["To_Operator_Type"].ToString() == "SINT")
            {
                twelfthOperand = Rows[11]["To_New_Operator"].ToString();
            }

        }

        // Thirteenth Operand 
        if (Rows.Count > 12 && Convert.ToInt32(Rows[12]["To_Pin_Order"]) == 13)
        {
            if (Rows[12]["To_Rename"].ToString() == "Rule_Keep")
            {
                int fromPinOrder = Convert.ToInt32(Rows[12]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        thirteenthOperand = match.Groups[fromPinOrder].Value.Trim();
                        thirteenthOperand = GetElementAndSubElementReplacement(thirteenthOperand, listfp, options);
                        thirteenthOperand = thirteenthOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[12]["To_Pin_Name"].ToString() == "MSW")
            {
                if (dataType == "SPC")
                {
                    string pattern2 = $@"HMI_UNIT\[\d+\]";

                    Match match2 = Regex.Match(oldText, pattern2);
                    if (match2.Success)
                    {
                        thirteenthOperand = match2.Value;
                    }
                }
                else
                {
                    if (dataType == "Dosax")
                    {
                        string pattern2 = $@"HMI_UNIT\[\d+\]";
                        Match match2 = Regex.Match(oldText, pattern2);
                        if (match2.Success)
                        {
                            thirteenthOperand = match2.Value;
                        }
                    }
                    else
                    {
                        // Dynamically create the pattern using the dataType
                        string pattern = $@"HMI_{dataType.ToUpper()}\[\d+\]";
                        Match match = Regex.Match(oldText, pattern);
                        if (match.Success)
                        {
                            thirteenthOperand = match.Value;
                        }
                    }
                    
                }
               
            }

            else if (Rows[12]["To_Operator_Type"].ToString() == "constant")
            {
                thirteenthOperand = Rows[12]["To_New_Operator"].ToString();
            }

            else if (Rows[12]["To_Pin_Name"].ToString() == "TOKEN")
            {
                if (dataType == "Schenck")
                {
                    string pattern = @"(\w+)_TOKEN";
                    Match match = Regex.Match(oldText, pattern);
                    if (match.Success)
                    {
                        string prefix = match.Groups[1].Value;
                        if (dataType == "Group")
                        {
                            thirteenthOperand = "Token";
                        }
                        else
                        {
                            thirteenthOperand = $@"\{prefix}.Token";
                        }

                    }
                }

                else if (dataType == "Pfister")
                {
                    string pattern = @"(\w+)_TK";
                    Match match = Regex.Match(oldText, pattern);
                    if (match.Success)
                    {
                        string prefix = match.Groups[1].Value;
                        if (dataType == "Group")
                        {
                            thirteenthOperand = "Token";
                        }
                        else
                        {
                            thirteenthOperand = $@"\{prefix}.Token";
                        }

                    }
                }

            }
        }

        // Fourteenth Operand 
        if (Rows.Count > 13 && Convert.ToInt32(Rows[13]["To_Pin_Order"]) == 14)
        {
            if (Rows[13]["To_Rename"].ToString() == "Rule_Keep" && Rows[13]["To_Pin_Name"].ToString() != "FACEPLATE")
            {
                int fromPinOrder = Convert.ToInt32(Rows[13]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        fourteenthOperand = match.Groups[fromPinOrder].Value.Trim();
                        fourteenthOperand = GetElementAndSubElementReplacement(fourteenthOperand, listfp, options);
                        fourteenthOperand = fourteenthOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[13]["To_Operator_Type"].ToString() == "constant")
            {
                fourteenthOperand = Rows[13]["To_New_Operator"].ToString();
            }

            else if (Rows[13]["To_Pin_Name"].ToString() == "FACEPLATE")
            {
                if (dataType == "SPC" || (dataType == "Dosax"))
                {
                    string pattern = @"(\w+)_FP";
                    Match match = Regex.Match(oldText, pattern);
                    if (match.Success)
                    {
                        fourteenthOperand = match.Value;
                    }
                }
            }

        }

        // Fifteenth Operand 
        if (Rows.Count > 14 && Convert.ToInt32(Rows[14]["To_Pin_Order"]) == 15)
        {
            if (Rows[14]["To_Rename"].ToString() == "Rule_Keep")
            {
                int fromPinOrder = Convert.ToInt32(Rows[14]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        fifteenthOperand = match.Groups[fromPinOrder].Value.Trim();
                        fifteenthOperand = GetElementAndSubElementReplacement(fifteenthOperand, listfp, options);
                        fifteenthOperand = fifteenthOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[14]["To_Pin_Name"].ToString() == "MSW")
            {
                // Dynamically create the pattern using the dataType
                string pattern = $@"HMI_UNIT\[\d+\]";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    fifteenthOperand = match.Value;
                }
            }

            else if (Rows[14]["To_Operator_Type"].ToString() == "constant")
            {
                fifteenthOperand = Rows[14]["To_New_Operator"].ToString();
            }

            else if (Rows[14]["To_Pin_Name"].ToString() == "TOKEN")
            {
                if (dataType == "SPC")
                {
                    string pattern = @"(\w+)_TOKEN";
                    Match match = Regex.Match(oldText, pattern);
                    if (match.Success)
                    {
                        string prefix = match.Groups[1].Value;
                        if (dataType == "Group")
                        {
                            fifteenthOperand = "Token";
                        }
                        else
                        {
                            fifteenthOperand = $@"\{prefix}.Token";
                        }

                    }
                }

                else
                {
                    string pattern = @"(\w+)_TK";
                    Match match = Regex.Match(oldText, pattern);
                    if (match.Success)
                    {
                        string prefix = match.Groups[1].Value;
                        if (dataType == "Group")
                        {
                            fifteenthOperand = "Token";
                        }
                        else
                        {
                            fifteenthOperand = $@"\{prefix}.Token";
                        }

                    }
                }
            }

        }

        // Sixteenth Operand 
        if (Rows.Count > 15 && Convert.ToInt32(Rows[15]["To_Pin_Order"]) == 16)
        {
            if ((Rows[15]["To_Rename"].ToString() == "Rule_Keep") && (Rows[15]["To_Pin_Name"].ToString() != "FACEPLATE"))
            {
                int fromPinOrder = Convert.ToInt32(Rows[15]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        sixteenthOperand = match.Groups[fromPinOrder].Value.Trim();
                        sixteenthOperand = GetElementAndSubElementReplacement(sixteenthOperand, listfp, options);
                        sixteenthOperand = sixteenthOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[15]["To_Pin_Name"].ToString() == "FACEPLATE")
            {
                string pattern = @"(\w+)_FB";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    sixteenthOperand = $"{prefix}_FP";
                }
            }
        }

        // Seventeenth Operand 
        if (Rows.Count > 16 && Convert.ToInt32(Rows[16]["To_Pin_Order"]) == 17)
        {
            if (Rows[16]["To_Rename"].ToString() == "Rule_Keep")
            {
                int fromPinOrder = Convert.ToInt32(Rows[16]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        seventeenthOperand = match.Groups[fromPinOrder].Value.Trim();
                        seventeenthOperand = GetElementAndSubElementReplacement(seventeenthOperand, listfp, options);
                        seventeenthOperand = seventeenthOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[16]["To_Pin_Name"].ToString() == "TOKEN")
            {
                string pattern = @"(\w+)_TK";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    if (dataType == "Group")
                    {
                        seventeenthOperand = "Token";
                    }
                    else
                    {
                        seventeenthOperand = $@"\{prefix}.Token";
                    }

                }
            }

            else if (Rows[16]["To_Operator_Type"].ToString() == "constant")
            {
                seventeenthOperand = Rows[16]["To_New_Operator"].ToString();
            }

            else if (Rows[16]["To_Pin_Name"].ToString() == "MSW")
            {
                // Dynamically create the pattern using the dataType
                string pattern = $@"HMI_UNIT\[\d+\]";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    seventeenthOperand = match.Value;
                }
            }
        }

        // Eighteenth Operand 
        if (Rows.Count > 17 && Convert.ToInt32(Rows[17]["To_Pin_Order"]) == 18)
        {
            if (Rows[17]["To_Rename"].ToString() == "Rule_Keep")
            {
                int fromPinOrder = Convert.ToInt32(Rows[17]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        eighteenthOperand = match.Groups[fromPinOrder].Value.Trim();
                        eighteenthOperand = GetElementAndSubElementReplacement(eighteenthOperand, listfp, options);
                        eighteenthOperand = eighteenthOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[17]["To_Operator_Type"].ToString() == "constant")
            {
                eighteenthOperand = Rows[17]["To_New_Operator"].ToString();
            }
        }

        // Nineteenth Operand 
        if (Rows.Count > 18 && Convert.ToInt32(Rows[18]["To_Pin_Order"]) == 19)
        {
            if (Rows[18]["To_Rename"].ToString() == "Rule_Keep")
            {
                int fromPinOrder = Convert.ToInt32(Rows[18]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        nineteenthOperand = match.Groups[fromPinOrder].Value.Trim();
                        nineteenthOperand = GetElementAndSubElementReplacement(nineteenthOperand, listfp, options);
                        nineteenthOperand = nineteenthOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[18]["To_Pin_Name"].ToString() == "TOKEN")
            {
                string pattern = @"(\w+)_TK";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    nineteenthOperand = $@"\{prefix}.Token";
                }
            }
            
        }

        //  Twentieth Operand 
        if (Rows.Count > 19 && Convert.ToInt32(Rows[19]["To_Pin_Order"]) == 20)
        {
            if (Rows[19]["To_Rename"].ToString() == "Rule_Keep")
            {
                int fromPinOrder = Convert.ToInt32(Rows[19]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        twentiethOperand = match.Groups[fromPinOrder].Value.Trim();
                        twentiethOperand = GetElementAndSubElementReplacement(twentiethOperand, listfp, options);
                        twentiethOperand = twentiethOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[19]["To_Pin_Name"].ToString() == "MSW")
            {
                // Dynamically create the pattern using the dataType
                string pattern = $@"HMI_UNIT\[\d+\]";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    twentiethOperand = match.Value;
                }
            }

            else if (Rows[19]["To_Operator_Type"].ToString() == "constant")
            {
                twentiethOperand = Rows[19]["To_New_Operator"].ToString();
            }
        }

        //  Twenty First Operand 
        if (Rows.Count > 20 && Convert.ToInt32(Rows[20]["To_Pin_Order"]) == 21)
        {
            if ((Rows[20]["To_Rename"].ToString() == "Rule_Keep") && (Rows[20]["To_Pin_Name"].ToString() != "FACEPLATE"))
            {
                int fromPinOrder = Convert.ToInt32(Rows[20]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        twentyFirstOperand = match.Groups[fromPinOrder].Value.Trim();
                        twentyFirstOperand = GetElementAndSubElementReplacement(twentyFirstOperand, listfp, options);
                        twentyFirstOperand = twentyFirstOperand.Replace("(", "").Replace(")", "");
                    }
                }                
            }

            else if (Rows[20]["To_Pin_Name"].ToString() == "FACEPLATE")
            {
                string pattern = @"(\w+)_FB";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    twentyFirstOperand = $"{prefix}_FP";
                }
            }

            else if (Rows[20]["To_Operator_Type"].ToString() == "constant")
            {
                twentyFirstOperand = Rows[20]["To_New_Operator"].ToString();
            }
        }

        //  Twenty Second Operand 
        if (Rows.Count > 21 && Convert.ToInt32(Rows[21]["To_Pin_Order"]) == 22)
        {
            if (Rows[21]["To_Rename"].ToString() == "Rule_Keep")
            {
                int fromPinOrder = Convert.ToInt32(Rows[21]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        twentySecondOperand = match.Groups[fromPinOrder].Value.Trim();
                        twentySecondOperand = GetElementAndSubElementReplacement(twentySecondOperand, listfp, options);
                        twentySecondOperand = twentySecondOperand.Replace("(", "").Replace(")", "");
                    }
                }                
            }

            else if (Rows[21]["To_Operator_Type"].ToString() == "constant")
            {
                twentySecondOperand = Rows[21]["To_New_Operator"].ToString();
            }


            else if (Rows[21]["To_Pin_Name"].ToString() == "TOKEN")
            {
                string pattern = @"(\w+)_TK";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    if (dataType == "Group")
                    {
                        twentySecondOperand = "Token";
                    }
                    else
                    {
                        twentySecondOperand = $@"\{prefix}.Token";
                    }

                }
            }


        }

        //  Twenty Third Operand 
        if (Rows.Count > 22 && Convert.ToInt32(Rows[22]["To_Pin_Order"]) == 23)
        {
            if (Rows[22]["To_Rename"].ToString() == "Rule_Keep")
            {
                int fromPinOrder = Convert.ToInt32(Rows[22]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        twentyThirdOperand = match.Groups[fromPinOrder].Value.Trim();
                        twentyThirdOperand = GetElementAndSubElementReplacement(twentyThirdOperand, listfp, options);
                        twentyThirdOperand = twentyThirdOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[22]["To_Operator_Type"].ToString() == "constant")
            {
                twentyThirdOperand = Rows[22]["To_New_Operator"].ToString();
            }
        }

        //  Twenty Fourth Operand 
        if (Rows.Count > 23 && Convert.ToInt32(Rows[23]["To_Pin_Order"]) == 24)
        {
            if ((Rows[23]["To_Pin_Name"].ToString() != "MSW") && (Rows[23]["To_Rename"].ToString() == "Rule_Keep"))
            {
                int fromPinOrder = Convert.ToInt32(Rows[23]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        twentyFourthOperand = match.Groups[fromPinOrder].Value.Trim();
                        twentyFourthOperand = GetElementAndSubElementReplacement(twentyFourthOperand, listfp, options);
                        twentyFourthOperand = twentyFourthOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if ((Rows[23]["To_Pin_Name"].ToString() == "MSW") && (Rows[23]["To_Rename"].ToString() == "Rule_1"))
            {
                // Dynamically create the pattern using the dataType
                string pattern = $@"HMI_UNIT\[\d+\]";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    twentyFourthOperand = match.Value;
                }
            }

            else if (Rows[23]["To_Operator_Type"].ToString() == "constant")
            {
                twentyFourthOperand = Rows[23]["To_New_Operator"].ToString();
            }
        }

        //  Twenty Fifth Operand 
        if (Rows.Count > 24 && Convert.ToInt32(Rows[24]["To_Pin_Order"]) == 25)
        {
            if (Rows[24]["To_Rename"].ToString() == "Rule_Keep")
            {
                int fromPinOrder = Convert.ToInt32(Rows[24]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        twentyFifthOperand = match.Groups[fromPinOrder].Value.Trim();
                        twentyFifthOperand = GetElementAndSubElementReplacement(twentyFifthOperand, listfp, options);
                        twentyFifthOperand = twentyFifthOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[24]["To_Pin_Name"].ToString() == "FACEPLATE")
            {
                string pattern = @"(\w+)_FB";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    twentyFifthOperand = $"{prefix}_FP";
                }
            }

            else if ((Rows[24]["To_Pin_Name"].ToString() == "MSW") && (Rows[24]["To_Rename"].ToString() == "Rule_1"))
            {
                // Dynamically create the pattern using the dataType
                string pattern = $@"HMI_UNIT\[\d+\]";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    twentyFifthOperand = match.Value;
                }
            }

            else if (Rows[24]["To_Operator_Type"].ToString() == "constant")
            {
                twentyFifthOperand = Rows[24]["To_New_Operator"].ToString();
            }
        }

        //  Twenty Sixth Operand 
        if (Rows.Count > 25 && Convert.ToInt32(Rows[25]["To_Pin_Order"]) == 26)
        {
            if ((Rows[25]["To_Rename"].ToString() == "Rule_Keep") && (Rows[25]["To_Pin_Name"].ToString() != "FACEPLATE"))
            {
                int fromPinOrder = Convert.ToInt32(Rows[25]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        twentySixthOperand = match.Groups[fromPinOrder].Value.Trim();
                        twentySixthOperand = GetElementAndSubElementReplacement(twentySixthOperand, listfp, options);
                        twentySixthOperand = twentySixthOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[25]["To_Pin_Name"].ToString() == "TOKEN")
            {
                string pattern = @"(\w+)_TK";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    twentySixthOperand = $@"\{prefix}.Token";
                }
            }

            else if (Rows[25]["To_Pin_Name"].ToString() == "FACEPLATE")
            {
                string pattern = @"(\w+)_FB";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    twentySixthOperand = $"{prefix}_FP";
                }
            }

            else if (Rows[25]["To_Operator_Type"].ToString() == "constant")
            {
                twentySixthOperand = Rows[25]["To_New_Operator"].ToString();
            }
        }

        //  Twenty Seventh Operand 
        if (Rows.Count > 26 && Convert.ToInt32(Rows[26]["To_Pin_Order"]) == 27)
        {
            if (Rows[26]["To_Rename"].ToString() == "Rule_Keep")
            {
                int fromPinOrder = Convert.ToInt32(Rows[26]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        twentySeventhOperand = match.Groups[fromPinOrder].Value.Trim();
                        twentySeventhOperand = GetElementAndSubElementReplacement(twentySeventhOperand, listfp, options);
                        twentySeventhOperand = twentySeventhOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[26]["To_Pin_Name"].ToString() == "DEPT_CMD")
            {
                string pattern = @"(\w+)_CMD";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    twentySeventhOperand = $@"\{prefix}.Dept_LINK";
                }
            }

            else if (Rows[26]["To_Operator_Type"].ToString() == "constant")
            {
                twentySeventhOperand = Rows[26]["To_New_Operator"].ToString();
            }
        }

        //  Twenty Eigth Operand 
        if (Rows.Count > 27 && Convert.ToInt32(Rows[27]["To_Pin_Order"]) == 28)
        {
            if ((Rows[27]["To_Pin_Name"].ToString() != "HLC_LINK") && (Rows[27]["To_Rename"].ToString() == "Rule_Keep"))
            {
                int fromPinOrder = Convert.ToInt32(Rows[27]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        twentyEighthOperand = match.Groups[fromPinOrder].Value.Trim();
                        twentyEighthOperand = GetElementAndSubElementReplacement(twentyEighthOperand, listfp, options);
                        twentyEighthOperand = twentyEighthOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[27]["To_Pin_Name"].ToString() == "HLC_LINK")
            {
                twentyEighthOperand = Rows[27]["To_Pin_Name"].ToString();
            }

            else if (Rows[27]["To_Operator_Type"].ToString() == "constant")
            {
                twentyEighthOperand = Rows[27]["To_New_Operator"].ToString();
            }

        }

        //  Twenty Ninth Operand 
        if (Rows.Count > 28 && Convert.ToInt32(Rows[28]["To_Pin_Order"]) == 29)
        {
            if (Rows[28]["To_Rename"].ToString() == "Rule_Keep")
            {
                int fromPinOrder = Convert.ToInt32(Rows[28]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        twentyNinthOperand = match.Groups[fromPinOrder].Value.Trim();
                        twentyNinthOperand = GetElementAndSubElementReplacement(twentyNinthOperand, listfp, options);
                        twentyNinthOperand = twentyNinthOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[28]["To_Operator_Type"].ToString() == "constant")
            {
                twentyNinthOperand = Rows[28]["To_New_Operator"].ToString();
            }
        }

        //  Thirtieth Operand 
        if (Rows.Count > 29 && Convert.ToInt32(Rows[29]["To_Pin_Order"]) == 30)
        {
            if (Rows[29]["To_Rename"].ToString() == "Rule_Keep")
            {
                int fromPinOrder = Convert.ToInt32(Rows[29]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        thirtiethOperand = match.Groups[fromPinOrder].Value.Trim();
                        thirtiethOperand = GetElementAndSubElementReplacement(thirtiethOperand, listfp, options);
                        thirtiethOperand = thirtiethOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[29]["To_Operator_Type"].ToString() == "constant")
            {
                thirtiethOperand = Rows[29]["To_New_Operator"].ToString();
            }
        }

        //  Thirty First Operand 
        if (Rows.Count > 30 && Convert.ToInt32(Rows[30]["To_Pin_Order"]) == 31)
        {
            if (Rows[30]["To_Rename"].ToString() == "Rule_Keep")
            {
                int fromPinOrder = Convert.ToInt32(Rows[30]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        thirtyFirstOperand = match.Groups[fromPinOrder].Value.Trim();
                        thirtyFirstOperand = GetElementAndSubElementReplacement(thirtyFirstOperand, listfp, options);
                        thirtyFirstOperand = thirtyFirstOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[30]["To_Operator_Type"].ToString() == "constant")
            {
                thirtyFirstOperand = Rows[30]["To_New_Operator"].ToString();
            }
        }


        //  Thirty Second Operand 
        if (Rows.Count > 31 && Convert.ToInt32(Rows[31]["To_Pin_Order"]) == 32)
        {
            if ((Rows[31]["To_Pin_Name"].ToString() == "MSW") && (Rows[31]["To_Rename"].ToString() == "Rule_1"))
            {
                // Dynamically create the pattern using the dataType
                string pattern = $@"HMI_UNIT\[\d+\]";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    thirtySecondOperand = match.Value;
                }
            }

            else if (Rows[31]["To_Rename"].ToString() == "Rule_Keep")
            {
                int fromPinOrder = Convert.ToInt32(Rows[31]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 34)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        thirtySecondOperand = match.Groups[fromPinOrder].Value.Trim();
                        thirtySecondOperand = GetElementAndSubElementReplacement(thirtySecondOperand, listfp, options);
                        thirtySecondOperand = thirtySecondOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[31]["To_Operator_Type"].ToString() == "constant")
            {
                thirtySecondOperand = Rows[31]["To_New_Operator"].ToString();
            }
        }

        //  Thirty Third Operand 
        if (Rows.Count > 32 && Convert.ToInt32(Rows[32]["To_Pin_Order"]) == 33)
        {
            if ((Rows[32]["To_Rename"].ToString() == "Rule_Keep") && (Rows[32]["To_Pin_Name"].ToString() != "FACEPLATE"))
            {
                int fromPinOrder = Convert.ToInt32(Rows[32]["From_Pin_Order"]);

                // Check if fromPinOrder is within the valid range
                if (fromPinOrder >= 2 && fromPinOrder <= 36)
                {
                    // Generate the pattern dynamically based on fromPinOrder
                    string pattern = $@"\(([^,]+{string.Concat(Enumerable.Repeat(",([^,]+)", fromPinOrder - 1))})";
                    Match match = Regex.Match(oldText, pattern);

                    if (match.Success)
                    {
                        // Extract the operand based on fromPinOrder
                        thirtyThirdOperand = match.Groups[fromPinOrder].Value.Trim();
                        thirtyThirdOperand = GetElementAndSubElementReplacement(thirtyThirdOperand, listfp, options);
                        thirtyThirdOperand = thirtyThirdOperand.Replace("(", "").Replace(")", "");
                    }
                }
            }

            else if (Rows[32]["To_Pin_Name"].ToString() == "FACEPLATE")
            {
                string pattern = @"(\w+)_FB";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    thirtyThirdOperand = $"{prefix}_FP";
                }
            }

            else if (Rows[32]["To_Operator_Type"].ToString() == "constant")
            {
                thirtyThirdOperand = Rows[32]["To_New_Operator"].ToString();
            }
        }

        //  Thirty Fourth Operand 
        if (Rows.Count > 33 && Convert.ToInt32(Rows[33]["To_Pin_Order"]) == 34)
        {
            if ((Rows[33]["To_Pin_Name"].ToString() == "MSW") && (Rows[33]["To_Rename"].ToString() == "Rule_1"))
            {
                // Dynamically create the pattern using the dataType
                string pattern = $@"HMI_UNIT\[\d+\]";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    thirtyFourthOperand = match.Value;
                }
            }

            else if (Rows[33]["To_Pin_Name"].ToString() == "TOKEN")
            {
                string pattern = @"(\w+)_TK";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    thirtyFourthOperand = $@"\{prefix}.Token";
                }
            }
        }

        //  Thirty Fifth Operand 
        if (Rows.Count > 34 && Convert.ToInt32(Rows[34]["To_Pin_Order"]) == 35)
        {
            if (Rows[34]["To_Pin_Name"].ToString() == "FACEPLATE")
            {
                string pattern = @"(\w+)_FB";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    thirtyFifthOperand = $"{prefix}_FP";
                }
            }
        }

        //  Thirty Sixth Operand 
        if (Rows.Count > 35 && Convert.ToInt32(Rows[35]["To_Pin_Order"]) == 36)
        {
            if (Rows[35]["To_Pin_Name"].ToString() == "TOKEN")
            {
                string pattern = @"(\w+)_TK";
                Match match = Regex.Match(oldText, pattern);
                if (match.Success)
                {
                    string prefix = match.Groups[1].Value;
                    thirtySixthOperand = $@"\{prefix}.Token";
                }
            }
        }

        List<string> operands = new List<string>
{
    firstOperand, secondOperand, thirdOperand, fourthOperand, fifthOperand, sixthOperand, seventhOperand, eighthOperand, ninthOperand,
    tenthOperand, eleventhOperand, twelfthOperand, thirteenthOperand, fourteenthOperand, fifteenthOperand, sixteenthOperand, seventeenthOperand, eighteenthOperand, nineteenthOperand,
    twentiethOperand, twentyFirstOperand, twentySecondOperand, twentyThirdOperand, twentyFourthOperand, twentyFifthOperand, twentySixthOperand, twentySeventhOperand, twentyEighthOperand, twentyNinthOperand,
    thirtiethOperand, thirtyFirstOperand, thirtySecondOperand, thirtyThirdOperand, thirtyFourthOperand, thirtyFifthOperand, thirtySixthOperand
};

        operands = operands.Select(o => o?.Replace(";", "")).ToList();

        // Remove any null or empty operands
        operands.RemoveAll(op => string.IsNullOrEmpty(op));

        // Combine operands
        string newoperand = string.Join(",", operands);

        return newoperand;

    }

    public string GetOperatorConversionMapping(string oldOperator, RockwellUpgradeOptions options)
    {
        string pattern = @"(ADPT)(.*)(_MOTOR|_GATE)";

        // Retrieve the filtered master rows based on the specified options
        IEnumerable<DataRow> rows = dtMaster.Select($"From_Object = '{oldOperator}'");

        if (!rows.Any() && Regex.IsMatch(oldOperator, pattern))
        {
            // Replace the content between "ADPT" and either "_MOTOR" or "_GATE" with "xxxx"
            oldOperator = Regex.Replace(oldOperator, pattern, "ADPTxxxx$3");

            rows = dtMaster.Select($"From_Object = '{oldOperator}'");
        }

        // Iterate over the filtered master rows to find the matching oldOperator
        foreach (DataRow masterRow in rows)
        {
            // Check if the From_Object field matches the oldOperator and is not empty
            if (masterRow.Field<string>("From_Object") == oldOperator &&
                !string.IsNullOrEmpty(masterRow.Field<string>("From_Object")))
            {
                // Retrieve the corresponding detail row
                DataRow detail = dtDetail.AsEnumerable()
                    .FirstOrDefault(d => d.Field<long>("mid") == masterRow.Field<long>("mid"));

                if (detail != null)
                {
                    // Extract the To_Object field which represents the newOperator
                    string newOperator = detail.Field<string>("To_Object") ?? string.Empty;

                    if (newOperator.Contains("xxxx"))
                    {
                        newOperator = newOperator.Replace("xxxx", "3UF70");
                    }

                    if (newOperator == "AsysSel" && options.IsExtendedSelect)
                    {
                        newOperator = "AsysExtSelect";
                    }

                    // Return the newOperator value
                    return newOperator;
                }
            }
        }

        // Return null or an empty string if no match is found
        return null;
    }

    public List<string> GetOperandsConversionForNonAseysOperator(RockwellUpgradeOptions options, string oldText, string dataType, List<string> HMI_Interlock, string MaxHMIInterlock, List<string> InputOperandForInterlock, int NewProgramIndex, IProgress<string> progress)
    {      

        // Regex to find all occurrences of the patterns, accommodating any leading element in _FB 

        string pattern = @"([A-Z]+)\(([^)_]+)_(FB)\.([^,\)]+)\)";
        List<string> resultList = new List<string>();

        if (!string.IsNullOrEmpty(oldText))
        {
            // Check if there is an OTE without a preceding element pattern
            bool oteWithoutElement = Regex.IsMatch(oldText, @"(?<![\),])\bOTE\([^\)]*\)");

            string OperatorCluster = string.Empty;
            string OTECluster = string.Empty;
            

            // Use Regex.Replace with a MatchEvaluator to handle replacements
            string result = Regex.Replace(oldText, pattern, match =>
            {
                string fullMatch = match.Value;
                string leadingElement = match.Groups[1].Value;
                string tag = match.Groups[2].Value;
                string suffix = match.Groups[3].Value;
                string element = match.Groups[4].Value;

                // Select rows where From_Pin_Name matches the element
                DataRow[] rows = dtRungConversion.Select($"From_Pin_Name = '{element}'");

                if (rows.Length > 0 && !string.IsNullOrEmpty(rows[0]["To_Pin_Name"].ToString()))
                {
                    string newElement = rows[0]["To_Pin_Name"].ToString();

                    if (newElement == "Rule_Interlock" || newElement == "Rule_InterlockStop")
                    {
                        newElement = element;
                    }

                    return $"{leadingElement}({tag}_{suffix}.{newElement})";
                }

                return fullMatch; // Return the original match if no replacement is found
            });

            // Extract the element before OTE element
            Match match = Regex.Match(result, @"(?<OperatorCluster>[A-Z]+\([^)]*\))\s*(?<OTECluster>.*)");
            if (match.Success)
            {
                // Extract the relevant part without OTE
                OperatorCluster = match.Groups["OperatorCluster"].Value;

                // Extract the OTECluster
                OTECluster = match.Groups["OTECluster"].Value.Trim();

                resultList = InterLockConditions(options, result, OperatorCluster, HMI_Interlock, MaxHMIInterlock, InputOperandForInterlock, NewProgramIndex,dataType, progress);
            }
            else
            {
                resultList.Add(result);
            }
        }       

        

        return resultList;
    }

    private List<string> InterLockConditions(RockwellUpgradeOptions options, string result, string operatorCluster, List<string> HMI_Interlock, string MaxHMIInterlock, List<string> InputOperandForInterlock, int NewProgramIndex, string datatype, IProgress<string> progress)
    {
        if (NewProgramIndex > CurrentInterLockIndex)
        {
            prefixIndexMap.Clear();
            CurrentInterLockIndex = NewProgramIndex;
        }

        string ExtSelected = options.IsExtendedInterlock ? "AsysExtInterlock" : "AsysInterlock";
        List<string> resultList = new List<string>();
        Match oteMatch = Regex.Match(result, @"OTE\(([^)]+)\)");

        if (oteMatch.Success && !Regex.IsMatch(result, @"AFI\(\)"))
        {
            string fullOteContent = oteMatch.Groups[1].Value;
            Match prMatch = Regex.Match(fullOteContent, @"_(FB|FP)\.([^.]+)");
            string prContent = prMatch.Success ? prMatch.Groups[2].Value : string.Empty;

            string pattern = @"^(.*?)_FB";
            Match match = Regex.Match(fullOteContent, pattern);
            string extractedContent = match.Groups[1].Value;

            string EN = "0";
            string IN = "1";

            string rung1 = "";
            string rung2 = "";
            string rung3 = "";

            if (prContent == "PR" || prContent == "SA" || prContent == "MACH" || prContent == "OP" || prContent == "OP1" || prContent == "OP2" || prContent == "OP3" || prContent == "STI" || prContent == "STI1" || prContent == "STI2" || prContent == "GSTI"
                || prContent == "GOP" || prContent == "GSTRR" || prContent == "RSTI" || prContent == "ROP" || prContent == "DSE" || prContent == "GSTPR" || prContent == "ENAB" || prContent == "FCON" || prContent == "FCOFF")
            {
                EN = "1";
            }

            if (InputOperandForInterlock != null && HMI_Interlock != null && HMI_Interlock.Count > 0)
            {
                resultList.Clear();

                LINK = GetLink(prContent);
                
                for (int i = 0; i < InputOperandForInterlock.Count; i++)
                {
                    if (!string.IsNullOrEmpty(InputOperandForInterlock[i]))
                    {
                        string prefix = GetIntPrefix(prContent);
                        if (!prefixIndexMap.ContainsKey(prefix))
                        {
                            prefixIndexMap[prefix] = 1;
                        }

                        string newInt = extractedContent + prefix + prefixIndexMap[prefix].ToString("D2");

                        rung1 = $"{InputOperandForInterlock[i]}OTE({newInt}.IN);";

                        // Set rung2 based on prContent
                        if (prContent == "STI")
                        {
                            rung2 = $"[XIO(FB.RUN)]OTE({newInt}.EN);";
                        }
                        else if (prContent == "STI1")
                        {
                            rung2 = $"[XIO(FB.RUN1)]OTE({newInt}.EN);";
                        }
                        else if (prContent == "STI2")
                        {
                            rung2 = $"[XIO(FB.RUN2)]OTE({newInt}.EN);";
                        }
                        else
                        {
                            rung2 = $"[XIC(FB.AUT),XIC(FB.SS)]OTE({newInt}.EN);";
                        }


                        string hmiInterlockValue = (i < HMI_Interlock.Count) ? HMI_Interlock[i] : string.Empty;
                        if (hmiInterlockValue == string.Empty)
                        {
                            MaxHMIInterlock = IncrementInterlock(MaxHMIInterlock);
                            hmiInterlockValue = MaxHMIInterlock;
                        }
                        rung3 = $"{ExtSelected}({newInt},{LINK},{hmiInterlockValue});";

                        if (prContent == "MACH" || prContent == "OP" || prContent == "OP1" || prContent == "OP2" || prContent == "OP3" || prContent == "STI" || prContent == "STI1" || prContent == "STI2")
                        {
                            resultList.Add(rung1);
                            resultList.Add(rung2);
                            resultList.Add(rung3);
                            prefixIndexMap[prefix]++; // Increment the index for this prefix
                            progress.Report($"Interlock rungs has been added for .{prContent} interlock type");
                        }
                        else if (prContent == "PR" || prContent == "SA" || prContent == "GSTI" || prContent == "GOP" || prContent == "GSTRR" || prContent == "RSTI" || prContent == "ROP" || prContent == "GSTPR" || prContent == "FCON" || prContent == "FCOFF")
                        {
                            resultList.Add(rung1);
                            resultList.Add(rung3);
                            prefixIndexMap[prefix]++; // Increment the index for this prefix
                            progress.Report($"Interlock rungs has been added for .{prContent} interlock type");
                        }
                        else
                        {
                            if (Regex.IsMatch(result, @"OTE\(.+?\.FB\.GON\)"))
                            {
                                // Use regex to clear content before "OTE(anything.FB.GON)"
                                string modifiedResult = Regex.Replace(result, @".*?(?=OTE\(.+?\.FB\.GON\))", string.Empty);
                                resultList.Add(modifiedResult);
                            }
                            else if (Regex.IsMatch(result, @"OTE\(.+?\.FB\.GRDY\)"))
                            {
                                // Use regex to clear content before "OTE(anything.FB.GON)"
                                string modifiedResult = Regex.Replace(result, @".*?(?=OTE\(.+?\.FB\.GRDY\))", string.Empty);
                                resultList.Add(modifiedResult);
                            }
                            else if (Regex.IsMatch(result, @"OTE\(.+?\.FB\.GOFF\)"))
                            {
                                // Use regex to clear content before "OTE(anything.FB.GON)"
                                string modifiedResult = Regex.Replace(result, @".*?(?=OTE\(.+?\.FB\.GOFF\))", string.Empty);
                                resultList.Add(modifiedResult);
                            }                            

                            else 
                            {
                                string pattern1 = @"OTE\(\w+_FBINT\d+\)|\.PR\b|\.SA\b|\.MACH\b|\.OP\b|\.OP1\b|\.OP2\b|\.OP3\b|\.STI\b|\.STI1\b|\.STI2\b|\.GSTI\b|\.GOP\b|\.GSTRR\b|\.RSTI\b|\.ROP\b|\.DSE\b|\.GSTPR\b|\.ENAB\b|\.FCON\b|\.FCOFF\b|AsysInterlock\b|AsysExtInterlock\b";
                                string pattern2 = @"OTE\(\w+_FBINT\d+\)";
                                string pattern3 = @"OSF\(([^),]*FBINT[^),]*),([^),]*FBINT[^),]*)\)";
                                if (Regex.IsMatch(result, pattern1) || Regex.IsMatch(result, pattern2) || Regex.IsMatch(result, pattern3))
                                {
                                    progress.Report($"Interlock rungs has been delete for .{prContent} interlock type where input operands");
                                    return null;
                                }

                                resultList.Add(result);
                            }
                            
                        }
                    }
                   
                }
            }
            else if (InputOperandForInterlock != null && (HMI_Interlock == null || HMI_Interlock.Count == 0))
            {
                resultList.Clear();
                LINK = GetLink(prContent);

                if (InputOperandForInterlock.Count > 0)
                {
                    for (int i = 0; i < InputOperandForInterlock.Count; i++)
                    {
                        string prefix = GetIntPrefix(prContent);
                        if (!prefixIndexMap.ContainsKey(prefix))
                        {
                            prefixIndexMap[prefix] = 1;
                        }

                        if (prContent == "MACH" || prContent == "OP" || prContent == "OP1" || prContent == "OP2" || prContent == "OP3" || prContent == "STI" || prContent == "STI1" || prContent == "STI2" || prContent == "DSE")
                        {
                            string newInt = extractedContent + prefix + prefixIndexMap[prefix].ToString("D2");
                            if (InputOperandForInterlock[i] != string.Empty)
                            {
                                rung1 = $"{InputOperandForInterlock[i]}OTE({newInt}.IN);";
                            }
                            else
                            {
                                rung1 = $"OTE({newInt}.IN);";
                            }

                            // Set rung2 based on prContent
                            if (prContent == "STI")
                            {
                                rung2 = $"[XIO(FB.RUN)]OTE({newInt}.EN);";
                            }
                            else if (prContent == "STI1")
                            {
                                rung2 = $"[XIO(FB.RUN1)]OTE({newInt}.EN);";
                            }
                            else if (prContent == "STI2")
                            {
                                rung2 = $"[XIO(FB.RUN2)]OTE({newInt}.EN);";
                            }
                            else
                            {
                                rung2 = $"[XIC(FB.AUT),XIC(FB.SS)]OTE({newInt}.EN);";
                            }
                            // Update MaxHMIInterlock with incremented value
                            MaxHMIInterlock = IncrementInterlock(MaxHMIInterlock);
                            rung3 = $"{ExtSelected}({newInt},{LINK}, {MaxHMIInterlock});";

                            resultList.Add(rung1);
                            resultList.Add(rung2);
                            resultList.Add(rung3);
                            prefixIndexMap[prefix]++; // Increment the index for this prefix
                            progress.Report($"Interlock rungs has been added for .{prContent} interlock type");
                        }
                        else if (prContent == "PR" || prContent == "SA" || prContent == "GSTI" || prContent == "GOP" || prContent == "GSTRR" || prContent == "RSTI" || prContent == "ROP" || prContent == "GSTPR" || prContent == "FCON" || prContent == "FCOFF")
                        {
                            string newInt = extractedContent + prefix + prefixIndexMap[prefix].ToString("D2");
                            if (InputOperandForInterlock[0] != string.Empty)
                            {
                                rung1 = $"{InputOperandForInterlock[0]}OTE({newInt}.IN);";
                            }
                            else
                            {
                                rung1 = $"OTE({newInt}.IN);";
                            }

                            MaxHMIInterlock = IncrementInterlock(MaxHMIInterlock);
                            rung3 = $"{ExtSelected}({newInt}, {EN}, {IN}, {LINK}, {MaxHMIInterlock});";
                            resultList.Add(rung1);
                            resultList.Add(rung3);
                            prefixIndexMap[prefix]++; // Increment the index for this prefix
                            progress.Report($"Interlock rungs has been added for .{prContent} interlock type");
                        }
                        else
                        {
                            string pattern1 = @"\bOTE\(\w+_FBINT\d+\)\b|\.PR\b|\.SA\b|\.MACH\b|\.OP\b|\.OP1\b|\.OP2\b|\.OP3\b|\.STI\b|\.STI1\b|\.STI2\b|\.GSTI\b|\.GOP\b|\.GSTRR\b|\.RSTI\b|\.ROP\b|\.DSE\b|\.GSTPR\b|\.ENAB\b|\.FCON\b|\.FCOFF\b|AsysInterlock\b|AsysExtInterlock\b";
                            if (Regex.IsMatch(result, pattern1))
                            {
                                progress.Report($"Interlock rungs has been delete for .{prContent} interlock type where input operands");
                                return null;
                            }

                            string pattern2 = @"OTE\(\w+_FBINT\d+\)";
                            if (Regex.IsMatch(result, pattern2))
                            {
                                return null;
                            }


                            // Check if "OTE(anything.FB.GON)" exists
                            if (Regex.IsMatch(result, @"OTE\(.+?\.FB\.GON\)"))
                            {
                                // Use regex to clear content before "OTE(anything.FB.GON)"
                                string modifiedResult = Regex.Replace(result, @".*?(?=OTE\(.+?\.FB\.GON\))", string.Empty);
                                resultList.Add(modifiedResult);
                            }
                            else if (Regex.IsMatch(result, @"OTE\(.+?\.FB\.GRDY\)"))
                            {
                                // Use regex to clear content before "OTE(anything.FB.GON)"
                                string modifiedResult = Regex.Replace(result, @".*?(?=OTE\(.+?\.FB\.GRDY\))", string.Empty);
                                resultList.Add(modifiedResult);
                            }
                            else if (Regex.IsMatch(result, @"OTE\(.+?\.FB\.GOFF\)"))
                            {
                                // Use regex to clear content before "OTE(anything.FB.GON)"
                                string modifiedResult = Regex.Replace(result, @".*?(?=OTE\(.+?\.FB\.GOFF\))", string.Empty);
                                resultList.Add(modifiedResult);
                            }
                            // Replace OTE(FB.DIR) with OTE(FB.PREQ1)
                            // Check if the result contains OTE(XXX_FB.DIR)
                            else if (Regex.IsMatch(result, @"OTE\(.+?\.FB\.DIR\)"))
                            {
                                if (datatype == "Valve")
                                {
                                    rung1 = Regex.Replace(result, @"OTE\((.+?)\.FB\.DIR\)", "OTE($1.FB.PREQ1)");

                                    // Match the new OTE(XXX_FB.PREQ1) in rung1
                                    Match match1 = Regex.Match(rung1, @"OTE\((.+?)\.FB\.PREQ1\)"); // Corrected to capture the content inside parentheses
                                    if (match1.Success)
                                    {
                                        string extractedContent1 = match1.Groups[1].Value;
                                        // Replace FB.DIR with FB.PREQ1 and FB.PREQ2
                                        string modifiedContent = $"{extractedContent1}.FB.PREQ1";
                                        string modifiedContent2 = $"{extractedContent1}.FB.PREQ2";
                                        // Wrap the modified content in XIO(XXX) and OTE(XXX)
                                        string wrappedContent = $"XIO({modifiedContent})";
                                        string wrappedContent2 = $"OTE({modifiedContent2})";

                                        // Combine the wrapped contents into rung2
                                        rung2 = $"{wrappedContent}{wrappedContent2};";

                                        // Add the original and modified rungs to the result list
                                        resultList.Add(rung1);
                                        resultList.Add(rung2);
                                    }
                                }

                                else
                                {
                                    string pattern4 = @"OTE\(\w+_FBINT\d+\)|\.PR\b|\.SA\b|\.MACH\b|\.OP\b|\.OP1\b|\.OP2\b|\.OP3\b|\.STI\b|\.STI1\b|\.STI2\b|\.GSTI\b|\.GOP\b|\.GSTRR\b|\.RSTI\b|\.ROP\b|\.DSE\b|\.GSTPR\b|\.ENAB\b|\.FCON\b|\.FCOFF\b|AsysInterlock\b|AsysExtInterlock\b";
                                    string pattern5 = @"OTE\(\w+_FBINT\d+\)";
                                    string pattern6 = @"OSF\(([^),]*FBINT[^),]*),([^),]*FBINT[^),]*)\)";
                                    if (Regex.IsMatch(result, pattern4) || Regex.IsMatch(result, pattern5) || Regex.IsMatch(result, pattern6))
                                    {
                                        return null;
                                    }
                                    resultList.Add(result);
                                }
                            }

                            else
                            {
                                resultList.Add(result);
                            }
                        }
                    }
                }

                else
                { 
                    if (Regex.IsMatch(result, @"OTE\(.+?\.FB\.DIR\)"))
                    {
                        if (datatype == "Valve")
                        {
                            rung1 = Regex.Replace(result, @"OTE\((.+?)\.FB\.DIR\)", "OTE($1.FB.PREQ1)");

                            // Match the new OTE(XXX_FB.PREQ1) in rung1
                            Match match1 = Regex.Match(rung1, @"OTE\((.+?)\.FB\.PREQ1\)"); // Corrected to capture the content inside parentheses
                            if (match1.Success)
                            {
                                string extractedContent1 = match1.Groups[1].Value;
                                // Replace FB.DIR with FB.PREQ1 and FB.PREQ2
                                string modifiedContent = $"{extractedContent1}.FB.PREQ1";
                                string modifiedContent2 = $"{extractedContent1}.FB.PREQ2";
                                // Wrap the modified content in XIO(XXX) and OTE(XXX)
                                string wrappedContent = $"XIO({modifiedContent})";
                                string wrappedContent2 = $"OTE({modifiedContent2})";

                                // Combine the wrapped contents into rung2
                                rung2 = $"{wrappedContent}{wrappedContent2};";

                                // Add the original and modified rungs to the result list
                                resultList.Add(rung1);
                                resultList.Add(rung2);
                            }
                        }

                        else
                        {
                            resultList.Add(result);
                        }

                    }

                    else
                    {
                        string pattern1 = @"OTE\(\w+_FBINT\d+\)|\.PR\b|\.SA\b|\.MACH\b|\.OP\b|\.OP1\b|\.OP2\b|\.OP3\b|\.STI\b|\.STI1\b|\.STI2\b|\.GSTI\b|\.GOP\b|\.GSTRR\b|\.RSTI\b|\.ROP\b|\.DSE\b|\.GSTPR\b|\.ENAB\b|\.FCON\b|\.FCOFF\b|AsysInterlock\b|AsysExtInterlock\b";
                        string pattern2 = @"OTE\(\w+_FBINT\d+\)";
                        string pattern3 = @"OSF\(([^),]*FBINT[^),]*),([^),]*FBINT[^),]*)\)";
                        if (Regex.IsMatch(result, pattern1) || Regex.IsMatch(result, pattern2) || Regex.IsMatch(result, pattern3))
                        {
                            return null;
                        }
                        else
                        {
                            resultList.Add(result);
                        }
                        
                    }                    
                }

            }
        }
        else
        {
            string pattern1 = @"OTE\(\w+_FBINT\d+\)|\.PR\b|\.SA\b|\.MACH\b|\.OP\b|\.OP1\b|\.OP2\b|\.OP3\b|\.STI\b|\.STI1\b|\.STI2\b|\.GSTI\b|\.GOP\b|\.GSTRR\b|\.RSTI\b|\.ROP\b|\.DSE\b|\.GSTPR\b|\.ENAB\b|\.FCON\b|\.FCOFF\b|AsysInterlock\b|AsysExtInterlock\b";
            string pattern2 = @"OTE\(\w+_FBINT\d+\)";
            string pattern3 = @"OSF\(([^),]*FBINT[^),]*),([^),]*FBINT[^),]*)\)";
            if (Regex.IsMatch(result, pattern1) || Regex.IsMatch(result, pattern2) || Regex.IsMatch(result, pattern3))
            {
                return null;
            }

            resultList.Add(result);
        }

        return resultList;
    }

    private string IncrementInterlock(string interlock)
    {
        // Use regex to find the number inside square brackets
        var match = interlockRegex.Match(interlock);
        if (match.Success)
        {
            if (HMI_INTERLOCK_INDEX == 0)
            {
                HMI_INTERLOCK_INDEX = int.Parse(match.Groups[1].Value);
            }

            HMI_INTERLOCK_INDEX++; // Increment the number
                                   // Replace the old number with the new incremented number
            interlock = interlockRegex.Replace(interlock, $"[{HMI_INTERLOCK_INDEX}]");
        }
        return interlock;
    }

    private string GetLink(string prContent)
    {
        return prContent switch
        {
            "PR" => "INTL",
            "SA" => "INTL_SEQ_STR1",
            "MACH" => "INTL",
            "OP" => "INTL_SEQ_STR1",
            "OP1" => "INTL_SEQ_STR1",
            "OP2" => "INTL_SEQ_STR2",
            "OP3" => "INTL_SEQ_STR3",
            "STI" => "INTL_SEQ_STR",
            "STI1" => "INTL_SEQ_STR1",
            "STI2" => "INTL_SEQ_STR2",
            "GSTI" => "INTL_SEQ_STR",
            "GOP" => "INTL",
            "GSTRR" => "INTL_SEQ_STR",
            "RSTI" => "INTL_SEQ_STR",
            "ROP" => "INTL",
            "DSE" => "INTL_SEQ_STP",
            "GSTPR" => "INTLSEQ_STP",
            "ENAB" => "INTL_ENAB",
            "FCON" => "INTL_FCON",
            "FCOFF" => "INTL_FCOFF",
            _ => "INTL"
        };
    }

    private string GetIntPrefix(string prContent)
    {
        return prContent switch
        {
            "OP" => "STR",
            "OP1" => "STR",
            "OP2" => "STR",
            "OP3" => "STR",
            "STI" => "STR",
            "STI1" => "STR",
            "STI2" => "STR",
            "DSE" => "STP",
            "GSTRR" => "STR",
            "GSTI" => "STR",
            "SA" => "STR",
            _ => "INT"
        };
    }

    public string GetElementAndSubElementReplacement(string oldText, List<string> fpDataType, RockwellUpgradeOptions options)
    {
        // Define a regex pattern to match the content inside parentheses
        string pattern = @"\(([^)]+)\)|\b\w+_FP\.\w+\b";
        string result = string.Empty;

        if (oldText != null) 
        {
            // Use Regex.Replace with a lambda expression to capture the dataType
            result = Regex.Replace(oldText, pattern, match => ReplaceMatch(match, fpDataType, options));
            
        }

        return result;
    }

    private string ReplaceMatch(Match match, List<string> fpDataType, RockwellUpgradeOptions options)
    {
        // Extract the matched text, which is inside the parentheses
        string matchedText = match.Groups[1].Value;

        if (matchedText == string.Empty) 
        {
            matchedText = match.Groups[0].Value;
        }

        // Define a regex pattern to find '_FP.' followed by the desired element
        string subPattern = @"(_FP\.(\w+))";

        // Check if the matched text contains '_FP.'
        var regex = new Regex(subPattern);
        var subMatch = regex.Match(matchedText);

        if (subMatch.Success)
        {
            // Extract the full match and the captured element
            string fullMatch = subMatch.Groups[1].Value;
            string element = subMatch.Groups[2].Value;
            string pattern = Regex.Escape(element);
            string extractedstring = Regex.Replace(matchedText, pattern, string.Empty);

            if (element == "AL_LL" || element == "AL_L" || element == "AL_H" || element == "AL_HH")
            {
                fpDataType.Add("ACESYS_FACEPLATE_ANALOG");
            }

            else if (element == "Alarm_HH_Enable" || element == "Alarm_H_Enable" || element == "Alarm_L_Enable" || element == "Alarm_LL_Enable")
            {
                fpDataType.Add("ACESYS_FACEPLATE_ANALOG");
            }

            else if (element == "AlarmDelay_Enable_HH" || element == "AlarmDelay_Enable_H" || element == "AlarmDelay_Enable_L" || element == "AlarmDelay_Enable_LL")
            {
                fpDataType.Add("ACESYS_FACEPLATE_ANALOG");
            }

            else if (element == "X_HH" || element == "X_H" || element == "X_L" || element == "X_LL")
            {
                fpDataType.Add("ACESYS_FACEPLATE_ANALOG");
            }

            else if (element == "AlarmLimit_HH" || element == "AlarmLimit_H" || element == "AlarmLimit_L" || element == "AlarmLimit_LL")
            {
                fpDataType.Add("ACESYS_FACEPLATE_ANALOG");
            }

            for (int i = 0; i < fpDataType.Count; i++)
            {
                DataRow[] Rows = dtFPMember.Select($"From_Attribute = '{element}' AND To_Version = '{fpDataType[i]}'");

                if (Rows.Length > 0)
                {
                    if (options.IsMapByFunction)
                    {
                        if (Rows[0].Table.Columns.Contains("To_Attribute_MapByFunction"))
                        {
                            var toAttributeMapByFunction = Rows[0]["To_Attribute_MapByFunction"]?.ToString();

                            if (!string.IsNullOrEmpty(toAttributeMapByFunction))
                            {
                                // Perform the replacement
                                matchedText = matchedText.Replace(fullMatch, $"_FP.{toAttributeMapByFunction}");
                                break;
                            }
                        }
                    }

                    else if ((options.IsMapByFunction == false) && (Rows[0]["To_Attribute_MapByName"] != string.Empty))
                    {
                        string replacement = Rows[0]["To_Attribute_MapByName"].ToString();
                        // Replace the matched text with the modified content
                        matchedText = matchedText.Replace(fullMatch, $"_FP.{replacement}");
                        break;
                    }

                }

            }

        }

        // Return the modified or original text with parentheses
        return $"({matchedText})";
    }

    public string GetConfigData(string key)
    {
        DataRow[] row = dtAnalogScanCounter.Select($"Key = '{key}'");
        if (row.Length > 0)
        {
            return row[0]["Config_Data"].ToString();
        }

        return null;
    }

    public DataRow[] GetTagsNameDataType(string dataType, RockwellUpgradeOptions options)
    {        

        dataType = dataType == "Unimotor" ? "Motor" : dataType == "AsysSel" ? "Sel" : dataType == "Department" ? "Dept" : dataType == "AsysRcp" ? "Rcp" : dataType == "Recipe" ? "Rcp" : dataType;

        DataRow[] rows;
        

        if (dataType == "Sel" && options.IsExtendedSelect)
        {
            rows = dtRungConversion.Select($"To_FB = 'AsysExtSelect'");
        }

        else if (dataType == "Positioner")
        {
            dataType = "Pos";
            rows = dtRungConversion.Select($"From_FB = 'Asys'+'{dataType}'");
        }

        else if (dataType == "Bimotor")
        {
            dataType = "Mot2";
            rows = dtRungConversion.Select($"From_FB = 'Asys'+'{dataType}'");
        }

        else
        {
            rows = dtRungConversion.Select($"To_FB = 'Asys'+'{dataType}'");
        }

        return rows;
    }

    public DataRow[] GetProgramTags(string dataType, string? selectedfield, RockwellUpgradeOptions options)
    {        
        string filterExpression = string.Empty;

        if (selectedfield == null)
        {
            return Array.Empty<DataRow>();
        }

        if (dataType == "AsysSel")
        {
            if (options.IsExtendedSelect)
            {
                selectedfield = "AsysExtSelect";
                filterExpression = $"To_FB = '{selectedfield}' AND (To_Rename IS NOT NULL OR To_Operator_Type IS NOT NULL)";
            }

            else
            {
                selectedfield = "AsysSel";
                filterExpression = $"From_FB = '{selectedfield}' AND To_FB = '{selectedfield}' AND (To_Rename IS NOT NULL OR To_Operator_Type IS NOT NULL)";
            }
        }

        else
        {
            filterExpression = $"From_FB = '{selectedfield}' AND (To_Rename IS NOT NULL OR To_Operator_Type IS NOT NULL)";
        }

        
        DataRow[] rows = dtRungConversion.Select(filterExpression);

        // Remove duplicate rows based on From_FB, To_Rename, and To_Operator_Type
        var distinctRows = rows
            .GroupBy(row => new
            {
                From_FB = row["From_FB"],
                To_Rename = row["To_Rename"],
                To_Operator_Type = row["To_Operator_Type"],
                To_New_Operator = row["To_New_Operator"]
            })
            .Select(group => group.First())
            .ToArray();

        return distinctRows;
    }

    public XmlNode ExtractDataFormat(string dataType)
    {
        DataRow[] rows = dtDetail.Select($"To_Object = '{dataType}'");
        return null;
    }

    public string GetElementConversionFB(string oldText, string dataType, IProgress<string> progress)
    {
        if (string.IsNullOrEmpty(oldText))
        {
            return null;
        }

        // Define the regex pattern to match any operator followed by content in parentheses
        string patternOperator = @"(\w+)\(([^)]+)\)";
        MatchCollection matches = Regex.Matches(oldText, patternOperator);

        foreach (Match match in matches)
        {
            string operatorName = match.Groups[1].Value;
            string content = match.Groups[2].Value;

            // Split the content to identify the relevant parts
            string[] parts = content.Split(new[] { "_FB." }, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                string prefix = parts[0];
                string fbMatch = parts[1];

                // Check for specific replacements in the DataTable
                DataRow[] rows = dtRungConversion.Select($"From_Pin_Name = '{fbMatch}'");
                if (rows.Length > 0)
                {
                    string replacedFbMatched = rows[0]["To_Pin_Name"].ToString();

                    if (replacedFbMatched == "Rule_Interlock" || replacedFbMatched == "Rule_InterlockStop")
                    {
                        continue;
                    }

                    if (replacedFbMatched == "")
                    {
                        return null;
                    }

                    string replacedContent;
                    if (replacedFbMatched.Contains("Rule_FP."))
                    {
                        string patternFP = @"Rule_FP\.(\w+)";
                        Match matchFP = Regex.Match(replacedFbMatched, patternFP);

                        if (matchFP.Success)
                        {
                            string fbMatchFP = matchFP.Groups[1].Value;
                            replacedContent = $"\\{prefix}.FB.{fbMatchFP}";

                            if (replacedContent.Contains(","))
                            {
                                // Split prefix into two parts based on comma separator
                                string[] prefixParts = prefix.Split(',');
                                if (prefixParts.Length == 2)
                                {
                                    // Construct the replaced content with two parts of prefix
                                    replacedContent = $"{prefixParts[0]},\\{prefixParts[1]}.FB.{replacedFbMatched}";
                                }
                                else
                                {
                                    // Handle the case where comma is not correctly placed
                                    // (You may want to add error handling or fallback logic here)
                                    replacedContent = $"\\{prefix}.FB.{replacedFbMatched}";
                                }
                            }

                            // Replace the old content with the new one in the oldText
                            return oldText = oldText.Replace(content, replacedContent);
                        }
                        else
                        {
                            replacedContent = $"\\{prefix}.FB.{replacedFbMatched}";
                        }
                    }
                    else
                    {
                        replacedContent = $"\\{prefix}.FB.{replacedFbMatched}";
                    }

                    // Check if replacedContent contains MOV operator
                    // Check if replacedContent contains comma separator
                    if (replacedContent.Contains(","))
                    {
                        // Split prefix into two parts based on comma separator
                        string[] prefixParts = prefix.Split(',');
                        if (prefixParts.Length == 2)
                        {
                            // Construct the replaced content with two parts of prefix
                            replacedContent = $"{prefixParts[0]},\\{prefixParts[1]}.FB.{replacedFbMatched}";
                        }
                        else
                        {
                            // Handle the case where comma is not correctly placed
                            // (You may want to add error handling or fallback logic here)
                            replacedContent = $"\\{prefix}.FB.{replacedFbMatched}";
                        }
                    }

                    // Replace the old content with the new one in the oldText
                    return oldText = oldText.Replace(content, replacedContent);
                }
            }
        }

        return oldText;
    }

    public string GetElementConversionADPT(string oldText, string dataType, IProgress<string> progress)
    {
        if (string.IsNullOrEmpty(oldText))
        {
            return null;
        }

        // Define the regex pattern to match content after _ADPT. and before )
        string pattern = @"(\w+_ADPT\.)(\w+)(\))";
        MatchCollection matches = Regex.Matches(oldText, pattern);

        foreach (Match match in matches)
        {
            string prefix = match.Groups[1].Value;   // The part before the content (e.g., "prefix_ADPT.")
            string fbMatch = match.Groups[2].Value;  // The content that needs replacement
            string suffix = match.Groups[3].Value;   // The closing parenthesis

            // Check for specific replacements in the DataTable
            DataRow[] rows = dtRungConversion.Select($"From_Pin_Name = '{fbMatch}'");
            if (rows.Length > 0)
            {
                string replacedADPTMatched = rows[0]["To_Pin_Name"].ToString();

                if (string.IsNullOrEmpty(replacedADPTMatched))
                {
                    return null;
                }

                // Perform the replacement in the oldText
                string replacedContent = $"{prefix}{replacedADPTMatched}{suffix}";

                oldText = oldText.Replace(match.Value, replacedContent);
            }
        }

        return oldText;
    }

    public string GetECSPointTypeReplacement(string replacement,string Unit, DbHelper dbHelper)
    {
        DataRow[] row = dtPointType.Select($"Point_Type_V7 = '{replacement}' AND Type = '{Unit}'");

        if (!string.IsNullOrEmpty(replacement) && !replacement.Contains("Interlock", StringComparison.OrdinalIgnoreCase) && row.Length == 0)
        {
            row = dtPointType.Select($"Point_Type_V7 = '{replacement}'");
        }

        if (row.Length == 0)
        {
            return null;
        }

        if (row != null) 
        {
            string newtext = row[0]["Point_Type_V8"].ToString();

            if (newtext != null)
            {
                return newtext;
            }
        }

        return null;
    }

    public string GetAppendDataXml(string pointTypeId)
    {
        DataRow[] row = dtPointType.Select($"Point_Type_V7 = '{pointTypeId}'");

        if (row.Length == 0)
        {
            return null;
        }

        if (row != null)
        {
            string newtext = row[0]["Append_Data"].ToString();

            if (!string.IsNullOrEmpty(newtext))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(newtext);

                XmlNode entityNode = xmlDoc.SelectSingleNode("//Entity");

                if (entityNode != null && entityNode.HasChildNodes)
                {
                    return entityNode.InnerXml;
                }
            }
        }

        return null;
    }

    public XmlNode GetFormatNodeECSPoints(string pointTypeId)
    {
        DataRow[] row = dtPointType.Select($"Point_Type_V8 = '{pointTypeId}'");

        if (row.Length == 0)
        {
            return null;
        }

        if (row != null)
        {
            string formatNode = row[0]["Format_Data"].ToString();

            if (!string.IsNullOrEmpty(formatNode)) 
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(formatNode);

                // Get the root node as XmlNode
                XmlNode xmlNode = xmlDoc.DocumentElement;

                if (xmlNode != null && xmlNode.HasChildNodes)
                {
                    return xmlNode.FirstChild;
                }

                return null;
            }            
        }

        return null;

    }

    public string ExtractpLinkInterlockElement(string dotContent)
    {
        DataRow[] row = dtInterlock.Select($"Element = '{dotContent}'");

        if (row.Length == 0)
        {
            return null;
        }

        if (row != null)
        {
            string pLINK = row[0]["pLINK"].ToString();
            return pLINK;           
        }

        return null;


    }

    public string GetInterlockAppendDataXml(string newPointTypeIdValue)
    {
        DataRow[] row = dtPointType.Select($"Point_Type_V8 = '{newPointTypeIdValue}'");

        if (row.Length == 0)
        {
            return null;
        }

        if (row != null)
        {
            string newtext = row[0]["Append_Data"].ToString();

            if (!string.IsNullOrEmpty(newtext))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(newtext);

                XmlNode entityNode = xmlDoc.SelectSingleNode("//Entity");

                if (entityNode != null && entityNode.HasChildNodes)
                {
                    return entityNode.InnerXml;
                }
            }
        }

        return null;
    }

    public string ExtractIntSuffixValueInterlock(string designationValue)
    {
        DataRow[] row = dtInterlock.Select($"Element = '{designationValue}'");

        if (row.Length == 0)
        {
            return null;
        }

        if (row != null)
        {
            string IntSuffix = row[0]["IntSuffix"].ToString();
            return IntSuffix;
        }

        return null;
    }

    public string? GetLanguageDataXml(string pointTypeId)
    {
        DataRow[] row = dtPointType.Select($"Point_Type_V7 = '{pointTypeId}'");

        if (row.Length == 0)
        {
            return null;
        }

        if (row != null)
        {
            string newtext = row[0]["Language_Data"].ToString();

            if (!string.IsNullOrEmpty(newtext))
            {
                return newtext;
            }
        }

        return null;
    }

    public string ApplyTextConversionSimulationRungs(string dataType, string processedText)
    {
        if (dataType == "Simulation")
        {
            // Define the regex patterns to capture the desired elements
            string fpPattern = @"FP\.([^\)]+)\)";
            string simPattern = @"SIM\.([^\)]+)\)";

            // Extract the elements using regex
            var fpMatch = Regex.Match(processedText, fpPattern);
            var simMatch = Regex.Match(processedText, simPattern);

            if (fpMatch.Success || simMatch.Success)
            {
                // Extract FP and SIM elements
                string fpElement = fpMatch.Groups[1].Value;
                string simElement = simMatch.Groups[1].Value;

                // Look up the replacements in the DataTable
                DataRow[] rows = dtRungConversion.Select($"From_Pin_Name = '{fpElement}'");
                string fpElementReplacement = rows.Length > 0 ? rows[0]["To_Pin_Name"].ToString() : fpElement;                

                rows = dtRungConversion.Select($"From_Pin_Name = '{simElement}'");
                string simElementReplacement = rows.Length > 0 ? rows[0]["To_Pin_Name"].ToString() : simElement;

                if (fpElementReplacement == string.Empty && simElementReplacement == string.Empty)
                {
                    return null;
                }

                // Replace the elements in the processedText
                processedText = Regex.Replace(processedText, @"FP\." + Regex.Escape(fpElement) + @"\)", $"FP.{fpElementReplacement})");
                processedText = Regex.Replace(processedText, @"SIM\." + Regex.Escape(simElement) + @"\)", $"SIM.{simElementReplacement})");
            }
        }

        return processedText;
    }

    public List<string> DataTypeListToBeDeleted()
    {        
        List<string> blockList = new List<string>();
        
        if (dtUDTSiemens != null)
        {           
            foreach (DataRow row in dtUDTSiemens.Rows)
            {                
                string fromV77Name = row["From_V77_Name"]?.ToString();
                
                if (!string.IsNullOrEmpty(fromV77Name))
                {
                    blockList.Add(fromV77Name);
                }
            }
        }
        
        return blockList;
    }

    public List<string> FunctionalBlockListToBeDeleted()
    {
        List<string> blockList = new List<string>();

        if (dtFBSiemens != null)
        {
            foreach (DataRow row in dtFBSiemens.Rows)
            {
                string fromV77Name = row["V77_Name"]?.ToString();

                if (!string.IsNullOrEmpty(fromV77Name))
                {
                    blockList.Add(fromV77Name);
                }
            }
        }

        return blockList;
    }

    public List<string> SFBListToBeDeleted()
    {
        List<string> blockList = new List<string>();

        if (dtSFBSiemens != null)
        {
            foreach (DataRow row in dtSFBSiemens.Rows)
            {
                string fromV77Name = row["V77_Name"]?.ToString();

                if (!string.IsNullOrEmpty(fromV77Name))
                {
                    blockList.Add(fromV77Name);
                }
            }
        }

        return blockList;
    }
}

