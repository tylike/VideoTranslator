using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using VideoTranslator.Interfaces;
using VideoTranslator.Services;
using VT.Module.BusinessObjects;



namespace VT.Module;

public interface IServices
{
    IServiceProvider ServiceProvider { get; set; }
    public IFileDialogService FileDialogService => ServiceProvider.GetService<IFileDialogService>();
    public IYouTubeService YouTubeService => ServiceProvider.GetService<IYouTubeService>();
    public IYouTubeInputDialogService YouTubeInputDialogService => ServiceProvider.GetService<IYouTubeInputDialogService>();
    public IProgressService ProgressService => ServiceProvider.GetService<IProgressService>();
    public ISpeechRecognitionService SpeechRecognitionService => ServiceProvider.GetRequiredService<ISpeechRecognitionService>();
    public IFFmpegService FfmpegService => ServiceProvider.GetRequiredService<IFFmpegService>();
    public ISubtitleService SubtitleService => ServiceProvider.GetRequiredService<ISubtitleService>();
    public IAudioService AudioService => ServiceProvider.GetRequiredService<IAudioService>();
    public IAudioSeparationService AudioSeparationService => ServiceProvider.GetRequiredService<IAudioSeparationService>();
    public ITranslationService TranslationService => ServiceProvider.GetRequiredService<ITranslationService>();
    public LMStudioTranslationService LmStudioTranslationService => ServiceProvider.GetRequiredService<LMStudioTranslationService>();
    public ITTSService ttsService => ServiceProvider.GetRequiredService<ITTSService>();
    public ITTSServiceManager TtsServiceManager => ServiceProvider.GetRequiredService<ITTSServiceManager>();
    public IProgressService progress => ServiceProvider.GetRequiredService<IProgressService>();
    //public ISegmentSourceSRTService SegmentSourceSRTService => ServiceProvider.GetRequiredService<ISegmentSourceSRTService>();

    public IObjectSpace ObjectSpace => throw new NotImplementedException();
    public VideoProject GetCurrentVideoProject();
}
