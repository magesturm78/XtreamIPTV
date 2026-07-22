using System.Collections.Generic;
using System.Collections.ObjectModel;
<<<<<<< HEAD
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

=======

namespace XtreamIPTV.Models
{
    public class Season
    {
        public int SeasonNumber { get; set; }
        public ObservableCollection<Episode> Episodes { get; set; } = new();

>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
        public string SeasonId => $"Season {SeasonNumber}";
    }
}
