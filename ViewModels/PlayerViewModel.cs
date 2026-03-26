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

        public double StartPositionSeconds => _continue.GetProgress(_currentEpisodeId);

        public void Play(string episodeId, string url)
        {
            _currentEpisodeId = episodeId;
            CurrentStreamUrl = url;
        }

        public void SavePosition(double seconds)
        {
            if (!string.IsNullOrEmpty(_currentEpisodeId))
                _continue.SaveProgress(_currentEpisodeId, seconds);
        }
    }
}