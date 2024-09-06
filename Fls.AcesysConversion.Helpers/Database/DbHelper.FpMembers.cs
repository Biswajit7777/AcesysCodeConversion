using Fls.AcesysConversion.Common;
using Fls.AcesysConversion.Common.DTOs;
using System.Data;

namespace Fls.AcesysConversion.Helpers.Database;

public partial class DbHelper
{
    private readonly DataTable dtFpMembers = new("FP_MEMBERS");
    private readonly DataTable dtAddOnMembers = new("ADDON_MEMBERS");

    public List<FpMemberDto> GetFpMembers(RockwellUpgradeOptions options)
    {
        List<FpMemberDto> fpMembers = new();

        foreach (DataRow fpMemberRow in dtFpMembers.Rows!)
        {
            string fromObject = fpMemberRow.Field<string>("From_Object") ?? string.Empty;
            string toObject = fpMemberRow.Field<string>("To_Object") ?? string.Empty;
            string fromAttribute = fpMemberRow.Field<string>("From_Attribute") ?? string.Empty;

            string toAttribute = options.IsMapByFunction
                ? fpMemberRow.Field<string>("To_Attribute_MapByFunction") ?? string.Empty
                : fpMemberRow.Field<string>("To_Attribute_MapByName") ?? string.Empty;
            fpMembers.Add(new FpMemberDto(fromObject, toObject, fromAttribute, toAttribute));
        }
        return fpMembers;
    }

    public List<FpMemberDto> GetAddOnMembers(RockwellUpgradeOptions options)
    {
        List<FpMemberDto> addOnMembers = new();

        foreach (DataRow addOnMemberRow in dtAddOnMembers.Rows!)
        {
            string fromObject = addOnMemberRow.Field<string>("From_Object") ?? string.Empty;
            string toObject = addOnMemberRow.Field<string>("To_Object") ?? string.Empty;
            string fromAttribute = addOnMemberRow.Field<string>("From_Attribute") ?? string.Empty;

            string toAttribute = options.IsMapByFunction
                ? addOnMemberRow.Field<string>("To_Attribute_MapByFunction") ?? string.Empty
                : addOnMemberRow.Field<string>("To_Attribute_MapByName") ?? string.Empty;
            addOnMembers.Add(new FpMemberDto(fromObject, toObject, fromAttribute, toAttribute));
        }
        return addOnMembers;
    }

}

