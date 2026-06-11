using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Security.Policy;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using XtreamIPTV.Models;
using XtreamIPTV.Services;
using XtreamIPTV.ViewModels;

namespace XtreamIPTV.Views
{
    /// <summary>
    /// Interaction logic for HTMLPlayerView.xaml
    /// </summary>
    public partial class HTMLPlayerView : UserControl
    {
        private DispatcherTimer? _timer;
        private PlayerViewModel? VM => DataContext as PlayerViewModel;

        public HTMLPlayerView()
        {
            InitializeComponent();
            InitializeWebView();
            Unloaded += (_, _) =>
            {
                _timer?.Stop();
                _timer = null;
                UpdateTimePlayed();
                webView?.CoreWebView2?.NavigateToString("<HTML></HTML>");
            };
        }

        private async void InitializeWebView()
        {
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);

            webView.CoreWebView2.Navigate(VM?.CurrentStreamUrl);
            webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested1;
            webView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;

            webView.CoreWebView2.NavigationCompleted += async (s, args) =>
            {
                string filename = $"log_{DateTime.Now:yyyy_MM_dd}.txt";
                File.AppendAllText(filename, $"\r\n\r\nNewPage\r\n{DateTime.Now} : \t\t{webView.CoreWebView2.Source}\r\n");
                _ = await webView.CoreWebView2.ExecuteScriptAsync("const vid = document.querySelector('video'); " +
                                                                  "vid.addEventListener('loadedmetadata', function() {" +
                                                                  $"    this.currentTime = {VM?.StartPositionSeconds}; " +
                                                                  "}, false);");
                _ = await webView.CoreWebView2.ExecuteScriptAsync(@"window.addEventListener('mousemove', (event) => {
                                                                        window.chrome.webview.postMessage({
                                                                            x: event.clientX,
                                                                            y: event.clientY
                                                                        });
                                                                    });");

                webView.WebMessageReceived += (s, args) =>
                {
                    var data = JsonSerializer.Deserialize<JsonElement>(args.WebMessageAsJson);
                    var x = data.GetProperty("x").GetInt32();
                    var y = data.GetProperty("y").GetInt32();
                    // Check if mouse is on the left side (X < half of width)
                    if (x < 25 && y < 250)
                    {
                        MainViewModel.Instance.ShowSideNav();
                    }
                };
                if (_timer == null && webView.CoreWebView2.Source != "about:blank")
                {
                    _timer = new();
                    _timer.Interval = TimeSpan.FromSeconds(5);
                    _timer.Tick += (_, _) =>
                    {
                        UpdateTimePlayed();
                    };
                    _timer.Start();
                }
            };

            webView.CoreWebView2.SourceChanged += (s, args) =>
            {
                if (webView.CoreWebView2.Source == VM?.CurrentStreamUrl) return;
                if (webView.CoreWebView2.Source.StartsWith("https://www.google.com")) return;

                var window = System.Windows.Window.GetWindow(this);
                if (window?.DataContext is MainViewModel mvm)
                {
                    if (VM?.CurrentEpisodeId.StartsWith("series") == true)
                    {
                        //https://vidfast.pro/tv/21510/1/6?autoPlay=true&server=Alpha
                        var info = webView.CoreWebView2.Source.Split(new[] { "https://vidfast.pro/tv/", "?autoPlay=true" }, StringSplitOptions.RemoveEmptyEntries);
                        var episode_info = info[0].Split('/');
                        var seasonId = int.Parse(episode_info[0]);
                        var seasonNum = int.Parse(episode_info[1]);
                        var episodeNum = int.Parse(episode_info[2]);

                        if (mvm == null || mvm.SeriesVM == null || mvm.SeriesVM.SelectedSeries == null)
                        {
                            mvm?.ShowSeriesCommand.Execute(this);
                            return;
                        }

                        if (mvm.SeriesVM.SelectedSeries.Id != seasonId)
                        {
                            mvm.ShowSeriesCommand.Execute(this);
                            return;
                        }

                        var season = mvm.SeriesVM.SelectedSeries?.Seasons.Where(s => s.SeasonNumber == seasonNum).FirstOrDefault();
                        if (season == null)
                        {
                            mvm.ShowSeriesCommand.Execute(this);
                            return;
                        }

                        var episode = season.Episodes.Where(s => s.EpisodeNumber == episodeNum).FirstOrDefault();
                        if (episode == null)
                        {
                            mvm.ShowSeriesCommand.Execute(this);
                            return;
                        }

                        episode.StreamUrl = webView.CoreWebView2.Source;
                        mvm.SeriesVM.SelectedEpisode = episode;

                        string title = $"{mvm.SeriesVM.GetSeriesTitle(episode.SeriesId)} {episode.Title}";
                        VM?.Play($"series-{episode.EpisodeId}", title, episode.StreamUrl);
                        VM?.CurrentEpisodeId = $"series-{episode.EpisodeId}";
                        mvm.SeriesProgress.SetLastWatched(episode);

                        mvm.Title = $"XtreamIPTV Playing {title}";

                    }
                    else if (VM?.CurrentEpisodeId.StartsWith("movie") == true)
                    {
                        //mvm.ShowMoviesCommand.Execute(this);
                    }
                    else if (webView.CoreWebView2.Source.StartsWith("https://www.themoviedb.org/movie/"))
                    {
                        var movieid = webView.CoreWebView2.Source.Replace("https://www.themoviedb.org/movie/", "").Split("-")[0];
                        if (!mvm.MoviesVM.AllMovies.Any(m => m.Id == int.Parse(movieid)))
                            mvm.MoviesVM.GetMovieDetails(int.Parse(movieid));
                    }
                }
            };

