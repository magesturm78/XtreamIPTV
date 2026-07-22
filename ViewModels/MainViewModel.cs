<<<<<<< HEAD
﻿using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
=======
﻿using System.ComponentModel;
using System.Windows.Input;
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
using XtreamIPTV.Models;
using XtreamIPTV.Services;
using XtreamIPTV.Views;

namespace XtreamIPTV.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
<<<<<<< HEAD
        private readonly DispatcherTimer _hideTimer = new();

        public static MainViewModel Instance { get; private set; }
        private string _title = "XtreamIPTV";
        private bool _download = false;
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
=======

>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
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

<<<<<<< HEAD
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

        public LiveViewModel LiveVM { get; }
        public MoviesViewModel MoviesVM { get; }
        public SeriesViewModel SeriesVM { get; }
        public PlayerViewModel PlayerVM { get; }
        public SearchViewModel SearchVM { get; }
        public SettingsViewModel SettingsVM { get; }

        public SearchView SearchView { get; }
        public LiveView LiveView { get; }
        public MoviesView MoviesView { get; }
        public SeriesView SeriesView { get; }
        public SettingsView SettingsView { get; }

        public FavoritesService Favorites { get; }
        public ContinueWatchingService Continue { get; }
        public SeriesEpisodeService SeriesProgress { get; }
        public IIPTVService Xtream { get; }

        public ICommand ShowLiveCommand { get; }
        public ICommand ShowSearchCommand { get; }
        public ICommand ShowSeriesCommand { get; }
        public ICommand ShowMoviesCommand { get; }
        public ICommand ShowSettingsCommand { get; }

        public MainViewModel()
        {
            Instance = this;

            SettingsVM = new SettingsViewModel();

            if (SettingsVM.XtreamMode == "Database") { 
                Xtream = new DatabaseService();
                _download = true;
            }
            else 
                Xtream = new XtreamCodesService();

            Favorites = new FavoritesService();
            Continue = new ContinueWatchingService();
            SeriesProgress = new SeriesEpisodeService();

            LiveVM = new LiveViewModel(Xtream);
            MoviesVM = new MoviesViewModel(Xtream, Favorites, Continue);
            SeriesVM = new SeriesViewModel(Xtream, Favorites, SeriesProgress);
            SearchVM = new SearchViewModel(MoviesVM, SeriesVM);
            PlayerVM = new PlayerViewModel(Continue);

            LiveView = new LiveView { DataContext = LiveVM };
            SearchView = new SearchView { DataContext = SearchVM };
            MoviesView = new MoviesView { DataContext = MoviesVM };
            SeriesView = new SeriesView { DataContext = SeriesVM };
            SettingsView = new SettingsView { DataContext = SettingsVM };

            ShowSearchCommand = new RelayCommand(_ => CurrentView = SearchView);
            ShowLiveCommand = new RelayCommand(_ => CurrentView = LiveView);
            ShowMoviesCommand = new RelayCommand(_ => CurrentView = MoviesView);
            ShowSeriesCommand = new RelayCommand(_ => CurrentView = SeriesView);
            ShowSettingsCommand = new RelayCommand(_ => CurrentView = SettingsView);

            CurrentView = LiveView;

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
                $"https://vidfast.vc/tv/{ep.SeriesId}/{ep.SeasonId}/{ep.EpisodeNumber}?autoPlay=true&server=Alpha",
            ];
            string url = string.Empty;
            var title = $"{SeriesVM.GetSeriesTitle(ep.SeriesId)} {ep.Title}";

            //PlayerVM.Play($"series-{ep.EpisodeId}", title, $"https://www.google.com/search?q={UrlEncoder.Default.Encode(title)}&udm=7");
            //CurrentView = new HTMLPlayerView { DataContext = PlayerVM };
            //SeriesProgress.SetLastWatched(ep);
            //Title = $"XtreamIPTV Searching {title}";
            ////MessageBox.Show($"Cannot locate {title}");
            //return false;

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

            if (urls.Contains(ep.StreamUrl))
            {
                if (external)
                {
                    Process.Start(new ProcessStartInfo(ep.StreamUrl) { UseShellExecute = true });
                }
                else
                {
                    PlayerVM.Play($"episode-{ep.EpisodeId}", title, ep.StreamUrl);
                    CurrentView = new HTMLPlayerView { DataContext = PlayerVM };
                    SeriesProgress.SetLastWatched(ep);
                    Title = $"XtreamIPTV Playing {title}";
                }
            }
            else if (!string.IsNullOrEmpty(ep.StreamUrl))
            {
                var response2 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, ep.StreamUrl));

                ep.StreamUrl = response2?.RequestMessage?.RequestUri?.AbsoluteUri ?? ep.StreamUrl;
                //Always download to E:\Episodes and play from there to avoid streaming issues and allow external players to play without streaming issues
                if (!ep.StreamUrl.StartsWith(@"E:\"))
                {
                    DownloadEpisode(ep, external, client);
                }
                if (external)
                {
                    string exe = "C:\\Program Files\\MPC-HC\\mpc-hc64.exe";
                    string arguments = $"\"{ep.StreamUrl}\"";
                    Process.Start(exe, arguments);
                }
                else
                {
                    PlayerVM.ErrorMessage = string.Empty;
                    PlayerVM.Play($"episode-{ep.EpisodeId}", title, ep.StreamUrl);
                    CurrentView = new PlayerView { DataContext = PlayerVM };
                    Title = $"XtreamIPTV Playing {title}";
                }
                SeriesProgress.SetLastWatched(ep);
            }
            else
            {
                //https://www.google.com/search?q={title.Replace(" ", "+")}
                PlayerVM.Play($"episode-{ep.EpisodeId}", title, $"https://www.google.com/search?q={UrlEncoder.Default.Encode(title)}&udm=7");
                CurrentView = new HTMLPlayerView { DataContext = PlayerVM };
                SeriesProgress.SetLastWatched(ep);
                Title = $"XtreamIPTV Searching {title}";
                //MessageBox.Show($"Cannot locate {title}");
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
            string contentType = string.Empty;
            List<string> urls = [
                $"https://vidfast.vc/movie/{movie.Id}?autoPlay=true&server=vFast",
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
                    contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
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
                            contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
                            break;
                        }
                    }
                }
            }
            else if (!movie.StreamUrl.StartsWith(@"E:\"))
            {
                var response2 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, movie.StreamUrl));

                contentType = response2.Content.Headers.ContentType?.MediaType ?? string.Empty;
            }

            //movie.StreamUrl = string.Empty;
            //contentType = string.Empty;

            if (!string.IsNullOrEmpty(movie.StreamUrl) && 
                !string.IsNullOrEmpty(contentType) && 
                contentType != "application/octet-stream" &&
                contentType != "video/mp4")
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
                    if (movie.StreamUrl.EndsWith(".m3u8"))
                    {
                        CurrentView = new HTMLPlayerView { DataContext = PlayerVM };
                    }
                    else
                    {
                        CurrentView = new PlayerView { DataContext = PlayerVM };
                    }
                    Title = $"XtreamIPTV Playing {movie.Title}";
                }
            }
            else
            {
                Directory.GetDirectories("E:\\Movies").ToList().ForEach(d =>
                {
                    if (d.StartsWith($"E:\\Movies\\{movie.Id}."))
                    {
                        string fName = d + "\\index.m3u8";
                        if (File.Exists(fName) && M3U8Downloader.ValidM3U8(d))
                        {
                            movie.StreamUrl = d + "\\index.m3u8";
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
                            return;
                        }
                    }
                });
                if (string.IsNullOrEmpty(movie.StreamUrl))
                {
                    PlayerVM.Play($"movie-{movie.Id}", movie.Title, $"https://www.google.com/search?q={movie.Title.Replace(" ", "+").Replace("(", "%28").Replace(")", "%29")}&udm=7");
                    CurrentView = new HTMLPlayerView { DataContext = PlayerVM };
                    Title = $"XtreamIPTV Searching {movie.Title}";
                    //url = await GetPrimewireURL(movie, client);
                    //Process.Start(new ProcessStartInfo($"https://www.primewire.mov/api/v1/s?tmdb={movie.Id}&type=movie") { UseShellExecute = true });
                    //MessageBox.Show($"Cannot locate {movie.Title}");
                }
                return;
            }
            Title = $"XtreamIPTV Playing {movie.Title}";
        }

        public async Task<string> GetMediaType(string url)
        {
            using var client = new HttpClient();

            // Use HEAD request to get headers only (saves bandwidth)
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await client.SendAsync(request);

            // If HEAD is not supported by the server, fall back to a partial GET
            if (!response.IsSuccessStatusCode)
            {
                // Use HttpCompletionOption.ResponseHeadersRead to stop after headers are received
                response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            }

            return response.Content.Headers.ContentType?.MediaType;
        }

        private async Task<bool> DownloadMovie(Movie movie, bool external, HttpClient client)
        {
            if (!_download) return false;
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

        private async Task<bool> DownloadEpisode(Episode episode, bool external, HttpClient client)
        {
            if (!_download) return false;
            // Start streaming the download
            char[] invalidChars = Path.GetInvalidFileNameChars();

            // Use LINQ Aggregate for a concise replacement
            // This approach traverses the string once
            string safeName = invalidChars.Aggregate(episode.Title, (current, c) => current.Replace(c, '_'));

            // Optional: Trim trailing periods and spaces, which are invalid on Windows
            safeName = safeName.TrimEnd('.', ' ');
            string localPath = Path.Combine(@"E:\Episodes", $"{episode.EpisodeId}.{safeName}.{episode.StreamUrl.Split('.').Last()}");

            using var response = await client.GetAsync(episode.StreamUrl, HttpCompletionOption.ResponseHeadersRead);
            using var streamToRead = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

            // Copy to file while playing
            await streamToRead.CopyToAsync(fileStream);
            episode.StreamUrl = localPath;
            if (!external)
                PlayerVM.Play($"episode-{episode.EpisodeId}", episode.Title, episode.StreamUrl);
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
                    Process.Start(new ProcessStartInfo($"https://www.primewire.mov/api/v1/s?tmdb={movie.Id}&type=movie") { UseShellExecute = true });
                    return string.Empty;
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

        internal void NavigateToUri(string id, string title, Uri uri)
        {
            PlayerVM.Play(id, title, uri.ToString());
            CurrentView = new HTMLPlayerView { DataContext = PlayerVM };
            Title = $"XtreamIPTV {uri}";
        }

        internal void PlayLive(Live selectedLive)
        {
            PlayerVM.Play($"live-{selectedLive.StreamId}", selectedLive.Name, selectedLive.StreamUrl);
            CurrentView = new PlayerView { DataContext = PlayerVM };
            Title = $"XtreamIPTV {selectedLive.Name}";
=======
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
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
        }
    }
}