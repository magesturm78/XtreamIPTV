using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using XtreamIPTV.Models;

namespace XtreamIPTV.Services
{
    public class XtreamCodesService : IIPTVService
    {
        private bool CreateNewMoviesFromSimiliar = false;
        private const string FilePath = "settings.json";
        private readonly HttpClient _http = new();
        private HttpClient? _tmdbclient = null;
        private string _baseUrl = "http://10.0.0.100:9000";
        private string _username = "12";
        private string _password = "12";
        private static Dictionary<string, string> langMap = new()
                {
                    {"English", "en"},
                    {"French", "fr"},
                    {"Spanish", "es"},
                    {"Japanese", "ja"},
                    {"German", "de"},
                    {"Italian", "it"},
                    {"Russian", "ru"},
                    {"Mandarin", "zh"},
                    {"Portuguese", "pt"},
                    {"Korean", "ko"},
                    {"Hindi", "hi"},
                    {"Cantonese", "cn"},
                    {"Turkish", "tr"},
                    {"Dutch", "nl"},
                    {"Tamil", "ta"},
                    {"Malayalam", "ml"},
                    {"Swedish", "sv"},
                    {"Tagalog", "tl"},
                    {"Polish", "pl"},
                    {"Arabic", "ar"},
                    {"Czech", "cs"},
                    {"Indonesian", "id"},
                    {"Telugu", "te"},
                    {"Danish", "da"},
                    {"Greek", "el"},
                    {"Thai", "th"},
                    {"Persian", "fa"},
                    {"Finnish", "fi"},
                };

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

