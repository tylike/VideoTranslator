using System.Collections.Generic;

namespace VideoTranslator.SRT.Models
{
    public class TtsOptimizationConfig
    {
        #region 时长限制

        public double MinDurationMs { get; set; } = 500;

        public double MaxDurationMs { get; set; } = 15000;

        public double OptimalDurationMs { get; set; } = 5000;

        #endregion

        #region 文本过滤

        public List<string> ExcludePatterns { get; set; } = new List<string>
        {
            "(upbeat music)",
            "(music)",
            "(laughing)",
            "(applause)",
            "[music]",
            "[laughter]",
            "[applause]"
        };

        public int MinTextLength { get; set; } = 2;

        #endregion

        #region 智能合并

        public bool EnableSmartMerge { get; set; } = true;

        public double MergeThresholdMs { get; set; } = 1000;

        #endregion

        #region 智能拆分

        public bool EnableSmartSplit { get; set; } = true;

        public double SplitThresholdMs { get; set; } = 15000;

        #endregion

        #region 异常检测

        public bool EnableAnomalyDetection { get; set; } = true;

        public double MaxTokenDurationMs { get; set; } = 2000;

        public double MinConfidence { get; set; } = 0.5;

        public int MaxRepetitionCount { get; set; } = 3;

        #endregion

        #region 构造函数

        public TtsOptimizationConfig()
        {
        }

        public TtsOptimizationConfig(double minDurationMs, double maxDurationMs, double optimalDurationMs)
        {
            MinDurationMs = minDurationMs;
            MaxDurationMs = maxDurationMs;
            OptimalDurationMs = optimalDurationMs;
        }

        #endregion
    }
}
