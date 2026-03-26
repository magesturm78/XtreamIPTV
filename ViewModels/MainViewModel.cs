using System.ComponentModel;
using System.Windows.Input;
using XtreamIPTV.Models;
using XtreamIPTV.Services;
using XtreamIPTV.Views;

namespace XtreamIPTV.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public object? CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                PropertyChanged?.Invoke(this, new(nameof(CurrentView)));
            }
        }
        private object? _currentView;

        public MoviesViewModel MoviesVM { get; }
        public SeriesViewModel SeriesVM { get; }
        public PlayerViewModel PlayerVM { get; }

        public FavoritesService Favorites { get; }
        public ContinueWatchingService Continue { get; }
        public IIPTVService Xtream { get; }

        public ICommand ShowSeriesCommand { get; }
        public ICommand ShowMoviesCommand { get; }
        public ICommand ShowPlayerCommand { get; }

        public MainViewModel()
        {
            //NCW
            //Xtream = new XtreamCodesService();
            Xtream = new DatabaseService();
            Favorites = new FavoritesService();
            Continue = new ContinueWatchingService();

            // TODO: configure with your server
            // Xtream.Configure("http://your-server:port", "username", "password");

            MoviesVM = new MoviesViewModel(Xtream, Favorites);
            SeriesVM = new SeriesViewModel(Xtream, Favorites);
            PlayerVM = new PlayerViewModel(Continue);

            ShowMoviesCommand = new RelayCommand(_ => CurrentView = new MoviesView { DataContext = MoviesVM });
            ShowSeriesCommand = new RelayCommand(_ => CurrentView = new SeriesView { DataContext = SeriesVM });
            ShowPlayerCommand = new RelayCommand(_ => CurrentView = new PlayerView { DataContext = PlayerVM });

            CurrentView = new MoviesView { DataContext = MoviesVM };
        }

        public void PlayEpisode(Episode ep)
        {
            PlayerVM.ErrorMessage = string.Empty;
            PlayerVM.Play(ep.EpisodeId, ep.StreamUrl);
            CurrentView = new PlayerView { DataContext = PlayerVM };
        }

        public void PlayMovie(Movie movie)
        {
            PlayerVM.ErrorMessage = string.Empty;
            PlayerVM.Play(movie.Id.ToString(), movie.StreamUrl);
            CurrentView = new PlayerView { DataContext = PlayerVM };
        }
    }
}