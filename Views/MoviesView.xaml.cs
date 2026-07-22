using System;
<<<<<<< HEAD
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Intrinsics.Arm;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
=======
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
using System.Windows.Media;
using System.Windows.Media.Imaging;
using XtreamIPTV.Models;
using XtreamIPTV.ViewModels;
<<<<<<< HEAD
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
=======
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4

namespace XtreamIPTV.Views
{
    public partial class MoviesView : UserControl
    {
        private static DateTime scrollTime = DateTime.Now;

        public MoviesView()
        {
            InitializeComponent();

<<<<<<< HEAD
            List<string> decades = ["ALL"];
            for (int dec = 2020; dec >= 1900; dec -=10 )
            {
                decades.Add(dec.ToString());
            }
            decades.ForEach(d =>
            {
                Button btn = new() { Content = d, FontSize = 20 };
                btn.Click += delegate
                {
                    var scrollViewer = GetScrollViewer(MoviesListBox);
                    scrollViewer?.ScrollToTop();
                    DecadeTogglePopupButton.IsChecked = false;
                    foreach (var child in DecadePanel.Children)
                    {
                        if (child is not Button cbtn) continue;
                        cbtn.FontWeight = FontWeights.Normal;
                    }
                    btn.FontWeight = FontWeights.Bold;
                    if (DataContext is not MoviesViewModel vm) return;
                    vm.DecadeFilter = d == "ALL" ? 0 : int.Parse(d);
                    BackButton.Visibility = Visibility.Hidden;
                };
                DecadePanel.Children.Add(btn);
            });
            List<string> languages = ["ALL","English","French","Spanish","Japanese","German","Italian","Russian","Mandarin","Portuguese","Korean","Hindi","Cantonese",
                                      "Turkish","Dutch","Tamil","Malayalam","Swedish","Tagalog","Polish","Arabic","Czech","Indonesian","Telugu","Danish","Greek",
                                      "Thai","Persian","Finnish"];
            languages.ForEach(d =>
            {
                Button btn = new() { Content = d, FontSize = 20 };
                btn.Click += delegate
                {
                    var scrollViewer = GetScrollViewer(MoviesListBox);
                    scrollViewer?.ScrollToTop();
                    LanguageTogglePopupButton.IsChecked = false;
                    foreach (var child in LanguagePanel.Children)
                    {
                        if (child is not Button cbtn) continue;
                        cbtn.FontWeight = FontWeights.Normal;
                    }
                    btn.FontWeight = FontWeights.Bold;
                    if (DataContext is not MoviesViewModel vm) return;
                    vm.LanguageFilter = d == "ALL" ? string.Empty : d;
                    BackButton.Visibility = Visibility.Hidden;
                };
                LanguagePanel.Children.Add(btn);
            });


=======
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
            Loaded += async (_, _) =>
            {
                if (DataContext is MoviesViewModel vm)
                {
<<<<<<< HEAD
                    var window = System.Windows.Window.GetWindow(this);
                    if (window?.DataContext is MainViewModel mvm)
                    {
                        mvm.Title = "XtreamIPTV Movies";
                    }
                    await vm.LoadMovieCategoriesAsync();
                    await vm.LoadMoviesAsync();

                    if (AgePanel.Children.Count == 0)
                    {
                        foreach (var d in vm.AgeRatings)
                        {
                            Button btn = new() { Content = d, FontSize = 20 };
                            btn.Click += delegate
                            {
                                var scrollViewer = GetScrollViewer(MoviesListBox);
                                scrollViewer?.ScrollToTop();
                                AgeTogglePopupButton.IsChecked = false;
                                foreach (var child in AgePanel.Children)
                                {
                                    if (child is not Button cbtn) continue;
                                    cbtn.FontWeight = FontWeights.Normal;
                                }
                                btn.FontWeight = FontWeights.Bold;
                                if (DataContext is not MoviesViewModel vm) return;
                                vm.AgeRatingFilter = d == "ALL" ? string.Empty : d;
                                BackButton.Visibility = Visibility.Hidden;
                            };
                            AgePanel.Children.Add(btn);
                        }
                    }
                    foreach (var child in SortPanel.Children)
                    {
                        if (child is not Button cbtn) continue;
                        if (cbtn.Content.ToString() == vm.Sort.ToString())
                            cbtn.FontWeight = FontWeights.Bold;
                        else
                            cbtn.FontWeight = FontWeights.Normal;
                    }

                    foreach (var child in DecadePanel.Children)
                    {
                        if (child is not Button cbtn) continue;
                        if (cbtn.Content.ToString() == vm.DecadeFilter.ToString() || (vm.DecadeFilter == 0 && cbtn.Content.ToString() == "ALL"))
                            cbtn.FontWeight = FontWeights.Bold;
                        else
                            cbtn.FontWeight = FontWeights.Normal;
                    }

                    foreach (var child in LanguagePanel.Children)
                    {
                        if (child is not Button cbtn) continue;
                        if (cbtn.Content.ToString() == vm.LanguageFilter.ToString() || (vm.LanguageFilter == string.Empty && cbtn.Content.ToString() == "ALL"))
                            cbtn.FontWeight = FontWeights.Bold;
                        else
                            cbtn.FontWeight = FontWeights.Normal;
                    }
=======
                    await vm.LoadMovieCategoriesAsync();
                    await vm.LoadMoviesAsync();
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
                }
            };
        }

<<<<<<< HEAD
        private async void Button_Click(object sender, System.Windows.RoutedEventArgs e)
=======
        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
        {
            if (DataContext is not MoviesViewModel movievm) return;

            if (movievm.SelectedMovie == null) return;

            // Bubble up to MainViewModel via DataContext of Window
            var window = System.Windows.Window.GetWindow(this);
            if (window?.DataContext is MainViewModel mvm)
            {
<<<<<<< HEAD
                Cursor = Cursors.Wait;

                try
                {
                    await mvm.PlayMovie(movievm.SelectedMovie);
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
=======
                mvm.PlayMovie(movievm.SelectedMovie);
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
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
<<<<<<< HEAD
                {
                    if (vm.MovePlayTime > 0)
                    {
                        var ts = TimeSpan.FromSeconds(vm.MovePlayTime);
                        PlayButton.Content = $"Resume {ts.Hours:0}:{ts.Minutes:00}";
                        PlayButton.FontSize = 20;
                    }
                    else
                    {
                        PlayButton.Content = $"Play";
                        PlayButton.FontSize = 25;
                    }
                    FavoriteButton.Content = vm.IsFavorite(vm.SelectedMovie) ? "*Favorite" : "Favorite";
                }
=======
                    FavoriteButton.Content = vm.IsFavorite(vm.SelectedMovie) ? "*Favorite" : "Favorite";
                var window = Window.GetWindow(this);
                if (window != null)
                    window.Title = $"XtreamIPTV Player: Movies {vm.FilteredMovies.Count}";
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
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
<<<<<<< HEAD
                        if (vm.FilteredMovies.Count >= vm.FilteredMoviesCount) return;
                        if ((DateTime.Now - scrollTime).TotalMilliseconds < 50) return; // prevent multiple triggers in short time
                        Debug.WriteLine("Loading more data!");

                        vm.ScrollMovies();
=======
                        if (vm.AllMovies.Count < vm.MovieCount) return;
                        if (scrollTime != null && (DateTime.Now - scrollTime).TotalMilliseconds < 50) return; // prevent multiple triggers in short time
                        Debug.WriteLine("Loading more data!");

                        vm.ScrollMovies();
                        var window = Window.GetWindow(this);
                        window.Title = $"XtreamIPTV Player: Movies {vm.FilteredMovies.Count}";
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
                    }
                    scrollTime = DateTime.Now;
                }
            }
        }

