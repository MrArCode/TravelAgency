using TravelAgencyApi.Models;

namespace TravelAgencyApi.Services
{
    public interface ITripService
    {
        Task<IEnumerable<Trip>> GetAllTripsAsync();
    }
}