using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security.Permissions;
using System.Threading.Tasks;
using XtreamIPTV.Models;
using XtreamIPTV.Services;

namespace XtreamIPTV.ViewModels
{
    public enum Sort
    {
        Default,
        Name,
        Date,
        Popularity
    }
    public class MoviesViewModel : INotifyPropertyChanged
    {
        const int ROW_SIZE = 10;
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly IIPTVService _xtream;
        private readonly FavoritesService _favorites;
        private readonly ContinueWatchingService _continue;

        public ObservableCollection<Category> Categories { get; set; } = new();
        public ObservableCollection<Movie> AllMovies { get; set; } = new();
        public ObservableCollection<Movie> FilteredMovies { get; set; } = new();
        public ObservableCollection<Movie> SearchedMovies { get; set; } = new();
        public int MovieCount { get; set; } = ROW_SIZE * 5;
        public double MovePlayTime 
        { 
            get
            {
                return _continue.GetProgress($"movie-{SelectedMovie?.Id}");
            } 
        }
        private int _count = 0;
        public int FilteredMoviesCount
        {
            get { return _count; }
            set
            {
                _count = value;
                PropertyChanged?.Invoke(this, new(nameof(FilteredMoviesCount)));
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
                PropertyChanged?.Invoke(this, new(nameof(FilteredMovies)));
            }
        }
        private Category? _selectedCategory;

        public Movie? SelectedMovie
        {
            get => _selectedMovie;
            set
            {
                _selectedMovie = value;
                PropertyChanged?.Invoke(this, new(nameof(SelectedMovie)));
            }
        }

        private Sort _sort = Sort.Default;
        public Sort Sort { 
            get 
            { 
                return _sort; 
            } 
            set { 
                _sort = value;
                ApplyFilters();
                PropertyChanged?.Invoke(this, new(nameof(Sort)));
                PropertyChanged?.Invoke(this, new(nameof(FilteredMovies)));
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
                PropertyChanged?.Invoke(this, new(nameof(FilteredMovies)));
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
                PropertyChanged?.Invoke(this, new(nameof(FilteredMovies)));
            }
        }

        private Movie? _selectedMovie;

        public MoviesViewModel(IIPTVService @xtream, FavoritesService @favorites, ContinueWatchingService @continue)
        {
            _xtream = @xtream;
            _favorites = @favorites;
            _continue = @continue;
        }

        public async Task LoadMoviesAsync()
        {
            if (AllMovies.Count > 0) return;
            AllMovies = new ObservableCollection<Movie>(await _xtream.GetMoviesAsync());
            //AllMovies = new ObservableCollection<Movie>(await _xtream.GetMoviesAsync(_selectedCategory.Id,_decadeFilter,LanguageFilter,));
            //AllMovies.Clear();
            //var list = await _xtream.GetMoviesAsync();
            //foreach (var m in list)
            //    AllMovies.Add(m);

            ApplyFilters();
        }

        public async Task LoadMovieCategoriesAsync()
        {
            if (Categories.Count > 0) return;
            Categories.Clear();
            var list = await _xtream.GetMovieCategoriesAsync();
            Categories.Add(new Category { Id = -1, Name = "All" });
            Categories.Add(new Category { Id = -2, Name = "Favorites" });
            Categories.Add(new Category { Id = -3, Name = "History" });
            foreach (var mc in list)
                Categories.Add(mc);
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == -2);//Default to Favorites
        }

        public void ApplyFilters()
        {
            FilteredMovies.Clear();
            MovieCount = ROW_SIZE * 10;
            foreach (var m in GetFilteredMovies().Take(MovieCount))
            {
                FilteredMovies.Add(m);
            }
            SelectedMovie = FilteredMovies.FirstOrDefault();
        }

        private IEnumerable<Movie> GetFilteredMovies()
        {
            var filter = (_selectedCategory?.Id) switch
            {
                -1 => AllMovies,
                -2 => AllMovies.Where(m => _favorites.IsFavorite($"movie-{m.Id}")),
                -3 => AllMovies.Where(m => _continue.GetProgress($"movie-{m.Id}") > 0.0),
                _ => _selectedCategory == null ? AllMovies : AllMovies.Where(m => m.CategoryId == _selectedCategory.Id),
            };

            switch(_sort) 
            { 
                case Sort.Date:
                    filter = filter.OrderByDescending(m => m.Added);
                    break;
                case Sort.Name:
                    filter = filter.OrderBy(m => m.Title);
                    break;
                case Sort.Popularity:
                    filter = filter.OrderByDescending(m => m.Rating);
                    break;
                default:
                    break;
            }
            if (_decadeFilter > 0)
            {
                int startYear = _decadeFilter;
                int endYear = _decadeFilter + 9;
                filter = filter.Where(m => m.ReleaseDate.Year >= startYear && m.ReleaseDate.Year <= endYear);
            }
            if (!string.IsNullOrEmpty(_languageFilter) && _languageFilter != "ALL")
            {
                filter = filter.Where(m => m.OriginalLanguage.Equals(_languageFilter, StringComparison.OrdinalIgnoreCase));
            }
            FilteredMoviesCount = filter.Count();
            return filter;
        }

        public void ScrollMovies()
        {
            int count = ROW_SIZE * 3;

            foreach (var m in GetFilteredMovies().Skip(FilteredMovies.Count).Take(count))
            {
                FilteredMovies.Add(m);
            }
            MovieCount = FilteredMovies.Count;
        }

        public async void GetSelectedMovieData()
        {
            if (_selectedMovie == null) return;
            //if (!string.IsNullOrEmpty(_selectedMovie.Backdrop)) return;
            if (!string.IsNullOrEmpty(_selectedMovie.ReleaseInfo) && !_selectedMovie.ReleaseInfo.EndsWith(" * ")) return;


            SelectedMovie = await _xtream.GetMovieDetailAsync(_selectedMovie);
            PropertyChanged?.Invoke(this, new(nameof(SelectedMovie)));
        }

        public void ToggleFavorite()
        {
            if (SelectedMovie != null)
                _favorites.ToggleFavorite($"movie-{SelectedMovie.Id}");
        }

        public bool IsFavorite(Movie m) =>
            _favorites.IsFavorite($"movie-{m.Id}");

        internal async Task Search(string text)
        {
            await LoadMoviesAsync();
            SearchedMovies = new ObservableCollection<Movie>(AllMovies.Where(m => m.Title.Contains(text, StringComparison.OrdinalIgnoreCase)).OrderByDescending(m => m.Added).Take(2500));
            PropertyChanged?.Invoke(this, new(nameof(SearchedMovies)));
        }

        internal async void GetSimiliarMovies()
        {
            if (SelectedMovie == null) return;

            var movie = (Movie)SelectedMovie?.Clone();
            FilteredMovies.Clear();
            PropertyChanged?.Invoke(this, new(nameof(FilteredMovies)));
            FilteredMovies = new ObservableCollection<Movie>(await _xtream.GetSimiliarMovies(movie, AllMovies));
            PropertyChanged?.Invoke(this, new(nameof(FilteredMovies)));
            SelectedMovie = FilteredMovies.FirstOrDefault();
            FilteredMoviesCount = FilteredMovies.Count();
        }
    }
}