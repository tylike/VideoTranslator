using DevExpress.Xpo;

namespace VT.Module.BusinessObjects;

public class TimeScaleClip(Session s) : Clip(s)
{
    public override MediaType Type => MediaType.Video;
}
