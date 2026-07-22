using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Intrinsics.Arm;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using XtreamIPTV.Models;
using XtreamIPTV.ViewModels;

namespace XtreamIPTV.Views
{
    public partial class SeriesView : UserControl
    {
        private static DateTime scrollTime = DateTime.Now;
        private bool isSimiliar = false;

        public SeriesView()
        {
            InitializeComponent();

            List<string> decades = ["ALL"];
            for (int dec = 2020; dec >= 1900; dec -= 10)
            {
                decades.Add(dec.ToString());
            }
            decades.ForEach(d =>
            {
                Button btn = new() { Content = d, FontSize = 20 };
                btn.Click += delegate
                {
                    var scrollViewer = GetScrollViewer(SeriesListBox);
                    scrollViewer?.ScrollToTop();
                    DecadeTogglePopupButton.IsChecked = false;
                    if (DataContext is not SeriesViewModel vm) return;
                    foreach (var child in DecadePanel.Children)
                    {
                        if (child is not Button cbtn) continue;
                        cbtn.FontWeight = FontWeights.Normal;
                    }
                    btn.FontWeight = FontWeights.Bold;
                    vm.DecadeFilter = d == "ALL" ? 0 : int.Parse(d);
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
                    var scrollViewer = GetScrollViewer(SeriesListBox);
                    scrollViewer?.ScrollToTop();
                    LanguageTogglePopupButton.IsChecked = false;
                    foreach (var child in LanguagePanel.Children)
                    {
                        if (child is not Button cbtn) continue;
                        cbtn.FontWeight = FontWeights.Normal;
                    }
                    btn.FontWeight = FontWeights.Bold;
                    if (DataContext is not SeriesViewModel vm) return;
                    vm.LanguageFilter = d == "ALL" ? string.Empty : d;
                };
                //btn.Click += DecadeButton_Click;
                LanguagePanel.Children.Add(btn);
            });


            Loaded += async (_, _) =>
            {
                if (DataContext is SeriesViewModel vm)
                {
                    var window = System.Windows.Window.GetWindow(this);
                    if (window?.DataContext is MainViewModel mvm)
                    {
                        mvm.Title = "XtreamIPTV Series";
                    }
                    await vm.LoadCategoriesAsync();
                    await vm.LoadSeriesAsync();

                    if (AgePanel.Children.Count == 0)
                    {
                        foreach (var d in vm.AgeRatings)
                        {
                            Button btn = new() { Content = d, FontSize = 20 };
                            btn.Click += delegate
                            {
                                var scrollViewer = GetScrollViewer(SeriesListBox);
                                scrollViewer?.ScrollToTop();
                                AgeTogglePopupButton.IsChecked = false;
                                foreach (var child in AgePanel.Children)
                                {
                                    if (child is not Button cbtn) continue;
                                    cbtn.FontWeight = FontWeights.Normal;
                                }
                                btn.FontWeight = FontWeights.Bold;
                                if (DataContext is not SeriesViewModel vm) return;
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

                    //if (vm.SelectedSeries == null) return;
                    //if (vm.SelectedEpisode == null) return;
                    //ShowEpisodeControls(true);
                }
            };
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
            if (DataContext is not SeriesViewModel vm) return;
            RemoveHistButton.Visibility = vm?.SelectedCategory?.Id == -3 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SeriesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not SeriesViewModel vm) return;

            if (vm.SelectedSeries != null)
            {
                FavoriteButton.Content = vm.IsFavorite(vm.SelectedSeries) ? "*Favorite" : "Favorite";
            }
        }

        private void SeriesListBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0) // Check if vertical scrolling occurred
            {
                if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight)
                {
                    if (DataContext is SeriesViewModel vm)
                    {
                        if (vm.FilteredSeriesCount <= vm.FilteredSeries.Count) return;
                        if ((DateTime.Now - scrollTime).TotalMilliseconds < 50) return; // prevent multiple triggers in short time
                        Debug.WriteLine("Loading more data!");

                        vm.ScrollSeries();
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
                SimiliarButton.Visibility = Visibility.Hidden;
                //Episodes Buttons
                SeasonsScrollView.Visibility = Visibility.Visible;
                PlayButton.Visibility = Visibility.Visible;
                PlayButton2.Visibility = Visibility.Visible;
                BackButton.Visibility = Visibility.Visible;
                episodePlot.Visibility = Visibility.Visible;
                episodeTitle.Visibility = Visibility.Visible;
                episodeReleaseDate.Visibility = Visibility.Visible;
            }
            else
            {
                //Series Buttons
                SeriesListBox.Visibility = Visibility.Visible;
                FavoriteButton.Visibility = Visibility.Visible;
                SimiliarButton.Visibility = Visibility.Visible;
                //Episodes Buttons
                SeasonsScrollView.Visibility = Visibility.Hidden;
                PlayButton.Visibility = Visibility.Hidden;
                PlayButton2.Visibility = Visibility.Hidden;
                BackButton.Visibility = Visibility.Hidden;
                episodeTitle.Visibility = Visibility.Hidden;
                episodePlot.Visibility = Visibility.Hidden;
                episodeReleaseDate.Visibility = Visibility.Hidden;
            }
        }

        private void Episodes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not SeriesViewModel svm) return;
            if (sender is not ListBox seasonLB) return;

