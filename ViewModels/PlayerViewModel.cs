using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using XtreamIPTV.Services;

namespace XtreamIPTV.ViewModels
{
    public class PlayerViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _currentStreamUrl = "";
        private string _currentEpisodeId = "";

        private readonly ContinueWatchingService _continue;

        private string _errorMessage = "";
<<<<<<< HEAD
        private string _title = "";
=======
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4

        public string ErrorMessage 
        { 
            get
            {
                return _errorMessage;
            }
            set
            {
                _errorMessage = value;
                PropertyChanged?.Invoke(this, new(nameof(ErrorMessage)));
            }
        }

        public PlayerViewModel(ContinueWatchingService cont)
        {
            _continue = cont;
        }

        public string CurrentStreamUrl
        {
            get => _currentStreamUrl;
            set
            {
                _currentStreamUrl = value;
                PropertyChanged?.Invoke(this, new(nameof(CurrentStreamUrl)));
            }
        }

<<<<<<< HEAD
        public string CurrentEpisodeId
        {
            get => _currentEpisodeId;
            set
            {
                _currentEpisodeId = value;
                PropertyChanged?.Invoke(this, new(nameof(CurrentEpisodeId)));
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                PropertyChanged?.Invoke(this, new(nameof(Title)));
            }
        }

        public double StartPositionSeconds => _continue.GetProgress(_currentEpisodeId);

        public void Play(string episodeId, string title, string url)
        {
            _currentEpisodeId = episodeId;
            Title = title;
=======
        public double StartPositionSeconds => _continue.GetProgress(_currentEpisodeId);

        public void Play(string episodeId, string url)
        {
            _currentEpisodeId = episodeId;
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
            CurrentStreamUrl = url;
        }

        public void SavePosition(double seconds)
        {
<<<<<<< HEAD
            if (seconds == 0)
                return;
=======
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
            if (!string.IsNullOrEmpty(_currentEpisodeId))
                _continue.SaveProgress(_currentEpisodeId, seconds);
        }
    }
}