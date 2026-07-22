using System.Collections.Generic;
using System.Threading.Tasks;
using XtreamIPTV.Models;

namespace XtreamIPTV.Services
{
    public interface IIPTVService
    {
<<<<<<< HEAD
        Task<IEnumerable<Category>> GetLiveCategoriesAsync();
        Task<IEnumerable<Live>> GetLiveAsync();
        Task<IEnumerable<Category>> GetMovieCategoriesAsync();
        Task<Movie> GetMovieDetailAsync(Movie movie);
        Task<IEnumerable<Movie>> GetMoviesAsync();
        Task<IEnumerable<Season>> GetSeasonsAsync(Series series);
        Task<IEnumerable<Series>> GetSeriesAsync();
        Task<IEnumerable<Category>> GetSeriesCategoriesAsync();
        Task<IEnumerable<Movie>> GetSimiliarMovies(Movie? movie, System.Collections.ObjectModel.ObservableCollection<Movie> allMovies);
        Task<IEnumerable<Series>> GetSimiliarSeries(Series? series, System.Collections.ObjectModel.ObservableCollection<Series> allSeries);
=======
        Task<List<Category>> GetMovieCategoriesAsync();
        Task<Movie> GetMovieDetailAsync(Movie movie);
        Task<List<Movie>> GetMoviesAsync();
        Task<List<Season>> GetSeasonsAsync(Series series);
        Task<List<Series>> GetSeriesAsync();
        Task<List<Category>> GetSeriesCategoriesAsync();
>>>>>>> 363c62477059520f3013ca59559e4972dc8805c4
    }
}