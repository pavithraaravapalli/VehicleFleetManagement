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
    /// RESTful Web API Controller for Driver workforce records.
    /// Strictly protects driver data isolation: drivers can only view their own profile.
    /// Admins maintain full CRUD management.
    /// </summary>
    [ApiController]
    [Route("api/drivers")]
    [Produces("application/json")]
    [Authorize]
    public class DriversApiController : ControllerBase
    {
        private readonly IDriverService _driverService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DriversApiController(
            IDriverService driverService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _driverService = driverService;
            _userManager = userManager;
            _context = context;
        }

        // ==========================================
        // 1. GET: api/drivers
        // ==========================================
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<DriverDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Driver Data Isolation: If authenticated as Driver, return only their own profile
            if (User.IsInRole(DbSeeder.RoleDriver))
            {
                var personalDriver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == user.Id);
                if (personalDriver == null)
                {
                    return Ok(ApiResponse<IEnumerable<DriverDto>>.Ok(Enumerable.Empty<DriverDto>(), "No driver profile linked."));
                }
                return Ok(ApiResponse<IEnumerable<DriverDto>>.Ok(new[] { MapToDto(personalDriver) }));
            }

            // Admin access: return paginated fleet drivers
            var drivers = await _driverService.GetAllDriversAsync(search, status, page, pageSize);
            var dtos = drivers.Select(MapToDto);
            return Ok(ApiResponse<IEnumerable<DriverDto>>.Ok(dtos, "Drivers retrieved successfully."));
        }

        // ==========================================
        // 2. GET: api/drivers/{id}
        // ==========================================
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<DriverDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 403)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetById(int id)
        {
            var driver = await _driverService.GetDriverByIdAsync(id);
            if (driver == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Driver with ID #{id} was not found."));
            }

            // Driver Data Isolation: Prevent accessing other drivers' records
            if (User.IsInRole(DbSeeder.RoleDriver))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null || driver.UserId != currentUser.Id)
                {
                    return StatusCode(403, ApiResponse<object>.Fail("Forbidden: You are not authorized to view another driver's records."));
                }
            }

            return Ok(ApiResponse<DriverDto>.Ok(MapToDto(driver)));
        }

        // ==========================================
        // 3. POST: api/drivers
        // ==========================================
        [HttpPost]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ProducesResponseType(typeof(ApiResponse<DriverDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> Create([FromBody] CreateDriverDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
            }

            var driver = new Driver
            {
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                LicenseNumber = dto.LicenseNumber.Trim().ToUpperInvariant(),
                LicenseExpiryDate = dto.LicenseExpiryDate,
                Address = dto.Address.Trim(),
                JoiningDate = dto.JoiningDate,
                AvailabilityStatus = DriverStatus.Available
            };

            var (success, message) = await _driverService.CreateDriverAsync(driver);
            if (!success)
            {
                return BadRequest(ApiResponse<object>.Fail(message));
            }

            var responseDto = MapToDto(driver);
            return CreatedAtAction(nameof(GetById), new { id = driver.DriverId }, ApiResponse<DriverDto>.Created(responseDto, message));
        }

        // ==========================================
        // 4. PUT: api/drivers/{id}
        // ==========================================
        [HttpPut("{id:int}")]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ProducesResponseType(typeof(ApiResponse<DriverDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDriverDto dto)
        {
            if (id != dto.DriverId)
            {
                return BadRequest(ApiResponse<object>.Fail("Route ID and payload DriverId mismatch."));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
            }

            var existing = await _driverService.GetDriverByIdAsync(id);
            if (existing == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Driver with ID #{id} was not found."));
            }

            existing.FullName = dto.FullName.Trim();
            existing.Email = dto.Email.Trim().ToLowerInvariant();
            existing.PhoneNumber = dto.PhoneNumber.Trim();
            existing.LicenseNumber = dto.LicenseNumber.Trim().ToUpperInvariant();
            existing.LicenseExpiryDate = dto.LicenseExpiryDate;
            existing.Address = dto.Address.Trim();
            existing.JoiningDate = dto.JoiningDate;
            existing.AvailabilityStatus = dto.AvailabilityStatus.Trim();

            var (success, message) = await _driverService.UpdateDriverAsync(existing);
            if (!success)
            {
                return BadRequest(ApiResponse<object>.Fail(message));
            }

            return Ok(ApiResponse<DriverDto>.Ok(MapToDto(existing), message));
        }

        // ==========================================
        // 5. DELETE: api/drivers/{id}
        // ==========================================
        [HttpDelete("{id:int}")]
        [Authorize(Roles = DbSeeder.RoleAdmin)]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _driverService.GetDriverByIdAsync(id);
            if (existing == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Driver with ID #{id} was not found."));
            }

            var (success, message) = await _driverService.DeleteOrDeactivateDriverAsync(id);
            if (!success)
            {
                return BadRequest(ApiResponse<object>.Fail(message));
            }

            return Ok(ApiResponse<object>.Ok(null, message));
        }

        private static DriverDto MapToDto(Driver d)
        {
            return new DriverDto
            {
                DriverId = d.DriverId,
                FullName = d.FullName,
                Email = d.Email,
                PhoneNumber = d.PhoneNumber,
                LicenseNumber = d.LicenseNumber,
                LicenseExpiryDate = d.LicenseExpiryDate,
                Address = d.Address,
                JoiningDate = d.JoiningDate,
                AvailabilityStatus = d.AvailabilityStatus
            };
        }
    }
}
