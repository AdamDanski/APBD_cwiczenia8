using Tutorial8.Controllers;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface ITripsService
{
    Task<List<TripDTO>> GetTrips();
    Task<List<ClientTripDTO>> GetClientTrips(int clientId);
    Task AddClientAsync(ClientDTO client);
    Task EnrollClientToTrip(int clientId, int tripId);
    Task RemoveClientFromTrip(int clientId, int tripId);

}