using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TimeLine.Services;

public interface IAudioPlayerService : IDisposable
{
    Task PlayAsync(string filePath);
    void Stop();
    bool IsPlaying { get; }
    event EventHandler? PlaybackStarted;
    event EventHandler? PlaybackEnded;
}

public class AudioPlayerService : IAudioPlayerService
{
    private MediaPlayer? _mediaPlayer;
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;

    public event EventHandler? PlaybackStarted;
    public event EventHandler? PlaybackEnded;

    public AudioPlayerService()
    {
        _mediaPlayer = new MediaPlayer();
        _mediaPlayer.MediaEnded += OnMediaEnded;
        _mediaPlayer.MediaFailed += OnMediaFailed;
    }

    public async Task PlayAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("音频文件不存在", filePath);

        Stop();

        _mediaPlayer!.Open(new Uri(filePath));
        _mediaPlayer!.Play();
        _isPlaying = true;
        PlaybackStarted?.Invoke(this, EventArgs.Empty);

        await Task.CompletedTask;
    }

    public void Stop()
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Close();
            _isPlaying = false;
        }
    }

    private void OnMediaEnded(object? sender, EventArgs e)
    {
        _mediaPlayer?.Close();
        _isPlaying = false;
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }

    private void OnMediaFailed(object? sender, ExceptionEventArgs e)
    {
        _mediaPlayer?.Close();
        _isPlaying = false;
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        Stop();
        if (_mediaPlayer != null)
        {
            _mediaPlayer.MediaEnded -= OnMediaEnded;
            _mediaPlayer.MediaFailed -= OnMediaFailed;
            _mediaPlayer.Close();
            _mediaPlayer = null;
        }
    }
}
