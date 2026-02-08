using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test1.Application.Interfaces.Repositories;
using Test1.Domain.Entities;
using Test1.Domain.Enums;
using Test1.Persistence.Context;

namespace Test1.Persistence.Repositories
{
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Payment>> GetUserPaymentsAsync(string userId)
        {
            return await _dbSet
                .Where(p => p.UserId == userId)
                .Include(p => p.Booking)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Payment?> GetPaymentByTransactionIdAsync(string transactionId)
        {
            return await _dbSet
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(PaymentStatus status)
        {
            return await _dbSet
                .Where(p => p.Status == status)
                .Include(p => p.Booking)
                .Include(p => p.User)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _dbSet.Where(p => p.Status == PaymentStatus.Completed);

            if (startDate.HasValue)
                query = query.Where(p => p.PaidAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.PaidAt <= endDate.Value);

            return await query.SumAsync(p => p.Amount);
        }
    }

    public class DriverRepository : GenericRepository<Driver>, IDriverRepository
    {
        public DriverRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Driver>> GetAvailableDriversAsync()
        {
            return await _dbSet
                .Where(d => d.Status == DriverStatus.Available && d.IsApproved && d.IsVerified)
                .Include(d => d.User)
                .ToListAsync();
        }

        public async Task<Driver?> GetDriverByUserIdAsync(string userId)
        {
            return await _dbSet
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.UserId == userId);
        }

        public async Task<IEnumerable<Driver>> GetTopRatedDriversAsync(int count = 10)
        {
            return await _dbSet
                .Where(d => d.IsApproved && d.IsVerified)
                .OrderByDescending(d => d.AverageRating)
                .Take(count)
                .Include(d => d.User)
                .ToListAsync();
        }

        public async Task UpdateDriverStatusAsync(Guid driverId, DriverStatus status)
        {
            var driver = await GetByIdAsync(driverId);
            if (driver != null)
            {
                driver.Status = status;
                await UpdateAsync(driver);
            }
        }

        public async Task UpdateDriverLocationAsync(Guid driverId, double latitude, double longitude)
        {
            var driver = await GetByIdAsync(driverId);
            if (driver != null)
            {
                driver.CurrentLatitude = latitude;
                driver.CurrentLongitude = longitude;
                driver.LastLocationUpdate = DateTime.UtcNow;
                await UpdateAsync(driver);
            }
        }
    }

