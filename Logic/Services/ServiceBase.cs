using Microsoft.Extensions.DependencyInjection;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.Models;

namespace VideoTranslator.Services;

public class ServiceBase
{
    public static IServiceProvider ServiceProvider { get; set; }
    protected IProgressService progress { get=>ServiceProvider.GetRequiredService<IProgressService>(); }
    protected PathConfig Settings { get => ConfigurationService.Configuration.VideoTranslator.Paths; }
    public IFFmpegService Ffmpeg => ServiceProvider.GetRequiredService<IFFmpegService>();
    public ConfigurationRoot Config => ConfigurationService.Configuration;
    public ServiceBase()
    {        
    }
}
