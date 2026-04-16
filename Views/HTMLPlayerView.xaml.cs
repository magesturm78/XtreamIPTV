using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using XtreamIPTV.Controls;
using XtreamIPTV.Models;
using XtreamIPTV.ViewModels;

namespace XtreamIPTV.Views
{
    /// <summary>
    /// Interaction logic for HTMLPlayerView.xaml
    /// </summary>
    public partial class HTMLPlayerView : UserControl
    {
        private readonly DispatcherTimer _timer = new();
        private PlayerViewModel? VM => DataContext as PlayerViewModel;

        public HTMLPlayerView()
        {
            InitializeComponent();
            InitializeWebView();
            Unloaded += (_, _) =>
            {
                _timer?.Stop();
                UpdateTimePlayed();
                webView?.CoreWebView2?.NavigateToString("<HTML></HTML>");
            };
        }

        private async void InitializeWebView()
        {
            await webView.EnsureCoreWebView2Async(null);

            webView.CoreWebView2.Navigate(VM?.CurrentStreamUrl);
            webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested1;

            webView.CoreWebView2.NavigationCompleted += async (s, args) =>
            {
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
                    if (x < 25 && y < 200)
                    {
                        MainViewModel.Instance.ShowSideNav();
                    }
                };
                _timer.Interval = TimeSpan.FromSeconds(5);
                _timer.Tick += (_, _) =>
                {
                    UpdateTimePlayed();
                };
                _timer.Start();
            };

            webView.CoreWebView2.SourceChanged += (s, args) =>
            {
                if (webView.CoreWebView2.Source == VM?.CurrentStreamUrl) return;

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

                        mvm.Title = $"XtreamIPTV Playing {title}";

                    }
                    if (VM?.CurrentEpisodeId.StartsWith("movie") == true)
                    {
                        mvm.ShowMoviesCommand.Execute(this);
                    }
                }
            };

            webView.CoreWebView2.ContainsFullScreenElementChanged += (s, args) =>
            {
                bool isFullScreen = webView.CoreWebView2.ContainsFullScreenElement;
                ToggleFormFullScreen(isFullScreen);
            };
        }

        private async void UpdateTimePlayed()
        {
            string currentTime = await webView.ExecuteScriptAsync("document.querySelector('video').currentTime.toString();");
            if (!string.IsNullOrEmpty(currentTime))
            {
                Debug.Print(currentTime);
                if (double.TryParse(currentTime.Trim('"'), out double seconds))
                {
                    VM?.SavePosition(seconds);
                }
            }
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
