using DevExpress.ExpressApp.Model;
using DevExpress.Xpo;
using System.Diagnostics;
using System.Text;
using TrackMenuAttributes;

namespace VT.Module.BusinessObjects;

public abstract class TrackInfo(Session s) : VTBaseObject(s)
{
    #region Media Properties

    [XafDisplayName("媒体源")]
    public MediaSource Media
    {
        get { return field; }
        set { SetPropertyValue("Media", ref field, value); }
    }

    MediaType? type;
    public MediaType TrackType
    {
        get { return type ?? Media?.MediaType ?? MediaType.None; }
        set { SetPropertyValue("TrackType", ref type, value); }
    }


    [Association, Aggregated]
    public virtual XPCollection<Clip> Segments => GetCollection<Clip>(nameof(Segments));

    [Association]
    public VideoProject VideoProject
    {
        get { return field; }
        set { SetPropertyValue("VideoProject", ref field, value); }
    }

    #endregion

    //public SRTTrackInfo MainSrtTrack => VideoProject.Tracks.OfType<SRTTrackInfo>().SingleOrDefault();

    #region UI Display Properties

    [XafDisplayName("轨道标题")]
    [Size(100)]
    public string Title
    {
        get
        {
            var title = GetPropertyValue<string>(nameof(Title));
            if (!IsLoading && !IsSaving && string.IsNullOrEmpty(title))
            {
                return TrackType.ToString();
            }
            return title;
        }
        set { SetPropertyValue(nameof(Title), value); }
    }

    [XafDisplayName("轨道高度")]
    public double Height
    {
        get
        {
            var h = GetPropertyValue<double>(nameof(Height));
            if (!IsLoading && !IsSaving && h <= 0)
            {
                return 50;
            }
            return h;
        }
        set { SetPropertyValue(nameof(Height), value); }
    }

    [XafDisplayName("轨道颜色")]
    [Size(20)]
    public string Color
    {
        get { return GetPropertyValue<string>(nameof(Color)) ?? "#00FF00"; }
        set { SetPropertyValue(nameof(Color), value); }
    }

    [XafDisplayName("轨道索引")]
    public int Index
    {
        get { return GetPropertyValue<int>(nameof(Index)); }
        set { SetPropertyValue(nameof(Index), value); }
    }

    #endregion

    [ContextMenuAction("打开媒体位置")]
    public void OpenMediaLocation()
    {
        if (Media != null)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{this.Media.FileFullName}\"",
                UseShellExecute = true
            });
        }
        else
        {
            throw new Exception("错误，没有媒体文件!");
        }
    }

    public override void AfterConstruction()
    {
        base.AfterConstruction();
        Visible = true;
    }

    public bool Visible
    {
        get { return field; }
        set { SetPropertyValue("Visible", ref field, value); }
    }

    [ContextMenuAction("隐藏轨道", Tooltip = "隐藏当前轨道", IsAutoCommit = true)]
    public void Hide()
    {
        Visible = false;
    }


    [ContextMenuAction("删除轨道", Tooltip ="删除当前轨道",IsAutoCommit = true)]
    public void DeleteTrack()
    {
        this.VideoProject.Tracks.Remove(this);
        this.Delete();
    }

    [Size(-1)]
    public string ErrorMessage
    {
        get { return field; }
        set { SetPropertyValue("ErrorMessage", ref field, value); }
    }

    public virtual void Validate()
    {
        var sb = new StringBuilder();
        foreach (var item in Segments)
        {
            var rst = item.Validate();
            if (!string.IsNullOrEmpty(rst))
            {
                sb.Append(rst);
            }
        }
        this.ErrorMessage = sb.ToString();
    }

}

