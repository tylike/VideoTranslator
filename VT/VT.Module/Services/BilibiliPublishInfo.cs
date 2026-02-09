using System;
using System.Collections.Generic;

namespace VT.Module.Services
{
    #region Publish Information Model
    /// <summary>
    /// Model containing information needed to publish a video to Bilibili
    /// 发布视频到B站所需的信息模型
    /// </summary>
    public class BilibiliPublishInfo
    {
        /// <summary>
        /// Path to the video file to publish
        /// 要发布的视频文件路径
        /// </summary>
        public string VideoFilePath { get; set; } = "";

        /// <summary>
        /// Title of the video
        /// 视频标题
        /// </summary>
        public string Title { get; set; } = "";

        /// <summary>
        /// Type of video ("自制" or "转载")
        /// 视频类型（"自制" 或 "转载"）
        /// </summary>
        public string Type { get; set; } = "自制";

        /// <summary>
        /// List of tags for the video
        /// 视频标签列表
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Description of the video
        /// 视频描述
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Whether this is a reposted video
        /// 是否为转载视频
        /// </summary>
        public bool IsRepost { get; set; } = false;

        /// <summary>
        /// Source address for reposted videos
        /// 转载视频的源地址
        /// </summary>
        public string SourceAddress { get; set; } = "";

        /// <summary>
        /// Whether to enable original watermark
        /// 是否启用原创水印
        /// </summary>
        public bool EnableOriginalWatermark { get; set; } = false;

        /// <summary>
        /// Whether to enable no-repost setting
        /// 是否启用禁止转载设置
        /// </summary>
        public bool EnableNoRepost { get; set; } = false;
    }
    #endregion
}