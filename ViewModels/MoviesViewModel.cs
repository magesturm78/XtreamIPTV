<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Threading.Tasks;
using System.Windows.Automation.Provider;
=======
﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security.Permissions;
using System.Threading.Tasks;
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
using XtreamIPTV.Models;
using XtreamIPTV.Services;

namespace XtreamIPTV.ViewModels
{
    public enum Sort
    {
<<<<<<< HEAD
        Default,
        Name,
        Date,
        Popularity
    }
    public struct MovieHistoryItem
    {
        public Movie SelectedMovie;
        public ObservableCollection<Movie> FilteredMovies;
=======
        None,
        ReleaseDate,
        Rating
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
    }
    public class MoviesViewModel : INotifyPropertyChanged
    {
        const int ROW_SIZE = 10;
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly IIPTVService _xtream;
        private readonly FavoritesService _favorites;
<<<<<<< HEAD
        private readonly ContinueWatchingService _continue;
        private readonly Stack<MovieHistoryItem> _historyStack = new Stack<MovieHistoryItem>();
=======
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4

        public ObservableCollection<Category> Categories { get; set; } = new();
        public ObservableCollection<Movie> AllMovies { get; set; } = new();
        public ObservableCollection<Movie> FilteredMovies { get; set; } = new();
<<<<<<< HEAD
        public ObservableCollection<Movie> SearchedMovies { get; set; } = new();
        public ObservableCollection<String> AgeRatings { get; set; } = new();
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
=======
        public int MovieCount { get; set; } = ROW_SIZE * 5;
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4

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

<<<<<<< HEAD
        private Sort _sort = Sort.Default;
=======
        private Sort _sort = Sort.None;
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
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

<<<<<<< HEAD
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

        private string _ageRatingFilter = string.Empty;
        public string AgeRatingFilter
        {
            get
            {
                return _languageFilter;
            }
            set
            {
                _ageRatingFilter = value;
                ApplyFilters();
                PropertyChanged?.Invoke(this, new(nameof(AgeRatingFilter)));
                PropertyChanged?.Invoke(this, new(nameof(FilteredMovies)));
            }
        }

        private Movie? _selectedMovie;

        public MoviesViewModel(IIPTVService @xtream, FavoritesService @favorites, ContinueWatchingService @continue)
        {
            _xtream = @xtream;
            _favorites = @favorites;
            _continue = @continue;
=======
        private Movie? _selectedMovie;

        public MoviesViewModel(IIPTVService xtream, FavoritesService favorites)
        {
            _xtream = xtream;
            _favorites = favorites;
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
        }

        public async Task LoadMoviesAsync()
        {
            if (AllMovies.Count > 0) return;
<<<<<<< HEAD
            AllMovies = new ObservableCollection<Movie>(await _xtream.GetMoviesAsync());
            AgeRatings = new ObservableCollection<string>(["ALL", "R", "NC-17", "G", "NR", "18", "18+", "19","19+","X", "K-18", "N-18", "R18", "M/18", "20", "D", "C", "K18", "VM18", "18SX", "Adult"]);
            //AllMovies = new ObservableCollection<Movie>(await _xtream.GetMoviesAsync(_selectedCategory.Id,_decadeFilter,LanguageFilter,));
            //AllMovies.Clear();
            //var list = await _xtream.GetMoviesAsync();
            //foreach (var m in list)
            //    AllMovies.Add(m);
=======
            AllMovies.Clear();
            var list = await _xtream.GetMoviesAsync();
            foreach (var m in list)
                AllMovies.Add(m);
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4

            ApplyFilters();
        }

        public async Task LoadMovieCategoriesAsync()
        {
            if (Categories.Count > 0) return;
            Categories.Clear();
            var list = await _xtream.GetMovieCategoriesAsync();
            Categories.Add(new Category { Id = -1, Name = "All" });
            Categories.Add(new Category { Id = -2, Name = "Favorites" });
<<<<<<< HEAD
            Categories.Add(new Category { Id = -3, Name = "History" });
            foreach (var mc in list)
                Categories.Add(mc);
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == -2);//Default to Favorites
        }

        public async void ApplyFilters()
        {
            _historyStack.Clear();
            FilteredMovies.Clear();
            MovieCount = ROW_SIZE * 10;
=======
            foreach (var mc in list)
                Categories.Add(mc);
            SelectedCategory = Categories.FirstOrDefault();
        }

