using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;

namespace VT.Win.Services;

public class WindowsYouTubeInputDialogService : IYouTubeInputDialogService
{
    public Task<YouTubeDownloadSelection?> ShowUrlInputDialogAsync()
    {
        return Task.Run(() =>
        {
            var dialog = new YouTubeUrlInputDialog();
            var result = dialog.ShowDialog();
            return result == DialogResult.OK ? dialog.GetSelection() : null;
        });
    }
}
