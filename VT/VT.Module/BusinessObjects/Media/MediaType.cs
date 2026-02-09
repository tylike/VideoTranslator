using System;
using System.Linq;

namespace VT.Module.BusinessObjects;

public enum MediaType
{
    源视频,
    静音视频,
    源音频,
    说话音频,

    Video,
    Audio,
    Image,
    Subtitles,


    源字幕,
    目标字幕,
    源字幕Vad,
    源字幕下载,
    TTS分段,
    调整音频段,
    目标音频,
    背景音频,
    时间线,
    Vad,
    None,
    合并后字幕,
    源音分段,
    说话人识别
}
