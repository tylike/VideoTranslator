using System;
using NAudio.Wave;

namespace VT.Win.Forms;

public class AudioPlayer
{
    #region Fields

    private WaveOutEvent? currentWaveOut;
    private AudioFileReader? currentAudioFile;
    private bool isPlaying;

    #endregion

    #region Events

    public event EventHandler? PlaybackStateChanged;

    #endregion

    #region Properties

    public bool IsPlaying => isPlaying;

    #endregion

    #region Public Methods

    public void Play(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
        {
            return;
        }

        Stop();

        try
        {
            currentAudioFile = new AudioFileReader(filePath);
            currentWaveOut = new WaveOutEvent();
            currentWaveOut.Init(currentAudioFile);
            currentWaveOut.Play();

            isPlaying = true;

            currentWaveOut.PlaybackStopped += (s, a) =>
            {
                isPlaying = false;
                OnPlaybackStateChanged();
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Play failed: {ex.Message}");
            isPlaying = false;
        }

        OnPlaybackStateChanged();
    }

    public void Stop()
    {
        if (currentWaveOut != null)
        {
            currentWaveOut.Stop();
            currentWaveOut.Dispose();
            currentWaveOut = null;
        }
        if (currentAudioFile != null)
        {
            currentAudioFile.Dispose();
            currentAudioFile = null;
        }
        isPlaying = false;
        OnPlaybackStateChanged();
    }

    public void TogglePlay(string filePath)
    {
        if (isPlaying)
        {
            Stop();
        }
        else
        {
            Play(filePath);
        }
    }

    #endregion

    #region Private Methods

    private void OnPlaybackStateChanged()
    {
        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
