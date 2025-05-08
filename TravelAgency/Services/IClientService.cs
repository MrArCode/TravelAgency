using TravelAgencyApi.Models;

namespace TravelAgencyApi.Services
{
    public interface IClientService
    {
        Task<int> CreateClientAsync(Client client);
        Task<IEnumerable<ClientTrip>> GetClientTripsAsync(int clientId);
        Task<bool> RegisterClientForTripAsync(int clientId, int tripId);
        Task<bool> RemoveClientFromTripAsync(int clientId, int tripId);
        Task<bool> ClientExistsAsync(int clientId);
    }
}