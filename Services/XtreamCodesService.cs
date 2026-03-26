using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using XtreamIPTV.Models;

namespace XtreamIPTV.Services
{
    public class XtreamCodesService : IIPTVService
    {
        private const string FilePath = "settings.json";
        private readonly HttpClient _http = new();
        private string _baseUrl = "http://10.0.0.100:9000";
        private string _username = "12";
        private string _password = "12";

        public XtreamCodesService()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                _baseUrl = data.GetProperty("baseUrl").GetString() ?? "";
                _username = data.GetProperty("username").GetString() ?? "";
                _password = data.GetProperty("password").GetString() ?? "";
            }
            else
            {
                Configure(_baseUrl, _username, _password);
            }
        }

        public void Configure(string baseUrl, string username, string password)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _username = username;
            _password = password;

            JsonObject jo =
            [
                new KeyValuePair<string, JsonNode?>("baseUrl", baseUrl),
                new KeyValuePair<string, JsonNode?>("username", username),
                new KeyValuePair<string, JsonNode?>("password", password),
            ];

            File.WriteAllText(FilePath, JsonSerializer.Serialize(jo));
        }

        private string BuildUrl(string? action = null, string extra = "")
        {
            var url = $"{_baseUrl}/player_api.php?username={_username}&password={_password}";
            if (!string.IsNullOrEmpty(action))
                url += $"&action={action}";
            if (!string.IsNullOrEmpty(extra))
                url += $"&{extra}";
            return url;
        }

        public async Task<List<Series>> GetSeriesAsync()
        {
            var json = await GetAsync(BuildUrl("get_series"));
            var data = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? [];

            var list = new List<Series>();

            foreach (var s in data)
            {
                var id = s.GetProperty("series_id").GetInt32();
                var name = s.GetProperty("name").GetString() ?? "";
                var plot = s.TryGetProperty("plot", out var p) ? p.GetString() ?? "" : "";
                var rating = s.TryGetProperty("rating", out var r) ? r.GetDouble() : 0.0;
                var lang = s.TryGetProperty("lang", out var l) ? l.GetString() ?? "" : "";
                var cover = s.TryGetProperty("cover", out var m) ? m.GetString() ?? "" : "";
                var backdrop_path = string.Empty;
                try
                {
                    if (s.TryGetProperty("backdrop_path", out var n))
                    {
                        if (n.ValueKind == JsonValueKind.Array)
                        {
                            var arr = n.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList();
                            backdrop_path = arr.FirstOrDefault();
                        }
                        else
                        {
                            backdrop_path = n.GetString() ?? "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error parsing backdrop_path for series {id}: {ex.Message}");
                }
                var category_id = int.Parse(s.GetProperty("category_id").ToString());

                var releaseDate = DateTime.MinValue;

                if (s.TryGetProperty("releaseDate", out var rd) && rd.ValueKind == JsonValueKind.String)
                {
                    DateTime.TryParse(rd.GetString(), out releaseDate);
                }
                long lastModified = s.TryGetProperty("last_modified", out var lm) ? long.Parse(lm.ToString() ?? "O") : 0;

                list.Add(new Series
                {
                    Id = id,
                    Title = name,
                    Plot = plot,
                    Rating = rating,
                    Language = lang,
                    CategoryId = category_id,
                    Poster = cover,
                    Backdrop = backdrop_path,
                    ReleaseDate = releaseDate,
                    LastModified = lastModified,
                });
            }

            return list;
        }

        public async Task<List<Season>> GetSeasonsAsync(Series series)
        {
            var json = await _http.GetStringAsync(BuildUrl("get_series_info", $"series_id={series.Id}"));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var seasons = new List<Season>();

            if (!root.TryGetProperty("seasons", out var seasonsElement) ||
                !root.TryGetProperty("episodes", out var episodesElement) ||
                !root.TryGetProperty("info", out var infoElement))
                return seasons;

            series.Genre = infoElement.TryGetProperty("genre", out var g) ? g.GetString() ?? "" : "";

            foreach (var seasonJson in seasonsElement.EnumerateArray())
            {
                var seasonNumber = seasonJson.GetProperty("season_number").GetInt32();

                var season = new Season { SeasonNumber = seasonNumber };

                if (episodesElement.TryGetProperty(seasonNumber.ToString(), out var epsArray))
                {
                    foreach (var ep in epsArray.EnumerateArray())
                    {
                        var title = ep.GetProperty("title").GetString() ?? "";
                        var epNum = ep.GetProperty("episode_num").GetInt32();
                        var id = ep.GetProperty("id").GetInt32();
                        var info = ep.GetProperty("info");
                        var cover_big = info.TryGetProperty("cover_big", out var cb) ? cb.GetString() ?? null : null;
                        var plot = info.TryGetProperty("plot", out var p) ? p.GetString() ?? "" : "";
                        var url = $"{_baseUrl}/series/{_username}/{_password}/{id}.mp4";

                        if (string.IsNullOrEmpty(cover_big))
                            cover_big = null;

                        season.Episodes.Add(new Episode
                        {
                            Title = title,
                            EpisodeNumber = epNum,
                            StreamUrl = url,
                            Poster = cover_big,
                            Plot = plot,
                            EpisodeId = $"S{seasonNumber:00}E{epNum:00} - {title}"
                        });
                    }
                }

                seasons.Add(season);
            }

            return seasons;
        }

        public async Task<List<Movie>> GetMoviesAsync()
        {
            var json = await GetAsync(BuildUrl("get_vod_streams"));
            var data = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? [];

            var list = new List<Movie>();
            foreach (var m in data)
            {
                var id = m.GetProperty("stream_id").GetInt32();
                var name = m.GetProperty("name").GetString() ?? "";
                var poster = m.GetProperty("stream_icon").GetString() ?? "";
                var plot = m.TryGetProperty("plot", out var p) ? p.GetString() ?? "" : "";
                var rating = m.TryGetProperty("rating", out var r) ? r.GetDouble() : 0.0;
                var lang = m.TryGetProperty("lang", out var l) ? l.GetString() ?? "" : "";
                var category_id = m.GetProperty("category_id").GetInt32();
                var direct_source = m.TryGetProperty("direct_source", out var ds) ? ds.ToString() ?? "" : "";
                var releaseDate = DateTime.MinValue;
                var added = m.TryGetProperty("added", out var t) ? long.Parse(t.GetString() ?? "0") : 0;

                if (m.TryGetProperty("releasedate", out var rd) && rd.ValueKind == JsonValueKind.String)
                {
                    DateTime.TryParse(rd.GetString(), out releaseDate);
                }

                var url = !string.IsNullOrEmpty(direct_source) ? direct_source : $"{_baseUrl}/movie/{_username}/{_password}/{id}.mp4";

                list.Add(new Movie
                {
                    Id = id,
                    Title = name,
                    Plot = plot,
                    Poster = poster,
                    Rating = rating,
                    Language = lang,
                    StreamUrl = url,
                    CategoryId = category_id,
                    ReleaseDate = releaseDate,
                    Added = added
                });
            }

            return list;
        }

        public async Task<List<Category>> GetMovieCategoriesAsync()
        {
            var json = await GetAsync(BuildUrl("get_vod_categories"));
            var data = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? [];

            var list = new List<Category>();
            foreach (var m in data)
            {
                var id = m.GetProperty("category_id").ToString() ?? "";
                var name = m.GetProperty("category_name").ToString() ?? "";
                var parentId = m.GetProperty("parent_id").ToString() ?? "";

                list.Add(new Category
                {
                    Id = int.Parse(id),
                    Name = name,
                    ParentId = int.Parse(parentId)
                });
            }

            return list;
        }

        public async Task<List<Category>> GetSeriesCategoriesAsync()
        {
            var json = await GetAsync(BuildUrl("get_series_categories"));
            var data = JsonSerializer.Deserialize<List<JsonElement>>(json) ?? [];

            var list = new List<Category>();
            foreach (var m in data)
            {
                var id = m.GetProperty("category_id").ToString() ?? "";
                var name = m.GetProperty("category_name").ToString() ?? "";
                var parentId = m.GetProperty("parent_id").ToString() ?? "";

                list.Add(new Category
                {
                    Id = int.Parse(id),
                    Name = name,
                    ParentId = int.Parse(parentId)
                });
            }

            return list;
        }

        public async Task<Movie> GetMovieDetailAsync(Movie movie)
        {
            var json = await _http.GetStringAsync(BuildUrl("get_vod_info", $"vod_id={movie.Id}"));
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            var info = data.GetProperty("info");
            var backdrop = info.TryGetProperty("backdrop_path", out JsonElement bp) ? bp.GetString() : "";
            var genre = info.GetProperty("genre").GetString() ?? "";
            var cast = info.GetProperty("cast").GetString() ?? "";
            var director = info.GetProperty("director").GetString() ?? "";

            if (info.TryGetProperty("releasedate", out var rd) && rd.ValueKind == JsonValueKind.String)
            {
                //if (DateTime.TryParse(rd.GetString(), out var dt))
                //    genre = (dt.Year / 10) * 10;
                genre = $"{rd.GetString()} * {genre}";
            }

            movie.Backdrop = backdrop;
            movie.ReleaseInfo = genre;
            if (!string.IsNullOrEmpty(cast))
                movie.CastInfo = $"Cast: {cast}";
            if (!string.IsNullOrEmpty(director))
                movie.DirectorInfo = $"Director: {director}";
            return movie;
        }

        private async Task<string> GetAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                Uri uri = new(url);
                string action = string.Empty;
                foreach (string val in uri.Query.Split('&'))
                {
                    if (val.Replace("?", "").StartsWith("action="))
                        action = val.Replace("?", "").Replace("action=", "");
                }
                if (!string.IsNullOrEmpty(action))
                {
                    string filename = $"cache_{action}.json";
                    if (File.Exists(filename))
                    {
                        var lastWrite = File.GetLastWriteTime(filename);
                        if (DateTime.Now - lastWrite < TimeSpan.FromHours(8))
                        {
                            return await File.ReadAllTextAsync(filename, cancellationToken);
                        }
                    }
                }
                var response = await _http.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                string retValue = response.Content.ReadAsStringAsync(cancellationToken).Result;
                if (!string.IsNullOrEmpty(action))
                {
                    string filename = $"cache_{action}.json";
                    await File.WriteAllTextAsync(filename, retValue, cancellationToken);
                }
                return retValue;
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log them)
                return string.Empty;
            }
        }
    }
}