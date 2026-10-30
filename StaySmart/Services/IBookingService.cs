using StaySmart.Models;
namespace StaySmart.Services;

public interface IBookingService
{
    Task<(Booking? Booking, string? Error)> CreateAsync(BookingInput input, string? userId);
    Task<bool> IsAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut);
    Task<(bool Ok, string? Error)> CancelAsync(int bookingId, string userId, bool isStaff, string changedBy);
    Task<(bool Ok, string? Error)> ChangeStatusAsync(int bookingId, BookingStatus newStatus, string changedBy);
}
