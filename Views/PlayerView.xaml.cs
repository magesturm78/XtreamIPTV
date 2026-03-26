using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using XtreamIPTV.ViewModels;

namespace XtreamIPTV.Views
{
    public partial class PlayerView : UserControl
    {
        private readonly DispatcherTimer _timer = new();
        private PlayerViewModel? VM => DataContext as PlayerViewModel;

        public PlayerView()
        {
            InitializeComponent();

            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (_, _) =>
            {
                VM?.SavePosition(VideoPlayer.Position.TotalSeconds);
                txtCurrentPosition.Text = VideoPlayer.Position.ToString(@"hh\:mm\:ss");
                positionSlider.Value = VideoPlayer.Position.TotalSeconds;
            };

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
                }
                _timer.Start();
            };

            Loaded += (_, _) =>
            {
                VideoPlayer.Play();
            };

            Unloaded += (_, _) =>
            {
                _timer.Stop();
                VM?.SavePosition(VideoPlayer.Position.TotalSeconds);
            };
        }

        private void VideoPlayer_MediaEnded(object sender, System.Windows.RoutedEventArgs e)
        {
            VM?.SavePosition(0);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Play();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Pause();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Stop();
            VideoPlayer.Play();
        }

        private bool _isFullscreen = false;
        
        private void Media_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                var window = Window.GetWindow(this);

                if (window.WindowStyle != WindowStyle.None)
                {
                    window.WindowStyle = WindowStyle.None;
                    window.WindowState = WindowState.Maximized;
                    window.ResizeMode = ResizeMode.NoResize;
                    _isFullscreen = true;
                }
                else
                {
                    window.WindowStyle = WindowStyle.SingleBorderWindow;
                    window.WindowState = WindowState.Normal;
                    window.ResizeMode = ResizeMode.CanResize;
                    _isFullscreen = false;
                }
                window.Topmost = _isFullscreen;
            }
        }

        private void positionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Set the media position when the user moves the slider (only if they are interacting)
            // You might need a flag to differentiate user interaction from timer updates
            if (VideoPlayer.NaturalDuration.HasTimeSpan)
            {
                TimeSpan newPosition = TimeSpan.FromSeconds(positionSlider.Value);
                VideoPlayer.Position = newPosition;
            }
        }
    }
}