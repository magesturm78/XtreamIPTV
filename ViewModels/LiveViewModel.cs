using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XtreamIPTV.Models;
using XtreamIPTV.Services;

namespace XtreamIPTV.ViewModels
{
    public class LiveViewModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly IIPTVService _xtream;

        private Category? _selectedCategory;
        private Live? _selectedLive;

        public ObservableCollection<Category> Categories { get; set; } = [];
        public ObservableCollection<Live> AllLive { get; set; } = [];
        public ObservableCollection<Live> FilteredLive { get; set; } = [];

        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                PropertyChanged?.Invoke(this, new(nameof(SelectedCategory)));
            }
        }

        public Live? SelectedLive
        {
            get => _selectedLive;
            set
            {
                if (value == null) return;
                _selectedLive = value;
                PropertyChanged?.Invoke(this, new(nameof(SelectedLive)));
            }
        }

        public LiveViewModel(IIPTVService xtream) 
        { 
            _xtream = xtream;
        }

        public async Task LoadLiveCategoriesAsync()
        {
            if (Categories.Count > 0) return;
            Categories.Clear();
            var list = await _xtream.GetLiveCategoriesAsync();
            Categories.Add(new Category { Id = -1, Name = "All" });
            foreach (var mc in list.OrderBy(c => c.Name))
                Categories.Add(mc);
            SelectedCategory = Categories.OrderBy(c => c.Name).FirstOrDefault(c => c.Id != -1);//Default to All
        }

        public async Task LoadLiveAsync()
        {
            if (AllLive.Count > 0) return;
            AllLive = new ObservableCollection<Live>(await _xtream.GetLiveAsync());
            ApplyFilters();
        }
        public async void ApplyFilters()
        {
            FilteredLive.Clear();
            var filter = (_selectedCategory?.Id) switch
            {
                -1 => AllLive,
                _ => _selectedCategory == null ? AllLive : AllLive.Where(l => l.CategoryId == _selectedCategory.Id),
            };
            FilteredLive = new ObservableCollection<Live>(filter.OrderBy(l => l.Name));
            //SelectedLive = FilteredLive.FirstOrDefault();
            PropertyChanged?.Invoke(this, new(nameof(FilteredLive)));
        }

        public async void Refresh()
        {
            var selectedLive = _selectedLive;
            _selectedLive = null;
            PropertyChanged?.Invoke(this, new(nameof(SelectedLive)));
            Task.Delay(10).Wait();
            SelectedLive = selectedLive;
        }
    }
}
