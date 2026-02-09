using DevExpress.Xpo;
using System;
using System.Linq;
using VideoTranslator.SRT.Core.Models;
using TrackMenuAttributes;
using VT.Core;

namespace VT.Module.BusinessObjects;

public class SRTClip(Session s) : Clip(s),ISRTOperate,ISrtSubtitle
{
    public override MediaType Type => MediaType.Subtitles;

    public string Speaker
    {
        get { return field; }
        set { SetPropertyValue("Speaker", ref field, value); }
    }
    public override string DisplayText => string.IsNullOrEmpty(Speaker) ? Text : $"{Speaker}:{Text}";

    public AudioClip TTSReference
    {
        get { return field; }
        set { SetPropertyValue("TTSReference", ref field, value); }
    }


    public Task Translate(Language targetLanguage)
    {
        throw new NotImplementedException();
    }

    public Task TTS()
    {
        throw new NotImplementedException();
    }

    string ISrtSubtitle.ToSrtTimeString()
    {
        throw new NotImplementedException();
    }

    #region 上下文菜单方法

    [ContextMenuAction("翻译字幕", Order = 10, Group = "翻译")]
    public async Task TranslateSubtitle()
    {
        await Translate(Language.Chinese);
    }

    [ContextMenuAction("生成TTS音频", Order = 20, Group = "音频")]
    public async Task GenerateTTS()
    {
        await TTS();
    }

    [ContextMenuAction("编辑文本", Order = 10, Group = "编辑")]
    public void EditText()
    {
    }

    #endregion
}
