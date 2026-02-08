using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test1.Application.DTOs.Review
{
    public class CreateReviewDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public Guid CarId { get; set; }

        public Guid? BookingId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [StringLength(1000)]
        public string Comment { get; set; }
    }

    public class UpdateReviewDto
    {
        [Required]
        public string UserId { get; set; }

        [Range(1, 5)]
        public int? Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }
    }

    public class BulkReviewActionDto
    {
        [Required]
        public List<Guid> ReviewIds { get; set; }
    }
}