        private void ListBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            var scrollViewer = GetScrollViewer(MoviesListBox);
            scrollViewer?.ScrollToTop();
<<<<<<< HEAD
            if (DataContext is not MoviesViewModel vm) return;
            RemoveHistButton.Visibility = vm?.SelectedCategory?.Id == -3 ? Visibility.Visible : Visibility.Collapsed;
            BackButton.Visibility = Visibility.Hidden;
            
        }

        private async void PlayButton2_Click(object sender, RoutedEventArgs e)
=======
        }

        private void PlayButton2_Click(object sender, RoutedEventArgs e)
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
        {
            if (DataContext is not MoviesViewModel movievm) return;

            if (movievm.SelectedMovie == null) return;

<<<<<<< HEAD
            // Bubble up to MainViewModel via DataContext of Window
            var window = System.Windows.Window.GetWindow(this);
            if (window?.DataContext is MainViewModel mvm)
            {
                Cursor = Cursors.Wait;

                try
                {
                    await mvm.PlayMovie(movievm.SelectedMovie, true);
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
=======
            string exe = "C:\\Program Files\\MPC-HC\\mpc-hc64.exe";
            string arguments = $"\"{movievm.SelectedMovie.StreamUrl}\"";
            Process.Start(exe, arguments);
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
        }

        private void DefaultSort_Click(object sender, RoutedEventArgs e)
        {
<<<<<<< HEAD
            UpdateSort(Sort.Default);
=======
            UpdateSort(Sort.None);
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
        }

        private void DateSort_Click(object sender, RoutedEventArgs e)
        {
<<<<<<< HEAD
            UpdateSort(Sort.Date);
=======
            UpdateSort(Sort.ReleaseDate);
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
        }

        private void PopularitySort_Click(object sender, RoutedEventArgs e)
        {
<<<<<<< HEAD
            UpdateSort(Sort.Popularity);
        }

        private void NameSort_Click(object sender, RoutedEventArgs e)
        {
            UpdateSort(Sort.Name);
=======
            UpdateSort(Sort.Rating);
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
        }

        private void UpdateSort(Sort order)
        {
            var scrollViewer = GetScrollViewer(MoviesListBox);
            scrollViewer?.ScrollToTop();
<<<<<<< HEAD
            SortTogglePopupButton.IsChecked = false;
            if (DataContext is not MoviesViewModel movievm) return;
            movievm.Sort = order;
            foreach (var child in SortPanel.Children)
            {
                if (child is not Button cbtn) continue;
                if (cbtn.Content.ToString() == order.ToString())
                    cbtn.FontWeight = FontWeights.Bold;
                else
                    cbtn.FontWeight = FontWeights.Normal;
            }
            BackButton.Visibility = Visibility.Hidden;
=======
            TogglePopupButton.IsChecked = false;
            if (DataContext is not MoviesViewModel movievm) return;
            movievm.Sort = order;

>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
        }

        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MoviesViewModel vm) return;
            vm.ToggleFavorite();
            if (vm.SelectedMovie != null)
                FavoriteButton.Content = vm.IsFavorite(vm.SelectedMovie) ? "*Favorite" : "Favorite";
        }
