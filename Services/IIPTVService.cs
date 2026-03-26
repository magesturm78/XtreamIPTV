using System.Collections.Generic;
using System.Threading.Tasks;
using XtreamIPTV.Models;

namespace XtreamIPTV.Services
{
    public interface IIPTVService
    {
        Task<List<Category>> GetMovieCategoriesAsync();
        Task<Movie> GetMovieDetailAsync(Movie movie);
        Task<List<Movie>> GetMoviesAsync();
        Task<List<Season>> GetSeasonsAsync(Series series);
        Task<List<Series>> GetSeriesAsync();
        Task<List<Category>> GetSeriesCategoriesAsync();
    }
}