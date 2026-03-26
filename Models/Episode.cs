namespace XtreamIPTV.Models
{
    public class Episode
    {
        public string Title { get; set; } = "";
        public int EpisodeNumber { get; set; }
        public string StreamUrl { get; set; } = "";
        public string Plot { get; set; } = "";
        public string EpisodeId { get; set; } = "";
        public string? Poster { get; set; } = null;
    }
}