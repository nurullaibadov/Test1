using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Test1.Application.DTOs.Notification;
using Test1.Application.Interfaces.Repositories;
using Test1.Domain.Entities;
using Test1.Domain.Enums;

namespace Test1.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public NotificationsController(
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Get all notifications (Admin only)
        /// </summary>
        [HttpGet]
  [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetAll()
        {
            try
            {
                var notifications = await _notificationRepository.GetAllAsync();
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving notifications", error = ex.Message });
            }
        }

        /// <summary>
        /// Get notification by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Notification>> GetById(Guid id)
        {
            try
            {
                var notification = await _notificationRepository.GetByIdAsync(id);
                if (notification == null)
                    return NotFound(new { message = "Notification not found" });

                return Ok(notification);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the notification", error = ex.Message });
            }
        }

        /// <summary>
        /// Get user notifications (all or unread only)
        /// Usage: /api/notifications/user/123 or /api/notifications/user/123?unreadOnly=true
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetUserNotifications(
            string userId,
            [FromQuery] bool unreadOnly = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(new { message = "User ID is required" });
                }

                var notifications =
                    await _notificationRepository.GetUserNotificationsAsync(userId, unreadOnly);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving user notifications", error = ex.Message });
            }
        }

        /// <summary>
        /// Get notifications by type
        /// FIX: Changed type parameter from NotificationType enum to string
        /// Usage: /api/notifications/user/123/type/BookingConfirmation
        /// Valid types: BookingConfirmation, BookingCancelled, DriverArrived, TripStarted, TripCompleted, PaymentProcessed, ReviewRequest
        /// </summary>
        [HttpGet("user/{userId}/type/{type}")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetByType(
            string userId,
            string type)  // ✅ FIX: Changed from NotificationType to string
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(new { message = "User ID is required" });
                }

                if (string.IsNullOrWhiteSpace(type))
                {
                    return BadRequest(new { message = "Notification type is required" });
                }

                // ✅ FIX: Convert string to NotificationType enum using Enum.TryParse
                if (!Enum.TryParse<NotificationType>(type, ignoreCase: true, out var notificationType))
                {
                    var validTypes = string.Join(", ", Enum.GetNames(typeof(NotificationType)));
                    return BadRequest(new
                    {
                        message = $"Invalid notification type. Valid types are: {validTypes}"
                    });
                }

                var notifications =
                    await _notificationRepository.GetUserNotificationsAsync(userId);

                var result = notifications
                    .Where(n => n.Type == notificationType)
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving notifications by type", error = ex.Message });
            }
        }

        /// <summary>
        /// Create new notification (Admin only)
        /// </summary>
        [HttpPost]
  [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult> Create([FromBody] CreateNotificationDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(dto.UserId))
                {
                    return BadRequest(new { message = "User ID is required" });
                }

                if (string.IsNullOrWhiteSpace(dto.Title))
                {
                    return BadRequest(new { message = "Title is required" });
                }

                if (string.IsNullOrWhiteSpace(dto.Message))
                {
                    return BadRequest(new { message = "Message is required" });
                }

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = dto.UserId,
                    Title = dto.Title,
                    Message = dto.Message,
                    Type = dto.Type,
                    RelatedEntityId = dto.RelatedEntityId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationRepository.AddAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById),
                    new { id = notification.Id },
                    notification);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the notification", error = ex.Message });
            }
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        [HttpPatch("{id}/read")]
        public async Task<ActionResult> MarkAsRead(Guid id)
        {
            try
            {
                var notification = await _notificationRepository.GetByIdAsync(id);
                if (notification == null)
                    return NotFound(new { message = "Notification not found" });

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;

                await _notificationRepository.UpdateAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while marking notification as read", error = ex.Message });
            }
        }

        /// <summary>
        /// Mark multiple notifications as read
        /// </summary>
        [HttpPatch("mark-as-read")]
        public async Task<ActionResult> MarkMultipleAsRead([FromBody] List<Guid> notificationIds)
        {
            try
            {
                if (notificationIds == null || notificationIds.Count == 0)
                {
                    return BadRequest(new { message = "Notification IDs are required" });
                }

                var notifications = await _notificationRepository.GetAllAsync();
                var notificationsToUpdate = notifications
                    .Where(n => notificationIds.Contains(n.Id) && !n.IsRead)
                    .ToList();

                if (notificationsToUpdate.Count == 0)
                {
                    return Ok(new { message = "No unread notifications to update" });
                }

                foreach (var notification in notificationsToUpdate)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                    await _notificationRepository.UpdateAsync(notification);
                }

                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = $"{notificationsToUpdate.Count} notifications marked as read" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while marking notifications as read", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete notification
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            try
            {
                var notification = await _notificationRepository.GetByIdAsync(id);
                if (notification == null)
                    return NotFound(new { message = "Notification not found" });

                await _notificationRepository.DeleteAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Notification deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the notification", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete multiple notifications
        /// </summary>
        [HttpDelete("delete-multiple")]
        public async Task<ActionResult> DeleteMultiple([FromBody] List<Guid> notificationIds)
        {
            try
            {
                if (notificationIds == null || notificationIds.Count == 0)
                {
                    return BadRequest(new { message = "Notification IDs are required" });
                }

                var notifications = await _notificationRepository.GetAllAsync();
                var notificationsToDelete = notifications
                    .Where(n => notificationIds.Contains(n.Id))
                    .ToList();

                if (notificationsToDelete.Count == 0)
                {
                    return NotFound(new { message = "No notifications found to delete" });
                }

                foreach (var notification in notificationsToDelete)
                {
                    await _notificationRepository.DeleteAsync(notification);
                }

                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = $"{notificationsToDelete.Count} notifications deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting notifications", error = ex.Message });
            }
        }

        /// <summary>
        /// Get unread notification count for user
        /// </summary>
        [HttpGet("user/{userId}/unread-count")]
        public async Task<ActionResult> GetUnreadCount(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(new { message = "User ID is required" });
                }

                var notifications = await _notificationRepository.GetUserNotificationsAsync(userId, unreadOnly: true);
                var unreadCount = notifications.Count();

                return Ok(new { unreadCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving unread count", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete all notifications for user
        /// </summary>
        [HttpDelete("user/{userId}/delete-all")]
        public async Task<ActionResult> DeleteAllUserNotifications(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(new { message = "User ID is required" });
                }

                var notifications = await _notificationRepository.GetUserNotificationsAsync(userId);

                if (notifications.Count() == 0)
                {
                    return Ok(new { message = "No notifications to delete" });
                }

                foreach (var notification in notifications)
                {
                    await _notificationRepository.DeleteAsync(notification);
                }

                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = $"All notifications for user {userId} deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting user notifications", error = ex.Message });
            }
        }
    }
}