#nullable enable
using DevExpress.ExpressApp.Model;
using DevExpress.Xpo;

namespace VT.Module.BusinessObjects;

public partial class VideoProject
{
    [XafDisplayName("使用 GPU 加速")]
    public bool UseGPU
    {
        get { return GetPropertyValue<bool>(nameof(UseGPU)); }
        set { SetPropertyValue(nameof(UseGPU), value); }
    }

    [XafDisplayName("速度优化级别")]
    public EncodingPreset EncodingPreset
    {
        get { return GetPropertyValue<EncodingPreset>(nameof(EncodingPreset)); }
        set { SetPropertyValue(nameof(EncodingPreset), value); }
    }

    [XafDisplayName("极速字幕渲染")]
    public bool FastSubtitleRendering
    {
        get { return GetPropertyValue<bool>(nameof(FastSubtitleRendering)); }
        set { SetPropertyValue(nameof(FastSubtitleRendering), value); }
    }

    [XafDisplayName("写入源字幕")]
    public bool WriteSourceSubtitle
    {
        get { return GetPropertyValue<bool>(nameof(WriteSourceSubtitle)); }
        set { SetPropertyValue(nameof(WriteSourceSubtitle), value); }
    }

    [XafDisplayName("写入目标字幕")]
    public bool WriteTargetSubtitle
    {
        get { return GetPropertyValue<bool>(nameof(WriteTargetSubtitle)); }
        set { SetPropertyValue(nameof(WriteTargetSubtitle), value); }
    }

    [XafDisplayName("源字幕类型")]
    public SubtitleType SourceSubtitleType
    {
        get { return GetPropertyValue<SubtitleType>(nameof(SourceSubtitleType)); }
        set { SetPropertyValue(nameof(SourceSubtitleType), value); }
    }

    [XafDisplayName("目标字幕类型")]
    public SubtitleType TargetSubtitleType
    {
        get { return GetPropertyValue<SubtitleType>(nameof(TargetSubtitleType)); }
        set { SetPropertyValue(nameof(TargetSubtitleType), value); }
    }


    #region ffmpeg output setting
    public string GetFFmpegVideoEncoder()
    {
        return UseGPU ? "h264_nvenc" : "libx264";
    }

    public string GetFFmpegHWAccel()
    {
        return UseGPU ? "cuda" : "";
    }

    public string GetFFmpegPreset()
    {
        return UseGPU ? "p4" : EncodingPreset switch
        {
            EncodingPreset.UltraFast => "ultrafast",
            EncodingPreset.SuperFast => "superfast",
            EncodingPreset.VeryFast => "veryfast",
            EncodingPreset.Faster => "faster",
            EncodingPreset.Fast => "fast",
            EncodingPreset.Medium => "medium",
            EncodingPreset.Slow => "slow",
            EncodingPreset.Slower => "slower",
            EncodingPreset.VerySlow => "veryslow",
            _ => "fast"
        };
    }

    public int GetCRFValue()
    {
        return EncodingPreset switch
        {
            EncodingPreset.UltraFast => 28,
            EncodingPreset.SuperFast => 26,
            EncodingPreset.VeryFast => 24,
            EncodingPreset.Faster => 23,
            EncodingPreset.Fast => 22,
            EncodingPreset.Medium => 23,
            EncodingPreset.Slow => 20,
            EncodingPreset.Slower => 18,
            EncodingPreset.VerySlow => 17,
            _ => 23
        };
    }
#endregion

    [XafDisplayName("B站发布标题")]
    [Size(500)]
    public string BilibiliPublishTitle
    {
        get { return GetPropertyValue<string>(nameof(BilibiliPublishTitle)); }
        set { SetPropertyValue(nameof(BilibiliPublishTitle), value); }
    }

    [XafDisplayName("B站发布标题(中文)")]
    [Size(500)]
    public string BilibiliPublishTitleChinese
    {
        get { return GetPropertyValue<string>(nameof(BilibiliPublishTitleChinese)); }
        set { SetPropertyValue(nameof(BilibiliPublishTitleChinese), value); }
    }

    [XafDisplayName("B站发布简介")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "5")]
    public string BilibiliPublishDescription
    {
        get { return GetPropertyValue<string>(nameof(BilibiliPublishDescription)); }
        set { SetPropertyValue(nameof(BilibiliPublishDescription), value); }
    }

    [XafDisplayName("B站发布标签")]
    [Size(SizeAttribute.Unlimited)]
    public string BilibiliPublishTags
    {
        get { return GetPropertyValue<string>(nameof(BilibiliPublishTags)); }
        set { SetPropertyValue(nameof(BilibiliPublishTags), value); }
    }

    [XafDisplayName("B站发布类型")]
    [Size(50)]
    public string BilibiliPublishType
    {
        get { return GetPropertyValue<string>(nameof(BilibiliPublishType)); }
        set { SetPropertyValue(nameof(BilibiliPublishType), value); }
    }

    [XafDisplayName("B站发布-是否转载")]
    public bool BilibiliPublishIsRepost
    {
        get { return GetPropertyValue<bool>(nameof(BilibiliPublishIsRepost)); }
        set { SetPropertyValue(nameof(BilibiliPublishIsRepost), value); }
    }

    [XafDisplayName("B站发布-来源地址")]
    [Size(SizeAttribute.Unlimited)]
    [ModelDefault("RowCount", "1")]
    public string BilibiliPublishSourceAddress
    {
        get { return GetPropertyValue<string>(nameof(BilibiliPublishSourceAddress)); }
        set { SetPropertyValue(nameof(BilibiliPublishSourceAddress), value); }
    }

    [XafDisplayName("B站发布-保留原水印")]
    public bool BilibiliPublishEnableOriginalWatermark
    {
        get { return GetPropertyValue<bool>(nameof(BilibiliPublishEnableOriginalWatermark)); }
        set { SetPropertyValue(nameof(BilibiliPublishEnableOriginalWatermark), value); }
    }

    [XafDisplayName("B站发布-禁止转载")]
    public bool BilibiliPublishEnableNoRepost
    {
        get { return GetPropertyValue<bool>(nameof(BilibiliPublishEnableNoRepost)); }
        set { SetPropertyValue(nameof(BilibiliPublishEnableNoRepost), value); }
    }

}
