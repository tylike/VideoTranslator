namespace PublishToBilibili.Models
{
    public class PublishInfo
    {
        public string VideoFilePath { get; set; } = "";
        public string Title { get; set; } = "";
        public string Type { get; set; } = "自制";
        public List<string> Tags { get; set; } = new List<string>();
        public string Description { get; set; } = "";
        public bool IsRepost { get; set; } = false;
        public string SourceAddress { get; set; } = "";
        public bool EnableOriginalWatermark { get; set; } = false;
        public bool EnableNoRepost { get; set; } = false;
    }
}
