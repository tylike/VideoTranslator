using System.IO;
using VideoTranslator.Interfaces;
using System.Windows.Forms;

namespace VideoTranslator.Services;

public class WindowsFileDialogService : IFileDialogService
{
    public string? OpenVideoFile(string? defaultPath = null)
    {
        using var openFileDialog = new OpenFileDialog
        {
            Filter = "视频文件|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm;*.m4v|所有文件|*.*",
            Title = "选择视频文件",
            RestoreDirectory = true
        };

        if (!string.IsNullOrEmpty(defaultPath) && Directory.Exists(defaultPath))
        {
            openFileDialog.InitialDirectory = defaultPath;
        }

        return openFileDialog.ShowDialog() == DialogResult.OK ? openFileDialog.FileName : null;
    }

    public string? OpenFile(string filter, string? defaultPath = null)
    {
        using var openFileDialog = new OpenFileDialog
        {
            Filter = filter,
            Title = "选择文件",
            RestoreDirectory = true
        };

        if (!string.IsNullOrEmpty(defaultPath) && Directory.Exists(defaultPath))
        {
            openFileDialog.InitialDirectory = defaultPath;
        }

        return openFileDialog.ShowDialog() == DialogResult.OK ? openFileDialog.FileName : null;
    }

    public string? SaveFile(string filter, string? defaultPath = null, string? defaultFileName = null)
    {
        using var saveFileDialog = new SaveFileDialog
        {
            Filter = filter,
            Title = "保存文件",
            RestoreDirectory = true,
            FileName = defaultFileName
        };

        if (!string.IsNullOrEmpty(defaultPath) && Directory.Exists(defaultPath))
        {
            saveFileDialog.InitialDirectory = defaultPath;
        }

        return saveFileDialog.ShowDialog() == DialogResult.OK ? saveFileDialog.FileName : null;
    }
}
