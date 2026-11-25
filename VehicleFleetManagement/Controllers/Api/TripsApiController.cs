using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleFleetManagement.Data;
using VehicleFleetManagement.DTOs;
using VehicleFleetManagement.Models;
using VehicleFleetManagement.Services.Interfaces;

namespace VehicleFleetManagement.Controllers.Api
{
    /// <summary>
    /// RESTful Web API Controller for Trip dispatch, tracking, and lifecycle updates.
    /// Drivers can inspect only their assigned journeys, start trips, and complete trips.
    /// Admins maintain full dispatch, scheduling, and deletion privileges.
    /// </summary>
    [ApiController]
    [Route("api/trips")]
    [Produces("application/json")]
    [Authorize]
    public class TripsApiController : ControllerBase
    {
        private readonly ITripService _tripService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public TripsApiController(
            ITripService tripService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _tripService = tripService;
            _userManager = userManager;
            _context = context;
        }

        // ==========================================
        // 1. GET: api/trips
        // ==========================================
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<TripDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? status, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Driver Data Isolation: Authenticated drivers view ONLY their assigned journeys
            if (User.IsInRole(DbSeeder.RoleDriver))
            {
                var personalDriver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == user.Id);
                if (personalDriver == null)
                {
                    return Ok(ApiResponse<IEnumerable<TripDto>>.Ok(Enumerable.Empty<TripDto>(), "No driver profile linked."));
                }

                var driverTrips = await _tripService.GetDriverTripsAsync(personalDriver.DriverId);
                return Ok(ApiResponse<IEnumerable<TripDto>>.Ok(driverTrips.Select(MapToDto), "Assigned trips retrieved."));
            }

