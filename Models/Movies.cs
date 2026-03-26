using System;
using System.Collections.Generic;

namespace XtreamIPTV.Models
{
    public class Movie: ICloneable
    {
        public int Id { get; set; }
        public string Poster { get; set; } = string.Empty;
        public string? Backdrop { get; internal set; } = null;
        public string Title { get; set; } = string.Empty;
        public string Plot { get; set; } = string.Empty;
        public string ReleaseInfo { get; set; } = string.Empty;
        public string CastInfo { get; set; } = string.Empty;
        public string DirectorInfo { get; set; } = string.Empty;
        public string OriginalLanguage { get; set; } = string.Empty;
        public List<string> Languages { get; set; } = [];
        public List<int> Similiar { get; set; } = [];
        public double Rating { get; set; } = 0.0;
        public int CategoryId { get; set; }
        public long Added { get; set; } = 0;
        public string StreamUrl { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}