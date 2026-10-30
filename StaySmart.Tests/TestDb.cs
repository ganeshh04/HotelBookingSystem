using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using StaySmart.Data;
using StaySmart.Models;

namespace StaySmart.Tests;

internal static class TestDb
{
    /// <summary>Fresh in-memory database with one room (id 1, capacity 2, ₹1000/night).</summary>
    public static HotelDbContext Create()
    {
        var options = new DbContextOptionsBuilder<HotelDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            // The in-memory provider has no real transactions; ignore that warning (SQL Server does use them).
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new HotelDbContext(options);
        db.Rooms.Add(new Room
        {
            Id = 1, RoomNumber = "101", Name = "Test room", Category = "Deluxe", Capacity = 2, PricePerNight = 1000,
            Description = "d", Amenities = "a", ImageUrl = "x"
        });
        db.SaveChanges();
        return db;
    }

    public static BookingInput Input(int startInDays, int nights, int guests = 2) => new()
    {
        RoomId = 1, GuestName = "Test Guest", Email = "guest@test.com", Phone = "+911234567890",
        CheckIn = DateTime.Today.AddDays(startInDays), CheckOut = DateTime.Today.AddDays(startInDays + nights), Guests = guests
    };
}
