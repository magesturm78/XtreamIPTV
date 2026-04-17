using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
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
using XtreamIPTV.Models;
using XtreamIPTV.ViewModels;

namespace XtreamIPTV.Views
{
    /// <summary>
    /// Interaction logic for SearchView.xaml
    /// </summary>
    public partial class SearchView : UserControl
    {
        public SearchView()
        {
            InitializeComponent();
            Loaded += async (_, _) =>
            {
                var window = System.Windows.Window.GetWindow(this);
                if (window?.DataContext is MainViewModel mvm)
                {
                    mvm.Title = "XtreamIPTV Search";
                }

                if (DataContext is not SearchViewModel vm) return;

                //MoviesListBox.ItemsSource = vm.MoviesVM.SearchedMovies;
                //SeriesListBox.ItemsSource = vm.SeriesVM.SearchedSeries;
            };
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private async void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is not SearchViewModel vm) return;
                var window = System.Windows.Window.GetWindow(this);
                if (window?.DataContext is MainViewModel mvm)
                {
                    Cursor = Cursors.Wait;

                    try
                    {
                        await mvm.Search();
                        //MoviesListBox.ItemsSource = vm.MoviesVM.SearchedMovies;
                        //SeriesListBox.ItemsSource = vm.SeriesVM.SearchedSeries;
                    }
                    finally
                    {
                        // Revert cursor to default after the operation is complete
                        Cursor = Cursors.Arrow;
                    }
                }
            }
        }

        private async void PlayMovieButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SearchViewModel vm) return;

            if (MoviesListBox.SelectedItem is not Movie m) return;
            var window = System.Windows.Window.GetWindow(this);
            if (window?.DataContext is MainViewModel mvm)
            {
                Cursor = Cursors.Wait;

                try
                {
                    await mvm.PlayMovie(m);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error playing movie: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    // Revert cursor to default after the operation is complete
                    Cursor = Cursors.Arrow;
                }
            }

        }

        private void MoviesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not SearchViewModel vm) return;

            if (vm.MoviesVM.SelectedMovie != null)
                FavoriteMovieButton.Content = vm.MoviesVM.IsFavorite(vm.MoviesVM.SelectedMovie) ? "*Favorite" : "Favorite";
        }

        private void SeriesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not SearchViewModel vm) return;

            if (vm.SeriesVM.SelectedSeries != null)
                FavoriteSeriesButton.Content = vm.SeriesVM.IsFavorite(vm.SeriesVM.SelectedSeries) ? "*Favorite" : "Favorite";
        }

        private void FavoriteMovieButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SearchViewModel vm) return;
            vm.MoviesVM.ToggleFavorite();
            if (vm.MoviesVM.SelectedMovie != null)
                FavoriteMovieButton.Content = vm.MoviesVM.IsFavorite(vm.MoviesVM.SelectedMovie) ? "*Favorite" : "Favorite";

        }

        private void FavoriteSeriesButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SearchViewModel vm) return;
            vm.SeriesVM.ToggleFavorite();
            if (vm.SeriesVM.SelectedSeries != null)
                FavoriteSeriesButton.Content = vm.SeriesVM.IsFavorite(vm.SeriesVM.SelectedSeries) ? "*Favorite" : "Favorite";

        }
    }
}
