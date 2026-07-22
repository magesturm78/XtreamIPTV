using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Policy;

namespace XtreamIPTV.Models
{
    public class Series: ICloneable
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Plot { get; set; } = "";
        public string OriginalLanguage { get; set; } = "";
        public List<string> Languages { get; set; } = [];
        public List<int> Similiar { get; set; } = [];
        public double Rating { get; set; } = 0.0;
        public int CategoryId { get; set; }
        public string? Poster { get; set; } = null;
        public string NavigaionUrl { get; set; } = string.Empty;
        public string? Backdrop { get; internal set; } = null;
        public string Genre { get; internal set; } = string.Empty;
        public DateTime ReleaseDate { get; internal set; } = DateTime.MinValue;
        public string Age { get; set; } = string.Empty;

        public long LastModified { get; set; }
        public string ReleaseInfo => $"{Rating} * {ReleaseDate:yyyy-MM-dd} * Seasons: {Seasons.Count} * {Genre}";
        public List<LinkItem> Directors { get; set; } = [];
        public List<LinkItem> Actors { get; set; } = [];

        public ObservableCollection<Season> Seasons { get; set; } = new();

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
