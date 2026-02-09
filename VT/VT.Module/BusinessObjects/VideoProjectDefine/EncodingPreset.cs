#nullable enable
using System;
using System.Linq;

namespace VT.Module.BusinessObjects;

public enum EncodingPreset
{
    [XafDisplayName("极快 (质量最差)")]
    UltraFast,
    [XafDisplayName("超快")]
    SuperFast,
    [XafDisplayName("非常快")]
    VeryFast,
    [XafDisplayName("比较快")]
    Faster,
    [XafDisplayName("快")]
    Fast,
    [XafDisplayName("平衡 (默认)")]
    Medium,
    [XafDisplayName("慢 (质量较好)")]
    Slow,
    [XafDisplayName("很慢")]
    Slower,
    [XafDisplayName("极慢 (质量最好)")]
    VerySlow
}
