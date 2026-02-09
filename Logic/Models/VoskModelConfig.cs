namespace VideoTranslator.Models;

public class VoskModelConfig
{
    public string TwoLetterLanguageCode { get; set; } = string.Empty;

    public string LanguageName { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public override string ToString()
    {
        return LanguageName;
    }
}
