using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using XtreamIPTV.Models;
using XtreamIPTV.ViewModels;

namespace XtreamIPTV.Views
{
    public partial class PlayerView : UserControl
    {
        private readonly DispatcherTimer _timer = new();
        private readonly DispatcherTimer _hideTimer = new();
        private PlayerViewModel? VM => DataContext as PlayerViewModel;

        public PlayerView()
        {
            InitializeComponent();

            VideoPlayer.Volume = 1.0;

            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (_, _) =>
            {
                VM?.SavePosition(VideoPlayer.Position.TotalSeconds);
                txtCurrentPosition.Text = VideoPlayer.Position.ToString(@"hh\:mm\:ss");
                positionSlider.Value = VideoPlayer.Position.TotalSeconds;
            };

            _hideTimer.Interval = TimeSpan.FromSeconds(3); // Hide after 3 seconds
            _hideTimer.Tick += (_, _) =>
            {
                // Hide the control
                VideoControls.Visibility = Visibility.Hidden;
                PositionGrid.Visibility = Visibility.Hidden;
                TitleText.Visibility = Visibility.Hidden;
                Cursor = Cursors.None;
                _hideTimer.Stop();
            }; 
            
            _hideTimer.Start();
            
            VideoPlayer.MediaFailed += (o, args) => {
                VM?.ErrorMessage = "Media Failed: " + args.ErrorException.Message;
            };

            VideoPlayer.MediaOpened += (o, args) =>
            {
                VideoControls.Visibility = Visibility.Visible;
                if (VM != null)
                    VideoPlayer.Position = TimeSpan.FromSeconds(VM.StartPositionSeconds);
                if (VideoPlayer.NaturalDuration.HasTimeSpan)
                {
                    TimeSpan duration = VideoPlayer.NaturalDuration.TimeSpan;
                    txtTotalDuration.Text = duration.ToString(@"hh\:mm\:ss");
                    positionSlider.Maximum = duration.TotalSeconds;
                    positionSlider.Value = VideoPlayer.Position.TotalSeconds;
                }
                _timer.Start();
            };

            Loaded += (_, _) =>
            {
                var window = System.Windows.Window.GetWindow(this);
                if (window?.DataContext is MainViewModel mvm)
                {
                    if (VM?.CurrentEpisodeId.StartsWith("series") == true)
                    {
                        positionSlider.SmallChange = 10;
                        positionSlider.LargeChange = 60;
                        NextButton.Visibility = Visibility.Visible;
                    }
                    if (VM?.CurrentEpisodeId.StartsWith("movie") == true)
                    {
                        positionSlider.SmallChange = 30;
                        positionSlider.LargeChange = 300;
                        NextButton.Visibility = Visibility.Hidden;
                    }
                }
                Play();
            };

            Unloaded += (_, _) =>
            {
                Cursor = Cursors.Arrow;
                _timer.Stop();
                Pause();
                VM?.SavePosition(VideoPlayer.Position.TotalSeconds);
            };
        }

        private void VideoPlayer_MediaEnded(object sender, System.Windows.RoutedEventArgs e)
        {
            VM?.SavePosition(0);
        }

        public void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlayPauseIcon.Data == (Geometry)FindResource("PlayIconData"))
            {
                Play();
            }
            else
            {
                Pause();
            }
        }

        private bool _isFullscreen = false;
        
        private void Media_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                PlayButton_Click(null, null);
            }
        }

        private void positionSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            VideoPlayer.Position = TimeSpan.FromSeconds(positionSlider.Value);
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            // Show the control
            VideoControls.Visibility = Visibility.Visible;
            PositionGrid.Visibility = Visibility.Visible;
            TitleText.Visibility = Visibility.Visible;
            Cursor = Cursors.Arrow;

            // Restart the timer to wait another 3 seconds
            _hideTimer.Stop();
            _hideTimer.Start();
        }

        private void Play()
        {
            VideoPlayer.Play();
            PlayPauseIcon.Data = (Geometry)FindResource("PauseIconData");
        }

        private void Pause()
        {
            VideoPlayer.Pause();
            PlayPauseIcon.Data = (Geometry)FindResource("PlayIconData");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Position -= TimeSpan.FromSeconds(10);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Position += TimeSpan.FromSeconds(10);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);

            if (window.WindowStyle != WindowStyle.None)
            {
                window.WindowStyle = WindowStyle.None;
                window.WindowState = WindowState.Maximized;
                window.ResizeMode = ResizeMode.NoResize;
                _isFullscreen = true;
                FullScreenIcon.Data = (Geometry)FindResource("FullScreeenRestoreIconData");
            }
            else
            {
                window.WindowStyle = WindowStyle.SingleBorderWindow;
                window.WindowState = WindowState.Normal;
                window.ResizeMode = ResizeMode.CanResize;
                _isFullscreen = false;
                FullScreenIcon.Data = (Geometry)FindResource("FullScreeenIconData");
            }
            window.Topmost = _isFullscreen;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            var window = System.Windows.Window.GetWindow(this);
            if (window?.DataContext is MainViewModel mvm)
            {
                if (VM?.CurrentEpisodeId.StartsWith("series") == true) {
                    mvm.ShowSeriesCommand.Execute(this);
                }
                if (VM?.CurrentEpisodeId.StartsWith("movie") == true)
                {
                    mvm.ShowMoviesCommand.Execute(this);
                }
            }
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            var window = System.Windows.Window.GetWindow(this);
            if (window?.DataContext is MainViewModel mvm)
            {
                if (VM?.CurrentEpisodeId.StartsWith("series") == true)
                {
                    Cursor = Cursors.Wait;
                    try
                    {
                        VideoPlayer.Pause();
                        await mvm.PlayNextEpisode();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error playing next episode: " + ex.Message);
                    }
                    finally
                    {
                        Cursor = Cursors.Arrow;
                    }
                }
            }

        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Space))
            {
                PlayButton_Click(sender, null);
            }
        }
    }
}