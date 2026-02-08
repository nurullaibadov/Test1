using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test1.Domain.Enums;

namespace Test1.Application.DTOs.Drive
{
    public class CreateDriverDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string LicenseNumber { get; set; }

        [Required]
        public DateTime LicenseExpiryDate { get; set; }

        [Required]
        public DateTime LicenseIssueDate { get; set; }

        [Required]
        [Range(0, 50)]
        public int YearsOfExperience { get; set; }

        [StringLength(100)]
        public string VehicleType { get; set; }
    }

    public class UpdateDriverDto
    {
        [StringLength(50)]
        public string? LicenseNumber { get; set; }

        public DateTime? LicenseExpiryDate { get; set; }

        public DateTime? LicenseIssueDate { get; set; }

        [Range(0, 50)]
        public int? YearsOfExperience { get; set; }

        [StringLength(100)]
        public string? VehicleType { get; set; }
    }

    public class UpdateDriverStatusDto
    {
        [Required]
        public DriverStatus Status { get; set; }
    }

    public class UpdateDriverLocationDto
    {
        [Required]
        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Required]
        [Range(-180, 180)]
        public double Longitude { get; set; }
    }

    public class UpdateDriverRatingDto
    {
        [Required]
        [Range(1, 5)]
        public double Rating { get; set; }
    }
}
