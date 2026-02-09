using FlaUI.Core.Definitions;

namespace PublishToBilibili.Models
{
    public enum PublishScenario
    {
        SelfMade,
        Repost
    }

    public enum FormFieldType
    {
        Title,
        Type,
        Category,
        Tags,
        Description,
        SourceAddress,
        OriginalWatermarkCheckbox,
        NoRepostCheckbox,
        AcceptTermsCheckbox,
        MoreOptionsText,
        TagButtons
    }
}
