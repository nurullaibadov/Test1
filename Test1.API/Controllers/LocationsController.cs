using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Test1.Application.DTOs.Location;
using Test1.Application.Interfaces.Repositories;
using Test1.Domain.Entities;

namespace Test1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class LocationsController : ControllerBase
    {
        private readonly ILocationRepository _locationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public LocationsController(ILocationRepository locationRepository, IUnitOfWork unitOfWork)
        {
            _locationRepository = locationRepository;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Get all locations
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Location>>> GetAllLocations()
        {
            try
            {
                var locations = await _locationRepository.GetAllAsync();
                return Ok(locations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving locations", error = ex.Message });
            }
        }

        /// <summary>
        /// Get location by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Location>> GetLocationById(Guid id)
        {
            try
            {
                var location = await _locationRepository.GetByIdAsync(id);

                if (location == null)
                {
                    return NotFound(new { message = "Location not found" });
                }

                return Ok(location);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the location", error = ex.Message });
            }
        }

        /// <summary>
        /// Get active locations
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<Location>>> GetActiveLocations()
        {
            try
            {
                var locations = await _locationRepository.GetActiveLocationsAsync();
                return Ok(locations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving active locations", error = ex.Message });
            }
        }

        /// <summary>
        /// Get nearest location based on coordinates
        /// </summary>
        [HttpGet("nearest")]
        public async Task<ActionResult<Location>> GetNearestLocation([FromQuery] double latitude, [FromQuery] double longitude)
        {
            try
            {
                if (latitude < -90 || latitude > 90)
                {
                    return BadRequest(new { message = "Latitude must be between -90 and 90" });
                }

                if (longitude < -180 || longitude > 180)
                {
                    return BadRequest(new { message = "Longitude must be between -180 and 180" });
                }

                var location = await _locationRepository.GetNearestLocationAsync(latitude, longitude);

                if (location == null)
                {
                    return NotFound(new { message = "No locations found" });
                }

                // Calculate distance
                var distance = CalculateDistance(latitude, longitude, location.Latitude, location.Longitude);

                return Ok(new
                {
                    location,
                    distanceInKm = Math.Round(distance, 2)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while finding nearest location", error = ex.Message });
            }
        }

        /// <summary>
        /// Search locations by name or city
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Location>>> SearchLocations([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest(new { message = "Search term is required" });
                }

                var allLocations = await _locationRepository.GetAllAsync();
                var filteredLocations = allLocations.Where(l =>
                    l.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    l.City.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (l.Address != null && l.Address.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                return Ok(filteredLocations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching locations", error = ex.Message });
            }
        }

        /// <summary>
        /// Get locations by city
        /// </summary>
        [HttpGet("city/{city}")]
        public async Task<ActionResult<IEnumerable<Location>>> GetLocationsByCity(string city)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    return BadRequest(new { message = "City is required" });
                }

                var allLocations = await _locationRepository.GetAllAsync();
                var cityLocations = allLocations.Where(l =>
                    l.City.Equals(city, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                return Ok(cityLocations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving locations by city", error = ex.Message });
            }
        }

        /// <summary>
        /// Get locations within radius
        /// </summary>
        [HttpGet("within-radius")]
        public async Task<ActionResult<IEnumerable<object>>> GetLocationsWithinRadius(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double radiusInKm = 10)
        {
            try
            {
                if (latitude < -90 || latitude > 90)
                {
                    return BadRequest(new { message = "Latitude must be between -90 and 90" });
                }

                if (longitude < -180 || longitude > 180)
                {
                    return BadRequest(new { message = "Longitude must be between -180 and 180" });
                }

                if (radiusInKm <= 0 || radiusInKm > 1000)
                {
                    return BadRequest(new { message = "Radius must be between 0 and 1000 km" });
                }

                var allLocations = await _locationRepository.GetActiveLocationsAsync();
                var locationsWithDistance = allLocations.Select(l => new
                {
                    location = l,
                    distanceInKm = CalculateDistance(latitude, longitude, l.Latitude, l.Longitude)
                })
                .Where(x => x.distanceInKm <= radiusInKm)
                .OrderBy(x => x.distanceInKm)
                .Select(x => new
                {
                    x.location,
                    distanceInKm = Math.Round(x.distanceInKm, 2)
                })
                .ToList();

                return Ok(locationsWithDistance);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching locations", error = ex.Message });
            }
        }

        /// <summary>
        /// Create new location
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<Location>> CreateLocation([FromBody] CreateLocationDto locationDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if location with same name and city already exists
                var allLocations = await _locationRepository.GetAllAsync();
                var existingLocation = allLocations.FirstOrDefault(l =>
                    l.Name.Equals(locationDto.Name, StringComparison.OrdinalIgnoreCase) &&
                    l.City.Equals(locationDto.City, StringComparison.OrdinalIgnoreCase));

                if (existingLocation != null)
                {
                    return Conflict(new { message = "Location with this name already exists in the city" });
                }

                var location = new Location
                {
                    Id = Guid.NewGuid(),
                    Name = locationDto.Name,
                    Address = locationDto.Address,
                    City = locationDto.City,
                    State = locationDto.State,
                    Country = locationDto.Country,
                    PostalCode = locationDto.PostalCode,
                    Latitude = locationDto.Latitude,
                    Longitude = locationDto.Longitude,
                    PhoneNumber = locationDto.PhoneNumber,
                    Email = locationDto.Email,
                    OpeningHours = locationDto.OpeningHours,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _locationRepository.AddAsync(location);
                await _unitOfWork.SaveChangesAsync();

                return CreatedAtAction(nameof(GetLocationById), new { id = location.Id }, location);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the location", error = ex.Message });
            }
        }

        /// <summary>
        /// Update location
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> UpdateLocation(Guid id, [FromBody] UpdateLocationDto locationDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var location = await _locationRepository.GetByIdAsync(id);
                if (location == null)
                {
                    return NotFound(new { message = "Location not found" });
                }

                // Update fields
                if (!string.IsNullOrEmpty(locationDto.Name))
                    location.Name = locationDto.Name;

                if (!string.IsNullOrEmpty(locationDto.Address))
                    location.Address = locationDto.Address;

                if (!string.IsNullOrEmpty(locationDto.City))
                    location.City = locationDto.City;

                if (!string.IsNullOrEmpty(locationDto.State))
                    location.State = locationDto.State;

                if (!string.IsNullOrEmpty(locationDto.Country))
                    location.Country = locationDto.Country;

                if (!string.IsNullOrEmpty(locationDto.PostalCode))
                    location.PostalCode = locationDto.PostalCode;

                if (locationDto.Latitude.HasValue)
                    location.Latitude = locationDto.Latitude.Value;

                if (locationDto.Longitude.HasValue)
                    location.Longitude = locationDto.Longitude.Value;

                if (!string.IsNullOrEmpty(locationDto.PhoneNumber))
                    location.PhoneNumber = locationDto.PhoneNumber;

                if (!string.IsNullOrEmpty(locationDto.Email))
                    location.Email = locationDto.Email;

                if (!string.IsNullOrEmpty(locationDto.OpeningHours))
                    location.OpeningHours = locationDto.OpeningHours;

                location.UpdatedAt = DateTime.UtcNow;

                await _locationRepository.UpdateAsync(location);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Location updated successfully", location });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the location", error = ex.Message });
            }
        }

        /// <summary>
        /// Activate/Deactivate location
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> ToggleLocationStatus(Guid id)
        {
            try
            {
                var location = await _locationRepository.GetByIdAsync(id);
                if (location == null)
                {
                    return NotFound(new { message = "Location not found" });
                }

                location.IsActive = !location.IsActive;
                location.UpdatedAt = DateTime.UtcNow;

                await _locationRepository.UpdateAsync(location);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Location {(location.IsActive ? "activated" : "deactivated")} successfully",
                    isActive = location.IsActive
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating location status", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete location
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> DeleteLocation(Guid id)
        {
            try
            {
                var location = await _locationRepository.GetByIdAsync(id);
                if (location == null)
                {
                    return NotFound(new { message = "Location not found" });
                }

                await _locationRepository.DeleteAsync(location);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Location deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the location", error = ex.Message });
            }
        }

        /// <summary>
        /// Get location statistics
        /// </summary>
        [HttpGet("{id}/statistics")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> GetLocationStatistics(Guid id)
        {
            try
            {
                var location = await _locationRepository.GetByIdAsync(id);
                if (location == null)
                {
                    return NotFound(new { message = "Location not found" });
                }

                // Note: You would need to add methods to get these statistics
                // This is a placeholder showing what statistics could be included
                var statistics = new
                {
                    locationId = location.Id,
                    locationName = location.Name,
                    city = location.City,
                    isActive = location.IsActive,
                    createdAt = location.CreatedAt
                    // Add more statistics as needed:
                    // totalCarsAtLocation = ...
                    // totalBookingsFromLocation = ...
                    // totalBookingsToLocation = ...
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving location statistics", error = ex.Message });
            }
        }

        /// <summary>
        /// Calculate distance between two coordinates using Haversine formula
        /// </summary>
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth's radius in kilometers

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = R * c;

            return distance;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}
