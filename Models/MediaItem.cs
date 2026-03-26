namespace XtreamIPTV.Models
{
    public class MediaItem
    {
        public string Title { get; set; } = "";
        public string Plot { get; set; } = "";
        public string Language { get; set; } = "";
        public string Rating { get; set; } = "";
        public int Decade { get; set; }
        public bool IsSeries { get; set; }
        public string StreamUrl { get; set; } = "";
    }
}