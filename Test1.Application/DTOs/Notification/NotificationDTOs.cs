using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test1.Domain.Enums;

namespace Test1.Application.DTOs.Notification
{
    public class CreateNotificationDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Message { get; set; }

        [Required]
        [StringLength(50)]
        public NotificationType Type { get; set; }

        public Guid? RelatedEntityId { get; set; }
    }

    public class CreateBulkNotificationDto
    {
        [Required]
        public List<string> UserIds { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Message { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; }

        public Guid? RelatedEntityId { get; set; }
    }


}
