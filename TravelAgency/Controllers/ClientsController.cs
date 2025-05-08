using Microsoft.AspNetCore.Mvc;
using TravelAgencyApi.Models;
using TravelAgencyApi.Services;

namespace TravelAgencyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _clientService;

        public ClientsController(IClientService clientService)
        {
            _clientService = clientService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] Client client)
        {
            if (string.IsNullOrEmpty(client.FirstName) || string.IsNullOrEmpty(client.LastName) ||
                string.IsNullOrEmpty(client.Email))
            {
                return BadRequest("FirstName, LastName, and Email are required.");
            }

            try
            {
                int newClientId = await _clientService.CreateClientAsync(client);
                return CreatedAtAction(nameof(GetClientTrips), new { id = newClientId },
                    new { IdClient = newClientId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}/trips")]
        public async Task<IActionResult> GetClientTrips(int id)
        {
            try
            {
                bool clientExists = await _clientService.ClientExistsAsync(id);
                if (!clientExists)
                {
                    return NotFound($"Client with id {id} not found.");
                }

                var trips = await _clientService.GetClientTripsAsync(id);
                
                if (trips is List<ClientTrip> tripList && tripList.Count == 0)
                {
                    return Ok(new { Message = $"Client with id {id} has no trips." });
                }

                return Ok(trips);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{id}/trips/{tripId}")]
        public async Task<IActionResult> RegisterClientForTrip(int id, int tripId,
            [FromBody] RegistrationRequest request)
        {
            try
            {
                bool clientExists = await _clientService.ClientExistsAsync(id);
                if (!clientExists)
                {
                    return NotFound($"Client with id {id} not found.");
                }

                bool result = await _clientService.RegisterClientForTripAsync(id, tripId);

                if (!result)
                {
                    return BadRequest(
                        "Failed to register client for trip. The trip may not exist, the client may already be registered, or the trip may be at full capacity.");
                }

                return Ok(new { Message = "Client successfully registered for the trip." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpDelete("{id}/trips/{tripId}")]
        public async Task<IActionResult> RemoveClientFromTrip(int id, int tripId)
        {
            try
            {
                bool result = await _clientService.RemoveClientFromTripAsync(id, tripId);

                if (!result)
                {
                    return NotFound("Client is not registered for this trip.");
                }

                return Ok(new { Message = "Client's registration for the trip has been removed." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}