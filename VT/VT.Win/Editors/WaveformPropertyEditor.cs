using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Xpo;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NAudio.Wave;
using VT.Module.BusinessObjects;
using VT.Win.Forms;

namespace VT.Win.Editors;

[PropertyEditor(typeof(AudioSource), "Waveform", false)]
public class WaveformPropertyEditor : PropertyEditor, IComplexControl, IComplexViewItem
{
    #region ctor
    public WaveformPropertyEditor(Type type, IModelMemberViewItem viewItem) : base(type, viewItem)
    {
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            waveformControl?.Dispose();
            waveformControl = null;
        }
        base.Dispose(disposing);
    }
    #endregion

    #region 创建控件
    private WaveformControl waveformControl;
    private IObjectSpace _os;
    private XafApplication Application;

    protected override object CreateControlCore()
    {
        if (waveformControl == null)
        {
            waveformControl = new WaveformControl(new WaveformData());
            waveformControl.Dock = DockStyle.Fill;
            ReadValueCore();
        }
        return waveformControl;
    }

    private WaveformData LoadWaveformData(AudioSource audioSource)
    {
        if (audioSource == null || string.IsNullOrEmpty(audioSource.FileFullName) || !File.Exists(audioSource.FileFullName))
        {
            return new WaveformData
            {
                FileName = string.Empty,
                FilePath = string.Empty,
                Samples = Array.Empty<float>(),
                Duration = TimeSpan.Zero,
                SampleRate = 0,
                VadSegments = Array.Empty<VadSegmentInfo>(),
                Clips = Array.Empty<TimeLineClip>()
            };
        }

        try
        {
            using var audioFile = new AudioFileReader(audioSource.FileFullName);
            var samples = new System.Collections.Generic.List<float>();
            var buffer = new float[audioFile.WaveFormat.SampleRate * 10];
            int samplesRead;

            while ((samplesRead = audioFile.Read(buffer, 0, buffer.Length)) > 0)
            {
                samples.AddRange(buffer.Take(samplesRead));
            }

            var vadSegments = LoadVadSegments(audioSource);
            var clips = LoadClips(audioSource);

            return new WaveformData
            {
                FileName = Path.GetFileName(audioSource.FileFullName),
                FilePath = audioSource.FileFullName,
                Samples = samples.ToArray(),
                Duration = audioFile.TotalTime,
                SampleRate = audioFile.WaveFormat.SampleRate,
                VadSegments = vadSegments,
                Clips = clips
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载波形数据失败 {audioSource.FileFullName}: {ex.Message}");
            return new WaveformData
            {
                FileName = Path.GetFileName(audioSource.FileFullName),
                FilePath = audioSource.FileFullName,
                Samples = Array.Empty<float>(),
                Duration = TimeSpan.Zero,
                SampleRate = 0,
                VadSegments = Array.Empty<VadSegmentInfo>(),
                Clips = Array.Empty<TimeLineClip>()
            };
        }
    }

    private VadSegmentInfo[] LoadVadSegments(AudioSource audioSource)
    {
        if (audioSource.VadSegments == null || audioSource.VadSegments.Count == 0)
        {
            return Array.Empty<VadSegmentInfo>();
        }

        var segments = new VadSegmentInfo[audioSource.VadSegments.Count];
        for (int i = 0; i < audioSource.VadSegments.Count; i++)
        {
            var vadSegment = audioSource.VadSegments[i];
            segments[i] = new VadSegmentInfo
            {
                StartMS = vadSegment.StartMS,
                EndMS = vadSegment.EndMS,
                Index = vadSegment.Index
            };
        }

        return segments;
    }

    private TimeLineClip[] LoadClips(AudioSource audioSource)
    {
        if (audioSource.VideoProject == null || audioSource.VideoProject.Clips == null || audioSource.VideoProject.Clips.Count == 0)
        {
            return Array.Empty<TimeLineClip>();
        }

        return audioSource.VideoProject.Clips.ToArray();
    }
    #endregion

    #region 编辑器功能实现，数据库的交互
    #region load from db
    bool IsLoading;

    protected override void ReadValueCore()
    {
        
        IsLoading = true;
        try
        {
            var audioSource = PropertyValue as AudioSource;

            if (audioSource != null && waveformControl != null)
            {
                //audioSource.VideoProject.Clips
                var waveformData = LoadWaveformData(audioSource);
                waveformControl = new WaveformControl(waveformData);
                waveformControl.Dock = DockStyle.Fill;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    bool _isRefreshing;

    public override void Refresh()
    {
        _isRefreshing = true;
        ReadValueCore();
        base.Refresh();
        _isRefreshing = false;
    }
    #endregion

    #region get control value
    protected override object GetControlValueCore()
    {
        return null;
    }
    #endregion

    #region setup
    public void Setup(IObjectSpace objectSpace, XafApplication application)
    {
        this._os = objectSpace;
        this.Application = application;
    }
    #endregion
    #endregion
}
