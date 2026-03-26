using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace XtreamIPTV.Services
{
    public class ContinueWatchingService
    {
        private const string FilePath = "continue.json";

        public Dictionary<string, double> Progress { get; private set; } = new();

        public ContinueWatchingService()
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                Progress = JsonSerializer.Deserialize<Dictionary<string, double>>(json) ?? new();
            }
        }

        public void SaveProgress(string episodeId, double seconds)
        {
            Progress[episodeId] = seconds;
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Progress));
        }

        public double GetProgress(string episodeId)
        {
            return Progress.TryGetValue(episodeId, out var sec) ? sec : 0;
        }
    }
}