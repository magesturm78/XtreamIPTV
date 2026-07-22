using System;
using System.Collections.Generic;
using System.Text;

namespace XtreamIPTV.Models
{
    public class Live
    {
        public int Num { get; set; }
        public int StreamId { get; set; }
        public string StreamUrl { get; set; } = string.Empty;
        public string? StreamIcon { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
    }
}
