using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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
using XtreamIPTV.ViewModels;

namespace XtreamIPTV.Views
{
    /// <summary>
    /// Interaction logic for LiveView.xaml
    /// </summary>
    public partial class LiveView : UserControl
    {
        private readonly DispatcherTimer _hideTimer = new();

        public LiveView()
        {
            InitializeComponent();

            Loaded += async (_, _) =>
            {
                if (DataContext is LiveViewModel vm)
                {
                    var window = System.Windows.Window.GetWindow(this);
                    if (window?.DataContext is MainViewModel mvm)
                    {
                        mvm.Title = "XtreamIPTV Live";
                    }
                    await vm.LoadLiveCategoriesAsync();
                    await vm.LoadLiveAsync();
                }
            };

            VideoPlayer.MediaFailed += (o, args) => {
                ResolutionText.Text = args.ErrorException.Message;
                Debug.Print(args.ErrorException.Message);
            };

            VideoPlayer.MediaOpened += (o, args) =>
            {
                VideoControls.Visibility = Visibility.Visible;
                ResolutionText.Text = $"{VideoPlayer.NaturalVideoWidth}x{VideoPlayer.NaturalVideoHeight}";
                _hideTimer.Start();
            };

            _hideTimer.Interval = TimeSpan.FromSeconds(3); // Hide after 3 seconds
            _hideTimer.Tick += (_, _) =>
            {
                // Hide the control
                VideoControls.Visibility = Visibility.Collapsed;
                _hideTimer.Stop();
            };
            _hideTimer.Start();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not LiveViewModel vm) return;
            vm.ApplyFilters();
        }

        private void LiveListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is LiveViewModel vm)
            {
                var window = System.Windows.Window.GetWindow(this);
                if (window?.DataContext is MainViewModel mvm)
                {
                    mvm.Title = $"XtreamIPTV Live {vm.SelectedLive?.Name}";
                }
            }
            if (sender is ListBox listBox && listBox.Items.Count > 0)
            {
                _hideTimer.Stop();
                ResolutionText.Text = "Loading...";
                VideoPlayer.Play();
                VideoControls.Visibility = Visibility.Visible;
            }

        }

        private void LiveListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not LiveViewModel vm) return;
            var window = System.Windows.Window.GetWindow(this);
            if (window?.DataContext is MainViewModel mvm)
            {
                if (vm.SelectedLive == null)
                    return;
                mvm.PlayLive(vm.SelectedLive);
            }

        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not LiveViewModel vm) return;
            _hideTimer.Stop();
            ResolutionText.Text = "ReLoading...";
            VideoControls.Visibility = Visibility.Visible;
            vm.Refresh();
            //VideoPlayer.Play();
        }

        private void VideoPlayer_MouseMove(object sender, MouseEventArgs e)
        {
            VideoControls.Visibility = Visibility.Visible;
            _hideTimer.Stop();
            _hideTimer.Start();
        }
    }
}
