using DevExpress.Xpo;
using System;
using System.Linq;
using VideoTranslator.Models;

namespace VT.Module.BusinessObjects;

public class SpeakerDiarizationClip : Clip
{
    public override MediaType Type => MediaType.说话人识别;

    public SpeakerDiarizationClip(Session s) : base(s)
    {
    }

    public override string DisplayText => $"{Speaker} ({(End - Start).TotalSeconds:0.##}s)";

    public string Speaker
    {
        get { return GetPropertyValue<string>(nameof(Speaker)); }
        set { SetPropertyValue(nameof(Speaker), value); }
    }
}