        public async Task<IEnumerable<Series>> GetSeriesAsync()
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
                    Languages = [lang],
                    CategoryId = category_id,
                    Poster = cover,
                    Backdrop = backdrop_path,
                    ReleaseDate = releaseDate,
                    LastModified = lastModified,
                });
            }

            return list;
        }

        public async Task<IEnumerable<Season>> GetSeasonsAsync(Series series)
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
                        var extension = ep.TryGetProperty("container_extension", out var e) ? e.GetString() ?? string.Empty : string.Empty;
                        var url = $"{_baseUrl}/series/{_username}/{_password}/{id}.{extension}";

                        if (string.IsNullOrEmpty(cover_big))
                            cover_big = null;

                        season.Episodes.Add(new Episode
                        {
                            Title = $"S{seasonNumber:00}E{epNum:00} - {title}",
                            EpisodeNumber = epNum,
                            DirectSource = url,
                            Poster = cover_big,
                            Plot = plot,
                            EpisodeId = id
                        });
                    }
                }

                seasons.Add(season);
            }

            return seasons;
        }

        public async Task<IEnumerable<Movie>> GetMoviesAsync()
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
                var backdrop_path = m.TryGetProperty("backdrop_path", out var bp) ? bp.GetString() ?? "" : "";
                var releaseDate = DateTime.MinValue;
                var added = m.TryGetProperty("added", out var t) ? long.Parse(t.ToString() ?? "0") : 0;

                if (m.TryGetProperty("releasedate", out var rd) && rd.ValueKind == JsonValueKind.String)
                {
                    DateTime.TryParse(rd.GetString(), out releaseDate);
                } else
                {
                    // If releasedate is not available, use the 'added' timestamp to estimate the release date
                    if (added > 0)
                    {
                        // Assuming 'added' is a Unix timestamp in seconds
                        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        releaseDate = epoch.AddSeconds(added).ToLocalTime();
                    }
                }

                var url = !string.IsNullOrEmpty(direct_source) ? direct_source : $"{_baseUrl}/movie/{_username}/{_password}/{id}.mp4";

                list.Add(new Movie
                {
                    Id = id,
                    Title = name,
                    Plot = plot,
                    Poster = poster,
                    Rating = rating,
                    Languages = [lang],
                    StreamUrl = url,
                    CategoryId = category_id,
                    ReleaseDate = releaseDate,
                    Backdrop = backdrop_path,
                    Added = added
                });
            }

            return list;
        }

        public async Task<IEnumerable<Category>> GetMovieCategoriesAsync()
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

        public async Task<IEnumerable<Category>> GetSeriesCategoriesAsync()
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
                if (DateTime.TryParse(rd.GetString(), out var dt))
                    movie.ReleaseDate = dt;
                //    genre = (dt.Year / 10) * 10;
                genre = $"{rd.GetString()} * {genre}";
            }

            if (!string.IsNullOrEmpty(backdrop))
                movie.Backdrop = backdrop;

            if (!string.IsNullOrEmpty(genre))
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
                            Debug.WriteLine($"Using cached data for action {action}");
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
            catch
            {
                // Handle exceptions (e.g., log them)
                return string.Empty;
            }
        }

        public async Task<IEnumerable<Movie>> GetSimiliarMovies(Movie? movie, System.Collections.ObjectModel.ObservableCollection<Movie> allMovies)
        {
            var list = new List<Movie>();
            if (movie == null) return list;

            if (movie.Similiar.Count == 0)
            {
                if (_tmdbclient == null)
                {
                    _tmdbclient = new HttpClient();
                    _tmdbclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ConfigurationManager.AppSettings["Bearer"]);
                    _tmdbclient.DefaultRequestHeaders.Add("accept", "application/json");
                }

                List<string> urls = [$"https://api.themoviedb.org/3/movie/{movie.Id}/recommendations",
                                            $"https://api.themoviedb.org/3/movie/{movie.Id}/similar"];
                foreach (string url in urls)
                {
                    try
                    {
                        var json = await _tmdbclient.GetStringAsync(url);
                        var page_results = JsonSerializer.Deserialize<JsonElement>(json);
                        if (page_results.TryGetProperty("results", out JsonElement results))
                            foreach (var data in results.EnumerateArray())
                            {
                                if (data.TryGetProperty("id", out var mid))
                                {
                                    try
                                    {
                                        int id = mid.GetInt32();
                                        //_ = await _http.GetStringAsync(BuildUrl("get_vod_info", $"vod_id={mid}"));
                                        if (!movie.Similiar.Contains(id))
                                            movie.Similiar.Add(mid.GetInt32());
                                        if (CreateNewMoviesFromSimiliar && !allMovies.Any(m => m.Id == id))
                                        {
                                            var new_movie = CreateMovieFromJSON(id, data);
                                            if (new_movie != null)
                                            {
                                                allMovies.Add(new_movie);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.Print($"Error fetching movie info for ID {mid}: {ex.Message}");
                                    }
                                }
                            }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"Error fetching similar movies for ID {movie.Id}: {ex.Message}");
                    }
                }
            }

            if (!movie.Similiar.Contains(movie.Id))
            {
                movie.Similiar.Insert(0, movie.Id);
            }

            foreach (var tmdbid in movie.Similiar)
            {
                var fmov = allMovies.FirstOrDefault(m => m.Id == tmdbid);
                if (fmov != null)
                    list.Add(fmov);
            }
            return list.OrderByDescending(x => x.Id == movie.Id).ThenByDescending(x => x.ReleaseDate);
        }

        private Movie CreateMovieFromJSON(int id, JsonElement data)
        {
            Movie movie = new Movie
            {
                Id = id,
                Backdrop = data.TryGetProperty("backdrop_path", out JsonElement bp) ? $"https://image.tmdb.org/t/p/w780{bp.GetString()}" : "",
                OriginalLanguage = data.TryGetProperty("original_language", out JsonElement ol) ? ol.GetString() ?? "" : "",
                Plot = data.TryGetProperty("overview", out JsonElement o) ? o.GetString() ?? "" : "",
                Rating = data.TryGetProperty("vote_average", out JsonElement va) ? va.GetDouble() : 0.0,
                ReleaseDate = data.TryGetProperty("release_date", out JsonElement rd) && rd.ValueKind == JsonValueKind.String && DateTime.TryParse(rd.GetString(), out var dt) ? dt : DateTime.MinValue,
                Poster = data.TryGetProperty("poster_path", out JsonElement p) ? $"https://image.tmdb.org/t/p/w154{p.GetString()}" : "",
                Title = data.TryGetProperty("title", out JsonElement t) ? t.GetString() ?? "" : ""
            };

            return movie;
        }

        public async Task<IEnumerable<Series>> GetSimiliarSeries(Series? series, System.Collections.ObjectModel.ObservableCollection<Series> allSeries)
        {
            var list = new List<Series>();
            if (series == null) return list;
            if (series.Similiar.Count == 0)
            {
                if (_tmdbclient == null)
                {
                    _tmdbclient = new HttpClient();
                    _tmdbclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ConfigurationManager.AppSettings["Bearer"]);
                    _tmdbclient.DefaultRequestHeaders.Add("accept", "application/json");
                }
                List<string> urls = [$"https://api.themoviedb.org/3/tv/{series.Id}/recommendations",
                                            $"https://api.themoviedb.org/3/tv/{series.Id}/similar"];
                foreach (string url in urls)
                {
                    try
                    {
                        var json = await _tmdbclient.GetStringAsync(url);
                        var data = JsonSerializer.Deserialize<JsonElement>(json);
                        if (data.TryGetProperty("results", out JsonElement results))
                            foreach (var id in results.EnumerateArray())
                            {
                                if (id.TryGetProperty("id", out var mid))
                                {
                                    try
                                    {
                                        //_ = await _http.GetStringAsync(BuildUrl("get_series_info", $"series_id={mid}"));
                                        if (!series.Similiar.Contains(mid.GetInt32()))
                                            series.Similiar.Add(mid.GetInt32());
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.Print($"Error fetching series info for ID {mid}: {ex.Message}");
                                    }
                                }
                            }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"Error fetching similar series for ID {series.Id}: {ex.Message}");
                    }
                }
            }
            if (!series.Similiar.Contains(series.Id))
            {
                series.Similiar.Insert(0, series.Id);
            }

            foreach (var id in series.Similiar)
            {
                var fser = allSeries.FirstOrDefault(s => s.Id == id);
                if (fser != null)
                    list.Add(fser);
            }
            //return list.OrderBy(x => series.Similiar.IndexOf(x.Id));
            return list.OrderByDescending(x => x.Id == series.Id).ThenByDescending(x => x.ReleaseDate);
        }

        private Series? CreateSeriesFromJSON(int id, string data, string credits)
        {
            if (string.IsNullOrEmpty(data) || data == "null")
                return null;
            // Process the data as needed
            dynamic? obj1 = JsonSerializer.Deserialize<JsonElement>(data);
            string backdrop = obj1.TryGetProperty("backdrop_path", out JsonElement bp) ? bp.ToString() : "";
            string title = obj1.TryGetProperty("name", out JsonElement t) ? t.ToString() : "";
            string poster = obj1.TryGetProperty("poster_path", out JsonElement p) ? p.ToString() : "";
            string release_date = obj1.TryGetProperty("first_air_date", out JsonElement rd) ? rd.ToString() : "";
            string last_air_date = obj1.TryGetProperty("last_air_date", out JsonElement lad) ? lad.ToString() : "";
            string overview = obj1.TryGetProperty("overview", out JsonElement o) ? o.ToString() : "";
            string rating = obj1.TryGetProperty("vote_average", out JsonElement va) ? va.ToString() : "0";
            string o_lang = obj1.TryGetProperty("original_language", out JsonElement ol) ? ol.ToString() : "";
            string similiar = string.Empty;
            var categoryId = 0;

            if (string.IsNullOrEmpty(last_air_date) || last_air_date == "null")
                last_air_date = release_date;

            List<string> cast = [];
            List<string> director = [];

            if (string.IsNullOrEmpty(poster) || string.IsNullOrEmpty(release_date))
                return null;

            string genre = string.Empty;
            if (obj1.TryGetProperty("genres", out JsonElement genres))
            {
                foreach (var g in genres.EnumerateArray())
                {
                    if (!string.IsNullOrEmpty(genre))
                        genre += ", ";
                    else
                        categoryId = g.GetProperty("id").GetInt32();
                    genre += g.GetProperty("name");
                }
            }
            if (obj1.TryGetProperty("genre_ids", out JsonElement genre_ids))
            {
                foreach (var g in genre_ids.EnumerateArray())
                {
                    if (!string.IsNullOrEmpty($"{g}") && categoryId == 0)
                        categoryId = int.Parse($"{g}");
                }
            }
            List<string> langs = [];
            if (obj1.TryGetProperty("spoken_languages", out JsonElement spoken_languages))
            {
                foreach (var language in spoken_languages.EnumerateArray())
                {
                    langs.Add(language.GetProperty("english_name").ToString());
                }
            }
            if (langs.Count > 1 || langs.SingleOrDefault() != "English")
            {
                overview = $"({string.Join(", ", langs)}) {overview}";
            }
            poster = $"https://image.tmdb.org/t/p/w154{poster}";
            if (!String.IsNullOrEmpty(backdrop))
                backdrop = $"https://image.tmdb.org/t/p/w780{backdrop}";

            string runtime = string.Empty;
            if (obj1.TryGetProperty("episode_run_time", out JsonElement rt))
            {
                foreach (var rti in rt.EnumerateArray())
                {
                    if (string.IsNullOrEmpty(runtime))
                    {
                        runtime = getRunTime(int.Parse($"{rti}"));
                        break;
                    }
                }
            }

            string age = "";
            if (obj1.TryGetProperty("content_ratings", out JsonElement releases))
            {
                foreach (var country in (releases.TryGetProperty("results", out JsonElement x) ? x : default).EnumerateArray())
                {
                    if (country.GetProperty("iso_3166_1").ToString() == "US" &&
                        !string.IsNullOrEmpty(country.GetProperty("rating").ToString()))
                    {
                        age = $"({country.GetProperty("rating")}) ";
                        break;
                    }
                }
                if (string.IsNullOrEmpty(age))
                {
                    foreach (var country in (releases.TryGetProperty("results", out JsonElement x) ? x : default).EnumerateArray())
                    {
                        if (!string.IsNullOrEmpty(country.GetProperty("rating").ToString()))
                        {
                            age = $"({country.GetProperty("rating")}) * ";
                            break;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(credits) && credits != "null")
            {
                dynamic? objCredits = JsonSerializer.Deserialize<JsonElement>(credits);
                {
                    if (objCredits.TryGetProperty("cast", out JsonElement ocast))
                        foreach (var c in ocast.EnumerateArray())
                        {
                            if (c.GetProperty("known_for_department").ToString() == "Acting")
                                cast.Add(c.GetProperty("name").ToString());
                        }
                    if (objCredits.TryGetProperty("crew", out JsonElement ocrew))
                        foreach (var c in ocrew.EnumerateArray())
                        {
                            if (c.GetProperty("department").ToString() == "Directing")
                                director.Add(c.GetProperty("name").ToString());
                        }
                }
            }

            _ = DateTime.TryParse(release_date, out DateTime releaseDate);
            _ = DateTime.TryParse(last_air_date, out DateTime lastDate);
            long ticks = lastDate.Year * 1000000;
            ticks += lastDate.Month * 1000;
            ticks += lastDate.Day;
            string serTitle = $"{title} ({release_date[..4]}";
            if (release_date[..4] != last_air_date[..4])
                serTitle += $" - {last_air_date[..4]}";
            serTitle += ")";
            var series = new Series
            {
                Id = id,
                Title = serTitle,
                ReleaseDate = releaseDate,
                Backdrop = backdrop,
                Poster = poster,
                Plot = overview,
                LastModified = ticks,
                CategoryId = categoryId,
                Languages = langs,
                OriginalLanguage = langMap.Any(l => l.Value == o_lang) ? langMap.Where(l => l.Value == o_lang).FirstOrDefault().Key : langs.Count() > 0 ? langs[0] : string.Empty,
                //ReleaseInfo = $"{release_date} * {age}{genre}{runtime}",
                Rating = double.TryParse(rating, out var rat) ? rat : 0.0,
                CastInfo = $"Cast: {string.Join(", ", cast)}",
                DirectorInfo = $"Director: {string.Join(", ", director)}",
            };
            return series;
        }

        private static string getRunTime(int runtime)
        {
            if (runtime < 0) return string.Empty;
            int hours = runtime / 60;
            int minutes = runtime % 60;
            if (hours > 0)
                return $" * {hours}h {minutes}m";
            else
                return $" * {minutes}m";
        }

    }
}