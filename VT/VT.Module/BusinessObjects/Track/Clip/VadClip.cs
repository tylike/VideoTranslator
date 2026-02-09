using DevExpress.Xpo;
using System;
using System.Linq;
using VadTimeProcessor.Models;


namespace VT.Module.BusinessObjects;

public class VadClip : Clip
{
    public override MediaType Type => MediaType.Vad;

    public VadClip(Session s) : base(s)
    {
    }
    public override string DisplayText => (this.End - this.Start).TotalSeconds.ToString("0.##");
}

