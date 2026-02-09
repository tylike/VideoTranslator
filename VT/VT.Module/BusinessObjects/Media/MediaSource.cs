using DevExpress.ExpressApp;
using DevExpress.Xpo;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using TrackMenuAttributes;

namespace VT.Module.BusinessObjects;

public abstract class MediaSource(Session s) : VTBaseObject(s)
{
    [XafDisplayName("视频项目")]
    [Association]
    public VideoProject VideoProject
    {
        get { return GetPropertyValue<VideoProject>(nameof(VideoProject)); }
        set { SetPropertyValue(nameof(VideoProject), value); }
    }

    [XafDisplayName("文件路径")]
    [Size(-1)]
    public string FileFullName
    {
        get { return GetPropertyValue<string>(nameof(FileFullName)); }
        set { SetPropertyValue(nameof(FileFullName), value); }
    }


    public string Name
    {
        get { return field; }
        set { SetPropertyValue("Name", ref field, value); }
    }


    [XafDisplayName("媒体类型")]
    public MediaType MediaType
    {
        get { return GetPropertyValue<MediaType>(nameof(MediaType)); }
        set { SetPropertyValue(nameof(MediaType), value); }
    }

    #region 上下文菜单操作

    [ContextMenuAction("打开文件所在位置", Order = 10, Group = "文件")]
    public void OpenFileLocation()
    {
        try
        {
            if (string.IsNullOrEmpty(FileFullName) || !System.IO.File.Exists(FileFullName))
            {
                throw new Exception("文件不存在!");                
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{FileFullName}\"",
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            throw;
        }
    }
       

    #endregion

}
