using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using XtreamIPTV.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Net.WebRequestMethods;

namespace XtreamIPTV.Services
{
    public class DatabaseService : IIPTVService
    {
        private static readonly string connectionString = "Data Source=D:\\tv_data\\database.db;";
        private readonly HttpClient _http = new();
        private HttpClient? _tmdbclient = null;
        private readonly string _baseUrl = "http://10.0.0.100:9000";
        private readonly string _username = "12";
        private readonly string _password = "12";
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

        public DatabaseService() 
        { 

        }

        public async Task<IEnumerable<Category>> GetMovieCategoriesAsync()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            string query = "SELECT ID, name FROM movie_genres order by name";
            using var command = new SqliteCommand(query, connection);
            var reader = command.ExecuteReader();

            var list = new List<Category>();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string name = reader.GetString(1);
                // Process the data as needed
                list.Add(new Category { 
                    Id = id,
                    Name = name,
                    ParentId = 0
                });
            }
            reader.Close();
            connection.Close();
            return list;
        }

        public async Task<Movie> GetMovieDetailAsync(Movie? movie)
        {
            if (movie == null)
                return new Movie();
            var json = await _http.GetStringAsync(BuildUrl("get_vod_info", $"vod_id={movie.Id}"));
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            var info = data.GetProperty("info");
            var backdrop = info.TryGetProperty("backdrop_path", out JsonElement bp) ? bp.GetString() : "";
            var genre = info.GetProperty("genre").GetString() ?? "";
            var cast = info.GetProperty("cast").GetString() ?? "";
            var director = info.GetProperty("director").GetString() ?? "";
            var plot = info.GetProperty("plot").GetString() ?? "";

            if (info.TryGetProperty("releasedate", out var rd) && rd.ValueKind == JsonValueKind.String)
            {
                genre = $"{rd.GetString()} * {genre}";
            }

            movie.Backdrop = backdrop;
            movie.ReleaseInfo = genre;
            if (!string.IsNullOrEmpty(cast))
                movie.CastInfo = $"Cast: {cast}";
            if (!string.IsNullOrEmpty(director))
                movie.DirectorInfo = $"Director: {director}";
            movie.Plot = plot;
            return movie;
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

        public async Task<IEnumerable<Movie>> GetMoviesAsync()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            string query = "SELECT movies.tmdb_id, json(data), json(credits) " +
                            //", similiar " +
                            "FROM movies " +
                           //$"left outer join similiar_movies on movies.tmdb_id = similiar_movies.tmdb_id " +
                            "WHERE 1=1 " +
                           //$"  AND release_date < '{DateTime.Now:yyyy-MM-dd}' " +
                            //"  AND poster_path is not null " +
                            //"  AND (CAST(data->'$.runtime' as integer) > 20 or " +
                            //"       CAST(data->'$.runtime' as integer) = 0 or " +
                            //"       data->'$.runtime' is null) " +
                            "order by popularity desc, release_date desc " +
                            //"LIMIT 10000" +
                            "";
            using var command = new SqliteCommand(query, connection);
            var reader = command.ExecuteReader();

            var list = new List<Movie>();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string data = reader.GetString(1);
                string credits = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty;
                //string similiar = !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty;
                // Process the data as needed
                var movie = CreateMovieFromJSON(id, data, credits);
                if (movie != null)
                {
                    //if (!string.IsNullOrEmpty(similiar))
                    //    movie.Similiar = similiar.Split(',').Select(s => int.TryParse(s, out var smid) ? smid : 0).Where(smid => smid > 0).ToList();
                    list.Add(movie);
                }
            }
            reader.Close();
            connection.Close();
            return list;
        }

        public async Task<IEnumerable<Movie>> GetMoviesAsync(int categoryId, int decade, string language, int count)
        {
            var list = new List<Movie>();
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            string query = "FROM movies " +
                            "WHERE 1=1 " +
                            "  AND poster_path is not null " +
                            "  AND (CAST(data->'$.runtime' as integer) > 20 or " +
                            "       CAST(data->'$.runtime' as integer) = 0 or " +
                            "       data->'$.runtime' is null) ";
            //filters
            if (categoryId > 0)
            {
                query += $"AND (genre_id_0 = {categoryId} or" +
                         $"  genre_id_1 = {categoryId}) ";
            }
            if (language != null && !string.IsNullOrEmpty(language.ToString()))
            {
                query += $"AND data->>'$.original_language' = '{langMap[language]}' ";
            }

            if (decade > 0)
            {
                string max_date = $"{decade + 10}-01-01";
                if (max_date.CompareTo(DateTime.Now.ToString("yyyy-MM-dd")) > 0)
                    max_date = DateTime.Now.ToString("yyyy-MM-dd");
                query += $"AND (release_date >= '{decade}-01-01' AND ";
                query += $"     release_date < '{max_date}') ";
            }
            else
            {
                query += $" AND release_date < '{DateTime.Now:yyyy-MM-dd}' ";
            }

            query += "order by popularity desc, release_date desc ";

            string selectQuery = $"SELECT movies.tmdb_id, json(data), json(credits) {query} LIMIT {count}";
            string countQuery = $"SELECT count(movies.tmdb_id) {query}";

            using var countCommand = new SqliteCommand(countQuery, connection);
            int totalCount = int.Parse(countCommand.ExecuteScalar().ToString());

            if (totalCount == 0)
            {
                connection.Close();
                return list;
            }
            using var command = new SqliteCommand(selectQuery, connection);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string data = reader.GetString(1);
                string credits = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty;
                // Process the data as needed
                var movie = CreateMovieFromJSON(id, data, credits);
                if (movie != null)
                {
                    if (categoryId > 0 && movie.CategoryId != categoryId)
                    {
                        Debug.Print($"here cat {movie.CategoryId} - {categoryId}");
                        CreateMovieFromJSON(id, data, credits);
                            }
                    list.Add(movie);
                }
                else
                    Debug.Print("here");
            }
            reader.Close();
            connection.Close();
            return list;
        }

        private static Movie? CreateMovieFromJSON(int id, string data, string credits)
        {
            dynamic? obj1 = JsonSerializer.Deserialize<JsonElement>(data);
            string backdrop = obj1.TryGetProperty("backdrop_path", out JsonElement bp) ? bp.ToString() : "";
            string title = obj1.TryGetProperty("title", out JsonElement t) ? t.ToString() : "";
            string poster = obj1.TryGetProperty("poster_path", out JsonElement p) ? p.ToString() : "";
            string release_date = obj1.TryGetProperty("release_date", out JsonElement rd) ? rd.ToString() : "";
            string overview = obj1.TryGetProperty("overview", out JsonElement o) ? o.ToString() : "";
            string rating = obj1.TryGetProperty("vote_average", out JsonElement va) ? va.ToString() : "0";
            string o_lang = obj1.TryGetProperty("original_language", out JsonElement ol) ? ol.ToString() : "";
            string similiar = string.Empty;
            var categoryId = 0;

            List<string> cast = [];
            List<string> director = [];

            if (string.IsNullOrEmpty(poster) || string.IsNullOrEmpty(release_date))
                return null;

            string age = "";
            if (obj1.TryGetProperty("releases", out JsonElement releases))
            {
                foreach (var country in (releases.TryGetProperty("countries", out JsonElement x) ? x : default).EnumerateArray())
                {
                    if (country.GetProperty("iso_3166_1").ToString() == "US" &&
                        !string.IsNullOrEmpty(country.GetProperty("certification").ToString()))
                    {
                        age = $"({country.GetProperty("certification")}) ";
                        break;
                    }
                }
                if (string.IsNullOrEmpty(age))
                {
                    foreach (var country in (releases.TryGetProperty("countries", out JsonElement x) ? x : default).EnumerateArray())
                    {
                        if (!string.IsNullOrEmpty(country.GetProperty("certification").ToString()))
                        {
                            age = $"({country.GetProperty("certification")}) * ";
                            break;
                        }
                    }
                }
            }

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
                if (obj1.TryGetProperty("genre_ids", out JsonElement genre2))
                {
                    foreach (var g in genre2.EnumerateArray())
                    {
                        if (categoryId == 0)
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
            if (obj1.TryGetProperty("runtime", out JsonElement rt))
            {
                runtime = getRunTime(int.Parse($"{rt}"));
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

            DateTime.TryParse(release_date, out DateTime releaseDate);
            long ticks = releaseDate.Year * 1000000;
            ticks += releaseDate.Month * 1000;
            ticks += releaseDate.Day;

            return new Movie
            {
                Id = id,
                Title = $"{title} ({release_date[..4]})",
                ReleaseDate = releaseDate,
                Backdrop = backdrop,
                Poster = poster,
                Plot = overview,
                Added = ticks,
                CategoryId = categoryId,
                ReleaseInfo = $"{release_date} * {age}{genre}{runtime}",
                Rating = double.TryParse(rating, out var rat) ? rat : 0.0,
                CastInfo = $"Cast: {string.Join(", ", cast)}",
                DirectorInfo = $"Director: {string.Join(", ", director)}",
                Languages = langs,
                OriginalLanguage = langMap.Any(l => l.Value == o_lang) ? langMap.Where(l => l.Value == o_lang).FirstOrDefault().Key : langs.Count() > 0 ? langs[0] : string.Empty,
                //StreamUrl = $"http://10.0.0.100:9000/movie/12/12/{id}.mp4",
                //Similiar = similiar,
                //UpdateNeeded = updateNeeded,
            };
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
                        //if (cover_big != null)
                        //    cover_big = cover_big.Replace("/original/", "/w200/");
                        var plot = info.TryGetProperty("plot", out var p) ? p.GetString() ?? "" : "";
                        var url = $"{_baseUrl}/series/{_username}/{_password}/{id}.mp4";

                        if (string.IsNullOrEmpty(cover_big))
                            cover_big = null;

                        season.Episodes.Add(new Episode
                        {
                            SeriesId = series.Id,
                            SeasonId = seasonNumber,
                            Title = $"S{seasonNumber:00}E{epNum:00} - {title}",
                            EpisodeNumber = epNum,
                            //StreamUrl = url,
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

        public async Task<IEnumerable<Series>> GetSeriesAsync()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            string query = "SELECT tmdb_id, json(data), json(credits), updated " +
                            "FROM series " +
                            "WHERE 1=1 " +
                            //$"  AND  data->>'$.first_air_date' < '{DateTime.Now:yyyy-MM-dd}' " +
                            //"  AND poster_path is not null " +
                            "order by data->>'$.popularity' desc, data->>'$.first_air_date' desc " +
                            //"LIMIT 100" +
                            "";
            using var command = new SqliteCommand(query, connection);
            var reader = command.ExecuteReader();

            var list = new List<Series>();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string data = !reader.IsDBNull(1) ? reader.GetString(1) : string.Empty;
                string credits = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty;
                var series = CreateSeriesFromJSON(id, data, credits);
                if (series != null)
                    list.Add(series);
            }
            reader.Close();
            connection.Close();
            return list;
        }

        public async Task<IEnumerable<Category>> GetSeriesCategoriesAsync()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            string query = "SELECT ID, name FROM series_genres order by name";
            using var command = new SqliteCommand(query, connection);
            var reader = command.ExecuteReader();

            var list = new List<Category>();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string name = reader.GetString(1);
                // Process the data as needed
                list.Add(new Category
                {
                    Id = id,
                    Name = name,
                    ParentId = 0
                });
            }
            reader.Close();
            connection.Close();
            return list;
        }

        public async Task<IEnumerable<Movie>> GetSimiliarMovies(Movie? movie, System.Collections.ObjectModel.ObservableCollection<Movie> allMovies)
        {
            var list = new List<Movie>();
            if (movie == null) return list;

            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            if (movie.Similiar.Count == 0)
            {
                string query1 = "SELECT similiar " +
                                "FROM similiar_movies " +
                                "WHERE 1=1 " +
                               $"  AND tmdb_id = {movie.Id} ";
                using var command1 = new SqliteCommand(query1, connection);
                var saved_list = command1.ExecuteScalar();

                if (saved_list != null)
                {
                    string similiar = saved_list.ToString() ?? "";
                    if (!string.IsNullOrEmpty(similiar))
                        movie.Similiar = similiar.Split(',').Select(s => int.TryParse(s, out var smid) ? smid : 0).Where(smid => smid > 0).ToList();
                }
                else
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
                            var data = JsonSerializer.Deserialize<JsonElement>(json);
                            if (data.TryGetProperty("results", out JsonElement results))
                                foreach (var id in results.EnumerateArray())
                                {
                                    if (id.TryGetProperty("id", out var mid))
                                    {
                                        try
                                        {
                                            _ = await _http.GetStringAsync(BuildUrl("get_vod_info", $"vod_id={mid}"));
                                            if (!movie.Similiar.Contains(mid.GetInt32()))
                                                movie.Similiar.Add(mid.GetInt32());
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
                    query1 = $"INSERT or REPLACE into similiar_movies (tmdb_id, similiar) " +
                    $"VALUES ({movie.Id}, '{string.Join(",", movie.Similiar)}')";
                    using var command2 = new SqliteCommand(query1, connection);
                    command2.ExecuteNonQuery();
                }
            }

            if (!movie.Similiar.Contains(movie.Id))
            {
                movie.Similiar.Insert(0, movie.Id);
            }

            string query = "SELECT movies.tmdb_id, json(data), json(credits), updated " +
                            "FROM movies " +
                            "WHERE 1=1 " +
                           $"  AND movies.tmdb_id in ({string.Join(", ", movie.Similiar)}) " +
                            "order by release_date desc " +
                            "";
            using var command = new SqliteCommand(query, connection);
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string data = reader.GetString(1);
                string credits = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty;
                // Process the data as needed

                var simmovie = allMovies.FirstOrDefault(m => m.Id == id);
                if (simmovie == null)
                {
                    simmovie = CreateMovieFromJSON(id, data, credits);
                    if (simmovie != null)
                    {
                        allMovies.Add(simmovie);
                    }
                }
                if (simmovie != null)
                    list.Add(simmovie);
            }
            reader.Close();
            connection.Close();
            //return list.OrderBy(x => movie.Similiar.IndexOf(x.Id));
            return list.OrderByDescending(x => x.Id == movie.Id).ThenByDescending(x => x.ReleaseDate);
        }

        public async Task<IEnumerable<Series>> GetSimiliarSeries(Series? series, System.Collections.ObjectModel.ObservableCollection<Series> allSeries)
        {
            var list = new List<Series>();
            if (series == null) return list;
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            if (series.Similiar.Count == 0)
            {
                string query1 = "SELECT similiar " +
                                "FROM similiar_series " +
                                "WHERE 1=1 " +
                               $"  AND tmdb_id = {series.Id} ";
                using var command1 = new SqliteCommand(query1, connection);
                var saved_list = command1.ExecuteScalar();
                if (saved_list != null)
                {
                    string similiar = saved_list.ToString() ?? "";
                    if (!string.IsNullOrEmpty(similiar))
                        series.Similiar = similiar.Split(',').Select(s => int.TryParse(s, out var smid) ? smid : 0).Where(smid => smid > 0).ToList();
                }
                else
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
                                            _ = await _http.GetStringAsync(BuildUrl("get_series_info", $"series_id={mid}"));
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
                    query1 = $"INSERT or REPLACE into similiar_series (tmdb_id, similiar) " +
                            $"VALUES ({series.Id}, '{string.Join(",", series.Similiar)}')";
                        using var command2 = new SqliteCommand(query1, connection);
                        command2.ExecuteNonQuery();
                }
            }
            if (!series.Similiar.Contains(series.Id))
            {
                series.Similiar.Insert(0, series.Id);
            }

            string query = "SELECT series.tmdb_id, json(data), json(credits), updated " +
                            "FROM series " +
                            "WHERE 1=1 " +
                           $"  AND series.tmdb_id in ({string.Join(", ", series.Similiar)}) " +
                            "";
            using var command = new SqliteCommand(query, connection);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string data = reader.GetString(1);
                string credits = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty;
                var simseries = allSeries.FirstOrDefault(s => s.Id == id);
                if (simseries == null)
                {
                    simseries = CreateSeriesFromJSON(id, data, credits);
                    if (simseries != null)
                        allSeries.Add(simseries);
                }
                if (simseries != null)
                    list.Add(simseries);
            }

            //return list.OrderBy(x => series.Similiar.IndexOf(x.Id));
            return list.OrderByDescending(x => x.ReleaseDate);
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
    }
}
