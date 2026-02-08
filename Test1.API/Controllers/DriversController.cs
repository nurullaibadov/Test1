using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Test1.Application.DTOs.Drive;
using Test1.Application.Interfaces.Repositories;
using Test1.Domain.Entities;
using Test1.Domain.Enums;

namespace Test1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]

    public class DriversController : ControllerBase
    {
        private readonly IDriverRepository _driverRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DriversController(
            IDriverRepository driverRepository,
            IUnitOfWork unitOfWork)
        {
            _driverRepository = driverRepository;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Get all drivers (Admin only)
        /// </summary>
        [HttpGet]
  [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<IEnumerable<Driver>>> GetAll()
        {
            try
            {
                var drivers = await _driverRepository.GetAllAsync();
                return Ok(drivers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving drivers", error = ex.Message });
            }
        }

        /// <summary>
        /// Get driver by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Driver>> GetById(Guid id)
        {
            try
            {
                var driver = await _driverRepository.GetByIdAsync(id);
                if (driver == null)
                    return NotFound(new { message = "Driver not found" });

                return Ok(driver);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the driver", error = ex.Message });
            }
        }

        /// <summary>
        /// Create new driver (Admin only)
        /// </summary>
        [HttpPost]
  [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> Create([FromBody] CreateDriverDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var driver = new Driver
                {
                    Id = Guid.NewGuid(),
                    UserId = dto.UserId,
                    LicenseNumber = dto.LicenseNumber,
                    LicenseIssueDate = dto.LicenseIssueDate,
                    LicenseExpiryDate = dto.LicenseExpiryDate,
                    YearsOfExperience = dto.YearsOfExperience,
                    Status = DriverStatus.OffDuty,
                    IsApproved = false,
                    IsVerified = false,
                    AverageRating = 0,
                    TotalTrips = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _driverRepository.AddAsync(driver);
                await _unitOfWork.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById),
                    new { id = driver.Id },
                    driver);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the driver", error = ex.Message });
            }
        }

        /// <summary>
        /// Update driver status
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<ActionResult> UpdateStatus(
            Guid id,
            [FromBody] UpdateDriverStatusDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var driver = await _driverRepository.GetByIdAsync(id);
                if (driver == null)
                    return NotFound(new { message = "Driver not found" });

                driver.Status = dto.Status;
                driver.UpdatedAt = DateTime.UtcNow;

                await _driverRepository.UpdateAsync(driver);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Driver status updated successfully", status = driver.Status });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating driver status", error = ex.Message });
            }
        }

        /// <summary>
        /// Update driver rating
        /// FIX: (decimal) cast added to convert double to decimal
        /// </summary>
        [HttpPatch("{id}/rating")]
        public async Task<ActionResult> UpdateRating(
            Guid id,
            [FromBody] UpdateDriverRatingDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (dto.Rating < 1 || dto.Rating > 5)
                {
                    return BadRequest(new { message = "Rating must be between 1 and 5" });
                }

                var driver = await _driverRepository.GetByIdAsync(id);
                if (driver == null)
                    return NotFound(new { message = "Driver not found" });

                var totalScore = driver.AverageRating * driver.TotalTrips;
                driver.TotalTrips++;

                // ✅ FIX: Cast dto.Rating (double) to decimal
                driver.AverageRating =
                    (totalScore + (decimal)dto.Rating) / driver.TotalTrips;

                driver.UpdatedAt = DateTime.UtcNow;

                await _driverRepository.UpdateAsync(driver);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new
                {
                    message = "Driver rating updated successfully",
                    averageRating = Math.Round(driver.AverageRating, 2),
                    totalTrips = driver.TotalTrips
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating driver rating", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete driver (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
  [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var driver = await _driverRepository.GetByIdAsync(id);
                if (driver == null)
                    return NotFound(new { message = "Driver not found" });

                await _driverRepository.DeleteAsync(driver);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Driver deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the driver", error = ex.Message });
            }
        }

        /// <summary>
        /// Verify driver (Admin only)
        /// </summary>
        [HttpPatch("{id}/verify")]
  [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> VerifyDriver(Guid id)
        {
            try
            {
                var driver = await _driverRepository.GetByIdAsync(id);
                if (driver == null)
                    return NotFound(new { message = "Driver not found" });

                driver.IsVerified = true;
                driver.UpdatedAt = DateTime.UtcNow;

                await _driverRepository.UpdateAsync(driver);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Driver verified successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while verifying the driver", error = ex.Message });
            }
        }

        /// <summary>
        /// Approve driver (Admin only)
        /// </summary>
        [HttpPatch("{id}/approve")]
  [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> ApproveDriver(Guid id)
        {
            try
            {
                var driver = await _driverRepository.GetByIdAsync(id);
                if (driver == null)
                    return NotFound(new { message = "Driver not found" });

                driver.IsApproved = true;
                driver.UpdatedAt = DateTime.UtcNow;

                await _driverRepository.UpdateAsync(driver);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Driver approved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while approving the driver", error = ex.Message });
            }
        }
    }
}