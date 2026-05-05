using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using XtreamIPTV.Models;
using XtreamIPTV.Services;
using static XtreamIPTV.ViewModels.MoviesViewModel;

namespace XtreamIPTV.ViewModels
{
    public class SeriesViewModel : INotifyPropertyChanged
    {
        const int ROW_SIZE = 10;
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly IIPTVService _xtream;
        private readonly FavoritesService _favorites;
        private readonly SeriesEpisodeService _seriesEpisode;

        public ObservableCollection<Series> AllSeries { get; set; } = new();

        public ObservableCollection<Series> FilteredSeries { get; set; } = new();

        public ObservableCollection<Series> SearchedSeries { get; set; } = new();

        public ObservableCollection<Category> Categories { get; set; } = new();

        private int _count = 0;
        public int FilteredSeriesCount
        {
            get { return _count; }
            set 
            {                 
                _count = value;
                PropertyChanged?.Invoke(this, new(nameof(FilteredSeriesCount)));
            }
        }

        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                ApplyFilters();
                PropertyChanged?.Invoke(this, new(nameof(SelectedCategory)));
                PropertyChanged?.Invoke(this, new(nameof(FilteredSeries)));
            }
        }
        private Category? _selectedCategory;

        private Series? _selectedSeries;
        public Series? SelectedSeries
        {
            get => _selectedSeries;
            set
            {
                SelectedEpisode = null;
                SelectedSeason = null;
                _selectedSeries = value;
                _ = LoadSeasonsAsync();
                PropertyChanged?.Invoke(this, new(nameof(SelectedSeries)));
            }
        }

        private Season? _selectedSeason;
        public Season? SelectedSeason
        {
            get => _selectedSeason;
            set
            {
                _selectedSeason = value;
                PropertyChanged?.Invoke(this, new(nameof(SelectedSeason)));
            }
        }

        public Episode? SelectedEpisode
        {
            get => SelectedSeason?.SelectedEpisode;
            set
            {
                SelectedSeason = SelectedSeries?.Seasons.FirstOrDefault(s => s.Episodes.Any(e => e.EpisodeId == value?.EpisodeId));
                SelectedSeason?.SelectedEpisode = value;
                PropertyChanged?.Invoke(this, new(nameof(SelectedEpisode)));
            }
        }

        private Sort _sort = Sort.Default;
        public Sort Sort
        {
            get
            {
                return _sort;
            }
            set
            {
                _sort = value;
                ApplyFilters();
                PropertyChanged?.Invoke(this, new(nameof(Sort)));
                PropertyChanged?.Invoke(this, new(nameof(FilteredSeries)));
            }
        }

        private int _decadeFilter = 0;
        public int DecadeFilter
        {
            get
            {
                return _decadeFilter;
            }
            set
            {
                _decadeFilter = value;
                ApplyFilters();
                PropertyChanged?.Invoke(this, new(nameof(DecadeFilter)));
                PropertyChanged?.Invoke(this, new(nameof(FilteredSeries)));
            }
        }

        private string _languageFilter = string.Empty;
        public string LanguageFilter
        {
            get
            {
                return _languageFilter;
            }
            set
            {
                _languageFilter = value;
                ApplyFilters();
                PropertyChanged?.Invoke(this, new(nameof(LanguageFilter)));
                PropertyChanged?.Invoke(this, new(nameof(FilteredSeries)));
            }
        }

        public SeriesViewModel(IIPTVService xtream, FavoritesService favorites, SeriesEpisodeService seriesEpisode)
        {
            _xtream = xtream;
            _favorites = favorites;
            _seriesEpisode = seriesEpisode;
        }

        private IEnumerable<Series> GetFilteredSeries()
        {
            var seriesProgress = _seriesEpisode.GetSeriesIds();
            var filter = (_selectedCategory?.Id) switch
            {
                //All
                -1 => AllSeries,
                //Favorite
                -2 => AllSeries.Where(m => _favorites.IsFavorite($"series-{m.Id}")),
                //-3 => AllSeries.Where(m => _seriesEpisode.GetLastWatched(m.Id) > 0),
                -3 => AllSeries.Where(m => seriesProgress.Contains(m.Id)),
                _ => _selectedCategory == null ? AllSeries : AllSeries.Where(m => m.CategoryId == _selectedCategory.Id),
            };
            switch (_sort)
            {
                case Sort.Date:
                    filter = filter.OrderByDescending(m => m.LastModified);
                    break;
                case Sort.Name:
                    filter = filter.OrderBy(m => m.Title);
                    break;
                case Sort.Popularity:
                    filter = filter.OrderByDescending(m => m.Rating);
                    break;
                default:
                    if (_selectedCategory?.Id == -3)
                    {
                        filter = filter.OrderByDescending(m => seriesProgress.Contains(m.Id) ? seriesProgress.IndexOf(m.Id) : int.MaxValue);
                    }
                    break;
            }
            if (_decadeFilter > 0)
            {
                int startYear = _decadeFilter;
                int endYear = _decadeFilter + 9;
                filter = filter.Where(m => m.ReleaseDate.Year >= startYear && m.ReleaseDate.Year <= endYear);
            }
            if (string.IsNullOrEmpty(_languageFilter) == false)
            {
                filter = filter.Where(m => m.OriginalLanguage.Equals(_languageFilter, StringComparison.OrdinalIgnoreCase));
            }
            FilteredSeriesCount = filter.Count();
            return filter;
        }

        public async Task LoadSeriesAsync()
        {
            if (AllSeries.Count > 0) return;
            AllSeries.Clear();
            var list = await _xtream.GetSeriesAsync();
            foreach (var s in list)
                AllSeries.Add(s);
            ApplyFilters();
        }

        public void ScrollSeries()
        {
            int count = ROW_SIZE * 3;

            foreach (var m in GetFilteredSeries().Skip(FilteredSeries.Count).Take(count))
            {
                FilteredSeries.Add(m);
            }
        }

        public async Task LoadCategoriesAsync()
        {
            if (Categories.Count > 0) return;
            Categories.Clear();
            var list = await _xtream.GetSeriesCategoriesAsync();
            Categories.Add(new Category { Id = -1, Name = "All" });
            Categories.Add(new Category { Id = -2, Name = "Favorites" });
            Categories.Add(new Category { Id = -3, Name = "History" });
            foreach (var mc in list)
                Categories.Add(mc);
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == -2);//Default to Favorites
        }

        public void ApplyFilters()
        {
            FilteredSeries.Clear();
            foreach (var m in GetFilteredSeries().Take(ROW_SIZE * 5))
            {
                FilteredSeries.Add(m);
            }
            SelectedSeries = FilteredSeries.FirstOrDefault();
        }

        private async Task LoadSeasonsAsync()
        {
            if (SelectedSeries == null) return;
            var series = AllSeries.FirstOrDefault(s => s.Id == SelectedSeries.Id);
            if (series == null) return;
            if (series.Seasons.Count == 0)
            {
                series.Seasons.Clear();
                var seasons = await _xtream.GetSeasonsAsync(series);
                foreach (var s in seasons)
                    series.Seasons.Add(s);
            }
            int lastEpisodeId = _seriesEpisode.GetLastWatched(series.Id);
            if (lastEpisodeId > 0) {
                var allEpisodes = series.Seasons.SelectMany(s => s.Episodes).ToList();
                var lastWatchedEpisode = allEpisodes.FirstOrDefault(n => n.EpisodeId == lastEpisodeId);

                var nextEpisode = allEpisodes.SkipWhile(x => x != lastWatchedEpisode)
                                        .Skip(1)
                                        .DefaultIfEmpty(allEpisodes[0]) // Wraps back to the first item if at the end
                                        .FirstOrDefault();
                
                SelectedSeason = series.Seasons.FirstOrDefault(s => s.SeasonNumber == nextEpisode?.SeasonId);
                SelectedEpisode = nextEpisode;
            }
            else
            {
                SelectedSeason = series.Seasons.OrderBy(s => s.SeasonNumber).FirstOrDefault();
                SelectedEpisode = SelectedSeason?.Episodes.OrderBy(s => s.EpisodeNumber).FirstOrDefault();
            }
            PropertyChanged?.Invoke(this, new(nameof(SelectedSeries)));
            PropertyChanged?.Invoke(this, new(nameof(SelectedSeason)));
            PropertyChanged?.Invoke(this, new(nameof(SelectedEpisode)));
        }

        public void ToggleFavorite()
        {
            if (SelectedSeries != null)
                _favorites.ToggleFavorite($"series-{SelectedSeries.Id}");
        }

        public bool IsFavorite(Series m) =>
                _favorites.IsFavorite($"series-{m.Id}");

        internal string GetSeriesTitle(int seriesId)
        {
            return AllSeries.FirstOrDefault(s => s.Id == seriesId)?.Title ?? "Unknown Series";
        }

        internal async Task Search(string text)
        {
            await LoadSeriesAsync();
            SearchedSeries = new ObservableCollection<Series>(AllSeries.Where(m => m.Title.Contains(text, StringComparison.OrdinalIgnoreCase)).OrderByDescending(m => m.LastModified).Take(2500));
            PropertyChanged?.Invoke(this, new(nameof(SearchedSeries)));
        }

        internal async void GetSimiliarSeries()
        {
            if (SelectedSeries == null) return;
            var series = AllSeries.FirstOrDefault(s => s.Id == SelectedSeries.Id);
            if (series == null) return;

            FilteredSeries.Clear();
            PropertyChanged?.Invoke(this, new(nameof(FilteredSeries)));
            FilteredSeries = new ObservableCollection<Series>(await _xtream.GetSimiliarSeries(series, AllSeries));
            PropertyChanged?.Invoke(this, new(nameof(FilteredSeries)));
            SelectedSeries = FilteredSeries.FirstOrDefault();
            FilteredSeriesCount = FilteredSeries.Count();
        }

    }
}