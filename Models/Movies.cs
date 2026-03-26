using System;

namespace XtreamIPTV.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Poster { get; set; } = string.Empty;
        public string? Backdrop { get; internal set; } = null;
        public string Title { get; set; } = string.Empty;
        public string Plot { get; set; } = string.Empty;
        public string ReleaseInfo { get; set; } = string.Empty;
        public string CastInfo { get; set; } = string.Empty;
        public string DirectorInfo { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public double Rating { get; set; } = 0.0;
        public int CategoryId { get; set; }
        public long Added { get; set; } = 0;
        public string StreamUrl { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
    }
}