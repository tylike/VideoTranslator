using System;
using System.Linq;
using VT.Module.BusinessObjects;

namespace TimeLine.Controls;

public class Tracks(StackPanel left, StackPanel right)
{
    StackPanel left = left;
    StackPanel right = right;

    public List<TrackControl> TrackControls { get; } = new List<TrackControl>();

    public void Add(TrackControl trackControl)
    {
        var has = false;
        if (trackControl.Info.Oid != -1)
        {
            has = TrackControls.Any(x => x.Info.Oid == trackControl.Info.Oid);
        }
        else
        {
            has = TrackControls.Any(x => x.GetHashCode() == trackControl.Info.GetHashCode());
        }
        if (has)
        {
            var inf = trackControl.Info;
            throw new Exception($"已经存在实例:{inf.Oid},{inf.Media?.MediaType.ToString()}");
        }
        TrackControls.Add(trackControl);
        left.Children.Add(trackControl.Header);
        right.Children.Add(trackControl.Control);
    }

    public void Remove(TrackControl trackControl)
    {
        TrackControls.Remove(trackControl);
        left.Children.Remove(trackControl.Header);
        right.Children.Remove(trackControl.Control);
    }

    public void Clear()
    {
        TrackControls.Clear();
        left.Children.Clear();
        right.Children.Clear();
    }

    public int Count => TrackControls.Count;
    public TrackControl? Find(TrackInfo trackInfo)
    {
        return TrackControls.FirstOrDefault(x => x.Info.Oid == trackInfo.Oid);
    }

    public void Zoom(double value)
    {
        foreach (var control in TrackControls)
        {
            control.Control.ZoomFactor = value;
        }
    }
    public void UpdateAllTracksDuration(double value)
    {
        foreach (var control in TrackControls)
        {
            control.Control.TotalDuration = value;
        }
    }

    public int IndexOf(TrackInfo trackInfo)
    {
        for (int i = 0; i < TrackControls.Count; i++)
        {
            if (TrackControls[i].Info.Oid == trackInfo.Oid)
            {
                return i;
            }
        }
        return -1;
    }

    public TrackInfo this[int index]
    {
        get { return TrackControls[index].Info; }
        set { }
    }
}