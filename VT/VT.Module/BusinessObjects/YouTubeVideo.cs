using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using System;
using System.Collections.Generic;

namespace VT.Module.BusinessObjects;

public enum YouTubeDownloadStatus
{
    NotDownloaded,
    Downloading,
    Completed,
    Failed
}

public class YouTubeVideo(Session s) : VTBaseObject(s)
{
    [XafDisplayName("视频项目")]
    [Association]
    public VideoProject VideoProject
    {
        get { return GetPropertyValue<VideoProject>(nameof(VideoProject)); }
        set { SetPropertyValue(nameof(VideoProject), value); }
    }

    [XafDisplayName("视频ID")]
    public string VideoId
    {
        get { return GetPropertyValue<string>(nameof(VideoId)); }
        set { SetPropertyValue(nameof(VideoId), value); }
    }

    [XafDisplayName("下载地址")]
    [Size(-1)]
    public string DownloadUrl
    {
        get { return GetPropertyValue<string>(nameof(DownloadUrl)); }
        set { SetPropertyValue(nameof(DownloadUrl), value); }
    }

    [XafDisplayName("标题")]
    [Size(500)]
    public string Title
    {
        get { return GetPropertyValue<string>(nameof(Title)); }
        set { SetPropertyValue(nameof(Title), value); }
    }

    [XafDisplayName("描述")]
    [Size(-1)]
    [ModelDefault("RowCount", "5")]
    public string Description
    {
        get { return GetPropertyValue<string>(nameof(Description)); }
        set { SetPropertyValue(nameof(Description), value); }
    }

    [XafDisplayName("缩略图URL")]
    [Size(-1)]
    public string ThumbnailUrl
    {
        get { return GetPropertyValue<string>(nameof(ThumbnailUrl)); }
        set { SetPropertyValue(nameof(ThumbnailUrl), value); }
    }

    [XafDisplayName("上传者")]
    [Size(200)]
    public string Uploader
    {
        get { return GetPropertyValue<string>(nameof(Uploader)); }
        set { SetPropertyValue(nameof(Uploader), value); }
    }

    [XafDisplayName("上传日期")]
    public DateTime? UploadDate
    {
        get { return GetPropertyValue<DateTime?>(nameof(UploadDate)); }
        set { SetPropertyValue(nameof(UploadDate), value); }
    }

    [XafDisplayName("时长(秒)")]
    public int Duration
    {
        get { return GetPropertyValue<int>(nameof(Duration)); }
        set { SetPropertyValue(nameof(Duration), value); }
    }

    [XafDisplayName("时长文本")]
    [Size(50)]
    public string DurationText
    {
        get { return GetPropertyValue<string>(nameof(DurationText)); }
        set { SetPropertyValue(nameof(DurationText), value); }
    }

    [XafDisplayName("视图数")]
    public long ViewCount
    {
        get { return GetPropertyValue<long>(nameof(ViewCount)); }
        set { SetPropertyValue(nameof(ViewCount), value); }
    }

    [XafDisplayName("点赞数")]
    public long LikeCount
    {
        get { return GetPropertyValue<long>(nameof(LikeCount)); }
        set { SetPropertyValue(nameof(LikeCount), value); }
    }

    [XafDisplayName("评论数")]
    public long CommentCount
    {
        get { return GetPropertyValue<long>(nameof(CommentCount)); }
        set { SetPropertyValue(nameof(CommentCount), value); }
    }

    [XafDisplayName("标签")]
    [Size(-1)]
    public string Tags
    {
        get { return GetPropertyValue<string>(nameof(Tags)); }
        set { SetPropertyValue(nameof(Tags), value); }
    }

    [XafDisplayName("分类")]
    [Size(100)]
    public string Category
    {
        get { return GetPropertyValue<string>(nameof(Category)); }
        set { SetPropertyValue(nameof(Category), value); }
    }

