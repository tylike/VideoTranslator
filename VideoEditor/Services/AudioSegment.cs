namespace VideoEditor.Services
{
    public class AudioSegment
    {
        public decimal Start { get; set; }
        public decimal End { get; set; }
        public decimal Duration => End - Start;
        public decimal SplitSilenceDuration { get; set; }

        public override string ToString()
        {
            return $"{Start:F2},{End:F2},{Duration:F2}";
        }
    }
}
