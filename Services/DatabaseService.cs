using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using XtreamIPTV.Models;

namespace XtreamIPTV.Services
{
    public class DatabaseService : IIPTVService
    {
        private static readonly string connectionString = "Data Source=D:\\tv_data\\database.db;";

        public DatabaseService() 
        { 

        }

        public async Task<List<Category>> GetMovieCategoriesAsync()
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
            return list;
        }

        public async Task<Movie> GetMovieDetailAsync(Movie movie)
        {
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

        public async Task<List<Movie>> GetMoviesAsync()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            string query = "SELECT movies.tmdb_id, json(data), json(credits), updated " +
                            "FROM movies " +
                            "WHERE 1=1 " +
                            $"  AND release_date < '{DateTime.Now:yyyy-MM-dd}' " +
                            "  AND poster_path is not null " +
                            "  AND (CAST(data->'$.runtime' as integer) > 20 or " +
                            "       CAST(data->'$.runtime' as integer) = 0 or " +
                            "       data->'$.runtime' is null) " +
                            "order by popularity desc, release_date desc " +
                            //"LIMIT 100" +
                            "";
            using var command = new SqliteCommand(query, connection);
            var reader = command.ExecuteReader();

            var list = new List<Movie>();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string data = reader.GetString(1);
                string credits = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty;
                // Process the data as needed
                dynamic? obj1 = JsonSerializer.Deserialize<JsonElement>(data);
                string backdrop = obj1.TryGetProperty("backdrop_path", out JsonElement bp) ? bp.ToString() : "";
                string title = obj1.TryGetProperty("title", out JsonElement t) ? t.ToString() : "";
                string poster = obj1.TryGetProperty("poster_path", out JsonElement p) ? p.ToString() : "";
                string release_date = obj1.TryGetProperty("release_date", out JsonElement rd) ? rd.ToString() : "";
                string overview = obj1.TryGetProperty("overview", out JsonElement o) ? o.ToString() : "";
                string rating = obj1.TryGetProperty("vote_average", out JsonElement va) ? va.ToString() : "0";  
                string similiar = string.Empty;
                var categoryId = 0;

                List<string> cast = [];
                List<string> director = [];

                if (string.IsNullOrEmpty(poster) || string.IsNullOrEmpty(release_date)) 
                    continue;

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
                string langs = string.Empty;
                if (obj1.TryGetProperty("spoken_languages", out JsonElement spoken_languages))
                {
                    foreach (var language in spoken_languages.EnumerateArray())
                    {
                        if (!string.IsNullOrEmpty(langs))
                            langs += ", ";
                        langs += language.GetProperty("english_name");
                    }
                }
                if (!string.IsNullOrEmpty(langs) && langs != "English")
                {
                    overview = $"({langs}) {overview}";
                }
                poster = $"https://image.tmdb.org/t/p/original{poster}";
                if (!String.IsNullOrEmpty(backdrop))
                    backdrop = $"https://image.tmdb.org/t/p/original{backdrop}";

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

                list.Add(new Movie
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
                    CastInfo = $"Cast: {string.Join(", ",cast)}",
                    DirectorInfo = $"Director: {string.Join(", ", director)}",
                    //Similiar = similiar,
                    //UpdateNeeded = updateNeeded,
                });
            }
            reader.Close();
            return list;
        }

        public async Task<List<Season>> GetSeasonsAsync(Series series)
        {
            var list = new List<Season>();
            return list;
        }

        public async Task<List<Series>> GetSeriesAsync()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            string query = "SELECT tmdb_id, json(data), json(credits), updated " +
                            "FROM series " +
                            "WHERE 1=1 " +
                            $"  AND  data->>'$.first_air_date' < '{DateTime.Now:yyyy-MM-dd}' " +
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
                string data = reader.GetString(1);
                string credits = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty;
                // Process the data as needed
                dynamic? obj1 = JsonSerializer.Deserialize<JsonElement>(data);
                string backdrop = obj1.TryGetProperty("backdrop_path", out JsonElement bp) ? bp.ToString() : "";
                string title = obj1.TryGetProperty("name", out JsonElement t) ? t.ToString() : "";
                string poster = obj1.TryGetProperty("poster_path", out JsonElement p) ? p.ToString() : "";
                string release_date = obj1.TryGetProperty("first_air_date", out JsonElement rd) ? rd.ToString() : "";
                string last_air_date = obj1.TryGetProperty("last_air_date", out JsonElement lad) ? lad.ToString() : "";
                string overview = obj1.TryGetProperty("overview", out JsonElement o) ? o.ToString() : "";
                string rating = obj1.TryGetProperty("vote_average", out JsonElement va) ? va.ToString() : "0";
                string similiar = string.Empty;
                var categoryId = 0;

                List<string> cast = [];
                List<string> director = [];

                if (string.IsNullOrEmpty(poster) || string.IsNullOrEmpty(release_date))
                    continue;

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
                string langs = string.Empty;
                if (obj1.TryGetProperty("spoken_languages", out JsonElement spoken_languages))
                {
                    foreach (var language in spoken_languages.EnumerateArray())
                    {
                        if (!string.IsNullOrEmpty(langs))
                            langs += ", ";
                        langs += language.GetProperty("english_name");
                    }
                }
                if (!string.IsNullOrEmpty(langs) && langs != "English")
                {
                    overview = $"({langs}) {overview}";
                }
                poster = $"https://image.tmdb.org/t/p/original{poster}";
                if (!String.IsNullOrEmpty(backdrop))
                    backdrop = $"https://image.tmdb.org/t/p/original{backdrop}";

                string runtime = string.Empty;
                if (obj1.TryGetProperty("episode_run_time", out JsonElement rt))
                {
                    //runtime = getRunTime(int.Parse($"{rt.EnumerateArray().FirstOrDefault()}"));
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

                DateTime.TryParse(release_date, out DateTime releaseDate);
                long ticks = releaseDate.Year * 1000000;
                ticks += releaseDate.Month * 1000;
                ticks += releaseDate.Day;

                list.Add(new Series
                {
                    Id = id,
                    Title = $"{title} ({release_date[..4]})",
                    ReleaseDate = releaseDate,
                    Backdrop = backdrop,
                    Poster = poster,
                    Plot = overview,
                    LastModified = ticks,
                    CategoryId = categoryId,
                    //ReleaseInfo = $"{release_date} * {age}{genre}{runtime}",
                    Rating = double.TryParse(rating, out var rat) ? rat : 0.0,
                    CastInfo = $"Cast: {string.Join(", ", cast)}",
                    DirectorInfo = $"Director: {string.Join(", ", director)}",
                    
                    //Similiar = similiar,
                    //UpdateNeeded = updateNeeded,
                });
            }
            reader.Close();
            return list;
        }

        public async Task<List<Category>> GetSeriesCategoriesAsync()
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
            return list;
        }
    }
}
