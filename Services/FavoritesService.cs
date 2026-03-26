using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace XtreamIPTV.Services
{
    public class FavoritesService
    {
        private const string FilePath = "favorites.json";

        public HashSet<string> Favorites { get; private set; } = new();

        public FavoritesService()
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                Favorites = JsonSerializer.Deserialize<HashSet<string>>(json) ?? new();
            }
        }

        public void ToggleFavorite(string key)
        {
            if (!Favorites.Remove(key))
                Favorites.Add(key);

            File.WriteAllText(FilePath, JsonSerializer.Serialize(Favorites));
        }

        public bool IsFavorite(string key) => Favorites.Contains(key);
    }
}