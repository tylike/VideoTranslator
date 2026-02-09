using CommunityToolkit.Mvvm.ComponentModel;
using VT.Module.BusinessObjects;

namespace TimeLine.Models;

public partial class TimeLineModel : ObservableObject
{
    #region Properties

    [ObservableProperty]
    private double _totalDuration;

    [ObservableProperty]
    private double _zoomFactor = 1.0;

    [ObservableProperty]
    private double _leftPanelWidth = 150;

    [ObservableProperty]
    private ObservableCollection<TrackInfo> _rows = new();

    #endregion

    #region Computed Properties

    partial void OnZoomFactorChanged(double value)
    {
        OnPropertyChanged(nameof(ScaledTotalDuration));
    }

    public double ScaledTotalDuration => TotalDuration * ZoomFactor * 100.0;

    #endregion
}
