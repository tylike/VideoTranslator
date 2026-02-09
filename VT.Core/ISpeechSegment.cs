using static System.Net.Mime.MediaTypeNames;

namespace VT.Core;

public interface ISpeechSegment
{
    int Index { get; set; }

    TimeSpan StartTime { get; set; }
    TimeSpan EndTime { get; set; }
    
    ISpeechSegment? Next { get; set; }
    ISpeechSegment? Previous { get; set; }

}

public static class ISpeechSegmentExt
{
    extension(ISpeechSegment s)
    {
        public TimeSpan Duration => s.EndTime - s.StartTime;
        public double StartSeconds { get => s.StartTime.TotalSeconds; set => s.StartTime = TimeSpan.FromSeconds(value); }
        public double EndSeconds { get => s.EndTime.TotalSeconds; set => s.EndTime = TimeSpan.FromSeconds(value); }

        public double StartMS { get => s.StartTime.TotalMilliseconds; set => s.StartTime = TimeSpan.FromMilliseconds(value); }
        public double EndMS { get => s.EndTime.TotalMilliseconds; set => s.EndTime = TimeSpan.FromMilliseconds(value); }

        public double DurationMS { get => s.Duration.TotalMilliseconds; }
        public double DurationSeconds { get => s.Duration.TotalSeconds; }

        public double GetGapToNext()
        {
            if (s.Next == null)
            {
                return 0;
            }

            return s.Next.StartMS - s.EndMS;
        }

        public double GetGapFromPrevious()
        {
            if (s.Previous == null)
            {
                return 0;
            }

            return s.StartMS - s.Previous.EndMS;
        }
    }
}