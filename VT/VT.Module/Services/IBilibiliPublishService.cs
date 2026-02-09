using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VT.Module.Services
{
    #region Interface Definition
    /// <summary>
    /// Service interface for publishing videos to Bilibili platform
    /// 用于发布视频到B站的服务接口
    /// </summary>
    public interface IBilibiliPublishService
    {
        /// <summary>
        /// Publishes a video to Bilibili platform
        /// 发布视频到B站
        /// </summary>
        /// <param name="publishInfo">Publish information containing video details and settings</param>
        /// <returns>True if publish was successful, false otherwise</returns>
        Task<bool> PublishVideoAsync(BilibiliPublishInfo publishInfo);

        /// <summary>
        /// Checks if the service is available and properly configured
        /// 检查服务是否可用且配置正确
        /// </summary>
        /// <returns>True if service is available, false otherwise</returns>
        bool IsServiceAvailable();
    }
    #endregion
}