        public void ApplyFilters()
        {
            FilteredMovies.Clear();
            MovieCount = ROW_SIZE * 5;
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
            foreach (var m in GetFilteredMovies().Take(MovieCount))
            {
                FilteredMovies.Add(m);
            }
            SelectedMovie = FilteredMovies.FirstOrDefault();
        }

<<<<<<< HEAD
        private IEnumerable<Movie> GetFilteredMovies()
        {
            var movieProgress = _continue.GetMovieProgress();
            var filter = (_selectedCategory?.Id) switch
            {
                -1 => AllMovies,
                -2 => AllMovies.Where(m => _favorites.IsFavorite($"movie-{m.Id}")),
                //-3 => AllMovies.Where(m => _continue.GetProgress($"movie-{m.Id}") > 0),
                -3 => AllMovies.Where(m => movieProgress.Contains(m.Id.ToString())),
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
                    if (_selectedCategory?.Id == -3) // History
                        filter = filter.OrderByDescending(m => movieProgress.IndexOf(m.Id.ToString()));
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
            if (!string.IsNullOrEmpty(_ageRatingFilter) && _ageRatingFilter != "ALL")
            {
                filter = filter.Where(m => m.Age.Equals(_ageRatingFilter, StringComparison.OrdinalIgnoreCase));
            } 
            else
            {
                filter = filter.Where(m => !m.Age.Equals("Adult", StringComparison.OrdinalIgnoreCase));
            }
            FilteredMoviesCount = filter.Count();
            return filter;
        }

        public bool GoBackInHistory()
        {
            if (_historyStack.Count == 0) return false;
            var item = _historyStack.Pop();
            FilteredMovies = item.FilteredMovies;
            PropertyChanged?.Invoke(this, new(nameof(FilteredMovies)));
            SelectedMovie = item.SelectedMovie;
            return (_historyStack.Count != 0);
        }

=======
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

>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
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
<<<<<<< HEAD
            var movie = _selectedMovie;
            Directory.GetFiles("E:\\Movies", $"{movie.Id}.*.*").ToList().ForEach(f =>
            {
                if (!movie.Title.EndsWith("(Cached)"))
                {
                    movie.Title = movie.Title + " (Cached)";
                    PropertyChanged?.Invoke(this, new(nameof(SelectedMovie)));
                }
            });
            if (movie.RetrievedDetails) return;

            movie = await _xtream.GetMovieDetailAsync(movie);
            movie.RetrievedDetails = true;
            Directory.GetFiles("E:\\Movies", $"{movie.Id}.*.*").ToList().ForEach(f =>
            {
                if (!movie.Title.EndsWith("(Cached)"))
                    movie.Title = movie.Title + " (Cached)";
            });
            if (SelectedMovie?.Id == movie.Id)
                PropertyChanged?.Invoke(this, new(nameof(SelectedMovie)));
=======
            if (!string.IsNullOrEmpty(_selectedMovie.Backdrop)) return;

            SelectedMovie = await _xtream.GetMovieDetailAsync(_selectedMovie);
            PropertyChanged?.Invoke(this, new(nameof(SelectedMovie)));
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
        }

        public void ToggleFavorite()
        {
            if (SelectedMovie != null)
                _favorites.ToggleFavorite($"movie-{SelectedMovie.Id}");
        }

        public bool IsFavorite(Movie m) =>
            _favorites.IsFavorite($"movie-{m.Id}");
<<<<<<< HEAD

        internal async Task Search(string text)
        {
            await LoadMoviesAsync();
            SearchedMovies = new ObservableCollection<Movie>(AllMovies.Where(m => m.Title.Contains(text, StringComparison.OrdinalIgnoreCase)).OrderByDescending(m => m.Added).Take(2500));
            PropertyChanged?.Invoke(this, new(nameof(SearchedMovies)));
        }

        internal async void GetSimiliarMovies()
        {
            if (SelectedMovie == null) return;

            _historyStack.Push(new MovieHistoryItem { SelectedMovie = SelectedMovie, FilteredMovies = new ObservableCollection<Movie>(FilteredMovies) });

            var movie = (Movie)SelectedMovie?.Clone();
            FilteredMovies.Clear();
            PropertyChanged?.Invoke(this, new(nameof(FilteredMovies)));
            FilteredMovies = new ObservableCollection<Movie>(await _xtream.GetSimiliarMovies(movie, AllMovies));
            PropertyChanged?.Invoke(this, new(nameof(FilteredMovies)));
            SelectedMovie = FilteredMovies.FirstOrDefault();
            FilteredMoviesCount = FilteredMovies.Count();
        }

        internal void GetMoviesByDirector(string director)
        {
            if (string.IsNullOrEmpty(director)) return;

            _historyStack.Push(new MovieHistoryItem { SelectedMovie = SelectedMovie, FilteredMovies = new ObservableCollection<Movie>(FilteredMovies) });

            FilteredMovies = new ObservableCollection<Movie>(AllMovies.Where(x => x.Directors.Any(d => d.Text.Equals(director, StringComparison.OrdinalIgnoreCase))).OrderByDescending(m => m.ReleaseDate));
            PropertyChanged?.Invoke(this, new(nameof(FilteredMovies)));
            FilteredMoviesCount = FilteredMovies.Count();
        }

        internal void GetMoviesByActor(string actor)
        {
            if (string.IsNullOrEmpty(actor)) return;

            _historyStack.Push(new MovieHistoryItem { SelectedMovie = SelectedMovie, FilteredMovies = new ObservableCollection<Movie>(FilteredMovies) });

            FilteredMovies = new ObservableCollection<Movie>(AllMovies.Where(x => x.Actors.Any(a => a.Text.Equals(actor, StringComparison.OrdinalIgnoreCase))).OrderByDescending(m => m.ReleaseDate));
            PropertyChanged?.Invoke(this, new(nameof(FilteredMovies)));
            FilteredMoviesCount = FilteredMovies.Count();
        }

        internal async void GetMovieDetails(int movieId)
        {
            var movie = AllMovies.FirstOrDefault(m => m.Id == movieId);
            if (movie?.RetrievedDetails == true) return;
            movie ??= new Movie() { Id= movieId };
            movie = await _xtream.GetMovieDetailAsync(movie);
            if (_xtream is not DatabaseService databaseService)
                return;
            movie = await databaseService.LoadMovieFromDB(movieId);
            if (movie == null)
                return;
            AllMovies.Add(movie);
            SelectedMovie = movie;
        }
=======
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
    }
}