<<<<<<< HEAD

        private void SimiliarButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MoviesViewModel vm) return;
            var scrollViewer = GetScrollViewer(MoviesListBox);
            scrollViewer?.ScrollToTop();
            vm.GetSimiliarMovies();
            BackButton.Visibility = Visibility.Visible;
            scrollTime = DateTime.Now;
        }

        private void RemoveHistButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MoviesViewModel vm) return;

            if (vm.SelectedMovie == null) return;

            var window = System.Windows.Window.GetWindow(this);
            if (window?.DataContext is MainViewModel mvm)
            {
                mvm.RemoveLastWatched(vm.SelectedMovie);
            }

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var scrollViewer = GetScrollViewer(MoviesListBox);
            scrollViewer?.ScrollToTop();
            scrollTime = DateTime.Now;
            if (DataContext is not MoviesViewModel movievm) return;

            if (movievm.GoBackInHistory()) 
                return;
            movievm.Sort = movievm.Sort;
            BackButton.Visibility = Visibility.Hidden;
        }

        private void DirectorHyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            if (DataContext is not MoviesViewModel vm) return;

            var temp = e.Uri.ToString().Split(":");
            if (temp[0] == "director")
            {
                var scrollViewer = GetScrollViewer(MoviesListBox);
                scrollViewer?.ScrollToTop();
                vm.GetMoviesByDirector(temp[1]);
                BackButton.Visibility = Visibility.Visible;
                scrollTime = DateTime.Now;
            }
        }

        private void ActorHyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            if (DataContext is not MoviesViewModel vm) return;

            var temp = e.Uri.ToString().Split(":");
            if (temp[0] == "actor")
            {
                var scrollViewer = GetScrollViewer(MoviesListBox);
                scrollViewer?.ScrollToTop();
                vm.GetMoviesByActor(temp[1]);
                BackButton.Visibility = Visibility.Visible;
                scrollTime = DateTime.Now;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            // Bubble up to MainViewModel via DataContext of Window
            var window = System.Windows.Window.GetWindow(this);
            if (window?.DataContext is MainViewModel mvm)
            {
                Cursor = Cursors.Wait;
                try
                {
                    var movie = mvm.MoviesVM.SelectedMovie;
                    mvm.NavigateToUri($"movie-{movie.Id}", movie.Title, e.Uri);
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
=======
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
    }
}