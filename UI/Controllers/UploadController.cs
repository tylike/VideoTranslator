using Microsoft.AspNetCore.Mvc;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;
using System.IO;

namespace VideoTranslator.UI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IProjectManagerService _projectManagerService;
    private readonly ILogger<UploadController> _logger;

    public UploadController(IProjectManagerService projectManagerService, ILogger<UploadController> logger)
    {
        _projectManagerService = projectManagerService;
        _logger = logger;
    }

    [HttpPost("video")]
    public async Task<IActionResult> UploadVideo([FromForm] IFormFile file, [FromForm] string projectId)
    {
        _logger.LogInformation($"收到上传请求 - File: {(file != null ? file.FileName : "null")}, ProjectId: '{projectId}'");
        
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "请选择文件" });
            }

            var allowedExtensions = new[] { ".mp4", ".avi", ".mov", ".mkv", ".wmv" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { success = false, message = "不支持的文件格式" });
            }

            var maxSize = 2L * 1024 * 1024 * 1024;
            if (file.Length > maxSize)
            {
                return BadRequest(new { success = false, message = "文件大小超过限制（最大 2GB）" });
            }

            var project = await _projectManagerService.GetProjectAsync(projectId);
            if (project == null)
            {
                return NotFound(new { success = false, message = "项目不存在" });
            }

            var sourceDir = Path.Combine(project.BaseDirectory, "source");
            Directory.CreateDirectory(sourceDir);

            var filePath = Path.Combine(sourceDir, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            project.SourceVideoPath = filePath;
            project.Status = VideoStatus.Processing;
            project.UpdatedAt = DateTime.Now;
            
            var existingResource = project.GetResourceByFilePath(filePath);
            if (existingResource == null)
            {
                var resource = new ProjectResource
                {
                    FileName = file.FileName,
                    FilePath = filePath,
                    Type = ResourceType.Video,
                    FileSize = file.Length,
                    IsUsed = false,
                    CreatedAt = DateTime.Now
                };
                project.AddResource(resource);
                _logger.LogInformation($"已添加资源到项目: {resource.FileName}");
            }
            
            await _projectManagerService.UpdateProjectAsync(project);

            _logger.LogInformation($"视频文件上传成功: {filePath}");

            return Ok(new 
            { 
                success = true, 
                message = "视频上传成功", 
                filePath = filePath,
                fileName = file.FileName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传视频文件失败");
            return StatusCode(500, new { success = false, message = $"上传失败: {ex.Message}" });
        }
    }

    [HttpPost("audio")]
    public async Task<IActionResult> UploadAudio([FromForm] IFormFile file, [FromForm] string projectId)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "请选择文件" });
            }

            var allowedExtensions = new[] { ".wav", ".mp3", ".m4a", ".aac", ".ogg" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { success = false, message = "不支持的音频格式" });
            }

            var maxSize = 500L * 1024 * 1024;
            if (file.Length > maxSize)
            {
                return BadRequest(new { success = false, message = "文件大小超过限制（最大 500MB）" });
            }

            var project = await _projectManagerService.GetProjectAsync(projectId);
            if (project == null)
            {
                return NotFound(new { success = false, message = "项目不存在" });
            }

            var sourceDir = Path.Combine(project.BaseDirectory, "source");
            Directory.CreateDirectory(sourceDir);

            var filePath = Path.Combine(sourceDir, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            project.SourceAudioPath = filePath;
            project.UpdatedAt = DateTime.Now;
            
            var existingResource = project.GetResourceByFilePath(filePath);
            if (existingResource == null)
            {
                var resource = new ProjectResource
                {
                    FileName = file.FileName,
                    FilePath = filePath,
                    Type = ResourceType.Audio,
                    FileSize = file.Length,
                    IsUsed = false,
                    CreatedAt = DateTime.Now
                };
                project.AddResource(resource);
                _logger.LogInformation($"已添加音频资源到项目: {resource.FileName}");
            }
            
            await _projectManagerService.UpdateProjectAsync(project);

            _logger.LogInformation($"音频文件上传成功: {filePath}");

            return Ok(new 
            { 
                success = true, 
                message = "音频上传成功", 
                filePath = filePath,
                fileName = file.FileName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传音频文件失败");
            return StatusCode(500, new { success = false, message = $"上传失败: {ex.Message}" });
        }
    }

    [HttpDelete("resource/{projectId}/{resourceId}")]
    public async Task<IActionResult> DeleteResource(string projectId, string resourceId)
    {
        try
        {
            var project = await _projectManagerService.GetProjectAsync(projectId);
            if (project == null)
            {
                return NotFound(new { success = false, message = "项目不存在" });
            }

            var resource = project.GetResource(resourceId);
            if (resource == null)
            {
                return NotFound(new { success = false, message = "资源不存在" });
            }

            if (resource.IsUsed)
            {
                return BadRequest(new { success = false, message = "资源正在使用中，无法删除" });
            }

            if (System.IO.File.Exists(resource.FilePath))
            {
                System.IO.File.Delete(resource.FilePath);
                _logger.LogInformation($"已删除资源文件: {resource.FilePath}");
            }

            project.RemoveResource(resourceId);
            await _projectManagerService.UpdateProjectAsync(project);

            _logger.LogInformation($"资源已从项目删除: {resource.FileName}");

            return Ok(new { success = true, message = "资源删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除资源失败");
            return StatusCode(500, new { success = false, message = $"删除失败: {ex.Message}" });
        }
    }

    [HttpPost("subtitle")]
    public async Task<IActionResult> UploadSubtitle([FromForm] IFormFile file, [FromForm] string projectId)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "请选择文件" });
            }

            var allowedExtensions = new[] { ".srt", ".ass", ".vtt" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { success = false, message = "不支持的字幕格式" });
            }

            var maxSize = 10L * 1024 * 1024;
            if (file.Length > maxSize)
            {
                return BadRequest(new { success = false, message = "文件大小超过限制（最大 10MB）" });
            }

            var project = await _projectManagerService.GetProjectAsync(projectId);
            if (project == null)
            {
                return NotFound(new { success = false, message = "项目不存在" });
            }

            var sourceDir = Path.Combine(project.BaseDirectory, "source");
            Directory.CreateDirectory(sourceDir);

            var filePath = Path.Combine(sourceDir, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            project.SourceSubtitlePath = filePath;
            project.UpdatedAt = DateTime.Now;
            
            var existingResource = project.GetResourceByFilePath(filePath);
            if (existingResource == null)
            {
                var resource = new ProjectResource
                {
                    FileName = file.FileName,
                    FilePath = filePath,
                    Type = ResourceType.Subtitle,
                    FileSize = file.Length,
                    IsUsed = false,
                    CreatedAt = DateTime.Now
                };
                project.AddResource(resource);
                _logger.LogInformation($"已添加字幕资源到项目: {resource.FileName}");
            }
            
            await _projectManagerService.UpdateProjectAsync(project);

            _logger.LogInformation($"字幕文件上传成功: {filePath}");

            return Ok(new 
            { 
                success = true, 
                message = "字幕上传成功", 
                filePath = filePath,
                fileName = file.FileName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传字幕文件失败");
            return StatusCode(500, new { success = false, message = $"上传失败: {ex.Message}" });
        }
    }
}
