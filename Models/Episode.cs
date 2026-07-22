namespace XtreamIPTV.Models
{
    public class Episode
    {
        public string Title { get; set; } = "";
        public int EpisodeNumber { get; set; }
        public string StreamUrl { get; set; } = "";
<<<<<<< HEAD
        public string DirectSource { get; set; } = "";
        public string Plot { get; set; } = "";
        public string ReleaseDate { get; set; } = "";
        public int EpisodeId { get; set; }
        public int SeasonId { get; set; }
        public int SeriesId { get; set; }
=======
        public string Plot { get; set; } = "";
        public string EpisodeId { get; set; } = "";
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
        public string? Poster { get; set; } = null;
    }
}