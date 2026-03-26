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
        None,
        ReleaseDate,
        Rating
    }
    public class MoviesViewModel : INotifyPropertyChanged
    {
        const int ROW_SIZE = 10;
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly IIPTVService _xtream;
        private readonly FavoritesService _favorites;

        public ObservableCollection<Category> Categories { get; set; } = new();
        public ObservableCollection<Movie> AllMovies { get; set; } = new();
        public ObservableCollection<Movie> FilteredMovies { get; set; } = new();
        public int MovieCount { get; set; } = ROW_SIZE * 5;

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

        private Sort _sort = Sort.None;
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

        private Movie? _selectedMovie;

        public MoviesViewModel(IIPTVService xtream, FavoritesService favorites)
        {
            _xtream = xtream;
            _favorites = favorites;
        }

        public async Task LoadMoviesAsync()
        {
            if (AllMovies.Count > 0) return;
            AllMovies.Clear();
            var list = await _xtream.GetMoviesAsync();
            foreach (var m in list)
                AllMovies.Add(m);

            ApplyFilters();
        }

        public async Task LoadMovieCategoriesAsync()
        {
            if (Categories.Count > 0) return;
            Categories.Clear();
            var list = await _xtream.GetMovieCategoriesAsync();
            Categories.Add(new Category { Id = -1, Name = "All" });
            Categories.Add(new Category { Id = -2, Name = "Favorites" });
            foreach (var mc in list)
                Categories.Add(mc);
            SelectedCategory = Categories.FirstOrDefault();
        }

        public void ApplyFilters()
        {
            FilteredMovies.Clear();
            MovieCount = ROW_SIZE * 5;
            foreach (var m in GetFilteredMovies().Take(MovieCount))
            {
                FilteredMovies.Add(m);
            }
            SelectedMovie = FilteredMovies.FirstOrDefault();
        }

        private List<Movie> GetFilteredMovies()
        {
            List<Movie> filter = [];
            switch (_selectedCategory?.Id)
            {
                case -1: //All
                    filter = [.. AllMovies];
                    break;
                case -2://Favorite
                    filter = [.. AllMovies.Where(m => _favorites.IsFavorite($"movie-{m.Id}")).ToList()];
                    break;
                default:
                    filter = _selectedCategory == null ? [.. AllMovies] : AllMovies.Where(m => m.CategoryId == _selectedCategory.Id).ToList();
                    break;
            }

            switch(_sort) 
            { 
                case Sort.ReleaseDate:
                    filter = [.. filter.OrderByDescending(m => m.Added)];
                    break;
                case Sort.Rating:
                    filter = [.. filter.OrderByDescending(m => m.Rating)];
                    break;
                default:
                    break;
            }
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
            if (!string.IsNullOrEmpty(_selectedMovie.Backdrop)) return;

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
    }
}