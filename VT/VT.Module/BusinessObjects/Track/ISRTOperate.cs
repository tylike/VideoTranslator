using VT.Core;

namespace VT.Module.BusinessObjects;

public interface ISRTOperate
{
    /// <summary>
    /// 将所有段落的内容翻译为目标语言
    /// </summary>
    /// <returns></returns>
    Task Translate(Language targetLanguage);

    /// <summary>
    /// 将所有字幕转为语音
    /// </summary>
    /// <returns></returns>
    Task TTS();
}