    [XafDisplayName("年龄限制")]
    public int AgeLimit
    {
        get { return GetPropertyValue<int>(nameof(AgeLimit)); }
        set { SetPropertyValue(nameof(AgeLimit), value); }
    }

    [XafDisplayName("是否直播")]
    public bool IsLive
    {
        get { return GetPropertyValue<bool>(nameof(IsLive)); }
        set { SetPropertyValue(nameof(IsLive), value); }
    }

    [XafDisplayName("是否4K")]
    public bool Is4K
    {
        get { return GetPropertyValue<bool>(nameof(Is4K)); }
        set { SetPropertyValue(nameof(Is4K), value); }
    }

    [XafDisplayName("分辨率")]
    [Size(50)]
    public string Resolution
    {
        get { return GetPropertyValue<string>(nameof(Resolution)); }
        set { SetPropertyValue(nameof(Resolution), value); }
    }

    [XafDisplayName("格式")]
    [Size(50)]
    public string Format
    {
        get { return GetPropertyValue<string>(nameof(Format)); }
        set { SetPropertyValue(nameof(Format), value); }
    }

    [XafDisplayName("文件大小")]
    public long FileSize
    {
        get { return GetPropertyValue<long>(nameof(FileSize)); }
        set { SetPropertyValue(nameof(FileSize), value); }
    }

    [XafDisplayName("文件大小文本")]
    [Size(50)]
    public string FileSizeText
    {
        get { return GetPropertyValue<string>(nameof(FileSizeText)); }
        set { SetPropertyValue(nameof(FileSizeText), value); }
    }

    [XafDisplayName("下载状态")]
    public YouTubeDownloadStatus DownloadStatus
    {
        get { return GetPropertyValue<YouTubeDownloadStatus>(nameof(DownloadStatus)); }
        set { SetPropertyValue(nameof(DownloadStatus), value); }
    }

    [XafDisplayName("本地文件路径")]
    [Size(-1)]
    public string LocalFilePath
    {
        get { return GetPropertyValue<string>(nameof(LocalFilePath)); }
        set { SetPropertyValue(nameof(LocalFilePath), value); }
    }

    [XafDisplayName("下载日期")]
    public DateTime? DownloadDate
    {
        get { return GetPropertyValue<DateTime?>(nameof(DownloadDate)); }
        set { SetPropertyValue(nameof(DownloadDate), value); }
    }

    [XafDisplayName("错误信息")]
    [Size(-1)]
    [ModelDefault("RowCount", "3")]
    public string ErrorMessage
    {
        get { return GetPropertyValue<string>(nameof(ErrorMessage)); }
        set { SetPropertyValue(nameof(ErrorMessage), value); }
    }

    [XafDisplayName("备注")]
    [Size(-1)]
    [ModelDefault("RowCount", "3")]
    public string Notes
    {
        get { return GetPropertyValue<string>(nameof(Notes)); }
        set { SetPropertyValue(nameof(Notes), value); }
    }

    public string GetFormattedDuration()
    {
        if (Duration <= 0) return "00:00";
        var hours = Duration / 3600;
        var minutes = (Duration % 3600) / 60;
        var seconds = Duration % 60;
        return hours > 0 
            ? $"{hours:D2}:{minutes:D2}:{seconds:D2}" 
            : $"{minutes:D2}:{seconds:D2}";
    }

    public string GetFormattedFileSize()
    {
        if (FileSize <= 0) return "0 B";
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        var order = 0;
        var size = FileSize;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    public void MarkAsDownloading()
    {
        DownloadStatus = YouTubeDownloadStatus.Downloading;
        ErrorMessage = null;
    }

    public void MarkAsCompleted(string localPath)
    {
        DownloadStatus = YouTubeDownloadStatus.Completed;
        LocalFilePath = localPath;
        DownloadDate = DateTime.Now;
        ErrorMessage = null;
    }

    public void MarkAsFailed(string error)
    {
        DownloadStatus = YouTubeDownloadStatus.Failed;
        ErrorMessage = error;
    }
}