            // Admin: full operational fleet trips
            var trips = await _tripService.GetAllTripsAsync(search, status, fromDate, toDate, page, pageSize);
            return Ok(ApiResponse<IEnumerable<TripDto>>.Ok(trips.Select(MapToDto), "Trips retrieved successfully."));
        }

        // ==========================================
        // 2. GET: api/trips/{id}
        // ==========================================
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<TripDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetById(int id)
        {
            var trip = await _tripService.GetTripByIdAsync(id);
            if (trip == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Trip #{id} was not found."));
            }

            // Driver Data Isolation check
            if (User.IsInRole(DbSeeder.RoleDriver))
            {
                var user = await _userManager.GetUserAsync(User);
                var personalDriver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == user!.Id);
                if (personalDriver == null || trip.DriverId != personalDriver.DriverId)
                {
                    return StatusCode(403, ApiResponse<object>.Fail("Forbidden: You are not authorized to access another driver's trip records."));
                }
            }

            return Ok(ApiResponse<TripDto>.Ok(MapToDto(trip)));
        }

        // ==========================================
        // 3. POST: api/trips (Admin Scheduling)
        // ==========================================
        [HttpPost]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ProducesResponseType(typeof(ApiResponse<TripDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> Create([FromBody] CreateTripDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
            }

            var trip = new Trip
            {
                VehicleId = dto.VehicleId,
                DriverId = dto.DriverId,
                Source = dto.Source.Trim(),
                Destination = dto.Destination.Trim(),
                StartDate = dto.StartDate,
                ExpectedEndDate = dto.ExpectedEndDate,
                StartOdometer = dto.StartOdometer,
                Purpose = dto.Purpose.Trim(),
                TripStatus = TripStatusEnum.Scheduled,
                CreatedAt = DateTime.UtcNow
            };

            var (success, message) = await _tripService.CreateTripAsync(trip);
            if (!success)
            {
                return BadRequest(ApiResponse<object>.Fail(message));
            }

            var createdTrip = await _tripService.GetTripByIdAsync(trip.TripId);
            return CreatedAtAction(nameof(GetById), new { id = trip.TripId }, ApiResponse<TripDto>.Created(MapToDto(createdTrip ?? trip), message));
        }

        // ==========================================
        // 4. PUT: api/trips/{id} (Admin Modification)
        // ==========================================
        [HttpPut("{id:int}")]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ProducesResponseType(typeof(ApiResponse<TripDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTripDto dto)
        {
            if (id != dto.TripId)
            {
                return BadRequest(ApiResponse<object>.Fail("Route ID and payload TripId mismatch."));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
            }

            var existing = await _tripService.GetTripByIdAsync(id);
            if (existing == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Trip #{id} was not found."));
            }

            existing.VehicleId = dto.VehicleId;
            existing.DriverId = dto.DriverId;
            existing.Source = dto.Source.Trim();
            existing.Destination = dto.Destination.Trim();
            existing.StartDate = dto.StartDate;
            existing.ExpectedEndDate = dto.ExpectedEndDate;
            existing.StartOdometer = dto.StartOdometer;
            existing.Purpose = dto.Purpose.Trim();
            existing.TripStatus = dto.TripStatus.Trim();

            var (success, message) = await _tripService.UpdateTripAsync(existing);
            if (!success)
            {
                return BadRequest(ApiResponse<object>.Fail(message));
            }

            return Ok(ApiResponse<TripDto>.Ok(MapToDto(existing), message));
        }

        // ==========================================
        // 5. POST: api/trips/{id}/start
        // ==========================================
        [HttpPost("{id:int}/start")]
        [ProducesResponseType(typeof(ApiResponse<TripDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> StartTrip(int id, [FromBody] StartTripDto dto)
        {
            var trip = await _tripService.GetTripByIdAsync(id);
            if (trip == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Trip #{id} was not found."));
            }

            // Driver Data Isolation check
            if (User.IsInRole(DbSeeder.RoleDriver))
            {
                var user = await _userManager.GetUserAsync(User);
                var personalDriver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == user!.Id);
                if (personalDriver == null || trip.DriverId != personalDriver.DriverId)
                {
                    return StatusCode(403, ApiResponse<object>.Fail("Forbidden: You cannot start another driver's trip."));
                }
            }

            var (success, message) = await _tripService.StartTripAsync(id, dto.StartOdometer);
            if (!success)
            {
                return BadRequest(ApiResponse<object>.Fail(message));
            }

            var updatedTrip = await _tripService.GetTripByIdAsync(id);
            return Ok(ApiResponse<TripDto>.Ok(MapToDto(updatedTrip!), message));
        }

        // ==========================================
        // 6. POST: api/trips/{id}/complete
        // ==========================================
        [HttpPost("{id:int}/complete")]
        [ProducesResponseType(typeof(ApiResponse<TripDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> CompleteTrip(int id, [FromBody] CompleteTripDto dto)
        {
            var trip = await _tripService.GetTripByIdAsync(id);
            if (trip == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Trip #{id} was not found."));
            }

            // Driver Data Isolation check
            if (User.IsInRole(DbSeeder.RoleDriver))
            {
                var user = await _userManager.GetUserAsync(User);
                var personalDriver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == user!.Id);
                if (personalDriver == null || trip.DriverId != personalDriver.DriverId)
                {
                    return StatusCode(403, ApiResponse<object>.Fail("Forbidden: You cannot complete another driver's trip."));
                }
            }

            var (success, message) = await _tripService.CompleteTripAsync(id, dto.EndOdometer);
            if (!success)
            {
                return BadRequest(ApiResponse<object>.Fail(message));
            }

            var updatedTrip = await _tripService.GetTripByIdAsync(id);
            return Ok(ApiResponse<TripDto>.Ok(MapToDto(updatedTrip!), message));
        }

        // ==========================================
        // 7. DELETE: api/trips/{id}
        // ==========================================
        [HttpDelete("{id:int}")]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Delete(int id)
        {
            var trip = await _tripService.GetTripByIdAsync(id);
            if (trip == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Trip #{id} was not found."));
            }

            var (success, message) = await _tripService.DeleteTripAsync(id);
            if (!success)
            {
                return BadRequest(ApiResponse<object>.Fail(message));
            }

            return Ok(ApiResponse<object>.Ok(null, message));
        }

        private static TripDto MapToDto(Trip t)
        {
            return new TripDto
            {
                TripId = t.TripId,
                VehicleId = t.VehicleId,
                VehicleName = t.Vehicle?.VehicleName ?? "N/A",
                RegistrationNumber = t.Vehicle?.RegistrationNumber ?? "N/A",
                DriverId = t.DriverId,
                DriverName = t.Driver?.FullName ?? "N/A",
                Source = t.Source,
                Destination = t.Destination,
                StartDate = t.StartDate,
                ExpectedEndDate = t.ExpectedEndDate,
                ActualEndDate = t.ActualEndDate,
                StartOdometer = t.StartOdometer,
                EndOdometer = t.EndOdometer,
                Distance = t.Distance,
                Purpose = t.Purpose,
                TripStatus = t.TripStatus,
                CreatedAt = t.CreatedAt
            };
        }
    }
}
