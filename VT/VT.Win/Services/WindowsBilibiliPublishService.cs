using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PublishToBilibili.Interfaces;
using PublishToBilibili.Models;
using PublishToBilibili.Services;
using VT.Module.Services;

namespace VT.Win.Services
{
    #region Windows Implementation
    /// <summary>
    /// Windows-specific implementation of Bilibili publish service
    /// Windows平台特定的B站发布服务实现
    /// </summary>
    public class WindowsBilibiliPublishService : IBilibiliPublishService
    {
        #region Fields
        private readonly IProcessService _processService;
        private readonly IWindowService _windowService;
        private readonly string _bcutPath = @"C:\Users\Administrator\AppData\Local\BcutBilibili\BCUT.exe";
        #endregion

        #region Constructor
        public WindowsBilibiliPublishService()
        {
            _processService = new ProcessService();
            _windowService = new WindowService();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Publishes a video to Bilibili platform
        /// 发布视频到B站
        /// </summary>
        /// <param name="publishInfo">Publish information containing video details and settings</param>
        /// <returns>True if publish was successful, false otherwise</returns>
        public async Task<bool> PublishVideoAsync(BilibiliPublishInfo publishInfo)
        {
            try
            {
                // Convert to PublishInfo format used by the original implementation
                var publishInfoInternal = ConvertToInternalPublishInfo(publishInfo);

                // Initialize publish API
                var publishApi = new BilibiliPublishApi(_processService, _windowService);

                // Execute publish in background thread
                return await Task.Run(() => publishApi.PublishVideo(publishInfoInternal));
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to add proper logging)
                Console.WriteLine($"Error publishing to Bilibili: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if the service is available and properly configured
        /// 检查服务是否可用且配置正确
        /// </summary>
        /// <returns>True if service is available, false otherwise</returns>
        public bool IsServiceAvailable()
        {
            try
            {
                // Check if BCUT path exists
                if (!File.Exists(_bcutPath))
                {
                    return false;
                }

                // Check if required services are available
                return _processService != null && _windowService != null;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Converts BilibiliPublishInfo to internal PublishInfo format
        /// 将BilibiliPublishInfo转换为内部PublishInfo格式
        /// </summary>
        /// <param name="publishInfo">External publish info</param>
        /// <returns>Internal publish info</returns>
        private PublishInfo ConvertToInternalPublishInfo(BilibiliPublishInfo publishInfo)
        {
            return new PublishInfo
            {
                VideoFilePath = publishInfo.VideoFilePath,
                Title = publishInfo.Title,
                Type = publishInfo.Type,
                Tags = publishInfo.Tags,
                Description = publishInfo.Description,
                IsRepost = publishInfo.IsRepost,
                SourceAddress = publishInfo.SourceAddress,
                EnableOriginalWatermark = publishInfo.EnableOriginalWatermark,
                EnableNoRepost = publishInfo.EnableNoRepost
            };
        }
        #endregion
    }
    #endregion
}