    public class ReviewRepository : GenericRepository<Review>, IReviewRepository
    {
        public ReviewRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Review>> GetCarReviewsAsync(Guid carId)
        {
            return await _dbSet
                .Where(r => r.CarId == carId && r.IsApproved)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Review>> GetUserReviewsAsync(string userId)
        {
            return await _dbSet
                .Where(r => r.UserId == userId)
                .Include(r => r.Car)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Review>> GetPendingReviewsAsync()
        {
            return await _dbSet
                .Where(r => !r.IsApproved)
                .Include(r => r.User)
                .Include(r => r.Car)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<double> GetAverageRatingForCarAsync(Guid carId)
        {
            var reviews = await _dbSet
                .Where(r => r.CarId == carId && r.IsApproved)
                .ToListAsync();

            return reviews.Any() ? reviews.Average(r => r.Rating) : 0;
        }
    }

    public class LocationRepository : GenericRepository<Location>, ILocationRepository
    {
        public LocationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Location>> GetActiveLocationsAsync()
        {
            return await _dbSet
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync();
        }

        public async Task<Location?> GetNearestLocationAsync(double latitude, double longitude)
        {
            // Simple distance calculation (for production use PostGIS or similar)
            var locations = await GetActiveLocationsAsync();

            return locations
                .OrderBy(l => Math.Sqrt(
                    Math.Pow(l.Latitude - latitude, 2) +
                    Math.Pow(l.Longitude - longitude, 2)))
                .FirstOrDefault();
        }
    }

    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
        {
            var query = _dbSet.Where(n => n.UserId == userId);

            if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            var notification = await GetByIdAsync(notificationId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await UpdateAsync(notification);
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var notifications = await _dbSet
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await UpdateRangeAsync(notifications);
        }
    }


    public class CarRepository : GenericRepository<Car>, ICarRepository
    {
        public CarRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Car>> GetAvailableCarsAsync(DateTime startDate, DateTime endDate)
        {
            var bookedCarIds = await _context.Bookings
                .Where(b => b.Status != BookingStatus.Cancelled &&
                           ((b.StartDate <= startDate && b.EndDate >= startDate) ||
                            (b.StartDate <= endDate && b.EndDate >= endDate) ||
                            (b.StartDate >= startDate && b.EndDate <= endDate)))
                .Select(b => b.CarId)
                .ToListAsync();

            return await _dbSet
                .Where(c => c.Status == CarStatus.Available &&
                           c.IsAvailableForBooking &&
                           !bookedCarIds.Contains(c.Id))
                .Include(c => c.Location)
                .ToListAsync();
        }

        public async Task<IEnumerable<Car>> GetFeaturedCarsAsync(int count = 10)
        {
            return await _dbSet
                .Where(c => c.IsFeatured && c.IsAvailableForBooking && c.Status == CarStatus.Available)
                .Include(c => c.Location)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Car>> SearchCarsAsync(string searchTerm)
        {
            return await _dbSet
                .Where(c => c.Brand.Contains(searchTerm) ||
                           c.Model.Contains(searchTerm) ||
                           c.Category.Contains(searchTerm))
                .Include(c => c.Location)
                .ToListAsync();
        }

        public async Task<IEnumerable<Car>> GetCarsByCategoryAsync(string category)
        {
            return await _dbSet
                .Where(c => c.Category == category && c.IsAvailableForBooking)
                .Include(c => c.Location)
                .ToListAsync();
        }

        public async Task<IEnumerable<Car>> GetCarsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            return await _dbSet
                .Where(c => c.PricePerDay >= minPrice && c.PricePerDay <= maxPrice && c.IsAvailableForBooking)
                .Include(c => c.Location)
                .ToListAsync();
        }

        public async Task<bool> IsCarAvailableAsync(Guid carId, DateTime startDate, DateTime endDate)
        {
            var hasOverlappingBooking = await _context.Bookings
                .AnyAsync(b => b.CarId == carId &&
                              b.Status != BookingStatus.Cancelled &&
                              ((b.StartDate <= startDate && b.EndDate >= startDate) ||
                               (b.StartDate <= endDate && b.EndDate >= endDate) ||
                               (b.StartDate >= startDate && b.EndDate <= endDate)));

            return !hasOverlappingBooking;
        }

        public async Task UpdateCarStatusAsync(Guid carId, CarStatus status)
        {
            var car = await GetByIdAsync(carId);
            if (car != null)
            {
                car.Status = status;
                await UpdateAsync(car);
            }
        }


    }


    public class BookingRepository : GenericRepository<Booking>, IBookingRepository
    {
        public BookingRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Booking>> GetUserBookingsAsync(string userId)
        {
            return await _dbSet
                .Where(b => b.UserId == userId)
                .Include(b => b.Car)
                .Include(b => b.PickupLocation)
                .Include(b => b.ReturnLocation)
                .Include(b => b.Payment)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetBookingsByStatusAsync(BookingStatus status)
        {
            return await _dbSet
                .Where(b => b.Status == status)
                .Include(b => b.Car)
                .Include(b => b.User)
                .Include(b => b.PickupLocation)
                .Include(b => b.ReturnLocation)
                .ToListAsync();
        }

        public async Task<Booking?> GetBookingByNumberAsync(string bookingNumber)
        {
            return await _dbSet
                .Include(b => b.Car)
                .Include(b => b.User)
                .Include(b => b.PickupLocation)
                .Include(b => b.ReturnLocation)
                .Include(b => b.Payment)
                .Include(b => b.AssignedDriver)
                .FirstOrDefaultAsync(b => b.BookingNumber == bookingNumber);
        }

        public async Task<IEnumerable<Booking>> GetUpcomingBookingsAsync()
        {
            return await _dbSet
                .Where(b => b.StartDate > DateTime.UtcNow && b.Status == BookingStatus.Confirmed)
                .Include(b => b.Car)
                .Include(b => b.User)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetActiveBookingsAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _dbSet
                .Where(b => b.StartDate <= today &&
                           b.EndDate >= today &&
                           b.Status == BookingStatus.InProgress)
                .Include(b => b.Car)
                .Include(b => b.User)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetBookingsByCarIdAsync(Guid carId)
        {
            return await _dbSet
                .Where(b => b.CarId == carId)
                .Include(b => b.User)
                .OrderByDescending(b => b.StartDate)
                .ToListAsync();
        }

        public async Task<bool> HasOverlappingBookingsAsync(Guid carId, DateTime startDate, DateTime endDate, Guid? excludeBookingId = null)
        {
            var query = _dbSet.Where(b => b.CarId == carId &&
                                         b.Status != BookingStatus.Cancelled &&
                                         ((b.StartDate <= startDate && b.EndDate >= startDate) ||
                                          (b.StartDate <= endDate && b.EndDate >= endDate) ||
                                          (b.StartDate >= startDate && b.EndDate <= endDate)));

            if (excludeBookingId.HasValue)
            {
                query = query.Where(b => b.Id != excludeBookingId.Value);
            }

            return await query.AnyAsync();
        }
    }

}
