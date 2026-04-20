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
                UpdatePositionDisplay();
            };

            _hideTimer.Interval = TimeSpan.FromSeconds(3); // Hide after 3 seconds
            _hideTimer.Tick += (_, _) =>
            {
                // Hide the control
                VideoControls.Visibility = Visibility.Collapsed;
                PositionGrid.Visibility = Visibility.Collapsed;
                TitleText.Visibility = Visibility.Collapsed;
                Cursor = Cursors.None;
                _hideTimer.Stop();
            }; 
            
            _hideTimer.Start();
            
            VideoPlayer.MediaFailed += (o, args) => {
                if (args.ErrorException.Message.ToString() == "0xC00D11D2")
                {
                    VM?.ErrorMessage = $"Access Denied while trying to play {VM?.Title}";
                    return;
                }
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
                    if (duration.TotalMinutes < 30)
                    {
                        positionSlider.SmallChange = 10; //10 Seconds
                        positionSlider.LargeChange = 30; //30 Seconds
                    }
                    else
                    if (duration.TotalMinutes < 60)
                    {
                        positionSlider.SmallChange = 10; //10 Seconds
                        positionSlider.LargeChange = 60; //1 Minute
                    } 
                    else
                    {
                        positionSlider.SmallChange = 30; //30 Seconds
                        positionSlider.LargeChange = 300; //5 Minutes
                    }
                }
                ResolutionText.Text = $"{VideoPlayer.NaturalVideoWidth}x{VideoPlayer.NaturalVideoHeight}";

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
                        NextButton.Visibility = Visibility.Collapsed;
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

        private bool UpdatePositionDisplay()
        {
            VM?.SavePosition(VideoPlayer.Position.TotalSeconds);
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                return false; // Don't update position while user is dragging the slider

            if (VideoPlayer.Position.TotalSeconds > positionSlider.Maximum)
            {
                return false;
                //txtTotalDuration.Text = (TimeSpan.FromSeconds(VideoPlayer.Position.TotalSeconds)).ToString(@"hh\:mm\:ss");
                //positionSlider.Maximum = VideoPlayer.Position.TotalSeconds;
            }

            positionSlider.Value = VideoPlayer.Position.TotalSeconds;
            return true;
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

        public void Button_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Position -= TimeSpan.FromSeconds(10);
            UpdatePositionDisplay();
        }

        public void Button_Click_1(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Position += TimeSpan.FromSeconds(10);
            UpdatePositionDisplay();
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

        private void positionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            txtCurrentPosition.Text = TimeSpan.FromSeconds(positionSlider.Value).ToString(@"hh\:mm\:ss");
        }

        private void ResolutionText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var window = System.Windows.Window.GetWindow(this);
            //var topBorderHeight = window.Height - window.RenderSize.Height;
            var topBorderHeight = 32;
            window.Width = VideoPlayer.NaturalVideoWidth;
            window.Height = VideoPlayer.NaturalVideoHeight + topBorderHeight;
        }
    }
}