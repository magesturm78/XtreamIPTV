using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using XtreamIPTV.Models;
using XtreamIPTV.ViewModels;

namespace XtreamIPTV.Views
{
    public partial class MoviesView : UserControl
    {
        private static DateTime scrollTime = DateTime.Now;

        public MoviesView()
        {
            InitializeComponent();

            Loaded += async (_, _) =>
            {
                if (DataContext is MoviesViewModel vm)
                {
                    await vm.LoadMovieCategoriesAsync();
                    await vm.LoadMoviesAsync();
                }
            };
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not MoviesViewModel movievm) return;

            if (movievm.SelectedMovie == null) return;

            // Bubble up to MainViewModel via DataContext of Window
            var window = System.Windows.Window.GetWindow(this);
            if (window?.DataContext is MainViewModel mvm)
            {
                mvm.PlayMovie(movievm.SelectedMovie);
            }

        }

        private static ScrollViewer? GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer viewer) return viewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                ScrollViewer? viewerChild = GetScrollViewer(child);
                if (viewerChild != null) return viewerChild;
            }
            return null;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MoviesViewModel vm)
            {
                vm.GetSelectedMovieData();
                if (vm.SelectedMovie != null)
                    FavoriteButton.Content = vm.IsFavorite(vm.SelectedMovie) ? "*Favorite" : "Favorite";
                var window = Window.GetWindow(this);
                if (window != null)
                    window.Title = $"XtreamIPTV Player: Movies {vm.FilteredMovies.Count}";
            }
        }

        private void ListBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0) // Check if vertical scrolling occurred
            {
                if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight)
                {
                    if (DataContext is MoviesViewModel vm)
                    {
                        if (vm.AllMovies.Count < vm.MovieCount) return;
                        if (scrollTime != null && (DateTime.Now - scrollTime).TotalMilliseconds < 50) return; // prevent multiple triggers in short time
                        Debug.WriteLine("Loading more data!");

                        vm.ScrollMovies();
                        var window = Window.GetWindow(this);
                        window.Title = $"XtreamIPTV Player: Movies {vm.FilteredMovies.Count}";
                    }
                    scrollTime = DateTime.Now;
                }
            }
        }

        private void ListBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            var scrollViewer = GetScrollViewer(MoviesListBox);
            scrollViewer?.ScrollToTop();
        }

        private void PlayButton2_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MoviesViewModel movievm) return;

            if (movievm.SelectedMovie == null) return;

            string exe = "C:\\Program Files\\MPC-HC\\mpc-hc64.exe";
            string arguments = $"\"{movievm.SelectedMovie.StreamUrl}\"";
            Process.Start(exe, arguments);
        }

        private void DefaultSort_Click(object sender, RoutedEventArgs e)
        {
            UpdateSort(Sort.None);
        }

        private void DateSort_Click(object sender, RoutedEventArgs e)
        {
            UpdateSort(Sort.ReleaseDate);
        }

        private void PopularitySort_Click(object sender, RoutedEventArgs e)
        {
            UpdateSort(Sort.Rating);
        }

        private void UpdateSort(Sort order)
        {
            var scrollViewer = GetScrollViewer(MoviesListBox);
            scrollViewer?.ScrollToTop();
            TogglePopupButton.IsChecked = false;
            if (DataContext is not MoviesViewModel movievm) return;
            movievm.Sort = order;

        }

        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MoviesViewModel vm) return;
            vm.ToggleFavorite();
            if (vm.SelectedMovie != null)
                FavoriteButton.Content = vm.IsFavorite(vm.SelectedMovie) ? "*Favorite" : "Favorite";
        }
    }
}