            webView.CoreWebView2.ContainsFullScreenElementChanged += (s, args) =>
            {
                bool isFullScreen = webView.CoreWebView2.ContainsFullScreenElement;
                //ToggleFormFullScreen(isFullScreen);
            };
        }

        string[] adDomains = {
                "doubleclick.net",
                "google-analytics.com",
                "googlesyndication.com",
                "://pubmatic.com",
                "://google.com",
                "://analytics.google.com",
                "://cdn.pornfhd.com/",
                "://t.snaptrckr.fun",
                "://hotsoz.com",
                "://pics.pornfhd.com",
                "tsyndicate.com",
                "://cdnjs.elastic-sync.xyz",
                "://cdn.twinrdengine.com",
                "://media-hls.saawsedge.com",
                "://www-freeporntube-com",
                "://zw2a.cupidnets.com"
        };
        private void CoreWebView2_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            string uri = e.Request.Uri;
            string filename = $"log_{DateTime.Now:yyyy_MM_dd}.txt";
            File.AppendAllText(filename, $"{DateTime.Now} : \t\t{uri}\r\n");

            foreach (string domain in adDomains)
            {
                if (uri.Contains(domain))
                {
                    // Cancel the request by returning an empty 404 response
                    e.Response = webView.CoreWebView2.Environment.CreateWebResourceResponse(
                        null, 404, "Blocked", null);
                    return;
                }
            }
            //File.AppendAllText(filename, $"{DateTime.Now} : \t\t\t{e.ResourceContext}\r\n");
            if (VM?.CurrentEpisodeId.StartsWith("movie") == false)
                return;

            if (!uri.StartsWith("file:") && (uri.Split("?")[0].EndsWith(".m3u8") || uri.Contains("type=hls")))
            {
                var headers = new Dictionary<string, string>();

                foreach (var header in e.Request.Headers)
                    headers.Add(header.Key, header.Value);
                try
                {
                    download(uri, headers);
                } catch (Exception ex)
                {
                    Debug.Print($"Error downloading {uri}: {ex.Message}");
                }
            } else
            {
                string basePath = "E:\\movies";
                string movieDir = Path.Combine(basePath, MainViewModel.Instance.MoviesVM.SelectedMovie?.FileName);
                string m3u8FilePath = Path.Combine(movieDir, "orig.m3u8");
                if (File.Exists(m3u8FilePath)) {
                    var playlist_zzz = M3u8Playlist.Parse(m3u8FilePath);
                    int index = playlist_zzz.Segments.FindIndex(s => s.Uri == uri);
                    if (index >= 0)
                    {
                        string newFile = Path.Combine(movieDir, $"index{index}.ts");
                        if (File.Exists(newFile))
                        {
                            byte[] fileBytes = File.ReadAllBytes(newFile);
                            var memoryStream = new MemoryStream(fileBytes);
                            // 4. Create the Web Resource Response
                            var handler = new HttpClientHandler
                            {
                                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                            };

                            using var client = new HttpClient(handler);

                            // Apply headers
                            foreach (var header in e.Request.Headers)
                                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);

                            using var request = new HttpRequestMessage(HttpMethod.Head, uri);

                            using var head_response = client.Send(request);

                            // Ensure the request was successful
                            head_response.EnsureSuccessStatusCode();

                            string headers = string.Empty;
                            // Retrieve all response headers
                            foreach (var header in head_response.Headers)
                            {
                                headers += $"{header.Key}: {string.Join(", ", header.Value)}\r\n";
                            }
                            var response = webView.CoreWebView2.Environment.CreateWebResourceResponse(
                                Content: memoryStream,
                                StatusCode: 200,
                                ReasonPhrase: "OK",
                                Headers: headers
                            );

                            // 5. Assign the response to the event args
                            Debug.Print($"loading from Cache: {index}, {uri}");
                            e.Response = response;
                            return;
                        }
                    }
                }
            }
        }

        private bool Downloading = false;
        private async void download(string url, Dictionary<string,string> headers)
        {
            if (Downloading) return;
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var client = new HttpClient(handler);

            // Apply headers
            if (headers != null)
            {
                foreach (var kv in headers)
                    client.DefaultRequestHeaders.TryAddWithoutValidation(kv.Key, kv.Value);
            }

            try
            {
                Stream stream = await client.GetStreamAsync(url);

                var uri = new Uri(url);
                var baseUrl = $"{uri.Scheme}://{uri.Host}";
                using var reader = new StreamReader(stream);

                if (reader == null) return;

                string line;
                bool downloadnextline = false;

                while ((line = reader.ReadLine()) != null)
                {
                    Debug.Print(line);
                    if (downloadnextline && !line.StartsWith("#"))
                    {
                        downloadnextline = false;
                        Debug.Print(baseUrl + line);
                        url = baseUrl + line;
                    }
                    if (line.StartsWith("#EXTINF"))
                    {
                        var myUniqueFileName = string.Format(@"{0}", Guid.NewGuid());
                        if (VM?.CurrentEpisodeId.StartsWith("movie") == true)
                        {
                            myUniqueFileName = (MainViewModel.Instance.MoviesVM.SelectedMovie?.FileName) ?? myUniqueFileName;
                        }
                        Debug.Print(url);
                        //return;
                        Downloading = true;
                        await M3U8Downloader.Download(
                            url,
                            myUniqueFileName,
                            headers
                        );
                        Downloading = false;
                        downloadnextline = true;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"Error downloading {url}: {ex.Message}");
            }
        }

        //bool downloaded = false;

        private async void UpdateTimePlayed()
        {
            try
            {
                string currentTime = await webView.ExecuteScriptAsync("document.querySelector('video').currentTime.toString();");
                if (!string.IsNullOrEmpty(currentTime))
                {
                    //Debug.Print(currentTime);
                    if (double.TryParse(currentTime.Trim('"'), out double seconds) && seconds > 0)
                    {
                        VM?.SavePosition(seconds);
                        if (VM?.CurrentEpisodeId.StartsWith("movie") == true)
                        {
                            var movie = MainViewModel.Instance.MoviesVM.AllMovies.FirstOrDefault(m => m.Id == int.Parse(VM?.CurrentEpisodeId.Split('-')[1]));
                            if (movie != null && movie.StreamUrl != webView.CoreWebView2.Source)
                            {
                                movie.StreamUrl = webView.CoreWebView2.Source;
                            }
                        }
                    }
                }
                //string source = await webView.ExecuteScriptAsync("document.querySelector('video').currentSrc;");
                //downloaded = true;
                //if (!string.IsNullOrEmpty(source) && source.StartsWith("\"http") && !downloaded)
                //{
                //    source = source.Replace("\"", "");
                //    Debug.Print(source);
                //    var mediaType = await MainViewModel.Instance.GetMediaType(source);
                //    if (mediaType == "video/mp4")
                //    {
                //        downloaded = true;
                //        var myUniqueFileName = string.Format(@"{0}.mp4", Guid.NewGuid());
                //        if (VM?.CurrentEpisodeId.StartsWith("movie") == true)
                //        {
                //            myUniqueFileName = (MainViewModel.Instance.MoviesVM.SelectedMovie?.FileName + ".mp4") ?? myUniqueFileName;
                //        }
                //        var handler = new HttpClientHandler
                //        {
                //            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                //        };

                //        using var client = new HttpClient(handler);
                //        using var response = await client.GetAsync(source, HttpCompletionOption.ResponseHeadersRead);
                //        using var streamToRead = await response.Content.ReadAsStreamAsync();
                //        using var fileStream = new FileStream($"E:\\movies\\{myUniqueFileName}", FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                //        await streamToRead.CopyToAsync(fileStream);
                //    }
                //    Debug.Print(mediaType);
                //}
            }
            catch { }
        }

        private void CoreWebView2_NewWindowRequested1(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
        }

        private void ToggleFormFullScreen(bool isFullScreen)
        {
            var window = System.Windows.Window.GetWindow(this);
            if (window == null) return;
            if (isFullScreen)
            {
                window.WindowStyle = WindowStyle.None;
                window.WindowState = WindowState.Maximized;
                window.ResizeMode = ResizeMode.NoResize;
            }
            else
            {
                window.WindowStyle = WindowStyle.SingleBorderWindow;
                window.WindowState = WindowState.Normal;
                window.ResizeMode = ResizeMode.CanResize;
            }
            window.Topmost = isFullScreen;
        }
    }
}
