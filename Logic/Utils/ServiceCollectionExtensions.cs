﻿﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VideoTranslator.Config;
using VideoTranslator.Interfaces;
using VideoTranslator.Services;
using VideoTranslator.SRT.Core.Interfaces;
using VideoTranslator.SRT.Core.Services;

namespace VideoTranslator.Utils;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVideoTranslatorServices(
        this IServiceCollection services,
        bool registerProgressService = true)
    {
        Console.WriteLine($"[ServiceCollectionExtensions] 开始读取配置...");
                
        services.AddLogging();

        if (registerProgressService)
        {
            services.AddSingleton<IProgressService, SimpleProgressService>();
        }

        #region 核心服务 - 自动依赖解析

        services.AddSingleton<ISrtReader, SrtReader>();
        services.AddSingleton<ISrtWriter, SrtWriter>();
        services.AddSingleton<ISubtitleService, SubtitleService>();

        #endregion

        #region 音频与视频服务

        services.AddSingleton<IFFmpegService, FFmpegService>();
        services.AddSingleton<IYouTubeService, YouTubeService>();
        services.AddSingleton<IAudioService, AudioService>();

        #endregion

        #region 翻译服务

        services.AddSingleton<ITranslationService, TranslationService>();
        services.AddSingleton<LMStudioTranslationService>();

        #endregion

        #region 语音识别服务

        services.AddSingleton<ISpeechRecognitionService, WhisperRecognitionServiceSimple>();
        services.AddSingleton<ISpeechRecognitionServiceVad, WhisperRecognitionServiceVad>();
        services.AddSingleton<VoskRecognitionService>();
        services.AddSingleton<WhisperRecognitionService>();
        services.AddSingleton<PurfviewFasterWhisperRecognitionService>();
        services.AddSingleton<SherpaSpeakerDiarizationService>();

        #endregion

        #region TTS服务

        services.AddSingleton<ITTSService, TTSService>();
        services.AddSingleton<ITTSServiceManager, TTSServiceManager>();

        #endregion

        #region 音频分离服务

        services.AddSingleton<IAudioSeparationService, AudioSeparationService>();

        #endregion

        #region VAD工作流服务

        services.AddSingleton<VideoVadWorkflowService>();

        #endregion

        return services;
    }
}
