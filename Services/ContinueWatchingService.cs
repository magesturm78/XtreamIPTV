using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace XtreamIPTV.Services
{
    public class ContinueWatchingService
    {
        private const string FilePath = "continue.json";

        public OrderedDictionary<string, double> Progress { get; private set; } = new();

        public ContinueWatchingService()
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                Progress = JsonSerializer.Deserialize<OrderedDictionary<string, double>>(json) ?? new();
            }
        }

        public void SaveProgress(string episodeId, double seconds)
        {
            if (Progress.ContainsKey(episodeId) && Progress[episodeId] == seconds) 
                return;
            Progress.Remove(episodeId);
            Progress.Add(episodeId, seconds);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Progress));
        }

        public double GetProgress(string episodeId)
        {
            return Progress.TryGetValue(episodeId, out var sec) ? sec : 0;
        }

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
    }
}