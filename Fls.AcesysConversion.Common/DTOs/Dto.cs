namespace Fls.AcesysConversion.Common.DTOs
{
    public record Dto(string FromObject, string ToObject, string XmlStandard);
    public record FpMemberDto(string From_Object,
                                string To_Object,
                                string From_Attribute,
                                string To_Attribute);
    public record ProgressDto(string FromObject, string ToObject, string TypeOfNode);


}
