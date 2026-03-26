using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace XtreamIPTV.Models
{
    public class Season: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public int SeasonNumber { get; set; }
        public ObservableCollection<Episode> Episodes { get; set; } = new();

        Episode? _selectedEpisode = null;


        public Episode? SelectedEpisode 
        {
            get => _selectedEpisode; 
            set
            {
                _selectedEpisode = value;
                PropertyChanged?.Invoke(this, new(nameof(SelectedEpisode)));
            }
        }

        public string SeasonId => $"Season {SeasonNumber}";
    }
}
