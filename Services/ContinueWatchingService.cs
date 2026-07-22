using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace XtreamIPTV.Services
{
    public class ContinueWatchingService
    {
        private const string FilePath = "continue.json";

<<<<<<< HEAD
        public OrderedDictionary<string, double> Progress { get; private set; } = new();
=======
        public Dictionary<string, double> Progress { get; private set; } = new();
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4

        public ContinueWatchingService()
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
<<<<<<< HEAD
                Progress = JsonSerializer.Deserialize<OrderedDictionary<string, double>>(json) ?? new();
=======
                Progress = JsonSerializer.Deserialize<Dictionary<string, double>>(json) ?? new();
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
            }
        }

        public void SaveProgress(string episodeId, double seconds)
        {
<<<<<<< HEAD
            if (Progress.ContainsKey(episodeId) && Progress[episodeId] == seconds) 
                return;
            Progress.Remove(episodeId);
            Progress.Add(episodeId, seconds);
=======
            Progress[episodeId] = seconds;
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Progress));
        }

        public double GetProgress(string episodeId)
        {
            return Progress.TryGetValue(episodeId, out var sec) ? sec : 0;
        }
<<<<<<< HEAD

        public List<string> GetMovieProgress()
        {
            var list = new List<string>();
            foreach (var kvp in Progress)
            {
                if (kvp.Key.StartsWith("movie-"))
                {
                    list.Add(kvp.Key.Replace("movie-", ""));
                }
            }
            return list;
        }

        public void RemoveProgress(string episodeId)
        {
            if (!Progress.ContainsKey(episodeId)) return;

            Progress.Remove(episodeId);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Progress));
        }
=======
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
    }
}