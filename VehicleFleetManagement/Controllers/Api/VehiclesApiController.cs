using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleFleetManagement.Data;
using VehicleFleetManagement.DTOs;
using VehicleFleetManagement.Models;
using VehicleFleetManagement.Services.Interfaces;

namespace VehicleFleetManagement.Controllers.Api
{
    /// <summary>
    /// RESTful Web API Controller for Vehicle asset lifecycle operations.
    /// Exposes standard CRUD operations, attribute routing, pagination, and status checks.
    /// </summary>
    [ApiController]
    [Route("api/vehicles")]
    [Produces("application/json")]
    [Authorize]
    public class VehiclesApiController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;

        public VehiclesApiController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        // ==========================================
        // 1. GET: api/vehicles
        // ==========================================
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<VehicleDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? type, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var vehicles = await _vehicleService.GetAllVehiclesAsync(search, type, status, page, pageSize);
            var dtos = vehicles.Select(MapToDto);
            return Ok(ApiResponse<IEnumerable<VehicleDto>>.Ok(dtos, "Vehicles retrieved successfully."));
        }

        // ==========================================
        // 2. GET: api/vehicles/{id}
        // ==========================================
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<VehicleDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetById(int id)
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id);
            if (vehicle == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Vehicle with ID #{id} was not found."));
            }

            return Ok(ApiResponse<VehicleDto>.Ok(MapToDto(vehicle)));
        }

        // ==========================================
        // 3. POST: api/vehicles
        // ==========================================
        [HttpPost]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ProducesResponseType(typeof(ApiResponse<VehicleDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> Create([FromBody] CreateVehicleDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
            }

            var vehicle = new Vehicle
            {
                RegistrationNumber = dto.RegistrationNumber.Trim().ToUpperInvariant(),
                VehicleName = dto.VehicleName.Trim(),
                Model = dto.Model.Trim(),
                VehicleType = dto.VehicleType.Trim(),
                FuelType = dto.FuelType.Trim(),
                ManufacturingYear = dto.ManufacturingYear,
                SeatingCapacity = dto.SeatingCapacity,
                CurrentOdometer = dto.CurrentOdometer,
                RegistrationDate = dto.RegistrationDate,
                InsuranceExpiryDate = dto.InsuranceExpiryDate,
                Status = VehicleStatus.Available,
                CreatedAt = DateTime.UtcNow
            };

            var (success, message) = await _vehicleService.CreateVehicleAsync(vehicle);
            if (!success)
            {
                return BadRequest(ApiResponse<object>.Fail(message));
            }

            var responseDto = MapToDto(vehicle);
            return CreatedAtAction(nameof(GetById), new { id = vehicle.VehicleId }, ApiResponse<VehicleDto>.Created(responseDto, message));
        }

        // ==========================================
        // 4. PUT: api/vehicles/{id}
        // ==========================================
        [HttpPut("{id:int}")]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ProducesResponseType(typeof(ApiResponse<VehicleDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateVehicleDto dto)
        {
            if (id != dto.VehicleId)
            {
                return BadRequest(ApiResponse<object>.Fail("Route ID and payload VehicleId mismatch."));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
            }

            var existing = await _vehicleService.GetVehicleByIdAsync(id);
            if (existing == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Vehicle with ID #{id} was not found."));
            }

            existing.RegistrationNumber = dto.RegistrationNumber.Trim().ToUpperInvariant();
            existing.VehicleName = dto.VehicleName.Trim();
            existing.Model = dto.Model.Trim();
            existing.VehicleType = dto.VehicleType.Trim();
            existing.FuelType = dto.FuelType.Trim();
            existing.ManufacturingYear = dto.ManufacturingYear;
            existing.SeatingCapacity = dto.SeatingCapacity;
            existing.CurrentOdometer = dto.CurrentOdometer;
            existing.RegistrationDate = dto.RegistrationDate;
            existing.InsuranceExpiryDate = dto.InsuranceExpiryDate;
            existing.Status = dto.Status.Trim();

            var (success, message) = await _vehicleService.UpdateVehicleAsync(existing);
            if (!success)
            {
                return BadRequest(ApiResponse<object>.Fail(message));
            }

            return Ok(ApiResponse<VehicleDto>.Ok(MapToDto(existing), message));
        }

        // ==========================================
        // 5. DELETE: api/vehicles/{id}
        // ==========================================
        [HttpDelete("{id:int}")]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _vehicleService.GetVehicleByIdAsync(id);
            if (existing == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Vehicle with ID #{id} was not found."));
            }

            var (success, message) = await _vehicleService.DeleteVehicleAsync(id);
            if (!success)
            {
                return BadRequest(ApiResponse<object>.Fail(message));
            }

            return Ok(ApiResponse<object>.Ok(null, message));
        }

        private static VehicleDto MapToDto(Vehicle v)
        {
            return new VehicleDto
            {
                VehicleId = v.VehicleId,
                RegistrationNumber = v.RegistrationNumber,
                VehicleName = v.VehicleName,
                Model = v.Model,
                VehicleType = v.VehicleType,
                FuelType = v.FuelType,
                ManufacturingYear = v.ManufacturingYear,
                SeatingCapacity = v.SeatingCapacity,
                CurrentOdometer = v.CurrentOdometer,
                RegistrationDate = v.RegistrationDate,
                InsuranceExpiryDate = v.InsuranceExpiryDate,
                Status = v.Status,
                CreatedAt = v.CreatedAt
            };
        }
    }
}
