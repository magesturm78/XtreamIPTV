using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using XtreamIPTV.Models;
using XtreamIPTV.ViewModels;

namespace XtreamIPTV.Views
{
    public partial class SeriesView : UserControl
    {
        private static DateTime scrollTime = DateTime.Now;

        public SeriesView()
        {
            InitializeComponent();

            Loaded += async (_, _) =>
            {
                if (DataContext is SeriesViewModel vm)
                {
                    await vm.LoadCategoriesAsync();
                    await vm.LoadSeriesAsync();
                }
            };

            // Optional: double-click episode to play
            //this.AddHandler(ListBox.MouseDoubleClickEvent, new System.Windows.Input.MouseButtonEventHandler(OnDoubleClick), true);
        }

        private void OnDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is not SeriesViewModel svm) return;

            if (svm.SelectedEpisode == null) return;

            // Bubble up to MainViewModel via DataContext of Window
            var window = System.Windows.Window.GetWindow(this);
            if (window?.DataContext is MainViewModel mvm)
            {
                mvm.PlayEpisode(svm.SelectedEpisode);
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
            var scrollViewer = GetScrollViewer(SeriesListBox);
            scrollViewer?.ScrollToTop();
            ShowEpisodeControls(false);
        }

        private void SeriesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not SeriesViewModel vm) return;

            if (vm.SelectedSeries != null)
                FavoriteButton.Content = vm.IsFavorite(vm.SelectedSeries) ? "*Favorite" : "Favorite";
        }

        private void SeriesListBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0) // Check if vertical scrolling occurred
            {
                if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight)
                {
                    if (DataContext is SeriesViewModel vm)
                    {
                        if (vm.AllSeries.Count <= vm.FilteredSeries.Count) return;
                        if ((DateTime.Now - scrollTime).TotalMilliseconds < 50) return; // prevent multiple triggers in short time
                        Debug.WriteLine("Loading more data!");

                        vm.ScrollSeries();
                        var window = Window.GetWindow(this);
                        window.Title = $"XtreamIPTV Player: Series {vm.FilteredSeries.Count}";
                    }
                    scrollTime = DateTime.Now;
                }
            }

        }

        private void EpisodesButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not SeriesViewModel svm) return;

            if (svm.SelectedSeries == null) return;
            ShowEpisodeControls(true);
        }

        private void ShowEpisodeControls(bool show)
        {
            if (show)
            {
                //Series Buttons
                SeriesListBox.Visibility = Visibility.Hidden;
                FavoriteButton.Visibility = Visibility.Hidden;
                //Episodes Buttons
                SeasonsListBox.Visibility = Visibility.Visible;
                PlayButton.Visibility = Visibility.Visible;
                BackButton.Visibility = Visibility.Visible;
            }
            else
            {
                //Series Buttons
                SeriesListBox.Visibility = Visibility.Visible;
                FavoriteButton.Visibility = Visibility.Visible;
                //Episodes Buttons
                SeasonsListBox.Visibility = Visibility.Hidden;
                PlayButton.Visibility = Visibility.Hidden;
                BackButton.Visibility = Visibility.Hidden;
            }
        }

        private void Episodes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not SeriesViewModel svm) return;

            svm.SelectedEpisode = (sender as ListBox)?.SelectedItem as Episode;
            if (svm.SelectedEpisode == null) return;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SeriesViewModel svm) return;

            if (svm.SelectedEpisode == null) return;

            // Bubble up to MainViewModel via DataContext of Window
            var window = System.Windows.Window.GetWindow(this);
            if (window?.DataContext is MainViewModel mvm)
            {
                mvm.PlayEpisode(svm.SelectedEpisode);
            }

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ShowEpisodeControls(false);

            if (DataContext is not SeriesViewModel svm) return;

            svm.SelectedEpisode = null;
            svm.SelectedSeason = null;

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
            var scrollViewer = GetScrollViewer(SeriesListBox);
            scrollViewer?.ScrollToTop();
            TogglePopupButton.IsChecked = false;
            if (DataContext is not SeriesViewModel movievm) return;
            movievm.Sort = order;

        }

        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SeriesViewModel vm) return;
            vm.ToggleFavorite();
            if (vm.SelectedSeries != null)
                FavoriteButton.Content = vm.IsFavorite(vm.SelectedSeries) ? "*Favorite" : "Favorite";
        }
    }
}