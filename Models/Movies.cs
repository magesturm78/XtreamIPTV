using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace XtreamIPTV.Models
{
    public class LinkItem
    {
        public string Text { get; set; }
        public string Url { get; set; }
    }

    public class Movie: ICloneable
    {
        public int Id { get; set; }
        public string? Poster { get; set; } = string.Empty;
        public string? Backdrop { get; internal set; } = null;
        public string Title { get; set; } = string.Empty;
        public string Plot { get; set; } = string.Empty;
        public string ReleaseInfo { get; set; } = string.Empty;
        public List<LinkItem> Directors { get; set; } = [];
        public List<LinkItem> Actors { get; set; } = [];
        public string OriginalLanguage { get; set; } = string.Empty;
        public string NavigaionUrl { get; set; } = string.Empty;
        public List<string> Languages { get; set; } = [];
        public List<int> Similiar { get; set; } = [];
        public double Rating { get; set; } = 0.0;
        public int CategoryId { get; set; }
        public long Added { get; set; } = 0;
        public string StreamUrl { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public string Age { get; set; } = string.Empty;
        public bool RetrievedDetails { get; set; } = false;

        public string FileName 
        { 
            get
            {
                char[] invalidChars = Path.GetInvalidFileNameChars();

                string safeName = invalidChars.Aggregate(Title, (current, c) => current.Replace(c, '_'));

                // Optional: Trim trailing periods and spaces, which are invalid on Windows
                safeName = safeName.TrimEnd('.', ' ');
                return $"{Id}.{safeName}";
            }
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}