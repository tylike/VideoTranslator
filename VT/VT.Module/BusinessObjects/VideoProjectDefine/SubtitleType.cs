#nullable enable
using System;
using System.Linq;

namespace VT.Module.BusinessObjects;

public enum SubtitleType
{
    [XafDisplayName("硬烧录")]
    HardBurn,
    [XafDisplayName("软字幕")]
    SoftSubtitle
}
