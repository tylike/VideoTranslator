using System;
using System.Linq;
using VT.Module.BusinessObjects;

namespace TimeLine.Controls;

public class TrackControl(TrackInfo Info, SimpleTrackControl Control, TrackHeaderControl Header)
{
    public TrackInfo Info { get; init; } = Info;
    public SimpleTrackControl Control { get; set; } = Control;
    public TrackHeaderControl Header { get; set; } = Header;
}
