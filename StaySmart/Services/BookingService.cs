using System.Data;
using Microsoft.EntityFrameworkCore;
using StaySmart.Data;
using StaySmart.Models;

namespace StaySmart.Services;

public class BookingService(HotelDbContext db) : IBookingService
{
    // Two stays overlap when existing.CheckIn < requested.CheckOut AND existing.CheckOut > requested.CheckIn.
    // Same-day turnover (check-out on the next guest's check-in date) is allowed.
    public async Task<bool> IsAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut)
    {
        if (checkOut.Date <= checkIn.Date) return false;
        return !await db.Bookings.AnyAsync(b =>
            b.RoomId == roomId
            && b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.CheckedOut
            && b.CheckIn < checkOut && b.CheckOut > checkIn);
    }

    public async Task<(Booking? Booking, string? Error)> CreateAsync(BookingInput input, string? userId)
    {
        var start = input.CheckIn.Date; var end = input.CheckOut.Date;
        if (start < DateTime.Today) return (null, "Check-in cannot be in the past.");
        if (end <= start) return (null, "Check-out must be at least one day after check-in.");
        if ((end - start).Days > 30) return (null, "Stays are limited to 30 nights per booking.");

        var room = await db.Rooms.SingleOrDefaultAsync(r => r.Id == input.RoomId && r.IsActive);
        if (room is null) return (null, "This room is not available for booking.");
        if (input.Guests > room.Capacity) return (null, $"This room accommodates up to {room.Capacity} guests.");

        // SERIALIZABLE transaction: the availability check and the insert happen atomically,
        // so two guests cannot book the same room/dates at the same moment.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            if (!await IsAvailableAsync(room.Id, start, end))
                return (null, "Those dates have just been booked. Please choose another room or dates.");

            var booking = new Booking
            {
                RoomId = room.Id, UserId = userId,
                GuestName = input.GuestName.Trim(), Email = input.Email.Trim(), Phone = input.Phone.Trim(),
                CheckIn = start, CheckOut = end, Guests = input.Guests,
                TotalPrice = PriceCalculator.Calculate(room.PricePerNight, start, end).Total, // always computed server-side (incl. weekend rule)
                SpecialRequest = input.SpecialRequest?.Trim(), Status = BookingStatus.Confirmed
            };
            db.Bookings.Add(booking);
            AddHistory(booking, null, BookingStatus.Confirmed, booking.Email);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return (booking, null);
        }
        catch (DbUpdateException)
        {
            return (null, "We could not complete this booking. Please retry and check availability.");
        }
    }

    public async Task<(bool Ok, string? Error)> CancelAsync(int bookingId, string userId, bool isStaff, string changedBy)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null) return (false, "Booking not found.");
        // Authorization rule: a guest may only cancel their own booking.
        if (!isStaff && booking.UserId != userId) return (false, "You can only cancel your own bookings.");
        if (booking.Status != BookingStatus.Confirmed) return (false, "Only confirmed bookings can be cancelled.");
        // Business rule: guests must cancel at least one day before check-in.
        if (!isStaff && booking.CheckIn.Date <= DateTime.Today) return (false, "Cancellation is only possible until the day before check-in. Please contact the hotel.");
        return await ChangeStatusAsync(bookingId, BookingStatus.Cancelled, changedBy);
    }

    // Single place where a status changes, so EVERY change is written to the audit log.
    public async Task<(bool Ok, string? Error)> ChangeStatusAsync(int bookingId, BookingStatus newStatus, string changedBy)
    {
        if (!Enum.IsDefined(newStatus)) return (false, "Invalid status.");
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null) return (false, "Booking not found.");
        if (booking.Status == newStatus) return (true, null);
        // Cancelled / Checked-out are final: re-opening them would bypass the availability check.
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.CheckedOut)
            return (false, $"A {booking.Status} booking can no longer be changed.");

        AddHistory(booking, booking.Status, newStatus, changedBy);
        booking.Status = newStatus;
        await db.SaveChangesAsync();
        return (true, null);
    }

    private void AddHistory(Booking booking, BookingStatus? oldStatus, BookingStatus newStatus, string changedBy) =>
        db.BookingHistories.Add(new BookingHistory
        {
            Booking = booking, OldStatus = oldStatus?.ToString(), NewStatus = newStatus.ToString(),
            ChangedBy = changedBy, ChangedAtUtc = DateTime.UtcNow
        });
}
