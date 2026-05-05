using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using XtreamIPTV.Models;

namespace XtreamIPTV.Services
{
    public class SeriesEpisodeService
    {
        private const string FilePath = "seriesEpisodes.json";

        public OrderedDictionary<int, int> Progress { get; private set; } = new();

        public SeriesEpisodeService()
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                Progress = JsonSerializer.Deserialize<OrderedDictionary<int, int>>(json) ?? new();
            }
        }

        public int GetLastWatched(int seriesId)
        {
            return Progress.TryGetValue(seriesId, out var episodeId) ? episodeId : 0;
        }

        internal void SetLastWatched(Episode selectedEpisode)
        {
            if (Progress.ContainsKey(selectedEpisode.SeriesId) && Progress[selectedEpisode.SeriesId] == selectedEpisode.EpisodeId)
                return;
            Progress.Remove(selectedEpisode.SeriesId);
            Progress.Add(selectedEpisode.SeriesId, selectedEpisode.EpisodeId);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Progress));
        }

        internal void RemoveLastWatched(int seriesId)
        {
            if (!Progress.ContainsKey(seriesId)) return;

            Progress.Remove(seriesId);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Progress));
        }

        public List<int> GetSeriesIds()
        {
            var list = new List<int>();
            foreach (var kvp in Progress)
            {
                list.Add(kvp.Key);
            }
            return list;
        }

    }
}