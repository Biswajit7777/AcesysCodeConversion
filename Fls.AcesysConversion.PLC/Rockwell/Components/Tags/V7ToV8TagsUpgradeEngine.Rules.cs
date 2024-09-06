using System.Xml;

namespace Fls.AcesysConversion.PLC.Rockwell.Components.Tags;

public partial class V7ToV8TagsUpgradeEngine : UpgradeEngine
{
    public static bool Apply_Rule_14(L5XTag originalTag, L5XTag tagToModify)
    {
        XmlNode? originalDataValueMember = originalTag.GetSingleDataValueMember("Repeat_Time_Pre");
        XmlNode? newDataValueMember = tagToModify.GetSingleDataValueMember("Enable_Repeat_Alarm");

        //if originalTag member "Repeat_Time_Pre" is not equal to 0, then move tagToModify member "Enable_Repeat_Alarm" value 1
        //if originalTag member "Repeat_Time_Pre" is equal to 0, then move tagToModify member "Enable_Repeat_Alarm" value 0

        bool isRuleAppliedSuccessfully;
        if (originalDataValueMember != null && newDataValueMember != null)
        {
            XmlNode newAttr = newDataValueMember.Attributes?.GetNamedItem("Value")!;
            XmlNode originalAttr = originalDataValueMember.Attributes?.GetNamedItem("Value")!;

            if (originalAttr.Value != "0")
            {
                newAttr.Value = "0";
                _ = (newDataValueMember.Attributes?.SetNamedItem(newAttr));
            }
            else
            {
                newAttr.Value = "1";
                _ = (newDataValueMember.Attributes?.SetNamedItem(newAttr));
            }
            isRuleAppliedSuccessfully = true;
        }        
        else
        {
            isRuleAppliedSuccessfully = false;
        }
        return isRuleAppliedSuccessfully;
    }




}