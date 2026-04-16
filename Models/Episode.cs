namespace XtreamIPTV.Models
{
    public class Episode
    {
        public string Title { get; set; } = "";
        public int EpisodeNumber { get; set; }
        public string StreamUrl { get; set; } = "";
        public string DirectSource { get; set; } = "";
        public string Plot { get; set; } = "";
        public int EpisodeId { get; set; }
        public int SeasonId { get; set; }
        public int SeriesId { get; set; }
        public string? Poster { get; set; } = null;
    }
}