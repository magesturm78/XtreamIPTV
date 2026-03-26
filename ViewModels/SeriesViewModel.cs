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

        public ObservableCollection<Series> AllSeries { get; set; } = new();

        public ObservableCollection<Series> FilteredSeries { get; set; } = new();

        public ObservableCollection<Category> Categories { get; set; } = new();

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

        private Episode? _selectedEpisode;
        public Episode? SelectedEpisode
        {
            get => _selectedEpisode;
            set
            {
                _selectedEpisode = value;
                PropertyChanged?.Invoke(this, new(nameof(SelectedEpisode)));
            }
        }

        private Sort _sort = Sort.None;
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

        public SeriesViewModel(IIPTVService xtream, FavoritesService favorites)
        {
            _xtream = xtream;
            _favorites = favorites;
        }

        private List<Series> GetFilteredSeries()
        {
            List<Series> filter = [];
            switch (_selectedCategory?.Id)
            {
                case -1: //All
                    filter = [.. AllSeries];
                    break;
                case -2://Favorite
                    filter = [.. AllSeries.Where(m => _favorites.IsFavorite($"series-{m.Id}")).ToList()];
                    break;
                default:
                    filter = _selectedCategory == null ? [.. AllSeries] : AllSeries.Where(m => m.CategoryId == _selectedCategory.Id).ToList();
                    break;
            }

            switch (_sort)
            {
                case Sort.ReleaseDate:
                    filter = [.. filter.OrderByDescending(m => m.LastModified)];
                    break;
                case Sort.Rating:
                    filter = [.. filter.OrderByDescending(m => m.Rating)];
                    break;
                default:
                    break;
            }
            return filter;

        }

        public async Task LoadSeriesAsync()
        {
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
            foreach (var mc in list)
                Categories.Add(mc);
            SelectedCategory = Categories.FirstOrDefault();
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

            if (SelectedSeries.Seasons.Count > 0) return;

            SelectedSeries.Seasons.Clear();
            var seasons = await _xtream.GetSeasonsAsync(SelectedSeries);
            foreach (var s in seasons)
                SelectedSeries.Seasons.Add(s);

            PropertyChanged?.Invoke(this, new(nameof(SelectedSeries)));
        }

        public void ToggleFavorite()
        {
            if (SelectedSeries != null)
                _favorites.ToggleFavorite($"series-{SelectedSeries.Id}");
        }

        public bool IsFavorite(Series m) =>
                _favorites.IsFavorite($"series-{m.Id}");
    }
}