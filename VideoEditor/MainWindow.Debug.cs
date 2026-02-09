using System.IO;
using System.Text;

namespace VideoEditor;

public partial class MainWindow
{
    #region 保存项目调试信息

    private void SaveProjectDebugInfo()
    {
        try
        {
            _logger.Information("开始保存项目调试信息");

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var logFilePath = Path.Combine(@"d:\VideoTranslator\logs", $"project_save_{timestamp}.txt");

            var debugInfo = new StringBuilder();

            #region 基本信息

            debugInfo.AppendLine("========================================");
            debugInfo.AppendLine("项目保存调试信息");
            debugInfo.AppendLine("========================================");
            debugInfo.AppendLine($"保存时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            debugInfo.AppendLine($"时间戳: {timestamp}");
            debugInfo.AppendLine();

            #endregion

            #region 项目信息

            debugInfo.AppendLine("----------------------------------------");
            debugInfo.AppendLine("项目基本信息");
            debugInfo.AppendLine("----------------------------------------");

            if (_currentProject != null)
            {
                debugInfo.AppendLine($"项目名称: {_currentProject.ProjectName}");
                debugInfo.AppendLine($"项目ID (Oid): {_currentProject.Oid}");
                debugInfo.AppendLine($"项目路径: {_currentProject.ProjectPath ?? "N/A"}");
                debugInfo.AppendLine($"源视频路径: {_currentProject.SourceVideoPath ?? "N/A"}");
                debugInfo.AppendLine($"源字幕路径: {_currentProject.SourceSubtitlePath ?? "N/A"}");
                debugInfo.AppendLine($"创建时间: {_currentProject.CreateTime:yyyy-MM-dd HH:mm:ss}");
                debugInfo.AppendLine($"修改时间: {_currentProject.LastUpdateTime:yyyy-MM-dd HH:mm:ss}");
            }
            else
            {
                debugInfo.AppendLine("当前项目: 无");
            }

            debugInfo.AppendLine();

            #endregion

            #region 轨道信息

            debugInfo.AppendLine("----------------------------------------");
            debugInfo.AppendLine("轨道信息");
            debugInfo.AppendLine("----------------------------------------");

            if (_currentProject != null && _currentProject.Tracks != null)
            {
                var tracks = _currentProject.Tracks.ToList();
                debugInfo.AppendLine($"轨道总数: {tracks.Count}");
                debugInfo.AppendLine();

                for (int i = 0; i < tracks.Count; i++)
                {
                    var track = tracks[i];
                    debugInfo.AppendLine($"轨道 #{i + 1}:");
                    debugInfo.AppendLine($"  - 标题: {track.Title}");
                    debugInfo.AppendLine($"  - 类型: {track.TrackType}");
                    debugInfo.AppendLine($"  - ID (Oid): {track.Oid}");
                    debugInfo.AppendLine($"  - 高度: {track.Height:F2}px");
                    debugInfo.AppendLine($"  - 颜色: {track.Color}");
                    debugInfo.AppendLine($"  - 索引: {track.Index}");

                    if (track.Segments != null)
                    {
                        var segments = track.Segments.ToList();
                        debugInfo.AppendLine($"  - 片段数量: {segments.Count}");

                        for (int j = 0; j < segments.Count; j++)
                        {
                            var segment = segments[j];
                            debugInfo.AppendLine($"    片段 #{j + 1}:");
                            debugInfo.AppendLine($"      - ID (Oid): {segment.Oid}");
                            debugInfo.AppendLine($"      - 索引: {segment.Index}");
                            debugInfo.AppendLine($"      - 开始时间: {segment.Start.TotalSeconds:F3}s");
                            debugInfo.AppendLine($"      - 结束时间: {segment.End.TotalSeconds:F3}s");
                            debugInfo.AppendLine($"      - 时长: {segment.Duration:F3}s");
                            debugInfo.AppendLine($"      - 类型: {segment.Type}");

                            if (!string.IsNullOrEmpty(segment.Text))
                            {
                                var text = segment.Text.Length > 50 ? segment.Text.Substring(0, 50) + "..." : segment.Text;
                                debugInfo.AppendLine($"      - 文本: {text}");
                            }

                            debugInfo.AppendLine($"      - 背景颜色: {segment.BackgroundColor}");
                        }
                    }

                    debugInfo.AppendLine();
                }
            }
            else
            {
                debugInfo.AppendLine("无轨道信息");
            }

            debugInfo.AppendLine();

            #endregion

            #region 时间线信息

            debugInfo.AppendLine("----------------------------------------");
            debugInfo.AppendLine("时间线信息");
            debugInfo.AppendLine("----------------------------------------");


            debugInfo.AppendLine();

            #endregion

            #region 视频播放器信息

            debugInfo.AppendLine("----------------------------------------");
            debugInfo.AppendLine("视频播放器信息");
            debugInfo.AppendLine("----------------------------------------");

            if (videoPlayerControl != null)
            {
                debugInfo.AppendLine("视频播放器: 已初始化");
            }
            else
            {
                debugInfo.AppendLine("视频播放器: 未初始化");
            }

            debugInfo.AppendLine();

            #endregion

            #region 状态日志

            debugInfo.AppendLine("----------------------------------------");
            debugInfo.AppendLine("状态日志 (最近100条)");
            debugInfo.AppendLine("----------------------------------------");

            var statusLog = (_progressService as Services.VideoEditorProgressService)?.StatusLog ?? "";
            var logLines = statusLog.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var recentLogs = logLines.TakeLast(100).ToArray();
            debugInfo.AppendLine(string.Join(Environment.NewLine, recentLogs));

            debugInfo.AppendLine();

            #endregion

            #region 系统信息

            debugInfo.AppendLine("----------------------------------------");
            debugInfo.AppendLine("系统信息");
            debugInfo.AppendLine("----------------------------------------");
            debugInfo.AppendLine($"操作系统: {Environment.OSVersion}");
            debugInfo.AppendLine($"机器名称: {Environment.MachineName}");
            debugInfo.AppendLine($"用户名: {Environment.UserName}");
            debugInfo.AppendLine($"处理器数量: {Environment.ProcessorCount}");
            debugInfo.AppendLine($"工作集内存: {Environment.WorkingSet / 1024 / 1024:F2} MB");

            debugInfo.AppendLine();

            #endregion

            debugInfo.AppendLine("========================================");
            debugInfo.AppendLine("调试信息结束");
            debugInfo.AppendLine("========================================");

            #region 写入文件

            Directory.CreateDirectory(@"d:\VideoTranslator\logs");
            File.WriteAllText(logFilePath, debugInfo.ToString(), Encoding.UTF8);

            _logger.Information("项目调试信息已保存到: {FilePath}", logFilePath);

            #endregion
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "保存项目调试信息失败");
        }
    }

    #endregion
}