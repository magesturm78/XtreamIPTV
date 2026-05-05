using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Security.Policy;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using XtreamIPTV.Models;
using XtreamIPTV.Services;
using XtreamIPTV.Views;
using static System.Net.WebRequestMethods;

namespace XtreamIPTV.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private readonly DispatcherTimer _hideTimer = new();

        public static MainViewModel Instance { get; private set; }
        private string _title = "XtreamIPTV";
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                PropertyChanged?.Invoke(this, new(nameof(Title)));
            }
        }
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

        private Visibility _menuVisibility = Visibility.Collapsed;
        public Visibility MenuVisibility
        {
            get => _menuVisibility;
            set
            {
                _menuVisibility = value;
                PropertyChanged?.Invoke(this, new(nameof(MenuVisibility)));
            }
        }

        public MoviesViewModel MoviesVM { get; }
        public SeriesViewModel SeriesVM { get; }
        public PlayerViewModel PlayerVM { get; }
        public SearchViewModel SearchVM { get; }
        public SettingsViewModel SettingsVM { get; }

        public SearchView SearchView { get; }
        public MoviesView MoviesView { get; }
        public SeriesView SeriesView { get; }
        public SettingsView SettingsView { get; }

        public FavoritesService Favorites { get; }
        public ContinueWatchingService Continue { get; }
        public SeriesEpisodeService SeriesProgress { get; }
        public IIPTVService Xtream { get; }

        public ICommand ShowSearchCommand { get; }
        public ICommand ShowSeriesCommand { get; }
        public ICommand ShowMoviesCommand { get; }
        public ICommand ShowSettingsCommand { get; }

        public MainViewModel()
        {
            Instance = this;
            //NCW
            //Xtream = new XtreamCodesService();
            Xtream = new DatabaseService();

            Favorites = new FavoritesService();
            Continue = new ContinueWatchingService();
            SeriesProgress = new SeriesEpisodeService();

            MoviesVM = new MoviesViewModel(Xtream, Favorites, Continue);
            SeriesVM = new SeriesViewModel(Xtream, Favorites, SeriesProgress);
            SearchVM = new SearchViewModel(MoviesVM, SeriesVM);
            PlayerVM = new PlayerViewModel(Continue);
            SettingsVM = new SettingsViewModel();

            SearchView = new SearchView { DataContext = SearchVM };
            MoviesView = new MoviesView { DataContext = MoviesVM };
            SeriesView = new SeriesView { DataContext = SeriesVM };
            SettingsView = new SettingsView { DataContext = SettingsVM };

            ShowSearchCommand = new RelayCommand(_ => CurrentView = SearchView);
            ShowMoviesCommand = new RelayCommand(_ => CurrentView = MoviesView);
            ShowSeriesCommand = new RelayCommand(_ => CurrentView = SeriesView);
            ShowSettingsCommand = new RelayCommand(_ => CurrentView = SettingsView);

            CurrentView = SearchView;

            _hideTimer.Interval = TimeSpan.FromSeconds(1); // Hide after 3 seconds
            _hideTimer.Tick += (_, _) =>
            {
                Point mousePos = Mouse.GetPosition(Application.Current.MainWindow);
                // Check if mouse is on the left side (X < half of width)
                if (mousePos.X < 25 && mousePos.Y < 250)
                    return;

                // Hide the control
                MenuVisibility = Visibility.Collapsed;
                _hideTimer.Stop();
            };

            _hideTimer.Start();
        }

        public async Task<bool> PlayEpisode(Episode ep, bool external = false)
        {
            HttpClient client = new();
            List<string> urls = [
                $"https://vidfast.pro/tv/{ep.SeriesId}/{ep.SeasonId}/{ep.EpisodeNumber}?autoPlay=true&server=Alpha",
            ];
            string url = string.Empty;
            if (string.IsNullOrEmpty(ep.StreamUrl))
            {
                if (!string.IsNullOrEmpty(ep.DirectSource))
                {
                    url = ep.DirectSource;
                    var responseds = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                    if (responseds != null && responseds.IsSuccessStatusCode)
                    {
                        url = responseds.RequestMessage?.RequestUri?.AbsoluteUri ?? url;
                        ep.StreamUrl = url;
                    }
                } 
                else 
                {
                    HttpClient tmdbclient = new HttpClient();
                    tmdbclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ConfigurationManager.AppSettings["Bearer"]);
                    tmdbclient.DefaultRequestHeaders.Add("accept", "application/json");

                    url = $"https://api.themoviedb.org/3/tv/225171/external_ids?series_id={ep.SeriesId}&language=en-US";
                    var json = await tmdbclient.GetStringAsync(url);
                    var data = JsonSerializer.Deserialize<JsonElement>(json);
                    var imdb_id = data.GetProperty("imdb_id").ToString();

                    var plainTextBytes = System.Text.Encoding.UTF8.GetBytes($"{imdb_id}:{ep.SeriesId}/season/{ep.SeasonId}/episode/{ep.EpisodeNumber}");
                    url = $"http://159.203.85.251/play.php?type=series&movieId={ep.EpisodeId}&data={Convert.ToBase64String(plainTextBytes)}";
                    var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                    if (response == null)
                        return false;
                    if (response.IsSuccessStatusCode)
                    {
                        url = response.RequestMessage?.RequestUri?.AbsoluteUri ?? url;
                        ep.StreamUrl = url;
                    }
                }
                if (string.IsNullOrEmpty(ep.StreamUrl))
                {
                    foreach (string checkpath in urls)
                    {
                        //HeadlessVidX check if url has video and return direct link to video
                        url = $"http://localhost:3202/get-video?url={WebUtility.UrlEncode(checkpath).Replace("/", "%2F").Replace(":", "%3A")}";
                        var json2 = await client.GetStringAsync(url);
                        var data2 = JsonSerializer.Deserialize<JsonElement>(json2);
                        if (data2.GetProperty("status").ToString() == "ok")
                        {
                            ep.StreamUrl = checkpath;
                            break;
                        }
                    }
                }
            }

            var title = $"{SeriesVM.GetSeriesTitle(ep.SeriesId)} {ep.Title}";
            if (urls.Contains(ep.StreamUrl))
            {
                if (external)
                {
                    Process.Start(new ProcessStartInfo(ep.StreamUrl) { UseShellExecute = true });
                }
                else
                {
                    PlayerVM.Play($"series-{ep.EpisodeId}", title, ep.StreamUrl);
                    CurrentView = new HTMLPlayerView { DataContext = PlayerVM };
                    SeriesProgress.SetLastWatched(ep);
                    Title = $"XtreamIPTV Playing {title}";
                }
            }
            else if (!string.IsNullOrEmpty(ep.StreamUrl))
            {
                var response2 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, ep.StreamUrl));

                ep.StreamUrl = response2?.RequestMessage?.RequestUri?.AbsoluteUri ?? ep.StreamUrl;
                if (external)
                {
                    string exe = "C:\\Program Files\\MPC-HC\\mpc-hc64.exe";
                    string arguments = $"\"{ep.StreamUrl}\"";
                    Process.Start(exe, arguments);
                }
                else
                {
                    PlayerVM.ErrorMessage = string.Empty;
                    PlayerVM.Play($"series-{ep.EpisodeId}", title, ep.StreamUrl);
                    CurrentView = new PlayerView { DataContext = PlayerVM };
                    Title = $"XtreamIPTV Playing {title}";
                }
                SeriesProgress.SetLastWatched(ep);
            }
            else
            {
                MessageBox.Show($"Cannot locate {title}");
                return false;
            }
            return true;
        }

        public async Task PlayMovie(Movie movie, bool external = false)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            HttpClient client = new(handler);
            Directory.GetFiles("E:\\Movies",$"{movie.Id}.*.*").ToList().ForEach(f =>
            {
                movie.StreamUrl = f;
            });
            List<string> urls = [
                $"https://vidfast.pro/movie/{movie.Id}?autoPlay=true&server=vFast",
                        //$"https://vidsrc.xyz/embed/movie/{movie.Id}?autoplay=1",
                        //$"https://111movies.com/movie/{movie.Id}",
                        //$"https://vidsrc.cc/v3/embed/movie/{movie.Id}",
                    ];
            string url = string.Empty;
            //movie.StreamUrl = urls[0];
            //url = await GetPrimewireURL(movie, client);
            //return;
            if (string.IsNullOrEmpty(movie.StreamUrl))
            {
                url = $"http://159.203.85.251/play.php?movieId={movie.Id}";
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                if (response == null)
                    return;
                if (response.IsSuccessStatusCode)
                {
                    url = response.RequestMessage?.RequestUri?.AbsoluteUri ?? url;
                    movie.StreamUrl = url;
                }
                else
                {
                    foreach (string checkpath in urls)
                    {
                        ////HeadlessVidX check if url has video and return direct link to video
                        url = $"http://localhost:3202/get-video?url={WebUtility.UrlEncode(checkpath).Replace("/", "%2F").Replace(":", "%3A")}";
                        var json = await client.GetStringAsync(url);
                        var data = JsonSerializer.Deserialize<JsonElement>(json);
                        if (data.GetProperty("status").ToString() == "ok")
                        {
                            movie.StreamUrl = checkpath;
                            break;
                        }
                    }
                }
            }
            if (urls.Contains(movie.StreamUrl))
            {
                if (external)
                {
                    Process.Start(new ProcessStartInfo(movie.StreamUrl) { UseShellExecute = true });
                }
                else
                {
                    PlayerVM.Play($"movie-{movie.Id}", movie.Title, movie.StreamUrl);
                    CurrentView = new HTMLPlayerView { DataContext = PlayerVM };
                    Title = $"XtreamIPTV Playing {movie.Title}";
                }
            }
            else if (!string.IsNullOrEmpty(movie.StreamUrl))
            {
                if (movie.StreamUrl.StartsWith("http"))
                {
                    try
                    {
                        var response2 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, movie.StreamUrl));

                        movie.StreamUrl = response2?.RequestMessage?.RequestUri?.AbsoluteUri ?? movie.StreamUrl;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
                //Always download to E:\Movies and play from there to avoid streaming issues and allow external players to play without streaming issues
                if (!movie.StreamUrl.StartsWith(@"E:\"))
                {
                    DownloadMovie(movie, external, client);
                }
                if (external)
                {
                    string exe = "C:\\Program Files\\MPC-HC\\mpc-hc64.exe";
                    string arguments = $"\"{movie.StreamUrl}\"";
                    Process.Start(exe, arguments);
                }
                else
                {
                    PlayerVM.ErrorMessage = string.Empty;
                    PlayerVM.Play($"movie-{movie.Id}", movie.Title, movie.StreamUrl);
                    CurrentView = new PlayerView { DataContext = PlayerVM };
                    Title = $"XtreamIPTV Playing {movie.Title}";
                }
            }
            else
            {
                //url = await GetPrimewireURL(movie, client);
                //Process.Start(new ProcessStartInfo($"https://www.primewire.mov/api/v1/s?tmdb={movie.Id}&type=movie") { UseShellExecute = true });
                MessageBox.Show($"Cannot locate {movie.Title}");
                return;
            }
            Title = $"XtreamIPTV Playing {movie.Title}";
        }

        private async Task<bool> DownloadMovie(Movie movie, bool external, HttpClient client)
        {
            // Start streaming the download
            char[] invalidChars = Path.GetInvalidFileNameChars();

            // Use LINQ Aggregate for a concise replacement
            // This approach traverses the string once
            string safeName = invalidChars.Aggregate(movie.Title, (current, c) => current.Replace(c, '_'));

            // Optional: Trim trailing periods and spaces, which are invalid on Windows
            safeName = safeName.TrimEnd('.', ' ');
            string localPath = Path.Combine(@"E:\Movies", $"{movie.Id}.{safeName}.{movie.StreamUrl.Split('.').Last()}");

            using var response = await client.GetAsync(movie.StreamUrl, HttpCompletionOption.ResponseHeadersRead);
            using var streamToRead = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

            // Copy to file while playing
            await streamToRead.CopyToAsync(fileStream);
            movie.StreamUrl = localPath;
            if (!external)
                PlayerVM.Play($"movie-{movie.Id}", movie.Title, movie.StreamUrl);
            return true;
        }

        private static async Task<string> GetPrimewireURL(Movie movie, HttpClient client)
        {
            string url = $"https://www.primewire.mov/api/v1/s?tmdb={movie.Id}&type=movie";
            var json = await client.GetStringAsync(url);
            PrimewireRoot primewireData = JsonSerializer.Deserialize<PrimewireRoot>(json);

            if (primewireData == null) return string.Empty;

            var servers = primewireData.servers.OrderBy(s => s.file_size);

            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            foreach (var server in servers)
            {
                try
                {
                    url = $"https://www.primewire.mov/api/v1/l?key={server.key}";

                    using var msg = new HttpRequestMessage(HttpMethod.Get, new Uri(url));

                    using var req = await client.SendAsync(msg);

                    var str1 = await req.Content.ReadAsStringAsync();

                    KeyData keyData = JsonSerializer.Deserialize<KeyData>(str1);

                    if (keyData == null) continue;

                    return keyData.link;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
            MessageBox.Show($"Cannot locate {movie.Title}");

            return string.Empty;
        }

        public async Task Search()
        {
            var task1 = MoviesVM.Search(SearchVM.SearchText);
            var task2 = SeriesVM.Search(SearchVM.SearchText);

            var tasks = new List<Task> { task1, task2 };
            // 3. Await Task.WhenAll to wait for all of them to finish
            await Task.WhenAll(tasks);
        }

        internal void RemoveLastWatched(Series series)
        {
            SeriesProgress.RemoveLastWatched(series.Id);
            foreach (var episode in series.Seasons.SelectMany(s => s.Episodes).ToList())
                Continue.RemoveProgress($"series-{episode.EpisodeId}");
        }

        internal void RemoveLastWatched(Movie movie)
        {
            Continue.RemoveProgress($"movie-{movie.Id}");
        }

        internal void ShowSideNav()
        {
            MenuVisibility = Visibility.Visible;
            _hideTimer.Stop();
            _hideTimer.Start();
        }

        internal async Task PlayNextEpisode()
        {
            if (SeriesVM == null || SeriesVM.SelectedSeries == null || SeriesVM.SelectedEpisode == null) return;

            var currentEpisodeId = SeriesVM.SelectedEpisode.EpisodeId;
            var allEpisodes = SeriesVM.SelectedSeries.Seasons.SelectMany(s => s.Episodes).ToList();
            var lastWatchedEpisode = allEpisodes.FirstOrDefault(n => n.EpisodeId == currentEpisodeId);

            var nextEpisode = allEpisodes.SkipWhile(x => x != lastWatchedEpisode)
                                    .Skip(1)
                                    .DefaultIfEmpty(allEpisodes[0]) // Wraps back to the first item if at the end
                                    .FirstOrDefault();

            if (nextEpisode == null) return;

            SeriesVM.SelectedEpisode = nextEpisode;
            _ = await PlayEpisode(nextEpisode);
        }

        internal void NavigateToUri(Uri uri)
        {
            PlayerVM.Play($"", "", uri.ToString());
            CurrentView = new HTMLPlayerView { DataContext = PlayerVM };
            Title = $"XtreamIPTV {uri}";
        }
    }
}