using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Tutorial8.Exceptions;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly ITripsService _tripsService;

        public TripsController(ITripsService tripsService)
        {
            _tripsService = tripsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTrips()
        {
            var trips = await _tripsService.GetTrips();
            return Ok(trips);
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTrip(int id)
        {
            return Ok();
        }
        [HttpGet("/api/clients/{id}/trips")]
        public async Task<IActionResult> GetClientTrips([FromRoute] int id)
        {
            var trips = await _tripsService.GetClientTrips(id);
            if (!trips.Any())
            {
                throw new NotFoundException($"Brak wycieczek dla klienta o ID {id}");
            }

            return Ok(trips);
        }
        [HttpPost("/api/clients")]
        public async Task<IActionResult> AddClient([FromBody] ClientDTO client)
        {
            await _tripsService.AddClientAsync(client);
            return Created("", null);
        }
        [HttpPut("/api/clients/{id}/trips/{tripId}")]
        public async Task<IActionResult> EnrollClient(int id, int tripId)
        {
            try
            {
                await _tripsService.EnrollClientToTrip(id, tripId);
                return NoContent(); // 204
            }
            catch (NotFoundException e)
            {
                return NotFound(e.Message);
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(e.Message);
            }
        }
        [HttpDelete("/api/clients/{id}/trips/{tripId}")]
        public async Task<IActionResult> RemoveClientFromTrip(int id, int tripId)
        {
            try
            {
                await _tripsService.RemoveClientFromTrip(id, tripId);
                return NoContent(); // 204
            }
            catch (NotFoundException e)
            {
                return NotFound(e.Message);
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(e.Message);
            }
        }


    }
    
}
