using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace XtreamIPTV.Models
{
    public class Series
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Plot { get; set; } = "";
        public string Language { get; set; } = "";
        public double Rating { get; set; } = 0.0;
        public int CategoryId { get; set; }
        public string? Poster { get; set; } = null;
        public string? Backdrop { get; internal set; } = null;
        public string Genre { get; internal set; } = string.Empty;
        public DateTime ReleaseDate { get; internal set; } = DateTime.MinValue;

        public long LastModified { get; set; }
        public string ReleaseInfo => $"{Rating} * {ReleaseDate:yyyy-MM-dd} * Seasons: {Seasons.Count} * {Genre}";
        public string CastInfo { get; set; } = string.Empty;
        public string DirectorInfo { get; set; } = string.Empty;

        public ObservableCollection<Season> Seasons { get; set; } = new();
    }
}