            svm.SelectedEpisode = (sender as ListBox)?.SelectedItem as Episode;
            //foreach (var item in SeasonsItemControl.Items)
            //{
            //    if (item is not ListBox itemLB) continue;
            //    if (itemLB.SelectedItem == svm.SelectedEpisode) continue;
            //    itemLB.SelectedItem = null;
            //}
            //if (svm.SelectedEpisode == null) return;
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SeriesViewModel svm) return;

            if (svm.SelectedEpisode == null) return;

            // Bubble up to MainViewModel via DataContext of Window
            var window = System.Windows.Window.GetWindow(this);
            if (window?.DataContext is MainViewModel mvm)
            {
                Cursor = Cursors.Wait;

                try
                {
                    await mvm.PlayEpisode(svm.SelectedEpisode);
                }
                finally
                {
                    // Revert cursor to default after the operation is complete
                    Cursor = Cursors.Arrow;
                }
            }

        }
        private async void PlayButton2_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SeriesViewModel svm) return;

            if (svm.SelectedEpisode == null) return;

            var window = System.Windows.Window.GetWindow(this);
            if (window?.DataContext is MainViewModel mvm)
            {
                Cursor = Cursors.Wait;

                try
                {
                    await mvm.PlayEpisode(svm.SelectedEpisode, true);
                }
                finally
                {
                    // Revert cursor to default after the operation is complete
                    Cursor = Cursors.Arrow;
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var scrollViewer = GetScrollViewer(SeriesListBox);
            scrollViewer?.ScrollToTop();
            scrollTime = DateTime.Now;
            if (DataContext is not SeriesViewModel seriesvm) return;

            if (seriesvm.GoBackInHistory())
                return;
            ShowEpisodeControls(false);
            //seriesvm.Sort = seriesvm.Sort;
            BackButton.Visibility = Visibility.Hidden;
        }

        private void DefaultSort_Click(object sender, RoutedEventArgs e)
        {
            UpdateSort(Sort.Default);
        }

        private void DateSort_Click(object sender, RoutedEventArgs e)
        {
            UpdateSort(Sort.Date);
        }

        private void PopularitySort_Click(object sender, RoutedEventArgs e)
        {
            UpdateSort(Sort.Popularity);
        }

        private void NameSort_Click(object sender, RoutedEventArgs e)
        {
            UpdateSort(Sort.Name);
        }

        private void UpdateSort(Sort order)
        {
            var scrollViewer = GetScrollViewer(SeriesListBox);
            scrollViewer?.ScrollToTop();
            SortTogglePopupButton.IsChecked = false;
            if (DataContext is not SeriesViewModel movievm) return;
            movievm.Sort = order;
            foreach (var child in SortPanel.Children)
            {
                if (child is not Button cbtn) continue;
                if (cbtn.Content.ToString() == order.ToString())
                    cbtn.FontWeight = FontWeights.Bold;
                else
                    cbtn.FontWeight = FontWeights.Normal;
            }
        }

        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SeriesViewModel vm) return;
            vm.ToggleFavorite();
            if (vm.SelectedSeries != null)
                FavoriteButton.Content = vm.IsFavorite(vm.SelectedSeries) ? "*Favorite" : "Favorite";
        }

        private void RemoveHistButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SeriesViewModel svm) return;

            if (svm.SelectedSeries == null) return;

            var window = System.Windows.Window.GetWindow(this);
            if (window?.DataContext is MainViewModel mvm)
            {
                mvm.RemoveLastWatched(svm.SelectedSeries);
            }
        }

        private void SimiliarButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SeriesViewModel vm) return;
            var scrollViewer = GetScrollViewer(SeriesListBox);
            scrollViewer?.ScrollToTop();
            vm.GetSimiliarSeries();
            BackButton.Visibility = Visibility.Visible;
            scrollTime = DateTime.Now;
            isSimiliar = true;
        }
        private void DirectorHyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            if (DataContext is not SeriesViewModel vm) return;

            var temp = e.Uri.ToString().Split(":");
            if (temp[0] == "director")
            {
                var scrollViewer = GetScrollViewer(SeriesListBox);
                scrollViewer?.ScrollToTop();
                vm.GetSeriesByDirector(temp[1]);
                BackButton.Visibility = Visibility.Visible;
                scrollTime = DateTime.Now;
            }
        }

        private void ActorHyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            if (DataContext is not SeriesViewModel vm) return;

            var temp = e.Uri.ToString().Split(":");
            if (temp[0] == "actor")
            {
                var scrollViewer = GetScrollViewer(SeriesListBox);
                scrollViewer?.ScrollToTop();
                vm.GetSeriesByActor(temp[1]);
                BackButton.Visibility = Visibility.Visible;
                scrollTime = DateTime.Now;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            var window = System.Windows.Window.GetWindow(this);
            if (window?.DataContext is MainViewModel mvm)
            {
                Cursor = Cursors.Wait;

                try
                {
                    var series = mvm.SeriesVM.SelectedSeries;
                    if (series != null)
                    mvm.NavigateToUri($"series-{series.Id}", series.Title, e.Uri);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error playing series: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    // Revert cursor to default after the operation is complete
                    Cursor = Cursors.Arrow;
                }
            }
        }
    }
}