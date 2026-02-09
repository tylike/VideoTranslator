using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PublishToBilibili.Interfaces;
using PublishToBilibili.Models;
using PublishToBilibili.Services;
using VT.Module.Services;
using Serilog;
using ILogger = Serilog.ILogger;

namespace VideoEditor.Services;

#region VideoEditor Implementation
/// <summary>
/// VideoEditor-specific implementation of Bilibili publish service
/// VideoEditor项目特定的B站发布服务实现
/// </summary>
public class VideoEditorBilibiliPublishService : IBilibiliPublishService
{
    #region Fields
    private readonly IProcessService _processService;
    private readonly IWindowService _windowService;
    private readonly string _bcutPath = @"C:\Users\Administrator\AppData\Local\BcutBilibili\BCUT.exe";
    private static readonly ILogger _logger = Log.ForContext<VideoEditorBilibiliPublishService>();
    #endregion

    #region Constructor
    public VideoEditorBilibiliPublishService(IProcessService processService, IWindowService windowService)
    {
        _processService = processService ?? throw new ArgumentNullException(nameof(processService));
        _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
        _logger.Information("VideoEditorBilibiliPublishService 已初始化");
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
            _logger.Information("开始发布视频到B站: {Title}", publishInfo.Title);

            var publishInfoInternal = ConvertToInternalPublishInfo(publishInfo);

            var publishApi = new BilibiliPublishApi(_processService, _windowService);

            _logger.Information("执行发布操作");
            var result = await Task.Run(() => publishApi.PublishVideo(publishInfoInternal));

            if (result)
            {
                _logger.Information("视频发布成功");
            }
            else
            {
                _logger.Warning("视频发布失败");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "发布视频到B站时发生错误");
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
            if (!File.Exists(_bcutPath))
            {
                _logger.Warning("BCUT路径不存在: {BcutPath}", _bcutPath);
                return false;
            }

            if (_processService == null || _windowService == null)
            {
                _logger.Warning("所需服务未正确初始化");
                return false;
            }

            _logger.Debug("服务可用性检查通过");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "检查服务可用性时发生错误");
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