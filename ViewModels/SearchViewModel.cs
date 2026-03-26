using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using XtreamIPTV.Services;

namespace XtreamIPTV.ViewModels
{
    public class SearchViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public SeriesViewModel SeriesVM { get; set; }
        public MoviesViewModel MoviesVM { get; set; }

        public SearchViewModel(MoviesViewModel moviesVM, SeriesViewModel seriesVM)
        {
            SeriesVM = seriesVM;
            MoviesVM = moviesVM;
        }

        private string _searchText = "";
        public string SearchText
        {
            get
            {
                return _searchText;
            }
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    PropertyChanged?.Invoke(this, new(nameof(SearchText)));
                }
            }
        }

    }
}
