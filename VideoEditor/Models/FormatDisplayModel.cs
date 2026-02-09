using YoutubeDLSharp.Metadata;

namespace VideoEditor.Models;

public class FormatDisplayModel
{
    private readonly FormatData _format;

    public FormatDisplayModel(FormatData format)
    {
        _format = format ?? throw new ArgumentNullException(nameof(format));
    }

    public string FormatId => _format.FormatId ?? string.Empty;

    public string Resolution => _format.Resolution ?? string.Empty;

    public string Fps => _format.FrameRate?.ToString("F0") ?? string.Empty;

    public string VCodec => _format.VideoCodec ?? string.Empty;

    public string ACodec => _format.AudioCodec ?? string.Empty;

    public string FileSize => GetFormattedFileSize();

    public string FormatNote => _format.FormatNote ?? string.Empty;

    public string Ext => _format.Extension ?? string.Empty;

    public string Abr => _format.AudioBitrate?.ToString("F2") ?? string.Empty;

    public bool RequiresPremium => CheckRequiresPremium();

    public string Width => _format.Width?.ToString() ?? string.Empty;

    public string Height => _format.Height?.ToString() ?? string.Empty;

    public string DynamicRange => _format.DynamicRange ?? string.Empty;

    public string Vbr => _format.VideoBitrate?.ToString("F2") ?? string.Empty;

    public string Tbr => _format.Bitrate?.ToString("F2") ?? string.Empty;

    public string Container => _format.ContainerFormat ?? string.Empty;

    public string Protocol => _format.Protocol ?? string.Empty;

    public string Language => _format.Language ?? string.Empty;

    public string AudioSamplingRate => _format.AudioSamplingRate?.ToString("F0") ?? string.Empty;

    public string AudioChannels => _format.AudioChannels?.ToString() ?? string.Empty;

    public string HasDrm => CheckHasDrm();

    public string FormatString => _format.Format ?? string.Empty;

    private string GetFormattedFileSize()
    {
        var bytes = _format.FileSize ?? _format.ApproximateFileSize ?? 0;
        return FormatFileSize(bytes);
    }

    private string FormatFileSize(long bytes)
    {
        if (bytes == 0)
            return "0 B";

        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    private bool CheckRequiresPremium()
    {
        if (!string.IsNullOrEmpty(_format.FormatNote) && _format.FormatNote.Contains("premium", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(_format.Resolution))
        {
            if (_format.Resolution.Contains("4K", StringComparison.OrdinalIgnoreCase) ||
                _format.Resolution.Contains("2160p", StringComparison.OrdinalIgnoreCase) ||
                _format.Resolution.Contains("1440p", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private string CheckHasDrm()
    {
        if (_format.HasDRM == YoutubeDLSharp.Metadata.MaybeBool.True)
        {
            return "是";
        }
        else if (_format.HasDRM == YoutubeDLSharp.Metadata.MaybeBool.False)
        {
            return "否";
        }
        else
        {
            return string.Empty;
        }
    }
}
