using System;
using System.Collections.Generic;
using System.Linq;
using VideoTranslator.Models;

namespace VideoTranslator.Models;

/// <summary>
/// AudioClipWithTime扩展方法
/// </summary>
public static class AudioClipWithTimeExtensions
{
    #region 验证方法

    /// <summary>
    /// 验证音频片段列表中是否存在重叠
    /// </summary>
    /// <param name="segments">音频片段列表</param>
    /// <exception cref="InvalidOperationException">当存在重叠片段时抛出异常</exception>
    public static void ValidateNoOverlaps(this List<AudioClipWithTime> sortedSegments)
    {
        if (sortedSegments == null || sortedSegments.Count == 0)
        {
            return;
        }
        
        for (int i = 0; i < sortedSegments.Count - 1; i++)
        {
            var current = sortedSegments[i];
            var next = sortedSegments[i + 1];
            if (current.End > next.Start)
            {
                throw new InvalidOperationException(
                    $"片段重叠: 片段{current.Index}结束于{current.End:g}，片段{next.Index}开始于{next.Start:g}");
            }
        }
    }

    #endregion
}
