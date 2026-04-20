using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Input;
using XtreamIPTV.Models;
using XtreamIPTV.Views;

namespace XtreamIPTV.ViewModels
{
    public class SettingsViewModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public ICommand SaveCommand { get; }

        private static string filePath = "settings.json";
        private string _serverUrl = "http://localhost";
        private string _username = "12";
        private string _password = "12";

        public string ServerUrl
        {
            get => _serverUrl;
            set
            {
                _serverUrl = value;
                PropertyChanged?.Invoke(this, new(nameof(ServerUrl)));
            }
        }
        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                PropertyChanged?.Invoke(this, new(nameof(Username)));
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                PropertyChanged?.Invoke(this, new(nameof(Password)));
            }
        }

        public SettingsViewModel() 
        {
            SaveCommand = new RelayCommand(_ => SaveSettings());
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                _serverUrl = data.GetProperty("baseUrl").GetString() ?? "";
                _username = data.GetProperty("username").GetString() ?? "";
                _password = data.GetProperty("password").GetString() ?? "";
            }
            else
            {
                SaveSettings();
            }
        }

        public void SaveSettings()
        {
            JsonObject jo =
            [
                new KeyValuePair<string, JsonNode?>("baseUrl", _serverUrl),
                new KeyValuePair<string, JsonNode?>("username", _username),
                new KeyValuePair<string, JsonNode?>("password", _password),
            ];

            File.WriteAllText(filePath, JsonSerializer.Serialize(jo));
            Directory.GetFiles(".", $"cache_*.json").ToList().ForEach(f =>
            {
                File.Delete(f);
            });
        }

    }
}
