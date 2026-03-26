using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace XtreamIPTV.Models
{
    public class Season
    {
        public int SeasonNumber { get; set; }
        public ObservableCollection<Episode> Episodes { get; set; } = new();

        public string SeasonId => $"Season {SeasonNumber}";
    }
}
