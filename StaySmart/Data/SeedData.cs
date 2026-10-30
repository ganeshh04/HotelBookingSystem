using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StaySmart.Models;
using StaySmart.Services;

namespace StaySmart.Data;

public static class SeedData
{
    // Demo credentials (documented in README). Change before any real deployment.
    public const string AdminEmail = "admin@staysmart.com", AdminPassword = "Admin@123";
    public const string StaffEmail = "staff@staysmart.com", StaffPassword = "Staff@123";
    public const string GuestEmail = "guest@staysmart.com", GuestPassword = "Guest@123";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<HotelDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { Roles.Admin, Roles.Staff, Roles.Guest })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        await EnsureUserAsync(userManager, AdminEmail, AdminPassword, "Hotel Admin", Roles.Admin);
        await EnsureUserAsync(userManager, StaffEmail, StaffPassword, "Front Desk", Roles.Staff);
        var guest = await EnsureUserAsync(userManager, GuestEmail, GuestPassword, "Demo Guest", Roles.Guest);

        if (!await db.Rooms.AnyAsync())
        {
            db.Rooms.AddRange(
                new Room { RoomNumber = "101", Name = "The Calm Retreat", Category = "Deluxe", Capacity = 2, PricePerNight = 3499, Description = "A thoughtfully designed room for slow mornings and restful nights.", Amenities = "King bed, Wi-Fi, Smart TV, Breakfast, Air conditioning", ImageUrl = "https://images.unsplash.com/photo-1611892440504-42a792e24d32?auto=format&fit=crop&w=1200&q=80" },
                new Room { RoomNumber = "202", Name = "Skyline Suite", Category = "Suite", Capacity = 3, PricePerNight = 5999, Description = "Extra space, elegant details and a comfortable lounge corner.", Amenities = "King bed, Lounge, Wi-Fi, Smart TV, Breakfast, Mini bar", ImageUrl = "https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=1200&q=80" },
                new Room { RoomNumber = "303", Name = "Family Haven", Category = "Family", Capacity = 4, PricePerNight = 4799, Description = "Room to reconnect, recharge and make a few new memories.", Amenities = "Two queen beds, Wi-Fi, Smart TV, Breakfast, Air conditioning", ImageUrl = "https://images.unsplash.com/photo-1566665797739-1674de7a421a?auto=format&fit=crop&w=1200&q=80" },
                new Room { RoomNumber = "104", Name = "The Work & Rest", Category = "Business", Capacity = 2, PricePerNight = 2999, Description = "A quiet, productive stay with everything within reach.", Amenities = "Work desk, Wi-Fi, Ergonomic chair, Smart TV, Coffee", ImageUrl = "https://images.unsplash.com/photo-1595576508898-0ad5c879a061?auto=format&fit=crop&w=1200&q=80" });
            await db.SaveChangesAsync();
        }

        if (!await db.Bookings.AnyAsync() && guest is not null)
        {
            var rooms = await db.Rooms.OrderBy(r => r.Id).Take(2).ToListAsync();
            var today = DateTime.Today;
            db.Bookings.AddRange(
                NewBooking(guest, rooms[0], today.AddDays(-10), today.AddDays(-8), BookingStatus.CheckedOut),
                NewBooking(guest, rooms[1], today.AddDays(14), today.AddDays(17), BookingStatus.Confirmed));
            await db.SaveChangesAsync();
        }
    }

    private static Booking NewBooking(ApplicationUser u, Room r, DateTime inD, DateTime outD, BookingStatus s)
    {
        var b = new Booking
        {
            UserId = u.Id, RoomId = r.Id, GuestName = u.FullName, Email = u.Email!, Phone = "+91 98765 43210",
            CheckIn = inD, CheckOut = outD, Guests = 2, Status = s,
            TotalPrice = PriceCalculator.Calculate(r.PricePerNight, inD, outD).Total
        };
        b.History.Add(new BookingHistory { OldStatus = null, NewStatus = "Confirmed", ChangedBy = u.Email!, ChangedAtUtc = DateTime.UtcNow.AddDays(-12) });
        if (s != BookingStatus.Confirmed)
            b.History.Add(new BookingHistory { OldStatus = "Confirmed", NewStatus = s.ToString(), ChangedBy = StaffEmail, ChangedAtUtc = DateTime.UtcNow.AddDays(-8) });
        return b;
    }

    private static async Task<ApplicationUser?> EnsureUserAsync(UserManager<ApplicationUser> um, string email, string password, string name, string role)
    {
        var user = await um.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, FullName = name, EmailConfirmed = true };
            var result = await um.CreateAsync(user, password);
            if (!result.Succeeded) return null;
        }
        if (!await um.IsInRoleAsync(user, role)) await um.AddToRoleAsync(user, role);
        return user;
    }
}
